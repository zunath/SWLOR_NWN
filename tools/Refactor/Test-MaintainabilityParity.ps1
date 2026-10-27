<#
.SYNOPSIS
    Parity check of the maintainability refactor branch against master (maintainability refactor, item P0-7).

.DESCRIPTION
    Proves the branch still behaves like master: it runs the branch's locking tests against master's own code and
    compares what master produces with the branch's checked-in snapshots, file by file.

      1. Creates a throwaway git worktree of master (detached) in a folder outside the repository, with a sparse
         SWLOR_Haks checkout (sw_2da, sw_tlk, sw_ability) at the commit master pins. It is never committed from and is
         always removed at the end, also when something fails.
      2. Applies the branch's behaviour-neutral seams to it: the seams in tools/Refactor/ParitySeams.txt (the P0-5
         commit that adds ScriptRegistry.LoadHandlersFromAssemblies and the boot-order tests, and Clock.cs), then every
         mechanical script in tools/Refactor/MechanicalScripts.txt (the clock seam rewrite and the NWScript engine seam generator). If a seam no longer
         applies to master the run stops (exit 3) and names the file.
      3. Copies SWLOR.Game.Server.CharacterizationTests (without its snapshots) into the copy and runs it with
         SWLOR_UPDATE_SNAPSHOTS=1 and SWLOR_SNAPSHOT_DIRECTORY pointing at a temporary folder, so the folder receives
         master's output. It also runs the P0-5 boot-order tests (ScriptHandlerOrderTests) in update mode on the copy,
         which writes master's handler order in reflection order.
      4. Compares: every characterization snapshot file with the branch's, and master's handler order with the
         branch's handler manifest (SWLOR.Game.Server/Core/ScriptHandlerOrder.txt) and the engine-test handler
         snapshot. Line endings are ignored; comment lines of the handler files are ignored.
      5. Exits 0 only when everything is identical; otherwise prints each differing snapshot with its differing
         rows (or each differing script and handler) and exits 1.

    Typical differences and what they mean: a snapshot that differs after a sync that master's content changes explain
    (new recipes, loot) means the branch's snapshots were not regenerated, or the branch changed the behaviour; a
    handler on master that the manifest lacks means the sync did not place it; a changed row in a stat or combat
    table means a refactor changed behaviour.

    It never pushes and never touches the branch. It needs a few GB of disk, a full build of master (several minutes)
    and the one-time SWLOR_Haks worktree setup from Readmes/AgentEnvironment.md.

.PARAMETER MasterRef
    Default origin/master (fetched first). Any commit-ish; a ref other than origin/<branch> is not fetched.

.PARAMETER SkipFetch
    Use the ref as it is.

.PARAMETER WorkDirectory
    A new folder for the copy and the results. Default: a folder under C:\Temp (or the system temp folder).

.PARAMETER KeepWorkDirectory
    For debugging a failed run only: leave the copy in place. The default always removes it.

.PARAMETER Configuration
    Build configuration. Default Debug.

.PARAMETER SummaryFile
    Write the one-line result here (the sync uses it).

.EXAMPLE
    powershell -NoProfile -File tools/Refactor/Test-MaintainabilityParity.ps1
#>
[CmdletBinding()]
param(
    [string]$MasterRef = 'origin/master',
    [switch]$SkipFetch,
    [string]$WorkDirectory,
    [switch]$KeepWorkDirectory,
    [string]$Configuration = 'Debug',
    [string]$SummaryFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'RefactorCommon.ps1')
. (Join-Path $PSScriptRoot 'MasterCopy.ps1')

$ManifestRelative = 'SWLOR.Game.Server/Core/ScriptHandlerOrder.txt'
$EngineSnapshotRelative = 'SWLOR.Game.Server.Tests/Core/Snapshots/ScriptHandlerOrder.EngineTests.txt'
$ProductionReferenceRelative = 'SWLOR.Game.Server.Tests/Core/Snapshots/ScriptHandlerOrder.txt'
$SnapshotsRelative = 'SWLOR.Game.Server.CharacterizationTests/Snapshots'

