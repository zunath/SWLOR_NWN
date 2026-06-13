param(
    [string]$BibleWorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx"
)

# 2026-06-11 Devices category-parity pass.
# The tier-consistency pass left Devices category totals lopsided at
# 58/55/56/71 (Grenadier/Field Engineer/Field Support/Assault Gadgets)
# because Assault Gadgets carries 21 rows versus 15-16 elsewhere. Category
# parity is the binding constraint: each Devices category must cost the same
# SP to complete. This pass reshapes prices within each category to land all
# four at 60 SP while keeping the Devices total at 240 SP, equal to Force.
# The Force/Devices twin lines (Throw Rock <-> Arc Projector at 2/3/4 and
# Radiant Lance <-> Ion Lance at 3/4/4) are not touched, so the Force sheet
# is unchanged. Assault Gadgets rows price lower on average by design
# because the category contains six more perks than its peers.

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

# Grenadier: 58 -> 60
# Field Engineer: 55 -> 60
# Field Support: 56 -> 60
# Assault Gadgets: 71 -> 60
$devicePrices = @{
    "Concussion Grenade II" = 4
    "Ion Grenade II" = 4
    "Blaster Beacon I" = 3
    "Incendiary Field II" = 4
    "Shock Beacon II" = 5
    "Diagnostic Sweep" = 5
    "Beacon Targeting II" = 5
    "Deflector Shield I" = 3
    "Overclock Routine" = 5
    "Rayshield Screen II" = 5
    "Group Deflector" = 5
    "Sonic Burst I" = 2
    "Sonic Burst II" = 3
    "Sonic Burst III" = 3
    "Gadget Harness" = 2
    "Tactical Uplink" = 2
    "Flamethrower II" = 2
    "Flamethrower III" = 3
    "Rail Dart I" = 2
    "Rail Dart II" = 3
    "Wrist Rocket II" = 3
    "Wrist Rocket III" = 3
    "Cryo Sprayer" = 3
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

    $devicesContext = Get-SheetContext -Zip $zip -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SharedStrings $sharedStrings -SheetName "Devices"
    $updates = Set-Prices -Context $devicesContext -SharedStrings $sharedStrings -PriceByPerkName $devicePrices
    Write-ZipEntryXml -Zip $zip -EntryPath $devicesContext.Path -Xml $devicesContext.Xml
}
finally {
    $zip.Dispose()
}

$sectionRepairScriptPath = Join-Path $PSScriptRoot "RepairCombatBibleSectionTotalSp.ps1"
if (Test-Path $sectionRepairScriptPath) {
    & $sectionRepairScriptPath -BibleWorkbookPath $BibleWorkbookPath
}

$totalRepairScriptPath = Join-Path $PSScriptRoot "RepairCombatBibleTotalSp.ps1"
if (Test-Path $totalRepairScriptPath) {
    & $totalRepairScriptPath -BibleWorkbookPath $BibleWorkbookPath
}

$updates | Sort-Object Row | Format-Table Row, PerkName, Price -AutoSize
Write-Host "Applied Devices category-parity pass in '$workbookPath'."
