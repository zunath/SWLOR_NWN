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

function Get-RowNode {
    param(
        [xml]$WorksheetXml,
        [int]$RowNumber
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($WorksheetXml.NameTable)
    $namespaceManager.AddNamespace("d", $namespace)
    $rowNode = $WorksheetXml.SelectSingleNode("//d:sheetData/d:row[@r='$RowNumber']", $namespaceManager)
    if ($null -ne $rowNode) {
        return $rowNode
    }

    $sheetData = $WorksheetXml.SelectSingleNode("//d:sheetData", $namespaceManager)
    $rowNode = $WorksheetXml.CreateElement("row", $namespace)
    $rowNode.SetAttribute("r", $RowNumber.ToString())
    [void]$sheetData.AppendChild($rowNode)
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

    $valueNodes = $Cell.GetElementsByTagName("v", $namespace)
    if ($valueNodes.Count -gt 0) {
        return $valueNodes[0].InnerText
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
        [System.Xml.XmlElement]$RowNode,
        [string]$CellReference,
        [string]$Value,
        [string]$Style = ""
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $cell = Get-Cell -WorksheetXml $WorksheetXml -RowNode $RowNode -CellReference $CellReference
    Clear-Cell -Cell $cell
    if (![string]::IsNullOrWhiteSpace($Style)) {
        $cell.SetAttribute("s", $Style)
    }

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    $cell.SetAttribute("t", "inlineStr")
    $inlineString = $WorksheetXml.CreateElement("is", $namespace)
    $textElement = $WorksheetXml.CreateElement("t", $namespace)
    $textElement.InnerText = $Value
    [void]$inlineString.AppendChild($textElement)
    [void]$cell.AppendChild($inlineString)
}

function Set-NumberCell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [string]$CellReference,
        [string]$Value,
        [string]$Style = ""
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $cell = Get-Cell -WorksheetXml $WorksheetXml -RowNode $RowNode -CellReference $CellReference
    Clear-Cell -Cell $cell
    if (![string]::IsNullOrWhiteSpace($Style)) {
        $cell.SetAttribute("s", $Style)
    }

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -eq "-") {
        Set-TextCell -WorksheetXml $WorksheetXml -RowNode $RowNode -CellReference $CellReference -Value $Value -Style $Style
        return
    }

    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $Value
    [void]$cell.AppendChild($valueElement)
}

function Set-FormulaCell {
    param(
        [xml]$WorksheetXml,
        [System.Xml.XmlElement]$RowNode,
        [string]$CellReference,
        [string]$Formula,
        [string]$CachedValue,
        [string]$Style = ""
    )

    $namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $cell = Get-Cell -WorksheetXml $WorksheetXml -RowNode $RowNode -CellReference $CellReference
    Clear-Cell -Cell $cell
    if (![string]::IsNullOrWhiteSpace($Style)) {
        $cell.SetAttribute("s", $Style)
    }

    $formulaElement = $WorksheetXml.CreateElement("f", $namespace)
    $formulaElement.InnerText = $Formula
    [void]$cell.AppendChild($formulaElement)

    $valueElement = $WorksheetXml.CreateElement("v", $namespace)
    $valueElement.InnerText = $CachedValue
    [void]$cell.AppendChild($valueElement)
}

function Copy-DeviceRow {
    param([object]$Source)

    return [ordered]@{
        Style = $Source.Style
        Price = $Source.Price
        PerkName = $Source.PerkName
        SkillRequirements = $Source.SkillRequirements
        CharacterType = $Source.CharacterType
        Type = $Source.Type
        Description = $Source.Description
        PrimaryStat = $Source.PrimaryStat
        SecondaryStat = $Source.SecondaryStat
        ScalingSource = $Source.ScalingSource
        FP = $Source.FP
        STM = $Source.STM
        CastingTime = $Source.CastingTime
        CooldownTime = $Source.CooldownTime
        DevStatus = $Source.DevStatus
        AdditionalRequirements = $Source.AdditionalRequirements
        Notes = $Source.Notes
    }
}

