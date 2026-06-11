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
        "chartype" { return "CharacterType" }
        "charactertype" { return "CharacterType" }
        "type" { return "Type" }
        "description" { return "Description" }
        "primarystat" { return "PrimaryStat" }
        "secondarystat" { return "SecondaryStat" }
        "scalingsource" { return "ScalingSource" }
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

    foreach ($requiredHeader in @("PerkName", "Type", "Description", "PrimaryStat", "SecondaryStat", "ScalingSource", "FP", "STM", "CastingTime", "CooldownTime", "DevStatus", "Notes")) {
        if (!$columnByHeader.ContainsKey($requiredHeader)) {
            throw "Workbook sheet '$SheetName' is missing required column '$requiredHeader'."
        }
    }

    return [pscustomobject]@{
        Path = $sheetPath
        Xml = $worksheetXml
        Namespace = $worksheetNamespace
        ColumnByHeader = $columnByHeader
    }
}

function Get-ExistingRow {
    param(
        [object]$Context,
        [int]$RowNumber
    )

    $rowNode = $Context.Xml.SelectSingleNode("//d:sheetData/d:row[@r='$RowNumber']", $Context.Namespace)
    if ($null -eq $rowNode) {
        throw "Workbook row '$RowNumber' was not found."
    }

    return $rowNode
}

function Set-ExistingPerkRow {
    param(
        [object]$Context,
        [int]$RowNumber,
        [hashtable]$Values
    )

    $rowNode = Get-ExistingRow -Context $Context -RowNumber $RowNumber

    foreach ($header in $Values.Keys) {
        if (!$Context.ColumnByHeader.ContainsKey($header)) {
            continue
        }

        Set-CellText -WorksheetXml $Context.Xml -RowNode $rowNode -ColumnIndex $Context.ColumnByHeader[$header] -Text ([string]$Values[$header])
    }
}

$fieldSupportRows = @(
    @{
        Row = 52
        Values = @{
            Type = "Trait"
            Description = "Field Support ally buffs from Deflector Shield, Power Cell, Group Deflector, and Emergency Bunker also grant affected allies 8% reduced ranged physical damage for 12 seconds."
            PrimaryStat = "None"
            SecondaryStat = "None"
            ScalingSource = "Design Added"
            FP = "-"
            STM = "-"
            CastingTime = "-"
            CooldownTime = "-"
            DevStatus = "Implemented"
            Notes = "Converted from a placed screen active to a trait for Field Support ally buffs. Former active values: STM 5; casting 1.5 seconds; cooldown 75 seconds."
        }
    }
    @{
        Row = 53
        Values = @{
            Type = "Trait"
            Description = "Field Support ally buffs from Deflector Shield, Power Cell, Group Deflector, and Emergency Bunker also grant affected allies 6% reduced physical and force damage for 8 seconds."
            PrimaryStat = "None"
            SecondaryStat = "None"
            ScalingSource = "Design Added"
            FP = "-"
            STM = "-"
            CastingTime = "-"
            CooldownTime = "-"
            DevStatus = "Implemented"
            Notes = "Converted from a single-target mitigation active to a trait for Field Support ally buffs. Former active values: STM 5; casting 1 second; cooldown 60 seconds."
        }
    }
    @{
        Row = 57
        Values = @{
            Type = "Trait"
            Description = "Field Support ally buffs from Deflector Shield, Power Cell, Group Deflector, and Emergency Bunker also grant affected allies 12% reduced ranged physical damage for 15 seconds."
            PrimaryStat = "None"
            SecondaryStat = "None"
            ScalingSource = "Design Added"
            FP = "-"
            STM = "-"
            CastingTime = "-"
            CooldownTime = "-"
            DevStatus = "Implemented"
            Notes = "Replacement tier: improves the ranged physical mitigation rider. Converted from a placed screen active to a trait for Field Support ally buffs. Former active values: STM 6; casting 1.5 seconds; cooldown 75 seconds."
        }
    }
    @{
        Row = 58
        Values = @{
            Type = "Trait"
            Description = "Field Support ally buffs from Deflector Shield, Power Cell, Group Deflector, and Emergency Bunker also grant affected allies 10% reduced physical and force damage for 10 seconds."
            PrimaryStat = "None"
            SecondaryStat = "None"
            ScalingSource = "Design Added"
            FP = "-"
            STM = "-"
            CastingTime = "-"
            CooldownTime = "-"
            DevStatus = "Implemented"
            Notes = "Replacement tier: improves the broad mitigation rider. Converted from a single-target mitigation active to a trait for Field Support ally buffs. Former active values: STM 7; casting 1 second; cooldown 60 seconds."
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

    $devicesContext = Get-SheetContext -Zip $zip -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SharedStrings $sharedStrings -SheetName "Devices"
    foreach ($row in $fieldSupportRows) {
        Set-ExistingPerkRow -Context $devicesContext -RowNumber ([int]$row.Row) -Values $row.Values
    }

    Write-ZipEntryXml -Zip $zip -EntryPath $devicesContext.Path -Xml $devicesContext.Xml
}
finally {
    $zip.Dispose()
}

$fieldSupportRows | ForEach-Object {
    [pscustomobject]@{
        Sheet = "Devices"
        Row = $_.Row
        Type = $_.Values.Type
        Description = $_.Values.Description
    }
} | Format-Table Sheet, Row, Type, Description -Wrap

Write-Host "Converted Field Support mitigation rows in '$workbookPath'."
