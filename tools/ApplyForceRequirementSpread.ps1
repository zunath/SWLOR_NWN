param(
    [string]$BibleWorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx"
)

# 2026-06-11 Force requirement-spread pass.
# Balances Force skill requirements across Light, Dark, and Universal lines:
# - Each alignment gets a rank-0 entry: Force Spark I (Dark DPS), Throw Rock I
#   (Light DPS, moved from Force 12), Benevolence I (Light support), and
#   Weaken Resolve I (Universal debuff, moved from Force 8), so both Light and
#   Dark players can deal damage and progress from rank 0.
# - Requirements are spread evenly across the 0-50 grid: Alter fills every
#   step with at most two rows (previously three rows each at 28, 38, and 48
#   with nothing at 2, 10, 20, 32, or 40-42), Control closes its 20-to-30 gap
#   and breaks up its triple at 45, and Sense's 16 perks land on 16 distinct
#   steps (previously four rows at 25).
# - Line rank ordering is preserved (Spark 0/18/42, Throw Rock 0/18/40,
#   Choke 8/20/30/48, Guardian Ward 2/15/35/45, Judgment 5/25/45, and so on)
#   and every row stays inside its SP price band, so no prices change and
#   Force remains 240 SP, equal to Devices.
# Rows are rewritten in requirement order within each section, mirroring
# tools/ApplyForceDevicesPerkOrdering.ps1. The Devices sheet is not touched.

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

    $valueNode = $Cell.GetElementsByTagName("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main") | Select-Object -First 1
    if ($null -eq $valueNode -or [string]::IsNullOrWhiteSpace($valueNode.InnerText)) {
        return ""
    }

    if ($cellType -eq "s") {
        return Normalize-CellText $SharedStrings[[int]$valueNode.InnerText]
    }

    return Normalize-CellText $valueNode.InnerText
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
        if ((Get-OpenXmlColumnIndex $candidate.GetAttribute("r")) -gt $ColumnIndex) {
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
        SharedStrings = $SharedStrings
    }
}

function Get-PerkRowsByName {
    param([object]$Context)

    $rowsByName = @{}
    foreach ($rowNode in $Context.Xml.SelectNodes("//d:sheetData/d:row", $Context.Namespace)) {
        $rowValues = @{}
        foreach ($header in $Context.ColumnByHeader.Keys) {
            $cell = $null
            foreach ($candidate in $rowNode.SelectNodes("d:c", $Context.Namespace)) {
                if ((Get-OpenXmlColumnIndex $candidate.GetAttribute("r")) -eq $Context.ColumnByHeader[$header]) {
                    $cell = $candidate
                    break
                }
            }

            $rowValues[$header] = Get-OpenXmlCellText -Cell $cell -SharedStrings $Context.SharedStrings
        }

        $perkName = $rowValues["PerkName"]
        $price = $rowValues["Price"]
        if ([string]::IsNullOrWhiteSpace($perkName) -or [string]::IsNullOrWhiteSpace($price) -or $perkName -eq "Perk Name" -or $perkName -eq "PerkName") {
            continue
        }

        if (!$rowsByName.ContainsKey($perkName)) {
            $rowsByName[$perkName] = $rowValues
        }
    }

    return $rowsByName
}

