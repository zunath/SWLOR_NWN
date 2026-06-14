[CmdletBinding()]
param(
    [string]$BibleWorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx",
    [switch]$NormalizeOnly
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

function Get-WorkbookEntryPath {
    param([string]$RelationshipTarget)

    $target = $RelationshipTarget.Replace("\", "/").TrimStart("/")
    if ($target.StartsWith("xl/", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $target
    }

    return "xl/$target"
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

function ConvertTo-OpenXmlColumnName {
    param([int]$ColumnIndex)

    if ($ColumnIndex -lt 1) {
        throw "Column index must be positive."
    }

    $name = ""
    $index = $ColumnIndex
    while ($index -gt 0) {
        $index--
        $name = [char]([int][char]"A" + ($index % 26)) + $name
        $index = [math]::Floor($index / 26)
    }

    return $name
}

function Normalize-CellText {
    param([object]$Value)

    if ($null -eq $Value) {
        return ""
    }

    $text = [string]$Value
    $text = $text -replace "[ \t]+\r?\n", "`n"
    return $text.Trim()
}

function Get-OpenXmlCellText {
    param(
        [System.Xml.XmlElement]$Cell,
        [System.Collections.Generic.IList[string]]$SharedStrings
    )

    $cellType = $Cell.GetAttribute("t")
    if ($cellType -eq "inlineStr") {
        return Normalize-CellText $Cell.InnerText
    }

    $rawValue = $Cell.InnerText
    if ([string]::IsNullOrWhiteSpace($rawValue)) {
        return ""
    }

    if ($cellType -eq "s") {
        return Normalize-CellText $SharedStrings[[int]$rawValue]
    }

    return Normalize-CellText $rawValue
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
        "alignment" { return "Alignment" }
        "affinityshift" { return "AffinityShift" }
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
        default { return "" }
    }
}

function Set-CellText {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex,
        [string]$Text
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $rowNumber = [int]$RowNode.GetAttribute("r")
    $cellReference = "$(ConvertTo-OpenXmlColumnName $ColumnIndex)$rowNumber"
    $cell = $null

    foreach ($candidate in $RowNode.GetElementsByTagName("c", $namespace)) {
        if ($candidate.GetAttribute("r") -eq $cellReference) {
            $cell = $candidate
            break
        }
    }

    if ($null -eq $cell) {
        $cell = $WorksheetXml.CreateElement("c", $namespace)
        $cell.SetAttribute("r", $cellReference)

        $insertBefore = $null
        foreach ($candidate in $RowNode.GetElementsByTagName("c", $namespace)) {
            $candidateColumn = Get-OpenXmlColumnIndex $candidate.GetAttribute("r")
            if ($candidateColumn -gt $ColumnIndex) {
                $insertBefore = $candidate
                break
            }
        }

        if ($null -eq $insertBefore) {
            [void]$RowNode.AppendChild($cell)
        }
        else {
            [void]$RowNode.InsertBefore($cell, $insertBefore)
        }
    }

    while ($cell.FirstChild) {
        [void]$cell.RemoveChild($cell.FirstChild)
    }

    $cell.SetAttribute("t", "inlineStr")
    $inlineString = $WorksheetXml.CreateElement("is", $namespace)
    $textElement = $WorksheetXml.CreateElement("t", $namespace)
    [void]$textElement.SetAttribute("space", "http://www.w3.org/XML/1998/namespace", "preserve")
    $textElement.InnerText = $Text
    [void]$inlineString.AppendChild($textElement)
    [void]$cell.AppendChild($inlineString)
}

function Set-CellNumber {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex,
        [string]$NumberText
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $rowNumber = [int]$RowNode.GetAttribute("r")
    $cellReference = "$(ConvertTo-OpenXmlColumnName $ColumnIndex)$rowNumber"
    $cell = $null

    foreach ($candidate in $RowNode.GetElementsByTagName("c", $namespace)) {
        if ($candidate.GetAttribute("r") -eq $cellReference) {
            $cell = $candidate
            break
        }
    }

    if ($null -eq $cell) {
        $cell = $WorksheetXml.CreateElement("c", $namespace)
        $cell.SetAttribute("r", $cellReference)

        $insertBefore = $null
        foreach ($candidate in $RowNode.GetElementsByTagName("c", $namespace)) {
            $candidateColumn = Get-OpenXmlColumnIndex $candidate.GetAttribute("r")
            if ($candidateColumn -gt $ColumnIndex) {
                $insertBefore = $candidate
                break
            }
        }

        if ($null -eq $insertBefore) {
            [void]$RowNode.AppendChild($cell)
        }
        else {
            [void]$RowNode.InsertBefore($cell, $insertBefore)
        }
    }

    while ($cell.FirstChild) {
        [void]$cell.RemoveChild($cell.FirstChild)
    }

    [void]$cell.RemoveAttribute("t")
    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $NumberText
    [void]$cell.AppendChild($valueElement)
}

function Set-CellFormula {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex,
        [string]$Formula,
        [string]$CachedValue
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $rowNumber = [int]$RowNode.GetAttribute("r")
    $cellReference = "$(ConvertTo-OpenXmlColumnName $ColumnIndex)$rowNumber"
    $cell = $null

    foreach ($candidate in $RowNode.GetElementsByTagName("c", $namespace)) {
        if ($candidate.GetAttribute("r") -eq $cellReference) {
            $cell = $candidate
            break
        }
    }

    if ($null -eq $cell) {
        $cell = $WorksheetXml.CreateElement("c", $namespace)
        $cell.SetAttribute("r", $cellReference)

        $insertBefore = $null
        foreach ($candidate in $RowNode.GetElementsByTagName("c", $namespace)) {
            $candidateColumn = Get-OpenXmlColumnIndex $candidate.GetAttribute("r")
            if ($candidateColumn -gt $ColumnIndex) {
                $insertBefore = $candidate
                break
            }
        }

        if ($null -eq $insertBefore) {
            [void]$RowNode.AppendChild($cell)
        }
        else {
            [void]$RowNode.InsertBefore($cell, $insertBefore)
        }
    }

    while ($cell.FirstChild) {
        [void]$cell.RemoveChild($cell.FirstChild)
    }

    [void]$cell.RemoveAttribute("t")
    $formulaElement = $WorksheetXml.CreateElement("f", $namespace)
    $formulaElement.InnerText = $Formula
    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $CachedValue
    [void]$cell.AppendChild($formulaElement)
    [void]$cell.AppendChild($valueElement)
}

function Write-ZipEntryXml {
    param(
        [System.IO.Compression.ZipArchive]$Zip,
        [string]$EntryPath,
        [xml]$Xml
    )

    $entry = $Zip.GetEntry($EntryPath)
    if ($null -ne $entry) {
        $entry.Delete()
    }

    $newEntry = $Zip.CreateEntry($EntryPath)
    $stream = $newEntry.Open()
    try {
        $settings = [System.Xml.XmlWriterSettings]::new()
        $settings.Encoding = [System.Text.Encoding]::UTF8
        $settings.Indent = $false
        $writer = [System.Xml.XmlWriter]::Create($stream, $settings)
        try {
            $Xml.Save($writer)
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Get-OrCreateRow {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$SheetData,
        [int]$RowNumber
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"

    foreach ($rowNode in $SheetData.GetElementsByTagName("row", $namespace)) {
        if ([int]$rowNode.GetAttribute("r") -eq $RowNumber) {
            return $rowNode
        }
    }

    $newRow = $WorksheetXml.CreateElement("row", $namespace)
    $newRow.SetAttribute("r", [string]$RowNumber)

    $insertBefore = $null
    foreach ($rowNode in $SheetData.GetElementsByTagName("row", $namespace)) {
        $candidateRow = [int]$rowNode.GetAttribute("r")
        if ($candidateRow -gt $RowNumber) {
            $insertBefore = $rowNode
            break
        }
    }

    if ($null -eq $insertBefore) {
        [void]$SheetData.AppendChild($newRow)
    }
    else {
        [void]$SheetData.InsertBefore($newRow, $insertBefore)
    }

    return $newRow
}

function Update-RowValues {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [hashtable]$ColumnByHeader,
        [hashtable]$Values
    )

    foreach ($entry in $Values.GetEnumerator()) {
        if (!$ColumnByHeader.ContainsKey($entry.Key)) {
            throw "Workbook sheet 'Force' is missing required column '$($entry.Key)'."
        }

        if ($entry.Key -in @("Price", "FP", "STM") -and ![string]::IsNullOrWhiteSpace($entry.Value) -and $entry.Value -ne "-") {
            $number = [decimal]$entry.Value
            if ($number % 1 -eq 0) {
                Set-CellNumber -WorksheetXml $WorksheetXml -RowNode $RowNode -ColumnIndex $ColumnByHeader[$entry.Key] -NumberText ([string][int]$number)
            }
            else {
                Set-CellNumber -WorksheetXml $WorksheetXml -RowNode $RowNode -ColumnIndex $ColumnByHeader[$entry.Key] -NumberText ([string]$number)
            }
        }
        else {
            Set-CellText -WorksheetXml $WorksheetXml -RowNode $RowNode -ColumnIndex $ColumnByHeader[$entry.Key] -Text $entry.Value
        }
    }
}

function ConvertTo-WholeNumberText {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text) -or $Text -eq "-") {
        return $null
    }

    try {
        $number = [decimal]$Text
    }
    catch {
        return $null
    }

    if ($number % 1 -ne 0) {
        return $null
    }

    return [string][int]$number
}

function ConvertTo-AffinityShiftText {
    param([string]$Text)

    $wholeNumberText = ConvertTo-WholeNumberText $Text
    if ($null -eq $wholeNumberText) {
        return $null
    }

    $number = [int]$wholeNumberText
    if ($number -gt 0) {
        return "+$number"
    }

    return [string]$number
}

function ConvertTo-IntegerCostProse {
    param([string]$Text)

    if ([string]::IsNullOrEmpty($Text)) {
        return $Text
    }

    return [regex]::Replace($Text, "\b(FP|STM) ([0-9]+)\.0\b", '$1 $2')
}

function Get-WorksheetCell {
    param(
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    foreach ($cell in $RowNode.GetElementsByTagName("c", $namespace)) {
        if ((Get-OpenXmlColumnIndex $cell.GetAttribute("r")) -eq $ColumnIndex) {
            return $cell
        }
    }

    return $null
}

function Set-CellStyle {
    param(
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex,
        [string]$StyleIndex
    )

    $cell = Get-WorksheetCell -RowNode $RowNode -ColumnIndex $ColumnIndex
    if ($null -ne $cell) {
        $cell.SetAttribute("s", $StyleIndex)
    }
}

function Normalize-WorksheetIntegerFormatting {
    param(
        [xml]$WorksheetXml,
        [System.Collections.Generic.IList[string]]$SharedStrings
    )

    $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($WorksheetXml.NameTable)
    $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    $headerRowNumber = 0
    $columnByHeader = @{}
    foreach ($rowNode in $WorksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText)) {
            continue
        }

        $cells = @{}
        foreach ($cell in $rowNode.SelectNodes("d:c", $worksheetNamespace)) {
            $columnIndex = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
            if ($columnIndex -gt 0) {
                $cells[$columnIndex] = Get-OpenXmlCellText -Cell $cell -SharedStrings $SharedStrings
            }
        }

        if (($cells.Values -join "|") -match "Perk Name|PerkName") {
            $headerRowNumber = [int]$rowNumberText
            foreach ($cellEntry in $cells.GetEnumerator()) {
                $canonicalHeader = Get-CanonicalManifestHeader $cellEntry.Value
                if (![string]::IsNullOrWhiteSpace($canonicalHeader) -and !$columnByHeader.ContainsKey($canonicalHeader)) {
                    $columnByHeader[$canonicalHeader] = $cellEntry.Key
                }
            }
            break
        }
    }

    if ($headerRowNumber -eq 0) {
        return $false
    }

    $changedCellCount = 0
    foreach ($rowNode in $WorksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText) -or [int]$rowNumberText -le $headerRowNumber) {
            continue
        }

        foreach ($cell in $rowNode.SelectNodes("d:c", $worksheetNamespace)) {
            $cellText = Get-OpenXmlCellText -Cell $cell -SharedStrings $SharedStrings
            $normalizedCellText = ConvertTo-IntegerCostProse $cellText
            if ($normalizedCellText -ne $cellText) {
                $columnIndex = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
                if ($columnIndex -gt 0) {
                    Set-CellText -WorksheetXml $WorksheetXml -RowNode $rowNode -ColumnIndex $columnIndex -Text $normalizedCellText
                    $changedCellCount++
                }
            }
        }

        foreach ($integerHeader in @("Price", "FP", "STM")) {
            if ($columnByHeader.ContainsKey($integerHeader)) {
                $integerCell = Get-WorksheetCell -RowNode $rowNode -ColumnIndex $columnByHeader[$integerHeader]
                if ($null -ne $integerCell -and $null -eq $integerCell.SelectSingleNode("d:f", $worksheetNamespace)) {
                    $integerText = Get-OpenXmlCellText -Cell $integerCell -SharedStrings $SharedStrings
                    $wholeIntegerText = ConvertTo-WholeNumberText $integerText
                    if ($null -ne $wholeIntegerText -and ($integerText -ne $wholeIntegerText -or ![string]::IsNullOrWhiteSpace($integerCell.GetAttribute("t")))) {
                        Set-CellNumber -WorksheetXml $WorksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader[$integerHeader] -NumberText $wholeIntegerText
                        $changedCellCount++
                    }
                }
            }
        }

        if ($columnByHeader.ContainsKey("AffinityShift")) {
            $affinityCell = Get-WorksheetCell -RowNode $rowNode -ColumnIndex $columnByHeader["AffinityShift"]
            if ($null -ne $affinityCell -and $null -eq $affinityCell.SelectSingleNode("d:f", $worksheetNamespace)) {
                $affinityText = Get-OpenXmlCellText -Cell $affinityCell -SharedStrings $SharedStrings
                $normalizedAffinityText = ConvertTo-AffinityShiftText $affinityText
                if ($null -ne $normalizedAffinityText -and $affinityText -ne $normalizedAffinityText) {
                    Set-CellText -WorksheetXml $WorksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["AffinityShift"] -Text $normalizedAffinityText
                    $changedCellCount++
                }
            }
        }
    }

    return $changedCellCount
}

