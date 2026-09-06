[CmdletBinding()]
param(
    [switch]$RefreshBible,
    [switch]$RefreshLocalBible,
    [string]$SpreadsheetId = "1rppEkwp2dX0oGKY1ftSbDTcg7GhopODseqbDb4cpNSU",
    [string]$BibleWorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx",
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\CombatUpgradeBiblePerkManifest.csv",
    [string]$AuditPath = "SWLOR.Game.Server\Readmes\CombatUpgradePerkAudit.csv"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $repoRoot $Path
}

function Get-SanitizedName {
    param([string]$Name)

    if ([string]::IsNullOrWhiteSpace($Name)) {
        return ""
    }

    $baseName = $Name -replace "\s+\([^)]*\)$", ""
    $baseName = $baseName -replace "\s+(I|II|III|IV|V|VI)$", ""
    return ($baseName -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
}

function Get-PropertyValue {
    param(
        [object]$Object,
        [string]$Name
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Get-FirstPropertyValue {
    param(
        [object]$Object,
        [string[]]$Names
    )

    foreach ($name in $Names) {
        $value = Get-PropertyValue $Object $name
        if ($null -ne $value) {
            return $value
        }
    }

    return $null
}

function Get-RepoRelativePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ""
    }

    $fullPath = $Path
    if (![System.IO.Path]::IsPathRooted($fullPath)) {
        return $fullPath
    }

    if ($fullPath.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($repoRoot.Length).TrimStart("\", "/")
    }

    return $fullPath
}

function Add-AuditRow {
    param(
        [System.Collections.Generic.List[object]]$Rows,
        [string]$AuditType,
        [object]$BibleRow,
        [string]$File = "",
        [string]$Details = ""
    )

    function Normalize-AuditText {
        param([object]$Value)

        if ($null -eq $Value) {
            return ""
        }

        return ([string]$Value -replace "\s+", " ").Trim()
    }

    $Rows.Add([pscustomobject]@{
        AuditType = Normalize-AuditText $AuditType
        Tab = Normalize-AuditText $BibleRow.Tab
        Row = Normalize-AuditText $BibleRow.Row
        Name = Normalize-AuditText $BibleRow.PerkName
        PerkType = ""
        File = Get-RepoRelativePath $File
        Style = Normalize-AuditText $BibleRow.Style
        Price = Normalize-AuditText $BibleRow.Price
        Requirements = Normalize-AuditText $BibleRow.SkillRequirements
        CharType = Normalize-AuditText $BibleRow.CharacterType
        Type = Normalize-AuditText $BibleRow.Type
        PrimaryStat = Normalize-AuditText (Get-PropertyValue $BibleRow "PrimaryStat")
        SecondaryStat = Normalize-AuditText (Get-PropertyValue $BibleRow "SecondaryStat")
        ScalingSource = Normalize-AuditText (Get-PropertyValue $BibleRow "ScalingSource")
        DevStatus = Normalize-AuditText $BibleRow.DevStatus
        Description = Normalize-AuditText $BibleRow.Description
        Details = Normalize-AuditText $Details
    }) | Out-Null
}

function Import-CodeNameIndex {
    param(
        [string]$Path,
        [string]$Filter
    )

    $index = @{}
    $files = Get-ChildItem -Path $Path -Filter $Filter -Recurse

    foreach ($file in $files) {
        $content = Get-Content $file.FullName -Raw
        $matches = [regex]::Matches($content, '\.Name\("([^"]+)"\)')
        foreach ($match in $matches) {
            $name = $match.Groups[1].Value
            if (!$index.ContainsKey($name)) {
                $index[$name] = New-Object System.Collections.Generic.List[object]
            }

            $index[$name].Add([pscustomobject]@{
                File = $file.FullName
                Content = $content
            }) | Out-Null
        }
    }

    return $index
}

function Import-NativeActionModePerkNameIndex {
    param([string]$Path)

    $index = @{}
    $files = Get-ChildItem -Path $Path -Filter "*PerkDefinition.cs" -Recurse

    foreach ($file in $files) {
        $content = Get-Content $file.FullName -Raw
        $builderSegments = [regex]::Split($content, '(?=\.Create\s*\()')
        foreach ($segment in $builderSegments) {
            if ($segment -notmatch '\.AutoAddActionModeToHotBar\s*\(') {
                continue
            }

            $nameMatch = [regex]::Match($segment, '\.Name\("([^"]+)"\)')
            if (!$nameMatch.Success) {
                continue
            }

            $index[(Get-SanitizedName $nameMatch.Groups[1].Value)] = $true
        }
    }

    return $index
}

function Import-AbilityFileIndex {
    param([string]$Path)

    $index = @{}
    $files = Get-ChildItem -Path $Path -Filter "*AbilityDefinition.cs" -Recurse

    foreach ($file in $files) {
        $key = ($file.BaseName -replace "AbilityDefinition$", "").ToLowerInvariant()
        $index[$key] = [pscustomobject]@{
            File = $file.FullName
            Content = Get-Content $file.FullName -Raw
        }
    }

    return $index
}

function Get-AbilityFileForBibleRow {
    param(
        [hashtable]$AbilityFilesByName,
        [object]$BibleRow
    )

    $key = Get-SanitizedName $BibleRow.PerkName
    if ($AbilityFilesByName.ContainsKey($key)) {
        return $AbilityFilesByName[$key]
    }

    return $null
}

function Test-AbilitySatisfiesStatusCheck {
    param(
        [string]$AbilityContent,
        [hashtable]$StatusDefinitionContentByName,
        [string]$StatusEffectClass,
        [string]$StatusEnum
    )

    if ([string]::IsNullOrWhiteSpace($AbilityContent)) {
        return $false
    }

    $statusEffectPattern = "\b$([regex]::Escape($StatusEffectClass))\b"
    if ($AbilityContent -match $statusEffectPattern) {
        return $true
    }

    if ($StatusEffectClass -eq "ExposedStatusEffect" -and
        $AbilityContent -match "\bTemporaryCostlyAbilityExposedDurationSeconds\b") {
        return $true
    }

    if ($StatusEffectClass -eq "ExposedStatusEffect" -and
        $AbilityContent -match "\bStatType\.BackAttackExposedPercent\b" -and
        $AbilityContent -match "\bStatType\.BackAttackExposedDurationSeconds\b") {
        return $true
    }

    if ($StatusEffectClass -eq "HemorrhageStatusEffect" -and
        $AbilityContent -match "\bConsumeBleedIntoHemorrhage\b") {
        return $true
    }

    $immunityPattern = "\bImmunityType\.$([regex]::Escape($StatusEnum))\b"
    if ($AbilityContent -match $immunityPattern) {
        return $true
    }

    $expectedResistanceType = $null
    if ($StatusDefinitionContentByName.ContainsKey($StatusEffectClass)) {
        $targetStatusDefinitionContent = $StatusDefinitionContentByName[$StatusEffectClass]
        if ($targetStatusDefinitionContent -match "ResistanceType\s*=>\s*ResistanceType\.(\w+)") {
            $expectedResistanceType = $Matches[1]
        }
    }

    $referencedStatusEffects = [regex]::Matches($AbilityContent, 'typeof\((\w+StatusEffect)\)') |
        ForEach-Object { $_.Groups[1].Value } |
        Select-Object -Unique

    foreach ($referencedStatusEffect in $referencedStatusEffects) {
        if (!$StatusDefinitionContentByName.ContainsKey($referencedStatusEffect)) {
            continue
        }

        $statusDefinitionContent = $StatusDefinitionContentByName[$referencedStatusEffect]
        if ($statusDefinitionContent -match $statusEffectPattern -or $statusDefinitionContent -match $immunityPattern) {
            return $true
        }
        if ($StatusEffectClass -eq "HamstringStatusEffect" -and
            ($statusDefinitionContent -match "\bStatType\.DamageDealtHamstring" -or
             $statusDefinitionContent -match "\bStatType\.AutoAttackHamstring")) {
            return $true
        }

        if (![string]::IsNullOrWhiteSpace($expectedResistanceType) -and
            $statusDefinitionContent -match "\bStatType\.$([regex]::Escape($expectedResistanceType))Resistance\b") {
            return $true
        }
    }

    foreach ($statusDefinitionContent in $StatusDefinitionContentByName.Values) {
        if ($statusDefinitionContent -notmatch $statusEffectPattern) {
            continue
        }

        if ($statusDefinitionContent -match "ResistanceType\s*=>\s*ResistanceType\.(\w+)") {
            $resistanceType = $Matches[1]
            if ($AbilityContent -match "\bStatType\.$([regex]::Escape($resistanceType))Resistance\b") {
                return $true
            }
        }
    }

    return $false
}

function Convert-CooldownToSeconds {
    param([string]$Cooldown)

    if ([string]::IsNullOrWhiteSpace($Cooldown) -or $Cooldown -eq "-") {
        return $null
    }

    $value = $Cooldown.Trim().ToLowerInvariant()
    if ($value -match "^(\d+)\s*seconds?$") {
        return [int]$matches[1]
    }
    if ($value -match "^(\d+)\s*minutes?$") {
        return [int]$matches[1] * 60
    }

    return $null
}

function Import-2DA {
    param([string]$Path)

    $lines = Get-Content $Path
    $headerLineIndex = -1
    for ($i = 1; $i -lt $lines.Count; $i++) {
        if (![string]::IsNullOrWhiteSpace($lines[$i])) {
            $headerLineIndex = $i
            break
        }
    }

    if ($headerLineIndex -lt 0) {
        return @()
    }

    $headers = $lines[$headerLineIndex].Trim() -split "\s+"
    $rows = New-Object System.Collections.Generic.List[object]

    for ($i = $headerLineIndex + 1; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $fields = $line.Trim() -split "\s+"
        if ($fields.Count -lt 2) {
            continue
        }

        $rowNumber = 0
        if (![int]::TryParse($fields[0], [ref]$rowNumber)) {
            continue
        }

        $rows.Add([pscustomobject]@{
            Number = $rowNumber
            Headers = $headers
            Fields = $fields
            SourceLine = $line
        }) | Out-Null
    }

    return $rows
}

function Import-BibleTabRows {
    param([string]$Csv)

    $lines = $Csv -split "\r?\n"
    $headerLineIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "Perk Name|PerkName") {
            $headerLineIndex = $i
            break
        }
    }

    if ($headerLineIndex -lt 0) {
        return @()
    }

    $rawHeaderNames = 1..80 | ForEach-Object { "Header$_" }
    $headerRow = $lines[$headerLineIndex] | ConvertFrom-Csv -Header $rawHeaderNames
    $headers = New-Object System.Collections.Generic.List[string]
    $seenHeaders = @{}
    $columnNumber = 1
    foreach ($property in $headerRow.PSObject.Properties) {
        $canonicalHeader = Get-CanonicalManifestHeader $property.Value
        if ([string]::IsNullOrWhiteSpace($canonicalHeader)) {
            $canonicalHeader = "Unused$columnNumber"
        }

        if ($seenHeaders.ContainsKey($canonicalHeader)) {
            $seenHeaders[$canonicalHeader]++
            $canonicalHeader = "$canonicalHeader$($seenHeaders[$canonicalHeader])"
        }
        else {
            $seenHeaders[$canonicalHeader] = 1
        }

        $headers.Add($canonicalHeader) | Out-Null
        $columnNumber++
    }

    $rows = New-Object System.Collections.Generic.List[object]
    for ($i = $headerLineIndex + 1; $i -lt $lines.Count; $i++) {
        if ([string]::IsNullOrWhiteSpace($lines[$i])) {
            continue
        }

        $parsedRows = $lines[$i] | ConvertFrom-Csv -Header $headers.ToArray()
        foreach ($row in $parsedRows) {
            $name = $row.PerkName
            $type = $row.Type
            $devStatus = $row.DevStatus
            if ([string]::IsNullOrWhiteSpace($name) -or (
                [string]::IsNullOrWhiteSpace($type) -and
                [string]::IsNullOrWhiteSpace($devStatus)
            )) {
                continue
            }

            $rows.Add([pscustomobject]@{
                Row = $i + 1
                Style = $row.Style
                Price = $row.Price
                PerkName = $name
                SkillRequirements = $row.SkillRequirements
                CharacterType = $row.CharacterType
                Type = $type
                Description = $row.Description
                PrimaryStat = $row.PrimaryStat
                SecondaryStat = $row.SecondaryStat
                ScalingSource = $row.ScalingSource
                CrossSkill = $row.CrossSkill
                FP = $row.FP
                STM = $row.STM
                CastingTime = $row.CastingTime
                CooldownTime = $row.CooldownTime
                DevStatus = $devStatus
                AdditionalRequirements = $row.AdditionalRequirements
                Notes = $row.Notes
            }) | Out-Null
        }
    }

    return $rows
}

