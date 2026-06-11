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

$rowUpdatesByName = @{
    "Utility Belt I" = @{
        PerkName = "Integrated Toolkit I"
        Description = "After using a Device ability, gain Integrated Toolkit for 10 seconds: +4% physical and Force ability Accuracy, +4% Defense, and +4% Evasion. This can refresh but does not stack."
        Notes = "Replaces the idle STM discount with an always-relevant device-user readiness trait."
    }
    "Utility Belt II" = @{
        PerkName = "Integrated Toolkit II"
        Description = "After using a Device ability, gain Integrated Toolkit for 12 seconds: +5% physical and Force ability Accuracy, +5% Defense, +5% Evasion, and +5 Trauma Resistance rating. This can refresh but does not stack."
        Notes = "Replacement tier: improves the universal device-user buff without depending on the Device ability's damage, healing, Accuracy, or critical chance."
    }
    "Utility Belt III" = @{
        PerkName = "Integrated Toolkit III"
        Description = "After using a Device ability, gain Integrated Toolkit for 12 seconds: +6% physical and Force ability Accuracy, +6% Defense, +6% Evasion, +8 Trauma Resistance rating, and 1 STM every 4 seconds. This can refresh but does not stack."
        Notes = "Replacement tier: adds modest sustained STM flow for regular Device users instead of rewarding gaps in Device usage."
    }
    "Adaptive Circuits" = @{
        Description = "When a Device ability affects at least one enemy, gain +8% physical and Force ability Accuracy and +8% critical chance for 12 seconds. When a Device ability affects at least one ally, including yourself, gain +8% Defense and +8% Evasion for 12 seconds. Both effects can be active at once."
        Notes = "Rethought as an adaptive combat trait that provides useful value for offensive, defensive, and utility Device abilities."
    }
    "Emergency Override" = @{
        Description = "When damage or resource spending leaves you below 35% HP or 35% STM, gain temporary HP equal to 20% of maximum HP plus PER scaling, restore 4 STM, and remove one standard negative effect. This can trigger once every 90 seconds."
        Notes = "Universal Devices capstone: emergency survival and recovery without requiring the player to hold a weak conditional Device use."
    }
    "Capacitor Rig I" = @{
        Description = "After you use two Field Support combat abilities within 20 seconds, restore 5% maximum STM to yourself and one ally within 10m. This can trigger once every 20 seconds."
        Notes = "Replaces direct shielding-perk amplification with a self-contained Field Support resource-flow line."
    }
    "Capacitor Rig II" = @{
        Description = "After you use two Field Support combat abilities within 20 seconds, restore 8% maximum STM to yourself and up to two allies within 10m. This can trigger once every 20 seconds."
        Notes = "Replacement tier: expands the Capacitor Rig resource payoff without increasing another perk line's shield values."
    }
    "Capacitor Rig III" = @{
        Description = "After you use two Field Support combat abilities within 20 seconds, restore 10% maximum STM to yourself and allies within 10m, and grant affected allies +5% Defense for 10 seconds. This can trigger once every 20 seconds."
        Notes = "Replacement tier: turns Capacitor Rig into a Field Support cadence reward instead of a multiplier on Deflector Shield, Group Deflector, or Emergency Bunker."
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

    $workbookNamespace = [System.Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
    $workbookNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $workbookNamespace.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")

    $devicesSheetPath = $null
    foreach ($sheet in $workbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
        if ($sheet.GetAttribute("name") -ne "Devices") {
            continue
        }

        $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $devicesSheetPath = $relationshipsById[$relationshipId]
        break
    }

    if ([string]::IsNullOrWhiteSpace($devicesSheetPath)) {
        throw "Workbook sheet 'Devices' was not found."
    }

    [xml]$worksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $devicesSheetPath
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

    foreach ($requiredHeader in @("PerkName", "Description", "Notes")) {
        if (!$columnByHeader.ContainsKey($requiredHeader)) {
            throw "Workbook sheet 'Devices' is missing required column '$requiredHeader'."
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
        if (!$rowUpdatesByName.ContainsKey($perkName)) {
            continue
        }

        $update = $rowUpdatesByName[$perkName]
        foreach ($updateColumn in $update.Keys) {
            Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader[$updateColumn] -Text $update[$updateColumn]
        }

        $newPerkName = $perkName
        if ($update.ContainsKey("PerkName")) {
            $newPerkName = $update["PerkName"]
        }

        $updatedRows.Add([pscustomobject]@{ Row = [int]$rowNumberText; PerkName = $perkName; NewPerkName = $newPerkName }) | Out-Null
    }

    $missingNames = $rowUpdatesByName.Keys | Where-Object { $name = $_; -not ($updatedRows | Where-Object { $_.PerkName -eq $name }) }
    if ($missingNames) {
        throw "Did not find expected Device perks: $($missingNames -join ', ')"
    }

    Write-ZipEntryXml -Zip $zip -EntryPath $devicesSheetPath -Xml $worksheetXml
}
finally {
    $zip.Dispose()
}

$updatedRows | Sort-Object Row | Format-Table -AutoSize
Write-Host "Updated targeted Devices design feedback in '$workbookPath'."
