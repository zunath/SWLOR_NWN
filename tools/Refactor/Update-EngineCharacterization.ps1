<#
.SYNOPSIS
    Refreshes the engine characterization snapshots after a master sync (maintainability refactor, item P0-7c).

.DESCRIPTION
    The engine scenarios (SWLOR.Game.Server.EngineTests/Characterization) depend on the module and on the 2DA, ability
    and TLK data as well as on the code, and master changes that content often. This script redoes the whole
    procedure of Readmes/EngineTesting.md ("Characterization scenarios") in the current worktree:

      1. Preflight: this worktree's sparse SWLOR_Haks is at the commit the branch pins and clean; the snapshot folder has
         no local changes; Docker answers; no other `swlor-engine-tests` compose project exists (see below).
      2. Builds SWLOR.CLI and packs the module from this worktree's Module/ folder under a throwaway name.
      3. Rebuilds the haks sw_2da and sw_ability and copies sw_tlk.tlk from this worktree's SWLOR_Haks.
      4. Prepares a throwaway server home outside the repository (swlor.env and settings.tml from
         SWLOR.Game.Server/Docker, the packed module, the TLK) and a hak set: every other hak is a NTFS hard link to the
         hak set of the shared dev server (see -BaseHakDirectory), so the 11 GB are not copied.
      5. Update pass: scripts/run-engine-tests.ps1 -Filter Characterization with SWLOR_UPDATE_SNAPSHOTS=1. The runner copies
         every snapshot that differs into SWLOR.Game.Server.EngineTests/Characterization/Snapshots.
      6. Prints the snapshot files that changed (git status and the changed row count of each).
      7. Confirm pass: the same run without SWLOR_UPDATE_SNAPSHOTS (it builds again, so the new snapshots are embedded).
         Skipped when the update pass changed nothing, because that pass already compared every snapshot.
      8. Removes the throwaway home (unless -KeepHome) and the compose containers its runs created.

    The script never commits and never reverts anything. It does not explain the changes: for every changed row, name the
    master commit or file that explains it (git log origin/master -- <file>); a change that master content does not
    explain is a regression, not drift, and must not be committed. Review with
    `git diff -U0 -- SWLOR.Game.Server.EngineTests/Characterization/Snapshots`.

    Why a separate script and not a switch on Sync-MaintainabilityBranch.ps1: the sync script is Docker-free and tests
    code only; this refresh needs Docker, exclusive use of one compose project, a packed module and about 30 minutes (measured
    28: pack 3.5 minutes, hak build 10 seconds, each of the two engine runs 12 minutes including its build). It is also
    useful without a merge, for example after a SWLOR_Haks pointer change.

    The shared compose project: scripts/run-engine-tests.ps1 always uses the project `swlor-engine-tests` and begins and ends
    with `docker compose down --volumes`, so two runs destroy each other. This script therefore refuses to start while
    that project has any container (exit 5) and never stops, removes or touches such a container. Wait and run it again,
    or pass -WaitMinutes to poll every 30 seconds. If containers are left over from a crashed run and you are sure nobody
    is using them, remove them yourself:
    docker compose -p swlor-engine-tests -f SWLOR.Game.Server/Docker/docker-compose.enginetests.yml down --volumes --remove-orphans

    Exit code: 0 the confirm pass (or the update pass when nothing changed) passed; 1 an engine run failed (the snapshots
    written by a failed update pass are partial: do not commit them); 4 the script could not do its job (missing haks or
    Docker, dirty snapshot folder, bad arguments); 5 the compose project is in use.

    Written for Windows PowerShell 5.1 (the only one tried) and PowerShell 7. Needs Docker, the .NET SDK, and NTFS for the hard links (a copy of the
    hak set is the fallback and takes 11 GB).

.PARAMETER CheckOnly
    Do not update: run only the comparison (the confirm pass) and report whether the snapshots match this branch.

.PARAMETER KeepHome
    Keep the throwaway work folder (home, hak set) and print its path. It holds about 0.4 GB plus the hard links; remove it
    yourself when done.

.PARAMETER ReuseHome
    A work folder kept by an earlier -KeepHome run: skip packing and the hak build and use it as it is. The module in it
    is as old as that run, so use it only to repeat a run on an unchanged branch. It is never deleted.

