<#
.SYNOPSIS
    Rewrites DateTime.UtcNow to Clock.UtcNow and Environment.TickCount64 to Clock.TickCount64 in the files listed in
    tools/Refactor/ClockSeamFiles.txt, adding `using SWLOR.Game.Server.Core;` where a file needs it.

.DESCRIPTION
    Part of the maintainability refactor (item P0-4d1). The rewrite is mechanical and idempotent: a second run changes
    nothing. The daily sync and the parity script re-run it after merging master, so new master code in the listed
    files is rewritten the same way.

    Only code is rewritten. A small C# scanner (compiled with Add-Type) walks each file the way the C# lexer does:
    comments, preprocessor lines, character literals and the text of string literals are copied unchanged, while the
    expressions inside the holes of interpolated strings (nested holes, nested strings and char literals included, but
    not their format clauses) are code. DateTime.UtcNow, System.DateTime.UtcNow and global::System.DateTime.UtcNow, and
    the same three forms of Environment.TickCount64, are rewritten. Reads through a using alias are not; the guard test
    (ClockSeamSourceGuardTests) resolves them and fails, so they are handled by hand.

    Runs on Windows PowerShell 5.1 and PowerShell 7. Files are read and written byte for byte (Latin-1), so the BOM,
    encoding and line endings of each file are kept exactly; inserted lines use the file's own line ending.

.PARAMETER Check
    Change nothing. Print what would change and exit 1 if anything would, 0 if every file is already rewritten.

.PARAMETER ListOwnedFiles
    Change nothing and print only the repository-relative paths (forward slashes, one per line) of the files this
    script owns, which are the files listed in ClockSeamFiles.txt. The daily sync uses it to decide which merge
    conflicts it may settle by taking master's version and running this script again
    (tools/Refactor/MechanicalScripts.txt).

.PARAMETER RepositoryRoot
    Repository root. Defaults to two folders above this script.

.EXAMPLE
    pwsh -NoProfile -File tools/Refactor/Apply-ClockSeam.ps1
    pwsh -NoProfile -File tools/Refactor/Apply-ClockSeam.ps1 -Check
    pwsh -NoProfile -File tools/Refactor/Apply-ClockSeam.ps1 -ListOwnedFiles
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$ListOwnedFiles,
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