$forceRows = @(
    @{ Row = 8; PerkName = "Force Push I"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Notes = "Universal kinetic pressure. Lower direct damage than Dark Ravager attacks because it also controls movement. No affinity scaling." }
    @{ Row = 9; PerkName = "Throw Lightsaber I"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Price = "2"; SkillRequirements = "Force 8"; CharacterType = "Force"; Type = "Combat"; Description = "Hurls your equipped weapon with the Force up to 15m, dealing weapon DMG + 10 physical DMG plus WIL/PER scaling to one target."; PrimaryStat = "WIL"; SecondaryStat = "PER"; ScalingSource = "Combat Formula"; FP = "2"; STM = "1"; CastingTime = "1.5 seconds"; CooldownTime = "18 seconds"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Universal ranged weapon attack. Lower scaling than dedicated weapon skill strikes because it works with any equipped weapon and adds Force utility." }
    @{ Row = 10; PerkName = "Force Leap I"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Notes = "Universal mobility attack. No affinity scaling." }
    @{ Row = 11; PerkName = "Mind Trick I"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Notes = "Universal mind-control utility. No affinity scaling." }
    @{ Row = 12; PerkName = "Saber Rend I"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Description = "After using a damaging Force power, your next melee attack within 8 seconds deals +10 force DMG plus WIL scaling."; Notes = "Universal melee hybrid trait. Kept below dedicated weapon follow-up damage because it triggers from any damaging Force power." }
    @{ Row = 13; PerkName = "Mind Shroud I"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Description = "After using a damaging Force power, reduce your force damage taken by 5% and gain +10 Confusion Resistance rating, +10 Daze Resistance rating, and +10 Fear Resistance rating for 12 seconds."; Notes = "Universal defensive utility trait using resistance ratings rather than percentage resistance language." }
    @{ Row = 14; PerkName = "Precognition"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Price = "3"; CharacterType = "Force"; Type = "Trait"; Description = "After spending FP on a Force power, gain +5% Defense and +5% Evasion for 8 seconds. This can trigger once every 12 seconds."; PrimaryStat = "None"; SecondaryStat = "None"; ScalingSource = "Design Added"; FP = "-"; STM = "-"; CastingTime = "-"; CooldownTime = "-"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Universal combat trait that benefits any Force style without adding an active button or equipment requirement." }
    @{ Row = 15; PerkName = "Force Push II"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Price = "3"; Notes = "Replacement tier: selected target receives the full lower-rank effect. Universal kinetic pressure. No affinity scaling." }
    @{ Row = 16; PerkName = "Throw Lightsaber II"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Price = "4"; SkillRequirements = "Force 25"; CharacterType = "Force"; Type = "Combat"; Description = "Hurls your equipped weapon with the Force up to 15m, dealing weapon DMG + 20 physical DMG plus WIL/PER scaling to the selected target and one additional enemy along the path."; PrimaryStat = "WIL"; SecondaryStat = "PER"; ScalingSource = "Combat Formula"; FP = "3"; STM = "1"; CastingTime = "1.5 seconds"; CooldownTime = "18 seconds"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Replacement tier: selected target receives the full lower-rank effect. Lower bonus damage than dedicated weapon lines because this is ranged, universal, and can add secondary targets." }
    @{ Row = 17; PerkName = "Force Leap II"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Price = "4"; Notes = "Replacement tier: selected target receives the full lower-rank effect. Universal mobility attack. No affinity scaling." }
    @{ Row = 18; PerkName = "Mind Trick II"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Price = "4"; Notes = "Replacement tier: selected target receives the full lower-rank effect. Universal mind-control utility. No affinity scaling." }
    @{ Row = 19; PerkName = "Saber Rend II"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Price = "4"; Description = "After using a damaging Force power, your next melee attack within 8 seconds deals +20 force DMG plus WIL scaling."; Notes = "Replacement tier: raises the melee follow-up without overtaking dedicated weapon strike lines." }
    @{ Row = 20; PerkName = "Mind Shroud II"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Price = "4"; Description = "After using a damaging Force power, reduce your force damage taken by 10% and gain +15 Confusion Resistance rating, +15 Daze Resistance rating, and +15 Fear Resistance rating for 12 seconds."; Notes = "Replacement tier: stronger force protection using resistance ratings rather than percentage resistance language." }
    @{ Row = 21; PerkName = "Throw Lightsaber III"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Price = "5"; SkillRequirements = "Force 42"; CharacterType = "Force"; Type = "Combat"; Description = "Hurls your equipped weapon with the Force up to 15m, dealing weapon DMG + 30 physical DMG plus WIL/PER scaling to the selected target and up to two additional enemies along the path."; PrimaryStat = "WIL"; SecondaryStat = "PER"; ScalingSource = "Combat Formula"; FP = "4"; STM = "2"; CastingTime = "1.5 seconds"; CooldownTime = "18 seconds"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Replacement tier: selected target receives the full lower-rank effect. Multi-target ranged utility stays below top dedicated weapon single-target strikes." }
    @{ Row = 22; PerkName = "Force Push III"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Price = "4"; Notes = "Replacement tier: selected target receives the full lower-rank effect. Universal kinetic pressure. No affinity scaling." }
    @{ Row = 23; PerkName = "Force Flow"; Style = "Universal"; Alignment = "Universal"; AffinityShift = "0"; Price = "4"; SkillRequirements = "Force 50"; CharacterType = "Force"; Type = "Trait"; Description = "Damaging Force powers you use restore 1 FP when they hit at least one target. This can trigger once every 12 seconds."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Design Added"; FP = "-"; STM = "-"; CastingTime = "-"; CooldownTime = "-"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "New high-rank Universal trait added to bring the line to 55 SP without adding an active button. Uses a broad damaging Force power trigger so Light, Dark, and Universal damage builds can benefit." }

    @{ Row = 26; PerkName = "Guardian Ward I"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; Price = "3"; SkillRequirements = "-"; Notes = "Opening Light Guardian shield rank." }
    @{ Row = 27; PerkName = "Deflective Presence"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; Price = "4"; SkillRequirements = "Force 8"; Description = "Light Guardian combat powers grant affected allies +4 Attack Deflection for 10 seconds."; Notes = "Early support trait placed between Guardian Ward I and II so the Ward ranks progress across the tree." }
    @{ Row = 28; PerkName = "Soothing Guard I"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; Price = "4"; SkillRequirements = "Force 12"; Description = "Light Guardian powers that grant temporary HP, absorb damage, or prevent defeat remove one minor negative effect (Bleed, Poison, or Hobble) from affected allies and grant 10% physical damage reduction for 8 seconds."; Notes = "Light Guardian is limited to minor negative-effect cleanup. Standard and major negative-effect removal belongs to Light Consular or First Aid." }
    @{ Row = 29; PerkName = "Guardian Ward II"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; Price = "4"; SkillRequirements = "Force 18"; Notes = "Second shield rank moved out of the opening band so the Ward line has a real progression." }
    @{ Row = 30; PerkName = "Courageous Resolve"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; Price = "4"; SkillRequirements = "Force 22"; Description = "Light Guardian powers grant affected allies +10 Fear Resistance rating, +10 Daze Resistance rating, and +10 Confusion Resistance rating for 12 seconds. If the ally has temporary HP from a Light Guardian power, increase those ratings to +15."; Notes = "Guardian mental protection stays in the resistance lane." }
    @{ Row = 31; PerkName = "Force Intercept"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; SkillRequirements = "Force 25" }
    @{ Row = 32; PerkName = "Reflective Barrier"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; Price = "4"; SkillRequirements = "Force 30"; Description = "Light Guardian powers that grant temporary HP reflect 8% of force and energy damage taken, plus WIL scaling, back to the attacker while the temporary HP remains."; Notes = "Reflect value is lower than dedicated damage bonuses because it rides on defensive powers." }
    @{ Row = 33; PerkName = "Guardian Ward III"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; Price = "5"; SkillRequirements = "Force 35"; Notes = "Third shield rank lands in the mid-late tree instead of immediately after Guardian Ward II." }
    @{ Row = 34; PerkName = "Purifying Wave"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; SkillRequirements = "Force 38"; Description = "Releases a 5m wave of focused light, dealing 22 force DMG plus WIL scaling to enemies and removing one minor negative effect (Bleed, Poison, or Hobble) from nearby allies."; Notes = "Light Guardian's offensive pressure plus minor cleanup." }
    @{ Row = 35; PerkName = "Bastion of Light"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; SkillRequirements = "Force 42"; Description = "Light Guardian powers that grant temporary HP, absorb damage, or prevent defeat also grant +8 Defense and +8 Force Defense for 12 seconds. If the ally is below 50% HP, this bonus becomes +12."; Notes = "Defensive trait that reinforces Guardian Ward and Last Stand without adding another active button." }
    @{ Row = 36; PerkName = "Guardian Ward IV"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; Price = "5"; SkillRequirements = "Force 45"; Notes = "Final regular Ward rank is delayed into the upper tree before the Force 50 capstones." }
    @{ Row = 37; PerkName = "Last Stand of the Light"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; SkillRequirements = "Force 50" }
    @{ Row = 38; PerkName = "Guardian's Mercy"; Style = "Light Guardian"; Alignment = "Light"; AffinityShift = "+1"; Price = "5"; SkillRequirements = "Force 50"; CharacterType = "Force"; Type = "Trait"; Description = "Light Guardian powers that grant temporary HP, absorb damage, cleanse minor negative effects, or prevent defeat also grant +10 Trauma Resistance rating and +5 Guard for 15 seconds."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Design Added"; FP = "-"; STM = "-"; CastingTime = "-"; CooldownTime = "-"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Keeps the trait Guardian-specific through Trauma Resistance and Guard." }

    @{ Row = 41; PerkName = "Benevolence I"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1" }
    @{ Row = 42; PerkName = "Force Judgment I"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1"; Price = "3"; SkillRequirements = "Force 5"; CharacterType = "Force"; Type = "Combat"; Description = "Deals 14 force DMG plus WIL scaling to one target and reduces outgoing weapon and force damage by 4% for 12 seconds."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Combat Formula"; FP = "2"; STM = "-"; CastingTime = "1.5 seconds"; CooldownTime = "18 seconds"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Light-side offensive pressure that still reads as restraint rather than raw Dark damage." }
    @{ Row = 43; PerkName = "Renewal I"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1" }
    @{ Row = 44; PerkName = "Serene Focus"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1"; Price = "3"; SkillRequirements = "Force 12"; CharacterType = "Force"; Type = "Trait"; Description = "Light Consular powers that restore HP cause affected allies to restore 1 STM and 1 FP every 6 seconds for 12 seconds. This benefit does not trigger when you target yourself."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Design Added"; FP = "-"; STM = "-"; CastingTime = "-"; CooldownTime = "-"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Defines the resource cadence explicitly and avoids self-target FP loops." }
    @{ Row = 45; PerkName = "Benevolence II"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1" }
    @{ Row = 46; PerkName = "Renewal II"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1" }
    @{ Row = 47; PerkName = "Force Judgment II"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1"; Price = "4"; SkillRequirements = "Force 28"; CharacterType = "Force"; Type = "Combat"; Description = "Deals 24 force DMG plus WIL scaling to the selected target and one nearby enemy, reducing outgoing weapon and force damage by 6% for 12 seconds."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Combat Formula"; FP = "3"; STM = "-"; CastingTime = "1.5 seconds"; CooldownTime = "18 seconds"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Replacement tier: selected target receives the full lower-rank effect before the nearby enemy is added." }
    @{ Row = 48; PerkName = "Force Mend"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1"; Description = "Light Consular powers that restore HP can remove one standard negative effect from the target and restore HP equal to 10% of maximum HP plus WIL scaling. This can trigger once every 24 seconds per target."; Notes = "Moves meaningful cleanse support to Light Consular while keeping the bonus heal below dedicated healing actives." }
    @{ Row = 49; PerkName = "Guided Judgment"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1"; Price = "4"; SkillRequirements = "Force 38"; CharacterType = "Force"; Type = "Trait"; Description = "Force Judgment I-III gain +5% Accuracy and reduce affected targets' Evasion by 5% for 12 seconds."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Design Added"; FP = "-"; STM = "-"; CastingTime = "-"; CooldownTime = "-"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Offensive Light-side support trait for the Force Judgment line." }
    @{ Row = 50; PerkName = "Force Sanctuary"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1"; Type = "Combat"; Description = "Creates a 4m sanctuary for 18 seconds. Allies inside gain regeneration equal to 2% of maximum HP plus WIL scaling every 3 seconds and take 5% less force damage."; FP = "6"; STM = "-"; CastingTime = "1.5 seconds"; CooldownTime = "90 seconds"; Notes = "Restored as Light Consular's dedicated AoE healing active." }
    @{ Row = 51; PerkName = "Benevolence III"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1" }
    @{ Row = 52; PerkName = "Renewal III"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1" }
    @{ Row = 53; PerkName = "Force Judgment III"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1"; Price = "4"; SkillRequirements = "Force 48"; CharacterType = "Force"; Type = "Combat"; Description = "Deals 36 force DMG plus WIL scaling to the selected target and nearby enemies, reducing outgoing weapon and force damage by 8% for 12 seconds."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Combat Formula"; FP = "4"; STM = "-"; CastingTime = "1.5 seconds"; CooldownTime = "24 seconds"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Replacement tier: selected target receives the full lower-rank effect before nearby enemies are added." }
    @{ Row = 54; PerkName = "Judgment Focus"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1"; Price = "5"; SkillRequirements = "Force 50"; CharacterType = "Force"; Type = "Trait"; Description = "Force Judgment I-III deal 8% more DMG and their outgoing damage reduction lasts 4 seconds longer."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Design Added"; FP = "-"; STM = "-"; CastingTime = "-"; CooldownTime = "-"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Offensive Light-side capstone trait tuned below Dark Ravager's dedicated damage multipliers." }
    @{ Row = 55; PerkName = "Harmonic Restoration"; Style = "Light Consular"; Alignment = "Light"; AffinityShift = "+1"; Price = "4"; SkillRequirements = "Force 50"; CharacterType = "Force"; Type = "Trait"; Description = "Light Consular powers that restore HP grant +8 Trauma Resistance rating for 15 seconds. Force Judgment I-III deal 6% more DMG to targets affected by your outgoing damage reduction."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Design Added"; FP = "-"; STM = "-"; CastingTime = "-"; CooldownTime = "-"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Keeps the trait split between restorative resilience and Light-side offense without stacking too high with Judgment Focus." }

    @{ Row = 58; PerkName = "Force Spark I"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Description = "Deals 16 force DMG plus WIL scaling to one target and reduces Evasion by 4% for 20 seconds." }
    @{ Row = 59; PerkName = "Force Body I"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Type = "Combat"; Description = "Sacrifice HP equal to 20% of your maximum HP to restore 15% of your maximum FP. This cannot reduce you below 1 HP."; FP = "-"; STM = "-"; CastingTime = "1 second"; CooldownTime = "60 seconds"; Notes = "Activated HP-for-FP conversion with a cooldown so it cannot feed low-HP survival loops." }
    @{ Row = 60; PerkName = "Force Lightning I"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Description = "Deals 10 force DMG plus WIL scaling to one target, then arcs to up to two nearby enemies for 50% damage. Affected targets suffer Shock for 6 seconds."; Notes = "Chain pressure line: lower primary damage than Force Drain, but adds multi-target Shock pressure." }
    @{ Row = 61; PerkName = "Force Drain I"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Description = "Deals 14 force DMG plus WIL scaling to one target and heals you for 30% of damage dealt. If the target is below 50% HP, healing increases to 40%."; Notes = "Single-target life siphon line: sustain-focused rather than chain damage." }
    @{ Row = 62; PerkName = "Fury Stance I"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1" }
    @{ Row = 63; PerkName = "Force Spark II"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Price = "3"; Description = "Deals 30 force DMG plus WIL scaling to one target and reduces Evasion by 6% for 20 seconds." }
    @{ Row = 64; PerkName = "Force Lightning II"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Description = "Deals 18 force DMG plus WIL scaling to one target, then arcs to up to three nearby enemies for 50% damage. Affected targets suffer Shock for 8 seconds."; Notes = "Replacement tier: stronger chain pressure and longer Shock duration, still distinct from Force Drain's single-target sustain." }
    @{ Row = 65; PerkName = "Force Drain II"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Description = "Deals 24 force DMG plus WIL scaling to one target and heals you for 35% of damage dealt. If the target is below 50% HP, healing increases to 45%."; Notes = "Replacement tier: improves single-target siphon damage and low-health sustain." }
    @{ Row = 66; PerkName = "Devouring Strike"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Description = "Dark Ravager attacks and damaging Dark Force powers deal 15% more damage to targets below 35% HP."; Notes = "Execute bonus now matches the broader perk ecosystem instead of exceeding other low-health damage traits." }
    @{ Row = 67; PerkName = "Force Body II"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Type = "Combat"; Description = "Sacrifice HP equal to 15% of your maximum HP to restore 15% of your maximum FP. This cannot reduce you below 1 HP."; FP = "-"; STM = "-"; CastingTime = "1 second"; CooldownTime = "60 seconds"; Notes = "Replacement tier: lower HP cost than Force Body I. Uses a concrete HP cost and avoids low-HP survival loops." }
    @{ Row = 68; PerkName = "Force Maelstrom"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Type = "Trait"; Description = "Force Lightning I-II and Hunger of the Dark also batter affected enemies with unstable force pressure, reducing Evasion by 5% for 12 seconds."; FP = "-"; STM = "-"; CastingTime = "-"; CooldownTime = "-"; Notes = "Changed from a creature-pull active to a trait so it does not rely on movement displacement behavior." }
    @{ Row = 69; PerkName = "Force Drain III"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Description = "Deals 36 force DMG plus WIL scaling to one target and heals you for 40% of damage dealt. If the target is below 50% HP, healing increases to 50%."; Notes = "Replacement tier: top life siphon rank remains single-target so it does not overlap with Force Lightning's chain role." }
    @{ Row = 70; PerkName = "Fury Stance II"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1" }
    @{ Row = 71; PerkName = "Force Spark III"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Price = "4"; Description = "Deals 44 force DMG plus WIL scaling to one target and reduces Evasion by 8% for 20 seconds." }
    @{ Row = 72; PerkName = "Hunger of the Dark"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Description = "For 45 seconds, Dark damage you deal heals you for 12% of damage dealt and defeated enemies restore 3 FP."; Notes = "Dark sustain capstone remains strong but no longer stacks into heavy self-healing loops as aggressively." }
    @{ Row = 73; PerkName = "Overflowing Hunger"; Style = "Dark Ravager"; Alignment = "Dark"; AffinityShift = "-1"; Price = "4"; SkillRequirements = "Force 50"; CharacterType = "Force"; Type = "Trait"; Description = "When a Dark Ravager power would restore your HP while you are already at full HP, restore 1 FP and gain +5% Dark Force DMG for 10 seconds. This can trigger once every 12 seconds."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Design Added"; FP = "-"; STM = "-"; CastingTime = "-"; CooldownTime = "-"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Overheal conversion trait for Force Drain and Hunger of the Dark with a bounded resource return." }

    @{ Row = 76; PerkName = "Creeping Terror I"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1"; Description = "Creates a visible 5m field within 15m for 12 seconds. Enemies inside are Hobbled and take 10 force DMG plus WIL scaling every 3 seconds."; Notes = "Reworked into a placed area denial field with direct pulses instead of a hit-gated PBAoE." }
    @{ Row = 77; PerkName = "Force Choke I"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1"; Price = "2"; SkillRequirements = "Force 5"; CharacterType = "Force"; Type = "Combat"; Description = "Immobilizes one target for 2 seconds, interrupts activation, and deals 8 force DMG plus WIL scaling over the duration."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Combat Formula"; FP = "2"; STM = "-"; CastingTime = "1.5 seconds"; CooldownTime = "36 seconds"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Single-target control line; damage stays below Dark Ravager because the immobilize and interrupt are the primary value." }
    @{ Row = 78; PerkName = "Weaken Resolve I"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1" }
    @{ Row = 79; PerkName = "Dark Bargain I"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1"; Price = "3"; SkillRequirements = "Force 12"; CharacterType = "Force"; Type = "Combat"; Description = "Sacrifice HP equal to 8% of your maximum HP to empower one ally for 15 seconds, granting +5% weapon and force DMG and restoring 1 FP and 1 STM every 5 seconds."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Design Added"; FP = "-"; STM = "-"; CastingTime = "1.5 seconds"; CooldownTime = "45 seconds"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Dark-side support active that gives the tree a non-damage ally tool without overtaking Leadership damage commands." }
    @{ Row = 80; PerkName = "Creeping Terror II"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1"; Description = "Creates a visible 5m field within 15m for 15 seconds. Enemies inside are Hobbled and take 14 force DMG plus WIL scaling every 3 seconds."; Notes = "Replacement tier: longer duration and higher pulse damage." }
    @{ Row = 81; PerkName = "Force Choke II"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1"; Price = "3"; SkillRequirements = "Force 22"; CharacterType = "Force"; Type = "Combat"; Description = "Immobilizes one target for 3 seconds, interrupts activation, and deals 16 force DMG plus WIL scaling over the duration."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Combat Formula"; FP = "3"; STM = "-"; CastingTime = "1.5 seconds"; CooldownTime = "36 seconds"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Replacement tier: increases duration and total damage without adding targets." }
    @{ Row = 82; PerkName = "Nightmare Field"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1" }
    @{ Row = 83; PerkName = "Weaken Resolve II"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1" }
    @{ Row = 84; PerkName = "Force Choke III"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1"; Price = "4"; SkillRequirements = "Force 30"; CharacterType = "Force"; Type = "Combat"; Description = "Immobilizes one target for 4 seconds, interrupts activation, and deals 24 force DMG plus WIL scaling over the duration."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Combat Formula"; FP = "4"; STM = "-"; CastingTime = "1.5 seconds"; CooldownTime = "36 seconds"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Replacement tier: makes the third rank part of the active Force Choke line instead of a dependent trait." }
    @{ Row = 85; PerkName = "Dark Bargain II"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1"; Price = "3"; SkillRequirements = "Force 38"; CharacterType = "Force"; Type = "Combat"; Description = "Sacrifice HP equal to 10% of your maximum HP to empower nearby allies for 15 seconds, granting +7% weapon and force DMG and restoring 1 FP and 1 STM every 5 seconds."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Design Added"; FP = "-"; STM = "-"; CastingTime = "1.5 seconds"; CooldownTime = "60 seconds"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Replacement tier: expands Dark Bargain into a nearby ally support effect while staying below dedicated Leadership party buffs." }
    @{ Row = 86; PerkName = "Shared Suffering"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1"; Price = "4"; SkillRequirements = "Force 40"; CharacterType = "Force"; Type = "Trait"; Description = "Dark Bargain I-II also grant affected allies +8 Trauma Resistance rating and +8 Mind Resistance rating for their duration."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Design Added"; FP = "-"; STM = "-"; CastingTime = "-"; CooldownTime = "-"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Support trait that lets Dark Bargain add defensive value." }
    @{ Row = 87; PerkName = "Creeping Terror III"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1"; Description = "Creates a visible 5m field within 15m for 18 seconds. Enemies inside are Hobbled and take 18 force DMG plus WIL scaling every 3 seconds."; Notes = "Replacement tier: larger area, longer duration, and higher pulse damage." }
    @{ Row = 88; PerkName = "Collapse Will"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1" }
    @{ Row = 89; PerkName = "Force Choke IV"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1"; Price = "4"; SkillRequirements = "Force 48"; CharacterType = "Force"; Type = "Combat"; Description = "Immobilizes one target for 5 seconds, interrupts activation, and deals 34 force DMG plus WIL scaling over the duration."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Combat Formula"; FP = "5"; STM = "-"; CastingTime = "1.5 seconds"; CooldownTime = "36 seconds"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Replacement tier: increases duration and total damage without adding a second target." }
    @{ Row = 90; PerkName = "Eclipse of Resolve"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1"; Description = "Nearby enemies suffer -12% physical and Force ability Accuracy, -12% Evasion, and +20% FP and STM costs for 30 seconds."; Notes = "Capstone debuff remains broad, but its duration and penalties are below Leadership's major command windows." }
    @{ Row = 91; PerkName = "Dread Certainty"; Style = "Dark Manipulator"; Alignment = "Dark"; AffinityShift = "-1"; Price = "4"; SkillRequirements = "Force 50"; CharacterType = "Force"; Type = "Trait"; Description = "When your Dark Manipulator power immobilizes, Hobbles, applies Weaken Resolve, applies Exposed, or applies Force Erosion, the target also suffers -5 Trauma Resistance rating and -5 Mind Resistance rating for 12 seconds."; PrimaryStat = "WIL"; SecondaryStat = "None"; ScalingSource = "Design Added"; FP = "-"; STM = "-"; CastingTime = "-"; CooldownTime = "-"; DevStatus = "Design Added"; AdditionalRequirements = ""; Notes = "Names exact statuses and debuffs instead of using control-ability shorthand." }
)

$forceDataRows = 8..93

$forceTotalRows = @(
    @{ Row = 24; Formula = "SUM(B8:B23)"; Value = "55" }
    @{ Row = 39; Formula = "SUM(B26:B38)"; Value = "55" }
    @{ Row = 56; Formula = "SUM(B41:B55)"; Value = "55" }
    @{ Row = 74; Formula = "SUM(B58:B73)"; Value = "55" }
    @{ Row = 92; Formula = "SUM(B76:B91)"; Value = "55" }
)

$workbookPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookPath)) {
    throw "Workbook '$workbookPath' was not found."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($workbookPath, [System.IO.Compression.ZipArchiveMode]::Update)

try {
    $sharedStrings = New-Object System.Collections.Generic.List[string]
    $sharedStringsEntry = $zip.GetEntry("xl/sharedStrings.xml")
    if ($null -ne $sharedStringsEntry) {
        [xml]$sharedStringsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/sharedStrings.xml"
        $sharedStringsNamespace = [System.Xml.XmlNamespaceManager]::new($sharedStringsXml.NameTable)
        $sharedStringsNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

        foreach ($stringItem in $sharedStringsXml.SelectNodes("//d:sst/d:si", $sharedStringsNamespace)) {
            $sharedStrings.Add((Normalize-CellText $stringItem.InnerText)) | Out-Null
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

    $forceSheetPath = $null
    foreach ($sheet in $workbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
        if ($sheet.GetAttribute("name") -ne "Force") {
            continue
        }

        $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $forceSheetPath = $relationshipsById[$relationshipId]
        break
    }

    if ([string]::IsNullOrWhiteSpace($forceSheetPath)) {
        throw "Workbook sheet 'Force' was not found."
    }

    [xml]$worksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $forceSheetPath
    $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($worksheetXml.NameTable)
    $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    $sheetData = $worksheetXml.SelectSingleNode("//d:sheetData", $worksheetNamespace)
    if ($null -eq $sheetData) {
        throw "Workbook sheet 'Force' has no sheetData node."
    }

    $headerRowNumber = 0
    $columnByHeader = @{}
    $rowByNumber = @{}

    foreach ($rowNode in $worksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText)) {
            continue
        }

        $rowNumber = [int]$rowNumberText
        $rowByNumber[$rowNumber] = $rowNode

        $cells = @{}
        foreach ($cell in $rowNode.SelectNodes("d:c", $worksheetNamespace)) {
            $columnIndex = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
            if ($columnIndex -gt 0) {
                $cells[$columnIndex] = Get-OpenXmlCellText -Cell $cell -SharedStrings $sharedStrings
            }
        }

        if ($headerRowNumber -eq 0 -and (($cells.Values -join "|") -match "Perk Name|PerkName")) {
            $headerRowNumber = $rowNumber
            foreach ($cellEntry in $cells.GetEnumerator()) {
                $canonicalHeader = Get-CanonicalManifestHeader $cellEntry.Value
                if (![string]::IsNullOrWhiteSpace($canonicalHeader) -and !$columnByHeader.ContainsKey($canonicalHeader)) {
                    $columnByHeader[$canonicalHeader] = $cellEntry.Key
                }
            }
        }
    }

    if ($headerRowNumber -eq 0) {
        throw "Workbook sheet 'Force' header row was not found."
    }

    foreach ($requiredHeader in @("Style", "PerkName", "SkillRequirements", "CharacterType", "Type", "Alignment", "AffinityShift", "Description", "PrimaryStat", "SecondaryStat", "ScalingSource", "FP", "STM", "CastingTime", "CooldownTime", "DevStatus", "AdditionalRequirements", "Notes")) {
        if (!$columnByHeader.ContainsKey($requiredHeader)) {
            throw "Workbook sheet 'Force' is missing required column '$requiredHeader'."
        }
    }

    $updatedRows = New-Object System.Collections.Generic.List[object]

    if (!$NormalizeOnly) {
        $rowValuesByPerkName = @{}
        foreach ($rowNode in $worksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
            $rowNumberText = $rowNode.GetAttribute("r")
            if ([string]::IsNullOrWhiteSpace($rowNumberText) -or [int]$rowNumberText -le $headerRowNumber) {
                continue
            }

            $values = @{}
            foreach ($headerEntry in $columnByHeader.GetEnumerator()) {
                $values[$headerEntry.Key] = ""
            }

            foreach ($cell in $rowNode.SelectNodes("d:c", $worksheetNamespace)) {
                $columnIndex = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
                $header = $columnByHeader.GetEnumerator() |
                    Where-Object { $_.Value -eq $columnIndex } |
                    Select-Object -First 1

                if ($null -ne $header) {
                    $values[$header.Key] = Get-OpenXmlCellText -Cell $cell -SharedStrings $sharedStrings
                }
            }

            $perkName = $values["PerkName"]
            if (![string]::IsNullOrWhiteSpace($perkName)) {
                $rowValuesByPerkName[$perkName] = $values
            }
        }

        foreach ($rowNumber in $forceDataRows) {
            $rowNode = Get-OrCreateRow -WorksheetXml $worksheetXml -SheetData $sheetData -RowNumber $rowNumber
            $blankValues = @{}
            foreach ($header in $columnByHeader.Keys) {
                $blankValues[$header] = ""
            }

            Update-RowValues -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnByHeader $columnByHeader -Values $blankValues
        }

        foreach ($forceRow in $forceRows) {
            $perkName = $forceRow.PerkName
            $values = @{}
            if ($rowValuesByPerkName.ContainsKey($perkName)) {
                foreach ($entry in $rowValuesByPerkName[$perkName].GetEnumerator()) {
                    $values[$entry.Key] = $entry.Value
                }
            }

            $values["PerkName"] = $perkName
            foreach ($entry in $forceRow.GetEnumerator()) {
                if ($entry.Key -in @("Row", "PerkName")) {
                    continue
                }

                $values[$entry.Key] = $entry.Value
            }

            $rowNode = Get-OrCreateRow -WorksheetXml $worksheetXml -SheetData $sheetData -RowNumber ([int]$forceRow.Row)
            Update-RowValues -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnByHeader $columnByHeader -Values $values
            $updatedRows.Add([pscustomobject]@{ Row = [int]$forceRow.Row; Style = $values["Style"]; PerkName = $perkName }) | Out-Null
        }

        foreach ($totalRow in $forceTotalRows) {
            $rowNode = Get-OrCreateRow -WorksheetXml $worksheetXml -SheetData $sheetData -RowNumber ([int]$totalRow.Row)
            Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["Style"] -Text "Total"
            Set-CellFormula -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["Price"] -Formula $totalRow.Formula -CachedValue $totalRow.Value
            Set-CellStyle -RowNode $rowNode -ColumnIndex $columnByHeader["Style"] -StyleIndex "2"
            Set-CellStyle -RowNode $rowNode -ColumnIndex $columnByHeader["Price"] -StyleIndex "2"
            $updatedRows.Add([pscustomobject]@{ Row = [int]$totalRow.Row; Style = "Total"; PerkName = "" }) | Out-Null
        }

        $topRow = Get-OrCreateRow -WorksheetXml $worksheetXml -SheetData $sheetData -RowNumber 4
        Set-CellFormula -WorksheetXml $worksheetXml -RowNode $topRow -ColumnIndex 4 -Formula "SUM(B24,B39,B56,B74,B92)" -CachedValue "275"

        $dimensionNode = $worksheetXml.SelectSingleNode("//d:dimension", $worksheetNamespace)
        if ($null -ne $dimensionNode) {
            $maxColumn = ($columnByHeader.Values | Measure-Object -Maximum).Maximum
            $maxRow = [Math]::Max(93, (($rowByNumber.Keys + @(93)) | Measure-Object -Maximum).Maximum)
            $dimensionNode.SetAttribute("ref", "A1:$(ConvertTo-OpenXmlColumnName $maxColumn)$maxRow")
        }
    }

    $normalizedCellCount = 0
    $normalizedCellCount += Normalize-WorksheetIntegerFormatting -WorksheetXml $worksheetXml -SharedStrings $sharedStrings
    Write-ZipEntryXml -Zip $zip -EntryPath $forceSheetPath -Xml $worksheetXml

    foreach ($sheet in $workbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
        $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $sheetPath = $relationshipsById[$relationshipId]
        if ($sheetPath -eq $forceSheetPath) {
            continue
        }

        [xml]$otherWorksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $sheetPath
        $sheetNormalizedCellCount = Normalize-WorksheetIntegerFormatting -WorksheetXml $otherWorksheetXml -SharedStrings $sharedStrings
        if ($sheetNormalizedCellCount -gt 0) {
            Write-ZipEntryXml -Zip $zip -EntryPath $sheetPath -Xml $otherWorksheetXml
            $normalizedCellCount += $sheetNormalizedCellCount
        }
    }

    Write-Host "Normalized $normalizedCellCount integer-format cells."
}
finally {
    $zip.Dispose()
}

if (!$NormalizeOnly) {
    $updatedRows |
        Sort-Object Row |
        Format-Table Row, PerkName -AutoSize

    Write-Host "Updated Force universal rows in '$workbookPath'."
}
else {
    Write-Host "Normalized workbook integer formatting in '$workbookPath'."
}
