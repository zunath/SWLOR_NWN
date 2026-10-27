<#
.SYNOPSIS
    Rewrites DateTime.UtcNow to Clock.UtcNow and Environment.TickCount64 to Clock.TickCount64 in the files listed in
    tools/Refactor/ClockSeamFiles.txt, adding `using SWLOR.Game.Server.Core;` where a file needs it.

.DESCRIPTION
    Part of the maintainability refactor (item P0-4d1). The rewrite is mechanical and idempotent: a second run changes
    nothing. The daily sync and the parity script re-run it after merging master, so new master code in the listed
    files is rewritten the same way.

    Only code is rewritten: the file is scanned for comments, string and character literals, and preprocessor lines,
    which are copied unchanged. The expressions inside the {holes} of interpolated strings are code and are rewritten.
    DateTime.UtcNow, System.DateTime.UtcNow and global::System.DateTime.UtcNow (and the same three forms of
    Environment.TickCount64) are all rewritten.

    Runs on Windows PowerShell 5.1 and PowerShell 7. Files are read and written byte for byte (Latin-1), so the BOM,
    encoding and line endings of each file are kept exactly; inserted lines use the file's own line ending.

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

# The reads to rewrite (not part of a longer member access such as Foo.DateTime.UtcNow), plus Clock reads already in
# place, which only tell the script that the file needs the using.
$readAlternatives = @(
    '(?<utc>(?<![\w.])(?<!::)(?:global::)?(?:System\.)?DateTime\.UtcNow\b)',
    '(?<tick>(?<![\w.])(?<!::)(?:global::)?(?:System\.)?Environment\.TickCount64\b)',
    '(?<clock>(?<![\w.])Clock\.(?:UtcNow|TickCount64)\b)'
)
$readPattern = $readAlternatives -join '|'

# Everything that is not code, tried before the reads so that a read inside one of them is never matched: comments,
# preprocessor lines, raw, verbatim, regular and character literals. Interpolated strings are matched separately because
# their {holes} are code.
$scanPattern = (@(
    '(?<skip>//[^\r\n]*)',
    '(?<skip>/\*[\s\S]*?\*/)',
    '(?<skip>(?m:^[ \t]*#[^\r\n]*))',
    '(?<skip>"""[\s\S]*?""")',
    '(?<interp>\$@"(?:[^"{]|""|\{\{|\{[^{}]*\})*")',
    '(?<interp>@\$"(?:[^"{]|""|\{\{|\{[^{}]*\})*")',
    '(?<interp>\$"(?:[^"\\\r\n{]|\\.|\{\{|\{[^{}\r\n]*\})*")',
    '(?<skip>@"(?:[^"]|"")*")',
    '(?<skip>"(?:[^"\\\r\n]|\\.)*")',
    '(?<skip>''(?:[^''\\\r\n]|\\(?:u[0-9A-Fa-f]{4}|x[0-9A-Fa-f]{1,4}|.))'')'
) + $readAlternatives) -join '|'
$holePattern = '\{\{|\}\}|(?<hole>\{[^{}]*\})'

$script:counts = @{ 'DateTime.UtcNow' = 0; 'Environment.TickCount64' = 0 }
$script:clockUsed = $false

$readEvaluator = [System.Text.RegularExpressions.MatchEvaluator]{
    param($m)
    if ($m.Groups['utc'].Success) { $script:counts['DateTime.UtcNow']++; return 'Clock.UtcNow' }
    if ($m.Groups['tick'].Success) { $script:counts['Environment.TickCount64']++; return 'Clock.TickCount64' }
    $script:clockUsed = $true
    return $m.Value
}

$holeEvaluator = [System.Text.RegularExpressions.MatchEvaluator]{
    param($m)
    if (-not $m.Groups['hole'].Success) { return $m.Value }
    return [regex]::Replace($m.Value, $readPattern, $readEvaluator)
}

$codeEvaluator = [System.Text.RegularExpressions.MatchEvaluator]{
    param($m)
    if ($m.Groups['interp'].Success) { return [regex]::Replace($m.Value, $holePattern, $holeEvaluator) }
    if ($m.Groups['skip'].Success) { return $m.Value }
    return $readEvaluator.Invoke($m)
}

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

function Add-CoreUsing([string]$text, [string]$eol) {
    $lines = [string[]]@([regex]::Split($text, '(?<=\n)') | Where-Object { $_.Length -gt 0 })

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

    # Before the first SWLOR using that sorts after it, else after the last SWLOR using, else after the last using, else
    # above the namespace with a blank line.
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
            $result.Add($usingLine + $eol)
            if ($blankAfter) { $result.Add($eol) }
        }
        $result.Add($lines[$i])
    }
    return -join $result
}

$files = [string[]]@(Get-ManifestFiles)
$total = @{ 'DateTime.UtcNow' = 0; 'Environment.TickCount64' = 0 }
$changedFiles = 0
$usingsAdded = 0

foreach ($path in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    $text = $latin1.GetString($bytes)

    # A UTF-8 BOM reads as three Latin-1 characters; keep it out of the text and put it back afterwards.
    $bom = ''
    $bomText = "$([char]0xEF)$([char]0xBB)$([char]0xBF)"
    if ($text.StartsWith($bomText)) {
        $bom = $bomText
        $text = $text.Substring(3)
    }

    $eol = "`n"
    if ($text.Contains("`r`n")) { $eol = "`r`n" }

    $script:counts = @{ 'DateTime.UtcNow' = 0; 'Environment.TickCount64' = 0 }
    $script:clockUsed = $false
    $rewritten = [regex]::Replace($text, $scanPattern, $codeEvaluator)

    $usingAdded = $false
    $inCoreNamespace = [regex]::IsMatch($rewritten, '(?m)^\s*namespace\s+SWLOR\.Game\.Server\.Core(\s|;|\.|$)')
    $hasUsing = [regex]::IsMatch($rewritten, '(?m)^\s*using\s+SWLOR\.Game\.Server\.Core\s*;')
    $replaced = $script:counts['DateTime.UtcNow'] + $script:counts['Environment.TickCount64']
    $usesClock = $script:clockUsed -or $replaced -gt 0
    if ($usesClock -and -not $hasUsing -and -not $inCoreNamespace) {
        $rewritten = Add-CoreUsing $rewritten $eol
        $usingAdded = $true
    }

    if ($replaced -eq 0 -and -not $usingAdded) { continue }

    $relative = $path.Substring($RepositoryRoot.Length).TrimStart('\', '/').Replace('\', '/')
    $summary = "{0}: {1} DateTime.UtcNow, {2} Environment.TickCount64" -f $relative, $script:counts['DateTime.UtcNow'], $script:counts['Environment.TickCount64']
    if ($usingAdded) { $summary += ', using added'; $usingsAdded++ }
    Write-Host $summary

    $changedFiles++
    $total['DateTime.UtcNow'] += $script:counts['DateTime.UtcNow']
    $total['Environment.TickCount64'] += $script:counts['Environment.TickCount64']

    if (-not $Check) {
        [System.IO.File]::WriteAllBytes($path, $latin1.GetBytes($bom + $rewritten))
    }
}

$verb = 'Changed'
if ($Check) { $verb = 'Would change' }
Write-Host ("{0} {1} of {2} files: {3} DateTime.UtcNow and {4} Environment.TickCount64 rewritten, {5} using lines added." -f `
    $verb, $changedFiles, $files.Length, $total['DateTime.UtcNow'], $total['Environment.TickCount64'], $usingsAdded)

if ($Check -and $changedFiles -gt 0) { exit 1 }
exit 0