.PARAMETER WorkDirectory
    A new folder for the throwaway work folder (it must not exist and must be outside the repository). Default: a folder
    under C:\Temp (or the system temp folder).

.PARAMETER BaseHakDirectory
    The hak set that supplies every hak except sw_2da and sw_ability. Default, first one that has every hak in
    Build/hakbuilder.json: this parameter, $env:SWLOR_ENGINE_TEST_HAK_DIR, <worktree>\debugserver\hak, the main
    checkout's debugserver\hak. Only the folder is read; the files are linked, never written.

.PARAMETER WaitMinutes
    When the compose project is in use, poll every 30 seconds for this long before giving up with exit 5. Default 0.

.PARAMETER TimeoutMinutes
    Wall-clock limit of each engine run, passed to scripts/run-engine-tests.ps1. Default 60.

.EXAMPLE
    powershell -NoProfile -File tools/Refactor/Update-EngineCharacterization.ps1
    powershell -NoProfile -File tools/Refactor/Update-EngineCharacterization.ps1 -WaitMinutes 45
    powershell -NoProfile -File tools/Refactor/Update-EngineCharacterization.ps1 -CheckOnly
#>
[CmdletBinding()]
param(
    [switch]$CheckOnly,
    [switch]$KeepHome,
    [string]$ReuseHome,
    [string]$WorkDirectory,
    [string]$BaseHakDirectory,
    [int]$WaitMinutes = 0,
    [int]$TimeoutMinutes = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'RefactorCommon.ps1')

$ComposeProject = 'swlor-engine-tests'
$SnapshotsRelative = 'SWLOR.Game.Server.EngineTests/Characterization/Snapshots'
# The haks whose content the scenarios read and that this script rebuilds from SWLOR_Haks. sw_2da holds every 2DA (754
# files) and sw_ability the ability resources; all other haks only carry models, textures, sounds and the like.
$RebuiltHaks = @('sw_2da', 'sw_ability')
$TlkName = 'sw_tlk.tlk'
# A name of its own, so a module the developer packed in Module/ is never overwritten; swlor.env in the throwaway home
# names it.
$ModuleName = 'swlor_et_characterization'
$ExitComposeBusy = 5

