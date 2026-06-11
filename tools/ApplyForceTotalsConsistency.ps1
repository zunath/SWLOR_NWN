param(
    [string]$BibleWorkbookPath = "design/bible/SWLOR Design Bible - Combat Upgrade.xlsx"
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path (Get-Location) $Path
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

function Get-OpenXmlColumnIndex {
    param([string]$CellReference)

    if ([string]::IsNullOrWhiteSpace($CellReference)) {
        return 0
    }

    $letters = ($CellReference -replace "[^A-Z]", "")
    $index = 0
    foreach ($character in $letters.ToCharArray()) {
        $index = ($index * 26) + ([int][char]$character - [int][char]'A' + 1)
    }

    return $index
}

function Get-WorksheetCell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [string]$CellReference
    )

    $targetColumn = Get-OpenXmlColumnIndex $CellReference
    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    foreach ($cell in $RowNode.GetElementsByTagName("c", $namespace)) {
        if ((Get-OpenXmlColumnIndex $cell.GetAttribute("r")) -eq $targetColumn) {
            return $cell
        }
    }

    $cell = $WorksheetXml.CreateElement("c", $namespace)
    $cell.SetAttribute("r", $CellReference)
    [void]$RowNode.AppendChild($cell)
    return $cell
}

function Set-FormulaCell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [string]$CellReference,
        [string]$Formula,
        [string]$CachedValue
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $cell = Get-WorksheetCell -WorksheetXml $WorksheetXml -RowNode $RowNode -CellReference $CellReference
    $cell.RemoveAttribute("t")

    while ($cell.HasChildNodes) {
        [void]$cell.RemoveChild($cell.FirstChild)
    }

    $formulaElement = $WorksheetXml.CreateElement("f", $namespace)
    $formulaElement.InnerText = $Formula
    [void]$cell.AppendChild($formulaElement)

    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $CachedValue
    [void]$cell.AppendChild($valueElement)
}

function Get-CellScalarText {
    param([System.Xml.XmlElement]$Cell)

    if ($null -eq $Cell) {
        return $null
    }

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $inlineTextNodes = $Cell.GetElementsByTagName("t", $namespace)
    if ($inlineTextNodes.Count -gt 0) {
        return (($inlineTextNodes | ForEach-Object { $_.InnerText }) -join "")
    }

    $valueNodes = $Cell.GetElementsByTagName("v", $namespace)
    if ($valueNodes.Count -gt 0) {
        return $valueNodes[0].InnerText
    }

    return $null
}

function Set-NumericCell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [string]$CellReference,
        [string]$Value
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $cell = Get-WorksheetCell -WorksheetXml $WorksheetXml -RowNode $RowNode -CellReference $CellReference
    $cell.RemoveAttribute("t")

    while ($cell.HasChildNodes) {
        [void]$cell.RemoveChild($cell.FirstChild)
    }

    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $Value
    [void]$cell.AppendChild($valueElement)
}

function Get-SingleColumnSumRows {
    param([string]$Formula)

    $match = [regex]::Match($Formula, "^SUM\(B(?<Start>\d+):B(?<End>\d+)\)$")
    if (!$match.Success) {
        throw "Formula '$Formula' is not a single-column B range SUM."
    }

    return [pscustomobject]@{
        Start = [int]$match.Groups["Start"].Value
        End = [int]$match.Groups["End"].Value
    }
}

function Get-RowNode {
    param(
        [xml]$WorksheetXml,
        [int]$RowNumber,
        [string]$SheetName
    )

    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($WorksheetXml.NameTable)
    $namespaceManager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $rowNode = $WorksheetXml.SelectSingleNode("//d:sheetData/d:row[@r='$RowNumber']", $namespaceManager)
    if ($null -eq $rowNode) {
        throw "Workbook sheet '$SheetName' is missing row '$RowNumber'."
    }

    return $rowNode
}

function Get-WorksheetPath {
    param(
        [xml]$WorkbookXml,
        [hashtable]$RelationshipsById,
        [string]$SheetName
    )

    $workbookNamespace = [System.Xml.XmlNamespaceManager]::new($WorkbookXml.NameTable)
    $workbookNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $workbookNamespace.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")

    foreach ($sheet in $WorkbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
        if ($sheet.GetAttribute("name") -ne $SheetName) {
            continue
        }

        $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        return $RelationshipsById[$relationshipId]
    }

    throw "Workbook sheet '$SheetName' was not found."
}

