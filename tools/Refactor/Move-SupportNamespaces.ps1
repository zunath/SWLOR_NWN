<#
.SYNOPSIS
    Moves every SWLOR.Game.Server.Service.<Name>Service[.<Topic>] support namespace to
    SWLOR.Game.Server.Domain.<Domain>[.<Topic>] and renames the other clashing namespaces (maintainability refactor,
    item P2-Z2).

.DESCRIPTION
    Rerunnable and idempotent. Exactly one mode:

      -Check   change nothing; print every change -Apply would make and exit 1 if there is any (0 when the tree is done).
      -Apply   make the changes; a second -Apply changes nothing.
      -Report  change nothing; list, for the current tree, what the P2-Z2 guard test checks: namespace segments equal to
               a type name of the server assembly, leaf segments under two parents, and any remaining
               Service.*Service namespace. Exit 1 when it lists anything.

    The domain of a service comes from SWLOR.Architecture.Tests/DomainMap.json. A service name missing from the map, a
    name in two domains, or two files that would land on the same path are reported and nothing is changed (exit 2).

    What it rewrites, in every tracked or new text file of the repository (code, razor, project files, scripts,
    generators, templates, manifests and guides; not Module/, SWLOR_Haks/, design/, the Maintainability* status guides,
    HandlerRenames.txt and the files named in textExcludes of the data file):

      * dotted names: Service.<N>Service becomes Domain.<Domain> (with SWLOR.Game.Server. kept or, for a partial name,
        added), so namespace, using, using static, alias, global using and fully qualified names follow; the topic
        children keep their names (Service.CraftService.Minigame -> Domain.Crafting.Minigame);
      * the renames of NamespaceMoves.json (Domain.Core, the three Feature clashes), dotted and as folder paths;
      * folder paths (Service/<N>Service/... in any text, with / or \ ) and the string-literal path pieces tests and
        generators build ("Service", "AIService"); the files themselves move (a filesystem move, then git add);
      * the handler order manifest (names are rewritten in place) and HandlerRenames.txt (a renamed handler is added with
        its old name, or its branch name is updated);
      * ClockSeamFiles.txt (expandGlobFiles: a moved folder glob becomes the moved files), the facade names of
        ConvertedServices.json (kept sorted) and the exact textEdits of the data file. Other path lists
        (CoverageFloors.json, PlayerMessageAudit.json) follow the folder-path rewrite.

    A using line that becomes a duplicate of one above it is dropped. Files are read and written byte for byte (Latin-1),
    so BOMs, encodings and line endings stay.

    Runs on Windows PowerShell 5.1 and PowerShell 7.

.PARAMETER Check
    Print the changes and exit 1 if there are any.

.PARAMETER Apply
    Make the changes.

.PARAMETER Report
    List the guard findings of the current tree.

.PARAMETER RepositoryRoot
    Repository root. Defaults to two folders above this script. Use a throwaway checkout to try it.

.PARAMETER MovesFile
    The data file. Defaults to NamespaceMoves.json beside the script.

.EXAMPLE
    powershell.exe -NoProfile -File tools/Refactor/Move-SupportNamespaces.ps1 -Check
    powershell.exe -NoProfile -File tools/Refactor/Move-SupportNamespaces.ps1 -Apply
    powershell.exe -NoProfile -File tools/Refactor/Move-SupportNamespaces.ps1 -Report
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$Apply,
    [switch]$Report,
    [string]$RepositoryRoot,
    [string]$MovesFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (([int][bool]$Check + [int][bool]$Apply + [int][bool]$Report) -ne 1) {
    Write-Host 'Pass exactly one of -Check, -Apply, -Report.'
    exit 2
}
if (-not $RepositoryRoot) { $RepositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot) }
$root = [System.IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/')
if (-not $MovesFile) { $MovesFile = Join-Path $PSScriptRoot 'NamespaceMoves.json' }

$latin1 = [System.Text.Encoding]::GetEncoding(28591)
$serverFolder = 'SWLOR.Game.Server'
$serverRoot = 'SWLOR.Game.Server.'
$domainMapPath = 'SWLOR.Architecture.Tests/DomainMap.json'
$manifestPath = 'SWLOR.Game.Server/Core/ScriptHandlerOrder.txt'
$handlerRenamesPath = 'tools/Refactor/HandlerRenames.txt'
$textExtensions = @('.cs', '.razor', '.cshtml', '.csx', '.ps1', '.py', '.sh', '.cmd', '.csproj', '.props', '.targets',
    '.txt', '.json', '.md', '.yml', '.yaml', '.runsettings', '.xml')
$skippedRoots = @('Module/', 'SWLOR_Haks/', 'design/', '.git/', '.claude/', 'node_modules/')

function Join-Rel { param([string]$Path) return [System.IO.Path]::Combine($root, $Path.Replace('/', [System.IO.Path]::DirectorySeparatorChar)) }

function Invoke-GitText {
    param([string[]]$Arguments)
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = 'git'
    $start.Arguments = (($Arguments | ForEach-Object { if ($_ -match '[\s"]') { '"' + $_.Replace('"', '\"') + '"' } else { $_ } }) -join ' ')
    $start.WorkingDirectory = $root
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $process = [System.Diagnostics.Process]::Start($start)
    $errors = $process.StandardError.ReadToEndAsync()
    $output = $process.StandardOutput.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "git $($start.Arguments) failed: $($errors.Result)" }
    return $output
}