function ConvertTo-ForwardSlash {
    param([string]$Path)
    return $Path.Replace('\', '/')
}

function Format-Size {
    param([long]$Bytes)
    if ($Bytes -ge 1GB) { return ('{0:N1} GB' -f ($Bytes / 1GB)) }
    return ('{0:N1} MB' -f ($Bytes / 1MB))
}

function Get-DefaultWorkDirectory {
    $base = [System.IO.Path]::GetTempPath()
    if ($script:OnWindows -and (Test-Path -LiteralPath 'C:\Temp' -PathType Container)) { $base = 'C:\Temp' }
    return (Join-Path $base ('swlor-engine-characterization-' + [guid]::NewGuid().ToString('N').Substring(0, 8)))
}

function Test-PathInside {
    param([string]$Path, [string]$Folder)
    $full = [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $parent = [System.IO.Path]::GetFullPath($Folder).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    return $full.StartsWith($parent, [System.StringComparison]::OrdinalIgnoreCase)
}

# --- The shared compose project -------------------------------------------------------------------------------------

# One line per container of the swlor-engine-tests project, running or not. Nothing here changes a container.
function Get-ComposeContainers {
    $docker = Resolve-Executable 'docker'
    $result = Invoke-Process -FilePath $docker -Arguments @('ps', '-a', '--filter', "label=com.docker.compose.project=$ComposeProject", '--format', '{{.Names}} ({{.Status}})')
    if ($result.ExitCode -ne 0) {
        throw (New-Failure ("Docker did not answer (docker ps exit $($result.ExitCode)): " + ($result.Error.Trim() + ' ' + $result.Output.Trim()).Trim() + ' Start Docker Desktop and run this again.') 4)
    }
    return @($result.Lines)
}

# Returns when the project has no container; throws (exit code 5) when it does not become free in time.
function Wait-ComposeFree {
    param([int]$Minutes)
    $deadline = (Get-Date).AddMinutes($Minutes)
    while ($true) {
        $containers = @(Get-ComposeContainers)
        if ($containers.Count -eq 0) { return }
        if ((Get-Date) -ge $deadline) {
            $list = ($containers | ForEach-Object { "  $_" }) -join "`n"
            throw (New-Failure ("The compose project '$ComposeProject' is in use (another agent may be running the engine tests):`n$list`nThis script never stops it. Wait for that run to end and run this again, or pass -WaitMinutes. If these are leftovers of a crashed run and nobody uses them, remove them yourself (see the script help).") $ExitComposeBusy)
        }
        Write-Host ("The compose project '$ComposeProject' is in use ({0} container(s)); checking again in 30 seconds ..." -f $containers.Count)
        Start-Sleep -Seconds 30
    }
}

# --- Inputs ---------------------------------------------------------------------------------------------------------

function Get-MainCheckout {
    param([string]$Root)
    $common = (Invoke-Git -Repository $Root -Arguments @('rev-parse', '--path-format=absolute', '--git-common-dir')).Lines[0]
    return (Split-Path -Parent $common)
}

function Get-HakNamesFromBuildConfig {
    param([string]$Root)
    $config = Get-Content -LiteralPath (Join-Path $Root 'Build/hakbuilder.json') -Raw | ConvertFrom-Json
    return @($config.HakList | ForEach-Object { $_.Name })
}

# The hak set that supplies the haks this script does not rebuild: the first candidate that has all of them.
function Resolve-BaseHakDirectory {
    param([string]$Root, [string]$Requested, [string[]]$HakNames)
    $candidates = New-Object 'System.Collections.Generic.List[string]'
    if ($Requested) { $candidates.Add($Requested) }
    if ($env:SWLOR_ENGINE_TEST_HAK_DIR) { $candidates.Add($env:SWLOR_ENGINE_TEST_HAK_DIR) }
    $candidates.Add((Join-Path $Root 'debugserver/hak'))
    $candidates.Add((Join-Path (Get-MainCheckout $Root) 'debugserver/hak'))

    $needed = @($HakNames | Where-Object { $RebuiltHaks -notcontains $_ })
    $problems = New-Object 'System.Collections.Generic.List[string]'
    foreach ($candidate in $candidates) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Container)) { $problems.Add("$candidate (no such folder)"); continue }
        $missing = @($needed | Where-Object { -not (Test-Path -LiteralPath (Join-Path $candidate "$_.hak") -PathType Leaf) })
        if ($missing.Count -eq 0) { return [System.IO.Path]::GetFullPath($candidate) }
        $problems.Add("$candidate (missing $($missing.Count) hak(s), for example $(@($missing | Select-Object -First 3) -join ', '))")
    }
    throw (New-Failure ("No hak set supplies the haks that are not rebuilt here. Tried:`n  " + ($problems -join "`n  ") + "`nPass -BaseHakDirectory with a folder that holds every hak of Build/hakbuilder.json except $($RebuiltHaks -join ' and '), usually the dev server's debugserver\hak.") 4)
}

# --- Throwaway home -------------------------------------------------------------------------------------------------

function Build-Cli {
    param([string]$Root)
    Write-Section 'Building SWLOR.CLI (Release)'
    Invoke-DotnetBuild -Project (Join-Path $Root 'SWLOR.CLI/SWLOR.CLI.csproj') -Configuration 'Release'
    $dll = Join-Path $Root 'SWLOR.CLI/bin/Release/net10.0/SWLOR.CLI.dll'
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) { throw (New-Failure "SWLOR.CLI.dll was not built at $dll." 4) }
    return $dll
}

