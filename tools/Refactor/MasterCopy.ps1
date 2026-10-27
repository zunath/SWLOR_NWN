<#
.SYNOPSIS
    The throwaway copy of master that the parity script tests and the sync reads master's handler order from, plus
    the comparisons and the handler-manifest editing built on it. Not run on its own.

.DESCRIPTION
    Dot-source RefactorCommon.ps1 first, then this file. See Test-MaintainabilityParity.ps1 for how the pieces fit.

    The copy is a detached git worktree of master in a folder outside the repository. It is never committed from and is
    always removed by Remove-MasterCopy (callers use try/finally). What is applied to it:
      * the seams listed in tools/Refactor/ParitySeams.txt (cherry-picks and new files),
      * every mechanical script in tools/Refactor/MechanicalScripts.txt.
    The SWLOR_Haks submodule is a sparse worktree (sw_2da, sw_tlk, sw_ability) at the commit master pins.
#>

Set-StrictMode -Version Latest

# ---------------------------------------------------------------------------------------------------------------------
# The copy
# ---------------------------------------------------------------------------------------------------------------------

function Get-DefaultWorkDirectory {
    $base = [System.IO.Path]::GetTempPath()
    if ($script:OnWindows -and (Test-Path -LiteralPath 'C:\Temp' -PathType Container)) { $base = 'C:\Temp' }
    return (Join-Path $base ("swlor-mr-parity-" + [guid]::NewGuid().ToString('N').Substring(0, 8)))
}

# The repository that holds the SWLOR_Haks git data: this worktree's own sparse checkout when it has one, else the main
# checkout's.
function Get-HaksAnchor {
    param([string]$Root)
    if (Test-HaksCheckout $Root) { return (Join-Path $Root 'SWLOR_Haks') }
    $common = (Invoke-Git -Repository $Root -Arguments @('rev-parse', '--path-format=absolute', '--git-common-dir')).Lines[0]
    $main = Split-Path -Parent $common
    if (Test-Path -LiteralPath (Join-Path (Join-Path $main 'SWLOR_Haks') '.git')) { return (Join-Path $main 'SWLOR_Haks') }
    throw (New-Failure 'No SWLOR_Haks checkout was found in this worktree or in the main checkout; set it up as Readmes/AgentEnvironment.md describes.' 4)
}

# The state of a copy; nothing exists on disk yet. Pass it to Remove-MasterCopy in a finally block.
function New-MasterCopy {
    param([string]$Root, [string]$Sha, [string]$WorkDirectory)
    return [pscustomobject]@{
        Root          = $Root
        Sha           = $Sha
        WorkDirectory = $WorkDirectory
        Path          = (Join-Path $WorkDirectory 'master')
        HaksAnchor    = $null
        Registered    = $false
        HaksInstalled = $false
        CreatedWorkDirectory = $false
    }
}

function Add-MasterWorktree {
    param($Copy)
    if (-not (Test-Path -LiteralPath $Copy.WorkDirectory)) {
        New-Item -ItemType Directory -Force -Path $Copy.WorkDirectory | Out-Null
        $Copy.CreatedWorkDirectory = $true
    }
    if (Test-Path -LiteralPath $Copy.Path) { throw (New-Failure "$($Copy.Path) already exists; give -WorkDirectory a new folder." 4) }
    Write-Host "Creating the throwaway copy of master ($($Copy.Sha.Substring(0, 9))) at $($Copy.Path) ..."
    $Copy.Registered = $true
    Invoke-Git -Repository $Copy.Root -Arguments @('worktree', 'add', '--detach', $Copy.Path, $Copy.Sha) | Out-Null

    $Copy.HaksAnchor = Get-HaksAnchor $Copy.Root
    Write-Host 'Setting up the sparse SWLOR_Haks checkout ...'
    $Copy.HaksInstalled = $true
    Install-SparseHaks -WorktreeRoot $Copy.Path -AnchorHaksRepository $Copy.HaksAnchor
}

