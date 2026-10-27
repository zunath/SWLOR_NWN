<#
.SYNOPSIS
    Rewrites DateTime.UtcNow to Clock.UtcNow and Environment.TickCount64 to Clock.TickCount64 in the files listed in
    tools/Refactor/ClockSeamFiles.txt, adding `using SWLOR.Game.Server.Core;` where a file needs it.

.DESCRIPTION
    Part of the maintainability refactor (item P0-4d1). The rewrite is mechanical and idempotent: a second run changes
    nothing. The daily sync and the parity script re-run it after merging master, so new master code in the listed
    files is rewritten the same way.

    Runs on Windows PowerShell 5.1 and PowerShell 7. Files are read and written byte for byte (Latin-1), so the BOM,
    encoding and line endings of each file are kept exactly; inserted lines use the file's own line ending. Comment-only
    lines are left alone.

.PARAMETER Check
    Change nothing. Print what would change and exit 1 if anything would, 0 if every file is already rewritten.

.PARAMETER RepositoryRoot
    Repository root. Defaults to two folders above this script.

.EXAMPLE
    pwsh -NoProfile -File tools/Refactor/Apply-ClockSeam.ps1
    pwsh -NoProfile -File tools/Refactor/Apply-ClockSeam.ps1 -Check
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [string]$RepositoryRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $RepositoryRoot) {
    $RepositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}
$RepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
$serverRoot = Join-Path $RepositoryRoot 'SWLOR.Game.Server'
$manifestPath = Join-Path $PSScriptRoot 'ClockSeamFiles.txt'

$latin1 = [System.Text.Encoding]::GetEncoding(28591)
$usingLine = 'using SWLOR.Game.Server.Core;'

# DateTime.UtcNow / System.DateTime.UtcNow and Environment.TickCount64 / System.Environment.TickCount64, not part of
# a longer member access such as Foo.DateTime.UtcNow.
$rewrites = @(
    @{ Name = 'DateTime.UtcNow'; Pattern = '(?<![\w.])(?:System\.)?DateTime\.UtcNow\b'; Replacement = 'Clock.UtcNow' },
    @{ Name = 'Environment.TickCount64'; Pattern = '(?<![\w.])(?:System\.)?Environment\.TickCount64\b'; Replacement = 'Clock.TickCount64' }
)

function Get-ManifestFiles {
    $paths = New-Object 'System.Collections.Generic.List[string]'
    foreach ($raw in [System.IO.File]::ReadAllLines($manifestPath)) {
        $entry = $raw.Trim()
        if ($entry.Length -eq 0 -or $entry.StartsWith('#')) { continue }

        if ($entry.EndsWith('/**')) {
            $folder = Join-Path $serverRoot $entry.Substring(0, $entry.Length - 3)
            if (-not (Test-Path -LiteralPath $folder -PathType Container)) {
                throw "Manifest folder not found: $entry"
            }
            foreach ($file in Get-ChildItem -LiteralPath $folder -Recurse -File -Filter '*.cs') {
                $paths.Add($file.FullName)
            }
        }
        else {
            $file = Join-Path $serverRoot $entry
            if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
                throw "Manifest file not found: $entry"
            }
            $paths.Add([System.IO.Path]::GetFullPath($file))
        }
    }

    $unique = [string[]]@($paths | Select-Object -Unique)
    [System.Array]::Sort($unique, [System.StringComparer]::Ordinal)
    return $unique
}

function Add-CoreUsing([string[]]$lines, [string]$eol) {
    $namespaceIndex = -1
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match '^\s*namespace\s') { $namespaceIndex = $i; break }
    }
    if ($namespaceIndex -lt 0) { throw 'No namespace declaration found.' }

    $usings = @()
    $serverUsings = @()
    for ($i = 0; $i -lt $namespaceIndex; $i++) {
        if ($lines[$i] -match '^using\s') {
            $usings += $i
            if ($lines[$i] -match '^using\s+SWLOR\.') { $serverUsings += $i }
        }
    }

    $newLine = $usingLine + $eol
    $insertAt = -1
    $blankAfter = $false
    foreach ($i in $serverUsings) {
        if ([string]::CompareOrdinal($lines[$i].TrimEnd(), $usingLine) -gt 0) { $insertAt = $i; break }
    }
    if ($insertAt -lt 0 -and $serverUsings.Count -gt 0) { $insertAt = $serverUsings[-1] + 1 }
    if ($insertAt -lt 0 -and $usings.Count -gt 0) { $insertAt = $usings[-1] + 1 }
    if ($insertAt -lt 0) { $insertAt = $namespaceIndex; $blankAfter = $true }

    $result = New-Object 'System.Collections.Generic.List[string]'
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($i -eq $insertAt) {
            $result.Add($newLine)
            if ($blankAfter) { $result.Add($eol) }
        }
        $result.Add($lines[$i])
    }
    return ,$result.ToArray()
}

