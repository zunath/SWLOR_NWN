param(
    [string]$AssemblyDirectory = "SWLOR.Game.Server\bin\Debug\net10.0",
    [string]$Feat2daPath = "SWLOR_Haks\sw_2da\feat.2da",
    # Every player class uses this table; its OnMenu cell lists a known feat on the radial and quickbar class menus.
    [string]$ClassFeat2daPath = "SWLOR_Haks\sw_2da\CLS_FEAT_FIGHT.2da",
    [string]$FeatEnumPath = "SWLOR.NWN.API\NWScript\Enum\FeatType.cs",
    # Optional output of the same compiled-definition exporter, for reproducible offline generation.
    [string]$EligibilityJson = "",
    [switch]$CheckOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Export-EligibleAbilities {
    param([string]$Directory)

    $assemblyRoot = (Resolve-Path -LiteralPath $Directory).Path
    $serverDll = Join-Path $assemblyRoot "SWLOR.Game.Server.dll"
    $apiDll = Join-Path $assemblyRoot "SWLOR.NWN.API.dll"
    foreach ($required in @($serverDll, $apiDll)) {
        if (!(Test-Path -LiteralPath $required -PathType Leaf)) {
            throw "Missing compiled assembly: $required. Build the server with -p:RunPostBuildEvent=Never first."
        }
    }

    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $exportDirectory = Join-Path $temporaryRoot ("swlor-automatic-targeting-" + [Guid]::NewGuid().ToString("N"))
    [IO.Directory]::CreateDirectory($exportDirectory) | Out-Null
    try {
        $projectPath = Join-Path $exportDirectory "Export.csproj"
        $outputPath = Join-Path $exportDirectory "eligible.json"
        $escapedServerDll = [Security.SecurityElement]::Escape($serverDll)
        $escapedApiDll = [Security.SecurityElement]::Escape($apiDll)
        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup>
    <Reference Include="SWLOR.Game.Server"><HintPath>$escapedServerDll</HintPath></Reference>
    <Reference Include="SWLOR.NWN.API"><HintPath>$escapedApiDll</HintPath></Reference>
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $projectPath -Encoding utf8
        @'
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

var directory = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) => {
    var path = Path.Combine(directory, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
var abilities = new Dictionary<FeatType, AbilityDetail>();
foreach (var type in typeof(IAbilityListDefinition).Assembly.GetTypes()
             .Where(type => !type.IsAbstract && typeof(IAbilityListDefinition).IsAssignableFrom(type))
             .OrderBy(type => type.FullName))
    foreach (var entry in ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
        abilities.Add(entry.Key, entry.Value);
var eligible = abilities.OrderBy(entry => (int)entry.Key).Select(entry => new {
    Id = (int)entry.Key,
    Enum = entry.Key.ToString(),
    Self = QuickCastPolicy.IsEligible(entry.Value, QuickCastTargetType.Self),
    Enemy = QuickCastPolicy.IsEligible(entry.Value, QuickCastTargetType.CurrentEnemy)
}).Where(entry => entry.Self || entry.Enemy).ToArray();
File.WriteAllText(args[1], JsonSerializer.Serialize(eligible));
'@ | Set-Content -LiteralPath (Join-Path $exportDirectory "Program.cs") -Encoding utf8

        # A shared compiler server keeps the copied references open, which blocks the cleanup below.
        & dotnet run --project $projectPath -p:RunPostBuildEvent=Never -p:UseSharedCompilation=false -- $assemblyRoot $outputPath | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "Compiled ability discovery failed with exit code $LASTEXITCODE."
        }
        return @(Get-Content -LiteralPath $outputPath -Raw | ConvertFrom-Json)
    }
    finally {
        $cleanupPath = [IO.Path]::GetFullPath($exportDirectory)
        $expectedPrefix = $temporaryRoot + [IO.Path]::DirectorySeparatorChar + "swlor-automatic-targeting-"
        if (!$cleanupPath.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing cleanup outside the generated exporter directory: $cleanupPath"
        }
        Remove-Item -LiteralPath $cleanupPath -Recurse -Force
    }
}

$eligible = if ([string]::IsNullOrWhiteSpace($EligibilityJson)) {
    @(Export-EligibleAbilities $AssemblyDirectory)
} else {
    @(Get-Content -LiteralPath $EligibilityJson -Raw | ConvertFrom-Json)
}
if ($eligible.Count -eq 0) { throw "No eligible compiled abilities were found." }

$featPath = (Resolve-Path -LiteralPath $Feat2daPath).Path
$enumPath = (Resolve-Path -LiteralPath $FeatEnumPath).Path
$featBytes = [IO.File]::ReadAllBytes($featPath)
$featText = [Text.Encoding]::UTF8.GetString($featBytes)
$enumText = [IO.File]::ReadAllText($enumPath)
$newline = if ($featText.Contains("`r`n")) { "`r`n" } else { "`n" }
$enumNewline = if ($enumText.Contains("`r`n")) { "`r`n" } else { "`n" }
$columns = ($featText -split "\r?\n")[2].Trim() -split "\s+"
$columnIndex = @{}
for ($index = 0; $index -lt $columns.Count; $index++) { $columnIndex[$columns[$index]] = $index }
foreach ($required in @("LABEL", "TARGETSELF", "HostileFeat", "SPELLID")) {
    if (!$columnIndex.ContainsKey($required)) { throw "feat.2da is missing column $required." }
}

$rows = @{}
$aliases = @{}
foreach ($line in ($featText -split "\r?\n")) {
    $tokens = $line.Trim() -split "\s+"
    $id = 0
    if (![int]::TryParse($tokens[0], [ref]$id)) { continue }
    if ($tokens.Count -ne $columns.Count + 1) { throw "Malformed feat row $id." }
    if ($rows.ContainsKey($id)) { throw "Duplicate feat row $id." }
    $cells = [string[]]$tokens[1..($tokens.Count - 1)]
    $rows[$id] = $cells
    $label = $cells[$columnIndex["LABEL"]]
    if ($label.StartsWith("AutoCast", [StringComparison]::Ordinal)) {
        if ($aliases.ContainsKey($label)) { throw "Duplicate automatic targeting alias $label." }
        $aliases[$label] = $id
    }
}

$classFeatPath = (Resolve-Path -LiteralPath $ClassFeat2daPath).Path
$classFeatBytes = [IO.File]::ReadAllBytes($classFeatPath)
$classFeatText = [Text.Encoding]::UTF8.GetString($classFeatBytes)
$classFeatNewline = if ($classFeatText.Contains("`r`n")) { "`r`n" } else { "`n" }
$classFeatColumns = ($classFeatText -split "\r?\n")[2].Trim() -split "\s+"
if (($classFeatColumns -join ",") -ne "FeatLabel,FeatIndex,List,GrantedOnLevel,OnMenu") {
    throw "Unexpected class feat table columns: $($classFeatColumns -join ', ')."
}
$classFeatRowsByFeat = @{}
$nextClassFeatRow = 0
foreach ($line in ($classFeatText -split "\r?\n" | Select-Object -Skip 3)) {
    $tokens = $line.Trim() -split "\s+"
    $row = 0
    if (![int]::TryParse($tokens[0], [ref]$row)) { continue }
    if ($tokens.Count -ne $classFeatColumns.Count + 1) { throw "Malformed class feat row $row." }
    if ($row -ne $nextClassFeatRow) { throw "Class feat row $row is out of sequence; expected $nextClassFeatRow." }
    $nextClassFeatRow++
    $featIndex = 0
    if (![int]::TryParse($tokens[2], [ref]$featIndex)) { continue }
    if (!$classFeatRowsByFeat.ContainsKey($featIndex)) { $classFeatRowsByFeat[$featIndex] = [Collections.Generic.List[object]]::new() }
    $classFeatRowsByFeat[$featIndex].Add([string[]]$tokens)
}
$newClassFeatRows = [Collections.Generic.List[string]]::new()

$enumValues = @{}
$enumIds = @{}
foreach ($match in [regex]::Matches($enumText, '(?m)^\s*(?<name>\w+)\s*=\s*(?<id>-?\d+)\s*,')) {
    $name = $match.Groups["name"].Value
    $id = [int]$match.Groups["id"].Value
    $enumValues[$name] = $id
    $enumIds[$id] = $name
}
$nextId = [Math]::Max(($rows.Keys | Measure-Object -Maximum).Maximum, ($enumIds.Keys | Measure-Object -Maximum).Maximum) + 1
$newRows = [Collections.Generic.List[string]]::new()
$newEnums = [Collections.Generic.List[string]]::new()
$seenBaseIds = [Collections.Generic.HashSet[int]]::new()
foreach ($ability in ($eligible | Sort-Object Id)) {
    $baseId = [int]$ability.Id
    $baseName = [string]$ability.Enum
    if (!$seenBaseIds.Add($baseId)) { throw "Duplicate eligible feat $baseId." }
    if ($baseName -notmatch '^[A-Za-z_][A-Za-z0-9_]*$' -or $baseName.StartsWith("AutoCast")) {
        throw "Invalid canonical feat enum name $baseName."
    }
    if (($ability.Self -isnot [bool]) -or ($ability.Enemy -isnot [bool]) -or ($ability.Self -eq $ability.Enemy)) {
        throw "Feat $baseName must be eligible for exactly one automatic targeting mode."
    }
    if (!$rows.ContainsKey($baseId) -or !$enumValues.ContainsKey($baseName) -or $enumValues[$baseName] -ne $baseId) {
        throw "Canonical feat $baseName/$baseId does not match feat.2da and FeatType."
    }
    $aliasName = "AutoCast" + $baseName
    $expected = [string[]]$rows[$baseId].Clone()
    $expected[$columnIndex["LABEL"]] = $aliasName
    $expected[$columnIndex["TARGETSELF"]] = "1"
    $expected[$columnIndex["HostileFeat"]] = "****"
    if ($aliases.ContainsKey($aliasName)) {
        $aliasId = [int]$aliases[$aliasName]
        $actual = $rows[$aliasId]
        for ($index = 0; $index -lt $columns.Count; $index++) {
            if ($actual[$index] -ne $expected[$index]) {
                throw "Existing alias $aliasName/$aliasId differs from its canonical row at $($columns[$index]); existing rows are never rewritten."
            }
        }
    } else {
        if ($enumValues.ContainsKey($aliasName)) {
            throw "Enum $aliasName exists without its feat.2da row."
        }
        $aliasId = [int]$nextId
        if ($aliasId -gt [ushort]::MaxValue) { throw "Feat IDs exceed the native feat ID range." }
        $nextId++
        $newRows.Add("$aliasId   " + ($expected -join "   "))
        $rows[$aliasId] = $expected
        $aliases[$aliasName] = $aliasId
    }
    # The client lists class abilities from the class feat table, so an alias without its own row
    # disappears from the radial and quickbar menus while it replaces the canonical feat.
    # Feats players never learn, such as beast abilities, have no row and their aliases get none.
    $canonicalClassRows = [object[]]@()
    if ($classFeatRowsByFeat.ContainsKey($baseId)) { $canonicalClassRows = $classFeatRowsByFeat[$baseId].ToArray() }
    $aliasClassRows = [object[]]@()
    if ($classFeatRowsByFeat.ContainsKey($aliasId)) { $aliasClassRows = $classFeatRowsByFeat[$aliasId].ToArray() }
    if ($canonicalClassRows.Count -gt 1) { throw "Canonical feat $baseName/$baseId has more than one class feat row." }
    if ($aliasClassRows.Count -gt $canonicalClassRows.Count) {
        throw "Alias $aliasName/$aliasId has $($aliasClassRows.Count) class feat rows but $baseName has $($canonicalClassRows.Count)."
    }
    if ($canonicalClassRows.Count -eq 1) {
        $expectedClassCells = [string[]](@($aliasName, [string]$aliasId) + $canonicalClassRows[0][3..5])
        if ($aliasClassRows.Count -eq 1) {
            if (($aliasClassRows[0][1..5] -join " ") -ne ($expectedClassCells -join " ")) {
                throw "Existing class feat row $($aliasClassRows[0][0]) for $aliasName differs from its canonical row; existing rows are never rewritten."
            }
        } else {
            $newClassFeatRows.Add("$nextClassFeatRow   " + ($expectedClassCells -join "   "))
            $nextClassFeatRow++
        }
    }
    if ($enumValues.ContainsKey($aliasName)) {
        if ($enumValues[$aliasName] -ne $aliasId) { throw "Enum ID mismatch for $aliasName." }
    } else {
        if ($enumIds.ContainsKey($aliasId)) { throw "Feat enum ID $aliasId is already used by $($enumIds[$aliasId])." }
        $newEnums.Add("        $aliasName = $aliasId,")
        $enumValues[$aliasName] = $aliasId
        $enumIds[$aliasId] = $aliasName
    }
}

function Add-TableRows {
    param([string]$Path, [byte[]]$OriginalBytes, [string]$OriginalText, [string]$Newline, [string[]]$Rows)

    $separator = if ($OriginalText.EndsWith("`n")) { "" } else { $Newline }
    $appendBytes = [Text.Encoding]::UTF8.GetBytes($separator + ($Rows -join $Newline) + $Newline)
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Append, [IO.FileAccess]::Write)
    try { $stream.Write($appendBytes, 0, $appendBytes.Length) } finally { $stream.Dispose() }
    $afterBytes = [IO.File]::ReadAllBytes($Path)
    for ($index = 0; $index -lt $OriginalBytes.Length; $index++) {
        if ($afterBytes[$index] -ne $OriginalBytes[$index]) { throw "Original $([IO.Path]::GetFileName($Path)) bytes changed at offset $index." }
    }
}

if ($newRows.Count -gt 0 -or $newEnums.Count -gt 0 -or $newClassFeatRows.Count -gt 0) {
    if ($CheckOnly) { throw "Automatic targeting assets need $($newRows.Count) feat rows, $($newClassFeatRows.Count) class feat rows and $($newEnums.Count) enum entries. Run the generator without -CheckOnly." }
    $enumClosing = [regex]::Match($enumText, '(?m)^\s*}\s*\r?\n\s*}\s*\z')
    if (!$enumClosing.Success) { throw "Could not locate the FeatType enum closing braces." }
    if ($newEnums.Count -gt 0) {
        $updatedEnum = $enumText.Insert($enumClosing.Index, ($newEnums -join $enumNewline) + $enumNewline)
        [IO.File]::WriteAllText($enumPath, $updatedEnum, [Text.UTF8Encoding]::new($false))
    }
    if ($newRows.Count -gt 0) {
        Add-TableRows $featPath $featBytes $featText $newline $newRows
    }
    if ($newClassFeatRows.Count -gt 0) {
        Add-TableRows $classFeatPath $classFeatBytes $classFeatText $classFeatNewline $newClassFeatRows
    }
}
$selfCount = @($eligible | Where-Object Self).Count
$enemyCount = @($eligible | Where-Object Enemy).Count
Write-Host "Validated $($eligible.Count) eligible feats ($selfCount self, $enemyCount current enemy); appended $($newRows.Count) feat rows, $($newClassFeatRows.Count) class feat rows and $($newEnums.Count) enum entries."