function Set-WorksheetTotalFormulas {
    param(
        [System.IO.Compression.ZipArchive]$Zip,
        [xml]$WorkbookXml,
        [hashtable]$RelationshipsById,
        [string]$SheetName,
        [array]$TotalFormulas,
        [string]$GrandTotalFormula,
        [string]$GrandTotalValue
    )

    $sheetPath = Get-WorksheetPath -WorkbookXml $WorkbookXml -RelationshipsById $RelationshipsById -SheetName $SheetName
    [xml]$worksheetXml = Read-ZipEntryText -Zip $Zip -EntryPath $sheetPath

    foreach ($totalFormula in $TotalFormulas) {
        $sumRows = Get-SingleColumnSumRows -Formula $totalFormula.Formula
        foreach ($rowNumber in $sumRows.Start..$sumRows.End) {
            $priceRowNode = Get-RowNode -WorksheetXml $worksheetXml -RowNumber $rowNumber -SheetName $SheetName
            $priceCell = Get-WorksheetCell -WorksheetXml $worksheetXml -RowNode $priceRowNode -CellReference "B$rowNumber"
            $priceText = Get-CellScalarText -Cell $priceCell
            if ($priceText -match "^\d+$") {
                Set-NumericCell -WorksheetXml $worksheetXml -RowNode $priceRowNode -CellReference "B$rowNumber" -Value $priceText
            }
        }

        $rowNode = Get-RowNode -WorksheetXml $worksheetXml -RowNumber $totalFormula.Row -SheetName $SheetName
        Set-FormulaCell -WorksheetXml $worksheetXml -RowNode $rowNode -CellReference "B$($totalFormula.Row)" -Formula $totalFormula.Formula -CachedValue $totalFormula.Value
    }

    $grandTotalRow = Get-RowNode -WorksheetXml $worksheetXml -RowNumber 4 -SheetName $SheetName
    Set-FormulaCell -WorksheetXml $worksheetXml -RowNode $grandTotalRow -CellReference "D4" -Formula $GrandTotalFormula -CachedValue $GrandTotalValue

    Write-ZipEntryXml -Zip $Zip -EntryPath $sheetPath -Xml $worksheetXml

    return [pscustomobject]@{
        Sheet = $SheetName
        LineTotals = ($TotalFormulas | ForEach-Object { "B$($_.Row): $($_.Formula) = $($_.Value)" }) -join "; "
        GrandTotal = "D4: $GrandTotalFormula = $GrandTotalValue"
    }
}

$workbookPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookPath)) {
    throw "Workbook '$workbookPath' was not found."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($workbookPath, [System.IO.Compression.ZipArchiveMode]::Update)

try {
    [xml]$workbookXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/workbook.xml"
    [xml]$relationshipsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/_rels/workbook.xml.rels"

    $relationshipsById = @{}
    foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
        $relationshipsById[$relationship.Id] = "xl/" + $relationship.Target.TrimStart("/")
    }

    $summaries = @()
    $summaries += Set-WorksheetTotalFormulas -Zip $zip -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SheetName "Force" -GrandTotalFormula "SUM(B33,B58,B73)" -GrandTotalValue "220" -TotalFormulas @(
        @{ Row = 33; Formula = "SUM(B8:B32)"; Value = "85" },
        @{ Row = 58; Formula = "SUM(B35:B57)"; Value = "87" },
        @{ Row = 73; Formula = "SUM(B60:B72)"; Value = "48" }
    )

    $summaries += Set-WorksheetTotalFormulas -Zip $zip -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SheetName "Devices" -GrandTotalFormula "SUM(B25,B43,B62,B81)" -GrandTotalValue "220" -TotalFormulas @(
        @{ Row = 25; Formula = "SUM(B9:B24)"; Value = "55" },
        @{ Row = 43; Formula = "SUM(B28:B42)"; Value = "55" },
        @{ Row = 62; Formula = "SUM(B47:B61)"; Value = "55" },
        @{ Row = 81; Formula = "SUM(B66:B80)"; Value = "55" }
    )

    $workbookNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $workbookNamespaceManager = [System.Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
    $workbookNamespaceManager.AddNamespace("d", $workbookNamespace)
    $calcPr = $workbookXml.SelectSingleNode("//d:calcPr", $workbookNamespaceManager)
    if ($null -eq $calcPr) {
        $calcPr = $workbookXml.CreateElement("calcPr", $workbookNamespace)
        [void]$workbookXml.DocumentElement.AppendChild($calcPr)
    }

    $calcPr.SetAttribute("calcMode", "auto")
    $calcPr.SetAttribute("fullCalcOnLoad", "1")
    $calcPr.SetAttribute("forceFullCalc", "1")

    Write-ZipEntryXml -Zip $zip -EntryPath "xl/workbook.xml" -Xml $workbookXml
}
finally {
    $zip.Dispose()
}

$summaries | Format-List

Write-Host "Updated Force and Devices total formulas in '$workbookPath'."
