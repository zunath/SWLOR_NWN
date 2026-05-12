[CmdletBinding()]
param(
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\CombatUpgradeBiblePerkManifest.csv",
    [string]$PerkDefinitionPath = "SWLOR.Game.Server\Feature\PerkDefinition",
    [string]$Feat2daPath = "SWLOR_Haks\swlor2_2da\feat.2da",
    [string]$Spells2daPath = "SWLOR_Haks\swlor2_2da\spells.2da",
    [string]$TlkJsonPath = "SWLOR_Haks\swlor2_tlk\swlor2_tlk.tlk.json",
    [string]$TlkPath = "SWLOR_Haks\swlor2_tlk\swlor2_tlk.tlk",
    [string]$TlkToolPath = "SWLOR_Haks\nwn_tlk.exe",
    [int]$GeneratedFeatStart = 2000,
    [int]$GeneratedFeatEnd = 2299,
    [int[]]$ExcludedGeneratedFeatIds = @(2295)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$CustomTlkOffset = 16777216

$FeatColumnWidths = @(
    7, 49, 11, 14, 19, 17, 9, 9, 9, 9, 9, 9, 13, 14, 14, 15, 15, 19,
    11, 8, 10, 12, 10, 13, 13, 13, 13, 13, 13, 13, 11, 18, 12, 20, 49,
    18, 14, 11, 16, 11, 13, 13, 12
)

$SpellColumnWidths = @(
    7, 36, 11, 19, 9, 8, 7, 12, 13, 19, 7, 9, 8, 10, 9, 11, 9, 11,
    18, 18, 18, 19, 19, 19, 11, 11, 17, 17, 17, 19, 7, 19, 15, 15,
    19, 18, 17, 14, 14, 14, 14, 14, 14, 11, 9, 11, 12, 19, 20, 13,
    17, 12, 11, 11, 19, 13, 13, 13, 12
)

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path (Split-Path -Parent $PSScriptRoot) $Path
}

function Get-HeaderLineIndex {
    param([string[]]$Lines)

    for ($i = 1; $i -lt $Lines.Count; $i++) {
        if (![string]::IsNullOrWhiteSpace($Lines[$i])) {
            return $i
        }
    }

    throw "Could not locate 2DA header line."
}

function Convert-ToStringList {
    param([string]$Line)

    $list = [System.Collections.Generic.List[string]]::new()
    $list.AddRange([string[]]($Line.Trim() -split "\s+"))

    return ,$list
}

function Get-RowNumber {
    param([System.Collections.Generic.IList[string]]$Tokens)

    $rowNumber = 0
    if ($Tokens.Count -eq 0 -or ![int]::TryParse($Tokens[0], [ref]$rowNumber)) {
        return $null
    }

    return $rowNumber
}

function Get-TokenByHeader {
    param(
        [System.Collections.Generic.IList[string]]$Tokens,
        [string[]]$Headers,
        [string]$Header
    )

    $headerIndex = [array]::IndexOf($Headers, $Header)
    if ($headerIndex -lt 0) {
        throw "Could not find column '$Header'."
    }

    return $Tokens[$headerIndex + 1]
}

function Set-TokenByHeader {
    param(
        [System.Collections.Generic.IList[string]]$Tokens,
        [string[]]$Headers,
        [string]$Header,
        [string]$Value
    )

    $headerIndex = [array]::IndexOf($Headers, $Header)
    if ($headerIndex -lt 0) {
        throw "Could not find column '$Header'."
    }

    $Tokens[$headerIndex + 1] = $Value
}

function Format-2DARow {
    param(
        [string[]]$Tokens,
        [int[]]$Widths
    )

    $parts = for ($i = 0; $i -lt $Tokens.Count; $i++) {
        $width = if ($i -lt $Widths.Count) { $Widths[$i] } else { 8 }
        $Tokens[$i].PadRight($width)
    }

    return ($parts -join "").TrimEnd()
}

