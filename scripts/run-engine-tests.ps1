<#
.SYNOPSIS
    Builds SWLOR.Game.Server, runs the in-engine integration test suite in a
    headless Docker container, and reports pass/fail based on the JSON report
    the server writes (see SWLOR.Game.Server/Service/EngineTest.cs).

.DESCRIPTION
    Runs against a server home directory whose modules/, hak/, tlk/, and
    swlor.env are already populated (the normal dev/deploy-machine flow). The
    home resolves to -ServerHome, then SWLOR_ENGINE_TEST_SERVER_HOME, then the
    repo's debugserver/ directory, then SWLOR.Game.Server/Docker (the layout
    the CI workflow stages). The script only builds and stages the compiled
    .NET assembly into the home, then runs the test container against it.

.PARAMETER SkipBuild
    Skip building SWLOR.Game.Server and copying its output into the server
    home's dotnet/ directory.
    Use this when Docker/dotnet is already up to date (e.g. a prior step in
    the same pipeline already built and staged it).

.PARAMETER Filter
    Substring passed through as SWLOR_ENGINE_TEST_FILTER. Only engine tests
    whose name or category contains this text will run.

.PARAMETER ArenaResref
    Optional override for SWLOR_ENGINE_TEST_ARENA_RESREF.

.PARAMETER Configuration
    Build configuration to use. Defaults to Release.

.PARAMETER ServerHome
    The NWN home directory to run against (holds modules/, hak/, tlk/, swlor.env;
    receives dotnet/ and app_logs/). Defaults to SWLOR_ENGINE_TEST_SERVER_HOME,
    then the repo's debugserver/ directory if present, then SWLOR.Game.Server/Docker.

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
    [string]$Configuration = "Release",
    [string]$ServerHome = ""
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ServerProject = Join-Path $RepoRoot "SWLOR.Game.Server\SWLOR.Game.Server.csproj"

# The server home is the directory mounted as /nwn/home: it holds modules/, hak/,
# tlk/, dotnet/, swlor.env, and receives app_logs/. SWLOR.Game.Server/Docker only
# holds the tracked compose configuration - it is NOT a runtime home. Resolution
# order: -ServerHome parameter, SWLOR_ENGINE_TEST_SERVER_HOME env var, the
# repo's debugserver/ directory (the dev-machine convention), and finally
# SWLOR.Game.Server/Docker as the last resort (what the CI workflow stages).
if (-not $ServerHome) {
    $ServerHome = $env:SWLOR_ENGINE_TEST_SERVER_HOME
}
if (-not $ServerHome) {
    # A dedicated enginetests-home keeps the test server fully separate from the standard
    # dev/testing server that uses debugserver/ - no shared logs, database, report paths,
    # or module file locks. debugserver/ remains only as a shared-with-dev fallback.
    $engineTestsHomeDir = Join-Path $RepoRoot "enginetests-home"
    $debugServerDir = Join-Path $RepoRoot "debugserver"
    if (Test-Path (Join-Path $engineTestsHomeDir "swlor.env")) {
        $ServerHome = $engineTestsHomeDir
    }
    elseif (Test-Path (Join-Path $debugServerDir "swlor.env")) {
        $ServerHome = $debugServerDir
        Write-Host "WARNING: using debugserver/ as the server home - this is SHARED with the standard dev server (logs, database, module file). Create <repo>\enginetests-home (modules, hak, tlk, swlor.env) for full isolation." -ForegroundColor Yellow
    }
    else {
        $ServerHome = Join-Path $RepoRoot "SWLOR.Game.Server\Docker"
    }
}
if (-not (Test-Path (Join-Path $ServerHome "swlor.env"))) {
    Write-Host "Server home '$ServerHome' has no swlor.env - it doesn't look like an NWN home directory." -ForegroundColor Red
    exit 1
}

