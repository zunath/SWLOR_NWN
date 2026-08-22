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

.PARAMETER TimeoutMinutes
    Hard wall-clock limit for the containerized run. On expiry the stack is torn
    down and the run is reported as failed. This is a backstop against a server
    that never finishes on its own - e.g. one that boots healthy but schedules no
    tests, which stays responsive and therefore never trips the NWNX thread
    watchdog. Defaults to 90 (a full sweep takes roughly 45).

.EXAMPLE
    ./scripts/run-engine-tests.ps1
.EXAMPLE
    ./scripts/run-engine-tests.ps1 -SkipBuild -Filter Combat
.EXAMPLE
    ./scripts/run-engine-tests.ps1 -Filter Harness -TimeoutMinutes 10
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [string]$Filter = "",
    [string]$ArenaResref = "",
    [string]$Configuration = "Release",
    [string]$ServerHome = "",
    [int]$TimeoutMinutes = 90
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
# Build/stage the ENGINE TEST project, not the game project: it carries the game
# assembly along as a project reference, so its output directory contains both. The
# game project's own output deliberately excludes the test assembly, which is what
# keeps test code out of a production deploy.
$ServerProject = Join-Path $RepoRoot "SWLOR.Game.Server.EngineTests\SWLOR.Game.Server.EngineTests.csproj"

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

    # BOTH outputs are staged, game project first. A referenced project's DLL is copied
    # into the referencing project's output, but its runtimeconfig.json/deps.json are NOT -
    # and NWNX_DotNET boots the host by the GAME assembly's name, so it needs
    # SWLOR.Game.Server.runtimeconfig.json specifically. The engine-test output is layered
    # on top to add the test assembly.
    $gameOutputDir = Join-Path $RepoRoot "SWLOR.Game.Server\bin\$Configuration\net10.0"
    $engineTestOutputDir = Join-Path $RepoRoot "SWLOR.Game.Server.EngineTests\bin\$Configuration\net10.0"
    foreach ($dir in @($gameOutputDir, $engineTestOutputDir)) {
        if (-not (Test-Path $dir)) {
            throw "Expected build output directory not found: $dir"
        }
    }

    Write-Section "Deploying build output to $DotnetOutputDir"
    if (Test-Path $DotnetOutputDir) {
        Remove-Item $DotnetOutputDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $DotnetOutputDir -Force | Out-Null
    foreach ($dir in @($gameOutputDir, $engineTestOutputDir)) {
        Copy-Item (Join-Path $dir "*") $DotnetOutputDir -Recurse -Force
        Write-Host "Copied $dir -> $DotnetOutputDir"
    }

}
else {
    Write-Section "Skipping build (assuming $DotnetOutputDir is already current)"
}

# Checked for BOTH paths, not just after a build: with -SkipBuild against a stale
# game-only staging directory (exactly the state left behind by older revisions of
# this script, before the harness was its own assembly) the server would boot,
# schedule nothing, and burn the entire wall clock before failing. Fail here in a
# second instead.
$stagedTestAssembly = Join-Path $DotnetOutputDir "SWLOR.Game.Server.EngineTests.dll"
$stagedRuntimeConfig = Join-Path $DotnetOutputDir "SWLOR.Game.Server.runtimeconfig.json"
if (-not (Test-Path $stagedTestAssembly)) {
    Write-Host "$DotnetOutputDir is missing SWLOR.Game.Server.EngineTests.dll - no engine tests would run. Re-run without -SkipBuild to stage it." -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $stagedRuntimeConfig)) {
    Write-Host "$DotnetOutputDir is missing SWLOR.Game.Server.runtimeconfig.json - the NWNX .NET host cannot boot. Re-run without -SkipBuild to stage it." -ForegroundColor Red
    exit 1
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
# Everything mutated below is restored in the outer finally: PowerShell env changes
# outlive the script in the calling shell, and a leaked SWLOR_ENGINE_TEST_HAK_DIR
# from an auto-share would silently redirect a later run's hak mount. The snapshot
# happens BEFORE the try so every mutation - including the hak auto-share and its
# failure exits - is inside the restoration scope (finally blocks run on `exit`).
$previousFilter = $env:SWLOR_ENGINE_TEST_FILTER
$previousArenaResref = $env:SWLOR_ENGINE_TEST_ARENA_RESREF
$previousHakDir = $env:SWLOR_ENGINE_TEST_HAK_DIR
$previousHome = $env:SWLOR_ENGINE_TEST_HOME
try {
    $env:SWLOR_ENGINE_TEST_FILTER = $Filter
    $env:SWLOR_ENGINE_TEST_ARENA_RESREF = $ArenaResref

    # The compose file's mounts interpolate SWLOR_ENGINE_TEST_HOME rather than PWD:
    # native Windows PowerShell never exports a PWD environment variable (Push-Location
    # only updates the automatic $PWD), so ${PWD}-based interpolation would silently
    # mount the compose-file directory instead of the selected server home.
    $env:SWLOR_ENGINE_TEST_HOME = $ServerHome

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

        # HARD WALL CLOCK. `up --abort-on-container-exit` blocks until a container exits, and
        # a server that never schedules its tests (missing harness, bad filter) idles happily
        # forever - it is responsive, so the NWNX thread watchdog never fires either. Without
        # this, such a run hangs until someone notices. A watchdog job force-downs the stack
        # after the deadline, which makes `up` return.
        $timeoutSeconds = [int]($TimeoutMinutes * 60)
        $timedOutMarker = Join-Path ([System.IO.Path]::GetTempPath()) "swlor-engine-tests-timeout-$PID.marker"
        Remove-Item $timedOutMarker -Force -ErrorAction SilentlyContinue
        $watchdog = Start-Job -ScriptBlock {
            param($seconds, $project, $file, $marker)
            Start-Sleep -Seconds $seconds
            New-Item -ItemType File -Path $marker -Force | Out-Null
            & docker compose -p $project -f $file down --volumes --remove-orphans 2>&1 | Out-Null
        } -ArgumentList $timeoutSeconds, $ComposeProject, $ComposeFile, $timedOutMarker

        try {
            & docker compose -p $ComposeProject -f $ComposeFile up --abort-on-container-exit --exit-code-from swlor-server 2>&1 | ForEach-Object { "$_" } | Write-Host
            $composeExitCode = $LASTEXITCODE
        }
        finally {
            Stop-Job $watchdog -ErrorAction SilentlyContinue
            Remove-Job $watchdog -Force -ErrorAction SilentlyContinue
        }

        if (Test-Path $timedOutMarker) {
            Remove-Item $timedOutMarker -Force -ErrorAction SilentlyContinue
            Write-Host "TIMED OUT after $TimeoutMinutes minute(s) - the run was killed. The server never finished (look for 'ENGINE TEST HARNESS MISSING' or a stalled test above)." -ForegroundColor Red
            $composeExitCode = 124
        }

        Write-Section "Tearing down containers"
        & docker compose -p $ComposeProject -f $ComposeFile down --volumes 2>&1 | ForEach-Object { "$_" } | Write-Host
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
        Pop-Location
    }
}
finally {
    $env:SWLOR_ENGINE_TEST_FILTER = $previousFilter
    $env:SWLOR_ENGINE_TEST_ARENA_RESREF = $previousArenaResref
    $env:SWLOR_ENGINE_TEST_HAK_DIR = $previousHakDir
    $env:SWLOR_ENGINE_TEST_HOME = $previousHome
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
