[CmdletBinding()]
param(
    [string]$WorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx",
    [string]$LayoutPath = "tools\CombatUpgradeBibleWorkbookLayout.json"
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
        [object]$Zip,
        [string]$EntryPath
    )

    $entry = $Zip.GetEntry($EntryPath)
    if ($null -eq $entry) {
        throw "Workbook entry '$EntryPath' was not found."
    }

    $reader = [System.IO.StreamReader]::new($entry.Open())
    try {
        return $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
}

function Write-ZipEntryText {
    param(
        [object]$Zip,
        [string]$EntryPath,
        [xml]$Document
    )

    $oldEntry = $Zip.GetEntry($EntryPath)
    if ($null -eq $oldEntry) {
        throw "Workbook entry '$EntryPath' was not found."
    }

    $oldEntry.Delete()
    $newEntry = $Zip.CreateEntry($EntryPath, [System.IO.Compression.CompressionLevel]::Optimal)
    $stream = $newEntry.Open()
    try {
        $settings = [System.Xml.XmlWriterSettings]::new()
        $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
        $settings.Indent = $false
        $writer = [System.Xml.XmlWriter]::Create($stream, $settings)
        try {
            $Document.Save($writer)
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
    param([string]$Target)

    if ($Target.StartsWith("/", [System.StringComparison]::Ordinal)) {
        return $Target.TrimStart("/")
    }

    if ($Target.StartsWith("worksheets/", [System.StringComparison]::Ordinal)) {
        return "xl/$Target"
    }

    return "xl/$Target"
}

function Get-OpenXmlColumnIndex {
    param([string]$CellReference)

    if ($CellReference -notmatch "^([A-Z]+)") {
        return 0
    }

    $letters = $Matches[1]
    $column = 0
    foreach ($char in $letters.ToCharArray()) {
        $column = ($column * 26) + ([int][char]$char - [int][char]'A' + 1)
    }

    return $column
}

function Get-OpenXmlCellText {
    param(
        [System.Xml.XmlElement]$Cell,
        [System.Collections.Generic.List[string]]$SharedStrings
    )

    $cellType = $Cell.GetAttribute("t")
    if ($cellType -eq "inlineStr") {
        $texts = [System.Collections.Generic.List[string]]::new()
        foreach ($textNode in $Cell.GetElementsByTagName("t", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")) {
            $texts.Add($textNode.InnerText) | Out-Null
        }

        return ($texts -join "")
    }

    $valueNode = $Cell.GetElementsByTagName("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main") | Select-Object -First 1
    if ($null -eq $valueNode) {
        return ""
    }

    $rawValue = $valueNode.InnerText
    if ($cellType -eq "s") {
        $index = 0
        if ([int]::TryParse($rawValue, [ref]$index) -and $index -ge 0 -and $index -lt $SharedStrings.Count) {
            return $SharedStrings[$index]
        }
    }

    return $rawValue
}

function Set-ColumnWidths {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlNamespaceManager]$Namespace,
        [object[]]$Columns
    )

    $worksheetNamespaceUri = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $colsNode = $WorksheetXml.SelectSingleNode("/d:worksheet/d:cols", $Namespace)
    if ($null -eq $colsNode) {
        $colsNode = $WorksheetXml.CreateElement("cols", $worksheetNamespaceUri)
        $sheetDataNode = $WorksheetXml.SelectSingleNode("/d:worksheet/d:sheetData", $Namespace)
        [void]$WorksheetXml.DocumentElement.InsertBefore($colsNode, $sheetDataNode)
    }

    while ($colsNode.HasChildNodes) {
        [void]$colsNode.RemoveChild($colsNode.FirstChild)
    }

    foreach ($column in $Columns) {
        $colNode = $WorksheetXml.CreateElement("col", $worksheetNamespaceUri)
        $colNode.SetAttribute("min", [string]$column.Min)
        $colNode.SetAttribute("max", [string]$column.Max)
        $columnWidth = ([decimal]$column.Width).ToString([System.Globalization.CultureInfo]::InvariantCulture)
        $colNode.SetAttribute("width", $columnWidth)
        $colNode.SetAttribute("customWidth", "1")
        [void]$colsNode.AppendChild($colNode)
    }
}

function Set-MinimumColumnWidth {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlNamespaceManager]$Namespace,
        [int]$ColumnIndex,
        [decimal]$MinimumWidth
    )

    $worksheetNamespaceUri = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $colsNode = $WorksheetXml.SelectSingleNode("/d:worksheet/d:cols", $Namespace)
    if ($null -eq $colsNode) {
        $colsNode = $WorksheetXml.CreateElement("cols", $worksheetNamespaceUri)
        $sheetDataNode = $WorksheetXml.SelectSingleNode("/d:worksheet/d:sheetData", $Namespace)
        [void]$WorksheetXml.DocumentElement.InsertBefore($colsNode, $sheetDataNode)
    }

    $existingColumns = [System.Collections.Generic.List[object]]::new()
    $currentWidth = $null
    foreach ($colNode in $colsNode.SelectNodes("d:col", $Namespace)) {
        $min = [int]$colNode.GetAttribute("min")
        $max = [int]$colNode.GetAttribute("max")
        $width = [decimal]::Parse($colNode.GetAttribute("width"), [System.Globalization.CultureInfo]::InvariantCulture)
        $existingColumns.Add([pscustomobject]@{ Min = $min; Max = $max; Width = $width }) | Out-Null

        if ($ColumnIndex -ge $min -and $ColumnIndex -le $max) {
            $currentWidth = $width
        }
    }

    if ($null -ne $currentWidth -and $currentWidth -ge $MinimumWidth) {
        return
    }

    $updatedColumns = [System.Collections.Generic.List[object]]::new()
    $wasCovered = $false
    foreach ($column in $existingColumns) {
        if ($ColumnIndex -lt $column.Min -or $ColumnIndex -gt $column.Max) {
            $updatedColumns.Add($column) | Out-Null
            continue
        }

        $wasCovered = $true
        if ($ColumnIndex -gt $column.Min) {
            $updatedColumns.Add([pscustomobject]@{ Min = $column.Min; Max = ($ColumnIndex - 1); Width = $column.Width }) | Out-Null
        }

        $updatedColumns.Add([pscustomobject]@{ Min = $ColumnIndex; Max = $ColumnIndex; Width = $MinimumWidth }) | Out-Null

        if ($ColumnIndex -lt $column.Max) {
            $updatedColumns.Add([pscustomobject]@{ Min = ($ColumnIndex + 1); Max = $column.Max; Width = $column.Width }) | Out-Null
        }
    }

    if (!$wasCovered) {
        $updatedColumns.Add([pscustomobject]@{ Min = $ColumnIndex; Max = $ColumnIndex; Width = $MinimumWidth }) | Out-Null
    }

    while ($colsNode.HasChildNodes) {
        [void]$colsNode.RemoveChild($colsNode.FirstChild)
    }

    foreach ($column in ($updatedColumns | Sort-Object Min, Max)) {
        $colNode = $WorksheetXml.CreateElement("col", $worksheetNamespaceUri)
        $colNode.SetAttribute("min", [string]$column.Min)
        $colNode.SetAttribute("max", [string]$column.Max)
        $columnWidth = ([decimal]$column.Width).ToString([System.Globalization.CultureInfo]::InvariantCulture)
        $colNode.SetAttribute("width", $columnWidth)
        $colNode.SetAttribute("customWidth", "1")
        [void]$colsNode.AppendChild($colNode)
    }
}