function Write-Home {
    param([string]$Root, [string]$HomePath)
    foreach ($name in @('modules', 'tlk', 'hak', 'logs', 'app_logs')) { New-Item -ItemType Directory -Force -Path (Join-Path $HomePath $name) | Out-Null }
    $docker = Join-Path $Root 'SWLOR.Game.Server/Docker'
    # The tracked files hold no secrets. The server loads the module that swlor.env names.
    $envText = [System.IO.File]::ReadAllText((Join-Path $docker 'swlor.env'))
    $pattern = '(?m)^NWN_MODULE=.*$'
    if (-not [regex]::IsMatch($envText, $pattern)) { throw (New-Failure 'SWLOR.Game.Server/Docker/swlor.env has no NWN_MODULE line.' 4) }
    $envText = [regex]::Replace($envText, $pattern, ('NWN_MODULE="{0}"' -f $ModuleName))
    [System.IO.File]::WriteAllText((Join-Path $HomePath 'swlor.env'), $envText, $script:Utf8)
    Copy-Item -LiteralPath (Join-Path $docker 'settings.tml') -Destination (Join-Path $HomePath 'settings.tml')
}

function Pack-Module {
    param([string]$Root, [string]$CliDll, [string]$HomePath)
    Write-Section 'Packing the module from this worktree (about 2-3 minutes)'
    $moduleFolder = Join-Path $Root 'Module'
    $started = Get-Date
    $result = Invoke-Process -FilePath (Resolve-Executable 'dotnet') -Arguments @($CliDll, '--pack', "./$ModuleName.mod", '--no-prompt') -WorkingDirectory $moduleFolder -Environment (Get-DotnetEnvironment)
    $seconds = [int]((Get-Date) - $started).TotalSeconds
    $packed = Join-Path $moduleFolder "$ModuleName.mod"
    if ($result.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $packed -PathType Leaf)) {
        $tail = @(($result.Lines + @($result.Error -split "`r?`n")) | Where-Object { $_ -and $_ -notmatch '^Packed [0-9,]+/' } | Select-Object -Last 25) -join "`n"
        throw (New-Failure "Packing the module failed (exit $($result.ExitCode)) after ${seconds}s:`n$tail" 4)
    }
    $target = Join-Path (Join-Path $HomePath 'modules') "$ModuleName.mod"
    Move-Item -LiteralPath $packed -Destination $target -Force
    $size = (Get-Item -LiteralPath $target).Length
    Write-Host ("Packed {0}.mod: {1} in {2}s" -f $ModuleName, (Format-Size $size), $seconds)
    return [pscustomobject]@{ Seconds = $seconds; Bytes = $size }
}

function Build-Haks {
    param([string]$Root, [string]$CliDll, [string]$WorkPath, [string]$HomePath, [string]$HakPath)
    Write-Section "Rebuilding $($RebuiltHaks -join ', ') and copying the TLK from SWLOR_Haks"
    $haks = Join-Path $Root 'SWLOR_Haks'
    $configFolder = Join-Path $WorkPath 'hakbuild-config'
    $outputFolder = Join-Path $WorkPath 'hakbuild-output'
    New-Item -ItemType Directory -Force -Path $configFolder | Out-Null
    $config = [ordered]@{
        TlkPath                = ConvertTo-ForwardSlash (Join-Path (Join-Path $haks 'sw_tlk') $TlkName)
        OutputPath             = (ConvertTo-ForwardSlash $outputFolder) + '/'
        EnableChecksumChecking = $false
        HakList                = @($RebuiltHaks | ForEach-Object {
                [ordered]@{ Name = $_; Path = (ConvertTo-ForwardSlash (Join-Path $haks $_)) + '/'; CompileModels = $false }
            })
    }
    [System.IO.File]::WriteAllText((Join-Path $configFolder 'hakbuilder.json'), ($config | ConvertTo-Json -Depth 5), $script:Utf8)

    $started = Get-Date
    $result = Invoke-Process -FilePath (Resolve-Executable 'dotnet') -Arguments @($CliDll, '--hak') -WorkingDirectory $configFolder -Environment (Get-DotnetEnvironment)
    $seconds = [int]((Get-Date) - $started).TotalSeconds
    if ($result.ExitCode -ne 0) { throw (New-Failure ("Building the haks failed (exit $($result.ExitCode)): " + ($result.Error.Trim() + ' ' + $result.Output.Trim()).Trim()) 4) }

    foreach ($name in $RebuiltHaks) {
        $built = Join-Path (Join-Path $outputFolder 'hak') "$name.hak"
        if (-not (Test-Path -LiteralPath $built -PathType Leaf) -or (Get-Item -LiteralPath $built).Length -eq 0) { throw (New-Failure "$name.hak was not built (looked for $built).`n$($result.Output.Trim())" 4) }
        Move-Item -LiteralPath $built -Destination (Join-Path $HakPath "$name.hak") -Force
        Write-Host ("Built {0}.hak: {1}" -f $name, (Format-Size (Get-Item -LiteralPath (Join-Path $HakPath "$name.hak")).Length))
    }
    $tlk = Join-Path (Join-Path $outputFolder 'tlk') $TlkName
    if (-not (Test-Path -LiteralPath $tlk -PathType Leaf)) { throw (New-Failure "$TlkName was not copied (looked for $tlk)." 4) }
    Move-Item -LiteralPath $tlk -Destination (Join-Path (Join-Path $HomePath 'tlk') $TlkName) -Force
    Write-Host ("Copied {0}: {1}" -f $TlkName, (Format-Size (Get-Item -LiteralPath (Join-Path (Join-Path $HomePath 'tlk') $TlkName)).Length))
    Remove-Item -LiteralPath $configFolder, $outputFolder -Recurse -Force -ErrorAction SilentlyContinue
    return $seconds
}

