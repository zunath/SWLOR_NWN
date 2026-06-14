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

function Get-OpenXmlColumnName {
    param([int]$ColumnIndex)

    $name = ""
    while ($ColumnIndex -gt 0) {
        $ColumnIndex--
        $name = [char]([int][char]'A' + ($ColumnIndex % 26)) + $name
        $ColumnIndex = [math]::Floor($ColumnIndex / 26)
    }

    return $name
}

function Get-CanonicalHeader {
    param([string]$Text)

    $normalized = ($Text -replace "[\s\.\?]+", "").ToLowerInvariant()
    switch ($normalized) {
        "perkname" { return "PerkName" }
        "name" { return "Name" }
        "description" { return "Description" }
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

    $rowNumber = [int]$RowNode.GetAttribute("r")
    $cell.SetAttribute("r", "$(Get-OpenXmlColumnName $ColumnIndex)$rowNumber")

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

function Get-HeaderInfo {
    param(
        [xml]$WorksheetXml,
        [System.Collections.Generic.IList[string]]$SharedStrings,
        [string]$HeaderPattern
    )

    $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($WorksheetXml.NameTable)
    $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

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

        if (($cells.Values -join "|") -match $HeaderPattern) {
            $columnByHeader = @{}
            foreach ($cellEntry in $cells.GetEnumerator()) {
                $canonicalHeader = Get-CanonicalHeader $cellEntry.Value
                if (![string]::IsNullOrWhiteSpace($canonicalHeader) -and !$columnByHeader.ContainsKey($canonicalHeader)) {
                    $columnByHeader[$canonicalHeader] = $cellEntry.Key
                }
            }

            return [pscustomobject]@{
                RowNumber = [int]$rowNumberText
                ColumnByHeader = $columnByHeader
            }
        }
    }

    throw "Header matching '$HeaderPattern' was not found."
}

function Update-ForceRows {
    param(
        [xml]$WorksheetXml,
        [System.Collections.Generic.IList[string]]$SharedStrings
    )

    $updatesByName = @{
        "Mind Trick I" = @{
            Description = "Attempts to inflict Confusion on one non-mechanical target for 10 seconds. Caster Willpower increases duration, while target Willpower and Mind Resistance reduce it."
            Notes = "Sense mind-control utility. Confusion mechanics are defined in the Status Effects tab."
        }
        "Mind Trick II" = @{
            Description = "Attempts to inflict Confusion on the selected non-mechanical target and one nearby non-mechanical target for 10 seconds. Caster Willpower increases duration, while target Willpower and Mind Resistance reduce it."
            Notes = "Replacement tier: expands target count and uses the same Willpower contest as rank I. Confusion mechanics are defined in the Status Effects tab."
        }
    }

    $headerInfo = Get-HeaderInfo -WorksheetXml $WorksheetXml -SharedStrings $SharedStrings -HeaderPattern "Perk Name|PerkName"
    foreach ($requiredHeader in @("PerkName", "Description", "Notes")) {
        if (!$headerInfo.ColumnByHeader.ContainsKey($requiredHeader)) {
            throw "Workbook sheet 'Force' is missing required column '$requiredHeader'."
        }
    }

    $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($WorksheetXml.NameTable)
    $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    $updatedRows = [System.Collections.Generic.List[object]]::new()
    foreach ($rowNode in $WorksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText) -or [int]$rowNumberText -le $headerInfo.RowNumber) {
            continue
        }

        $perkNameCell = Get-WorksheetCell -RowNode $rowNode -ColumnIndex $headerInfo.ColumnByHeader["PerkName"]
        $perkName = Get-OpenXmlCellText -Cell $perkNameCell -SharedStrings $SharedStrings
        if (!$updatesByName.ContainsKey($perkName)) {
            continue
        }

        $update = $updatesByName[$perkName]
        Set-CellText -WorksheetXml $WorksheetXml -RowNode $rowNode -ColumnIndex $headerInfo.ColumnByHeader["Description"] -Text $update.Description
        Set-CellText -WorksheetXml $WorksheetXml -RowNode $rowNode -ColumnIndex $headerInfo.ColumnByHeader["Notes"] -Text $update.Notes
        $updatedRows.Add([pscustomobject]@{ Row = [int]$rowNumberText; PerkName = $perkName }) | Out-Null
    }

    $missingNames = $updatesByName.Keys | Where-Object { $name = $_; -not ($updatedRows | Where-Object { $_.PerkName -eq $name }) }
    if ($missingNames) {
        throw "Did not find expected Force perks: $($missingNames -join ', ')"
    }

    return $updatedRows
}