function Get-HeaderColumnIndexes {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlNamespaceManager]$Namespace,
        [System.Collections.Generic.List[string]]$SharedStrings,
        [string]$HeaderText
    )

    $columnIndexes = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($cell in $WorksheetXml.SelectNodes("//d:sheetData/d:row/d:c", $Namespace)) {
        $cellText = Get-OpenXmlCellText -Cell $cell -SharedStrings $SharedStrings
        if ($cellText.Trim() -ne $HeaderText) {
            continue
        }

        $columnIndex = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
        if ($columnIndex -gt 0) {
            [void]$columnIndexes.Add($columnIndex)
        }
    }

    return @($columnIndexes | Sort-Object)
}

function Remove-CustomRowHeights {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlNamespaceManager]$Namespace
    )

    foreach ($row in @($WorksheetXml.SelectNodes("//d:sheetData/d:row", $Namespace))) {
        if ($row.HasAttribute("ht")) {
            $row.RemoveAttribute("ht")
        }

        if ($row.HasAttribute("customHeight")) {
            $row.RemoveAttribute("customHeight")
        }
    }
}

function Test-IsPerkTableSheet {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlNamespaceManager]$Namespace,
        [System.Collections.Generic.List[string]]$SharedStrings
    )

    foreach ($rowNode in $WorksheetXml.SelectNodes("//d:sheetData/d:row", $Namespace)) {
        $cellsByColumn = @{}
        foreach ($cell in $rowNode.SelectNodes("d:c", $Namespace)) {
            $columnIndex = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
            if ($columnIndex -gt 0) {
                $cellsByColumn[$columnIndex] = Get-OpenXmlCellText -Cell $cell -SharedStrings $SharedStrings
            }
        }

        if ($cellsByColumn.Values -contains "Style" -and
            $cellsByColumn.Values -contains "SP Price" -and
            $cellsByColumn.Values -contains "Perk Name" -and
            $cellsByColumn.Values -contains "Type" -and
            $cellsByColumn.Values -contains "Description") {
            return $true
        }
    }

    return $false
}

