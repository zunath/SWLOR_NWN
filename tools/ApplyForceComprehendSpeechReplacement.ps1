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

function Get-CanonicalHeader {
    param([string]$Text)

    $normalized = ($Text -replace "[\s\.\?]+", "").ToLowerInvariant()
    switch ($normalized) {
        "perkname" { return "PerkName" }
        "name" { return "PerkName" }
        "description" { return "Description" }
        "primarystat" { return "PrimaryStat" }
        "secondarystat" { return "SecondaryStat" }
        "scalingsource" { return "ScalingSource" }
        "devstatus" { return "DevStatus" }
        "notes" { return "Notes" }
        default { return $null }
    }
}

function Get-OpenXmlCellText {
    param(
        [System.Xml.XmlElement]$Cell,
        [System.Collections.Generic.IList[string]]$SharedStrings
    )

    if ($null -eq $Cell) {
        return ""
    }

    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($Cell.OwnerDocument.NameTable)
    $namespaceManager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    $type = $Cell.GetAttribute("t")
    if ($type -eq "inlineStr") {
        $textNode = $Cell.SelectSingleNode("d:is/d:t", $namespaceManager)
        if ($null -eq $textNode) {
            return ""
        }

        return $textNode.InnerText
    }

    $valueNode = $Cell.SelectSingleNode("d:v", $namespaceManager)
    if ($null -eq $valueNode) {
        return ""
    }

    if ($type -eq "s") {
        return $SharedStrings[[int]$valueNode.InnerText]
    }

    return $valueNode.InnerText
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

function Set-CellText {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex,
        [string]$Text
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $cell = Get-WorksheetCell -RowNode $RowNode -ColumnIndex $ColumnIndex
    if ($null -eq $cell) {
        $cell = $WorksheetXml.CreateElement("c", $namespace)
        [void]$RowNode.AppendChild($cell)
    }

    while ($cell.HasChildNodes) {
        [void]$cell.RemoveChild($cell.FirstChild)
    }

    $cell.SetAttribute("t", "inlineStr")
    $inlineString = $WorksheetXml.CreateElement("is", $namespace)
    $textElement = $WorksheetXml.CreateElement("t", $namespace)
    $textElement.InnerText = $Text
    [void]$inlineString.AppendChild($textElement)
    [void]$cell.AppendChild($inlineString)
}

$replacement = @{
    PerkName = "Precognition"
    Description = "After spending FP on a Force power, gain +5% Defense and +5% Evasion for 8 seconds. This can trigger once every 12 seconds."
    PrimaryStat = "None"
    SecondaryStat = "None"
    ScalingSource = "Design Added"
    DevStatus = "Design Added"
    Notes = "Universal combat trait that benefits any Force style without adding an active button or equipment requirement."
}

$workbookPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookPath)) {
    throw "Workbook '$workbookPath' was not found."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($workbookPath, [System.IO.Compression.ZipArchiveMode]::Update)

try {
    $sharedStrings = [System.Collections.Generic.List[string]]::new()
    if ($null -ne $zip.GetEntry("xl/sharedStrings.xml")) {
        [xml]$sharedStringsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/sharedStrings.xml"
        $sharedStringsNamespace = [System.Xml.XmlNamespaceManager]::new($sharedStringsXml.NameTable)
        $sharedStringsNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
        foreach ($sharedStringNode in $sharedStringsXml.SelectNodes("//d:si", $sharedStringsNamespace)) {
            $sharedStrings.Add(($sharedStringNode.SelectNodes(".//d:t", $sharedStringsNamespace) | ForEach-Object { $_.InnerText }) -join "") | Out-Null
        }
    }

    [xml]$workbookXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/workbook.xml"
    [xml]$relationshipsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/_rels/workbook.xml.rels"

    $relationshipsById = @{}
    foreach ($relationship in $relationshipsXml.Relationships.Relationship) {
        $relationshipsById[$relationship.Id] = "xl/" + $relationship.Target.TrimStart("/")
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

    $columnByHeader = @{}
    $headerRowNumber = 0
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

        if (($cells.Values -join "|") -match "Perk Name|PerkName") {
            $headerRowNumber = [int]$rowNumberText
            foreach ($cellEntry in $cells.GetEnumerator()) {
                $canonicalHeader = Get-CanonicalHeader $cellEntry.Value
                if (![string]::IsNullOrWhiteSpace($canonicalHeader) -and !$columnByHeader.ContainsKey($canonicalHeader)) {
                    $columnByHeader[$canonicalHeader] = $cellEntry.Key
                }
            }
            break
        }
    }

    foreach ($requiredHeader in @("PerkName", "Description", "PrimaryStat", "SecondaryStat", "ScalingSource", "DevStatus", "Notes")) {
        if (!$columnByHeader.ContainsKey($requiredHeader)) {
            throw "Workbook sheet 'Force' is missing required column '$requiredHeader'."
        }
    }

    $updatedRows = [System.Collections.Generic.List[object]]::new()
    foreach ($rowNode in $worksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText) -or [int]$rowNumberText -le $headerRowNumber) {
            continue
        }

        $perkNameCell = Get-WorksheetCell -RowNode $rowNode -ColumnIndex $columnByHeader["PerkName"]
        $perkName = Get-OpenXmlCellText -Cell $perkNameCell -SharedStrings $sharedStrings
        if ([int]$rowNumberText -ne 14 -and $perkName -ne $replacement.PerkName) {
            continue
        }

        foreach ($updateColumn in $replacement.Keys) {
            Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader[$updateColumn] -Text $replacement[$updateColumn]
        }

        $updatedRows.Add([pscustomobject]@{ Row = [int]$rowNumberText; OldPerkName = $perkName; NewPerkName = $replacement.PerkName }) | Out-Null
    }

    if ($updatedRows.Count -ne 1) {
        throw "Expected to update exactly one Force Universal row 14 replacement, updated $($updatedRows.Count)."
    }

    Write-ZipEntryXml -Zip $zip -EntryPath $forceSheetPath -Xml $worksheetXml
}
finally {
    $zip.Dispose()
}

$updatedRows | Format-Table -AutoSize
Write-Host "Updated Force Universal row 14 replacement in '$workbookPath'."
