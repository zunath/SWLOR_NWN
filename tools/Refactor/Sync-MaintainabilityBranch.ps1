<#
.SYNOPSIS
    Daily sync of the maintainability refactor branch with master (maintainability refactor, item P0-7).

.DESCRIPTION
    Run it from a clean worktree on feature/maintainability-refactor (or a branch cut from it). It never pushes and
    never rebases; the agent that runs it reviews the result and pushes.

      1. Fetches master (git -c http.sslBackend=schannel fetch) and merges it with a merge commit.
      2. On conflicts: a conflicted file that a mechanical script owns (tools/Refactor/MechanicalScripts.txt; today the
         clock seam files) takes master's version, and the script runs again below. Every other conflict is left
         for you: the script lists the files and the master commits that touched them (for the port log in
         MaintainabilityRefactorStatus.md), leaves the merge open and exits 2. Resolve, `git add`, run again with
         -Continue.
      3. Re-runs every mechanical script, stages what changed, commits the merge, then checks each script reports
         nothing left to change. Moves the sparse SWLOR_Haks checkout to the commit master pins.
      4. Handler order manifest (SWLOR.Game.Server/Core/ScriptHandlerOrder.txt): when master touched
         [NWNEventHandler] lines (or with -PlaceHandlers) the script builds a throwaway copy of master, reads master's
         own reflection order from it and inserts every handler the manifest lacks right after its nearest
         predecessor in master's order. A handler that looks renamed or moved (same method or type name as a manifest
         line master no longer has) and a manifest line master removed are NOT edited: they are listed for a manual
         edit (exit 3). The manifest edit is left uncommitted for you to review.
      5. Builds and runs the test projects in tools/Refactor/SyncTests.txt (the unit, characterization and architecture
         tests), then rewrites the informational SWLOR.Architecture.Tests/Baseline/EdgesIntoCoreAndPlatform.txt (only that
         one test, in update mode; a new guarded edge still fails the run). With -RegenerateSnapshots it also rewrites
         the characterization snapshots; review `git diff` before committing them, because an unexplained snapshot
         diff is drift.
      6. Prints the line for the status file's sync log. Run Test-MaintainabilityParity.ps1 next (or pass -RunParity).

    Exit code: 0 all good; 1 build or test failure; 2 conflicts left for you; 3 the handler manifest needs a manual
    edit; 4 the script could not do its job (not a refactor branch, dirty worktree, git failure, ...).

.PARAMETER MasterRef
    What to merge. Default origin/master, fetched first. Another ref (a local branch, for testing the script) is not
    fetched.

.PARAMETER SkipFetch
    Merge the ref as it is.

.PARAMETER Continue
    Resume after you resolved the conflicts the script left (everything `git add`ed, merge still open).

.PARAMETER SkipTests
    Merge and re-run the mechanical scripts, but do not build or test.

.PARAMETER RegenerateSnapshots
    After the tests, rewrite the characterization snapshots (SWLOR_UPDATE_SNAPSHOTS=1) and list the changed files.

.PARAMETER PlaceHandlers
    Read master's handler order and place new handlers even when master changed no [NWNEventHandler] line.

.PARAMETER RunParity
    Run Test-MaintainabilityParity.ps1 against the merged master commit at the end and use its result in the log line.

.PARAMETER CoAuthor
    Added to the merge commit message as `Co-Authored-By: <value>` (for example "Claude Sonnet 5.5 <noreply@anthropic.com>").

.PARAMETER Configuration
    Build configuration. Default Debug.

.PARAMETER WorkDirectory
    Folder for the throwaway copy of master (a new folder). Default: a folder under C:\Temp (or the system temp folder).

.PARAMETER By
    Who to name in the sync log line.

.EXAMPLE
    powershell -NoProfile -File tools/Refactor/Sync-MaintainabilityBranch.ps1 -CoAuthor "Claude Sonnet 5.5 <noreply@anthropic.com>"
    powershell -NoProfile -File tools/Refactor/Sync-MaintainabilityBranch.ps1 -Continue