$ComposeFile = Join-Path $RepoRoot "SWLOR.Game.Server\Docker\docker-compose.enginetests.yml"
# A dedicated project name keeps these containers isolated from the normal dev
# stack even though both may use the same server home directory.
$ComposeProject = "swlor-engine-tests"
$DotnetOutputDir = Join-Path $ServerHome "dotnet"
$ReportPath = Join-Path $ServerHome "app_logs\engine_tests\engine-test-results.json"

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

    Write-Section "Deploying build output to $DotnetOutputDir"
    if (Test-Path $DotnetOutputDir) {
        Remove-Item $DotnetOutputDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $DotnetOutputDir -Force | Out-Null
    Copy-Item (Join-Path $buildOutputDir "*") $DotnetOutputDir -Recurse -Force
    Write-Host "Copied $buildOutputDir -> $DotnetOutputDir"
}
else {
    Write-Section "Skipping build (assuming $DotnetOutputDir is already current)"
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

Write-Section "Running engine tests via docker compose (server home: $ServerHome)"
# Everything set here is restored at the end of the run: PowerShell env changes
# outlive the script in the calling shell, and a leaked SWLOR_ENGINE_TEST_HAK_DIR
# from an auto-share would silently redirect a later run's hak mount.
$previousFilter = $env:SWLOR_ENGINE_TEST_FILTER
$previousArenaResref = $env:SWLOR_ENGINE_TEST_ARENA_RESREF
$previousHakDir = $env:SWLOR_ENGINE_TEST_HAK_DIR
$env:SWLOR_ENGINE_TEST_FILTER = $Filter
$env:SWLOR_ENGINE_TEST_ARENA_RESREF = $ArenaResref

# Share the dev server's hak set via a dedicated Docker mount when the test home has
# none of its own - NTFS junctions do not survive Docker bind mounts, so this is the
# supported way to avoid duplicating ~13GB of haks.
if (-not $env:SWLOR_ENGINE_TEST_HAK_DIR) {
    $homeHakDir = Join-Path $ServerHome "hak"
    if (-not (Test-Path (Join-Path $homeHakDir "*.hak"))) {
        # Probe both next to the repo root and next to the server home - when running
        # from a worktree, debugserver/ only exists beside the real server homes.
        $devHakCandidates = @(
            (Join-Path $RepoRoot "debugserver\hak"),
            (Join-Path (Split-Path -Parent $ServerHome) "debugserver\hak")
        )
        foreach ($devHakDir in $devHakCandidates) {
            if (Test-Path (Join-Path $devHakDir "*.hak")) {
                $env:SWLOR_ENGINE_TEST_HAK_DIR = $devHakDir
                Write-Host "Sharing hak set from $devHakDir (test home has no haks of its own)."
                break
            }
        }
        if (-not $env:SWLOR_ENGINE_TEST_HAK_DIR) {
            Write-Host "Server home '$ServerHome' has no haks and no debugserver hak set was found - the module will fail to load." -ForegroundColor Red
            exit 1
        }
    }
}

# Run from the server home so the compose file's ${PWD-.} mounts resolve to it.
Push-Location $ServerHome
# docker compose writes progress to stderr; under ErrorActionPreference=Stop with
# redirected output (CI, transcripts, IDE consoles) every such line would become a
# terminating NativeCommandError. Relax it for the native compose calls only.
$previousErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = "Continue"
try {
    # Remove anything a previously interrupted run left behind before starting fresh.
    & docker compose -p $ComposeProject -f $ComposeFile down --volumes --remove-orphans 2>&1 | ForEach-Object { "$_" } | Write-Host

    & docker compose -p $ComposeProject -f $ComposeFile up --abort-on-container-exit --exit-code-from swlor-server 2>&1 | ForEach-Object { "$_" } | Write-Host
    $composeExitCode = $LASTEXITCODE

    Write-Section "Tearing down containers"
    & docker compose -p $ComposeProject -f $ComposeFile down --volumes 2>&1 | ForEach-Object { "$_" } | Write-Host
}
finally {
    $ErrorActionPreference = $previousErrorActionPreference
    Pop-Location
    $env:SWLOR_ENGINE_TEST_FILTER = $previousFilter
    $env:SWLOR_ENGINE_TEST_ARENA_RESREF = $previousArenaResref
    $env:SWLOR_ENGINE_TEST_HAK_DIR = $previousHakDir
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