function Clear-PerkRows {
    param(
        [object]$Context,
        [int]$StartRow,
        [int]$EndRow
    )

    foreach ($rowNumber in $StartRow..$EndRow) {
        $rowNode = Get-OrCreateRow -WorksheetXml $Context.Xml -SheetData $Context.SheetData -RowNumber $rowNumber
        foreach ($header in $Context.ColumnByHeader.Keys) {
            Set-CellText -WorksheetXml $Context.Xml -RowNode $rowNode -ColumnIndex $Context.ColumnByHeader[$header] -Text ""
        }
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

function Set-PerkSection {
    param(
        [object]$Context,
        [hashtable]$RowsByName,
        [string[]]$PerkNames,
        [int]$StartRow
    )

    $rowNumber = $StartRow
    foreach ($perkName in $PerkNames) {
        if (!$RowsByName.ContainsKey($perkName)) {
            throw "Perk '$perkName' was not found on sheet '$($Context.Path)'."
        }

        Set-PerkRow -Context $Context -RowNumber $rowNumber -Values $RowsByName[$perkName]
        $rowNumber++
    }
}

function Set-TotalRow {
    param(
        [object]$Context,
        [int]$RowNumber,
        [string]$Formula,
        [string]$CachedValue
    )

    $rowNode = Get-OrCreateRow -WorksheetXml $Context.Xml -SheetData $Context.SheetData -RowNumber $RowNumber
    Set-CellText -WorksheetXml $Context.Xml -RowNode $rowNode -ColumnIndex $Context.ColumnByHeader["Style"] -Text "Total"
    Set-CellFormula -WorksheetXml $Context.Xml -RowNode $rowNode -ColumnIndex $Context.ColumnByHeader["Price"] -Formula $Formula -CachedValue $CachedValue

    foreach ($header in $Context.ColumnByHeader.Keys) {
        if ($header -eq "Style" -or $header -eq "Price") {
            continue
        }

        Set-CellText -WorksheetXml $Context.Xml -RowNode $rowNode -ColumnIndex $Context.ColumnByHeader[$header] -Text ""
    }
}

function Set-SheetTotal {
    param(
        [object]$Context,
        [string]$Formula,
        [string]$CachedValue
    )

    $rowNode = Get-OrCreateRow -WorksheetXml $Context.Xml -SheetData $Context.SheetData -RowNumber 4
    Set-CellFormula -WorksheetXml $Context.Xml -RowNode $rowNode -ColumnIndex 4 -Formula $Formula -CachedValue $CachedValue
}

function Update-Dimension {
    param(
        [object]$Context,
        [int]$MaxRow
    )

    $dimensionNode = $Context.Xml.SelectSingleNode("//d:dimension", $Context.Namespace)
    if ($null -eq $dimensionNode) {
        return
    }

    $maxColumn = ($Context.ColumnByHeader.Values | Measure-Object -Maximum).Maximum
    $dimensionNode.SetAttribute("ref", "A1:$(ConvertTo-OpenXmlColumnName $maxColumn)$MaxRow")
}

# New skill requirements per perk. "-" means no requirement (rank 0).
$forceRequirements = @{
    "Force Spark I" = "-"
    "Throw Rock I" = "-"
    "Creeping Terror I" = "Force 2"
    "Force Push I" = "Force 5"
    "Force Choke I" = "Force 8"
    "Throw Lightsaber I" = "Force 8"
    "Force Lightning I" = "Force 10"
    "Force Drain I" = "Force 12"
    "Creeping Terror II" = "Force 15"
    "Force Spark II" = "Force 18"
    "Throw Rock II" = "Force 18"
    "Force Choke II" = "Force 20"
    "Force Lightning II" = "Force 22"
    "Force Drain II" = "Force 25"
    "Force Push II" = "Force 28"
    "Devouring Strike" = "Force 28"
    "Throw Lightsaber II" = "Force 30"
    "Force Choke III" = "Force 30"
    "Unstable Pressure" = "Force 32"
    "Purifying Wave" = "Force 35"
    "Creeping Terror III" = "Force 38"
    "Force Drain III" = "Force 38"
    "Throw Rock III" = "Force 40"
    "Force Spark III" = "Force 42"
    "Throw Lightsaber III" = "Force 45"
    "Force Push III" = "Force 48"
    "Force Choke IV" = "Force 48"
    "Last Stand of the Light" = "Force 50"

    "Benevolence I" = "-"
    "Guardian Ward I" = "Force 2"
    "Deflective Presence" = "Force 5"
    "Renewal I" = "Force 8"
    "Force Leap I" = "Force 10"
    "Serene Focus" = "Force 12"
    "Fury Stance I" = "Force 12"
    "Guardian Ward II" = "Force 15"
    "Benevolence II" = "Force 18"
    "Renewal II" = "Force 20"
    "Reflective Barrier" = "Force 22"
    "Force Mend" = "Force 25"
    "Cruel Momentum" = "Force 28"
    "Force Leap II" = "Force 30"
    "Force Sanctuary" = "Force 32"
    "Guardian Ward III" = "Force 35"
    "Benevolence III" = "Force 38"
    "Renewal III" = "Force 40"
    "Fury Stance II" = "Force 42"
    "Guardian Ward IV" = "Force 45"
    "Harmonic Restoration" = "Force 45"
    "Force Convergence" = "Force 48"
    "Hunger of the Dark" = "Force 50"

    "Weaken Resolve I" = "-"
    "Force Judgment I" = "Force 5"
    "Radiant Lance I" = "Force 8"
    "Mind Trick I" = "Force 12"
    "Courageous Resolve" = "Force 15"
    "Nightmare Field" = "Force 18"
    "Precognition" = "Force 22"
    "Force Judgment II" = "Force 25"
    "Weaken Resolve II" = "Force 28"
    "Force Intercept" = "Force 32"
    "Radiant Lance II" = "Force 35"
    "Mind Trick II" = "Force 38"
    "Collapse Will" = "Force 42"
    "Force Judgment III" = "Force 45"
    "Radiant Lance III" = "Force 48"
    "Eclipse of Resolve" = "Force 50"
}

# Section orders sorted by the new requirements.
$forceAlterOrder = @(
    "Force Spark I",
    "Throw Rock I",
    "Creeping Terror I",
    "Force Push I",
    "Force Choke I",
    "Throw Lightsaber I",
    "Force Lightning I",
    "Force Drain I",
    "Creeping Terror II",
    "Force Spark II",
    "Throw Rock II",
    "Force Choke II",
    "Force Lightning II",
    "Force Drain II",
    "Force Push II",
    "Devouring Strike",
    "Throw Lightsaber II",
    "Force Choke III",
    "Unstable Pressure",
    "Purifying Wave",
    "Creeping Terror III",
    "Force Drain III",
    "Throw Rock III",
    "Force Spark III",
    "Throw Lightsaber III",
    "Force Push III",
    "Force Choke IV",
    "Last Stand of the Light"
)

$forceControlOrder = @(
    "Benevolence I",
    "Guardian Ward I",
    "Deflective Presence",
    "Renewal I",
    "Force Leap I",
    "Serene Focus",
    "Fury Stance I",
    "Guardian Ward II",
    "Benevolence II",
    "Renewal II",
    "Reflective Barrier",
    "Force Mend",
    "Cruel Momentum",
    "Force Leap II",
    "Force Sanctuary",
    "Guardian Ward III",
    "Benevolence III",
    "Renewal III",
    "Fury Stance II",
    "Guardian Ward IV",
    "Harmonic Restoration",
    "Force Convergence",
    "Hunger of the Dark"
)

$forceSenseOrder = @(
    "Weaken Resolve I",
    "Force Judgment I",
    "Radiant Lance I",
    "Mind Trick I",
    "Courageous Resolve",
    "Nightmare Field",
    "Precognition",
    "Force Judgment II",
    "Weaken Resolve II",
    "Force Intercept",
    "Radiant Lance II",
    "Mind Trick II",
    "Collapse Will",
    "Force Judgment III",
    "Radiant Lance III",
    "Eclipse of Resolve"
)

$orderedNames = @($forceAlterOrder + $forceControlOrder + $forceSenseOrder)
if ($orderedNames.Count -ne 67) {
    throw "Expected 67 Force perk rows, found $($orderedNames.Count)."
}

foreach ($perkName in $orderedNames) {
    if (!$forceRequirements.ContainsKey($perkName)) {
        throw "Perk '$perkName' has no requirement assignment."
    }
}

if ($forceRequirements.Count -ne 67) {
    throw "Requirement map has $($forceRequirements.Count) entries, expected 67."
}

$workbookPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookPath)) {
    throw "Workbook '$workbookPath' was not found."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($workbookPath, [System.IO.Compression.ZipArchiveMode]::Update)
$updates = @()

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
    $forceRowsByName = Get-PerkRowsByName -Context $forceContext

    foreach ($perkName in $orderedNames) {
        if (!$forceRowsByName.ContainsKey($perkName)) {
            throw "Perk '$perkName' was not found on the Force sheet."
        }

        $oldRequirement = $forceRowsByName[$perkName]["SkillRequirements"]
        $newRequirement = $forceRequirements[$perkName]
        if ($oldRequirement -ne $newRequirement) {
            $updates += [pscustomobject]@{
                PerkName = $perkName
                OldRequirement = $oldRequirement
                NewRequirement = $newRequirement
            }
        }

        $forceRowsByName[$perkName]["SkillRequirements"] = $newRequirement
    }

    Clear-PerkRows -Context $forceContext -StartRow 8 -EndRow 95
    Set-PerkSection -Context $forceContext -RowsByName $forceRowsByName -PerkNames $forceAlterOrder -StartRow 8
    Set-TotalRow -Context $forceContext -RowNumber 36 -Formula "SUM(B8:B35)" -CachedValue "94"
    Set-PerkSection -Context $forceContext -RowsByName $forceRowsByName -PerkNames $forceControlOrder -StartRow 38
    Set-TotalRow -Context $forceContext -RowNumber 61 -Formula "SUM(B38:B60)" -CachedValue "87"
    Set-PerkSection -Context $forceContext -RowsByName $forceRowsByName -PerkNames $forceSenseOrder -StartRow 63
    Set-TotalRow -Context $forceContext -RowNumber 79 -Formula "SUM(B63:B78)" -CachedValue "59"
    Set-SheetTotal -Context $forceContext -Formula "SUM(B36,B61,B79)" -CachedValue "240"
    Update-Dimension -Context $forceContext -MaxRow 79
    Write-ZipEntryXml -Zip $zip -EntryPath $forceContext.Path -Xml $forceContext.Xml
}
finally {
    $zip.Dispose()
}

$numericStyleRepairScriptPath = Join-Path $PSScriptRoot "RepairForceDevicesNumericStyles.ps1"
if (Test-Path $numericStyleRepairScriptPath) {
    & $numericStyleRepairScriptPath -BibleWorkbookPath $BibleWorkbookPath
}

$sectionRepairScriptPath = Join-Path $PSScriptRoot "RepairCombatBibleSectionTotalSp.ps1"
if (Test-Path $sectionRepairScriptPath) {
    & $sectionRepairScriptPath -BibleWorkbookPath $BibleWorkbookPath
}

$totalRepairScriptPath = Join-Path $PSScriptRoot "RepairCombatBibleTotalSp.ps1"
if (Test-Path $totalRepairScriptPath) {
    & $totalRepairScriptPath -BibleWorkbookPath $BibleWorkbookPath
}

$updates | Format-Table PerkName, OldRequirement, NewRequirement -AutoSize
Write-Host "Applied Force requirement spread in '$workbookPath'."