function Remove-MasterCopy {
    param($Copy)
    $problems = New-Object 'System.Collections.Generic.List[string]'
    try {
        if ($Copy.HaksInstalled -and $Copy.HaksAnchor) {
            $r = Invoke-Git -Repository $Copy.HaksAnchor -Arguments @('worktree', 'remove', '--force', (Join-Path $Copy.Path 'SWLOR_Haks')) -AllowFailure
            if ($r.ExitCode -ne 0) { $problems.Add("haks worktree: $($r.Error.Trim())") }
        }
        if ($Copy.Registered) {
            $r = Invoke-Git -Repository $Copy.Root -Arguments @('worktree', 'remove', '--force', $Copy.Path) -AllowFailure
            if ($r.ExitCode -ne 0) { $problems.Add("worktree: $($r.Error.Trim())") }
        }
        if (Test-Path -LiteralPath $Copy.Path) {
            if ($script:OnWindows) {
                Invoke-Process -FilePath (Join-Path $env:SystemRoot 'System32\cmd.exe') -Arguments @('/c', 'rmdir', '/s', '/q', $Copy.Path) | Out-Null
            }
            else {
                Remove-Item -LiteralPath $Copy.Path -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
        if (Test-Path -LiteralPath $Copy.Path) { $problems.Add("$($Copy.Path) could not be deleted") }
        if ($Copy.CreatedWorkDirectory -and (Test-Path -LiteralPath $Copy.WorkDirectory)) {
            Remove-Item -LiteralPath $Copy.WorkDirectory -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        $problems.Add($_.Exception.Message)
    }
    if ($problems.Count -gt 0) {
        Write-Warning ("The throwaway copy was not removed completely; delete {0} by hand and run 'git worktree list' to check for a stale entry. {1}" -f $Copy.Path, ($problems -join '; '))
    }
    else {
        Write-Host 'Removed the throwaway copy.'
    }
}

# Reads tools/Refactor/ParitySeams.txt: `commit <sha>` and `file <path>` lines.
function Read-Seams {
    param([string]$Root)
    $seams = New-Object 'System.Collections.Generic.List[object]'
    foreach ($line in (Read-ManifestLines (Join-Path $Root 'tools/Refactor/ParitySeams.txt'))) {
        $parts = $line -split '\s+', 2
        if ($parts.Count -ne 2 -or ($parts[0] -ne 'commit' -and $parts[0] -ne 'file')) {
            throw (New-Failure "tools/Refactor/ParitySeams.txt: '$line' is not 'commit <sha>' or 'file <path>'." 4)
        }
        $seams.Add([pscustomobject]@{ Kind = $parts[0]; Value = $parts[1].Trim() })
    }
    return $seams.ToArray()
}

# $Seams: all of them, or only the ones whose Kind/Value the caller kept (the handler reference needs only the commits).
function Install-Seams {
    param($Copy, $Seams)
    $identity = @{
        GIT_AUTHOR_NAME     = 'maintainability parity'
        GIT_AUTHOR_EMAIL    = 'parity@localhost'
        GIT_COMMITTER_NAME  = 'maintainability parity'
        GIT_COMMITTER_EMAIL = 'parity@localhost'
    }
    foreach ($seam in $Seams) {
        if ($seam.Kind -eq 'commit') {
            $r = Invoke-Git -Repository $Copy.Path -Arguments @('cherry-pick', $seam.Value) -AllowFailure -Environment $identity
            if ($r.ExitCode -ne 0) {
                $conflicts = (Invoke-Git -Repository $Copy.Path -Arguments @('diff', '--name-only', '--diff-filter=U') -AllowFailure).Lines
                Invoke-Git -Repository $Copy.Path -Arguments @('cherry-pick', '--abort') -AllowFailure | Out-Null
                throw (New-Failure ("The seam commit $($seam.Value) no longer applies to master. Conflicts: $($conflicts -join ', '). " +
                        "Update tools/Refactor/ParitySeams.txt so the seam applies to master as it is now. $($r.Error.Trim())") 3)
            }
            Write-Host "Seam: cherry-picked $($seam.Value.Substring(0, 9))"
        }
        else {
            $source = Join-Path $Copy.Root $seam.Value
            $target = Join-Path $Copy.Path $seam.Value
            if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw (New-Failure "Seam file $($seam.Value) does not exist on the branch." 4) }
            if (Test-Path -LiteralPath $target) {
                $same = [System.Linq.Enumerable]::SequenceEqual([System.IO.File]::ReadAllBytes($source), [System.IO.File]::ReadAllBytes($target))
                if (-not $same) { throw (New-Failure "Seam file $($seam.Value) exists on master and differs from the branch's; merge it by hand and update tools/Refactor/ParitySeams.txt." 3) }
                Write-Host "Seam: $($seam.Value) already on master"
                continue
            }
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
            [System.IO.File]::WriteAllBytes($target, [System.IO.File]::ReadAllBytes($source))
            Write-Host "Seam: copied $($seam.Value)"
        }
    }
}

function Invoke-MechanicalScriptsOnCopy {
    param($Copy)
    foreach ($script in (Get-MechanicalScripts -Root $Copy.Root)) {
        $apply = Invoke-MechanicalScript -Script $script -TargetRoot $Copy.Path -Mode Apply
        if ($apply.ExitCode -ne 0) {
            throw (New-Failure "$($script.RelativePath) failed on master (exit $($apply.ExitCode)): $($apply.Error.Trim()) $($apply.Output.Trim())" 3)
        }
        $summary = ($apply.Lines | Select-Object -Last 1)
        Write-Host "Mechanical script $($script.Name) on master: $summary"
        $check = Invoke-MechanicalScript -Script $script -TargetRoot $Copy.Path -Mode Check
        if ($check.ExitCode -ne 0) {
            throw (New-Failure "$($script.RelativePath) is not idempotent on master: a second run would still change files." 3)
        }
    }
}

# Copies a folder without build output and without the named top-level folders.
function Copy-ProjectTree {
    param([string]$Source, [string]$Destination, [string[]]$SkipFolders = @())
    $skip = @('bin', 'obj') + $SkipFolders
    $sourceRoot = (Resolve-Path -LiteralPath $Source).Path.TrimEnd('\', '/')
    foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Force) {
        $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart('\', '/')
        $first = ($relative -split '[\\/]')[0]
        if ($skip -contains $first) { continue }
        $target = Join-Path $Destination $relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }
}

# ---------------------------------------------------------------------------------------------------------------------
# Reference runs on the copy
# ---------------------------------------------------------------------------------------------------------------------

$script:HandlerSnapshots = @(
    'SWLOR.Game.Server.Tests/Core/Snapshots/ScriptHandlerOrder.txt',
    'SWLOR.Game.Server.Tests/Core/Snapshots/ScriptHandlerOrder.EngineTests.txt'
)

# Builds the copy's test project and runs the P0-5 boot-order tests (the seam commit adds them) in update mode, so the
# snapshots they write are master's own handler order in reflection order. Returns the two texts.
function Invoke-HandlerReference {
    param($Copy, [string]$Configuration, [string]$ResultsDirectory)
    foreach ($relative in $script:HandlerSnapshots) {
        $path = Join-Path $Copy.Path $relative
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
    }
    $project = Join-Path $Copy.Path 'SWLOR.Game.Server.Tests/SWLOR.Game.Server.Tests.csproj'
    Invoke-DotnetBuild -Project $project -Configuration $Configuration
    $run = Invoke-DotnetTest -Project $project -Configuration $Configuration -ResultsDirectory $ResultsDirectory `
        -Filter 'FullyQualifiedName~ScriptHandlerOrderTests' -Environment @{ SWLOR_UPDATE_SNAPSHOTS = '1' }
    if (-not $run.Ok) {
        $names = ($run.FailedTests | ForEach-Object { $_.Name }) -join ', '
        throw (New-Failure "The boot-order tests do not pass on master with the P0-5 seam ($names). Master's boot wiring changed; read the failures above." 3)
    }
    $texts = @{}
    foreach ($relative in $script:HandlerSnapshots) {
        $path = Join-Path $Copy.Path $relative
        if (-not (Test-Path -LiteralPath $path)) { throw (New-Failure "The boot-order tests did not write $relative on master." 3) }
        $texts[$relative] = [System.IO.File]::ReadAllText($path, $script:Utf8)
    }
    return $texts
}

# Copies the characterization project (without its snapshots) into the copy, builds it and runs it in update mode with
# the snapshot folder redirected, so $ReferenceDirectory receives master's own output.
function Invoke-CharacterizationReference {
    param($Copy, [string]$Configuration, [string]$ResultsDirectory, [string]$ReferenceDirectory)
    $name = 'SWLOR.Game.Server.CharacterizationTests'
    Copy-ProjectTree -Source (Join-Path $Copy.Root $name) -Destination (Join-Path $Copy.Path $name) -SkipFolders @('Snapshots')
    $project = Join-Path (Join-Path $Copy.Path $name) "$name.csproj"
    Invoke-DotnetBuild -Project $project -Configuration $Configuration
    New-Item -ItemType Directory -Force -Path $ReferenceDirectory | Out-Null
    return Invoke-DotnetTest -Project $project -Configuration $Configuration -ResultsDirectory $ResultsDirectory `
        -Environment @{ SWLOR_UPDATE_SNAPSHOTS = '1'; SWLOR_SNAPSHOT_DIRECTORY = $ReferenceDirectory }
}

# ---------------------------------------------------------------------------------------------------------------------
# Comparisons
# ---------------------------------------------------------------------------------------------------------------------

function Read-NormalizedText {
    param([string]$Path)
    return ([System.IO.File]::ReadAllText($Path, $script:Utf8)).Replace("`r`n", "`n")
}

# Lines that are in $Source more often than in $Other, with their 1-based line numbers (multiset difference, which is
# how the snapshot helper reports a table: one row per line).
function Get-MissingLines {
    param([string[]]$Source, [string[]]$Other)
    $available = New-Object 'System.Collections.Generic.Dictionary[string,int]'
    foreach ($line in $Other) {
        $count = 0
        [void]$available.TryGetValue($line, [ref]$count)
        $available[$line] = $count + 1
    }
    $missing = New-Object 'System.Collections.Generic.List[object]'
    for ($i = 0; $i -lt $Source.Length; $i++) {
        $count = 0
        if ($available.TryGetValue($Source[$i], [ref]$count) -and $count -gt 0) {
            $available[$Source[$i]] = $count - 1
        }
        else {
            $missing.Add([pscustomobject]@{ Line = $i + 1; Text = $Source[$i] })
        }
    }
    return $missing.ToArray()
}

# Compares two folders of snapshot files. $Branch is the branch's, $Reference is master's. Returns one entry per
# difference: Path, Problem and Details (text lines for the report).
function Compare-SnapshotFolders {
    param([string]$Branch, [string]$Reference, [int]$MaximumRows = 6)
    $differences = New-Object 'System.Collections.Generic.List[object]'
    $files = @{}
    $count = 0
    foreach ($side in @(@{ Root = $Branch; Key = 'Branch' }, @{ Root = $Reference; Key = 'Reference' })) {
        if (-not (Test-Path -LiteralPath $side.Root -PathType Container)) { continue }
        $root = (Resolve-Path -LiteralPath $side.Root).Path.TrimEnd('\', '/')
        foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File) {
            $relative = $file.FullName.Substring($root.Length).TrimStart('\', '/').Replace('\', '/')
            if (-not $files.ContainsKey($relative)) { $files[$relative] = @{} }
            $files[$relative][$side.Key] = $file.FullName
        }
    }
    foreach ($relative in ($files.Keys | Sort-Object { $_ } -CaseSensitive)) {
        $entry = $files[$relative]
        if (-not $entry.ContainsKey('Reference')) {
            $differences.Add([pscustomobject]@{ Path = $relative; Problem = 'only on the branch (master did not produce it)'; Details = @() })
            continue
        }
        if (-not $entry.ContainsKey('Branch')) {
            $differences.Add([pscustomobject]@{ Path = $relative; Problem = 'only in master''s output (the branch has no snapshot)'; Details = @() })
            continue
        }
        $count++
        $branchText = Read-NormalizedText $entry['Branch']
        $referenceText = Read-NormalizedText $entry['Reference']
        if ($branchText -ceq $referenceText) { continue }
        $branchLines = $branchText -split "`n"
        $referenceLines = $referenceText -split "`n"
        $details = New-Object 'System.Collections.Generic.List[string]'
        $onlyMaster = @(Get-MissingLines -Source $referenceLines -Other $branchLines)
        $onlyBranch = @(Get-MissingLines -Source $branchLines -Other $referenceLines)
        $details.Add("$($onlyMaster.Count) row(s) only in master's output (-), $($onlyBranch.Count) row(s) only in the branch's snapshot (+)")
        foreach ($row in ($onlyMaster | Select-Object -First $MaximumRows)) { $details.Add("  - line $($row.Line): $($row.Text)") }
        if ($onlyMaster.Count -gt $MaximumRows) { $details.Add("  - ... $($onlyMaster.Count - $MaximumRows) more") }
        foreach ($row in ($onlyBranch | Select-Object -First $MaximumRows)) { $details.Add("  + line $($row.Line): $($row.Text)") }
        if ($onlyBranch.Count -gt $MaximumRows) { $details.Add("  + ... $($onlyBranch.Count - $MaximumRows) more") }
        if ($onlyMaster.Count -eq 0 -and $onlyBranch.Count -eq 0) { $details.Add('  same rows, different order') }
        $differences.Add([pscustomobject]@{ Path = $relative; Problem = 'differs'; Details = $details.ToArray() })
    }
    return [pscustomobject]@{ Compared = $count; Differences = $differences.ToArray(); FileCount = $files.Count }
}

# ---------------------------------------------------------------------------------------------------------------------
# Handler order text (the manifest SWLOR.Game.Server/Core/ScriptHandlerOrder.txt and the P0-5 snapshots share a format)
# ---------------------------------------------------------------------------------------------------------------------

# script name -> @{ action = list of Namespace.Type.Method; conditional = list }, scripts in file order.
function ConvertFrom-HandlerOrderText {
    param([string]$Text)
    $scripts = New-Object 'System.Collections.Specialized.OrderedDictionary'
    $current = $null
    foreach ($raw in ($Text -split "`r?`n")) {
        $trimmed = $raw.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#') -or $raw.StartsWith('== ')) { continue }
        if ($raw[0] -ne ' ' -and $raw[0] -ne "`t") {
            $comment = $raw.IndexOf('  #')
            $name = $raw
            if ($comment -ge 0) { $name = $raw.Substring(0, $comment) }
            $name = $name.Trim()
            $current = @{
                action      = (New-Object 'System.Collections.Generic.List[string]')
                conditional = (New-Object 'System.Collections.Generic.List[string]')
            }
            $scripts[$name] = $current
            continue
        }
        if ($trimmed -eq '(no handlers)' -or $null -eq $current) { continue }
        $parts = $trimmed -split '\s+', 2
        if ($parts.Count -eq 2 -and $current.ContainsKey($parts[0])) { $current[$parts[0]].Add($parts[1].Trim()) }
    }
    return $scripts
}

function Get-HandlerCount {
    param($Parsed)
    $total = 0
    foreach ($name in $Parsed.Keys) { $total += $Parsed[$name].action.Count + $Parsed[$name].conditional.Count }
    return $total
}

# Differences between two parsed handler orders, as report lines. $Branch is the branch's manifest, $Reference is
# master's reflection order.
function Compare-HandlerOrders {
    param($Branch, $Reference, [int]$Maximum = 12)
    $lines = New-Object 'System.Collections.Generic.List[string]'
    $names = New-Object 'System.Collections.Generic.List[string]'
    foreach ($name in $Reference.Keys) { $names.Add($name) }
    foreach ($name in $Branch.Keys) { if (-not $names.Contains($name)) { $names.Add($name) } }
    foreach ($name in $names) {
        if (-not $Branch.Contains($name)) { $lines.Add("script '$name' is only on master ($($Reference[$name].action.Count + $Reference[$name].conditional.Count) handler(s))"); continue }
        if (-not $Reference.Contains($name)) { $lines.Add("script '$name' is only in the branch's manifest ($($Branch[$name].action.Count + $Branch[$name].conditional.Count) handler(s))"); continue }
        foreach ($kind in @('action', 'conditional')) {
            $b = @($Branch[$name][$kind])
            $r = @($Reference[$name][$kind])
            if ($b.Count -eq 0 -and $r.Count -eq 0) { continue }
            $identical = ($b.Count -eq $r.Count)
            for ($i = 0; $identical -and $i -lt $b.Count; $i++) {
                if ($b[$i] -cne $r[$i]) { $identical = $false }
            }
            if ($identical) { continue }
            $onlyMaster = @(Get-MissingLines -Source $r -Other $b)
            $onlyBranch = @(Get-MissingLines -Source $b -Other $r)
            foreach ($row in $onlyMaster) { $lines.Add("${name} ${kind}: on master, not in the branch's manifest: $($row.Text) (master position $($row.Line))") }
            foreach ($row in $onlyBranch) { $lines.Add("${name} ${kind}: in the branch's manifest, not on master: $($row.Text)") }

            # The handlers both sides have must also run in the same order, whatever else differs.
            $commonBranch = @($b | Where-Object { $r -ccontains $_ })
            $commonMaster = @($r | Where-Object { $b -ccontains $_ })
            $first = 0
            while ($first -lt $commonBranch.Count -and $first -lt $commonMaster.Count -and $commonBranch[$first] -ceq $commonMaster[$first]) { $first++ }
            if ($first -lt $commonBranch.Count -and $first -lt $commonMaster.Count) {
                $lines.Add("${name} ${kind}: different order; of the handlers both have, position $($first + 1) is '$($commonBranch[$first])' on the branch and '$($commonMaster[$first])' on master")
            }
        }
    }
    if ($lines.Count -gt $Maximum) {
        $rest = $lines.Count - $Maximum
        $lines = New-Object 'System.Collections.Generic.List[string]' (, @($lines | Select-Object -First $Maximum))
        $lines.Add("... $rest more")
    }
    return $lines.ToArray()
}

function Format-HandlerLine {
    param([string]$Kind, [string]$Name)
    if ($Kind -eq 'action') { return "  action      $Name" }
    return "  conditional $Name"
}

# Adds the handlers master's order lists and the manifest does not, each right after the nearest preceding handler (in
# master's order) that the manifest already lists; with none listed before it, right before the nearest following one.
# A new handler whose method or type name matches a manifest line that master no longer has is a probable rename or
# move: it is NOT inserted and is reported, because the manifest line keeps its position and a person edits the name.
function Add-NewHandlersToManifest {
    param([string]$ManifestText, [string]$ReferenceText)

    $manifest = ConvertFrom-HandlerOrderText $ManifestText
    $reference = ConvertFrom-HandlerOrderText $ReferenceText
    $eol = "`n"
    if ($ManifestText.Contains("`r`n")) { $eol = "`r`n" }
    $lines = New-Object 'System.Collections.Generic.List[string]'
    $lines.AddRange([string[]]@($ManifestText -split "`r?`n"))

    $inserted = New-Object 'System.Collections.Generic.List[string]'
    $renames = New-Object 'System.Collections.Generic.List[string]'
    $stale = New-Object 'System.Collections.Generic.List[string]'

    foreach ($script in $reference.Keys) {
        foreach ($kind in @('action', 'conditional')) {
            $r = @($reference[$script][$kind])
            $m = New-Object 'System.Collections.Generic.List[string]'
            if ($manifest.Contains($script)) { $m.AddRange([string[]]@($manifest[$script][$kind])) }

            # Match the manifest against master's list one for one.
            $left = New-Object 'System.Collections.Generic.Dictionary[string,int]'
            foreach ($name in $m) { $c = 0; [void]$left.TryGetValue($name, [ref]$c); $left[$name] = $c + 1 }
            $newIndexes = New-Object 'System.Collections.Generic.List[int]'
            for ($i = 0; $i -lt $r.Count; $i++) {
                $c = 0
                if ($left.TryGetValue($r[$i], [ref]$c) -and $c -gt 0) { $left[$r[$i]] = $c - 1 } else { $newIndexes.Add($i) }
            }
            $staleNames = New-Object 'System.Collections.Generic.List[string]'
            foreach ($name in $m) {
                $c = 0
                if ($left.TryGetValue($name, [ref]$c) -and $c -gt 0) { $staleNames.Add($name); $left[$name] = $c - 1 }
            }

            $toInsert = New-Object 'System.Collections.Generic.List[int]'
            foreach ($index in $newIndexes) {
                $name = $r[$index]
                $partner = $null
                foreach ($candidate in $staleNames) {
                    $sameMethod = ($candidate.Substring($candidate.LastIndexOf('.') + 1) -ceq $name.Substring($name.LastIndexOf('.') + 1))
                    $sameType = ($candidate.Substring(0, $candidate.LastIndexOf('.')).Split('.')[-1] -ceq $name.Substring(0, $name.LastIndexOf('.')).Split('.')[-1])
                    if ($sameMethod -or $sameType) { $partner = $candidate; break }
                }
                if ($partner) {
                    $renames.Add("$script ${kind}: $partner -> $name")
                    [void]$staleNames.Remove($partner)
                }
                else {
                    $toInsert.Add($index)
                }
            }
            foreach ($name in $staleNames) { $stale.Add("$script ${kind}: $name") }
            if ($toInsert.Count -eq 0) { continue }

            # Work out the new order of this script's kind.
            $work = New-Object 'System.Collections.Generic.List[string]'
            $work.AddRange([string[]]$m.ToArray())
            foreach ($index in $toInsert) {
                $position = -1
                for ($j = $index - 1; $j -ge 0 -and $position -lt 0; $j--) {
                    $at = $work.LastIndexOf($r[$j])
                    if ($at -ge 0) { $position = $at + 1 }
                }
                for ($j = $index + 1; $j -lt $r.Count -and $position -lt 0; $j++) {
                    $at = $work.IndexOf($r[$j])
                    if ($at -ge 0) { $position = $at }
                }
                if ($position -lt 0) { $position = $work.Count }
                $work.Insert($position, $r[$index])
                $inserted.Add("$script ${kind}: $($r[$index])")
            }

            Set-ManifestHandlers -Lines $lines -Script $script -Kind $kind -Names $work.ToArray()
        }
    }

    return [pscustomobject]@{
        Text             = ($lines.ToArray() -join $eol)
        Inserted         = $inserted.ToArray()
        RenameCandidates = $renames.ToArray()
        StaleLines       = $stale.ToArray()
    }
}

# Replaces the handler lines of one script and kind in the manifest lines, creating the script's block when it is new.
function Set-ManifestHandlers {
    param($Lines, [string]$Script, [string]$Kind, [string[]]$Names)

    $header = -1
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        $line = $Lines[$i]
        if ($line.Length -gt 0 -and $line[0] -ne ' ' -and $line[0] -ne "`t" -and $line[0] -ne '#' -and -not $line.StartsWith('== ') -and
            ($line.TrimEnd() -ceq $Script -or $line.StartsWith($Script + '  #'))) {
            $header = $i
            break
        }
    }

    $newLines = @($Names | ForEach-Object { Format-HandlerLine -Kind $Kind -Name $_ })

    if ($header -lt 0) {
        # A new script goes into "OTHER SCRIPTS", in alphabetical position (ordinal), as the generator writes them.
        $other = -1
        for ($i = 0; $i -lt $Lines.Count; $i++) { if ($Lines[$i].StartsWith('== OTHER SCRIPTS')) { $other = $i; break } }
        if ($other -lt 0) { throw (New-Failure "The manifest has no '== OTHER SCRIPTS' section to put the new script '$Script' in." 3) }
        $at = $Lines.Count
        for ($i = $other + 1; $i -lt $Lines.Count; $i++) {
            $line = $Lines[$i]
            if ($line.Length -gt 0 -and $line[0] -ne ' ' -and $line[0] -ne "`t" -and $line[0] -ne '#') {
                $name = $line
                $comment = $name.IndexOf('  #')
                if ($comment -ge 0) { $name = $name.Substring(0, $comment) }
                if ([string]::CompareOrdinal($name.Trim(), $Script) -gt 0) { $at = $i; break }
            }
        }
        $block = @($Script) + $newLines + @('')
        if ($at -ge $Lines.Count) {
            # Before the file's final empty element, after the last block's trailing blank.
            $final = $Lines.Count
            if ($final -gt 0 -and $Lines[$final - 1] -eq '') { $final-- }
            $Lines.InsertRange($final, [string[]](@('') + @($Script) + $newLines))
        }
        else {
            $Lines.InsertRange($at, [string[]]$block)
        }
        return
    }

    $end = $Lines.Count
    for ($i = $header + 1; $i -lt $Lines.Count; $i++) {
        $line = $Lines[$i]
        if ($line.Length -gt 0 -and $line[0] -ne ' ' -and $line[0] -ne "`t") { $end = $i; break }
    }

    # Drop the "(no handlers)" placeholder of an empty script.
    for ($i = $end - 1; $i -gt $header; $i--) {
        if ($Lines[$i].Trim() -eq '(no handlers)') { $Lines.RemoveAt($i); $end-- }
    }

    $kindIndexes = New-Object 'System.Collections.Generic.List[int]'
    for ($i = $header + 1; $i -lt $end; $i++) {
        $parts = $Lines[$i].Trim() -split '\s+', 2
        if ($parts[0] -ceq $Kind) { $kindIndexes.Add($i) }
    }

    if ($kindIndexes.Count -gt 0) {
        $first = $kindIndexes[0]
        for ($k = $kindIndexes.Count - 1; $k -ge 0; $k--) { $Lines.RemoveAt($kindIndexes[$k]) }
        $Lines.InsertRange($first, [string[]]$newLines)
        return
    }

    # No line of this kind yet: action lines come first in the block, conditional lines after the last handler line.
    $at = $header + 1
    if ($Kind -eq 'conditional') {
        $at = $end
        while ($at -gt $header + 1 -and $Lines[$at - 1].Trim().Length -eq 0) { $at-- }
    }
    $Lines.InsertRange($at, [string[]]$newLines)
}
