param(
    [string]$BibleWorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx"
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
        $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
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

    if ($null -eq $Cell) {
        return ""
    }

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

function Get-CanonicalHeader {
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

function Get-WorksheetCell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    foreach ($cell in $RowNode.GetElementsByTagName("c", $namespace)) {
        if ((Get-OpenXmlColumnIndex $cell.GetAttribute("r")) -eq $ColumnIndex) {
            return $cell
        }
    }

    $rowNumber = [int]$RowNode.GetAttribute("r")
    $cellReference = "$(ConvertTo-OpenXmlColumnName $ColumnIndex)$rowNumber"
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

    return $cell
}

function Get-OrCreateRow {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$SheetData,
        [int]$RowNumber
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    foreach ($rowNode in $SheetData.GetElementsByTagName("row", $namespace)) {
        if ($rowNode.GetAttribute("r") -eq [string]$RowNumber) {
            return $rowNode
        }
    }

    $row = $WorksheetXml.CreateElement("row", $namespace)
    $row.SetAttribute("r", [string]$RowNumber)

    $insertBefore = $null
    foreach ($candidate in $SheetData.GetElementsByTagName("row", $namespace)) {
        $candidateNumberText = $candidate.GetAttribute("r")
        if (![string]::IsNullOrWhiteSpace($candidateNumberText) -and [int]$candidateNumberText -gt $RowNumber) {
            $insertBefore = $candidate
            break
        }
    }

    if ($null -eq $insertBefore) {
        [void]$SheetData.AppendChild($row)
    }
    else {
        [void]$SheetData.InsertBefore($row, $insertBefore)
    }

    return $row
}

function Clear-Cell {
    param([System.Xml.XmlElement]$Cell)

    $cellReference = $Cell.GetAttribute("r")
    $style = $Cell.GetAttribute("s")
    while ($Cell.FirstChild) {
        [void]$Cell.RemoveChild($Cell.FirstChild)
    }

    $Cell.RemoveAllAttributes()
    $Cell.SetAttribute("r", $cellReference)
    if (![string]::IsNullOrWhiteSpace($style)) {
        $Cell.SetAttribute("s", $style)
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
    $cell = Get-WorksheetCell -WorksheetXml $WorksheetXml -RowNode $RowNode -ColumnIndex $ColumnIndex
    Clear-Cell -Cell $cell

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return
    }

    $cell.SetAttribute("t", "inlineStr")
    $inlineString = $WorksheetXml.CreateElement("is", $namespace)
    $textElement = $WorksheetXml.CreateElement("t", $namespace)
    [void]$textElement.SetAttribute("space", "http://www.w3.org/XML/1998/namespace", "preserve")
    $textElement.InnerText = $Text
    [void]$inlineString.AppendChild($textElement)
    [void]$cell.AppendChild($inlineString)
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
    $cell = Get-WorksheetCell -WorksheetXml $WorksheetXml -RowNode $RowNode -ColumnIndex $ColumnIndex
    Clear-Cell -Cell $cell

    $formulaElement = $WorksheetXml.CreateElement("f", $namespace)
    $formulaElement.InnerText = $Formula
    [void]$cell.AppendChild($formulaElement)

    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $CachedValue
    [void]$cell.AppendChild($valueElement)
}

function Get-SheetContext {
    param(
        [System.IO.Compression.ZipArchive]$Zip,
        [xml]$WorkbookXml,
        [hashtable]$RelationshipsById,
        [System.Collections.Generic.IList[string]]$SharedStrings,
        [string]$SheetName
    )

    $workbookNamespace = [System.Xml.XmlNamespaceManager]::new($WorkbookXml.NameTable)
    $workbookNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $workbookNamespace.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")

    $sheetPath = $null
    foreach ($sheet in $WorkbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
        if ($sheet.GetAttribute("name") -ne $SheetName) {
            continue
        }

        $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $sheetPath = $RelationshipsById[$relationshipId]
        break
    }

    if ([string]::IsNullOrWhiteSpace($sheetPath)) {
        throw "Workbook sheet '$SheetName' was not found."
    }

    [xml]$worksheetXml = Read-ZipEntryText -Zip $Zip -EntryPath $sheetPath
    $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($worksheetXml.NameTable)
    $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    $sheetData = $worksheetXml.SelectSingleNode("//d:sheetData", $worksheetNamespace)
    if ($null -eq $sheetData) {
        throw "Workbook sheet '$SheetName' has no sheetData node."
    }

    $columnByHeader = @{}
    foreach ($rowNode in $worksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $cells = @{}
        foreach ($cell in $rowNode.SelectNodes("d:c", $worksheetNamespace)) {
            $columnIndex = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
            if ($columnIndex -gt 0) {
                $cells[$columnIndex] = Get-OpenXmlCellText -Cell $cell -SharedStrings $SharedStrings
            }
        }

        if (($cells.Values -join "|") -notmatch "Perk Name|PerkName") {
            continue
        }

        foreach ($cellEntry in $cells.GetEnumerator()) {
            $canonicalHeader = Get-CanonicalHeader $cellEntry.Value
            if (![string]::IsNullOrWhiteSpace($canonicalHeader) -and !$columnByHeader.ContainsKey($canonicalHeader)) {
                $columnByHeader[$canonicalHeader] = $cellEntry.Key
            }
        }

        break
    }

    foreach ($requiredHeader in @("Style", "Price", "PerkName", "SkillRequirements", "CharacterType", "Type", "Description", "PrimaryStat", "SecondaryStat", "ScalingSource", "FP", "STM", "CastingTime", "CooldownTime", "DevStatus", "AdditionalRequirements", "Notes")) {
        if (!$columnByHeader.ContainsKey($requiredHeader)) {
            throw "Workbook sheet '$SheetName' is missing required column '$requiredHeader'."
        }
    }

    return [pscustomobject]@{
        Path = $sheetPath
        Xml = $worksheetXml
        Namespace = $worksheetNamespace
        SheetData = $sheetData
        ColumnByHeader = $columnByHeader
    }
}

function Set-PerkRow {
    param(
        [object]$Context,
        [int]$RowNumber,
        [hashtable]$Values
    )

    $rowNode = Get-OrCreateRow -WorksheetXml $Context.Xml -SheetData $Context.SheetData -RowNumber $RowNumber

    foreach ($header in $Context.ColumnByHeader.Keys) {
        $value = ""
        if ($Values.ContainsKey($header)) {
            $value = [string]$Values[$header]
        }

        Set-CellText -WorksheetXml $Context.Xml -RowNode $rowNode -ColumnIndex $Context.ColumnByHeader[$header] -Text $value
    }
}

function Get-ExistingPerkRowNumber {
    param(
        [object]$Context,
        [System.Collections.Generic.IList[string]]$SharedStrings,
        [string]$PerkName
    )

    foreach ($rowNode in $Context.Xml.SelectNodes("//d:sheetData/d:row", $Context.Namespace)) {
        $cell = $null
        foreach ($candidate in $rowNode.SelectNodes("d:c", $Context.Namespace)) {
            if ((Get-OpenXmlColumnIndex $candidate.GetAttribute("r")) -eq $Context.ColumnByHeader["PerkName"]) {
                $cell = $candidate
                break
            }
        }

        if ((Get-OpenXmlCellText -Cell $cell -SharedStrings $SharedStrings) -eq $PerkName) {
            return [int]$rowNode.GetAttribute("r")
        }
    }

    return 0
}

function Set-PerkRowByNameOrStagingRow {
    param(
        [object]$Context,
        [System.Collections.Generic.IList[string]]$SharedStrings,
        [int]$StagingRowNumber,
        [hashtable]$Values
    )

    $perkName = [string]$Values["PerkName"]
    $existingRowNumber = Get-ExistingPerkRowNumber -Context $Context -SharedStrings $SharedStrings -PerkName $perkName
    $targetRowNumber = if ($existingRowNumber -gt 0) { $existingRowNumber } else { $StagingRowNumber }

    Set-PerkRow -Context $Context -RowNumber $targetRowNumber -Values $Values
}

function Set-FormulaByCell {
    param(
        [object]$Context,
        [int]$RowNumber,
        [int]$ColumnIndex,
        [string]$Formula,
        [string]$CachedValue
    )

    $rowNode = Get-OrCreateRow -WorksheetXml $Context.Xml -SheetData $Context.SheetData -RowNumber $RowNumber
    Set-CellFormula -WorksheetXml $Context.Xml -RowNode $rowNode -ColumnIndex $ColumnIndex -Formula $Formula -CachedValue $CachedValue
}

function Update-Dimension {
    param(
        [object]$Context,
        [int]$MinimumMaxRow
    )

    $dimensionNode = $Context.Xml.SelectSingleNode("//d:dimension", $Context.Namespace)
    if ($null -eq $dimensionNode) {
        return
    }

    $maxColumn = ($Context.ColumnByHeader.Values | Measure-Object -Maximum).Maximum
    $maxRow = $MinimumMaxRow
    foreach ($rowNode in $Context.Xml.SelectNodes("//d:sheetData/d:row", $Context.Namespace)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if (![string]::IsNullOrWhiteSpace($rowNumberText)) {
            $maxRow = [Math]::Max($maxRow, [int]$rowNumberText)
        }
    }

    $dimensionNode.SetAttribute("ref", "A1:$(ConvertTo-OpenXmlColumnName $maxColumn)$maxRow")
}

$forceRows = @(
    @{
        Row = 74
        Values = @{
            Style = "Alter"
            Price = "2"
            PerkName = "Throw Rock I"
            SkillRequirements = "Force 12"
            CharacterType = "Force"
            Type = "Combat"
            Alignment = "Light"
            AffinityShift = "+1"
            Description = "Hurls stone or loose debris with the Force up to 15m, dealing 18 physical DMG plus WIL/PER scaling to one target."
            PrimaryStat = "WIL"
            SecondaryStat = "PER"
            ScalingSource = "Combat Formula"
            FP = "3"
            STM = "-"
            CastingTime = "1.5 seconds"
            CooldownTime = "18 seconds"
            DevStatus = "Design Added"
            AdditionalRequirements = ""
            Notes = "Additive Light Alter kinetic DPS line. Higher direct damage than Force Push because it does not knock down or slow."
        }
    }
    @{
        Row = 75
        Values = @{
            Style = "Alter"
            Price = "3"
            PerkName = "Throw Rock II"
            SkillRequirements = "Force 30"
            CharacterType = "Force"
            Type = "Combat"
            Alignment = "Light"
            AffinityShift = "+1"
            Description = "Hurls a heavier stone or debris with the Force up to 15m, dealing 32 physical DMG plus WIL/PER scaling to one target."
            PrimaryStat = "WIL"
            SecondaryStat = "PER"
            ScalingSource = "Combat Formula"
            FP = "4"
            STM = "-"
            CastingTime = "1.5 seconds"
            CooldownTime = "18 seconds"
            DevStatus = "Design Added"
            AdditionalRequirements = ""
            Notes = "Replacement tier: raises direct kinetic damage without adding Dark-style self-sustain, Shock, or execute pressure."
        }
    }
    @{
        Row = 76
        Values = @{
            Style = "Alter"
            Price = "4"
            PerkName = "Throw Rock III"
            SkillRequirements = "Force 45"
            CharacterType = "Force"
            Type = "Combat"
            Alignment = "Light"
            AffinityShift = "+1"
            Description = "Hurls a crushing mass of stone and debris with the Force up to 15m, dealing 46 physical DMG plus WIL/PER scaling to one target."
            PrimaryStat = "WIL"
            SecondaryStat = "PER"
            ScalingSource = "Combat Formula"
            FP = "5"
            STM = "-"
            CastingTime = "1.5 seconds"
            CooldownTime = "18 seconds"
            DevStatus = "Design Added"
            AdditionalRequirements = ""
            Notes = "Replacement tier: final direct-damage rank stays below Dark's sustained damage and self-sustain pressure because it has no drain, Shock, or execute rider."
        }
    }
    @{
        Row = 77
        Values = @{
            Style = "Sense"
            Price = "1"
            PerkName = "Radiant Lance I"
            SkillRequirements = "Force 15"
            CharacterType = "Force"
            Type = "Combat"
            Alignment = "Light"
            AffinityShift = "+1"
            Description = "Fires a focused lance of radiant Force energy in an 8m line, dealing 12 force DMG plus WIL scaling to hostile targets in the line."
            PrimaryStat = "WIL"
            SecondaryStat = "None"
            ScalingSource = "Combat Formula"
            FP = "4"
            STM = "-"
            CastingTime = "1.5 seconds"
            CooldownTime = "24 seconds"
            DevStatus = "Design Added"
            AdditionalRequirements = ""
            Notes = "Additive Light Sense line attack. Shorter range and lower per-target damage than Throw Rock because it can hit multiple enemies."
        }
    }
    @{
        Row = 78
        Values = @{
            Style = "Sense"
            Price = "3"
            PerkName = "Radiant Lance II"
            SkillRequirements = "Force 32"
            CharacterType = "Force"
            Type = "Combat"
            Alignment = "Light"
            AffinityShift = "+1"
            Description = "Fires a focused lance of radiant Force energy in an 8m line, dealing 22 force DMG plus WIL scaling to hostile targets in the line."
            PrimaryStat = "WIL"
            SecondaryStat = "None"
            ScalingSource = "Combat Formula"
            FP = "5"
            STM = "-"
            CastingTime = "1.5 seconds"
            CooldownTime = "24 seconds"
            DevStatus = "Design Added"
            AdditionalRequirements = ""
            Notes = "Replacement tier: raises line damage without adding Dark-style drain, Shock, execute pressure, or self-sustain."
        }
    }
    @{
        Row = 79
        Values = @{
            Style = "Sense"
            Price = "4"
            PerkName = "Radiant Lance III"
            SkillRequirements = "Force 48"
            CharacterType = "Force"
            Type = "Combat"
            Alignment = "Light"
            AffinityShift = "+1"
            Description = "Fires a focused lance of radiant Force energy in an 8m line, dealing 32 force DMG plus WIL scaling to hostile targets in the line."
            PrimaryStat = "WIL"
            SecondaryStat = "None"
            ScalingSource = "Combat Formula"
            FP = "6"
            STM = "-"
            CastingTime = "1.5 seconds"
            CooldownTime = "30 seconds"
            DevStatus = "Design Added"
            AdditionalRequirements = ""
            Notes = "Replacement tier: final line rank keeps controlled Light-side pressure and stays below Dark's raw sustained damage package."
        }
    }
)

$deviceRows = @(
    @{
        Row = 82
        Values = @{
            Style = "Assault Gadgets"
            Price = "1"
            PerkName = "Arc Projector I"
            SkillRequirements = "Devices 12"
            CharacterType = "Standard"
            Type = "Combat"
            Description = "Projects a focused electrical arc up to 15m, dealing 18 electrical DMG plus PER scaling to one target."
            PrimaryStat = "PER"
            SecondaryStat = "None"
            ScalingSource = "Combat Formula"
            CrossSkill = ""
            FP = "-"
            STM = "3"
            CastingTime = "1 second"
            CooldownTime = "18 seconds"
            DevStatus = "Design Added"
            AdditionalRequirements = ""
            Notes = "Additive Devices DPS line added to keep Devices SP even with the additive Light Force Throw Rock line."
        }
    }
    @{
        Row = 83
        Values = @{
            Style = "Assault Gadgets"
            Price = "1"
            PerkName = "Arc Projector II"
            SkillRequirements = "Devices 30"
            CharacterType = "Standard"
            Type = "Combat"
            Description = "Projects a stronger electrical arc up to 15m, dealing 32 electrical DMG plus PER scaling to one target."
            PrimaryStat = "PER"
            SecondaryStat = "None"
            ScalingSource = "Combat Formula"
            CrossSkill = ""
            FP = "-"
            STM = "4"
            CastingTime = "1 second"
            CooldownTime = "18 seconds"
            DevStatus = "Design Added"
            AdditionalRequirements = ""
            Notes = "Replacement tier: direct electrical pressure without grenade consumables, fields, or beacon uptime."
        }
    }
    @{
        Row = 84
        Values = @{
            Style = "Assault Gadgets"
            Price = "2"
            PerkName = "Arc Projector III"
            SkillRequirements = "Devices 45"
            CharacterType = "Standard"
            Type = "Combat"
            Description = "Projects an overcharged electrical arc up to 15m, dealing 46 electrical DMG plus PER scaling to one target."
            PrimaryStat = "PER"
            SecondaryStat = "None"
            ScalingSource = "Combat Formula"
            CrossSkill = ""
            FP = "-"
            STM = "5"
            CastingTime = "1 second"
            CooldownTime = "18 seconds"
            DevStatus = "Design Added"
            AdditionalRequirements = ""
            Notes = "Replacement tier: mirrors the additive Throw Rock budget without replacing any existing Devices option."
        }
    }
    @{
        Row = 85
        Values = @{
            Style = "Assault Gadgets"
            Price = "1"
            PerkName = "Ion Lance I"
            SkillRequirements = "Devices 15"
            CharacterType = "Standard"
            Type = "Combat"
            Description = "Fires a focused ion beam from a wrist projector in an 8m line, dealing 12 electrical DMG plus PER scaling to hostile targets in the line."
            PrimaryStat = "PER"
            SecondaryStat = "None"
            ScalingSource = "Combat Formula"
            CrossSkill = ""
            FP = "-"
            STM = "4"
            CastingTime = "1 second"
            CooldownTime = "24 seconds"
            DevStatus = "Design Added"
            AdditionalRequirements = ""
            Notes = "Additive Devices line attack added to keep Devices SP even with the additive Radiant Lance line."
        }
    }
    @{
        Row = 86
        Values = @{
            Style = "Assault Gadgets"
            Price = "2"
            PerkName = "Ion Lance II"
            SkillRequirements = "Devices 32"
            CharacterType = "Standard"
            Type = "Combat"
            Description = "Fires a focused ion beam from a wrist projector in an 8m line, dealing 22 electrical DMG plus PER scaling to hostile targets in the line."
            PrimaryStat = "PER"
            SecondaryStat = "None"
            ScalingSource = "Combat Formula"
            CrossSkill = ""
            FP = "-"
            STM = "5"
            CastingTime = "1 second"
            CooldownTime = "24 seconds"
            DevStatus = "Design Added"
            AdditionalRequirements = ""
            Notes = "Replacement tier: raises line damage without grenade consumables, fields, or beacon uptime."
        }
    }
    @{
        Row = 87
        Values = @{
            Style = "Assault Gadgets"
            Price = "2"
            PerkName = "Ion Lance III"
            SkillRequirements = "Devices 48"
            CharacterType = "Standard"
            Type = "Combat"
            Description = "Fires a focused ion beam from a wrist projector in an 8m line, dealing 32 electrical DMG plus PER scaling to hostile targets in the line."
            PrimaryStat = "PER"
            SecondaryStat = "None"
            ScalingSource = "Combat Formula"
            CrossSkill = ""
            FP = "-"
            STM = "6"
            CastingTime = "1 second"
            CooldownTime = "30 seconds"
            DevStatus = "Design Added"
            AdditionalRequirements = ""
            Notes = "Replacement tier: mirrors the additive Radiant Lance budget without replacing any existing Devices option."
        }
    }
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
    if ($null -ne $zip.GetEntry("xl/sharedStrings.xml")) {
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

    $forceContext = Get-SheetContext -Zip $zip -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SharedStrings $sharedStrings -SheetName "Force"
    foreach ($row in $forceRows) {
        Set-PerkRowByNameOrStagingRow -Context $forceContext -SharedStrings $sharedStrings -StagingRowNumber ([int]$row.Row) -Values $row.Values
    }

    Write-ZipEntryXml -Zip $zip -EntryPath $forceContext.Path -Xml $forceContext.Xml

    $devicesContext = Get-SheetContext -Zip $zip -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SharedStrings $sharedStrings -SheetName "Devices"
    foreach ($row in $deviceRows) {
        Set-PerkRowByNameOrStagingRow -Context $devicesContext -SharedStrings $sharedStrings -StagingRowNumber ([int]$row.Row) -Values $row.Values
    }

    Write-ZipEntryXml -Zip $zip -EntryPath $devicesContext.Path -Xml $devicesContext.Xml
}
finally {
    $zip.Dispose()
}

$orderingScriptPath = Join-Path $PSScriptRoot "ApplyForceDevicesPerkOrdering.ps1"
if (Test-Path $orderingScriptPath) {
    & $orderingScriptPath -BibleWorkbookPath $BibleWorkbookPath
}

@(
    $forceRows | ForEach-Object {
        [pscustomobject]@{
            Sheet = "Force"
            PerkName = $_.Values.PerkName
            Price = $_.Values.Price
            Requirement = $_.Values.SkillRequirements
        }
    }
    $deviceRows | ForEach-Object {
        [pscustomobject]@{
            Sheet = "Devices"
            PerkName = $_.Values.PerkName
            Price = $_.Values.Price
            Requirement = $_.Values.SkillRequirements
        }
    }
) | Format-Table Sheet, PerkName, Price, Requirement -AutoSize

Write-Host "Upserted additive Force and Devices DPS rows in '$workbookPath' and normalized their category ordering."
