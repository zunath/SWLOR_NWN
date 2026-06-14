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

function Get-SharedStringValues {
    param([xml]$SharedStringsXml)

    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($SharedStringsXml.NameTable)
    $namespaceManager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $items = $SharedStringsXml.SelectNodes("//d:sst/d:si", $namespaceManager)
    $values = @()
    foreach ($item in $items) {
        $values += (($item.SelectNodes(".//d:t", $namespaceManager) | ForEach-Object { $_.InnerText }) -join "")
    }

    return $values
}

function Get-CellText {
    param(
        [System.Xml.XmlElement]$Cell,
        [string[]]$SharedStrings
    )

    if ($null -eq $Cell) {
        return ""
    }

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $type = $Cell.GetAttribute("t")

    if ($type -eq "s") {
        $valueNodes = $Cell.GetElementsByTagName("v", $namespace)
        if ($valueNodes.Count -eq 0) {
            return ""
        }

        return $SharedStrings[[int]$valueNodes[0].InnerText]
    }

    if ($type -eq "inlineStr") {
        return (($Cell.GetElementsByTagName("t", $namespace) | ForEach-Object { $_.InnerText }) -join "")
    }

    $valueNode = $Cell.GetElementsByTagName("v", $namespace)
    if ($valueNode.Count -gt 0) {
        return $valueNode[0].InnerText
    }

    return ""
}

function Clear-Cell {
    param([System.Xml.XmlElement]$Cell)

    $cellReference = $Cell.GetAttribute("r")
    $style = $Cell.GetAttribute("s")
    while ($Cell.HasChildNodes) {
        [void]$Cell.RemoveChild($Cell.FirstChild)
    }

    $Cell.RemoveAllAttributes()
    $Cell.SetAttribute("r", $cellReference)
    if (![string]::IsNullOrWhiteSpace($style)) {
        $Cell.SetAttribute("s", $style)
    }
}

function Set-TextCell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$Cell,
        [string]$Value
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    Clear-Cell -Cell $Cell
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    $Cell.SetAttribute("t", "inlineStr")
    $inlineString = $WorksheetXml.CreateElement("is", $namespace)
    $textElement = $WorksheetXml.CreateElement("t", $namespace)
    $textElement.InnerText = $Value
    [void]$inlineString.AppendChild($textElement)
    [void]$Cell.AppendChild($inlineString)
}

$workbookPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookPath)) {
    throw "Workbook '$workbookPath' was not found."
}

$noteByPerk = @{
    "Force Push I" = "Alter kinetic pressure. Lower direct damage than dedicated attacks because it also controls movement. No affinity scaling."
    "Force Push II" = "Replacement tier: selected target receives the full lower-rank effect. Alter kinetic pressure. No affinity scaling."
    "Force Push III" = "Replacement tier: selected target receives the full lower-rank effect. Alter kinetic pressure. No affinity scaling."
    "Throw Lightsaber I" = "Alter ranged weapon attack. Lower scaling than dedicated weapon skill strikes because it works with any equipped weapon and adds Force utility."
    "Throw Lightsaber II" = "Replacement tier: selected target receives the full lower-rank effect. Lower bonus damage than dedicated weapon lines because this is ranged, works with any equipped weapon, and can add secondary targets."
    "Throw Lightsaber III" = "Replacement tier: selected target receives the full lower-rank effect. Multi-target ranged utility stays below top dedicated weapon single-target strikes."
    "Force Leap I" = "Alter mobility attack. No affinity scaling."
    "Force Leap II" = "Replacement tier: selected target receives the full lower-rank effect. Alter mobility attack. No affinity scaling."
    "Mind Trick I" = "Sense mind-control utility. Confusion mechanics are defined in the Status Effects tab."
    "Mind Trick II" = "Replacement tier: expands target count and uses the same Willpower contest as rank I. Confusion mechanics are defined in the Status Effects tab."
    "Precognition" = "Sense combat trait that benefits any Force style without adding an active button or equipment requirement."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($workbookPath, [System.IO.Compression.ZipArchiveMode]::Update)

try {
    [xml]$workbookXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/workbook.xml"
    [xml]$relationshipsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/_rels/workbook.xml.rels"
    [xml]$sharedStringsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/sharedStrings.xml"
    $sharedStrings = Get-SharedStringValues -SharedStringsXml $sharedStringsXml

    $relationshipsById = @{}
    foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
        $relationshipsById[$relationship.Id] = "xl/" + $relationship.Target.TrimStart("/")
    }

    $forceSheetPath = Get-WorksheetPath -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SheetName "Force"
    [xml]$forceWorksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $forceSheetPath
    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($forceWorksheetXml.NameTable)
    $namespaceManager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    $updated = 0
    foreach ($rowNode in $forceWorksheetXml.SelectNodes("//d:sheetData/d:row", $namespaceManager)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText)) {
            continue
        }

        $rowNumber = [int]$rowNumberText
        $nameCell = $forceWorksheetXml.SelectSingleNode("//d:c[@r='C$rowNumber']", $namespaceManager)
        $perkName = Get-CellText -Cell $nameCell -SharedStrings $sharedStrings
        if (!$noteByPerk.ContainsKey($perkName)) {
            continue
        }

        $notesCell = $forceWorksheetXml.SelectSingleNode("//d:c[@r='S$rowNumber']", $namespaceManager)
        if ($null -eq $notesCell) {
            $notesCell = $forceWorksheetXml.CreateElement("c", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
            $notesCell.SetAttribute("r", "S$rowNumber")
            [void]$rowNode.AppendChild($notesCell)
        }

        Set-TextCell -WorksheetXml $forceWorksheetXml -Cell $notesCell -Value $noteByPerk[$perkName]
        $updated++
    }

    Write-ZipEntryXml -Zip $zip -EntryPath $forceSheetPath -Xml $forceWorksheetXml
}
finally {
    $zip.Dispose()
}

[pscustomobject]@{
    Sheet = "Force"
    UpdatedNotes = $updated
} | Format-List

Write-Host "Cleaned Force category wording in '$workbookPath'."