function Read-ZipEntryText {
    param(
        [System.IO.Compression.ZipArchive]$Zip,
        [string]$EntryPath
    )

    $entry = $Zip.GetEntry($EntryPath)
    if ($null -eq $entry) {
        throw "Workbook entry '$EntryPath' was not found."
    }

    $stream = $entry.Open()
    try {
        $reader = [System.IO.StreamReader]::new($stream)
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Get-OpenXmlColumnIndex {
    param([string]$CellReference)

    $letters = ([regex]::Match($CellReference, "^[A-Z]+")).Value
    if ([string]::IsNullOrWhiteSpace($letters)) {
        return 0
    }

    $index = 0
    foreach ($character in $letters.ToCharArray()) {
        $index = ($index * 26) + ([int][char]$character - [int][char]"A" + 1)
    }

    return $index
}

function Get-WorkbookEntryPath {
    param([string]$RelationshipTarget)

    $target = $RelationshipTarget.Replace("\", "/").TrimStart("/")
    if ($target.StartsWith("xl/", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $target
    }

    return "xl/$target"
}

function Normalize-ManifestCellText {
    param([object]$Value)

    if ($null -eq $Value) {
        return ""
    }

    $text = [string]$Value
    $text = $text -replace "[ \t]+\r?\n", "`n"
    return $text.Trim()
}

function Test-ManifestValuePresent {
    param([object]$Value)

    if ($null -eq $Value) {
        return $false
    }

    $text = ([string]$Value).Trim()
    return ![string]::IsNullOrWhiteSpace($text) -and $text -ne "-"
}

function Get-OpenXmlCellText {
    param(
        [System.Xml.XmlElement]$Cell,
        [System.Collections.Generic.IList[string]]$SharedStrings
    )

    $cellType = $Cell.GetAttribute("t")
    if ($cellType -eq "inlineStr") {
        return Normalize-ManifestCellText $Cell.InnerText
    }

    $rawValue = $Cell.InnerText
    if ([string]::IsNullOrWhiteSpace($rawValue)) {
        return ""
    }

    if ($cellType -eq "s") {
        return Normalize-ManifestCellText $SharedStrings[[int]$rawValue]
    }

    return Normalize-ManifestCellText $rawValue
}

function Get-CanonicalManifestHeader {
    param([string]$Header)

    if ([string]::IsNullOrWhiteSpace($Header)) {
        return ""
    }

    $key = ($Header -replace "[\s\.\?]+", "").ToLowerInvariant()
    switch ($key) {
        "style" { return "Style" }
        "spprice" { return "Price" }
        "price" { return "Price" }
        "perkname" { return "PerkName" }
        "name" { return "PerkName" }
        "skillreqs" { return "SkillRequirements" }
        "skillrequirements" { return "SkillRequirements" }
        "requirements" { return "SkillRequirements" }
        "chartype" { return "CharacterType" }
        "charactertype" { return "CharacterType" }
        "type" { return "Type" }
        "description" { return "Description" }
        "primarystat" { return "PrimaryStat" }
        "secondarystat" { return "SecondaryStat" }
        "scalingsource" { return "ScalingSource" }
        "crossskill" { return "CrossSkill" }
        "fp" { return "FP" }
        "stm" { return "STM" }
        "castingtime" { return "CastingTime" }
        "cooldowntime" { return "CooldownTime" }
        "cooldown" { return "CooldownTime" }
        "devstatus" { return "DevStatus" }
        "additionalrequirements" { return "AdditionalRequirements" }
        "notes" { return "Notes" }
        "slots" { return "Slots" }
        "slot" { return "Slots" }
        default { return "" }
    }
}

function Get-MappedCellValue {
    param(
        [hashtable]$Cells,
        [hashtable]$ColumnByHeader,
        [string]$Header
    )

    if (!$ColumnByHeader.ContainsKey($Header)) {
        return ""
    }

    return $Cells[$ColumnByHeader[$Header]]
}

function Import-BibleWorkbookManifestRows {
    param(
        [string]$Path,
        [string[]]$SheetTabs,
        [hashtable]$SheetTabAliases = @{}
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $sharedStrings = [System.Collections.Generic.List[string]]::new()
        if ($null -ne $zip.GetEntry("xl/sharedStrings.xml")) {
            [xml]$sharedStringsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/sharedStrings.xml"
            $sharedStringNamespace = [System.Xml.XmlNamespaceManager]::new($sharedStringsXml.NameTable)
            $sharedStringNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

            foreach ($sharedString in $sharedStringsXml.SelectNodes("//d:si", $sharedStringNamespace)) {
                $texts = [System.Collections.Generic.List[string]]::new()
                foreach ($textNode in $sharedString.SelectNodes(".//d:t", $sharedStringNamespace)) {
                    $texts.Add($textNode.InnerText) | Out-Null
                }

                $sharedStrings.Add(($texts -join "")) | Out-Null
            }
        }

        [xml]$workbookXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/workbook.xml"
        [xml]$relationshipsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/_rels/workbook.xml.rels"

        $relationshipsById = @{}
        foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
            $relationshipsById[$relationship.Id] = Get-WorkbookEntryPath $relationship.Target
        }

        $workbookNamespace = [System.Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
        $workbookNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
        $workbookNamespace.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")

        $sheetPathsByName = @{}
        foreach ($sheet in $workbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
            $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
            $sheetPathsByName[$sheet.GetAttribute("name")] = $relationshipsById[$relationshipId]
        }

        $manifestRows = [System.Collections.Generic.List[object]]::new()
        foreach ($tab in $SheetTabs) {
            $workbookTab = $tab
            if (!$sheetPathsByName.ContainsKey($workbookTab) -and $SheetTabAliases.ContainsKey($tab)) {
                $workbookTab = $SheetTabAliases[$tab]
            }

            if (!$sheetPathsByName.ContainsKey($workbookTab)) {
                throw "Workbook sheet '$tab' was not found."
            }

            [xml]$worksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $sheetPathsByName[$workbookTab]
            $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($worksheetXml.NameTable)
            $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

            $headerRowNumber = 0
            $columnByHeader = @{}
            foreach ($rowNode in $worksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
                $rowNumberText = $rowNode.GetAttribute("r")
                if ([string]::IsNullOrWhiteSpace($rowNumberText)) {
                    continue
                }

                $cells = @{}
                foreach ($cell in $rowNode.SelectNodes("d:c", $worksheetNamespace)) {
                    $columnIndex = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
                    if ($columnIndex -gt 0) {
                        $cells[$columnIndex] = Get-OpenXmlCellText -Cell $cell -SharedStrings $sharedStrings
                    }
                }

                $rowNumber = [int]$rowNumberText
                if ($headerRowNumber -eq 0 -and (($cells.Values -join "|") -match "Perk Name|PerkName")) {
                    $headerRowNumber = $rowNumber
                    foreach ($cellEntry in $cells.GetEnumerator()) {
                        $canonicalHeader = Get-CanonicalManifestHeader $cellEntry.Value
                        if (![string]::IsNullOrWhiteSpace($canonicalHeader) -and !$columnByHeader.ContainsKey($canonicalHeader)) {
                            $columnByHeader[$canonicalHeader] = $cellEntry.Key
                        }
                    }
                    continue
                }

                if ($headerRowNumber -eq 0 -or $rowNumber -le $headerRowNumber) {
                    continue
                }

                $name = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "PerkName"
                if ([string]::IsNullOrWhiteSpace($name)) {
                    continue
                }

                $type = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "Type"
                $devStatus = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "DevStatus"
                if ([string]::IsNullOrWhiteSpace($type) -and [string]::IsNullOrWhiteSpace($devStatus)) {
                    continue
                }

                $manifestRows.Add([pscustomobject]@{
                    Tab = $tab
                    Row = $rowNumber
                    Style = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "Style"
                    Price = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "Price"
                    PerkName = $name
                    SkillRequirements = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "SkillRequirements"
                    CharacterType = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "CharacterType"
                    Type = $type
                    Description = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "Description"
                    PrimaryStat = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "PrimaryStat"
                    SecondaryStat = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "SecondaryStat"
                    ScalingSource = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "ScalingSource"
                    CrossSkill = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "CrossSkill"
                    FP = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "FP"
                    STM = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "STM"
                    CastingTime = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "CastingTime"
                    CooldownTime = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "CooldownTime"
                    DevStatus = $devStatus
                    AdditionalRequirements = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "AdditionalRequirements"
                    Notes = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "Notes"
                    Slots = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "Slots"
                }) | Out-Null
            }
        }

        return $manifestRows
    }
    finally {
        $zip.Dispose()
    }
}

$manifestFullPath = Resolve-RepoPath $ManifestPath
$auditFullPath = Resolve-RepoPath $AuditPath
$workbookFullPath = Resolve-RepoPath $BibleWorkbookPath

if ($RefreshBible -and $RefreshLocalBible) {
    throw "Use either -RefreshBible or -RefreshLocalBible, not both."
}

$sheetTabs = @(
    "Armor", "Vibroblade", "Vibroknife", "Lightsaber", "Heavy Vibroblade", "Spear",
    "Twin Blade", "Saberstaff", "Katar", "Staff", "Pistol", "Rifle", "Throwing",
    "Force", "Devices", "Beast Mastery", "Piloting", "Leadership", "First Aid",
    "Espionage", "Smithery", "Engineering", "Fabrication", "Research", "Agriculture", "Gathering",
    "Mimicry"
)
$localWorkbookSheetTabAliases = @{
    Armor = "General"
}

if ($RefreshBible) {
    $manifestRows = New-Object System.Collections.Generic.List[object]
    foreach ($tab in $sheetTabs) {
        $encodedTab = [System.Uri]::EscapeDataString($tab)
        $uri = "https://docs.google.com/spreadsheets/d/$SpreadsheetId/gviz/tq?tqx=out:csv&sheet=$encodedTab"
        $csv = Invoke-WebRequest -Uri $uri -UseBasicParsing | Select-Object -ExpandProperty Content
        $rows = Import-BibleTabRows -Csv $csv

        foreach ($row in $rows) {
            $name = Get-FirstPropertyValue $row @("PerkName", "Perk Name", "Name")
            if ([string]::IsNullOrWhiteSpace($name)) {
                $name = Get-PropertyValue $row "Name"
            }
            if ([string]::IsNullOrWhiteSpace($name)) {
                continue
            }

            $manifestRows.Add([pscustomobject]@{
                Tab = $tab
                Row = Get-FirstPropertyValue $row @("Row", "#")
                Style = Get-PropertyValue $row "Style"
                Price = Get-PropertyValue $row "Price"
                PerkName = $name
                SkillRequirements = Get-FirstPropertyValue $row @("SkillRequirements", "Skill Requirements", "Requirements")
                CharacterType = Get-FirstPropertyValue $row @("CharacterType", "Character Type", "CharType")
                Type = Get-PropertyValue $row "Type"
                Description = Get-PropertyValue $row "Description"
                PrimaryStat = Get-FirstPropertyValue $row @("PrimaryStat", "Primary Stat")
                SecondaryStat = Get-FirstPropertyValue $row @("SecondaryStat", "Secondary Stat")
                ScalingSource = Get-FirstPropertyValue $row @("ScalingSource", "Scaling Source")
                CrossSkill = Get-FirstPropertyValue $row @("CrossSkill", "Cross Skill")
                FP = Get-PropertyValue $row "FP"
                STM = Get-PropertyValue $row "STM"
                CastingTime = Get-FirstPropertyValue $row @("CastingTime", "Casting Time")
                CooldownTime = Get-FirstPropertyValue $row @("CooldownTime", "Cooldown Time", "Cooldown")
                DevStatus = Get-FirstPropertyValue $row @("DevStatus", "Dev Status")
                AdditionalRequirements = Get-FirstPropertyValue $row @("AdditionalRequirements", "Additional Requirements")
                Notes = Get-PropertyValue $row "Notes"
                Slots = Get-FirstPropertyValue $row @("Slots", "Slot")
            }) | Out-Null
        }
    }

    if ($manifestRows.Count -eq 0) {
        throw "No Bible perk rows were parsed. The sheet export format may have changed."
    }

    $manifestRows | Export-Csv -Path $manifestFullPath -NoTypeInformation
}

if ($RefreshLocalBible) {
    $formatterFullPath = Resolve-RepoPath "tools\FormatCombatUpgradeBibleWorkbook.ps1"
    if (Test-Path -LiteralPath $formatterFullPath) {
        & $formatterFullPath -WorkbookPath $workbookFullPath
    }

    $manifestRows = Import-BibleWorkbookManifestRows -Path $workbookFullPath -SheetTabs $sheetTabs -SheetTabAliases $localWorkbookSheetTabAliases
    if ($manifestRows.Count -eq 0) {
        throw "No local Bible workbook perk rows were parsed. The workbook export format may have changed."
    }

    $manifestRows | Export-Csv -Path $manifestFullPath -NoTypeInformation
}

$outOfScopeTabs = @(
    "Farming",
    "Agriculture",
    "Smithery",
    "Engineering",
    "Fabrication",
    "Research",
    "Gathering"
)
$manifest = Import-Csv $manifestFullPath |
    Where-Object {
        $outOfScopeTabs -notcontains $_.Tab -and
        ![string]::IsNullOrWhiteSpace($_.PerkName) -and
        @("Aura", "Capstone", "Combat", "Stance", "Toggle", "Trait") -contains $_.Type
    }

if (@($manifest).Count -eq 0) {
    throw "Combat upgrade manifest contains no scoped rows. Run with -RefreshLocalBible, run with -RefreshBible, or restore a valid manifest before auditing."
}

$perkIndex = Import-CodeNameIndex -Path (Resolve-RepoPath "SWLOR.Game.Server\Feature\PerkDefinition") -Filter "*PerkDefinition.cs"
$nativeActionModePerkBaseNameIndex = Import-NativeActionModePerkNameIndex -Path (Resolve-RepoPath "SWLOR.Game.Server\Feature\PerkDefinition")
$abilityNameIndex = Import-CodeNameIndex -Path (Resolve-RepoPath "SWLOR.Game.Server\Feature\AbilityDefinition") -Filter "*AbilityDefinition.cs"
$abilityFileIndex = Import-AbilityFileIndex -Path (Resolve-RepoPath "SWLOR.Game.Server\Feature\AbilityDefinition")
$playerAbilityFeatLabelsRequiringSpellLink = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
Get-ChildItem (Resolve-RepoPath "SWLOR.Game.Server\Feature\AbilityDefinition") -Filter "*AbilityDefinition.cs" -Recurse |
    Where-Object { $_.FullName -notmatch "\\NPC\\" } |
    ForEach-Object {
        $content = Get-Content $_.FullName -Raw
        if ($content -notmatch "IAbilityListDefinition") {
            return
        }

        if ($content -notmatch "\bSpell\.(?!Invalid\b)\w+") {
            return
        }

        $createdFeatPattern = "(?s)(?:\.Create\s*\(\s*|BuildArea\s*\(\s*[^,]+,\s*)FeatType\.(\w+)"
        foreach ($match in [regex]::Matches($content, $createdFeatPattern)) {
            $playerAbilityFeatLabelsRequiringSpellLink.Add($match.Groups[1].Value) | Out-Null
        }
    }
$statusDefinitionContentByName = @{}
$statusDefinitionTextParts = New-Object System.Collections.Generic.List[string]
Get-ChildItem (Resolve-RepoPath "SWLOR.Game.Server\Feature\StatusEffectDefinition") -Filter "*StatusEffect.cs" -Recurse |
    ForEach-Object {
        $content = Get-Content $_.FullName -Raw
        $statusDefinitionContentByName[$_.BaseName] = $content
        $statusDefinitionTextParts.Add($content) | Out-Null
    }
$statusDefinitionText = $statusDefinitionTextParts -join "`n"

$perkBaseNameIndex = @{}
foreach ($key in $perkIndex.Keys) {
    $perkBaseNameIndex[(Get-SanitizedName $key)] = $true
}

$abilityBaseNameIndex = @{}
foreach ($key in $abilityNameIndex.Keys) {
    $sanitizedKey = Get-SanitizedName $key
    $abilityBaseNameIndex[$sanitizedKey] = $true
    if (!$abilityFileIndex.ContainsKey($sanitizedKey) -and $abilityNameIndex[$key].Count -gt 0) {
        $abilityFileIndex[$sanitizedKey] = $abilityNameIndex[$key][0]
    }
}
foreach ($key in $abilityFileIndex.Keys) {
    $abilityBaseNameIndex[(Get-SanitizedName $key)] = $true
}

$statusApplicationVerb = "(?:inflict(?:s|ed|ing)?|appl(?:y|ies|ied)|grant(?:s|ed|ing)?|gain(?:s|ed|ing)?|become(?:s)?(?!\s+immune\b)|suffer(?:s|ed|ing)?|attempt(?:s|ed|ing)?\s+to\s+inflict)"
$statusChecks = @(
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}-\s*\d+\s+Poison Resistance\b"; Enum = "PoisonResistancePenalty" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bPoison\b(?!\s+(?:Damage|Resistance))"; Enum = "Poison" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bBleed\b(?!\s+duration)"; Enum = "Bleed" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bSunder(?:ed)?\b"; Enum = "Sunder" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bForce Disruption\b"; Enum = "ForceDisruption" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bBlind\b"; Enum = "Blind" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bShadow Toxin\b"; Enum = "ShadowToxin" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}(?<!Shadow\s)\bToxin\b"; Enum = "Toxin" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bDisoriented\b"; Enum = "Disoriented" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bWeakened\b"; Enum = "Weakened" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bDazed\b"; Enum = "Dazed" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bStunned\b"; Enum = "Stunned" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bExposed\b"; Enum = "Exposed" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bHemorrhage\b"; Enum = "Hemorrhage" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bKnockdown\b"; Enum = "Knockdown" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bVital Strike\b"; Enum = "VitalStrike" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bHamstring\b"; Enum = "Hamstring" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bExhausted\b"; Enum = "Exhausted" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bHobble\b"; Enum = "Hobble" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bForce Erosion\b"; Enum = "ForceErosion" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bFractured Focus\b"; Enum = "FracturedFocus" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bForce Warding\b"; Enum = "ForceWarding" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bFoggy Mind\b"; Enum = "FoggyMind" }
)

$auditRows = New-Object System.Collections.Generic.List[object]

foreach ($row in $manifest) {
    # Mimicry learned techniques are not purchasable perks and have no perk name, recast group, or
    # dedicated perk-menu ability; they are audited as feat-granting abilities elsewhere. Skip the
    # perk/ability/recast checks for them regardless of their (standard) Type label.
    if ($row.Style -eq "Technique") { continue }

    $rowBaseName = Get-SanitizedName $row.PerkName
    $expectsPerkDefinition = $row.DevStatus -eq "Implemented"
    if ($expectsPerkDefinition -and !$perkBaseNameIndex.ContainsKey($rowBaseName)) {
        Add-AuditRow -Rows $auditRows -AuditType "MissingPerkName" -BibleRow $row
    }

    $isActiveType = @("Aura", "Combat", "Stance", "Toggle") -contains $row.Type -or
        ($row.Type -eq "Capstone" -and (
            (Test-ManifestValuePresent $row.FP) -or
            (Test-ManifestValuePresent $row.STM) -or
            (Test-ManifestValuePresent $row.CastingTime) -or
            (Test-ManifestValuePresent $row.CooldownTime) -or
            $abilityBaseNameIndex.ContainsKey($rowBaseName)))
    $usesNativeActionMode = $nativeActionModePerkBaseNameIndex.ContainsKey($rowBaseName)
    if ($isActiveType -and !$usesNativeActionMode -and !$abilityBaseNameIndex.ContainsKey($rowBaseName)) {
        Add-AuditRow -Rows $auditRows -AuditType "MissingAbilityDefinition" -BibleRow $row
    }

    $abilityFile = Get-AbilityFileForBibleRow -AbilityFilesByName $abilityFileIndex -BibleRow $row
    $cooldownSeconds = Convert-CooldownToSeconds $row.CooldownTime
    $hasDetectedRecast = $abilityFile -and (
        $abilityFile.Content -match "HasRecastDelay" -or
        $abilityFile.Content -match "Configure(?:Hostile|SupportStatus|Cleanse|Heal|Revive)\("
    )
    if ($isActiveType -and $null -ne $cooldownSeconds -and $abilityFile -and !$hasDetectedRecast) {
        Add-AuditRow -Rows $auditRows -AuditType "MissingAbilityRecast" -BibleRow $row -File $abilityFile.File -Details "Expected cooldown: $($row.CooldownTime)"
    }

    if ($row.Description -match "(?i)\b(?:Fortitude|Reflex|Willpower|Will)\s+DC\d*\b|\bDC\d+\s*(?:Fortitude|Reflex|Willpower|Will)\b|\bsaving throw\b|\bsave DCs?\b|\bfailed save\b|\bon resist\b|\bmake(?:s)?\s+DC\d+\s*(?:Fortitude|Reflex|Willpower|Will)\b") {
        Add-AuditRow -Rows $auditRows -AuditType "StaleSavingThrowText" -BibleRow $row -Details "Remove save/DC wording; Resistances shorten effects."
    }

    foreach ($check in $statusChecks) {
        if ($row.Description -match $check.Pattern) {
            $statusEffectClass = "$($check.Enum)StatusEffect"
            if ($statusDefinitionText -notmatch "\b$statusEffectClass\b") {
                Add-AuditRow -Rows $auditRows -AuditType "MissingStatusEffectDefinition" -BibleRow $row -Details $statusEffectClass
            }
            elseif (
                $isActiveType -and
                $abilityFile -and
                !(Test-AbilitySatisfiesStatusCheck `
                    -AbilityContent $abilityFile.Content `
                    -StatusDefinitionContentByName $statusDefinitionContentByName `
                    -StatusEffectClass $statusEffectClass `
                    -StatusEnum $check.Enum)
            ) {
                Add-AuditRow -Rows $auditRows -AuditType "StatusNotAppliedInAbility" -BibleRow $row -File $abilityFile.File -Details $statusEffectClass
            }
        }
    }
}

$iconRoot = Resolve-RepoPath "SWLOR_Haks\sw_ability"
$iconNames = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
Get-ChildItem $iconRoot -Filter "*.tga" | ForEach-Object {
    $iconNames.Add([System.IO.Path]::GetFileNameWithoutExtension($_.Name)) | Out-Null
}

$featRows = Import-2DA (Resolve-RepoPath "SWLOR_Haks\sw_2da\feat.2da")
$combatUpgradeSpellIds = New-Object "System.Collections.Generic.HashSet[int]"
foreach ($row in $featRows) {
    $iconIndex = [array]::IndexOf($row.Headers, "ICON") + 1
    $spellIndex = [array]::IndexOf($row.Headers, "SPELLID") + 1
    $isCombatUpgradeFeat = $row.Number -ge 2000 -or $row.Fields[1] -match "^KoltoRecovery[123]$"
    if ($isCombatUpgradeFeat -and $iconIndex -gt 0 -and $row.Fields.Count -gt $iconIndex) {
        $icon = $row.Fields[$iconIndex]
        if ($icon -ne "****" -and !$iconNames.Contains($icon)) {
            $auditRows.Add([pscustomobject]@{
                AuditType = "MissingFeatIconResource"
                Tab = "2DA"
                Row = $row.Number
                Name = $row.Fields[1]
                PerkType = ""
                File = "SWLOR_Haks\sw_2da\feat.2da"
                Style = ""
                Price = ""
                Requirements = ""
                CharType = ""
                Type = "FeatIcon"
                PrimaryStat = ""
                SecondaryStat = ""
                ScalingSource = ""
                DevStatus = ""
                Description = ""
                Details = $icon
            }) | Out-Null
        }
    }

    if (!$isCombatUpgradeFeat -or $spellIndex -le 0 -or $row.Fields.Count -le $spellIndex) {
        continue
    }

    $spellId = 0
    if ([int]::TryParse($row.Fields[$spellIndex], [ref]$spellId)) {
        $combatUpgradeSpellIds.Add($spellId) | Out-Null
    }

    if ($row.Number -ge 2000 -and
        $row.Fields[1] -ne "****" -and
        $row.Fields[$spellIndex] -eq "****" -and
        $playerAbilityFeatLabelsRequiringSpellLink.Contains($row.Fields[1])) {
        $auditRows.Add([pscustomobject]@{
            AuditType = "GeneratedFeatMissingSpellLink"
            Tab = "2DA"
            Row = $row.Number
            Name = $row.Fields[1]
            PerkType = ""
            File = "SWLOR_Haks\sw_2da\feat.2da"
            Style = ""
            Price = ""
                Requirements = ""
                CharType = ""
                Type = "FeatSpell"
                PrimaryStat = ""
                SecondaryStat = ""
                ScalingSource = ""
                DevStatus = ""
                Description = ""
                Details = "SPELLID is ****"
        }) | Out-Null
    }
}

$spellRows = Import-2DA (Resolve-RepoPath "SWLOR_Haks\sw_2da\spells.2da")
foreach ($row in $spellRows) {
    if (!$combatUpgradeSpellIds.Contains($row.Number)) {
        continue
    }

    $iconIndex = [array]::IndexOf($row.Headers, "IconResRef") + 1
    if ($iconIndex -le 0 -or $row.Fields.Count -le $iconIndex) {
        continue
    }

    $icon = $row.Fields[$iconIndex]
    if ($icon -ne "****" -and !$iconNames.Contains($icon)) {
        $auditRows.Add([pscustomobject]@{
            AuditType = "MissingSpellIconResource"
            Tab = "2DA"
            Row = $row.Number
            Name = $row.Fields[1]
            PerkType = ""
            File = "SWLOR_Haks\sw_2da\spells.2da"
            Style = ""
            Price = ""
            Requirements = ""
            CharType = ""
            Type = "SpellIcon"
            PrimaryStat = ""
            SecondaryStat = ""
            ScalingSource = ""
            DevStatus = ""
            Description = ""
            Details = $icon
        }) | Out-Null
    }
}

if ($auditRows.Count -gt 0) {
    $auditRows | Sort-Object AuditType, Tab, Row, Name | Export-Csv -Path $auditFullPath -NoTypeInformation
}
else {
    '"AuditType","Tab","Row","Name","PerkType","File","Style","Price","Requirements","CharType","Type","PrimaryStat","SecondaryStat","ScalingSource","DevStatus","Description","Details"' |
        Set-Content -Path $auditFullPath
}

$summary = $auditRows | Group-Object AuditType | Sort-Object Name | ForEach-Object {
    [pscustomobject]@{
        AuditType = $_.Name
        Count = $_.Count
    }
}

$summary | Format-Table -AutoSize