function Get-LayoutColumns {
    param(
        [object]$Layout,
        [string]$SheetName
    )

    if ($null -eq $Layout -or $null -eq $Layout.ColumnsBySheet) {
        return $null
    }

    $property = $Layout.ColumnsBySheet.PSObject.Properties |
        Where-Object { $_.Name -eq $SheetName } |
        Select-Object -First 1

    if ($null -eq $property) {
        return $null
    }

    return @($property.Value)
}

function Get-SharedStrings {
    param([object]$Zip)

    $sharedStrings = [System.Collections.Generic.List[string]]::new()
    if ($null -eq $Zip.GetEntry("xl/sharedStrings.xml")) {
        return $sharedStrings
    }

    [xml]$sharedStringsXml = Read-ZipEntryText -Zip $Zip -EntryPath "xl/sharedStrings.xml"
    $sharedStringNamespace = [System.Xml.XmlNamespaceManager]::new($sharedStringsXml.NameTable)
    $sharedStringNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    foreach ($sharedString in $sharedStringsXml.SelectNodes("//d:si", $sharedStringNamespace)) {
        $texts = [System.Collections.Generic.List[string]]::new()
        foreach ($textNode in $sharedString.SelectNodes(".//d:t", $sharedStringNamespace)) {
            $texts.Add($textNode.InnerText) | Out-Null
        }

        $sharedStrings.Add(($texts -join "")) | Out-Null
    }

    return $sharedStrings
}