function Invoke-Parity {
    $root = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $PSScriptRoot '..') '..'))

    Write-Section 'Preflight'
    $branch = (Invoke-Git -Repository $root -Arguments @('rev-parse', '--abbrev-ref', 'HEAD')).Lines[0]
    $head = (Invoke-Git -Repository $root -Arguments @('rev-parse', '--short=9', 'HEAD')).Lines[0]
    $isRefactorBranch = Invoke-Git -Repository $root -Arguments @('cat-file', '-e', 'HEAD:SWLOR.Game.Server/Readmes/MaintainabilityRefactorPlan.md') -AllowFailure
    if ($isRefactorBranch.ExitCode -ne 0) { throw (New-Failure 'This is not the refactor branch or a branch cut from it; there is nothing to compare with master.' 4) }
    Write-Host "Branch $branch at $head"

    $compared = @((Invoke-Git -Repository $root -Arguments @('status', '--porcelain', '--', $SnapshotsRelative, $ManifestRelative, $EngineSnapshotRelative)).Lines)
    if ($compared.Count -gt 0) {
        Write-Host "Note: $($compared.Count) of the compared file(s) have uncommitted changes; the comparison uses the working tree."
    }

    if (-not $SkipFetch -and $MasterRef -match '^origin/(.+)$') {
        $name = $Matches[1]
        Invoke-Git -Repository $root -Arguments @('fetch', 'origin', "+refs/heads/${name}:refs/remotes/origin/${name}") -Network | Out-Null
    }
    $masterSha = (Invoke-Git -Repository $root -Arguments @('rev-parse', '--verify', "$MasterRef^{commit}")).Lines[0]
    Write-Host "$MasterRef = $masterSha"

    $dir = $WorkDirectory
    if (-not $dir) { $dir = Get-DefaultWorkDirectory }
    $referenceDirectory = Join-Path $dir 'reference-snapshots'
    $resultsDirectory = Join-Path $dir 'results'

    $copy = New-MasterCopy -Root $root -Sha $masterSha -WorkDirectory $dir
    $handlerTexts = $null
    $characterization = $null
    $failures = New-Object 'System.Collections.Generic.List[string]'
    $report = New-Object 'System.Collections.Generic.List[string]'
    $snapshotCompare = $null
    $handlerCount = 0
    $engineCount = 0

    try {
        Write-Section 'Throwaway copy of master'
        Add-MasterWorktree $copy

        Write-Section 'Seams and mechanical scripts'
        Install-Seams -Copy $copy -Seams @(Read-Seams $root)
        Invoke-MechanicalScriptsOnCopy -Copy $copy

        Write-Section 'Boot-order reference (master''s own handler order)'
        $handlerTexts = Invoke-HandlerReference -Copy $copy -Configuration $Configuration -ResultsDirectory (Join-Path $resultsDirectory 'handlers')

        Write-Section 'Characterization reference'
        $characterization = Invoke-CharacterizationReference -Copy $copy -Configuration $Configuration `
            -ResultsDirectory (Join-Path $resultsDirectory 'characterization') -ReferenceDirectory $referenceDirectory
        if (-not $characterization.Ok) {
            $names = ($characterization.FailedTests | Select-Object -First 10 | ForEach-Object { $_.Name }) -join ', '
            $failures.Add("the characterization tests failed on master ($($characterization.Failed) failed, results file: $($characterization.HasResults)): $names")
        }

        Write-Section 'Comparison'
        $snapshotCompare = Compare-SnapshotFolders -Branch (Join-Path $root $SnapshotsRelative) -Reference $referenceDirectory
        foreach ($difference in $snapshotCompare.Differences) {
            $failures.Add("snapshot $($difference.Path): $($difference.Problem)")
            $report.Add("snapshot $($difference.Path): $($difference.Problem)")
            foreach ($detail in $difference.Details) { $report.Add("    $detail") }
        }

        $branchManifest = ConvertFrom-HandlerOrderText ([System.IO.File]::ReadAllText((Join-Path $root $ManifestRelative), $script:Utf8))
        $masterOrder = ConvertFrom-HandlerOrderText $handlerTexts[$ProductionReferenceRelative]
        $handlerCount = Get-HandlerCount $masterOrder
        $handlerDifferences = @(Compare-HandlerOrders -Branch $branchManifest -Reference $masterOrder)
        foreach ($line in $handlerDifferences) { $report.Add("handler order: $line") }
        if ($handlerDifferences.Count -gt 0) { $failures.Add("handler order differs from master's reflection order in $($handlerDifferences.Count) place(s)") }

        $branchEngine = ConvertFrom-HandlerOrderText ([System.IO.File]::ReadAllText((Join-Path $root $EngineSnapshotRelative), $script:Utf8))
        $masterEngine = ConvertFrom-HandlerOrderText $handlerTexts[$EngineSnapshotRelative]
        $engineCount = Get-HandlerCount $masterEngine
        $engineDifferences = @(Compare-HandlerOrders -Branch $branchEngine -Reference $masterEngine)
        foreach ($line in $engineDifferences) { $report.Add("engine-test handler order: $line") }
        if ($engineDifferences.Count -gt 0) { $failures.Add("engine-test handler order differs in $($engineDifferences.Count) place(s)") }
    }
    finally {
        if ($KeepWorkDirectory) {
            Write-Warning "Leaving the throwaway copy at $($copy.Path) (-KeepWorkDirectory). Remove it with: git worktree remove --force; and the haks worktree under it first."
        }
        else {
            Write-Section 'Cleanup'
            Remove-MasterCopy $copy
        }
    }

    Write-Section 'Parity result'
    if ($report.Count -gt 0) {
        foreach ($line in $report) { Write-Host $line }
        Write-Host ''
    }
    $snapshotFiles = 0
    if ($snapshotCompare) { $snapshotFiles = $snapshotCompare.Compared }
    if ($failures.Count -eq 0) {
        $result = "parity identical to master $($masterSha.Substring(0, 9)) ($snapshotFiles characterization snapshot files, $handlerCount handlers, $engineCount engine-test handlers)"
        Write-Host "PARITY: $result"
        if ($SummaryFile) { [System.IO.File]::WriteAllText($SummaryFile, $result, $script:Utf8) }
        return 0
    }

    $result = "parity DIFFERS from master $($masterSha.Substring(0, 9)): $($failures.Count) problem(s)"
    Write-Host "PARITY FAILED: $result"
    foreach ($failure in $failures) { Write-Host "  - $failure" }
    if ($SummaryFile) { [System.IO.File]::WriteAllText($SummaryFile, $result, $script:Utf8) }
    return 1
}

$code = 0
try {
    $out = @(Invoke-Parity)
    $code = [int]$out[-1]
}
catch {
    $code = 1
    if ($_.Exception.Data.Contains('ExitCode')) { $code = [int]$_.Exception.Data['ExitCode'] }
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}
exit $code