$files = [string[]]@(Get-ManifestFiles)
$total = @{ 'DateTime.UtcNow' = 0; 'Environment.TickCount64' = 0 }
$changedFiles = 0
$usingsAdded = 0

foreach ($path in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    $text = $latin1.GetString($bytes)

    # A UTF-8 BOM reads as three Latin-1 characters; keep it out of the first line and put it back afterwards.
    $bom = ''
    $bomText = "$([char]0xEF)$([char]0xBB)$([char]0xBF)"
    if ($text.StartsWith($bomText)) {
        $bom = $text.Substring(0, 3)
        $text = $text.Substring(3)
    }

    $eol = "`n"
    if ($text.Contains("`r`n")) { $eol = "`r`n" }

    $lines = [string[]]@([regex]::Split($text, '(?<=\n)') | Where-Object { $_.Length -gt 0 })
    $counts = @{ 'DateTime.UtcNow' = 0; 'Environment.TickCount64' = 0 }
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        if ($line -match '^\s*(//|/\*|\*)') { continue }
        foreach ($rewrite in $rewrites) {
            $found = [regex]::Matches($line, $rewrite.Pattern)
            if ($found.Count -gt 0) {
                $line = [regex]::Replace($line, $rewrite.Pattern, $rewrite.Replacement)
                $counts[$rewrite.Name] += $found.Count
            }
        }
        $lines[$i] = $line
    }

    $usingAdded = $false
    $joined = -join $lines
    $usesClock = [regex]::IsMatch($joined, '(?<![\w.])Clock\.(UtcNow|TickCount64)\b')
    $inCoreNamespace = [regex]::IsMatch($joined, '(?m)^\s*namespace\s+SWLOR\.Game\.Server\.Core(\s|;|\.|$)')
    $hasUsing = [regex]::IsMatch($joined, '(?m)^\s*using\s+SWLOR\.Game\.Server\.Core\s*;')
    if ($usesClock -and -not $hasUsing -and -not $inCoreNamespace) {
        $lines = Add-CoreUsing $lines $eol
        $usingAdded = $true
    }

    $replaced = $counts['DateTime.UtcNow'] + $counts['Environment.TickCount64']
    if ($replaced -eq 0 -and -not $usingAdded) { continue }

    $relative = $path.Substring($RepositoryRoot.Length).TrimStart('\', '/').Replace('\', '/')
    $summary = "{0}: {1} DateTime.UtcNow, {2} Environment.TickCount64" -f $relative, $counts['DateTime.UtcNow'], $counts['Environment.TickCount64']
    if ($usingAdded) { $summary += ', using added'; $usingsAdded++ }
    Write-Host $summary

    $changedFiles++
    $total['DateTime.UtcNow'] += $counts['DateTime.UtcNow']
    $total['Environment.TickCount64'] += $counts['Environment.TickCount64']

    if (-not $Check) {
        [System.IO.File]::WriteAllBytes($path, $latin1.GetBytes($bom + (-join $lines)))
    }
}

$verb = 'Changed'
if ($Check) { $verb = 'Would change' }
Write-Host ("{0} {1} of {2} files: {3} DateTime.UtcNow and {4} Environment.TickCount64 rewritten, {5} using lines added." -f `
    $verb, $changedFiles, $files.Length, $total['DateTime.UtcNow'], $total['Environment.TickCount64'], $usingsAdded)

if ($Check -and $changedFiles -gt 0) { exit 1 }
exit 0