function Convert-PerkNameToFeatLabel {
    param([string]$Name)

    $romanRanks = @{
        I = "1"
        II = "2"
        III = "3"
        IV = "4"
        V = "5"
    }

    $workingName = $Name.Trim()
    $rank = $null
    if ($workingName -match "\s+(I|II|III|IV|V)$") {
        $rank = $romanRanks[$Matches[1]]
        $workingName = $workingName.Substring(0, $workingName.Length - $Matches[0].Length)
    }

    $label = [regex]::Replace($workingName, "[^A-Za-z0-9]", "")
    if ($rank) {
        return "$label$rank"
    }

    return "$label`1"
}

function Convert-CSharpStringLiteral {
    param([string]$Value)

    return $Value.
        Replace('\"', '"').
        Replace('\\', '\').
        Replace('\n', "`n").
        Replace('\r', "`r").
        Replace('\t', "`t")
}

function Add-LevelInfo {
    param(
        [hashtable]$Map,
        [string]$Name,
        [string]$Description,
        [System.Collections.Generic.List[string]]$Feats
    )

    if ([string]::IsNullOrWhiteSpace($Name) -or [string]::IsNullOrWhiteSpace($Description)) {
        return
    }

    foreach ($feat in $Feats) {
        $Map[$feat.ToLowerInvariant()] = [pscustomobject]@{
            Name = $Name
            Description = $Description
        }
    }
}

function Get-PerkDefinitionFeatInfo {
    param([string]$DirectoryPath)

    $map = @{}

    foreach ($file in Get-ChildItem $DirectoryPath -Filter "*.cs") {
        $currentName = $null
        $currentDescription = $null
        $currentFeats = [System.Collections.Generic.List[string]]::new()
        $inLevel = $false

        foreach ($line in [System.IO.File]::ReadAllLines($file.FullName)) {
            if ($line -match "\.Create\(") {
                Add-LevelInfo $map $currentName $currentDescription $currentFeats
                $currentDescription = $null
                $currentFeats = [System.Collections.Generic.List[string]]::new()
                $inLevel = $false
            }

            if ($line -match '\.Name\("((?:[^"\\]|\\.)*)"\)') {
                $currentName = Convert-CSharpStringLiteral $Matches[1]
            }

            if ($line -match "\.AddPerkLevel\(") {
                Add-LevelInfo $map $currentName $currentDescription $currentFeats
                $currentDescription = $null
                $currentFeats = [System.Collections.Generic.List[string]]::new()
                $inLevel = $true
            }

            if ($line -match "\.GrantsFeat\(FeatType\.([A-Za-z0-9_]+)\)") {
                $currentFeats.Add($Matches[1]) | Out-Null
            }

            if ($inLevel -and $line -match '\.Description\("((?:[^"\\]|\\.)*)"\)') {
                $currentDescription = Convert-CSharpStringLiteral $Matches[1]
            }
        }

        Add-LevelInfo $map $currentName $currentDescription $currentFeats
    }

    return $map
}

function Get-ManifestFeatInfo {
    param([string]$Path)

    $map = @{}

    foreach ($row in Import-Csv $Path) {
        if ([string]::IsNullOrWhiteSpace($row.PerkName) -or
            [string]::IsNullOrWhiteSpace($row.Description) -or
            $row.PerkName -eq "PerkName") {
            continue
        }

        $label = Convert-PerkNameToFeatLabel $row.PerkName
        $map[$label.ToLowerInvariant()] = [pscustomobject]@{
            Name = $row.PerkName
            Description = $row.Description
        }
    }

    return $map
}

function Get-GeneratedFeatLabels {
    param(
        [string]$Path,
        [int]$Start,
        [int]$End,
        [int[]]$ExcludedRows
    )

    $labels = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $lines = [System.IO.File]::ReadAllLines($Path)

    foreach ($line in $lines) {
        $tokens = Convert-ToStringList $line
        $rowNumber = Get-RowNumber $tokens
        if ($null -eq $rowNumber -or
            $rowNumber -lt $Start -or
            $rowNumber -gt $End -or
            $ExcludedRows -contains $rowNumber) {
            continue
        }

        if ($tokens.Count -gt 1 -and $tokens[1] -ne "****") {
            $labels.Add($tokens[1]) | Out-Null
        }
    }

    return $labels
}

function Add-TlkEntry {
    param(
        [hashtable]$TextToId,
        [System.Collections.Generic.HashSet[int]]$UsedIds,
        [System.Collections.Generic.Queue[int]]$OpenSlots,
        [hashtable]$ExistingBlankEntries,
        [System.Collections.Generic.List[object]]$NewEntries,
        [System.Collections.Generic.List[object]]$FilledBlankEntries,
        [ref]$NextId,
        [string]$Text
    )

    if ($TextToId.ContainsKey($Text)) {
        return [int]$TextToId[$Text]
    }

    if ($OpenSlots.Count -gt 0) {
        $id = $OpenSlots.Dequeue()
    }
    else {
        $id = $NextId.Value
        while ($UsedIds.Contains($id)) {
            $id++
        }

        $NextId.Value = $id + 1
    }

    $entry = [ordered]@{
        id = $id
        text = $Text
    }
    if ($ExistingBlankEntries.ContainsKey($id)) {
        $FilledBlankEntries.Add($entry) | Out-Null
    }
    else {
        $NewEntries.Add($entry) | Out-Null
    }

    $UsedIds.Add($id) | Out-Null
    $TextToId[$Text] = $id

    return $id
}

function ConvertTo-JsonStringLiteral {
    param([string]$Text)

    return ($Text | ConvertTo-Json -Compress)
}

function Invoke-TlkTool {
    param(
        [string]$ToolPath,
        [string[]]$Arguments
    )

    & $ToolPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "nwn_tlk failed with exit code $LASTEXITCODE."
    }
}