# --- Inputs -----------------------------------------------------------------------------------------------------------

if (-not (Test-Path -LiteralPath $MovesFile)) { Write-Host "Data file not found: $MovesFile"; exit 2 }
$moves = Get-Content -LiteralPath $MovesFile -Raw | ConvertFrom-Json
$renames = @{}      # relative old namespace -> relative new namespace
foreach ($entry in $moves.namespaceRenames) { $renames[[string]$entry.from] = [string]$entry.to }
$textExcludes = @($moves.textExcludes)
$facadeFiles = @($moves.facadeNameFiles)
$globFiles = @($moves.expandGlobFiles)
$textEdits = @($moves.textEdits)

$domainMapFile = Join-Path $root $domainMapPath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $domainMapFile)) { Write-Host "Domain map not found: $domainMapPath"; exit 2 }
$domainMap = Get-Content -LiteralPath $domainMapFile -Raw | ConvertFrom-Json
$serviceDomain = @{}
$problems = New-Object 'System.Collections.Generic.List[string]'
foreach ($domain in $domainMap.domains.PSObject.Properties) {
    foreach ($service in @($domain.Value.services)) {
        if ($serviceDomain.ContainsKey([string]$service) -and $serviceDomain[[string]$service] -ne $domain.Name) {
            $problems.Add("Service '$service' is in two domains of DomainMap.json: $($serviceDomain[[string]$service]) and $($domain.Name).")
        }
        $serviceDomain[[string]$service] = $domain.Name
    }
}

# --- Files ------------------------------------------------------------------------------------------------------------

