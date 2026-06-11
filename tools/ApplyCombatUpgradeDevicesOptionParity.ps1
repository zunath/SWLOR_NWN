[CmdletBinding()]
param(
    [string]$BibleWorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx"
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

    $cellType = $Cell.GetAttribute("t")
    if ($cellType -eq "inlineStr") {
        return Normalize-CellText $Cell.InnerText
    }

    $rawValue = $Cell.InnerText
    if ([string]::IsNullOrWhiteSpace($rawValue)) {
        return ""
    }

    if ($cellType -eq "s") {
        return Normalize-CellText $SharedStrings[[int]$rawValue]
    }

    return Normalize-CellText $rawValue
}

function Get-CanonicalManifestHeader {
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
        "skillreqs" { return "SkillRequirements" }
        "skillrequirements" { return "SkillRequirements" }
        "requirements" { return "SkillRequirements" }
        "chartype" { return "CharacterType" }
        "charactertype" { return "CharacterType" }
        "type" { return "Type" }
        "description" { return "Description" }
        "primarystat" { return "PrimaryStat" }
        "secondarystat" { return "SecondaryStat" }
        "scalingsource" { return "ScalingSource" }
        "crossskill" { return "CrossSkill" }
        "fp" { return "FP" }
        "stm" { return "STM" }
        "castingtime" { return "CastingTime" }
        "cooldowntime" { return "CooldownTime" }
        "cooldown" { return "CooldownTime" }
        "devstatus" { return "DevStatus" }
        "additionalrequirements" { return "AdditionalRequirements" }
        "notes" { return "Notes" }
        default { return "" }
    }
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
    $rowNumber = [int]$RowNode.GetAttribute("r")
    $cellReference = "$(ConvertTo-OpenXmlColumnName $ColumnIndex)$rowNumber"
    $cell = Get-WorksheetCell -RowNode $RowNode -ColumnIndex $ColumnIndex

    if ($null -eq $cell) {
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
    }

    while ($cell.FirstChild) {
        [void]$cell.RemoveChild($cell.FirstChild)
    }

    $cell.SetAttribute("t", "inlineStr")
    $inlineString = $WorksheetXml.CreateElement("is", $namespace)
    $textElement = $WorksheetXml.CreateElement("t", $namespace)
    [void]$textElement.SetAttribute("space", "http://www.w3.org/XML/1998/namespace", "preserve")
    $textElement.InnerText = $Text
    [void]$inlineString.AppendChild($textElement)
    [void]$cell.AppendChild($inlineString)
}

function Set-CellNumber {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex,
        [string]$NumberText
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $rowNumber = [int]$RowNode.GetAttribute("r")
    $cellReference = "$(ConvertTo-OpenXmlColumnName $ColumnIndex)$rowNumber"
    $cell = Get-WorksheetCell -RowNode $RowNode -ColumnIndex $ColumnIndex

    if ($null -eq $cell) {
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
    }

    while ($cell.FirstChild) {
        [void]$cell.RemoveChild($cell.FirstChild)
    }

    [void]$cell.RemoveAttribute("t")
    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $NumberText
    [void]$cell.AppendChild($valueElement)
}

function Set-CellFormula {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex,
        [string]$Formula,
        [string]$CachedValue
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $rowNumber = [int]$RowNode.GetAttribute("r")
    $cellReference = "$(ConvertTo-OpenXmlColumnName $ColumnIndex)$rowNumber"
    $cell = Get-WorksheetCell -RowNode $RowNode -ColumnIndex $ColumnIndex

    if ($null -eq $cell) {
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
    }

    while ($cell.FirstChild) {
        [void]$cell.RemoveChild($cell.FirstChild)
    }

    [void]$cell.RemoveAttribute("t")
    $formulaElement = $WorksheetXml.CreateElement("f", $namespace)
    $formulaElement.InnerText = $Formula
    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $CachedValue
    [void]$cell.AppendChild($formulaElement)
    [void]$cell.AppendChild($valueElement)
}