# Hard-links every hak the script does not rebuild into $HakPath (copies when the link fails; that needs 11 GB).
function Link-Haks {
    param([string]$BaseHaks, [string]$HakPath, [string[]]$HakNames)
    Write-Section 'Linking the other haks'
    $linked = 0
    $copied = 0
    foreach ($name in ($HakNames | Where-Object { $RebuiltHaks -notcontains $_ })) {
        $source = Join-Path $BaseHaks "$name.hak"
        $target = Join-Path $HakPath "$name.hak"
        try {
            New-Item -ItemType HardLink -Path $target -Value $source | Out-Null
            $linked++
        }
        catch {
            Copy-Item -LiteralPath $source -Destination $target
            $copied++
        }
    }
    Write-Host ("{0} hak(s) linked, {1} copied, from {2}" -f $linked, $copied, $BaseHaks)
    if ($copied -gt 0) { Write-Warning 'Hard links failed (different volume?), so haks were copied; the work folder is large.' }
    return [pscustomobject]@{ Linked = $linked; Copied = $copied }
}

# --- Engine runs ----------------------------------------------------------------------------------------------------

function Invoke-EngineRun {
    param([string]$Root, [string]$HomePath, [string]$HakPath, [bool]$UpdateSnapshots, [string]$Title)
    Write-Section $Title
    Wait-ComposeFree -Minutes $WaitMinutes
    $reportPath = Join-Path $HomePath 'app_logs/engine_tests/engine-test-results.json'
    $environment = @{
        SWLOR_ENGINE_TEST_HAK_DIR               = $HakPath
        SWLOR_ENGINE_TEST_SERVER_HOME           = $null
        SWLOR_ENGINE_TEST_SHUTDOWN              = $null
        SWLOR_ENGINE_TEST_STARTUP_DELAY_SECONDS = $null
        SWLOR_SNAPSHOT_DIRECTORY                = $null
        SWLOR_UPDATE_SNAPSHOTS                  = $(if ($UpdateSnapshots) { '1' } else { $null })
        MSBUILDUSESERVER                        = '0'
        DOTNET_CLI_TELEMETRY_OPTOUT             = '1'
        DOTNET_NOLOGO                           = '1'
    }
    $arguments = @('-NoProfile', '-NonInteractive', '-File', (Join-Path $Root 'scripts/run-engine-tests.ps1'),
        '-Filter', 'Characterization', '-ServerHome', $HomePath, '-TimeoutMinutes', [string]$TimeoutMinutes)
    $started = Get-Date
    $run = Invoke-Process -FilePath (Get-HostExecutable) -Arguments $arguments -WorkingDirectory $Root -Environment $environment -Stream
    $seconds = [int]((Get-Date) - $started).TotalSeconds

    $summary = $null
    if (Test-Path -LiteralPath $reportPath -PathType Leaf) {
        $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
        $summary = [pscustomobject]@{ Total = [int]$report.Total; Passed = [int]$report.Passed; Failed = [int]$report.Failed; Skipped = [int]$report.Skipped }
    }
    $text = if ($summary) { "$($summary.Passed) passed, $($summary.Failed) failed, $($summary.Skipped) skipped of $($summary.Total)" } else { 'no report' }
    $ok = ($run.ExitCode -eq 0 -and $summary -and $summary.Total -gt 0 -and $summary.Failed -eq 0)
    Write-Host ("{0}: exit {1}, {2} ({3} min {4} s)" -f $Title, $run.ExitCode, $text, [int][math]::Floor($seconds / 60), ($seconds % 60))
    return [pscustomobject]@{ Ok = [bool]$ok; ExitCode = $run.ExitCode; Text = $text; Seconds = $seconds }
}

