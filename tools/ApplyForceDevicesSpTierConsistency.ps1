param(
    [string]$BibleWorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx"
)

# 2026-06-11 SP tier-consistency pass.
# The prior SP rebalance reached Force/Devices parity (237/237) by pricing rows
# against the total instead of against their requirement band: the additive
# Arc Projector, Ion Lance, and Radiant Lance lines were priced at 1-2 SP while
# in-band peers cost 2-4, and several mid-tier Devices rows (Signal Jammer,
# Dampening Field I, Beacon Targeting, Rayshield Screen, Disruption Pulse,
# Diagnostic Sweep) were inflated to absorb the difference.
# This pass reprices every outlier to its requirement band and keeps the
# Force/Devices twin lines at identical prices:
#   Throw Rock (Force 0/18/40) 2/3/4  <-> Arc Projector (Devices 12/30/45) 2/3/4
#   Radiant Lance (Force 15/32/48) 3/4/4 <-> Ion Lance (Devices 15/32/48) 3/4/4
# Result: Force and Devices remain equal, both moving from 237 SP to 240 SP.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$normalStyleId = "4"

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
        default { return "" }
    }
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
    [void]$RowNode.AppendChild($cell)
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

function Set-CellNumber {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$Cell,
        [double]$Value
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    Clear-Cell -Cell $Cell
    $Cell.SetAttribute("s", $normalStyleId)

    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $Value.ToString("0.############", [System.Globalization.CultureInfo]::InvariantCulture)
    [void]$Cell.AppendChild($valueElement)
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
        $rowText = @()
        foreach ($cell in $rowNode.SelectNodes("d:c", $worksheetNamespace)) {
            $rowText += Get-OpenXmlCellText -Cell $cell -SharedStrings $SharedStrings
        }

        if (($rowText -join "|") -notmatch "Perk Name|PerkName") {
            continue
        }

        foreach ($cell in $rowNode.SelectNodes("d:c", $worksheetNamespace)) {
            $canonicalHeader = Get-CanonicalHeader (Get-OpenXmlCellText -Cell $cell -SharedStrings $SharedStrings)
            if (![string]::IsNullOrWhiteSpace($canonicalHeader) -and !$columnByHeader.ContainsKey($canonicalHeader)) {
                $columnByHeader[$canonicalHeader] = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
            }
        }

        break
    }

    if (!$columnByHeader.ContainsKey("PerkName") -or !$columnByHeader.ContainsKey("Price")) {
        throw "Workbook sheet '$SheetName' is missing PerkName or Price columns."
    }

    return [pscustomobject]@{
        Path = $sheetPath
        Xml = $worksheetXml
        Namespace = $worksheetNamespace
        ColumnByHeader = $columnByHeader
    }
}

function Set-Prices {
    param(
        [object]$Context,
        [System.Collections.Generic.IList[string]]$SharedStrings,
        [hashtable]$PriceByPerkName
    )

    $updates = @()
    $seen = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($rowNode in $Context.Xml.SelectNodes("//d:sheetData/d:row", $Context.Namespace)) {
        $perkName = Get-OpenXmlCellText -Cell (Get-CellByColumn -RowNode $rowNode -ColumnIndex $Context.ColumnByHeader["PerkName"]) -SharedStrings $SharedStrings
        if (!$PriceByPerkName.ContainsKey($perkName)) {
            continue
        }

        [void]$seen.Add($perkName)
        $priceCell = Get-OrCreateCell -WorksheetXml $Context.Xml -RowNode $rowNode -ColumnIndex $Context.ColumnByHeader["Price"]
        Set-CellNumber -WorksheetXml $Context.Xml -Cell $priceCell -Value ([double]$PriceByPerkName[$perkName])
        $updates += [pscustomobject]@{
            Sheet = ""
            Row = [int]$rowNode.GetAttribute("r")
            PerkName = $perkName
            Price = $PriceByPerkName[$perkName]
        }
    }

    foreach ($expectedName in $PriceByPerkName.Keys) {
        if (!$seen.Contains($expectedName)) {
            throw "Perk '$expectedName' was not found on the worksheet."
        }
    }

    return $updates
}

$forcePrices = @{
    "Radiant Lance I" = 3
    "Radiant Lance II" = 4
}

$devicePrices = @{
    "Arc Projector I" = 2
    "Arc Projector II" = 3
    "Arc Projector III" = 4
    "Ion Lance I" = 3
    "Ion Lance II" = 4
    "Ion Lance III" = 4
    "Beacon Targeting I" = 3
    "Beacon Targeting II" = 4
    "Signal Jammer" = 4
    "Rayshield Screen I" = 3
    "Rayshield Screen II" = 4
    "Dampening Field I" = 4
    "Disruption Pulse" = 4
    "Diagnostic Sweep" = 4
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
    $forceUpdates = Set-Prices -Context $forceContext -SharedStrings $sharedStrings -PriceByPerkName $forcePrices
    foreach ($update in $forceUpdates) {
        $update.Sheet = "Force"
        $updates += $update
    }
    Write-ZipEntryXml -Zip $zip -EntryPath $forceContext.Path -Xml $forceContext.Xml

    $devicesContext = Get-SheetContext -Zip $zip -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SharedStrings $sharedStrings -SheetName "Devices"
    $deviceUpdates = Set-Prices -Context $devicesContext -SharedStrings $sharedStrings -PriceByPerkName $devicePrices
    foreach ($update in $deviceUpdates) {
        $update.Sheet = "Devices"
        $updates += $update
    }
    Write-ZipEntryXml -Zip $zip -EntryPath $devicesContext.Path -Xml $devicesContext.Xml
}
finally {
    $zip.Dispose()
}

$totalRepairScriptPath = Join-Path $PSScriptRoot "RepairCombatBibleTotalSp.ps1"
if (Test-Path $totalRepairScriptPath) {
    & $totalRepairScriptPath -BibleWorkbookPath $BibleWorkbookPath
}

$updates | Sort-Object Sheet, Row | Format-Table Sheet, Row, PerkName, Price -AutoSize
Write-Host "Applied Force/Devices SP tier-consistency pass in '$workbookPath'."