function Get-RawTlkOpenSlots {
    param(
        [string]$ToolPath,
        [string]$TlkPath
    )

    $scratchPath = Join-Path (Split-Path -Parent $PSScriptRoot) "Build\tlk_sync"
    New-Item -ItemType Directory -Force -Path $scratchPath | Out-Null

    $reviewPath = Join-Path $scratchPath "swlor2_tlk.review"
    $debugPath = Join-Path $scratchPath "swlor2_tlk.debug"

    Invoke-TlkTool $ToolPath @(
        "-i", $TlkPath,
        "-l", "tlk",
        "-o", $reviewPath,
        "-k", "review",
        "--review-with-text"
    )
    Invoke-TlkTool $ToolPath @(
        "-i", $TlkPath,
        "-l", "tlk",
        "-o", $debugPath,
        "-k", "debug"
    )

    $stringCount = $null
    foreach ($line in [System.IO.File]::ReadLines($debugPath)) {
        if ($line -match "StringCount\s+(\d+)") {
            $stringCount = [int]$Matches[1]
            break
        }
    }
    if ($null -eq $stringCount) {
        throw "Could not determine StringCount from raw TLK debug output '$debugPath'."
    }

    $rawUsedIds = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($line in [System.IO.File]::ReadLines($reviewPath)) {
        if ($line -match "^(\d+)\s+") {
            $rawUsedIds.Add([int]$Matches[1]) | Out-Null
        }
    }

    $openIds = [System.Collections.Generic.List[int]]::new()
    for ($id = 1; $id -lt $stringCount; $id++) {
        if (!$rawUsedIds.Contains($id)) {
            $openIds.Add($id) | Out-Null
        }
    }

    return [pscustomobject]@{
        StringCount = $stringCount
        UsedCount = $rawUsedIds.Count
        OpenIds = $openIds
        ReviewPath = $reviewPath
        DebugPath = $debugPath
    }
}