# --- Result ---------------------------------------------------------------------------------------------------------

# Snapshot files git sees as changed, with their changed line counts (a snapshot has one row per line).
function Get-SnapshotChanges {
    param([string]$Root)
    $changes = New-Object 'System.Collections.Generic.List[object]'
    $status = @((Invoke-Git -Repository $Root -Arguments @('status', '--porcelain', '--untracked-files=all', '--', $SnapshotsRelative)).Lines)
    $numstat = @{}
    foreach ($line in (Invoke-Git -Repository $Root -Arguments @('diff', '--numstat', '--', $SnapshotsRelative)).Lines) {
        $parts = $line -split "`t", 3
        if ($parts.Count -eq 3) { $numstat[$parts[2]] = "+$($parts[0]) -$($parts[1])" }
    }
    foreach ($line in $status) {
        if ($line.Length -lt 4) { continue }
        $code = $line.Substring(0, 2).Trim()
        $path = $line.Substring(3).Trim('"')
        $rows = if ($numstat.ContainsKey($path)) { $numstat[$path] } else { '' }
        $changes.Add([pscustomobject]@{ Status = $code; Path = $path; Rows = $rows })
    }
    return $changes.ToArray()
}

function Remove-WorkFolder {
    param([string]$Path)
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop }
            return $true
        }
        catch {
            Start-Sleep -Seconds 3
        }
    }
    return -not (Test-Path -LiteralPath $Path)
}