function Get-RepositoryFiles {
    $paths = New-Object 'System.Collections.Generic.List[string]'
    if (Test-Path -LiteralPath (Join-Path $root '.git')) {
        $listing = Invoke-GitText @('-c', 'core.quotepath=off', 'ls-files', '-z', '--cached', '--others', '--exclude-standard')
        foreach ($name in $listing.Split([char]0)) {
            if ($name.Length -gt 0 -and [System.IO.File]::Exists((Join-Rel $name))) { $paths.Add($name) }
        }
    } else {
        $prefix = $root.Length + 1
        foreach ($file in [System.IO.Directory]::EnumerateFiles($root, '*', [System.IO.SearchOption]::AllDirectories)) {
            $relative = $file.Substring($prefix).Replace('\', '/')
            if ($relative -match '(^|/)(bin|obj|\.git)/') { continue }
            $paths.Add($relative)
        }
    }
    return $paths
}

function Test-InTextScope {
    param([string]$Path)
    foreach ($skipped in $skippedRoots) { if ($Path.StartsWith($skipped, [System.StringComparison]::Ordinal)) { return $false } }
    if ($Path -match '(^|/)(bin|obj)/') { return $false }
    $extension = [System.IO.Path]::GetExtension($Path).ToLowerInvariant()
    if ($textExtensions -notcontains $extension) { return $false }
    foreach ($pattern in $textExcludes) {
        if ($Path -like [string]$pattern) { return $false }
    }
    if ($Path -eq $handlerRenamesPath) { return $false }
    return $true
}

$allFiles = Get-RepositoryFiles
$serverCs = @($allFiles | Where-Object { $_.StartsWith("$serverFolder/", [System.StringComparison]::Ordinal) -and $_.EndsWith('.cs', [System.StringComparison]::Ordinal) -and $_ -notmatch '/(bin|obj)/' })

function Read-Latin1 { param([string]$Relative) return $latin1.GetString([System.IO.File]::ReadAllBytes((Join-Rel $Relative))) }

# The support namespaces that exist: every namespace line Service.<N>Service[.*] of the server, plus the folders.
$namespaceLine = New-Object System.Text.RegularExpressions.Regex('(?m)^[ \t]*namespace[ \t]+([A-Za-z0-9_.]+)', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
$declaration = New-Object System.Text.RegularExpressions.Regex('^(?<indent> *)(?:\[[^\]\r\n]*\]\s*)*(?:(?:public|internal|static|sealed|abstract|partial|readonly|unsafe|file|ref)\s+)*(?:class|struct|enum|interface|record(?:\s+(?:class|struct))?)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)', [System.Text.RegularExpressions.RegexOptions]::Multiline)
$serviceNames = New-Object 'System.Collections.Generic.HashSet[string]'
$serviceMembers = @{}   # service name -> top-level types and child namespace segments of Service.<N>Service
$fileNamespaces = @{}
$fileTypes = @{}
foreach ($path in $serverCs) {
    $text = Read-Latin1 $path
    $found = New-Object 'System.Collections.Generic.List[string]'
    foreach ($m in $namespaceLine.Matches($text)) { $found.Add($m.Groups[1].Value) }
    $fileNamespaces[$path] = $found
    $types = New-Object 'System.Collections.Generic.List[string]'
    if ($found.Count -eq 1) {
        $top = if ($text -match '(?m)^namespace[ \t]+[A-Za-z0-9_.]+[ \t]*;') { 0 } else { 4 }
        foreach ($m in $declaration.Matches($text)) {
            if ($m.Groups['indent'].Length -eq $top) { $types.Add($m.Groups['name'].Value) }
        }
    }
    $fileTypes[$path] = $types
    foreach ($ns in $found) {
        if ($ns -match '^SWLOR\.Game\.Server\.Service\.([A-Za-z0-9_]+?)Service(?:\.([A-Za-z0-9_]+))?(?:\.|$)') {
            $name = $Matches[1]
            $child = $Matches[2]
            [void]$serviceNames.Add($name)
            if (-not $serviceMembers.ContainsKey($name)) { $serviceMembers[$name] = New-Object 'System.Collections.Generic.HashSet[string]' }
            if ($child) { [void]$serviceMembers[$name].Add($child) }
            else { foreach ($type in $types) { [void]$serviceMembers[$name].Add($type) } }
        }
    }
}
foreach ($path in $allFiles) {
    if ($path -match '^SWLOR\.Game\.Server/Service/([A-Za-z0-9_]+?)Service/') { [void]$serviceNames.Add($Matches[1]) }
}
foreach ($name in ($serviceNames | Sort-Object)) {
    if (-not $serviceDomain.ContainsKey($name)) { $problems.Add("Service '$name' (Service.${name}Service) is missing from DomainMap.json.") }
}

function Resolve-DomainNamespace {
    param([string]$Service)
    $namespace = 'Domain.' + $serviceDomain[$Service]
    if ($renames.ContainsKey($namespace)) { return $renames[$namespace] }
    return $namespace
}

# --- Report mode ------------------------------------------------------------------------------------------------------

if ($Report) {
    $typeNames = @{}
    $declaration = New-Object System.Text.RegularExpressions.Regex('^(?<indent> *)(?:\[[^\]\r\n]*\]\s*)*(?:(?:public|internal|static|sealed|abstract|partial|readonly|unsafe|file|ref)\s+)*(?:class|struct|enum|interface|record(?:\s+(?:class|struct))?)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)', [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $namespaces = New-Object 'System.Collections.Generic.HashSet[string]'
    $namespaceFiles = @{}
    foreach ($path in $serverCs) {
        $text = Read-Latin1 $path
        $filescoped = $text -match '(?m)^namespace[ \t]+[A-Za-z0-9_.]+[ \t]*;'
        $top = if ($filescoped) { 0 } else { 4 }
        foreach ($ns in $fileNamespaces[$path]) {
            [void]$namespaces.Add($ns)
            if (-not $namespaceFiles.ContainsKey($ns)) { $namespaceFiles[$ns] = 0 }
            $namespaceFiles[$ns]++
        }
        if ($fileNamespaces[$path].Count -eq 1) {
            foreach ($m in $declaration.Matches($text)) {
                if ($m.Groups['indent'].Length -ne $top) { continue }
                $name = $m.Groups['name'].Value
                if (-not $typeNames.ContainsKey($name)) { $typeNames[$name] = New-Object 'System.Collections.Generic.List[string]' }
                $typeNames[$name].Add($fileNamespaces[$path][0] + '.' + $name)
            }
        }
    }

    $lines = New-Object 'System.Collections.Generic.List[string]'
    $segmentClash = New-Object 'System.Collections.Generic.List[string]'
    foreach ($ns in ($namespaces | Sort-Object)) {
        if (-not $ns.StartsWith($serverRoot, [System.StringComparison]::Ordinal)) { continue }
        $segments = $ns.Substring($serverRoot.Length).Split('.')
        for ($i = 0; $i -lt $segments.Length; $i++) {
            if ($typeNames.ContainsKey($segments[$i])) {
                $segmentClash.Add("$ns : segment '$($segments[$i])' is also a type ($(($typeNames[$segments[$i]] | Select-Object -First 2) -join ', '))")
                break
            }
        }
    }
    $lines.Add("Namespace segments equal to a type name of the server assembly: $($segmentClash.Count)")
    foreach ($line in $segmentClash) { $lines.Add("  $line") }

    $leaves = @{}
    foreach ($ns in $namespaces) {
        if (-not $ns.StartsWith($serverRoot, [System.StringComparison]::Ordinal)) { continue }
        $dot = $ns.LastIndexOf('.')
        $leaf = $ns.Substring($dot + 1)
        $parent = $ns.Substring(0, $dot)
        if ($parent -eq $ns.Substring(0, $serverRoot.Length - 1)) { continue }
        if (-not $leaves.ContainsKey($leaf)) { $leaves[$leaf] = New-Object 'System.Collections.Generic.List[string]' }
        $leaves[$leaf].Add($parent.Substring($serverRoot.Length))
    }
    $twice = @($leaves.Keys | Where-Object { $leaves[$_].Count -gt 1 } | Sort-Object)
    $lines.Add("Leaf segments under two parents: $($twice.Count)")
    foreach ($leaf in $twice) { $lines.Add("  $leaf under $((($leaves[$leaf] | Sort-Object) -join ', '))") }

    $remaining = @($namespaces | Where-Object { $_ -match '^SWLOR\.Game\.Server\.Service\.[A-Za-z0-9_]*Service(\.|$)' } | Sort-Object)
    $lines.Add("Remaining Service.*Service namespaces: $($remaining.Count)")
    foreach ($ns in $remaining) { $lines.Add("  $ns ($($namespaceFiles[$ns]) files)") }

    $lines | ForEach-Object { Write-Host $_ }
    if ($segmentClash.Count + $twice.Count + $remaining.Count -gt 0) { exit 1 }
    exit 0
}

if ($problems.Count -gt 0) {
    Write-Host 'Cannot continue:'
    $problems | ForEach-Object { Write-Host "  $_" }
    exit 2
}

# --- Path plan --------------------------------------------------------------------------------------------------------

$renamePaths = @{}   # 'Domain/Core' -> 'Domain/Foundation'
foreach ($from in $renames.Keys) { $renamePaths[$from.Replace('.', '/')] = $renames[$from].Replace('.', '/') }

# Maps a path below SWLOR.Game.Server/ (forward slashes) to its new path, or returns it unchanged.
function Get-MappedServerPath {
    param([string]$Relative)
    $path = $Relative
    if ($path -match '^Service/([A-Za-z0-9_]+?)Service/(.*)$' -and $serviceNames.Contains($Matches[1])) {
        $path = 'Domain/' + $serviceDomain[$Matches[1]] + '/' + $Matches[2]
    }
    foreach ($from in ($renamePaths.Keys | Sort-Object Length -Descending)) {
        if ($path.StartsWith($from + '/', [System.StringComparison]::Ordinal)) {
            $path = $renamePaths[$from] + $path.Substring($from.Length)
            break
        }
    }
    return $path
}

$moveMap = @{}      # old repository path -> new repository path
foreach ($path in $allFiles) {
    if (-not $path.StartsWith("$serverFolder/", [System.StringComparison]::Ordinal)) { continue }
    if ($path -match '/(bin|obj)/') { continue }
    $below = $path.Substring($serverFolder.Length + 1)
    $mapped = Get-MappedServerPath $below
    if ($mapped -ne $below) { $moveMap[$path] = "$serverFolder/$mapped" }
}

$destinations = @{}
foreach ($path in $allFiles) {
    $final = if ($moveMap.ContainsKey($path)) { $moveMap[$path] } else { $path }
    $key = $final.ToLowerInvariant()
    if (-not $destinations.ContainsKey($key)) { $destinations[$key] = New-Object 'System.Collections.Generic.List[string]' }
    $destinations[$key].Add($path)
}
foreach ($key in $destinations.Keys) {
    if ($destinations[$key].Count -gt 1) {
        $problems.Add("Would land on the same path ($key): $($destinations[$key] -join ', ')")
    }
}
if ($problems.Count -gt 0) {
    Write-Host 'Cannot continue:'
    $problems | ForEach-Object { Write-Host "  $_" }
    exit 2
}

# --- Text rewriting ---------------------------------------------------------------------------------------------------

$regexOptions = [System.Text.RegularExpressions.RegexOptions]::CultureInvariant
$renameDotted = ($renames.Keys | Sort-Object Length -Descending | ForEach-Object { [regex]::Escape($_) }) -join '|'
$dottedPattern = '(?<![\w.])(?<root>(?:SWLOR\.)?Game\.Server\.)?(?:Service\.(?<svc>[A-Za-z0-9_]+?)Service|(?<ren>' + $renameDotted + '))(?![\w])'
$dottedRegex = New-Object System.Text.RegularExpressions.Regex($dottedPattern, $regexOptions)
$renamePathAlt = ($renames.Keys | Sort-Object Length -Descending | ForEach-Object { ($_.Split('.') | ForEach-Object { [regex]::Escape($_) }) -join '[/\\]{1,2}' }) -join '|'
$pathRegex = New-Object System.Text.RegularExpressions.Regex(('(?<![\w])(?:Service(?<sep>[/\\]{1,2})(?<svc>[A-Za-z0-9_]+?)Service(?![\w])(?!\.\w)|(?<ren>' + $renamePathAlt + ')(?![\w]))'), $regexOptions)
$literalRegex = New-Object System.Text.RegularExpressions.Regex('"Service"(?<gap>\s*(?:,|/)\s*)"(?<svc>[A-Za-z0-9_]+?)Service"', $regexOptions)
$relativeRegex = New-Object System.Text.RegularExpressions.Regex('(?<![\w.])(?<svc>[A-Za-z0-9_]+?)Service\.(?<next>[A-Za-z_]\w*)', $regexOptions)
$renameLiterals = @{}
foreach ($from in $renames.Keys) {
    $oldSegments = $from.Split('.')
    $newSegments = $renames[$from].Split('.')
    if ($oldSegments.Length -ne $newSegments.Length) { continue }
    $pattern = ($oldSegments | ForEach-Object { '"' + [regex]::Escape($_) + '"' }) -join '(?<g>\s*(?:,|/)\s*)'
    $renameLiterals[$from] = New-Object System.Text.RegularExpressions.Regex($pattern, $regexOptions)
}
$facadeRegex = New-Object System.Text.RegularExpressions.Regex('("facade"\s*:\s*")(?<svc>[A-Za-z0-9_]+?)Service(?=\.)', $regexOptions)
$usingLineRegex = New-Object System.Text.RegularExpressions.Regex('(?m)^[ \t]*(?:global[ \t]+)?using[ \t]+(?!\()[^=;\r\n]+;[ \t]*\r?\n', $regexOptions)
$headerEndRegex = New-Object System.Text.RegularExpressions.Regex('(?m)^[ \t]*(?:namespace\b|\[|(?:public|internal|static|sealed|abstract|partial|readonly|unsafe|file|class|struct|enum|interface|record|delegate)\b)', $regexOptions)

$script:counts = @{ namespaceLines = 0; usingLines = 0; qualifiedNames = 0; partialNames = 0; paths = 0; literals = 0; renameNames = 0; duplicateUsings = 0; relativeNames = 0 }
$script:oldNamespaces = New-Object 'System.Collections.Generic.HashSet[string]'
$script:currentText = ''
$script:currentFile = ''
$script:warnings = New-Object 'System.Collections.Generic.List[string]'

function Get-LinePrefix {
    param([string]$Text, [int]$Index)
    $start = -1
    if ($Index -gt 0) { $start = $Text.LastIndexOf("`n", $Index - 1) }
    return $Text.Substring($start + 1, $Index - $start - 1)
}

$dottedEvaluator = [System.Text.RegularExpressions.MatchEvaluator] {
    param($m)
    $hasRoot = $m.Groups['root'].Success
    if ($m.Groups['svc'].Success) {
        $name = $m.Groups['svc'].Value
        if (-not $serviceNames.Contains($name)) { return $m.Value }
        $new = Resolve-DomainNamespace $name
        $prefix = Get-LinePrefix $script:currentText $m.Index
        if ($prefix -match '^\s*namespace\s+$') {
            $script:counts.namespaceLines++
            $rest = $script:currentText.Substring($m.Index)
            if ($rest -match '^[A-Za-z0-9_.]+') { [void]$script:oldNamespaces.Add($Matches[0]) }
        } elseif ($prefix -match '^\s*(global\s+)?using\s+(static\s+)?([A-Za-z0-9_.]+\s*=\s*)?(global::)?$' -or $prefix -match '^\s*@using\s+$') {
            $script:counts.usingLines++
        } else {
            $script:counts.qualifiedNames++
        }
        if (-not $hasRoot) { $script:counts.partialNames++ }
        if ($hasRoot) { return $m.Groups['root'].Value + $new }
        return $serverRoot + $new
    }
    $script:counts.renameNames++
    $old = $m.Groups['ren'].Value
    $prefix = Get-LinePrefix $script:currentText $m.Index
    if ($prefix -match '^\s*namespace\s+$') {
        $rest = $script:currentText.Substring($m.Index)
        if ($rest -match '^[A-Za-z0-9_.]+') { [void]$script:oldNamespaces.Add($Matches[0]) }
    }
    $result = $renames[$old]
    if ($hasRoot) { return $m.Groups['root'].Value + $result }
    return $result
}

$pathEvaluator = [System.Text.RegularExpressions.MatchEvaluator] {
    param($m)
    if ($m.Groups['svc'].Success) {
        $name = $m.Groups['svc'].Value
        if (-not $serviceNames.Contains($name)) { return $m.Value }
        $script:counts.paths++
        $sep = $m.Groups['sep'].Value
        $domainPath = 'Domain/' + $serviceDomain[$name]
        if ($renamePaths.ContainsKey($domainPath)) { $domainPath = $renamePaths[$domainPath] }
        return $domainPath.Replace('/', $sep)
    }
    $script:counts.paths++
    $old = $m.Groups['ren'].Value
    $sep = [regex]::Match($old, '[/\\]{1,2}').Value
    $key = [regex]::Replace($old, '[/\\]{1,2}', '/')
    return $renamePaths[$key].Replace('/', $sep)
}

$literalEvaluator = [System.Text.RegularExpressions.MatchEvaluator] {
    param($m)
    $name = $m.Groups['svc'].Value
    if (-not $serviceNames.Contains($name)) { return $m.Value }
    $script:counts.literals++
    $domainPath = 'Domain/' + $serviceDomain[$name]
    if ($renamePaths.ContainsKey($domainPath)) { $domainPath = $renamePaths[$domainPath] }
    $parts = $domainPath.Split('/')
    $gap = $m.Groups['gap'].Value
    return '"' + $parts[0] + '"' + $gap + '"' + $parts[1] + '"'
}

# A name relative to the Service namespace (AnimationService.AnimationClip, reached through a using of Service or an
# enclosing namespace) becomes fully qualified. Only a name whose second part is a type or child namespace of that
# support namespace counts, so a static member of the class <N>Service is left alone.
$relativeEvaluator = [System.Text.RegularExpressions.MatchEvaluator] {
    param($m)
    $name = $m.Groups['svc'].Value
    if (-not $serviceMembers.ContainsKey($name) -or -not $serviceMembers[$name].Contains($m.Groups['next'].Value)) { return $m.Value }
    $script:counts.qualifiedNames++
    $script:counts.relativeNames++
    return $serverRoot + (Resolve-DomainNamespace $name) + '.' + $m.Groups['next'].Value
}

$script:currentRename = ''
$renameLiteralEvaluator = [System.Text.RegularExpressions.MatchEvaluator] {
    param($m)
    $newSegments = $renames[$script:currentRename].Split('.')
    $gap = $m.Groups['g'].Captures
    $out = '"' + $newSegments[0] + '"'
    for ($i = 1; $i -lt $newSegments.Length; $i++) { $out += $gap[$i - 1].Value + '"' + $newSegments[$i] + '"' }
    $script:counts.literals++
    return $out
}
$facadeEvaluator = [System.Text.RegularExpressions.MatchEvaluator] {
    param($m)
    $name = $m.Groups['svc'].Value
    if (-not $serviceNames.Contains($name)) { return $m.Value }
    $new = Resolve-DomainNamespace $name
    return $m.Groups[1].Value + $new.Substring('Domain.'.Length)
}

function Remove-DuplicateUsings {
    param([string]$Text)
    $stop = $headerEndRegex.Match($Text)
    $limit = if ($stop.Success) { $stop.Index } else { $Text.Length }
    $seen = New-Object 'System.Collections.Generic.HashSet[string]'
    $drop = New-Object 'System.Collections.Generic.List[object]'
    foreach ($m in $usingLineRegex.Matches($Text)) {
        if ($m.Index -ge $limit) { break }
        $key = ($m.Value -replace '\s+', ' ').Trim()
        if (-not $seen.Add($key)) { $drop.Add($m) }
    }
    for ($i = $drop.Count - 1; $i -ge 0; $i--) {
        $Text = $Text.Remove($drop[$i].Index, $drop[$i].Length)
        $script:counts.duplicateUsings++
    }
    return $Text
}

# Code that sat in Service.<N>Service saw the facade classes of the parent namespace Service without a using, and its
# bare `Combat.` meant the facade. In Domain.<Domain> a domain namespace named like a facade (Combat, Space) hides the
# facade, so the moved file writes `Service.Combat.X` (the interim style of DependencyInjection.md), and a file that
# uses any facade gets `using SWLOR.Game.Server.Service;`. P2-Z removes both with the facades.
$facadeNames = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($path in $serverCs) {
    if ($fileNamespaces[$path].Count -eq 1 -and $fileNamespaces[$path][0] -eq 'SWLOR.Game.Server.Service') {
        foreach ($type in $fileTypes[$path]) { [void]$facadeNames.Add($type) }
    }
}
$hiddenFacades = New-Object 'System.Collections.Generic.List[string]'
foreach ($domainName in (@($serviceDomain.Values | Select-Object -Unique) + @($renames.Values | Where-Object { $_ -match '^Domain\.[^.]+$' } | ForEach-Object { $_.Substring(7) }))) {
    if ($facadeNames.Contains($domainName) -and -not $hiddenFacades.Contains($domainName)) { $hiddenFacades.Add($domainName) }
}
$facadeUseRegex = New-Object System.Text.RegularExpressions.Regex(('(?<![\w.@])(?:' + (($facadeNames | Sort-Object Length -Descending | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')\b'), $regexOptions)
$hiddenFacadeRegex = $null
if ($hiddenFacades.Count -gt 0) {
    $hiddenFacadeRegex = New-Object System.Text.RegularExpressions.Regex(('(?<![\w.@"''])(?<q>' + (($hiddenFacades | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')\.(?=[A-Za-z_])'), $regexOptions)
}
$serviceUsing = 'using SWLOR.Game.Server.Service;'
# A facade named like an NWScript function (Random) would bind to the using static method once the file leaves Service.
$nwscriptFunctions = New-Object 'System.Collections.Generic.HashSet[string]'
$nwscriptRegex = New-Object System.Text.RegularExpressions.Regex('\bstatic\s+[A-Za-z0-9_<>\[\],.?]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(', $regexOptions)
$nwscriptFolder = Join-Path $root 'SWLOR.NWN.API/NWScript'
$nwscriptFiles = if ([System.IO.Directory]::Exists($nwscriptFolder)) { [System.IO.Directory]::EnumerateFiles($nwscriptFolder, '*.cs', [System.IO.SearchOption]::TopDirectoryOnly) } else { @() }
foreach ($file in $nwscriptFiles) {
    foreach ($m in $nwscriptRegex.Matches($latin1.GetString([System.IO.File]::ReadAllBytes($file)))) { [void]$nwscriptFunctions.Add($m.Groups[1].Value) }
}
$clashingFacades = @($facadeNames | Where-Object { $nwscriptFunctions.Contains($_) })
$clashingFacadeRegex = $null
if ($clashingFacades.Count -gt 0) {
    $clashingFacadeRegex = New-Object System.Text.RegularExpressions.Regex(('(?<![\w.@"''])(?<q>' + (($clashingFacades | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')\.(?=[A-Za-z_])'), $regexOptions)
}

function Add-FacadeAccess {
    param([string]$Text, [bool]$Moved)
    if ($null -ne $hiddenFacadeRegex) { $Text = $hiddenFacadeRegex.Replace($Text, 'Service.${q}.') }
    if (-not $Moved) { return $Text }
    if ($null -ne $clashingFacadeRegex) { $Text = $clashingFacadeRegex.Replace($Text, 'Service.${q}.') }
    if ($Text.Contains($serviceUsing) -or -not $facadeUseRegex.IsMatch($Text)) { return $Text }
    $eol = if ($Text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $stop = $headerEndRegex.Match($Text)
    $limit = if ($stop.Success) { $stop.Index } else { 0 }
    $last = $null
    foreach ($m in $usingLineRegex.Matches($Text)) {
        if ($m.Index -ge $limit) { break }
        $last = $m
    }
    if ($null -ne $last) { return $Text.Insert($last.Index + $last.Length, $serviceUsing + $eol) }
    return $Text.Insert($limit, $serviceUsing + $eol + $eol)
}
# ConvertedServices.json keeps each "extras" list sorted by name; renamed facades sort elsewhere, so a list that is out of
# order is written again sorted (ordinal), in the file's own one-line shape.
$extrasLineRegex = New-Object System.Text.RegularExpressions.Regex('^(?<head>.*"extras"\s*:\s*\[\s*)(?<items>(?:\{\s*"(?:facade|part)"\s*:\s*"[^"]+"\s*\}\s*,?\s*)+)(?<tail>\].*)$', $regexOptions)
$extraItemRegex = New-Object System.Text.RegularExpressions.Regex('\{\s*"(?<kind>facade|part)"\s*:\s*"(?<name>[^"]+)"\s*\}', $regexOptions)
function Sort-ExtraLists {
    param([string]$Text)
    $lines = [regex]::Split($Text, '(?<=\n)')
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $body = $lines[$i].TrimEnd("`r", "`n")
        $m = $extrasLineRegex.Match($body)
        if (-not $m.Success) { continue }
        $items = @($extraItemRegex.Matches($m.Groups['items'].Value) | ForEach-Object { [pscustomobject]@{ Kind = $_.Groups['kind'].Value; Name = $_.Groups['name'].Value } })
        $keys = [string[]]@($items | ForEach-Object { $_.Name })
        $sorted = [object[]]@($items)
        [System.Array]::Sort($keys, $sorted, [System.StringComparer]::Ordinal)
        $same = $true
        for ($j = 0; $j -lt $items.Count; $j++) { if ($items[$j].Name -ne $sorted[$j].Name) { $same = $false } }
        if ($same) { continue }
        $rebuilt = ($sorted | ForEach-Object { '{ "' + $_.Kind + '": "' + $_.Name + '" }' }) -join ', '
        $lines[$i] = $m.Groups['head'].Value + $rebuilt + ' ' + $m.Groups['tail'].Value + $lines[$i].Substring($body.Length)
    }
    return ($lines -join '')
}
function Update-Text {
    param([string]$Path, [string]$Text, [bool]$MovedFromService = $false, [bool]$InDomain = $false)
    $script:currentFile = $Path
    $result = $Text
    if ($result.IndexOf('Service', [System.StringComparison]::Ordinal) -ge 0 -or $result.IndexOf('Domain', [System.StringComparison]::Ordinal) -ge 0 -or $result.IndexOf('Feature', [System.StringComparison]::Ordinal) -ge 0) {
        if ($facadeFiles -contains $Path) { $result = Sort-ExtraLists ($facadeRegex.Replace($result, $facadeEvaluator)) }
        $script:currentText = $result
        $result = $dottedRegex.Replace($result, $dottedEvaluator)
        $result = $pathRegex.Replace($result, $pathEvaluator)
        $result = $relativeRegex.Replace($result, $relativeEvaluator)
        if ($Path.EndsWith('.cs', [System.StringComparison]::Ordinal) -or $Path.EndsWith('.py', [System.StringComparison]::Ordinal)) {
            $result = $literalRegex.Replace($result, $literalEvaluator)
            foreach ($from in $renameLiterals.Keys) {
                $script:currentRename = $from
                $result = $renameLiterals[$from].Replace($result, $renameLiteralEvaluator)
            }        }
    }
    if ($InDomain) { $result = Add-FacadeAccess $result $MovedFromService }
    foreach ($edit in $textEdits) {
        if ([string]$edit.file -ne $Path) { continue }
        $find = [string]$edit.find
        $replace = [string]$edit.replace
        if ($result.Contains($find)) { $result = $result.Replace($find, $replace) }
        elseif (-not $result.Contains($replace)) { $script:warnings.Add("textEdit for ${Path}: neither the old nor the new text was found: $find") }
    }
    if ($result -ne $Text -and $Path.EndsWith('.cs', [System.StringComparison]::Ordinal)) { $result = Remove-DuplicateUsings $result }
    return $result
}

$changes = @{}          # old path -> new text
$handlerPairs = New-Object 'System.Collections.Generic.List[object]'
foreach ($path in $allFiles) {
    if (-not (Test-InTextScope $path)) { continue }
    $info = New-Object System.IO.FileInfo (Join-Rel $path)
    if ($info.Length -gt 8MB -or $info.Length -eq 0) { continue }
    $text = Read-Latin1 $path
    $new = $text
    if ($globFiles -contains $path) {
        # A line `Service/<N>Service/**` of a path list names the files of a moved folder. The list means exactly those
        # files (the Domain folder holds others that must keep their own rules), so each becomes a line of its own.
        $builder = New-Object System.Text.StringBuilder
        foreach ($line in [regex]::Split($new, '(?<=\n)')) {
            if ($line -match '^(Service/[A-Za-z0-9_]+Service(?:/[A-Za-z0-9_]+)*)/\*\*(\r?\n?)$') {
                $glob = $Matches[1]
                $eol = if ($Matches[2]) { $Matches[2] } else { "`n" }
                $folder = "$serverFolder/$glob/"
                if ((Get-MappedServerPath "$glob/x") -ne "$glob/x") {
                    $files = @($moveMap.Keys | Where-Object { $_.StartsWith($folder, [System.StringComparison]::Ordinal) -and $_.EndsWith('.cs', [System.StringComparison]::Ordinal) } |
                            ForEach-Object { $moveMap[$_].Substring($serverFolder.Length + 1) })
                    [System.Array]::Sort($files, [System.StringComparer]::Ordinal)
                    foreach ($file in $files) { [void]$builder.Append($file + $eol) }
                    continue
                }
            }
            [void]$builder.Append($line)
        }
        $new = $builder.ToString()
    }    $target = if ($moveMap.ContainsKey($path)) { $moveMap[$path] } else { $path }
    $isCs = $path.EndsWith(".cs", [System.StringComparison]::Ordinal)
    $new = Update-Text $path $new ($isCs -and $moveMap.ContainsKey($path) -and $path.StartsWith("$serverFolder/Service/", [System.StringComparison]::Ordinal)) ($isCs -and $target.StartsWith("$serverFolder/Domain/", [System.StringComparison]::Ordinal))
    if ($new -ne $text) {
        $changes[$path] = $new
        if ($path -eq $manifestPath) {
            $oldLines = $text -split "`n"
            $newLines = $new -split "`n"
            for ($i = 0; $i -lt $oldLines.Length; $i++) {
                if ($oldLines[$i] -ne $newLines[$i] -and $oldLines[$i] -match '^\s*(action|conditional)\s+(\S+)\s*$') {
                    $oldName = $Matches[2]
                    if ($newLines[$i] -match '^\s*(action|conditional)\s+(\S+)\s*$') { $handlerPairs.Add(@($oldName, $Matches[2])) }
                }
            }
        }
    }
}

# HandlerRenames.txt: `<master name> <branch name>`. A renamed handler keeps master's name on the left.
$handlerRenameCount = 0
if ($handlerPairs.Count -gt 0 -and ($allFiles -contains $handlerRenamesPath)) {
    $renameText = Read-Latin1 $handlerRenamesPath
    $eol = if ($renameText.Contains("`r`n")) { "`r`n" } else { "`n" }
    $renameLines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($line in ($renameText -split "`r?`n")) { $renameLines.Add($line) }
    if ($renameLines.Count -gt 0 -and $renameLines[$renameLines.Count - 1] -eq '') { $renameLines.RemoveAt($renameLines.Count - 1) }
    $seenPairs = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($pair in $handlerPairs) {
        if (-not $seenPairs.Add($pair[0] + ' ' + $pair[1])) { continue }
        $handlerRenameCount++
        $updated = $false
        for ($i = 0; $i -lt $renameLines.Count; $i++) {
            $parts = $renameLines[$i].Split(' ')
            if ($parts.Length -eq 2 -and -not $renameLines[$i].StartsWith('#') -and $parts[1] -eq $pair[0]) {
                $renameLines[$i] = $parts[0] + ' ' + $pair[1]
                $updated = $true
                break
            }
        }
        if (-not $updated) { $renameLines.Add($pair[0] + ' ' + $pair[1]) }
    }
    $changes[$handlerRenamesPath] = ($renameLines -join $eol) + $eol
}

# --- Summary ----------------------------------------------------------------------------------------------------------

$movedFiles = @($moveMap.Keys | Sort-Object)
$editedFiles = @($changes.Keys | Sort-Object)
Write-Host "Moves: $($script:oldNamespaces.Count) namespaces declared in files (Service.*Service and renamed), $($movedFiles.Count) files."
Write-Host "Rewrites: $($editedFiles.Count) files; $($script:counts.namespaceLines) namespace lines, $($script:counts.usingLines) using lines, $($script:counts.qualifiedNames) fully qualified names ($($script:counts.partialNames) written without SWLOR.Game.Server., $($script:counts.relativeNames) relative to Service), $($script:counts.renameNames) renamed namespace names, $($script:counts.paths) folder paths, $($script:counts.literals) path literals, $($script:counts.duplicateUsings) duplicate usings dropped, $handlerRenameCount handler renames."
foreach ($warning in $script:warnings) { Write-Host "Warning: $warning" }

if ($Check) {
    foreach ($path in $movedFiles) { Write-Host "  move  $path -> $($moveMap[$path])" }
    foreach ($path in $editedFiles) {
        if (-not $moveMap.ContainsKey($path)) { Write-Host "  edit  $path" }
    }
    if ($movedFiles.Count + $editedFiles.Count -gt 0) {
        Write-Host "Would change $($movedFiles.Count) moved and $($editedFiles.Count) edited files."
        exit 1
    }
    Write-Host 'Nothing to change.'
    exit 0
}

# --- Apply ------------------------------------------------------------------------------------------------------------

$touched = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($path in (@($movedFiles) + @($editedFiles | Where-Object { -not $moveMap.ContainsKey($_) }))) {
    $target = if ($moveMap.ContainsKey($path)) { $moveMap[$path] } else { $path }
    $targetFull = Join-Rel $target
    $directory = [System.IO.Path]::GetDirectoryName($targetFull)
    if (-not [System.IO.Directory]::Exists($directory)) { [void][System.IO.Directory]::CreateDirectory($directory) }
    if ($changes.ContainsKey($path)) {
        [System.IO.File]::WriteAllBytes($targetFull, $latin1.GetBytes($changes[$path]))
        if ($target -ne $path) { [System.IO.File]::Delete((Join-Rel $path)) }
    } else {
        [System.IO.File]::Move((Join-Rel $path), $targetFull)
    }
    if ($target -ne $path) { [void]$touched.Add($path) }
    [void]$touched.Add($target)
}

# Remove folders that the moves emptied.
$emptied = @($movedFiles | ForEach-Object { [System.IO.Path]::GetDirectoryName((Join-Rel $_)) } | Sort-Object -Unique)
foreach ($directory in $emptied) {
    $current = $directory
    while ($current -and $current.Length -gt $root.Length -and [System.IO.Directory]::Exists($current) -and
        -not (Get-ChildItem -LiteralPath $current -Force | Select-Object -First 1)) {
        [System.IO.Directory]::Delete($current)
        $current = [System.IO.Path]::GetDirectoryName($current)
    }
}

if (Test-Path -LiteralPath (Join-Path $root '.git')) {
    $list = @($touched | Sort-Object)
    for ($i = 0; $i -lt $list.Count; $i += 150) {
        $chunk = $list[$i..([Math]::Min($i + 149, $list.Count - 1))]
        [void](Invoke-GitText (@('add', '-A', '--') + $chunk))
    }
}
Write-Host "Applied: moved $($movedFiles.Count) files, rewrote $($editedFiles.Count) files."
exit 0