function Update-TlkJsonEntries {
    param(
        [string]$Path,
        [System.Collections.Generic.List[object]]$FilledBlankEntries,
        [System.Collections.Generic.List[object]]$NewEntries
    )

    if ($FilledBlankEntries.Count -eq 0 -and $NewEntries.Count -eq 0) {
        return
    }

    $raw = [System.IO.File]::ReadAllText($Path)
    $newline = if ($raw.Contains("`r`n")) { "`r`n" } else { "`n" }

    foreach ($entry in $FilledBlankEntries) {
        $replacementText = ConvertTo-JsonStringLiteral $entry.text
        $pattern = "(?s)(\{\s*`"id`":\s*$($entry.id),\s*`"text`":\s*)`"`"(\s*\})"
        $updated = [regex]::Replace(
            $raw,
            $pattern,
            "`${1}$replacementText`${2}",
            [System.Text.RegularExpressions.RegexOptions]::None,
            [TimeSpan]::FromSeconds(5))

        if ($updated -eq $raw) {
            throw "Could not locate blank TLK JSON entry $($entry.id) in '$Path'."
        }

        $raw = $updated
    }

    if ($NewEntries.Count -gt 0) {
        $entryBlocks = foreach ($entry in ($NewEntries | Sort-Object { [int]$_.id })) {
            @(
                "    {",
                "      `"id`": $($entry.id),",
                "      `"text`": $(ConvertTo-JsonStringLiteral $entry.text)",
                "    }"
            ) -join $newline
        }

        $insertText = "," + $newline + (($entryBlocks -join ("," + $newline)))
        $closingPattern = [regex]::Escape($newline) + "  \]" + [regex]::Escape($newline) + "\}\s*$"
        if ($raw -notmatch $closingPattern) {
            throw "Could not locate TLK JSON entries closing block in '$Path'."
        }

        $raw = [regex]::Replace(
            $raw,
            $closingPattern,
            "$insertText$newline  ]$newline}",
            [System.Text.RegularExpressions.RegexOptions]::None,
            [TimeSpan]::FromSeconds(5))
    }

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $raw, $utf8NoBom)
}

$manifestPath = Resolve-RepoPath $ManifestPath
$perkDefinitionPath = Resolve-RepoPath $PerkDefinitionPath
$feat2daPath = Resolve-RepoPath $Feat2daPath
$spells2daPath = Resolve-RepoPath $Spells2daPath
$tlkJsonPath = Resolve-RepoPath $TlkJsonPath
$tlkPath = Resolve-RepoPath $TlkPath
$tlkToolPath = Resolve-RepoPath $TlkToolPath

$manifestInfo = Get-ManifestFeatInfo $manifestPath
$perkInfo = Get-PerkDefinitionFeatInfo $perkDefinitionPath
$generatedLabels = Get-GeneratedFeatLabels $feat2daPath $GeneratedFeatStart $GeneratedFeatEnd $ExcludedGeneratedFeatIds
$rawTlkInfo = Get-RawTlkOpenSlots $tlkToolPath $tlkPath

$featInfo = @{}
foreach ($label in ($generatedLabels | Sort-Object)) {
    $key = $label.ToLowerInvariant()

    if (!$manifestInfo.ContainsKey($key)) {
        throw "No manifest row found for generated feat label '$label'."
    }

    $description = if ($perkInfo.ContainsKey($key)) {
        $perkInfo[$key].Description
    }
    else {
        $manifestInfo[$key].Description
    }

    $featInfo[$key] = [pscustomobject]@{
        Name = $manifestInfo[$key].Name
        Description = $description
    }
}

$tlk = Get-Content $tlkJsonPath -Raw | ConvertFrom-Json
$entries = [System.Collections.Generic.List[object]]::new()
$entries.AddRange([object[]]$tlk.entries)
$newEntries = [System.Collections.Generic.List[object]]::new()
$filledBlankEntries = [System.Collections.Generic.List[object]]::new()