function Set-CellStyle {
    param(
        [System.Xml.XmlElement]$RowNode,
        [int]$ColumnIndex,
        [string]$StyleIndex
    )

    $cell = Get-WorksheetCell -RowNode $RowNode -ColumnIndex $ColumnIndex
    if ($null -ne $cell) {
        $cell.SetAttribute("s", $StyleIndex)
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
        $settings.Encoding = [System.Text.Encoding]::UTF8
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

function Get-OrCreateRow {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$SheetData,
        [int]$RowNumber
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"

    foreach ($rowNode in $SheetData.GetElementsByTagName("row", $namespace)) {
        if ([int]$rowNode.GetAttribute("r") -eq $RowNumber) {
            return $rowNode
        }
    }

    $newRow = $WorksheetXml.CreateElement("row", $namespace)
    $newRow.SetAttribute("r", [string]$RowNumber)

    $insertBefore = $null
    foreach ($rowNode in $SheetData.GetElementsByTagName("row", $namespace)) {
        $candidateRow = [int]$rowNode.GetAttribute("r")
        if ($candidateRow -gt $RowNumber) {
            $insertBefore = $rowNode
            break
        }
    }

    if ($null -eq $insertBefore) {
        [void]$SheetData.AppendChild($newRow)
    }
    else {
        [void]$SheetData.InsertBefore($newRow, $insertBefore)
    }

    return $newRow
}

function Update-RowValues {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [hashtable]$ColumnByHeader,
        [hashtable]$Values
    )

    foreach ($entry in $Values.GetEnumerator()) {
        if (!$ColumnByHeader.ContainsKey($entry.Key)) {
            throw "Workbook sheet 'Devices' is missing required column '$($entry.Key)'."
        }

        if ($entry.Key -in @("Price", "FP", "STM") -and ![string]::IsNullOrWhiteSpace($entry.Value) -and $entry.Value -ne "-") {
            Set-CellNumber -WorksheetXml $WorksheetXml -RowNode $RowNode -ColumnIndex $ColumnByHeader[$entry.Key] -NumberText ([string][int][decimal]$entry.Value)
        }
        else {
            Set-CellText -WorksheetXml $WorksheetXml -RowNode $RowNode -ColumnIndex $ColumnByHeader[$entry.Key] -Text $entry.Value
        }
    }
}

$newTraitDefaults = @{
    CharacterType = "All"
    Type = "Trait"
    PrimaryStat = "PER"
    SecondaryStat = "None"
    ScalingSource = "Design Added"
    FP = "-"
    STM = "-"
    CastingTime = "-"
    CooldownTime = "-"
    DevStatus = "Design Added"
    AdditionalRequirements = ""
}

function New-DeviceTraitRow {
    param(
        [int]$Row,
        [string]$Style,
        [string]$Price,
        [string]$PerkName,
        [string]$SkillRequirements,
        [string]$Description,
        [string]$Notes
    )

    $rowData = @{
        Row = $Row
        Style = $Style
        Price = $Price
        PerkName = $PerkName
        SkillRequirements = $SkillRequirements
        Description = $Description
        Notes = $Notes
    }

    foreach ($entry in $newTraitDefaults.GetEnumerator()) {
        $rowData[$entry.Key] = $entry.Value
    }

    return $rowData
}

function New-DeviceCombatRow {
    param(
        [int]$Row,
        [string]$Style,
        [string]$Price,
        [string]$PerkName,
        [string]$SkillRequirements,
        [string]$Description,
        [string]$FP,
        [string]$STM,
        [string]$CastingTime,
        [string]$CooldownTime,
        [string]$Notes
    )

    return @{
        Row = $Row
        Style = $Style
        Price = $Price
        PerkName = $PerkName
        SkillRequirements = $SkillRequirements
        CharacterType = "All"
        Type = "Combat"
        Description = $Description
        PrimaryStat = "PER"
        SecondaryStat = "None"
        ScalingSource = "Design Added"
        FP = $FP
        STM = $STM
        CastingTime = $CastingTime
        CooldownTime = $CooldownTime
        DevStatus = "Design Added"
        AdditionalRequirements = ""
        Notes = $Notes
    }
}

$deviceRows = @(
    (New-DeviceTraitRow -Row 9 -Style "Universal" -Price "3" -PerkName "Integrated Toolkit I" -SkillRequirements "Devices 5" -Description "After using a Device ability, gain Integrated Toolkit for 10 seconds: +4% physical and Force ability Accuracy, +4% Defense, and +4% Evasion. This can refresh but does not stack." -Notes "Replaces the idle STM discount with an always-relevant device-user readiness trait.")
    (New-DeviceCombatRow -Row 10 -Style "Universal" -Price "4" -PerkName "Deploy Cover" -SkillRequirements "Devices 8" -Description "Deploys portable cover at a target point within 12m for 20 seconds. Allies within 3m gain +6% Defense and +6% Evasion." -FP "-" -STM "2" -CastingTime "1.5 seconds" -CooldownTime "30 seconds" -Notes "Shared Devices active. Positional cover stays below Field Engineer's stronger timed defense pulses.")
    (New-DeviceCombatRow -Row 11 -Style "Universal" -Price "4" -PerkName "Signal Jammer" -SkillRequirements "Devices 12" -Description "Deploys a signal jammer for 12 seconds. Hostile targets within 5m suffer -6% physical and Force ability Accuracy and cannot benefit from Haste while inside." -FP "-" -STM "2" -CastingTime "1.5 seconds" -CooldownTime "45 seconds" -Notes "Shared Devices active. Accuracy penalty stays below dedicated control and Leadership debuffs because it also suppresses Haste benefits.")
    (New-DeviceTraitRow -Row 12 -Style "Universal" -Price "4" -PerkName "Integrated Toolkit II" -SkillRequirements "Devices 15" -Description "After using a Device ability, gain Integrated Toolkit for 12 seconds: +5% physical and Force ability Accuracy, +5% Defense, +5% Evasion, and +5 Trauma Resistance rating. This can refresh but does not stack." -Notes "Replacement tier: improves the universal device-user buff without depending on the Device ability's damage, healing, Accuracy, or critical chance.")
    (New-DeviceCombatRow -Row 13 -Style "Universal" -Price "5" -PerkName "Disruption Pulse" -SkillRequirements "Devices 18" -Description "Emits a 5m disruption pulse at a target point within 12m, dealing 10 electrical DMG plus PER scaling to enemies and reducing their Accuracy by 5% for 12 seconds." -FP "-" -STM "3" -CastingTime "1.5 seconds" -CooldownTime "45 seconds" -Notes "Shared Devices active. Lower damage than Assault Gadgets area tools because the universal line also brings broad utility traits.")
    (New-DeviceTraitRow -Row 14 -Style "Universal" -Price "5" -PerkName "Diagnostic Sweep" -SkillRequirements "Devices 22" -Description "Device abilities that affect enemies also reveal hidden targets in the affected area and reduce Evasion by 4% for 10 seconds." -Notes "Shared Devices trait added so Devices and Force have matching 275 SP skill totals.")
    (New-DeviceCombatRow -Row 15 -Style "Universal" -Price "5" -PerkName "Power Surge" -SkillRequirements "Devices 25" -Description "Overcharges one ally for 15 seconds, granting +6% physical and Force ability Accuracy, +6% critical chance, and 1 STM every 5 seconds." -FP "-" -STM "3" -CastingTime "1.5 seconds" -CooldownTime "60 seconds" -Notes "Shared Devices active. Single-ally output support stays below Leadership's party command ceiling.")
    (New-DeviceTraitRow -Row 16 -Style "Universal" -Price "5" -PerkName "Integrated Toolkit III" -SkillRequirements "Devices 30" -Description "After using a Device ability, gain Integrated Toolkit for 12 seconds: +6% physical and Force ability Accuracy, +6% Defense, +6% Evasion, +8 Trauma Resistance rating, and 1 STM every 4 seconds. This can refresh but does not stack." -Notes "Replacement tier: adds modest sustained STM flow for regular Device users instead of rewarding gaps in Device usage.")
    (New-DeviceTraitRow -Row 17 -Style "Universal" -Price "5" -PerkName "Adaptive Circuits" -SkillRequirements "Devices 35" -Description "When a Device ability affects at least one enemy, gain +8% physical and Force ability Accuracy and +8% critical chance for 12 seconds. When a Device ability affects at least one ally, including yourself, gain +8% Defense and +8% Evasion for 12 seconds. Both effects can be active at once." -Notes "Rethought as an adaptive combat trait that provides useful value for offensive, defensive, and utility Device abilities.")
    (New-DeviceTraitRow -Row 18 -Style "Universal" -Price "5" -PerkName "Overclock Routine" -SkillRequirements "Devices 40" -Description "Device abilities gain +4% damage, healing, temporary HP, and damage absorption shield values." -Notes "Broad universal scaling is intentionally lower than tree-specific damage and shield traits.")
    (New-DeviceTraitRow -Row 19 -Style "Universal" -Price "5" -PerkName "Tactical Uplink" -SkillRequirements "Devices 45" -Description "Device abilities that affect allies also grant +4% physical and Force ability Accuracy for 12 seconds. Device abilities that affect enemies also reduce Evasion by 4% for 12 seconds." -Notes "Broad universal support is intentionally lower than dedicated Accuracy, critical chance, or Evasion debuffs.")
    (New-DeviceTraitRow -Row 20 -Style "Universal" -Price "5" -PerkName "Emergency Override" -SkillRequirements "Devices 50" -Description "When damage or resource spending leaves you below 35% HP or 35% STM, gain temporary HP equal to 20% of maximum HP plus PER scaling, restore 4 STM, and remove one standard negative effect. This can trigger once every 90 seconds." -Notes "Universal Devices capstone: emergency survival and recovery without requiring the player to hold a weak conditional Device use.")

    @{ Row = 24; PerkName = "Frag Grenade I"; Style = "Grenadier" }
    @{ Row = 25; PerkName = "Blast Radius I"; Style = "Grenadier" }
    @{ Row = 26; PerkName = "Concussion Grenade I"; Style = "Grenadier" }
    @{ Row = 27; PerkName = "Flash Grenade I"; Style = "Grenadier"; Description = "Attempts to inflict Flash, reducing physical and Force ability Accuracy by 8% for 20 seconds in a 4m blast. Consumes explosives." }
    @{ Row = 28; PerkName = "Frag Grenade II"; Style = "Grenadier" }
    @{ Row = 29; PerkName = "Ion Grenade I"; Style = "Grenadier" }
    @{ Row = 30; PerkName = "Blast Radius II"; Style = "Grenadier" }
    @{ Row = 31; PerkName = "Adhesive Grenade I"; Style = "Grenadier" }
    @{ Row = 32; PerkName = "Concussion Grenade II"; Style = "Grenadier" }
    @{ Row = 33; PerkName = "Cluster Grenade"; Style = "Grenadier"; Price = "4" }
    @{ Row = 34; PerkName = "Flash Grenade II"; Style = "Grenadier"; Description = "Attempts to inflict Flash, reducing physical and Force ability Accuracy by 14% for 20 seconds in a 4m blast. Consumes explosives." }
    @{ Row = 35; PerkName = "Ion Grenade II"; Style = "Grenadier" }
    @{ Row = 36; PerkName = "Frag Grenade III"; Style = "Grenadier" }
    @{ Row = 37; PerkName = "Adhesive Grenade II"; Style = "Grenadier"; Price = "4" }
    @{ Row = 38; PerkName = "Blast Radius III"; Style = "Grenadier"; Price = "4" }
    @{ Row = 39; PerkName = "Concussion Grenade III"; Style = "Grenadier" }
    @{ Row = 40; PerkName = "Thermal Detonator"; Style = "Grenadier"; Price = "5"; Description = "Deals 36 fire DMG plus PER scaling in a 5m blast and inflicts Burning for 30 seconds. Consumes explosives."; Notes = "Capstone blast is above Frag Grenade III's area utility only through radius and Burning, not raw immediate damage." }

    @{ Row = 44; PerkName = "Blaster Beacon I"; Style = "Field Engineer"; Description = "Plants a targeting beacon for 18 seconds. Every 3 seconds, one hostile target within 12m is hit for 6 physical DMG plus PER scaling."; Notes = "Pulse damage is balanced by total duration so beacons do not exceed dedicated direct-damage actives over a full uptime." }
    @{ Row = 45; PerkName = "Beacon Targeting I"; Style = "Field Engineer" }
    @{ Row = 46; PerkName = "Incendiary Field I"; Style = "Field Engineer"; Description = "Deploys a visible 5m fire field for 12 seconds. Every 3 seconds, enemies inside take 6 fire DMG plus PER scaling."; Notes = "Persistent area damage uses lower per-pulse values because enemies can take multiple pulses." }
    @{ Row = 47; PerkName = "Remote Charge I"; Style = "Field Engineer" }
    @{ Row = 48; PerkName = "Blaster Beacon II"; Style = "Field Engineer"; Description = "Plants a targeting beacon for 21 seconds. Every 3 seconds, one hostile target within 12m is hit for 8 physical DMG plus PER scaling."; Notes = "Replacement tier: longer duration and higher pulse damage without overtaking single-hit weapon and grenade values." }
    @{ Row = 49; PerkName = "Pulse Relay I"; Style = "Field Engineer" }
    @{ Row = 50; PerkName = "Beacon Targeting II"; Style = "Field Engineer" }
    @{ Row = 51; PerkName = "Shock Beacon I"; Style = "Field Engineer"; Description = "Plants a shock beacon for 15 seconds. Every 3 seconds, one hostile target within 10m is hit for 6 electrical DMG plus PER scaling and suffers Shock for 6 seconds."; Notes = "Shock rider keeps the pulse damage below Blaster Beacon at the same investment band." }
    @{ Row = 52; PerkName = "Incendiary Field II"; Style = "Field Engineer"; Description = "Deploys a visible 5m fire field for 15 seconds. Every 3 seconds, enemies inside take 8 fire DMG plus PER scaling."; Notes = "Replacement tier: longer field duration and higher pulse damage." }
    @{ Row = 53; PerkName = "Remote Charge II"; Style = "Field Engineer"; Price = "4" }
    @{ Row = 54; PerkName = "Blaster Beacon III"; Style = "Field Engineer"; Description = "Plants a targeting beacon for 24 seconds. Every 3 seconds, one hostile target within 14m is hit for 10 physical DMG plus PER scaling."; Notes = "Replacement tier: final beacon pulse remains lower than capstone output because Beacon Targeting adds Accuracy, critical chance, and damage." }
    @{ Row = 55; PerkName = "Pulse Relay II"; Style = "Field Engineer" }
    @{ Row = 56; PerkName = "Shock Beacon II"; Style = "Field Engineer"; Price = "4"; Description = "Plants a shock beacon for 18 seconds. Every 3 seconds, one hostile target within 12m is hit for 8 electrical DMG plus PER scaling and suffers Shock for 6 seconds."; Notes = "Replacement tier: longer duration and higher pulse damage while accounting for the Shock rider." }
    @{ Row = 57; PerkName = "Incendiary Field III"; Style = "Field Engineer"; Price = "4"; Description = "Deploys a visible 5m fire field for 18 seconds. Every 3 seconds, enemies inside take 10 fire DMG plus PER scaling."; Notes = "Replacement tier: final field total damage is tuned below large grenade bursts if enemies do not stay inside for the full duration." }
    @{ Row = 58; PerkName = "Beacon Targeting III"; Style = "Field Engineer" }
    @{ Row = 59; PerkName = "Remote Charge III"; Style = "Field Engineer" }
    @{ Row = 60; PerkName = "Killzone Beacon"; Style = "Field Engineer"; Price = "5"; Description = "Plants a killzone beacon for 24 seconds. Every 3 seconds, it triggers one 12 physical DMG pulse and one 8 electrical DMG pulse against hostile targets within 12m; the electrical pulse inflicts Shock for 24 seconds."; Notes = "Capstone beacon has strong sustained pressure, but duration and pulse values are capped so full uptime does not dwarf other Devices capstones." }

    @{ Row = 64; PerkName = "Deflector Shield I"; Style = "Field Support" }
    @{ Row = 65; PerkName = "Capacitor Rig I"; Style = "Field Support"; Description = "After you use two Field Support combat abilities within 20 seconds, restore 5% maximum STM to yourself and one ally within 10m. This can trigger once every 20 seconds."; Notes = "Replaces direct shielding-perk amplification with a self-contained Field Support resource-flow line." }
    @{ Row = 66; PerkName = "Weapon Jam I"; Style = "Field Support" }
    @{ Row = 67; PerkName = "Power Cell I"; Style = "Field Support" }
    @{ Row = 68; PerkName = "Deflector Shield II"; Style = "Field Support" }
    @{ Row = 69; PerkName = "Rayshield Screen I"; Style = "Field Support" }
    @{ Row = 70; PerkName = "Capacitor Rig II"; Style = "Field Support"; Description = "After you use two Field Support combat abilities within 20 seconds, restore 8% maximum STM to yourself and up to two allies within 10m. This can trigger once every 20 seconds."; Notes = "Replacement tier: expands the Capacitor Rig resource payoff without increasing another perk line's shield values." }
    @{ Row = 71; PerkName = "Dampening Field I"; Style = "Field Support" }
    @{ Row = 72; PerkName = "Weapon Jam II"; Style = "Field Support" }
    @{ Row = 73; PerkName = "Power Cell II"; Style = "Field Support" }
    @{ Row = 74; PerkName = "Deflector Shield III"; Style = "Field Support" }
    @{ Row = 75; PerkName = "Rayshield Screen II"; Style = "Field Support" }
    @{ Row = 76; PerkName = "Dampening Field II"; Style = "Field Support"; Price = "4" }
    @{ Row = 77; PerkName = "Group Deflector"; Style = "Field Support"; Price = "4" }
    @{ Row = 78; PerkName = "Capacitor Rig III"; Style = "Field Support"; Price = "4"; Description = "After you use two Field Support combat abilities within 20 seconds, restore 10% maximum STM to yourself and allies within 10m, and grant affected allies +5% Defense for 10 seconds. This can trigger once every 20 seconds."; Notes = "Replacement tier: turns Capacitor Rig into a Field Support cadence reward instead of a multiplier on Deflector Shield, Group Deflector, or Emergency Bunker." }
    @{ Row = 79; PerkName = "Power Cell III"; Style = "Field Support" }
    @{ Row = 80; PerkName = "Emergency Bunker"; Style = "Field Support"; Price = "5" }

    @{ Row = 84; PerkName = "Flamethrower I"; Style = "Assault Gadgets" }
    @{ Row = 85; PerkName = "Wrist Rocket I"; Style = "Assault Gadgets" }
    @{ Row = 86; PerkName = "Sonic Burst I"; Style = "Assault Gadgets" }
    @{ Row = 87; PerkName = "Gadget Harness I"; Style = "Assault Gadgets" }
    @{ Row = 88; PerkName = "Flamethrower II"; Style = "Assault Gadgets" }
    @{ Row = 89; PerkName = "Rail Dart I"; Style = "Assault Gadgets" }
    @{ Row = 90; PerkName = "Gadget Harness II"; Style = "Assault Gadgets" }
    @{ Row = 91; PerkName = "Wrist Rocket II"; Style = "Assault Gadgets" }
    @{ Row = 92; PerkName = "Sonic Burst II"; Style = "Assault Gadgets" }
    @{ Row = 93; PerkName = "Cryo Sprayer I"; Style = "Assault Gadgets" }
    @{ Row = 94; PerkName = "Flamethrower III"; Style = "Assault Gadgets" }
    @{ Row = 95; PerkName = "Rail Dart II"; Style = "Assault Gadgets" }
    @{ Row = 96; PerkName = "Wrist Rocket III"; Style = "Assault Gadgets"; Price = "4" }
    @{ Row = 97; PerkName = "Sonic Burst III"; Style = "Assault Gadgets"; Price = "4" }
    @{ Row = 98; PerkName = "Gadget Harness III"; Style = "Assault Gadgets"; Price = "4" }
    @{ Row = 99; PerkName = "Cryo Sprayer II"; Style = "Assault Gadgets" }
    @{ Row = 100; PerkName = "Overload Barrage"; Style = "Assault Gadgets"; Price = "5" }
)

$deviceDataRows = 9..102
$deviceTotalRows = @(
    @{ Row = 21; Formula = "SUM(B9:B20)"; Value = "55" }
    @{ Row = 41; Formula = "SUM(B24:B40)"; Value = "55" }
    @{ Row = 61; Formula = "SUM(B44:B60)"; Value = "55" }
    @{ Row = 81; Formula = "SUM(B64:B80)"; Value = "55" }
    @{ Row = 101; Formula = "SUM(B84:B100)"; Value = "55" }
)

$workbookPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookPath)) {
    throw "Workbook '$workbookPath' was not found."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($workbookPath, [System.IO.Compression.ZipArchiveMode]::Update)

try {
    $sharedStrings = New-Object System.Collections.Generic.List[string]
    $sharedStringsEntry = $zip.GetEntry("xl/sharedStrings.xml")
    if ($null -ne $sharedStringsEntry) {
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

    $workbookNamespace = [System.Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
    $workbookNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")
    $workbookNamespace.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")

    $deviceSheetPath = $null
    foreach ($sheet in $workbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
        if ($sheet.GetAttribute("name") -ne "Devices") {
            continue
        }

        $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $deviceSheetPath = $relationshipsById[$relationshipId]
        break
    }

    if ([string]::IsNullOrWhiteSpace($deviceSheetPath)) {
        throw "Workbook sheet 'Devices' was not found."
    }

    [xml]$worksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $deviceSheetPath
    $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($worksheetXml.NameTable)
    $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    $sheetData = $worksheetXml.SelectSingleNode("//d:sheetData", $worksheetNamespace)
    if ($null -eq $sheetData) {
        throw "Workbook sheet 'Devices' has no sheetData node."
    }

    $headerRowNumber = 0
    $columnByHeader = @{}
    $rowByNumber = @{}

    foreach ($rowNode in $worksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText)) {
            continue
        }

        $rowNumber = [int]$rowNumberText
        $rowByNumber[$rowNumber] = $rowNode

        $cells = @{}
        foreach ($cell in $rowNode.SelectNodes("d:c", $worksheetNamespace)) {
            $columnIndex = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
            if ($columnIndex -gt 0) {
                $cells[$columnIndex] = Get-OpenXmlCellText -Cell $cell -SharedStrings $sharedStrings
            }
        }

        if ($headerRowNumber -eq 0 -and (($cells.Values -join "|") -match "Perk Name|PerkName")) {
            $headerRowNumber = $rowNumber
            foreach ($cellEntry in $cells.GetEnumerator()) {
                $canonicalHeader = Get-CanonicalManifestHeader $cellEntry.Value
                if (![string]::IsNullOrWhiteSpace($canonicalHeader) -and !$columnByHeader.ContainsKey($canonicalHeader)) {
                    $columnByHeader[$canonicalHeader] = $cellEntry.Key
                }
            }
        }
    }

    if ($headerRowNumber -eq 0) {
        throw "Workbook sheet 'Devices' header row was not found."
    }

    foreach ($requiredHeader in @("Style", "Price", "PerkName", "SkillRequirements", "CharacterType", "Type", "Description", "PrimaryStat", "SecondaryStat", "ScalingSource", "FP", "STM", "CastingTime", "CooldownTime", "DevStatus", "AdditionalRequirements", "Notes")) {
        if (!$columnByHeader.ContainsKey($requiredHeader)) {
            throw "Workbook sheet 'Devices' is missing required column '$requiredHeader'."
        }
    }

    $rowValuesByPerkName = @{}
    foreach ($rowNode in $worksheetXml.SelectNodes("//d:sheetData/d:row", $worksheetNamespace)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText) -or [int]$rowNumberText -le $headerRowNumber) {
            continue
        }

        $values = @{}
        foreach ($headerEntry in $columnByHeader.GetEnumerator()) {
            $values[$headerEntry.Key] = ""
        }

        foreach ($cell in $rowNode.SelectNodes("d:c", $worksheetNamespace)) {
            $columnIndex = Get-OpenXmlColumnIndex $cell.GetAttribute("r")
            $header = $columnByHeader.GetEnumerator() |
                Where-Object { $_.Value -eq $columnIndex } |
                Select-Object -First 1

            if ($null -ne $header) {
                $values[$header.Key] = Get-OpenXmlCellText -Cell $cell -SharedStrings $sharedStrings
            }
        }

        $perkName = $values["PerkName"]
        if (![string]::IsNullOrWhiteSpace($perkName)) {
            $rowValuesByPerkName[$perkName] = $values
        }
    }

    foreach ($rowNumber in $deviceDataRows) {
        $rowNode = Get-OrCreateRow -WorksheetXml $worksheetXml -SheetData $sheetData -RowNumber $rowNumber
        $blankValues = @{}
        foreach ($header in $columnByHeader.Keys) {
            $blankValues[$header] = ""
        }

        Update-RowValues -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnByHeader $columnByHeader -Values $blankValues
    }

    $updatedRows = New-Object System.Collections.Generic.List[object]
    foreach ($deviceRow in $deviceRows) {
        $perkName = $deviceRow.PerkName
        $values = @{}
        if ($rowValuesByPerkName.ContainsKey($perkName)) {
            foreach ($entry in $rowValuesByPerkName[$perkName].GetEnumerator()) {
                $values[$entry.Key] = $entry.Value
            }
        }

        $values["PerkName"] = $perkName
        foreach ($entry in $deviceRow.GetEnumerator()) {
            if ($entry.Key -in @("Row", "PerkName")) {
                continue
            }

            $values[$entry.Key] = $entry.Value
        }

        $rowNode = Get-OrCreateRow -WorksheetXml $worksheetXml -SheetData $sheetData -RowNumber ([int]$deviceRow.Row)
        Update-RowValues -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnByHeader $columnByHeader -Values $values
        $updatedRows.Add([pscustomobject]@{ Row = [int]$deviceRow.Row; Style = $values["Style"]; PerkName = $perkName }) | Out-Null
    }

    foreach ($totalRow in $deviceTotalRows) {
        $rowNode = Get-OrCreateRow -WorksheetXml $worksheetXml -SheetData $sheetData -RowNumber ([int]$totalRow.Row)
        Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["Style"] -Text "Total"
        Set-CellFormula -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["Price"] -Formula $totalRow.Formula -CachedValue $totalRow.Value
        Set-CellStyle -RowNode $rowNode -ColumnIndex $columnByHeader["Style"] -StyleIndex "2"
        Set-CellStyle -RowNode $rowNode -ColumnIndex $columnByHeader["Price"] -StyleIndex "2"
        $updatedRows.Add([pscustomobject]@{ Row = [int]$totalRow.Row; Style = "Total"; PerkName = "" }) | Out-Null
    }

    $topRow = Get-OrCreateRow -WorksheetXml $worksheetXml -SheetData $sheetData -RowNumber 4
    Set-CellFormula -WorksheetXml $worksheetXml -RowNode $topRow -ColumnIndex 4 -Formula "SUM(B21,B41,B61,B81,B101)" -CachedValue "275"

    $dimensionNode = $worksheetXml.SelectSingleNode("//d:dimension", $worksheetNamespace)
    if ($null -ne $dimensionNode) {
        $maxColumn = ($columnByHeader.Values | Measure-Object -Maximum).Maximum
        $maxRow = [Math]::Max(102, (($rowByNumber.Keys + @(102)) | Measure-Object -Maximum).Maximum)
        $dimensionNode.SetAttribute("ref", "A1:$(ConvertTo-OpenXmlColumnName $maxColumn)$maxRow")
    }

    Write-ZipEntryXml -Zip $zip -EntryPath $deviceSheetPath -Xml $worksheetXml
}
finally {
    $zip.Dispose()
}

$updatedRows |
    Sort-Object Row |
    Format-Table Row, PerkName -AutoSize

Write-Host "Updated Devices option parity rows in '$workbookPath'."