#>
[CmdletBinding()]
param(
    [string]$MasterRef = 'origin/master',
    [switch]$SkipFetch,
    [switch]$Continue,
    [switch]$SkipTests,
    [switch]$RegenerateSnapshots,
    [switch]$PlaceHandlers,
    [switch]$RunParity,
    [string]$CoAuthor,
    [string]$Configuration = 'Debug',
    [string]$WorkDirectory,
    [string]$By = 'agent'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'RefactorCommon.ps1')
. (Join-Path $PSScriptRoot 'MasterCopy.ps1')

$ManifestRelative = 'SWLOR.Game.Server/Core/ScriptHandlerOrder.txt'
$EdgesRelative = 'SWLOR.Architecture.Tests/Baseline/EdgesIntoCoreAndPlatform.txt'
$SnapshotsRelative = 'SWLOR.Game.Server.CharacterizationTests/Snapshots'

function Get-UnmergedFiles {
    param([string]$Root)
    return @((Invoke-Git -Repository $Root -Arguments @('diff', '--name-only', '--diff-filter=U')).Lines)
}

function Get-MasterCommitsFor {
    param([string]$Root, [string]$Range, [string]$File)
    $log = Invoke-Git -Repository $Root -Arguments @('log', '--format=%h %ad %an: %s', '--date=short', $Range, '--', $File) -AllowFailure
    return @($log.Lines | Select-Object -First 8)
}

