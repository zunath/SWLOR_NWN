<#
.SYNOPSIS
    Builds SWLOR.Game.Server, runs the in-engine integration test suite in a
    headless Docker container, and reports pass/fail based on the JSON report
    the server writes (see SWLOR.Game.Server/Service/EngineTest.cs).

.DESCRIPTION
    This script assumes it is running on a machine where SWLOR.Game.Server/Docker
    already has modules/, hak/, and tlk/ populated with the current module and
    hak assets (the normal deploy-machine flow, e.g. after `SWLOR.CLI.exe -o`
    or the CI asset-assembly steps in .github/workflows/engine-tests.yml). It
    only builds and deploys the compiled .NET assembly, then runs the test
    container.

.PARAMETER SkipBuild
    Skip building SWLOR.Game.Server and copying its output into Docker/dotnet.
    Use this when Docker/dotnet is already up to date (e.g. a prior step in
    the same pipeline already built and staged it).

.PARAMETER Filter
    Substring passed through as SWLOR_ENGINE_TEST_FILTER. Only engine tests
    whose name or category contains this text will run.

.PARAMETER ArenaResref
    Optional override for SWLOR_ENGINE_TEST_ARENA_RESREF.

.PARAMETER Configuration
    Build configuration to use. Defaults to Release.

.EXAMPLE
    ./scripts/run-engine-tests.ps1
.EXAMPLE
    ./scripts/run-engine-tests.ps1 -SkipBuild -Filter Combat
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [string]$Filter = "",
    [string]$ArenaResref = "",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ServerProject = Join-Path $RepoRoot "SWLOR.Game.Server\SWLOR.Game.Server.csproj"
$DockerDir = Join-Path $RepoRoot "SWLOR.Game.Server\Docker"
$DotnetOutputDir = Join-Path $DockerDir "dotnet"
$ComposeFile = "docker-compose.enginetests.yml"
$ReportPath = Join-Path $DockerDir "app_logs\engine_tests\engine-test-results.json"

function Write-Section($message) {
    Write-Host ""
    Write-Host "=== $message ===" -ForegroundColor Cyan
}

if (-not $SkipBuild) {
    Write-Section "Building SWLOR.Game.Server ($Configuration)"

    # RunPostBuildEvent=Never skips the SWLOR.CLI -o post-build deploy step;
    # we do our own targeted copy below instead.
    & dotnet build $ServerProject -c $Configuration -p:RunPostBuildEvent=Never
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }

    $buildOutputDir = Join-Path $RepoRoot "SWLOR.Game.Server\bin\$Configuration\net10.0"
    if (-not (Test-Path $buildOutputDir)) {
        throw "Expected build output directory not found: $buildOutputDir"
    }

    Write-Section "Deploying build output to Docker/dotnet"
    if (Test-Path $DotnetOutputDir) {
        Remove-Item $DotnetOutputDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $DotnetOutputDir -Force | Out-Null
    Copy-Item (Join-Path $buildOutputDir "*") $DotnetOutputDir -Recurse -Force
    Write-Host "Copied $buildOutputDir -> $DotnetOutputDir"
}
else {
    Write-Section "Skipping build (assuming Docker/dotnet is already current)"
}

# Stale report from a previous run must not be mistaken for this run's result.
# If deletion fails and the server then crashes before writing a fresh report,
# we would otherwise parse the old report and report a bogus pass.
if (Test-Path $ReportPath) {
    Remove-Item $ReportPath -Force
}
if (Test-Path $ReportPath) {
    Write-Host "Could not remove stale report at $ReportPath - aborting so this run cannot be judged by a previous run's results." -ForegroundColor Red
    exit 1
}

Write-Section "Running engine tests via docker compose"
$env:SWLOR_ENGINE_TEST_FILTER = $Filter
$env:SWLOR_ENGINE_TEST_ARENA_RESREF = $ArenaResref

Push-Location $DockerDir
try {
    & docker compose -f $ComposeFile up --abort-on-container-exit --exit-code-from swlor-server
    $composeExitCode = $LASTEXITCODE

    Write-Section "Tearing down containers"
    & docker compose -f $ComposeFile down --volumes
}
finally {
    Pop-Location
}

Write-Host "docker compose exit code: $composeExitCode"

Write-Section "Engine test results"
if (-not (Test-Path $ReportPath)) {
    Write-Host "No report found at $ReportPath - the server likely crashed or was killed before it could write one." -ForegroundColor Red
    exit 1
}

$report = Get-Content $ReportPath -Raw | ConvertFrom-Json

$outcomeNames = @{ 0 = "Passed"; 1 = "Failed"; 2 = "Skipped" }
$rows = @()
foreach ($result in $report.Results) {
    $outcomeName = $outcomeNames[[int]$result.Outcome]
    if (-not $outcomeName) { $outcomeName = $result.Outcome }
    $rows += [PSCustomObject]@{
        Category = $result.Category
        Name     = $result.Name
        Outcome  = $outcomeName
        Ms       = $result.DurationMilliseconds
        Message  = $result.Message
    }
}
$rows | Sort-Object Category, Name | Format-Table -AutoSize | Out-String -Width 4096 | Write-Host

Write-Host "SUMMARY total=$($report.Total) passed=$($report.Passed) failed=$($report.Failed) skipped=$($report.Skipped)"

# The container exit status participates in the verdict: a server that crashed AFTER
# writing a passing report (e.g. during the delayed shutdown) must not be reported green.
if ($composeExitCode -eq 0 -and $report.Total -gt 0 -and $report.Failed -eq 0) {
    Write-Host "Engine tests passed." -ForegroundColor Green
    exit 0
}
else {
    if ($composeExitCode -ne 0) {
        Write-Host "Engine tests failed: server container exited with code $composeExitCode." -ForegroundColor Red
    }
    else {
        Write-Host "Engine tests failed (or none ran)." -ForegroundColor Red
    }
    exit 1
}