function New-DeviceTrait {
    param(
        [string]$Style,
        [string]$Price,
        [string]$PerkName,
        [string]$SkillRequirements,
        [string]$Description,
        [string]$Notes,
        [string]$PrimaryStat = "None",
        [string]$ScalingSource = "Design Added"
    )

    return [ordered]@{
        Style = $Style
        Price = $Price
        PerkName = $PerkName
        SkillRequirements = $SkillRequirements
        CharacterType = "Standard"
        Type = "Trait"
        Description = $Description
        PrimaryStat = $PrimaryStat
        SecondaryStat = "None"
        ScalingSource = $ScalingSource
        FP = "-"
        STM = "-"
        CastingTime = "-"
        CooldownTime = "-"
        DevStatus = "Design Added"
        AdditionalRequirements = ""
        Notes = $Notes
    }
}

function New-DeviceCombat {
    param(
        [string]$Style,
        [string]$Price,
        [string]$PerkName,
        [string]$SkillRequirements,
        [string]$Description,
        [string]$STM,
        [string]$CastingTime,
        [string]$CooldownTime,
        [string]$Notes,
        [string]$PrimaryStat = "PER",
        [string]$ScalingSource = "Combat Formula"
    )

    return [ordered]@{
        Style = $Style
        Price = $Price
        PerkName = $PerkName
        SkillRequirements = $SkillRequirements
        CharacterType = "Standard"
        Type = "Combat"
        Description = $Description
        PrimaryStat = $PrimaryStat
        SecondaryStat = "None"
        ScalingSource = $ScalingSource
        FP = "-"
        STM = $STM
        CastingTime = $CastingTime
        CooldownTime = $CooldownTime
        DevStatus = "Design Added"
        AdditionalRequirements = ""
        Notes = $Notes
    }
}

$workbookPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookPath)) {
    throw "Workbook '$workbookPath' was not found."
}

$descriptionOverrides = @{
    "Thermal Detonator" = "Deals 60 fire DMG plus PER scaling in a 5m blast and inflicts Burning for 45 seconds. Consumes explosives."
    "Blaster Beacon I" = "Plants a targeting beacon for 18 seconds. Every 3 seconds, one hostile target within 12m is hit by an automated ranged energy pulse for 10 energy DMG plus PER scaling."
    "Blaster Beacon II" = "Plants a targeting beacon for 21 seconds. Every 3 seconds, one hostile target within 12m is hit by an automated ranged energy pulse for 14 energy DMG plus PER scaling."
    "Blaster Beacon III" = "Plants a targeting beacon for 24 seconds. Every 3 seconds, one hostile target within 14m is hit by an automated ranged energy pulse for 18 energy DMG plus PER scaling."
    "Incendiary Field I" = "Deploys a visible fire field for 12 seconds. Enemies inside take 8 fire DMG plus PER scaling every 3 seconds."
    "Incendiary Field II" = "Deploys a visible fire field for 15 seconds. Enemies inside take 12 fire DMG plus PER scaling every 3 seconds."
    "Incendiary Field III" = "Deploys a visible fire field for 18 seconds. Enemies inside take 16 fire DMG plus PER scaling every 3 seconds."
    "Remote Charge I" = "Arms a visible charge at your target location that detonates after 3 seconds for 30 fire DMG plus PER scaling."
    "Remote Charge II" = "Arms a visible charge that detonates after 3 seconds for 42 fire DMG plus PER scaling and knock down."
    "Shock Beacon I" = "Plants a shock beacon for 15 seconds. Every 3 seconds, one hostile target within 10m is hit for 10 electrical DMG plus PER scaling and suffers Shock."
    "Shock Beacon II" = "Plants a shock beacon for 18 seconds. Every 3 seconds, one hostile target within 12m is hit for 14 electrical DMG plus PER scaling and suffers Shock."
    "Killzone Beacon" = "Plants a killzone beacon for 45 seconds. Every 3 seconds, it triggers one 16 physical DMG plus PER scaling pulse and one 16 electrical DMG plus PER scaling shock pulse against hostile targets within 12m."
    "Flamethrower I" = "Deals 16 fire DMG plus PER scaling to hostile targets in a cone."
    "Flamethrower II" = "Deals 28 fire DMG plus PER scaling to hostile targets in a cone and attempts to inflict Burning."
    "Flamethrower III" = "Deals 42 fire DMG plus PER scaling to hostile targets in a cone and attempts to inflict Burning."
    "Wrist Rocket I" = "Deals 20 fire DMG plus PER scaling to one target."
    "Wrist Rocket II" = "Deals 34 fire DMG plus PER scaling to one target and knock down for 2 seconds."
    "Wrist Rocket III" = "Deals 48 fire DMG plus PER scaling to one target and knock down for 3 seconds."
    "Rail Dart I" = "Fires a dart that deals 18 physical DMG plus PER scaling and attempts to inflict Bleed."
    "Rail Dart II" = "Fires a dart that deals 34 physical DMG plus PER scaling and attempts to inflict Bleed."
    "Cryo Sprayer" = "Deals 22 ice DMG plus PER scaling to hostile targets in a cone and slows movement for 5 seconds."
    "Overload Barrage" = "Unleashes three attacks at your primary target's location: a 42 fire DMG burst plus Burning for 45 seconds, a 48 fire DMG single-target hit plus brief Knockdown, and a 24 sonic DMG burst that interrupts activation and reduces Accuracy by 10% for 45 seconds."
}