function Invoke-Sync {
    $root = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $PSScriptRoot '..') '..'))
    $summary = New-Object 'System.Collections.Generic.List[string]'
    $followUps = New-Object 'System.Collections.Generic.List[string]'
    $exitCode = 0
    $manualEdit = $false

    # --- Preflight ---------------------------------------------------------------------------------------------------
    Write-Section 'Preflight'
    $branch = (Invoke-Git -Repository $root -Arguments @('rev-parse', '--abbrev-ref', 'HEAD')).Lines[0]
    if ($branch -eq 'HEAD') { throw (New-Failure 'HEAD is detached; check out the refactor branch (or a branch cut from it).' 4) }
    if ($branch -eq 'master') { throw (New-Failure 'This is master. Run the sync from feature/maintainability-refactor or a branch cut from it.' 4) }
    $isRefactorBranch = Invoke-Git -Repository $root -Arguments @('cat-file', '-e', 'HEAD:SWLOR.Game.Server/Readmes/MaintainabilityRefactorPlan.md') -AllowFailure
    if ($isRefactorBranch.ExitCode -ne 0) { throw (New-Failure "Branch $branch does not contain the maintainability refactor plan; it is not the refactor branch or a branch cut from it." 4) }

    $gitDirectory = Get-GitDirectory $root
    $mergeHeadPath = Join-Path $gitDirectory 'MERGE_HEAD'
    $statePath = Join-Path $gitDirectory 'mr-sync-state.json'
    $merging = Test-Path -LiteralPath $mergeHeadPath

    if ($merging -and -not $Continue) {
        throw (New-Failure 'A merge is already in progress. Resolve it and `git add` the files, then run this script with -Continue (or `git merge --abort` to start over).' 4)
    }
    if ($Continue -and -not $merging) { throw (New-Failure '-Continue was given but no merge is in progress.' 4) }
    if (-not $merging) {
        $dirty = @((Invoke-Git -Repository $root -Arguments @('status', '--porcelain', '--ignore-submodules=all')).Lines)
        if ($dirty.Count -gt 0) {
            throw (New-Failure ("The worktree is not clean ($($dirty.Count) path(s)); commit or move them first:`n  " + (($dirty | Select-Object -First 10) -join "`n  ")) 4)
        }
    }
    Write-Host "Branch: $branch"

    # --- Fetch -------------------------------------------------------------------------------------------------------
    $masterSha = $null
    $mergeBase = $null
    $otherConflicts = @()
    $ownedTaken = @()
    $alreadyMerged = $false
    $headBefore = (Invoke-Git -Repository $root -Arguments @('rev-parse', 'HEAD')).Lines[0]

    if ($merging) {
        $masterSha = (Get-Content -LiteralPath $mergeHeadPath -TotalCount 1).Trim()
        if (Test-Path -LiteralPath $statePath) {
            $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
            $mergeBase = $state.MergeBase
            $ownedTaken = @($state.OwnedTaken)
            $otherConflicts = @($state.OtherConflicts)
        }
        else {
            $mergeBase = (Invoke-Git -Repository $root -Arguments @('merge-base', 'HEAD', $masterSha)).Lines[0]
        }
    }
    else {
        Write-Section 'Fetch'
        if (-not $SkipFetch -and $MasterRef -match '^origin/(.+)$') {
            $name = $Matches[1]
            Invoke-Git -Repository $root -Arguments @('fetch', 'origin', "+refs/heads/${name}:refs/remotes/origin/${name}") -Network | Out-Null
        }
        $masterSha = (Invoke-Git -Repository $root -Arguments @('rev-parse', '--verify', "$MasterRef^{commit}")).Lines[0]
        Write-Host "$MasterRef = $masterSha"
        $alreadyMerged = ((Invoke-Git -Repository $root -Arguments @('merge-base', '--is-ancestor', $masterSha, 'HEAD') -AllowFailure).ExitCode -eq 0)
        $mergeBase = (Invoke-Git -Repository $root -Arguments @('merge-base', 'HEAD', $masterSha)).Lines[0]
    }
    $masterShort = $masterSha.Substring(0, 9)
    $commitCount = 0
    if ($mergeBase -ne $masterSha) {
        $commitCount = @((Invoke-Git -Repository $root -Arguments @('rev-list', '--count', "$mergeBase..$masterSha")).Lines)[0]
    }

    # --- Merge -------------------------------------------------------------------------------------------------------
    $mechanical = @(Get-MechanicalScripts -Root $root)
    if (-not $merging) {
        Write-Section 'Merge'
        if ($alreadyMerged) {
            Write-Host "The branch already contains $masterShort; nothing to merge."
        }
        else {
            Write-Host "Merging $masterShort ($commitCount master commit(s) since $($mergeBase.Substring(0, 9))) ..."
            $merge = Invoke-Git -Repository $root -Arguments @('merge', '--no-ff', '--no-commit', $masterSha) -AllowFailure
            $unmerged = @(Get-UnmergedFiles $root)
            if ($merge.ExitCode -ne 0 -and $unmerged.Count -eq 0) {
                Invoke-Git -Repository $root -Arguments @('merge', '--abort') -AllowFailure | Out-Null
                throw (New-Failure "git merge failed without conflicts: $($merge.Error.Trim()) $($merge.Output.Trim())" 4)
            }
            $merging = $true

            if ($unmerged.Count -gt 0) {
                Write-Host "$($unmerged.Count) conflicted file(s)."
                $patterns = @()
                foreach ($script in $mechanical) { $patterns += @(Get-OwnedFilePatterns -Script $script -Root $root) }
                $remaining = New-Object 'System.Collections.Generic.List[string]'
                $taken = New-Object 'System.Collections.Generic.List[string]'
                foreach ($file in $unmerged) {
                    if (Test-OwnedPath -Path $file -Patterns $patterns) {
                        $theirs = Invoke-Git -Repository $root -Arguments @('checkout', '--theirs', '--', $file) -AllowFailure
                        if ($theirs.ExitCode -ne 0) {
                            # Master deleted the file: follow master.
                            Invoke-Git -Repository $root -Arguments @('rm', '-f', '--', $file) | Out-Null
                        }
                        else {
                            Invoke-Git -Repository $root -Arguments @('add', '--', $file) | Out-Null
                        }
                        $taken.Add($file)
                        Write-Host "  owned by a mechanical script, took master's version: $file"
                    }
                    else {
                        $remaining.Add($file)
                    }
                }
                $ownedTaken = $taken.ToArray()
                $otherConflicts = $remaining.ToArray()
                $state = [pscustomobject]@{ MergeBase = $mergeBase; MasterSha = $masterSha; OwnedTaken = $ownedTaken; OtherConflicts = $otherConflicts }
                [System.IO.File]::WriteAllText($statePath, ($state | ConvertTo-Json -Depth 4), $script:Utf8)

                if ($remaining.Count -gt 0) {
                    Write-Section 'Conflicts left for you'
                    foreach ($file in $remaining) {
                        Write-Host $file
                        foreach ($commit in (Get-MasterCommitsFor -Root $root -Range "$mergeBase..$masterSha" -File $file)) { Write-Host "    master: $commit" }
                    }
                    Write-Host ''
                    Write-Host 'Resolve them, `git add` each file, then run this script again with -Continue.'
                    Write-Host 'Record each file that the branch had already converted in the port log of MaintainabilityRefactorStatus.md.'
                    return 2
                }
            }
            else {
                [System.IO.File]::WriteAllText($statePath, ([pscustomobject]@{ MergeBase = $mergeBase; MasterSha = $masterSha; OwnedTaken = @(); OtherConflicts = @() } | ConvertTo-Json -Depth 4), $script:Utf8)
            }
        }
    }
    else {
        $left = @(Get-UnmergedFiles $root)
        if ($left.Count -gt 0) { throw (New-Failure ("Unresolved files remain; resolve and `git add` them first:`n  " + ($left -join "`n  ")) 4) }
        Write-Host "Resuming the merge of $masterShort."
    }

    # --- Mechanical scripts -----------------------------------------------------------------------------------------
    Write-Section 'Mechanical scripts'
    $patterns = @()
    $mechanicalNotes = New-Object 'System.Collections.Generic.List[string]'
    foreach ($script in $mechanical) {
        $apply = Invoke-MechanicalScript -Script $script -TargetRoot $root -Mode Apply
        if ($apply.ExitCode -ne 0) {
            throw (New-Failure "$($script.RelativePath) failed (exit $($apply.ExitCode)): $($apply.Error.Trim()) $($apply.Output.Trim())" 4)
        }
        $line = $apply.Lines | Select-Object -Last 1
        Write-Host "$($script.Name): $line"
        $mechanicalNotes.Add("$($script.Name): $line")
        $patterns += @(Get-OwnedFilePatterns -Script $script -Root $root)
    }
    $changed = @((Invoke-Git -Repository $root -Arguments @('diff', '--name-only')).Lines | Where-Object { Test-OwnedPath -Path $_ -Patterns $patterns })
    foreach ($file in $changed) { Invoke-Git -Repository $root -Arguments @('add', '--', $file) | Out-Null }
    foreach ($script in $mechanical) {
        $check = Invoke-MechanicalScript -Script $script -TargetRoot $root -Mode Check
        if ($check.ExitCode -ne 0) {
            throw (New-Failure "$($script.RelativePath) -Check still reports changes after it ran:`n$($check.Output.Trim())" 4)
        }
    }

    # --- Commit ------------------------------------------------------------------------------------------------------
    $mergeCommit = $null
    if ($merging) {
        $body = New-Object 'System.Collections.Generic.List[string]'
        $body.Add("Merge master ($masterShort) into $branch")
        $body.Add('')
        $body.Add("Sync of $commitCount master commit(s), $($mergeBase.Substring(0, 9))..$masterShort (tools/Refactor/Sync-MaintainabilityBranch.ps1).")
        if ($ownedTaken.Count -gt 0) {
            $body.Add("Conflicts in files a mechanical script owns, settled by taking master's version and re-running the script: $($ownedTaken -join ', ').")
        }
        if ($otherConflicts.Count -gt 0) { $body.Add("Conflicts resolved by hand: $($otherConflicts -join ', ').") }
        if ($ownedTaken.Count -eq 0 -and $otherConflicts.Count -eq 0) { $body.Add('No conflicts.') }
        foreach ($note in $mechanicalNotes) { $body.Add("Mechanical script $note") }
        if ($CoAuthor) { $body.Add(''); $body.Add("Co-Authored-By: $CoAuthor") }
        $messagePath = Join-Path $gitDirectory 'mr-sync-message.txt'
        [System.IO.File]::WriteAllText($messagePath, (($body -join "`n") + "`n"), $script:Utf8)
        Invoke-Git -Repository $root -Arguments @('commit', '-F', $messagePath) | Out-Null
        Remove-Item -LiteralPath $messagePath -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $statePath -Force -ErrorAction SilentlyContinue
        $mergeCommit = (Invoke-Git -Repository $root -Arguments @('rev-parse', 'HEAD')).Lines[0]
        Write-Host "Merge commit $($mergeCommit.Substring(0, 9))."
    }
    elseif ($changed.Count -gt 0) {
        $body = "Re-apply the mechanical scripts after the sync with master ($masterShort)`n`n" + (($mechanicalNotes | ForEach-Object { "Mechanical script $_" }) -join "`n") + "`n"
        if ($CoAuthor) { $body += "`nCo-Authored-By: $CoAuthor`n" }
        $messagePath = Join-Path $gitDirectory 'mr-sync-message.txt'
        [System.IO.File]::WriteAllText($messagePath, $body, $script:Utf8)
        Invoke-Git -Repository $root -Arguments @('commit', '-F', $messagePath) | Out-Null
        Remove-Item -LiteralPath $messagePath -Force -ErrorAction SilentlyContinue
        $mergeCommit = (Invoke-Git -Repository $root -Arguments @('rev-parse', 'HEAD')).Lines[0]
        Write-Host "Committed the re-applied mechanical changes as $($mergeCommit.Substring(0, 9))."
    }

    $haksNote = Update-HaksPointer -WorktreeRoot $root
    if ($haksNote) { Write-Host $haksNote }

    # --- Handler order manifest ------------------------------------------------------------------------------------
    Write-Section 'Handler order manifest'
    $handlerFiles = @()
    if ($mergeBase -ne $masterSha) {
        $handlerFiles = @((Invoke-Git -Repository $root -Arguments @('diff', '--name-only', '-G', 'NWNEventHandler', $mergeBase, $masterSha)).Lines)
    }
    $handlerNote = 'master changed no [NWNEventHandler] line; the manifest is untouched'
    if ($PlaceHandlers -or $handlerFiles.Count -gt 0) {
        if ($handlerFiles.Count -gt 0) { Write-Host "Master touched handler attributes in: $($handlerFiles -join ', ')" }
        $dir = $WorkDirectory
        if (-not $dir) { $dir = Get-DefaultWorkDirectory }
        $copy = New-MasterCopy -Root $root -Sha $masterSha -WorkDirectory $dir
        $texts = $null
        try {
            Add-MasterWorktree $copy
            Install-Seams -Copy $copy -Seams @(Read-Seams $root | Where-Object { $_.Kind -eq 'commit' })
            $texts = Invoke-HandlerReference -Copy $copy -Configuration $Configuration -ResultsDirectory (Join-Path $dir 'results-handlers')
        }
        catch {
            # The rest of the sync still runs; the manifest is then placed by hand after the cause is fixed.
            Write-Host "WARNING: master's handler order could not be read: $($_.Exception.Message)" -ForegroundColor Yellow
            $manualEdit = $true
            $handlerNote = 'NOT PLACED: master''s handler order could not be read (see the warning above); run again with -PlaceHandlers after fixing it'
        }
        finally {
            Remove-MasterCopy $copy
        }

        if ($texts) {
            $manifestPath = Join-Path $root $ManifestRelative
            $manifestText = [System.IO.File]::ReadAllText($manifestPath, $script:Utf8)
            $placed = Add-NewHandlersToManifest -ManifestText $manifestText -ReferenceText $texts['SWLOR.Game.Server.Tests/Core/Snapshots/ScriptHandlerOrder.txt']
            if ($placed.Inserted.Count -gt 0) {
                [System.IO.File]::WriteAllText($manifestPath, $placed.Text, $script:Utf8)
                Write-Host "Inserted $($placed.Inserted.Count) handler(s) into $ManifestRelative where master's order puts them:"
                foreach ($line in $placed.Inserted) { Write-Host "  + $line" }
                $followUps.Add("$ManifestRelative (review the inserted handlers)")
            }
            foreach ($line in $placed.RenameCandidates) { Write-Host "  RENAME OR MOVE (edit the manifest line by hand, keeping its position): $line" }
            foreach ($line in $placed.StaleLines) { Write-Host "  GONE FROM MASTER (remove the manifest line, or keep it if the branch renamed the handler): $line" }
            if ($placed.RenameCandidates.Count -gt 0 -or $placed.StaleLines.Count -gt 0) { $manualEdit = $true }
            $handlerNote = "$($placed.Inserted.Count) inserted, $($placed.RenameCandidates.Count) rename/move candidate(s), $($placed.StaleLines.Count) line(s) gone from master"
        }
    }
    else {
        Write-Host $handlerNote
    }

    # --- Build and test ---------------------------------------------------------------------------------------------
    $results = New-Object 'System.Collections.Generic.List[object]'
    $buildNote = 'skipped (-SkipTests)'
    $testsOk = $true
    if (-not $SkipTests) {
        Write-Section 'Build'
        $projects = @(Read-SyncTests -Root $root)
        $resultsDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("mr-sync-results-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
        try {
            foreach ($project in $projects) {
                Invoke-DotnetBuild -Project (Join-Path $root "$($project.Name)/$($project.Name).csproj") -Configuration $Configuration
            }
            $buildNote = 'ok'

            Write-Section 'Tests'
            foreach ($project in $projects) {
                $filter = ConvertTo-TestFilter $project.Excludes
                Write-Host "Testing $($project.Name) ($($project.Excludes.Count) excluded term(s)) ..."
                $run = Invoke-DotnetTest -Project (Join-Path $root "$($project.Name)/$($project.Name).csproj") -Configuration $Configuration `
                    -ResultsDirectory (Join-Path $resultsDirectory $project.Name) -Filter $filter
                $results.Add($run)
                if (-not $run.Ok) { $testsOk = $false }
            }

            # The informational record of edges into Core and Platform: rewrite it with only that one test, so the
            # guarded baseline (a new cross-domain edge must fail) is never touched.
            Write-Section 'Informational architecture record'
            $architecture = Join-Path $root 'SWLOR.Architecture.Tests/SWLOR.Architecture.Tests.csproj'
            if (Test-Path -LiteralPath $architecture) {
                $update = Invoke-DotnetTest -Project $architecture -Configuration $Configuration -ResultsDirectory (Join-Path $resultsDirectory 'edges') `
                    -Filter 'FullyQualifiedName~Edges_into_core_and_platform_are_recorded' -Environment @{ SWLOR_UPDATE_SNAPSHOTS = '1' }
                if (-not $update.Ok) { Write-Warning 'Regenerating EdgesIntoCoreAndPlatform.txt failed; see the output above.'; $testsOk = $false }
                $edgesChanged = @((Invoke-Git -Repository $root -Arguments @('status', '--porcelain', '--', $EdgesRelative)).Lines)
                if ($edgesChanged.Count -gt 0) {
                    Write-Host "$EdgesRelative was out of date and has been rewritten (informational only)."
                    $followUps.Add($EdgesRelative)
                }
                else {
                    Write-Host "$EdgesRelative is up to date."
                }
            }

            if ($RegenerateSnapshots) {
                Write-Section 'Regenerating characterization snapshots'
                $characterization = Join-Path $root 'SWLOR.Game.Server.CharacterizationTests/SWLOR.Game.Server.CharacterizationTests.csproj'
                $regen = Invoke-DotnetTest -Project $characterization -Configuration $Configuration -ResultsDirectory (Join-Path $resultsDirectory 'regenerate') `
                    -Environment @{ SWLOR_UPDATE_SNAPSHOTS = '1' }
                if (-not $regen.Ok) { Write-Warning 'The characterization run in update mode failed.'; $testsOk = $false }
                $snapshotChanges = @((Invoke-Git -Repository $root -Arguments @('status', '--porcelain', '--', $SnapshotsRelative)).Lines)
                Write-Host "$($snapshotChanges.Count) snapshot file(s) changed. Review them with: git diff --stat -- $SnapshotsRelative"
                if ($snapshotChanges.Count -gt 0) { $followUps.Add("$SnapshotsRelative ($($snapshotChanges.Count) file(s))") }
            }
        }
        finally {
            if (Test-Path -LiteralPath $resultsDirectory) { Remove-Item -LiteralPath $resultsDirectory -Recurse -Force -ErrorAction SilentlyContinue }
        }
    }

    # --- Parity ------------------------------------------------------------------------------------------------------
    $parityNote = 'not run (Test-MaintainabilityParity.ps1)'
    if ($RunParity -and -not $SkipTests) {
        Write-Section 'Parity'
        $summaryFile = Join-Path ([System.IO.Path]::GetTempPath()) ("mr-parity-summary-" + [guid]::NewGuid().ToString('N').Substring(0, 8) + '.txt')
        $arguments = @('-NoProfile', '-NonInteractive', '-File', (Join-Path $PSScriptRoot 'Test-MaintainabilityParity.ps1'),
            '-MasterRef', $masterSha, '-SkipFetch', '-Configuration', $Configuration, '-SummaryFile', $summaryFile)
        if ($WorkDirectory) { $arguments += @('-WorkDirectory', $WorkDirectory) }
        $parity = Invoke-Process -FilePath (Get-HostExecutable) -Arguments $arguments -Stream
        if (Test-Path -LiteralPath $summaryFile) {
            $parityNote = [System.IO.File]::ReadAllText($summaryFile, $script:Utf8).Trim()
            Remove-Item -LiteralPath $summaryFile -Force
        }
        else {
            $parityNote = "failed to run (exit $($parity.ExitCode))"
        }
        if ($parity.ExitCode -ne 0) { $testsOk = $false }
    }

    # --- Summary -----------------------------------------------------------------------------------------------------
    Write-Section 'Sync summary'
    $mergedNote = if ($alreadyMerged -and -not $mergeCommit) { "$masterShort (already merged)" } else { "$masterShort ($commitCount commit(s) since $($mergeBase.Substring(0, 9)))" }
    Write-Host ("master merged      : {0}" -f $mergedNote)
    Write-Host ("merge commit       : {0}" -f $(if ($mergeCommit) { $mergeCommit.Substring(0, 9) } else { 'none' }))
    $conflictNote = 'none'
    if ($ownedTaken.Count -gt 0 -or $otherConflicts.Count -gt 0) {
        $conflictNote = "$($ownedTaken.Count) owned file(s) took master + script, $($otherConflicts.Count) resolved by hand"
    }
    Write-Host ("conflicts          : {0}" -f $conflictNote)
    foreach ($note in $mechanicalNotes) { Write-Host ("mechanical script  : {0}" -f $note) }
    Write-Host ("handler manifest   : {0}" -f $handlerNote)
    Write-Host ("haks               : {0}" -f $(if ($haksNote) { $haksNote } else { 'unchanged' }))
    Write-Host ("build              : {0}" -f $buildNote)
    foreach ($run in $results) {
        $state = if ($run.Ok) { 'passed' } else { 'FAILED' }
        Write-Host ("tests              : {0}: {1} - {2} passed, {3} failed, {4} skipped of {5} ({6}s)" -f $run.Project, $state, $run.Passed, $run.Failed, $run.Skipped, $run.Total, $run.Seconds)
        if (-not $run.HasResults) { Write-Host '                     no result file was produced (the test host crashed or the filter matched nothing)' }
        foreach ($test in ($run.FailedTests | Select-Object -First 15)) { Write-Host ("                     FAILED {0}: {1}" -f $test.Name, $test.Message) }
        if ($run.FailedTests.Count -gt 15) { Write-Host ("                     ... {0} more failed test(s)" -f ($run.FailedTests.Count - 15)) }
    }
    Write-Host ("parity             : {0}" -f $parityNote)
    if ($followUps.Count -gt 0) {
        Write-Host 'left uncommitted, review and commit:'
        foreach ($item in $followUps) { Write-Host "  $item" }
    }

    $testNote = 'not run'
    if ($results.Count -gt 0) {
        $passed = ($results | Measure-Object -Property Passed -Sum).Sum
        $failed = ($results | Measure-Object -Property Failed -Sum).Sum
        $testNote = "$passed passed, $failed failed"
        if (-not $testsOk -and $failed -eq 0) { $testNote += ' (a run failed without a failed test)' }
    }
    $conflictLog = if ($ownedTaken.Count -gt 0 -or $otherConflicts.Count -gt 0) { "$($ownedTaken.Count + $otherConflicts.Count) file(s)" } else { 'none' }
    Write-Host ''
    Write-Host 'Sync log line for MaintainabilityRefactorStatus.md:'
    Write-Host ('| {0} | `{1}` | {2} | {3}; tests: {4} | {5} |' -f (Get-Date -Format 'yyyy-MM-dd'), $masterShort, $conflictLog, $parityNote, $testNote, $By)

    if (-not $testsOk) { return 1 }
    if ($manualEdit) { return 3 }
    return $exitCode
}

$code = 0
try {
    $out = @(Invoke-Sync)
    $code = [int]$out[-1]
}
catch {
    $code = 1
    if ($_.Exception.Data.Contains('ExitCode')) { $code = [int]$_.Exception.Data['ExitCode'] }
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}
exit $code
