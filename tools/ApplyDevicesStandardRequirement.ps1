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

function Get-SharedStringIndex {
    param(
        [xml]$SharedStringsXml,
        [string]$Text
    )

    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($SharedStringsXml.NameTable)
    $namespaceManager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $items = $SharedStringsXml.SelectNodes("//d:sst/d:si", $namespaceManager)
    for ($index = 0; $index -lt $items.Count; $index++) {
        $value = ($items[$index].SelectNodes(".//d:t", $namespaceManager) | ForEach-Object { $_.InnerText }) -join ""
        if ($value -eq $Text) {
            return $index
        }
    }

    throw "Shared string '$Text' was not found."
}

function Get-RowNode {
    param(
        [xml]$WorksheetXml,
        [int]$RowNumber
    )

    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($WorksheetXml.NameTable)
    $namespaceManager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $rowNode = $WorksheetXml.SelectSingleNode("//d:sheetData/d:row[@r='$RowNumber']", $namespaceManager)
    if ($null -eq $rowNode) {
        throw "Workbook sheet 'Devices' is missing row '$RowNumber'."
    }

    return $rowNode
}

function Get-Cell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [string]$CellReference
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    foreach ($cell in $RowNode.GetElementsByTagName("c", $namespace)) {
        if ($cell.GetAttribute("r") -eq $CellReference) {
            return $cell
        }
    }

    $cell = $WorksheetXml.CreateElement("c", $namespace)
    $cell.SetAttribute("r", $CellReference)
    [void]$RowNode.AppendChild($cell)
    return $cell
}

function Set-SharedStringCell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [string]$CellReference,
        [int]$SharedStringIndex
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $cell = Get-Cell -WorksheetXml $WorksheetXml -RowNode $RowNode -CellReference $CellReference
    $cell.SetAttribute("t", "s")

    while ($cell.HasChildNodes) {
        [void]$cell.RemoveChild($cell.FirstChild)
    }

    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $SharedStringIndex.ToString()
    [void]$cell.AppendChild($valueElement)
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
    [xml]$sharedStringsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/sharedStrings.xml"

    $relationshipsById = @{}
    foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
        $relationshipsById[$relationship.Id] = "xl/" + $relationship.Target.TrimStart("/")
    }

    $standardIndex = Get-SharedStringIndex -SharedStringsXml $sharedStringsXml -Text "Standard"
    $devicesSheetPath = Get-WorksheetPath -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SheetName "Devices"
    [xml]$devicesWorksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $devicesSheetPath

    $perkRows = @(9..20) + @(24..40) + @(44..60) + @(64..80) + @(84..100)
    foreach ($rowNumber in $perkRows) {
        $rowNode = Get-RowNode -WorksheetXml $devicesWorksheetXml -RowNumber $rowNumber
        Set-SharedStringCell -WorksheetXml $devicesWorksheetXml -RowNode $rowNode -CellReference "E$rowNumber" -SharedStringIndex $standardIndex
    }

    Write-ZipEntryXml -Zip $zip -EntryPath $devicesSheetPath -Xml $devicesWorksheetXml
}
finally {
    $zip.Dispose()
}

[pscustomobject]@{
    Sheet = "Devices"
    CharacterType = "Standard"
    UpdatedPerkRows = 80
} | Format-List

Write-Host "Updated Devices perk rows to require Standard in '$workbookPath'."