# Plain C# 5 so that Windows PowerShell 5.1 can compile it.
$rewriterSource = @'
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace SwlorRefactor
{
    public sealed class ClockSeamResult
    {
        public string Text;
        public int UtcNow;
        public int TickCount64;
        public bool ClockUsed;
    }

    public sealed class ClockSeamRewriter
    {
        private static readonly Regex Utc = new Regex(@"\G(?:global::)?(?:System\.)?DateTime\.UtcNow\b", RegexOptions.CultureInvariant);
        private static readonly Regex Tick = new Regex(@"\G(?:global::)?(?:System\.)?Environment\.TickCount64\b", RegexOptions.CultureInvariant);
        private static readonly Regex ClockRead = new Regex(@"\GClock\.(?:UtcNow|TickCount64)\b", RegexOptions.CultureInvariant);

        private readonly string _s;
        private readonly StringBuilder _o;
        private int _i;
        private int _utc;
        private int _tick;
        private bool _clock;

        private ClockSeamRewriter(string text)
        {
            _s = text;
            _o = new StringBuilder(text.Length + 256);
        }

        public static ClockSeamResult Rewrite(string text)
        {
            ClockSeamRewriter rewriter = new ClockSeamRewriter(text);
            rewriter.Code(false);
            ClockSeamResult result = new ClockSeamResult();
            result.Text = rewriter._o.ToString();
            result.UtcNow = rewriter._utc;
            result.TickCount64 = rewriter._tick;
            result.ClockUsed = rewriter._clock;
            return result;
        }

        // Copies code, rewriting clock reads, to the end of the text or, inside an interpolation hole, to the hole's
        // closing brace (left unconsumed).
        private void Code(bool inHole)
        {
            int depth = 0;
            while (_i < _s.Length)
            {
                char c = _s[_i];
                char next = _i + 1 < _s.Length ? _s[_i + 1] : '\0';

                if (c == '/' && next == '/') { CopyToEndOfLine(); continue; }
                if (c == '/' && next == '*') { CopyBlockComment(); continue; }
                if (c == '#' && AtLineStart()) { CopyToEndOfLine(); continue; }
                if (c == '\'') { CharLiteral(); continue; }
                if (StartsString()) { StringLiteral(); continue; }

                if (inHole)
                {
                    if (c == '(' || c == '[' || c == '{') depth++;
                    else if (c == ')' || c == ']') depth--;
                    else if (c == '}')
                    {
                        if (depth == 0) return;
                        depth--;
                    }
                    else if (c == ':')
                    {
                        if (next == ':') { Append(2); continue; }
                        if (depth == 0)
                        {
                            // The format clause is text up to the hole's closing brace.
                            while (_i < _s.Length && _s[_i] != '}') Append(1);
                            return;
                        }
                    }
                }

                if (c == '@' && _i + 1 < _s.Length && IsIdentifierStart(next))
                {
                    Append(1);
                    CopyIdentifier();
                    continue;
                }

                if (IsIdentifierStart(c)) { IdentifierOrRead(); continue; }
                Append(1);
            }
        }

        private void IdentifierOrRead()
        {
            int p = _i;
            if (!IsQualifiedBefore(p))
            {
                Match m = Utc.Match(_s, p);
                if (m.Success) { _o.Append("Clock.UtcNow"); _i += m.Length; _utc++; return; }
                m = Tick.Match(_s, p);
                if (m.Success) { _o.Append("Clock.TickCount64"); _i += m.Length; _tick++; return; }
                if (ClockRead.Match(_s, p).Success) _clock = true;
            }
            CopyIdentifier();
        }

        // True when the identifier at p is a member of something (after '.' or '::'), so it is not the type we look for.
        private bool IsQualifiedBefore(int p)
        {
            int j = p - 1;
            while (j >= 0 && char.IsWhiteSpace(_s[j])) j--;
            if (j < 0) return false;
            if (_s[j] == '.') return true;
            return _s[j] == ':' && j > 0 && _s[j - 1] == ':';
        }

        private void CopyIdentifier()
        {
            int q = _i;
            while (q < _s.Length && IsIdentifierPart(_s[q])) q++;
            if (q == _i) q = _i + 1;
            _o.Append(_s, _i, q - _i);
            _i = q;
        }

        private static bool IsIdentifierStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsIdentifierPart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private bool AtLineStart()
        {
            int j = _i - 1;
            while (j >= 0 && (_s[j] == ' ' || _s[j] == '\t')) j--;
            return j < 0 || _s[j] == '\n';
        }

        private void Append(int count)
        {
            if (_i + count > _s.Length) count = _s.Length - _i;
            _o.Append(_s, _i, count);
            _i += count;
        }

        private void CopyToEndOfLine()
        {
            while (_i < _s.Length && _s[_i] != '\n') Append(1);
        }

        private void CopyBlockComment()
        {
            int end = _s.IndexOf("*/", _i + 2, StringComparison.Ordinal);
            Append(end < 0 ? _s.Length - _i : end + 2 - _i);
        }

        private void CharLiteral()
        {
            int n = _s.Length;
            if (_i + 1 < n && _s[_i + 1] == '\\')
            {
                int j = _i + 3;
                int limit = Math.Min(n, _i + 12);
                while (j < limit && _s[j] != '\'') j++;
                if (j < limit && _s[j] == '\'') { Append(j + 1 - _i); return; }
            }
            else if (_i + 2 < n && _s[_i + 2] == '\'')
            {
                Append(3);
                return;
            }
            Append(1);
        }

        private bool StartsString()
        {
            int j = _i;
            while (j < _s.Length && (_s[j] == '$' || _s[j] == '@')) j++;
            return j < _s.Length && _s[j] == '"';
        }

        private void StringLiteral()
        {
            int dollars = 0;
            bool verbatim = false;
            while (_s[_i] == '$' || _s[_i] == '@')
            {
                if (_s[_i] == '$') dollars++; else verbatim = true;
                Append(1);
            }

            int quotes = 0;
            while (_i + quotes < _s.Length && _s[_i + quotes] == '"') quotes++;
            if (quotes >= 3)
            {
                // Raw string literal: copied as text up to the matching run of quotes.
                Append(quotes);
                int end = _s.IndexOf(new string('"', quotes), _i, StringComparison.Ordinal);
                Append(end < 0 ? _s.Length - _i : end + quotes - _i);
                return;
            }

            Append(1);
            bool interpolated = dollars > 0;
            while (_i < _s.Length)
            {
                char c = _s[_i];
                char next = _i + 1 < _s.Length ? _s[_i + 1] : '\0';
                if (c == '"')
                {
                    if (verbatim && next == '"') { Append(2); continue; }
                    Append(1);
                    return;
                }
                if (!verbatim && c == '\\') { Append(2); continue; }
                if (interpolated && c == '{')
                {
                    if (next == '{') { Append(2); continue; }
                    Append(1);
                    Code(true);
                    if (_i < _s.Length && _s[_i] == '}') Append(1);
                    continue;
                }
                if (interpolated && c == '}' && next == '}') { Append(2); continue; }
                Append(1);
            }
        }
    }
}
'@

if (-not ('SwlorRefactor.ClockSeamRewriter' -as [type])) {
    Add-Type -TypeDefinition $rewriterSource -Language CSharp
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

if ($ListOwnedFiles) {
    foreach ($path in $files) {
        Write-Output $path.Substring($RepositoryRoot.Length).TrimStart('\', '/').Replace('\', '/')
    }
    exit 0
}

$total =@{ 'DateTime.UtcNow' = 0; 'Environment.TickCount64' = 0 }
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

    $result = [SwlorRefactor.ClockSeamRewriter]::Rewrite($text)
    $rewritten = $result.Text
    $utcCount = $result.UtcNow
    $tickCount = $result.TickCount64

    $usingAdded = $false
    $inCoreNamespace = [regex]::IsMatch($rewritten, '(?m)^\s*namespace\s+SWLOR\.Game\.Server\.Core(\s|;|\.|$)')
    $hasUsing = [regex]::IsMatch($rewritten, '(?m)^\s*using\s+SWLOR\.Game\.Server\.Core\s*;')
    $usesClock = $result.ClockUsed -or ($utcCount + $tickCount) -gt 0
    if ($usesClock -and -not $hasUsing -and -not $inCoreNamespace) {
        $rewritten = Add-CoreUsing $rewritten $eol
        $usingAdded = $true
    }

    if (($utcCount + $tickCount) -eq 0 -and -not $usingAdded) { continue }

    $relative = $path.Substring($RepositoryRoot.Length).TrimStart('\', '/').Replace('\', '/')
    $summary = "{0}: {1} DateTime.UtcNow, {2} Environment.TickCount64" -f $relative, $utcCount, $tickCount
    if ($usingAdded) { $summary += ', using added'; $usingsAdded++ }
    Write-Host $summary

    $changedFiles++
    $total['DateTime.UtcNow'] += $utcCount
    $total['Environment.TickCount64'] += $tickCount

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