function Invoke-Update {
    $root = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $PSScriptRoot '..') '..'))
    $total = [System.Diagnostics.Stopwatch]::StartNew()
    $notes = New-Object 'System.Collections.Generic.List[string]'
    $exitCode = 0

    # --- Preflight ----------------------------------------------------------------------------------------------------
    Write-Section 'Preflight'
    if ($ReuseHome -and ($WorkDirectory -or $BaseHakDirectory)) { throw (New-Failure '-ReuseHome cannot be combined with -WorkDirectory or -BaseHakDirectory.' 4) }
    if ($ReuseHome -and $KeepHome) { throw (New-Failure '-ReuseHome keeps its folder anyway; drop -KeepHome.' 4) }
    if (-not (Test-Path -LiteralPath (Join-Path $root $SnapshotsRelative) -PathType Container)) { throw (New-Failure "$SnapshotsRelative does not exist in $root." 4) }

    $branch = (Invoke-Git -Repository $root -Arguments @('rev-parse', '--abbrev-ref', 'HEAD')).Lines[0]
    $head = (Invoke-Git -Repository $root -Arguments @('rev-parse', '--short=9', 'HEAD')).Lines[0]
    Write-Host "Worktree $root, branch $branch at $head."

    $dirty = @((Invoke-Git -Repository $root -Arguments @('status', '--porcelain', '--untracked-files=all', '--', $SnapshotsRelative)).Lines)
    if ($dirty.Count -gt 0) {
        throw (New-Failure ("$SnapshotsRelative has local changes, so the files this run changes could not be told apart:`n  " + ($dirty -join "`n  ") + "`nCommit or revert them first.") 4)
    }

    if (-not (Test-HaksCheckout $root)) { throw (New-Failure 'SWLOR_Haks is not checked out in this worktree. Set up the sparse checkout (Readmes/AgentEnvironment.md, "Worktrees and the haks submodule").' 4) }
    $haks = Join-Path $root 'SWLOR_Haks'
    $pinned = (Invoke-Git -Repository $root -Arguments @('rev-parse', 'HEAD:SWLOR_Haks')).Lines[0]
    $actual = (Invoke-Git -Repository $haks -Arguments @('rev-parse', 'HEAD')).Lines[0]
    if ($pinned -ne $actual) { throw (New-Failure "SWLOR_Haks is at $($actual.Substring(0, 10)) but the branch pins $($pinned.Substring(0, 10)). Move it: git -C SWLOR_Haks checkout --detach $pinned (fetch the commit first when it is missing)." 4) }
    $haksDirty = @((Invoke-Git -Repository $haks -Arguments @('status', '--porcelain', '--', 'sw_2da', 'sw_ability', 'sw_tlk')).Lines)
    if ($haksDirty.Count -gt 0) { throw (New-Failure "SWLOR_Haks has local changes in the hak sources; the snapshots must come from the pinned commit:`n  $($haksDirty -join "`n  ")" 4) }
    foreach ($required in @('sw_2da', 'sw_ability', "sw_tlk/$TlkName")) {
        if (-not (Test-Path -LiteralPath (Join-Path $haks $required))) { throw (New-Failure "SWLOR_Haks/$required is missing (sparse checkout without it?)." 4) }
    }
    Write-Host "SWLOR_Haks at the pinned commit $($pinned.Substring(0, 10))."

    $hakNames = @(Get-HakNamesFromBuildConfig $root)
    Wait-ComposeFree -Minutes $WaitMinutes
    Write-Host "Docker answers; the compose project '$ComposeProject' has no container."

    # --- The work folder ----------------------------------------------------------------------------------------------
    $createdWork = $false
    $keep = [bool]$KeepHome
    $baseHaks = $null
    if ($ReuseHome) {
        $work = [System.IO.Path]::GetFullPath($ReuseHome)
        $keep = $true
        foreach ($required in @("home/modules/$ModuleName.mod", "home/tlk/$TlkName", 'home/swlor.env', 'hak/sw_2da.hak', 'hak/sw_ability.hak')) {
            if (-not (Test-Path -LiteralPath (Join-Path $work $required) -PathType Leaf)) { throw (New-Failure "-ReuseHome $work is not a work folder this script made (missing $required)." 4) }
        }
        $age = (Get-Date) - (Get-Item -LiteralPath (Join-Path $work "home/modules/$ModuleName.mod")).LastWriteTime
        Write-Host ("Reusing {0}; its module was packed {1:N1} hours ago." -f $work, $age.TotalHours)
    }
    else {
        $work = if ($WorkDirectory) { [System.IO.Path]::GetFullPath($WorkDirectory) } else { Get-DefaultWorkDirectory }
        if (Test-Path -LiteralPath $work) { throw (New-Failure "$work already exists; give -WorkDirectory a new folder." 4) }
        if (Test-PathInside -Path $work -Folder $root) { throw (New-Failure "$work is inside the repository; the throwaway home must be outside it." 4) }
        $baseHaks = Resolve-BaseHakDirectory -Root $root -Requested $BaseHakDirectory -HakNames $hakNames
        Write-Host "Other haks come from $baseHaks."
    }
    $homePath = Join-Path $work 'home'
    $hakPath = Join-Path $work 'hak'

    $packNote = 'reused'
    $hakNote = 'reused'
    $updateRun = $null
    $confirmRun = $null
    $changes = @()
    try {
        if (-not $ReuseHome) {
            New-Item -ItemType Directory -Path $work | Out-Null
            $createdWork = $true
            New-Item -ItemType Directory -Force -Path $hakPath | Out-Null

            $cliDll = Build-Cli -Root $root
            Write-Home -Root $root -HomePath $homePath
            $pack = Pack-Module -Root $root -CliDll $cliDll -HomePath $homePath
            $packNote = "{0}.mod, {1}, {2} s" -f $ModuleName, (Format-Size $pack.Bytes), $pack.Seconds
            $hakSeconds = Build-Haks -Root $root -CliDll $cliDll -WorkPath $work -HomePath $homePath -HakPath $hakPath
            $links = Link-Haks -BaseHaks $baseHaks -HakPath $hakPath -HakNames $hakNames
            $hakNote = "{0} rebuilt from SWLOR_Haks {1} ({2} s), {3} linked, {4} copied from {5}" -f ($RebuiltHaks -join ' and '), $pinned.Substring(0, 10), $hakSeconds, $links.Linked, $links.Copied, $baseHaks
        }

        # --- Update pass ------------------------------------------------------------------------------------------------
        if (-not $CheckOnly) {
            $updateRun = Invoke-EngineRun -Root $root -HomePath $homePath -HakPath $hakPath -UpdateSnapshots $true -Title 'Update pass (SWLOR_UPDATE_SNAPSHOTS=1)'
            $changes = @(Get-SnapshotChanges $root)
            Write-Section 'Changed snapshot files'
            if ($changes.Count -eq 0) {
                Write-Host 'None: every snapshot already matches this branch.'
            }
            else {
                foreach ($change in $changes) { Write-Host ("{0,-2} {1}  {2}" -f $change.Status, $change.Path, $change.Rows) }
                Write-Host ''
                Write-Host "Explain every changed row by a master commit or file before committing: git diff -U0 -- $SnapshotsRelative"
            }
            if (-not $updateRun.Ok) {
                $notes.Add('The update pass failed, so the snapshot files written above may be partial: do not commit them.')
                $exitCode = 1
            }
        }

        # --- Confirm pass -----------------------------------------------------------------------------------------------
        if ($exitCode -eq 0) {
            if (-not $CheckOnly -and $changes.Count -eq 0) {
                Write-Host ''
                Write-Host 'Confirm pass skipped: the update pass changed nothing, so it already compared every snapshot with the build.'
            }
            else {
                $confirmRun = Invoke-EngineRun -Root $root -HomePath $homePath -HakPath $hakPath -UpdateSnapshots $false -Title 'Confirm pass (snapshots compared, no update)'
                if (-not $confirmRun.Ok) { $exitCode = 1 }
            }
        }
    }
    finally {
        if ($createdWork -and -not $keep) {
            Write-Section 'Cleanup'
            if (Remove-WorkFolder $work) { Write-Host "Removed $work." } else { Write-Warning "Could not remove $work; delete it yourself." }
        }
        elseif ($keep) {
            Write-Host ''
            Write-Host "Kept $work (remove it when done; repeat a run with -ReuseHome `"$work`")."
        }
    }

    # --- Summary ------------------------------------------------------------------------------------------------------
    Write-Section 'Summary'
    Write-Host ("module             : {0}" -f $packNote)
    Write-Host ("haks               : {0}" -f $hakNote)
    if ($updateRun) { Write-Host ("update pass        : {0} ({1} s)" -f $updateRun.Text, $updateRun.Seconds) }
    if (-not $CheckOnly) { Write-Host ("snapshots changed  : {0} file(s)" -f $changes.Count) }
    if ($confirmRun) { Write-Host ("confirm pass       : {0} ({1} s)" -f $confirmRun.Text, $confirmRun.Seconds) }
    elseif ($exitCode -eq 0) { Write-Host 'confirm pass       : not needed (nothing changed)' }
    Write-Host ("total              : {0:N1} minutes" -f $total.Elapsed.TotalMinutes)
    foreach ($note in $notes) { Write-Host $note }

    $other = @((Invoke-Git -Repository $root -Arguments @('status', '--porcelain', '--untracked-files=all')).Lines | Where-Object { $_ -notmatch [regex]::Escape($SnapshotsRelative) })
    if ($other.Count -gt 0) {
        Write-Host 'Other changes in this worktree (not from the snapshot refresh? check before committing):'
        foreach ($line in $other) { Write-Host "  $line" }
    }
    if ($exitCode -eq 0 -and $changes.Count -gt 0) {
        Write-Host ''
        Write-Host 'Next: for each changed row name the master commit or file that explains it (git log origin/master -- <file>); an unexplained change is a regression: stop and report it, do not commit.'
    }
    return $exitCode
}

$code = 0
try {
    $out = @(Invoke-Update)
    $code = [int]$out[-1]
}
catch {
    $code = 1
    if ($_.Exception.Data.Contains('ExitCode')) { $code = [int]$_.Exception.Data['ExitCode'] }
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}
exit $code