function Update-StatusEffectsRows {
    param(
        [xml]$WorksheetXml,
        [System.Collections.Generic.IList[string]]$SharedStrings
    )

    $confusionDescription = "When the target attempts to activate a hostile combat, Force, Device, or NPC ability, the activation has a source-defined chance to fail. Default chance is 25%. Failed activations deal no damage, apply no effects, and do not consume FP, STM, or trigger cooldown. Basic attacks are unaffected."

    $headerInfo = Get-HeaderInfo -WorksheetXml $WorksheetXml -SharedStrings $SharedStrings -HeaderPattern "Name.*Description|Description.*Name"
    foreach ($requiredHeader in @("Name", "Description")) {
        if (!$headerInfo.ColumnByHeader.ContainsKey($requiredHeader)) {
            throw "Workbook sheet 'Status Effects' is missing required column '$requiredHeader'."
        }
    }

    $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($WorksheetXml.NameTable)
    $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"

    $lastContentRowNumber = $headerInfo.RowNumber
    $lastContentRow = $null
    $confusionRows = [System.Collections.Generic.List[System.Xml.XmlElement]]::new()
    foreach ($rowNode in $WorksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText) -or [int]$rowNumberText -le $headerInfo.RowNumber) {
            continue
        }

        $nameCell = Get-WorksheetCell -RowNode $rowNode -ColumnIndex $headerInfo.ColumnByHeader["Name"]
        $name = Get-OpenXmlCellText -Cell $nameCell -SharedStrings $SharedStrings
        $descriptionCell = Get-WorksheetCell -RowNode $rowNode -ColumnIndex $headerInfo.ColumnByHeader["Description"]
        $description = Get-OpenXmlCellText -Cell $descriptionCell -SharedStrings $SharedStrings
        if ($name -eq "Confusion") {
            $confusionRows.Add($rowNode) | Out-Null
            continue
        }

        if (![string]::IsNullOrWhiteSpace($name) -or ![string]::IsNullOrWhiteSpace($description)) {
            $lastContentRowNumber = [Math]::Max($lastContentRowNumber, [int]$rowNumberText)
            if ([int]$rowNumberText -eq $lastContentRowNumber) {
                $lastContentRow = $rowNode
            }
        }
    }

    $desiredRowNumber = $lastContentRowNumber + 1
    $confusionRow = $WorksheetXml.SelectSingleNode("//d:sheetData/d:row[@r='$desiredRowNumber']", $worksheetNamespace)
    if ($null -eq $confusionRow) {
        $sheetData = $WorksheetXml.SelectSingleNode("//d:sheetData", $worksheetNamespace)
        $confusionRow = $WorksheetXml.CreateElement("row", $namespace)
        $confusionRow.SetAttribute("r", [string]$desiredRowNumber)
        if ($null -ne $lastContentRow) {
            [void]$sheetData.InsertAfter($confusionRow, $lastContentRow)
        }
        else {
            [void]$sheetData.AppendChild($confusionRow)
        }

        $dimension = $WorksheetXml.SelectSingleNode("//d:dimension", $worksheetNamespace)
        if ($null -ne $dimension) {
            $dimension.SetAttribute("ref", "A1:B$desiredRowNumber")
        }
    }

    Set-CellText -WorksheetXml $WorksheetXml -RowNode $confusionRow -ColumnIndex $headerInfo.ColumnByHeader["Name"] -Text "Confusion"
    Set-CellText -WorksheetXml $WorksheetXml -RowNode $confusionRow -ColumnIndex $headerInfo.ColumnByHeader["Description"] -Text $confusionDescription

    foreach ($extraConfusionRow in $confusionRows) {
        if ([int]$extraConfusionRow.GetAttribute("r") -ne $desiredRowNumber) {
            [void]$extraConfusionRow.ParentNode.RemoveChild($extraConfusionRow)
        }
    }

    return [pscustomobject]@{
        Row = [int]$confusionRow.GetAttribute("r")
        Name = "Confusion"
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

    $forceSheetPath = Get-WorksheetPath -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SheetName "Force"
    $statusEffectsSheetPath = Get-WorksheetPath -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SheetName "Status Effects"

    [xml]$forceWorksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $forceSheetPath
    [xml]$statusEffectsWorksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $statusEffectsSheetPath

    $updatedForceRows = Update-ForceRows -WorksheetXml $forceWorksheetXml -SharedStrings $sharedStrings
    $updatedStatusRow = Update-StatusEffectsRows -WorksheetXml $statusEffectsWorksheetXml -SharedStrings $sharedStrings

    Write-ZipEntryXml -Zip $zip -EntryPath $forceSheetPath -Xml $forceWorksheetXml
    Write-ZipEntryXml -Zip $zip -EntryPath $statusEffectsSheetPath -Xml $statusEffectsWorksheetXml
}
finally {
    $zip.Dispose()
}

$updatedForceRows | Sort-Object Row | Format-Table -AutoSize
$updatedStatusRow | Format-Table -AutoSize
Write-Host "Updated Confusion status design feedback in '$workbookPath'."
