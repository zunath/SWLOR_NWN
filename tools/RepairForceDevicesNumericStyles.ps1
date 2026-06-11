param(
    [string]$BibleWorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$normalStyleId = "4"
$totalStyleId = "2"

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

function Get-NumberOrNull {
    param([string]$Text)

    $number = 0.0
    if ([double]::TryParse($Text, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$number)) {
        return $number
    }

    return $null
}

function Get-CellByColumn {
    param(
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex
    )

    foreach ($cell in $RowNode.GetElementsByTagName("c", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")) {
        if ((Get-OpenXmlColumnIndex $cell.GetAttribute("r")) -eq $ColumnIndex) {
            return $cell
        }
    }

    return $null
}

function Get-OrCreateCell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $existingCell = Get-CellByColumn -RowNode $RowNode -ColumnIndex $ColumnIndex
    if ($null -ne $existingCell) {
        return $existingCell
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

function Set-CellStyle {
    param(
        [System.Xml.XmlElement]$Cell,
        [string]$StyleId
    )

    if ([string]::IsNullOrWhiteSpace($StyleId)) {
        return
    }

    $Cell.SetAttribute("s", $StyleId)
}

function Set-CellNumber {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$Cell,
        [double]$Value,
        [string]$StyleId
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    Clear-Cell -Cell $Cell
    Set-CellStyle -Cell $Cell -StyleId $StyleId

    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $Value.ToString("0.############", [System.Globalization.CultureInfo]::InvariantCulture)
    [void]$Cell.AppendChild($valueElement)
}

function Get-HeaderColumns {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlNamespaceManager]$NamespaceManager,
        [System.Collections.Generic.IList[string]]$SharedStrings
    )

    foreach ($rowNode in $WorksheetXml.SelectNodes("//d:sheetData/d:row", $NamespaceManager)) {
        $columnByHeader = @{}
        foreach ($cell in $rowNode.SelectNodes("d:c", $NamespaceManager)) {
            $text = Get-OpenXmlCellText -Cell $cell -SharedStrings $SharedStrings
            $key = ($text -replace "[\s\.\?]+", "").ToLowerInvariant()
            switch ($key) {
                "style" { $columnByHeader["Style"] = Get-OpenXmlColumnIndex $cell.GetAttribute("r") }
                "spprice" { $columnByHeader["Price"] = Get-OpenXmlColumnIndex $cell.GetAttribute("r") }
                "price" { $columnByHeader["Price"] = Get-OpenXmlColumnIndex $cell.GetAttribute("r") }
                "perkname" { $columnByHeader["PerkName"] = Get-OpenXmlColumnIndex $cell.GetAttribute("r") }
                "name" { $columnByHeader["PerkName"] = Get-OpenXmlColumnIndex $cell.GetAttribute("r") }
            }
        }

        if ($columnByHeader.ContainsKey("Style") -and $columnByHeader.ContainsKey("Price") -and $columnByHeader.ContainsKey("PerkName")) {
            return [pscustomobject]@{
                RowNumber = [int]$rowNode.GetAttribute("r")
                ColumnByHeader = $columnByHeader
            }
        }
    }

    throw "No perk header row was found."
}

function Repair-Sheet {
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

    $header = Get-HeaderColumns -WorksheetXml $worksheetXml -NamespaceManager $worksheetNamespace -SharedStrings $SharedStrings
    $styleColumn = [int]$header.ColumnByHeader["Style"]
    $priceColumn = [int]$header.ColumnByHeader["Price"]
    $perkNameColumn = [int]$header.ColumnByHeader["PerkName"]
    $maxColumn = ($header.ColumnByHeader.Values | Measure-Object -Maximum).Maximum

    $convertedPrices = 0
    $normalRows = 0
    $totalRows = 0

    foreach ($rowNode in $worksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumber = [int]$rowNode.GetAttribute("r")
        if ($rowNumber -le $header.RowNumber) {
            continue
        }

        $styleText = Get-OpenXmlCellText -Cell (Get-CellByColumn -RowNode $rowNode -ColumnIndex $styleColumn) -SharedStrings $SharedStrings
        $perkName = Get-OpenXmlCellText -Cell (Get-CellByColumn -RowNode $rowNode -ColumnIndex $perkNameColumn) -SharedStrings $SharedStrings

        if ($styleText -eq "Total") {
            for ($columnIndex = 1; $columnIndex -le $maxColumn; $columnIndex++) {
                $cell = Get-CellByColumn -RowNode $rowNode -ColumnIndex $columnIndex
                if ($null -ne $cell) {
                    Set-CellStyle -Cell $cell -StyleId $totalStyleId
                }
            }

            $totalRows++
            continue
        }

        if ([string]::IsNullOrWhiteSpace($perkName)) {
            continue
        }

        for ($columnIndex = 1; $columnIndex -le $maxColumn; $columnIndex++) {
            $cell = Get-CellByColumn -RowNode $rowNode -ColumnIndex $columnIndex
            if ($null -ne $cell) {
                Set-CellStyle -Cell $cell -StyleId $normalStyleId
            }
        }

        $priceCell = Get-OrCreateCell -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $priceColumn
        $priceValue = Get-NumberOrNull (Get-OpenXmlCellText -Cell $priceCell -SharedStrings $SharedStrings)
        if ($null -ne $priceValue) {
            Set-CellNumber -WorksheetXml $worksheetXml -Cell $priceCell -Value ([double]$priceValue) -StyleId $normalStyleId
            $convertedPrices++
        }

        $normalRows++
    }

    Write-ZipEntryXml -Zip $Zip -EntryPath $sheetPath -Xml $worksheetXml

    return [pscustomobject]@{
        Sheet = $SheetName
        NumericPrices = $convertedPrices
        NormalRows = $normalRows
        TotalRows = $totalRows
    }
}

$workbookPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookPath)) {
    throw "Workbook '$workbookPath' was not found."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($workbookPath, [System.IO.Compression.ZipArchiveMode]::Update)
$results = @()

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

    foreach ($sheetName in @("Force", "Devices")) {
        $results += Repair-Sheet -Zip $zip -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SharedStrings $sharedStrings -SheetName $sheetName
    }
}
finally {
    $zip.Dispose()
}

$results | Format-Table Sheet, NumericPrices, NormalRows, TotalRows -AutoSize
Write-Host "Repaired numeric SP prices and row styles in '$workbookPath'."