$compactPerkColumns = @(
    [pscustomobject]@{ Min = 1; Max = 1; Width = 12.13 },
    [pscustomobject]@{ Min = 2; Max = 2; Width = 12.88 },
    [pscustomobject]@{ Min = 3; Max = 3; Width = 15.38 },
    [pscustomobject]@{ Min = 4; Max = 4; Width = 11.25 },
    [pscustomobject]@{ Min = 5; Max = 5; Width = 9.38 },
    [pscustomobject]@{ Min = 6; Max = 6; Width = 8.13 },
    [pscustomobject]@{ Min = 7; Max = 7; Width = 92.25 },
    [pscustomobject]@{ Min = 8; Max = 8; Width = 10.75 },
    [pscustomobject]@{ Min = 9; Max = 9; Width = 13.13 },
    [pscustomobject]@{ Min = 10; Max = 10; Width = 13.38 },
    [pscustomobject]@{ Min = 11; Max = 11; Width = 10.63 },
    [pscustomobject]@{ Min = 12; Max = 12; Width = 3.13 },
    [pscustomobject]@{ Min = 13; Max = 13; Width = 4.5 },
    [pscustomobject]@{ Min = 14; Max = 14; Width = 11.38 },
    [pscustomobject]@{ Min = 15; Max = 15; Width = 13.38 },
    [pscustomobject]@{ Min = 16; Max = 16; Width = 10.63 },
    [pscustomobject]@{ Min = 17; Max = 17; Width = 20.75 },
    [pscustomobject]@{ Min = 18; Max = 18; Width = 45.0 },
    [pscustomobject]@{ Min = 19; Max = 30; Width = 17.63 }
)
$minimumNotesColumnWidth = 45.0
$systemChangesColumns = @(
    [pscustomobject]@{ Min = 1; Max = 1; Width = 7.75 },
    [pscustomobject]@{ Min = 2; Max = 2; Width = 154.88 },
    [pscustomobject]@{ Min = 3; Max = 6; Width = 12.63 }
)

$workbookFullPath = Resolve-RepoPath $WorkbookPath
if (!(Test-Path -LiteralPath $workbookFullPath)) {
    throw "Workbook '$workbookFullPath' was not found."
}

$layoutFullPath = Resolve-RepoPath $LayoutPath
$layout = $null
if (Test-Path -LiteralPath $layoutFullPath) {
    $layout = Get-Content -LiteralPath $layoutFullPath -Raw | ConvertFrom-Json
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::Open($workbookFullPath, [System.IO.Compression.ZipArchiveMode]::Update)
try {
    $sharedStrings = Get-SharedStrings -Zip $zip

    [xml]$workbookXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/workbook.xml"
    [xml]$relationshipsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/_rels/workbook.xml.rels"

    $relationshipsById = @{}
    foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
        $relationshipsById[$relationship.Id] = Get-WorkbookEntryPath $relationship.Target
    }

    $workbookNamespace = [System.Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
    $workbookNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $workbookNamespace.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")

    foreach ($sheet in $workbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
        $sheetName = $sheet.GetAttribute("name")
        $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $worksheetEntryPath = $relationshipsById[$relationshipId]
        [xml]$worksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $worksheetEntryPath
        $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($worksheetXml.NameTable)
        $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

        $layoutColumns = Get-LayoutColumns -Layout $layout -SheetName $sheetName
        if ($null -ne $layoutColumns -and $layoutColumns.Count -gt 0) {
            Set-ColumnWidths -WorksheetXml $worksheetXml -Namespace $worksheetNamespace -Columns $layoutColumns
        }
        elseif ($sheetName -eq "System Changes & Migrations") {
            Set-ColumnWidths -WorksheetXml $worksheetXml -Namespace $worksheetNamespace -Columns $systemChangesColumns
        }
        elseif (Test-IsPerkTableSheet -WorksheetXml $worksheetXml -Namespace $worksheetNamespace -SharedStrings $sharedStrings) {
            Set-ColumnWidths -WorksheetXml $worksheetXml -Namespace $worksheetNamespace -Columns $compactPerkColumns
        }

        foreach ($notesColumnIndex in (Get-HeaderColumnIndexes -WorksheetXml $worksheetXml -Namespace $worksheetNamespace -SharedStrings $sharedStrings -HeaderText "Notes")) {
            Set-MinimumColumnWidth -WorksheetXml $worksheetXml -Namespace $worksheetNamespace -ColumnIndex $notesColumnIndex -MinimumWidth $minimumNotesColumnWidth
        }

        Remove-CustomRowHeights -WorksheetXml $worksheetXml -Namespace $worksheetNamespace
        Write-ZipEntryText -Zip $zip -EntryPath $worksheetEntryPath -Document $worksheetXml
    }
}
finally {
    $zip.Dispose()
}