$textToId = @{}
$usedIds = [System.Collections.Generic.HashSet[int]]::new()
$existingBlankEntries = @{}
$maxId = 0
foreach ($entry in $entries) {
    $entryId = [int]$entry.id
    $usedIds.Add($entryId) | Out-Null
    if ($entryId -gt $maxId) {
        $maxId = $entryId
    }

    if ($entry.text -eq "") {
        $existingBlankEntries[$entryId] = $true
        continue
    }

    if (!$textToId.ContainsKey($entry.text)) {
        $textToId[$entry.text] = $entryId
    }
}

$openSlots = [System.Collections.Generic.Queue[int]]::new()
$queuedOpenSlots = [System.Collections.Generic.HashSet[int]]::new()
foreach ($id in ($existingBlankEntries.Keys | Sort-Object { [int]$_ })) {
    $openId = [int]$id
    $openSlots.Enqueue($openId)
    $queuedOpenSlots.Add($openId) | Out-Null
}
foreach ($id in $rawTlkInfo.OpenIds) {
    if (!$usedIds.Contains($id) -and !$queuedOpenSlots.Contains($id)) {
        $openSlots.Enqueue($id)
        $queuedOpenSlots.Add($id) | Out-Null
    }
}
for ($id = 1; $id -lt $maxId; $id++) {
    if (!$usedIds.Contains($id) -and !$queuedOpenSlots.Contains($id)) {
        $openSlots.Enqueue($id)
        $queuedOpenSlots.Add($id) | Out-Null
    }
}

$nextId = [ref]($maxId + 1)
$strRefsByLabel = @{}
foreach ($entry in ($featInfo.GetEnumerator() | Sort-Object Name)) {
    $nameId = Add-TlkEntry `
        $textToId `
        $usedIds `
        $openSlots `
        $existingBlankEntries `
        $newEntries `
        $filledBlankEntries `
        $nextId `
        $entry.Value.Name
    $descriptionId = Add-TlkEntry `
        $textToId `
        $usedIds `
        $openSlots `
        $existingBlankEntries `
        $newEntries `
        $filledBlankEntries `
        $nextId `
        $entry.Value.Description

    $strRefsByLabel[$entry.Key] = [pscustomobject]@{
        Name = ($CustomTlkOffset + $nameId).ToString()
        Description = ($CustomTlkOffset + $descriptionId).ToString()
    }
}

$intuitivePilotingDescriptionId = Add-TlkEntry `
    $textToId `
    $usedIds `
    $openSlots `
    $existingBlankEntries `
    $newEntries `
    $filledBlankEntries `
    $nextId `
    "Allows for willpower to be used in place of perception when piloting."
$intuitivePilotingDescriptionStrRef = ($CustomTlkOffset + $intuitivePilotingDescriptionId).ToString()

Update-TlkJsonEntries $tlkJsonPath $filledBlankEntries $newEntries

$featLines = [System.Collections.Generic.List[string]]::new()
$featLines.AddRange([System.IO.File]::ReadAllLines($feat2daPath))
$featHeaderIndex = Get-HeaderLineIndex $featLines.ToArray()
$featHeaders = $featLines[$featHeaderIndex].Trim() -split "\s+"
$updatedFeatRows = 0