$notesOverrides = @{
    "Gadget Harness" = "Consolidates the old multi-rank harness into one early assault trait."
}

$removedPerks = @(
    "Flash Grenade II",
    "Concussion Grenade III",
    "Pulse Relay I",
    "Pulse Relay II",
    "Beacon Targeting III",
    "Remote Charge III",
    "Capacitor Rig I",
    "Capacitor Rig II",
    "Capacitor Rig III",
    "Weapon Jam II",
    "Gadget Harness II",
    "Gadget Harness III",
    "Cryo Sprayer II"
)

$layouts = @(
    @{
        Style = "Grenadier"
        StartRow = 9
        TotalRow = 25
        TotalFormula = "SUM(B9:B24)"
        Rows = @(
            @{ Source = "Frag Grenade I"; Price = "2"; SkillRequirements = "-" },
            @{ Source = "Blast Radius I"; Price = "2"; SkillRequirements = "Devices 5" },
            @{ Source = "Concussion Grenade I"; Price = "3"; SkillRequirements = "Devices 8" },
            @{ Source = "Flash Grenade"; Price = "3"; SkillRequirements = "Devices 12" },
            @{ Source = "Frag Grenade II"; Price = "3"; SkillRequirements = "Devices 15" },
            @{ Source = "Ion Grenade I"; Price = "3"; SkillRequirements = "Devices 18" },
            @{ Source = "Blast Radius II"; Price = "3"; SkillRequirements = "Devices 22" },
            @{ Source = "Adhesive Grenade I"; Price = "4"; SkillRequirements = "Devices 25" },
            @{ Source = "Concussion Grenade II"; Price = "3"; SkillRequirements = "Devices 28" },
            @{ Source = "Cluster Grenade"; Price = "4"; SkillRequirements = "Devices 30" },
            @{
                Row = New-DeviceCombat -Style "Grenadier" -Price "4" -PerkName "Disruption Pulse" -SkillRequirements "Devices 35" -Description "Emits a 5m disruption pulse at a target point within 12m, dealing 18 electrical DMG plus PER scaling to enemies and reducing physical and Force ability Accuracy by 6% for 12 seconds. Consumes explosives." -STM "4" -CastingTime "1.5 seconds" -CooldownTime "45 seconds" -Notes "Grenadier control explosive carried over from the shared Devices set; replaces redundant late Flash and Concussion ranks with an area disruption tool."
            },
            @{ Source = "Ion Grenade II"; Price = "3"; SkillRequirements = "Devices 38" },
            @{ Source = "Frag Grenade III"; Price = "5"; SkillRequirements = "Devices 40" },
            @{ Source = "Adhesive Grenade II"; Price = "4"; SkillRequirements = "Devices 42" },
            @{ Source = "Blast Radius III"; Price = "4"; SkillRequirements = "Devices 45" },
            @{ Source = "Thermal Detonator"; Price = "5"; SkillRequirements = "Devices 50" }
        )
    },
    @{
        Style = "Field Engineer"
        StartRow = 28
        TotalRow = 43
        TotalFormula = "SUM(B28:B42)"
        Rows = @(
            @{ Source = "Blaster Beacon I"; Price = "2"; SkillRequirements = "-" },
            @{ Source = "Beacon Targeting I"; Price = "3"; SkillRequirements = "Devices 5" },
            @{ Source = "Incendiary Field I"; Price = "3"; SkillRequirements = "Devices 8" },
            @{ Source = "Remote Charge I"; Price = "3"; SkillRequirements = "Devices 12" },
            @{ Source = "Blaster Beacon II"; Price = "3"; SkillRequirements = "Devices 15" },
            @{
                Row = New-DeviceCombat -Style "Field Engineer" -Price "4" -PerkName "Signal Jammer" -SkillRequirements "Devices 18" -Description "Deploys a signal jammer for 12 seconds. Hostile targets within 5m suffer -6% physical and Force ability Accuracy and cannot benefit from Haste while inside." -STM "4" -CastingTime "1.5 seconds" -CooldownTime "45 seconds" -Notes "Field Engineer control device carried over from the shared Devices set."
            },
            @{ Source = "Shock Beacon I"; Price = "4"; SkillRequirements = "Devices 22" },
            @{ Source = "Incendiary Field II"; Price = "3"; SkillRequirements = "Devices 25" },
            @{ Source = "Remote Charge II"; Price = "4"; SkillRequirements = "Devices 28" },
            @{ Source = "Blaster Beacon III"; Price = "4"; SkillRequirements = "Devices 30" },
            @{
                Row = New-DeviceTrait -Style "Field Engineer" -Price "4" -PerkName "Diagnostic Sweep" -SkillRequirements "Devices 35" -Description "Field Engineer beacons, fields, charges, and jammers reveal hidden enemies in their affected area and reduce Evasion by 4% for 10 seconds." -Notes "Placed in Field Engineer because the effect is sensor and area-control utility rather than direct support or personal assault."
            },
            @{ Source = "Shock Beacon II"; Price = "4"; SkillRequirements = "Devices 38" },
            @{ Source = "Beacon Targeting II"; Price = "4"; SkillRequirements = "Devices 42"; Description = "Beacon pulses gain +12% Accuracy, +12% critical chance, +8% damage, and +2m pulse range."; Notes = "Consolidates the old multi-rank beacon targeting support into one late Field Engineer trait." },
            @{ Source = "Incendiary Field III"; Price = "5"; SkillRequirements = "Devices 45" },
            @{ Source = "Killzone Beacon"; Price = "5"; SkillRequirements = "Devices 50" }
        )
    },
    @{
        Style = "Field Support"
        StartRow = 47
        TotalRow = 62
        TotalFormula = "SUM(B47:B61)"
        Rows = @(
            @{ Source = "Deflector Shield I"; Price = "2"; SkillRequirements = "-" },
            @{
                Row = New-DeviceTrait -Style "Field Support" -Price "3" -PerkName "Power Surge" -SkillRequirements "Devices 5" -Description "Power Cell's initial target also gains Power Surge for 12 seconds: +6% physical and Force ability Accuracy, +6% critical chance, and 1 STM every 4 seconds." -Notes "Carries Power Surge into Field Support as a Power Cell payoff instead of another active button."
            },
            @{ Source = "Weapon Jam"; Price = "3"; SkillRequirements = "Devices 8" },
            @{ Source = "Power Cell I"; Price = "3"; SkillRequirements = "Devices 12" },
            @{ Source = "Deflector Shield II"; Price = "3"; SkillRequirements = "Devices 15" },
            @{ Source = "Rayshield Screen I"; Price = "3"; SkillRequirements = "Devices 18" },
            @{ Source = "Dampening Field I"; Price = "4"; SkillRequirements = "Devices 22" },
            @{ Source = "Power Cell II"; Price = "4"; SkillRequirements = "Devices 25" },
            @{ Source = "Deflector Shield III"; Price = "4"; SkillRequirements = "Devices 30" },
            @{
                Row = New-DeviceTrait -Style "Field Support" -Price "4" -PerkName "Overclock Routine" -SkillRequirements "Devices 35" -Description "Field Support abilities that affect allies also grant Overclock Routine for 12 seconds. Affected allies gain +4% Device ability damage, healing, temporary HP, and damage absorption shield values." -Notes "Buffs other Gadgeteers through Field Support play without adding an active button."
            },
            @{ Source = "Rayshield Screen II"; Price = "4"; SkillRequirements = "Devices 38" },
            @{ Source = "Dampening Field II"; Price = "4"; SkillRequirements = "Devices 40" },
            @{ Source = "Group Deflector"; Price = "4"; SkillRequirements = "Devices 42" },
            @{ Source = "Power Cell III"; Price = "5"; SkillRequirements = "Devices 48" },
            @{ Source = "Emergency Bunker"; Price = "5"; SkillRequirements = "Devices 50" }
        )
    },
    @{
        Style = "Assault Gadgets"
        StartRow = 66
        TotalRow = 81
        TotalFormula = "SUM(B66:B80)"
        Rows = @(
            @{ Source = "Flamethrower I"; Price = "2"; SkillRequirements = "-" },
            @{ Source = "Wrist Rocket I"; Price = "2"; SkillRequirements = "Devices 5" },
            @{ Source = "Sonic Burst I"; Price = "3"; SkillRequirements = "Devices 8" },
            @{ Source = "Gadget Harness"; Price = "4"; SkillRequirements = "Devices 12"; Description = "Assault Gadget abilities gain +8% Accuracy and +8% critical chance." },
            @{ Source = "Flamethrower II"; Price = "3"; SkillRequirements = "Devices 15" },
            @{ Source = "Rail Dart I"; Price = "3"; SkillRequirements = "Devices 18" },
            @{
                Row = New-DeviceTrait -Style "Assault Gadgets" -Price "4" -PerkName "Tactical Uplink" -SkillRequirements "Devices 22" -Description "After an Assault Gadget ability damages an enemy, you and nearby allies gain Tactical Uplink for 10 seconds: +5% Device ability Accuracy and +5% Device critical chance." -Notes "Gadget support trait carried over from the shared Devices set; helps gadget-focused groups without adding another active attack."
            },
            @{ Source = "Wrist Rocket II"; Price = "4"; SkillRequirements = "Devices 25" },
            @{ Source = "Sonic Burst II"; Price = "3"; SkillRequirements = "Devices 28" },
            @{ Source = "Cryo Sprayer"; Price = "4"; SkillRequirements = "Devices 30" },
            @{ Source = "Flamethrower III"; Price = "4"; SkillRequirements = "Devices 35" },
            @{ Source = "Rail Dart II"; Price = "4"; SkillRequirements = "Devices 38" },
            @{ Source = "Wrist Rocket III"; Price = "5"; SkillRequirements = "Devices 40" },
            @{ Source = "Sonic Burst III"; Price = "5"; SkillRequirements = "Devices 42" },
            @{ Source = "Overload Barrage"; Price = "5"; SkillRequirements = "Devices 50" }
        )
    }
)

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

    $devicesSheetPath = Get-WorksheetPath -WorkbookXml $workbookXml -RelationshipsById $relationshipsById -SheetName "Devices"
    [xml]$devicesWorksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $devicesSheetPath

    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($devicesWorksheetXml.NameTable)
    $namespaceManager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    $columns = [ordered]@{
        Style = "A"
        Price = "B"
        PerkName = "C"
        SkillRequirements = "D"
        CharacterType = "E"
        Type = "F"
        Description = "G"
        PrimaryStat = "H"
        SecondaryStat = "I"
        ScalingSource = "J"
        FP = "K"
        STM = "L"
        CastingTime = "M"
        CooldownTime = "N"
        DevStatus = "O"
        AdditionalRequirements = "P"
        Notes = "Q"
    }

    $sourceRowsByName = @{}
    foreach ($rowNode in $devicesWorksheetXml.SelectNodes("//d:sheetData/d:row", $namespaceManager)) {
        $rowNumberText = $rowNode.GetAttribute("r")
        if ([string]::IsNullOrWhiteSpace($rowNumberText)) {
            continue
        }

        $rowNumber = [int]$rowNumberText
        if ($rowNumber -le 8) {
            continue
        }

        $source = [ordered]@{ Row = $rowNumber }
        foreach ($entry in $columns.GetEnumerator()) {
            $cellRef = "$($entry.Value)$rowNumber"
            $cell = $devicesWorksheetXml.SelectSingleNode("//d:c[@r='$cellRef']", $namespaceManager)
            $source[$entry.Key] = Get-CellText -Cell $cell -SharedStrings $sharedStrings
        }

        if (![string]::IsNullOrWhiteSpace($source.PerkName) -and $source.PerkName -ne "Total") {
            $sourceRowsByName[$source.PerkName] = [pscustomobject]$source
        }
    }

    foreach ($layout in $layouts) {
        foreach ($entry in $layout.Rows) {
            if ($entry.ContainsKey("Source") -and !$sourceRowsByName.ContainsKey($entry.Source)) {
                throw "Expected Devices perk '$($entry.Source)' was not found in the workbook."
            }
        }
    }

    foreach ($rowNumber in 9..83) {
        $rowNode = Get-RowNode -WorksheetXml $devicesWorksheetXml -RowNumber $rowNumber
        foreach ($columnName in $columns.Values) {
            $cell = Get-Cell -WorksheetXml $devicesWorksheetXml -RowNode $rowNode -CellReference "$columnName$rowNumber"
            Clear-Cell -Cell $cell
        }

        foreach ($columnName in @("R", "S")) {
            $cell = Get-Cell -WorksheetXml $devicesWorksheetXml -RowNode $rowNode -CellReference "$columnName$rowNumber"
            Clear-Cell -Cell $cell
        }
    }

    $writtenRows = @()
    foreach ($layout in $layouts) {
        $rowNumber = [int]$layout.StartRow
        foreach ($entry in $layout.Rows) {
            if ($entry.ContainsKey("Row")) {
                $row = [ordered]@{}
                foreach ($property in $entry.Row.GetEnumerator()) {
                    $row[$property.Key] = $property.Value
                }
            }
            else {
                $row = Copy-DeviceRow -Source $sourceRowsByName[$entry.Source]
                $row.Style = $layout.Style
                $row.Price = $entry.Price
                $row.SkillRequirements = $entry.SkillRequirements
                $row.CharacterType = "Standard"
                $row.SecondaryStat = "None"
                if ($descriptionOverrides.ContainsKey($row.PerkName)) {
                    $row.Description = $descriptionOverrides[$row.PerkName]
                }

                if ($notesOverrides.ContainsKey($row.PerkName)) {
                    $row.Notes = $notesOverrides[$row.PerkName]
                }

                foreach ($key in @("Description", "Notes")) {
                    if ($entry.ContainsKey($key)) {
                        $row[$key] = $entry[$key]
                    }
                }
            }

            $rowNode = Get-RowNode -WorksheetXml $devicesWorksheetXml -RowNumber $rowNumber
            foreach ($column in $columns.GetEnumerator()) {
                $cellReference = "$($column.Value)$rowNumber"
                $value = $row[$column.Key]
                if ($column.Key -in @("Price", "STM") -and $value -match "^\d+(\.0+)?$") {
                    $integerValue = ([int][decimal]$value).ToString()
                    Set-NumberCell -WorksheetXml $devicesWorksheetXml -RowNode $rowNode -CellReference $cellReference -Value $integerValue -Style "4"
                }
                else {
                    Set-TextCell -WorksheetXml $devicesWorksheetXml -RowNode $rowNode -CellReference $cellReference -Value $value -Style "4"
                }
            }

            $writtenRows += [pscustomobject]@{
                Style = $layout.Style
                Row = $rowNumber
                Price = [int]$row.Price
                PerkName = $row.PerkName
                Type = $row.Type
            }
            $rowNumber++
        }

        $totalRow = Get-RowNode -WorksheetXml $devicesWorksheetXml -RowNumber $layout.TotalRow
        Set-TextCell -WorksheetXml $devicesWorksheetXml -RowNode $totalRow -CellReference "A$($layout.TotalRow)" -Value "Total" -Style "2"
        Set-FormulaCell -WorksheetXml $devicesWorksheetXml -RowNode $totalRow -CellReference "B$($layout.TotalRow)" -Formula $layout.TotalFormula -CachedValue "55" -Style "2"
    }

    $grandTotalRow = Get-RowNode -WorksheetXml $devicesWorksheetXml -RowNumber 4
    Set-FormulaCell -WorksheetXml $devicesWorksheetXml -RowNode $grandTotalRow -CellReference "D4" -Formula "SUM(B25,B43,B62,B81)" -CachedValue "220" -Style "2"

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

    Write-ZipEntryXml -Zip $zip -EntryPath $devicesSheetPath -Xml $devicesWorksheetXml
    Write-ZipEntryXml -Zip $zip -EntryPath "xl/workbook.xml" -Xml $workbookXml
}
finally {
    $zip.Dispose()
}

$writtenRows |
    Group-Object Style |
    ForEach-Object {
        [pscustomobject]@{
            Style = $_.Name
            Perks = $_.Count
            SP = ($_.Group | Measure-Object Price -Sum).Sum
            Active = @($_.Group | Where-Object { $_.Type -in @("Combat", "Stance", "Toggle", "Aura") }).Count
            Trait = @($_.Group | Where-Object { $_.Type -eq "Trait" }).Count
        }
    } |
    Sort-Object Style |
    Format-Table -AutoSize

[pscustomobject]@{
    DevicePerks = $writtenRows.Count
    DeviceSP = ($writtenRows | Measure-Object Price -Sum).Sum
    RemovedPerks = $removedPerks.Count
    RemovedPerkNames = ($removedPerks -join ", ")
} | Format-List

Write-Host "Updated Devices to match Force option count and 220 SP in '$workbookPath'."