for ($i = $featHeaderIndex + 1; $i -lt $featLines.Count; $i++) {
    $tokens = Convert-ToStringList $featLines[$i]
    $rowNumber = Get-RowNumber $tokens
    if ($null -eq $rowNumber) {
        continue
    }

    $label = Get-TokenByHeader $tokens $featHeaders "LABEL"
    if ($rowNumber -ge $GeneratedFeatStart -and
        $rowNumber -le $GeneratedFeatEnd -and
        $ExcludedGeneratedFeatIds -notcontains $rowNumber -and
        $label -ne "****") {
        $key = $label.ToLowerInvariant()
        if (!$strRefsByLabel.ContainsKey($key)) {
            throw "No TLK string refs found for generated feat label '$label'."
        }

        Set-TokenByHeader $tokens $featHeaders "FEAT" $strRefsByLabel[$key].Name
        Set-TokenByHeader $tokens $featHeaders "DESCRIPTION" $strRefsByLabel[$key].Description
        $featLines[$i] = Format-2DARow $tokens.ToArray() $FeatColumnWidths
        $updatedFeatRows++
    }
    elseif ($label -eq "IntuitivePiloting") {
        Set-TokenByHeader $tokens $featHeaders "DESCRIPTION" $intuitivePilotingDescriptionStrRef
        $featLines[$i] = Format-2DARow $tokens.ToArray() $FeatColumnWidths
        $updatedFeatRows++
    }
}

[System.IO.File]::WriteAllLines($feat2daPath, $featLines)

$spellsLines = [System.Collections.Generic.List[string]]::new()
$spellsLines.AddRange([System.IO.File]::ReadAllLines($spells2daPath))
$spellsHeaderIndex = Get-HeaderLineIndex $spellsLines.ToArray()
$spellsHeaders = $spellsLines[$spellsHeaderIndex].Trim() -split "\s+"
$updatedSpellRows = 0

for ($i = $spellsHeaderIndex + 1; $i -lt $spellsLines.Count; $i++) {
    $tokens = Convert-ToStringList $spellsLines[$i]
    $rowNumber = Get-RowNumber $tokens
    if ($null -eq $rowNumber) {
        continue
    }

    $featId = Get-TokenByHeader $tokens $spellsHeaders "FeatID"
    $featIdNumber = 0
    if (![int]::TryParse($featId, [ref]$featIdNumber) -or
        $featIdNumber -lt $GeneratedFeatStart -or
        $featIdNumber -gt $GeneratedFeatEnd -or
        $ExcludedGeneratedFeatIds -contains $featIdNumber) {
        continue
    }

    $label = Get-TokenByHeader $tokens $spellsHeaders "Label"
    $key = $label.ToLowerInvariant()
    if (!$strRefsByLabel.ContainsKey($key)) {
        throw "No TLK string refs found for spell label '$label'."
    }

    Set-TokenByHeader $tokens $spellsHeaders "Name" $strRefsByLabel[$key].Name
    Set-TokenByHeader $tokens $spellsHeaders "SpellDesc" $strRefsByLabel[$key].Description
    $spellsLines[$i] = Format-2DARow $tokens.ToArray() $SpellColumnWidths
    $updatedSpellRows++
}

[System.IO.File]::WriteAllLines($spells2daPath, $spellsLines)

$rawMissingEntryCount = @($newEntries | Where-Object { [int]$_.id -lt $rawTlkInfo.StringCount }).Count
$appendedEntryCount = @($newEntries | Where-Object { [int]$_.id -ge $rawTlkInfo.StringCount }).Count

Invoke-TlkTool $tlkToolPath @(
    "-i", $tlkJsonPath,
    "-l", "json",
    "-o", $tlkPath,
    "-k", "tlk"
)

Write-Host "Updated $updatedFeatRows feat rows."
Write-Host "Updated $updatedSpellRows spell rows."
Write-Host "Raw TLK string count before sync: $($rawTlkInfo.StringCount)."
Write-Host "Raw open TLK slots available before sync: $($rawTlkInfo.OpenIds.Count)."
Write-Host "Filled $($filledBlankEntries.Count) existing blank TLK entries."
Write-Host "Used $rawMissingEntryCount raw missing TLK IDs before appending."
Write-Host "Added $appendedEntryCount TLK entries beyond the raw string count."
Write-Host "TLK max ID before sync: $maxId; after sync: $([Math]::Max($maxId, $nextId.Value - 1))."
