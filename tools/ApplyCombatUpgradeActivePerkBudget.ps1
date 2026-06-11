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

function Get-MappedCellValue {
    param(
        [hashtable]$Cells,
        [hashtable]$ColumnByHeader,
        [string]$Header
    )

    if (!$ColumnByHeader.ContainsKey($Header)) {
        return ""
    }

    return $Cells[$ColumnByHeader[$Header]]
}

function Get-BasePerkName {
    param([string]$Name)

    return (($Name.Trim()) -replace "\s+(I|II|III|IV|V|VI|VII|VIII|IX|X)$", "").Trim()
}

function Get-TargetKey {
    param(
        [string]$Tab,
        [string]$Style,
        [string]$PerkName
    )

    return "$Tab|$Style|$(Get-BasePerkName $PerkName)"
}

function Get-RowKey {
    param(
        [string]$Tab,
        [int]$Row
    )

    return "$Tab|$Row"
}

function ConvertTo-SentenceStart {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return ""
    }

    $trimmed = $Text.Trim()
    if ($trimmed.Length -eq 1) {
        return $trimmed.ToUpperInvariant()
    }

    return $trimmed.Substring(0, 1).ToUpperInvariant() + $trimmed.Substring(1)
}

function Get-CoreDescription {
    param([string]$Description)

    $cleanDescription = Normalize-CellText $Description
    $cleanDescription = $cleanDescription -replace "\s+", " "

    do {
        $previousDescription = $cleanDescription
        $cleanDescription = $cleanDescription -replace "^(?i)Passive upgrade\.\s*Retained\s+.+?\s+(?:abilities can deliver|attacks can trigger)\s+this effect\s+without a separate hotbar activation:\s*", ""
        $cleanDescription = $cleanDescription -replace "^(?i)Retained\s+.+?\s+(?:abilities can deliver|attacks can trigger)\s+this effect:\s*", ""
        $cleanDescription = $cleanDescription -replace "^(?i)Passive\.\s*", ""
        $cleanDescription = $cleanDescription -replace "^(?i)While meeting this perk's equipment requirements,\s*", ""
    } while ($cleanDescription -ne $previousDescription)

    return $cleanDescription.Trim()
}

function Get-TraitDescription {
    param(
        [string]$Style,
        [string]$PerkName,
        [string]$Type,
        [string]$Description
    )

    $cleanDescription = Get-CoreDescription $Description

    if ($PerkName -eq "Precognition") {
        return "After spending FP on a Force power, gain +5% Defense and +5% Evasion for 8 seconds. This can trigger once every 12 seconds."
    }

    if ($cleanDescription -match "^(?i)Your next attack") {
        return "$Style attacks can trigger this effect: $cleanDescription"
    }

    return "$Style abilities can deliver this effect: $cleanDescription"
}

function Join-NonBlank {
    param([string[]]$Values)

    return ($Values | Where-Object { ![string]::IsNullOrWhiteSpace($_) }) -join "; "
}

function Get-TraitNotes {
    param(
        [string]$Notes,
        [string]$FP,
        [string]$STM,
        [string]$CastingTime,
        [string]$CooldownTime
    )

    $legacyMarker = "Converted from separate active ability to passive trait for the 4-6 active-button budget."
    $marker = "Converted from separate active ability to Trait row for the 4-6 active-button budget."
    $cleanNotes = Normalize-CellText $Notes
    if ($cleanNotes.Contains($legacyMarker)) {
        return $cleanNotes.Replace($legacyMarker, $marker)
    }

    if ($cleanNotes.Contains($marker)) {
        return $cleanNotes
    }

    $formerValues = Join-NonBlank @(
        $(if (![string]::IsNullOrWhiteSpace($FP) -and $FP -ne "-") { "FP $FP" } else { "" }),
        $(if (![string]::IsNullOrWhiteSpace($STM) -and $STM -ne "-") { "STM $STM" } else { "" }),
        $(if (![string]::IsNullOrWhiteSpace($CastingTime) -and $CastingTime -ne "-") { "casting $CastingTime" } else { "" }),
        $(if (![string]::IsNullOrWhiteSpace($CooldownTime) -and $CooldownTime -ne "-") { "cooldown $CooldownTime" } else { "" })
    )

    $addition = $marker
    if (![string]::IsNullOrWhiteSpace($formerValues)) {
        $addition = "$addition Former active values: $formerValues."
    }

    if ([string]::IsNullOrWhiteSpace($cleanNotes)) {
        return $addition
    }

    return "$cleanNotes $addition"
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
    $cell = $null

    foreach ($candidate in $RowNode.GetElementsByTagName("c", $namespace)) {
        if ($candidate.GetAttribute("r") -eq $cellReference) {
            $cell = $candidate
            break
        }
    }

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

$targetBaseKeys = @(
    "Beast Mastery|Balanced|Pack Recovery",
    "Beast Mastery|Damage|Predator's Mark",
    "Beast Mastery|Damage|Predator Rush",
    "Devices|Field Support|Rayshield Screen",
    "Devices|Grenadier|Cluster Grenade",
    "First Aid|Combat Pharmacology|Coagulant",
    "First Aid|Trauma Medic|Emergency Sealant",
    "Force|Universal|Mind Shroud",
    "Force|Dark Manipulator|Fracture Focus",
    "Force|Dark Manipulator|Collapse Will",
    "Force|Dark Ravager|Force Body",
    "Force|Dark Ravager|Devouring Strike",
    "Force|Universal|Saber Rend",
    "Force|Universal|Precognition",
    "Force|Light Consular|Clarity",
    "Force|Light Consular|Force Mend",
    "Force|Light Guardian|Courageous Resolve",
    "Force|Light Guardian|Soothing Guard",
    "Force|Light Guardian|Reflective Barrier",
    "Force|Light Guardian|Bastion of Light",
    "Heavy Vibroblade|Defense|Anger Strike",
    "Heavy Vibroblade|Defense|Crushing Blow",
    "Heavy Vibroblade|Defense|Guardian's Resolve",
    "Heavy Vibroblade|Defense|Edge of Darkness",
    "Heavy Vibroblade|Defense|Blood Weapon",
    "Heavy Vibroblade|Offense|Essence Hunter",
    "Heavy Vibroblade|Offense|Bloodlust",
    "Heavy Vibroblade|Offense|Soul Sacrifice",
    "Heavy Vibroblade|Offense|Soul Ascension",
    "Katar|Iron Guard|Iron Elbows",
    "Katar|Iron Guard|Breaker Reversal",
    "Katar|Iron Guard|Covering Claws",
    "Katar|Venom Current|Venom Splash",
    "Katar|Venom Current|Twin Fang Flurry",
    "Katar|Venom Current|Toxic Rush",
    "Leadership|Field Steward|Bolster Resolve",
    "Leadership|Vanguard Command|Mark Target",
    "Lightsaber|Defense|Guardian's Influence",
    "Lightsaber|Offense|Centering",
    "Lightsaber|Offense|Second Wind",
    "Lightsaber|Offense|Purify",
    "Lightsaber|Offense|Surge Strike",
    "Lightsaber|Offense|Ripple Slash",
    "Lightsaber|Offense|Overwhelming Strike",
    "Lightsaber|Offense|Arc Strike",
    "Pistol|Skirmisher|Low Shot",
    "Pistol|Skirmisher|Ricochet Shot",
    "Pistol|Skirmisher|Snap Roll",
    "Rifle|Marksman|Expose Weak Point",
    "Rifle|Marksman|Kill Zone",
    "Rifle|Marksman|Breach Round",
    "Rifle|Pacification|Overwatch",
    "Rifle|Pacification|Neutralizing Shot",
    "Rifle|Pacification|Pinning Fire",
    "Saberstaff|Conduit|Force Lens",
    "Saberstaff|Conduit|Conduit Flare",
    "Saberstaff|Tempest|Force Gyre",
    "Spear|Damage|Breach Strike",
    "Spear|Damage|Improved Attentiveness",
    "Spear|Damage|Crippling Defense",
    "Spear|Disabler|Fracture Strike",
    "Spear|Disabler|Forcebane",
    "Spear|Disabler|Force Nullification",
    "Staff|Crusher|Skull Rattle",
    "Staff|Sentinel|Guarding Step",
    "Staff|Sentinel|Sentinel Guard",
    "Throwing|Bombardier|Saturation Toss",
    "Throwing|Bombardier|Cluster Storm",
    "Throwing|Deadeye|Marking Toss",
    "Throwing|Deadeye|Ricochet Toss",
    "Twin Blade|Cyclone|Sweeping Advance",
    "Twin Blade|Duelist|Reversal Cut",
    "Vibroblade|Offense|Whirlwind Assault",
    "Vibroknife|Saboteur|Toxic Coating",
    "Vibroknife|Saboteur|Sap Vitality",
    "Vibroknife|Saboteur|Cascade Failure",
    "Vibroknife|Shadow|Evasive Combat",
    "Vibroknife|Shadow|Marked for Death",
    "Vibroknife|Shadow|Decoy"
)

$targetBaseKeySet = @{}
foreach ($key in $targetBaseKeys) {
    $targetBaseKeySet[$key] = $true
}

$renameByRow = @{
    "Beast Mastery|61|Predator Rush" = "Predator's Mark II"
    "Heavy Vibroblade|40|Earthshatter" = "Earthshatter I"
    "Heavy Vibroblade|41|Edge of Darkness" = "Earthshatter II"
    "Lightsaber|22|Guardian's Challenge" = "Guardian's Challenge I"
    "Lightsaber|24|Thunderous Challenge" = "Guardian's Challenge II"
    "Vibroblade|31|Whirlwind Assault I" = "Riot Blade II"
    "Vibroblade|33|Riot Blade II" = "Riot Blade III"
    "Vibroblade|36|Savage Cleave" = "Savage Cleave I"
    "Vibroblade|37|Riot Blade III" = "Riot Blade IV"
    "Vibroblade|38|Whirlwind Assault II" = "Savage Cleave II"
    "Vibroknife|23|Smoke Bomb" = "Smoke Bomb I"
    "Vibroknife|25|Decoy" = "Smoke Bomb II"
}

$budgetDescriptionByRow = @{
    "Beast Mastery|100|Pack Recovery" = "When your beast uses a Balanced active ability, the beast and master each restore 1 STM. This can trigger once every 8 seconds."
    "Beast Mastery|53|Predator's Mark I" = "When your beast uses a Damage active ability, it marks the target for 12 seconds. The beast deals +10% damage to marked targets."
    "Beast Mastery|61|Predator Rush" = "Predator's Mark also causes damage against marked targets to grant the beast +5% Haste and +2% hit chance for 8 seconds, stacking up to +20% Haste and +8% hit chance."
    "Devices|18|Cluster Grenade" = "Grenadier explosive abilities split into secondary fragments, dealing 30% of the original damage to up to two nearby enemies. Consumes no extra explosives."
    "Devices|52|Rayshield Screen I" = "Field Support shielding abilities also reduce ranged physical damage taken by affected allies by 10% while their temporary HP remains."
    "Devices|58|Rayshield Screen II" = "Field Support shielding abilities also reduce ranged physical damage taken by affected allies by 15% while their temporary HP remains."
    "First Aid|11|Emergency Sealant I" = "Trauma Medic healing and treatment abilities also stop one Bleed or Burn effect and restore HP equal to 2% of maximum HP plus WIL scaling every 3 seconds for 12 seconds."
    "First Aid|29|Coagulant I" = "Combat Pharmacology stim effects also grant 50% Bleed Resistance and 10% resistance to incoming physical damage over time effects for 2 minutes."
    "First Aid|39|Coagulant II" = "Combat Pharmacology stim effects also grant Bleed immunity and 20% resistance to incoming physical damage over time effects for 2 minutes."
    "Force|12|Saber Rend I" = "After using a damaging Force power, your next melee attack within 8 seconds deals +12 force DMG plus WIL scaling."
    "Force|13|Mind Shroud I" = "After using a damaging Force power, reduce your force damage taken by 5% and gain +10% Confusion Resistance, +10% Daze Resistance, and +10% Fear Resistance for 12 seconds."
    "Force|14|Precognition" = "After spending FP on a Force power, gain +5% Defense and +5% Evasion for 8 seconds. This can trigger once every 12 seconds."
    "Force|19|Saber Rend II" = "After using a damaging Force power, your next melee attack within 8 seconds deals +24 force DMG plus WIL scaling."
    "Force|20|Mind Shroud II" = "After using a damaging Force power, reduce your force damage taken by 10% and gain +15% Confusion Resistance, +15% Daze Resistance, and +15% Fear Resistance for 12 seconds."
    "Force|28|Soothing Guard I" = "Light Guardian protection powers also remove one Poison, Bleed, Burn, Shock, or Disease effect from affected allies and grant 10% damage reduction for 8 seconds."
    "Force|30|Courageous Resolve" = "Light Guardian powers grant affected allies +10% Fear Resistance, +10% Daze Resistance, and +10% Confusion Resistance for 12 seconds. Affected allies take 5% less force damage while this benefit lasts."
    "Force|32|Reflective Barrier" = "Light Guardian barrier powers also reflect 15% of force and energy damage taken, plus WIL scaling, back to the attacker while the barrier remains."
    "Force|35|Bastion of Light" = "Light Guardian protection powers also grant nearby allies temporary HP equal to 10% of maximum HP plus WIL scaling and 10% reduced force damage taken for 12 seconds."
    "Force|42|Clarity I" = "Light Consular restorative powers also restore 10% of maximum STM to allies, or FP to you when self-targeted, and grant +4% physical and force ability hit chance for 15 seconds."
    "Force|46|Force Mend" = "Light Consular restorative powers can remove one major negative effect from the target and restore HP equal to 16% of maximum HP plus WIL scaling. This can trigger once every 20 seconds per target."
    "Force|47|Clarity II" = "Light Consular restorative powers also restore 18% of maximum STM to allies, or FP to you when self-targeted, and grant +6% physical and force ability hit chance for 15 seconds."
    "Force|56|Force Body I" = "Damaging Dark Force powers cost no FP while you are below 35% HP, but each one costs HP equal to 2% of your maximum HP."
    "Force|63|Devouring Strike" = "Dark Ravager attacks and damaging Dark Force powers deal 40% more damage to targets below 35% HP."
    "Force|64|Force Body II" = "Damaging Dark Force powers cost no FP while you are below 50% HP, but each one costs reduced HP when the target is below 50% HP."
    "Force|75|Fracture Focus I" = "Dark Manipulator control and resolve-breaking powers increase affected targets' FP and STM ability costs by 20% for 12 seconds."
    "Force|80|Force Grip III" = "Force Grip also Dazes the target for 3 seconds and applies force damage over time equal to 12 force DMG plus WIL scaling over 12 seconds."
    "Force|81|Fracture Focus II" = "Dark Manipulator control and resolve-breaking powers increase affected targets' FP and STM ability costs by 25% for 12 seconds."
    "Force|84|Collapse Will" = "Dark Manipulator area debuffs also apply Exposed and Force Erosion for 18 seconds."
    "Heavy Vibroblade|11|Essence Hunter" = "Heavy Vibroblade Offense weapon abilities also inflict Essence Drain, reducing the target's Attack by 15% for 12 seconds."
    "Heavy Vibroblade|19|Soul Sacrifice" = "After you spend HP on a Heavy Vibroblade Offense ability, gain +20% Attack and +10% critical chance for 12 seconds. The HP cost reduction scales with MGT."
    "Heavy Vibroblade|23|Bloodlust" = "After you spend HP on a Heavy Vibroblade Offense ability, restore 15% of maximum STM, increased by 1 percentage point per MGT to a maximum of 35%. This can trigger once every 30 seconds."
    "Heavy Vibroblade|26|Soul Ascension" = "Defeating an enemy after spending HP on a Heavy Vibroblade Offense ability grants +15% Attack and heals you for 20% of physical damage dealt for 20 seconds."
    "Heavy Vibroblade|30|Anger Strike" = "Heavy Vibroblade Defense attacks generate extra enmity, and your next attack after using a Heavy Vibroblade Defense ability deals +12 DMG."
    "Heavy Vibroblade|32|Crushing Blow" = "Heavy Vibroblade Defense attacks reduce affected targets' Defense by 15% for 16 seconds and generate significant enmity."
    "Heavy Vibroblade|37|Guardian's Resolve" = "When a Heavy Vibroblade Defense ability grants you Physical Defense or reduces incoming damage, you also gain a damage absorption shield equal to 12% of maximum HP for 12 seconds. You heal for 15% of damage absorbed. This can trigger once every 30 seconds."
    "Heavy Vibroblade|41|Edge of Darkness" = "Earthshatter deals +15 DMG to affected enemies and generates extra enmity."
    "Heavy Vibroblade|44|Blood Weapon" = "While you have a Heavy Vibroblade Defense Physical Defense or damage-reduction buff, restore HP equal to 2% of combat damage you deal."
    "Katar|13|Iron Elbows" = "Iron Guard counterattacks and guard pulses deal +15 DMG to nearby enemies and generate extra enmity."
    "Katar|16|Covering Claws" = "Iron Guard ally-protection abilities cause enemies hit to generate +25% Enmity toward you for 12 seconds."
    "Katar|24|Breaker Reversal" = "After guarding an attack, your next katar attack deals +35 DMG and inflicts Exposed, reducing Defense by 15% for 12 seconds."
    "Katar|36|Twin Fang Flurry" = "Single-target Venom Current abilities strike a second time for +10 DMG. If the target is poisoned, the second strike inflicts Bleed for 30 seconds."
    "Katar|37|Venom Splash" = "Venom Current strike abilities spread Poison to nearby enemies when they hit a poisoned target."
    "Katar|44|Toxic Rush" = "Damaging poisoned targets grants +4% Haste and +3% Attack for 6 seconds, stacking up to +20% Haste and +15% Attack. At maximum stacks, attacks against poisoned targets restore 2 STM."
    "Leadership|27|Mark Target I" = "Vanguard Command offensive commands mark affected enemies for 15 seconds. Party members deal +8% damage to marked targets. SOC scaling can raise this to +10%."
    "Leadership|34|Mark Target II" = "Vanguard Command offensive commands mark affected enemies for 15 seconds. Party members deal +12% damage to marked targets, and marked targets suffer -10% evasion chance. SOC scaling can raise these to +15% damage and -12% evasion."
    "Leadership|46|Bolster Resolve I" = "Field Steward recovery commands also grant nearby party members temporary HP equal to 8% of maximum HP for 12 seconds. SOC scaling can raise this to 10%."
    "Leadership|53|Bolster Resolve II" = "Field Steward recovery commands also grant nearby party members temporary HP equal to 12% of maximum HP and 12% damage reduction for 15 seconds. SOC scaling can raise these to 15% temporary HP and 15% damage reduction."
    "Lightsaber|15|Guardian's Influence" = "Lightsaber Defense abilities grant nearby allies +8 Attack Deflection for 12 seconds. You do not receive this benefit."
    "Lightsaber|31|Centering I" = "Using a Lightsaber Offense ability reduces your enmity by 10% and grants +10% Accuracy for 8 seconds. This can trigger once every 20 seconds."
    "Lightsaber|35|Second Wind" = "When you fall below 35% STM, your next Lightsaber Offense ability restores 50% of maximum STM, increased by 1 percentage point per MGT to a maximum of 75%. This can trigger once every 90 seconds."
    "Lightsaber|36|Overwhelming Strike" = "Lightsaber Offense area abilities inflict Sunder on enemies hit, reducing Defense and Force Defense by 15% for 30 seconds."
    "Lightsaber|38|Purify" = "Lightsaber Offense area abilities remove one debuff from you and transfer it to a nearby enemy. This can trigger once every 20 seconds."
    "Lightsaber|41|Arc Strike" = "Lightsaber Offense area abilities deal +20 DMG to nearby secondary targets."
    "Lightsaber|42|Surge Strike" = "Lightsaber Offense single-target abilities also inflict Force Disruption, preventing the target from using Force abilities for 8 seconds."
    "Lightsaber|43|Centering II" = "Using a Lightsaber Offense ability reduces your enmity by 20% and grants +20% Accuracy for 8 seconds. This can trigger once every 20 seconds."
    "Lightsaber|44|Ripple Slash" = "Lightsaber Offense area abilities also inflict Disoriented on nearby enemies, reducing Accuracy and Evasion by 15% for 20 seconds."
    "Pistol|31|Snap Roll I" = "Skirmisher evasive abilities grant +15% Evasion for 6 seconds and reduce current enmity by 10%."
    "Pistol|36|Ricochet Shot" = "Skirmisher precision shots can bounce to up to three nearby enemies for +12 DMG and Blind for 6 seconds. This can trigger once every 12 seconds."
    "Pistol|37|Snap Roll II" = "Skirmisher evasive abilities grant +25% Evasion for 8 seconds and make your next pistol attack within 8 seconds deal +10 DMG."
    "Pistol|38|Low Shot" = "Skirmisher close-range abilities also inflict Disoriented for 12 seconds and deal +20 DMG to disoriented targets."
    "Rifle|18|Expose Weak Point" = "Marksman precision shots mark the target for 12 seconds. Physical attacks against marked targets deal +10% damage."
    "Rifle|20|Breach Round" = "Marksman precision shots ignore 25% of the target's Defense and deal +35 DMG."
    "Rifle|24|Kill Zone" = "Repeated rifle attacks against the same target stack +4% rifle damage for 20 seconds, up to +20%. Switching targets clears this bonus."
    "Rifle|31|Pinning Fire I" = "Pacification control shots also inflict Dazed for 2 seconds."
    "Rifle|37|Pinning Fire II" = "Pacification control shots also inflict Knockdown for 3 seconds."
    "Rifle|38|Overwatch" = "Pacification control shots interrupt the target's current ability activation and inflict Foggy Mind for 12 seconds."
    "Rifle|43|Neutralizing Shot" = "Tranq Cone, Pacification Field, and Stasis Volley remove one beneficial combat effect from affected enemies and inflict Disoriented for 12 seconds."
    "Saberstaff|21|Force Gyre" = "Tempest area abilities inflict Force Erosion for 12 seconds on enemies hit."
    "Saberstaff|36|Force Lens" = "Conduit defensive abilities grant allies +15% Force Defense for 20 seconds and grant you +8 Attack Deflection."
    "Saberstaff|41|Conduit Flare" = "Conduit offensive abilities deal +20 DMG to nearby enemies and inflict Force Disruption for 8 seconds."
    "Spear|18|Force Nullification" = "Spear Disabler interrupt abilities disable the target's Force abilities for 8 seconds."
    "Spear|21|Fracture Strike" = "Disruption Field and Total Force Denial inflict Fractured Focus, doubling affected targets' FP costs for 30 seconds."
    "Spear|26|Forcebane" = "Spear Disabler suppression abilities reduce affected targets' FP recovery by 75% for 45 seconds."
    "Spear|33|Breach Strike" = "Spear Damage flanking abilities inflict Breach, reducing Evasion and Defense by 20% for 30 seconds."
    "Spear|37|Improved Attentiveness" = "While one of your Spear Damage stances is active, party members other than you gain +5% physical and Force ability hit chance."
    "Spear|46|Crippling Defense" = "Spear Damage area abilities reduce affected targets' Physical Defense and Force Defense by 15% for 45 seconds. Restore 15 STM when this affects at least two enemies."
    "Staff|14|Guarding Step" = "Using a Staff Sentinel ability grants +25% Evasion and +20% Defense for 8 seconds. This can trigger once every 20 seconds."
    "Staff|18|Sentinel Guard" = "Staff Sentinel protection abilities grant nearby allies +8 Attack Deflection for 12 seconds and generate extra enmity."
    "Staff|41|Skull Rattle" = "Staff Crusher finishers inflict Dazed for 3 seconds and deal +34 DMG."
    "Throwing|20|Cluster Storm" = "Bombardier area attacks split into three secondary explosives that each deal +12 DMG to nearby enemies."
    "Throwing|24|Saturation Toss" = "Bombardier control attacks leave the target area saturated for 12 seconds, dealing +10 DMG every 4 seconds to enemies inside."
    "Throwing|34|Marking Toss" = "Deadeye single-target throws mark targets for 12 seconds. Throwing damage against marked targets is increased by 10%."
    "Throwing|36|Ricochet Toss I" = "Deadeye single-target throws also hit up to two additional enemies within 5 meters for +15 DMG each."
    "Throwing|42|Ricochet Toss II" = "Deadeye single-target throws also hit up to four additional enemies within 5 meters for +24 DMG each."
    "Twin Blade|21|Sweeping Advance" = "Twin Blade Cyclone area abilities restore 6 STM and grant +10% Haste for 8 seconds when they hit at least three enemies."
    "Twin Blade|41|Reversal Cut" = "After you are hit, your next Twin Blade Duelist ability within 8 seconds deals +40 DMG and inflicts Dazed for 3 seconds."
    "Vibroblade|31|Whirlwind Assault I" = "Riot Blade also deals +12 DMG to nearby enemies."
    "Vibroblade|38|Whirlwind Assault II" = "Savage Cleave deals +20 DMG to nearby enemies and restores 2 STM per secondary target hit, up to 8 STM."
    "Vibroknife|13|Evasive Combat I" = "Vibroknife Shadow evasive abilities grant +10% Evasion, reduce enmity by 15%, and reduce Attack by 15% for 8 seconds."
    "Vibroknife|19|Marked for Death" = "Vibroknife Shadow single-target abilities mark the target. Your next three attacks against that target deal +12 DMG each."
    "Vibroknife|22|Evasive Combat II" = "Vibroknife Shadow evasive abilities grant +20% Evasion, reduce enmity by 25%, and reduce Attack by 15% for 8 seconds."
    "Vibroknife|25|Decoy" = "Smoke Bomb leaves a decoy behind when it ends, causing enemies targeting you to suffer -25% Accuracy for 12 seconds."
    "Vibroknife|32|Toxic Coating I" = "Vibroknife Saboteur strike abilities deal +10 DMG and inflict Toxin for 30 seconds. Toxin deals damage equal to 1% of maximum HP per second."
    "Vibroknife|36|Sap Vitality" = "Vibroknife Saboteur abilities that inflict Hamstring, Disoriented, or Incapacitate also inflict Exhausted, reducing Defense and Force Defense by 10% for 15 seconds."
    "Vibroknife|42|Sap Vitality II" = "Vibroknife Saboteur abilities that inflict Hamstring, Disoriented, or Incapacitate also inflict Exhausted, reducing Defense and Force Defense by 15% for 15 seconds."
    "Vibroknife|43|Toxic Coating II" = "Vibroknife Saboteur strike abilities deal +22 DMG and inflict Toxin for 30 seconds. Toxin deals damage equal to 1% of maximum HP per second."
    "Vibroknife|46|Cascade Failure" = "Incapacitate also hits enemies in a cone and inflicts Vulnerable, reducing Defense by 10% for 12 seconds."
}

$descriptionRewriteByRow = @{
    "General|10|Dual Wield I" = "Off-hand attack delay is reduced by 10% when making off-hand attacks."
    "General|11|Dual Wield II" = "Off-hand attack delay is reduced by 20% total when making off-hand attacks."
    "General|12|Dual Wield III" = "Off-hand attack delay is reduced by 30% total when making off-hand attacks."
    "Beast Mastery|74|Anger II" = "Goads a single target into attacking the beast and grants the beast temporary HP equal to 15% of its maximum HP for 12 seconds."
    "Beast Mastery|105|Poison Breath I" = "The beast breathes poison at hostile targets in a cone, dealing 10 poison DMG plus MGT scaling and attempting to inflict Poison."
    "Beast Mastery|106|Ice Breath I" = "The beast breathes ice at hostile targets in a cone, dealing 10 ice DMG plus MGT scaling and slowing affected enemies for 4 seconds."
    "Beast Mastery|107|Crushing Slam I" = "The beast slams nearby hostile enemies for 10 physical DMG plus MGT scaling and Dazes them for 2 seconds."
    "Beast Mastery|109|Poison Breath II" = "The beast breathes poison at hostile targets in a cone, dealing 14 poison DMG plus MGT scaling and attempting to inflict Poison."
    "Beast Mastery|111|Ice Breath II" = "The beast breathes ice at hostile targets in a cone, dealing 14 ice DMG plus MGT scaling and slowing affected enemies for 5 seconds."
    "Beast Mastery|112|Crushing Slam II" = "The beast slams nearby hostile enemies for 14 physical DMG plus MGT scaling and Dazes them for 2 seconds."
    "Beast Mastery|114|Rampage I" = "The beast attacks up to 3 nearby hostile enemies for 10 physical DMG plus MGT scaling each."
    "Beast Mastery|115|Poison Breath III" = "The beast breathes poison at hostile targets in a cone, dealing 18 poison DMG plus MGT scaling and attempting to inflict Poison."
    "Beast Mastery|117|Ice Breath III" = "The beast breathes ice at hostile targets in a cone, dealing 18 ice DMG plus MGT scaling and immobilizing affected enemies for 2 seconds."
    "Beast Mastery|118|Crushing Slam III" = "The beast slams nearby hostile enemies for 18 physical DMG plus MGT scaling and Dazes them for 3 seconds."
    "Beast Mastery|120|Rampage II" = "The beast attacks up to 4 nearby hostile enemies for 14 physical DMG plus MGT scaling each."
    "Beast Mastery|153|Mindful Hide" = "The beast takes 8% less force damage and gains +10% Confusion Resistance, +10% Daze Resistance, and +10% Fear Resistance."
    "Heavy Vibroblade|15|Vampiric Fury" = "Critical hits restore HP equal to 25% of damage dealt, increased by 1 percentage point per MGT to a maximum of 45%. This can trigger once every 6 seconds."
    "Heavy Vibroblade|24|Soul Strike III" = "Your next attack deals +45 DMG and heals you for 45% of damage dealt. Amount healed increases by 1 percentage point per MGT to a maximum of 60%."
    "Heavy Vibroblade|35|Unbreakable Will" = "Gain +5 Attack Deflection, increased by +1 per 2 MGT to a maximum of +15. Deflecting an attack restores 4 STM. This can trigger once every 6 seconds."
    "Vibroblade|11|Bulwark I" = "Grants +15 Shield Deflection."
    "Vibroblade|12|Fortified Position I" = "Grants +8 Mind Resistance rating, +8 Trauma Resistance rating, and +8 Mobility Resistance rating."
    "Vibroblade|17|Bulwark II" = "Grants +25 Shield Deflection total."
    "Vibroblade|21|Fortified Position II" = "Grants +15 Mind Resistance rating, +15 Trauma Resistance rating, and +15 Mobility Resistance rating total."
    "Vibroblade|22|Bulwark III" = "Grants +35 Shield Deflection total."
    "Vibroblade|23|Unbreakable" = "When reduced below 25% HP, gain +40% Physical Defense for 10s. Once per 5min."
    "Lightsaber|9|Deflection Training I" = "Grants +8 Attack Deflection."
    "Lightsaber|11|Taunting Deflection" = "Goads all nearby enemies into attacking you for 30 seconds. While this effect lasts, your successful Attack Deflections restore 2 FP and generate increased enmity."
    "Lightsaber|13|Deflection Training II" = "Grants +14 Attack Deflection total."
    "Lightsaber|20|Deflection Training III" = "Grants +20 Attack Deflection total."
    "Lightsaber|18|Deflection Counter" = "After deflecting an attack, your next hostile Lightsaber ability within 15 seconds activates instantly."
    "Lightsaber|40|Blade Blitz" = "After dealing a critical hit, your next lightsaber auto-attack within 15 seconds is quickened to your fastest possible swing speed."
    "Lightsaber|23|Impenetrable Guard" = "While active, successful Attack Deflections restore 1 FP and generate +20% enmity. Attack and Force Attack are reduced by 20%."
    "Twin Blade|29|Centerline Guard" = "Gain +10 Attack Deflection. After deflecting an attack, your next attack within 8 seconds deals +8 DMG."
    "Katar|9|Guard Training I" = "Grants a 15% chance to guard against physical attacks, reducing that hit's damage by 20% and generating extra enmity."
    "Katar|22|Impenetrable Grip" = "Gain +20% Knockdown Resistance and +20% Daze Resistance. Guarded hits restore 4 STM."
    "Staff|10|Staff Parry I" = "Gain +8 Attack Deflection."
    "Staff|12|Sentinel Stance" = "While active, grants +15% Evasion and +12 Attack Deflection, but reduces Attack by 15%."
    "Staff|13|Staff Parry II" = "Gain +16 Attack Deflection total."
    "Staff|17|Staff Parry III" = "Gain +24 Attack Deflection total. Deflecting attacks restores 2 STM."
    "Staff|23|Staff Parry IV" = "Gain +30 Attack Deflection total. Deflecting attacks restores 4 STM."
    "Staff|26|Unmoving Center" = "For 45 seconds, you cannot be Knocked down or Dazed, gain +30 Attack Deflection, and generate +30% enmity."
    "Saberstaff|15|Spinning Deflection I" = "Gain +8 Attack Deflection. After deflecting an attack, your next Circle Slash deals +8 DMG."
    "Saberstaff|22|Spinning Deflection II" = "Gain +16 Attack Deflection total. Deflecting an attack restores 4 FP."
    "Saberstaff|25|Flow of the Maelstrom" = "After hitting 3 or more enemies with one saberstaff ability, gain +15% Haste and +8 Attack Deflection for 12 seconds."
    "Saberstaff|26|Saber Cyclone" = "Deal weapon DMG + 18 to nearby enemies. For 45 seconds, pulse every 6 seconds, dealing 8 force DMG to nearby enemies and restoring 1 FP per enemy hit, up to 5 FP per pulse."
    "Saberstaff|31|Guarded Channel I" = "Gain +12 Attack Deflection and +20% Force Defense for 10 seconds."
    "Saberstaff|37|Guarded Channel II" = "Gain +22 Attack Deflection and +30% Force Defense for 12 seconds."
    "Saberstaff|43|Guarded Channel III" = "Gain +30 Attack Deflection and +35% Force Defense for 15 seconds."
    "Twin Blade|26|Tempest Bloom" = "Deal weapon DMG + 20 to nearby enemies. For 45 seconds, pulse every 6 seconds, dealing 8 physical DMG and applying a Tempest mark. Each mark increases physical damage taken by 2% to a maximum of 3 stacks."
    "Vibroblade|33|Riot Blade III" = "Instantly deals weapon DMG + 30 to your target."
    "Vibroblade|36|Savage Cleave I" = "Strike all enemies in front for weapon DMG + 25."
    "Vibroblade|37|Riot Blade IV" = "Instantly deals weapon DMG + 45 to your target."
    "Vibroknife|23|Smoke Bomb I" = "All enemies in the selected area are afflicted with Smoke Bomb, reducing Accuracy by 20% for 12 seconds."
    "Force|8|Force Push I" = "Deals 8 force DMG to one target, knocks down for 2 seconds, and slows movement for 3 seconds."
    "Force|15|Force Push II" = "Deals 12 force DMG to the selected target and up to 1 additional target in a line, knocks down for 2 seconds, and slows movement for 3 seconds."
    "Force|22|Force Push III" = "Deals 18 force DMG to the selected target and up to 2 additional targets in a cone, knocks down for 2 seconds, and slows movement for 4 seconds."
    "Force|27|Deflective Presence" = "Light Guardian combat powers increase attack deflection effectiveness by 8% for 10 seconds."
    "Force|40|Pacify I" = "Subdues one target, reducing outgoing weapon and force damage by 5% for 20 seconds."
    "Force|45|Pacify II" = "Subdues the selected target and one nearby enemy, reducing outgoing weapon and force damage by 8% for 20 seconds."
    "Force|51|Pacify III" = "Subdues the selected target and nearby enemies, reducing outgoing weapon and force damage by 12% for 20 seconds."
    "Force|85|Force Grip IV" = "Immobilizes the selected target and one additional nearby enemy for 4 seconds and interrupts activation."
    "Heavy Vibroblade|40|Earthshatter I" = "You deal weapon DMG + 20 to all enemies within the area of effect (line) from you. Inflicts Force Disruption on each target which disables the use of force abilities for 12 seconds."
    "Devices|25|Thermal Detonator" = "Deals 24 fire DMG plus PER scaling in a 5m blast and inflicts Burning for 45 seconds. Consumes explosives."
    "Devices|28|Blaster Beacon I" = "Plants a targeting beacon for 18 seconds. Every 3 seconds, one hostile target within 12m is hit for 10 physical DMG plus PER scaling."
    "Devices|30|Incendiary Field I" = "Deploys a visible 5m fire field for 12 seconds. Every 3 seconds, enemies inside take 10 fire DMG plus PER scaling."
    "Devices|31|Remote Charge I" = "Arms a visible charge at your target location that detonates after 3 seconds for 10 fire DMG plus PER scaling in a 5m blast."
    "Devices|32|Blaster Beacon II" = "Plants a targeting beacon for 21 seconds. Every 3 seconds, one hostile target within 12m is hit for 14 physical DMG plus PER scaling."
    "Devices|35|Shock Beacon I" = "Plants a shock beacon for 15 seconds. Every 3 seconds, one hostile target within 10m is hit for 10 electrical DMG plus PER scaling and suffers Shock for 6 seconds."
    "Devices|36|Incendiary Field II" = "Deploys a visible 5m fire field for 15 seconds. Every 3 seconds, enemies inside take 14 fire DMG plus PER scaling."
    "Devices|37|Remote Charge II" = "Arms a visible charge that detonates after 3 seconds for 14 fire DMG plus PER scaling in a 5m blast and Knockdown."
    "Devices|38|Blaster Beacon III" = "Plants a targeting beacon for 24 seconds. Every 3 seconds, one hostile target within 14m is hit for 18 physical DMG plus PER scaling."
    "Devices|40|Shock Beacon II" = "Plants a shock beacon for 18 seconds. Every 3 seconds, one hostile target within 12m is hit for 14 electrical DMG plus PER scaling and suffers Shock for 6 seconds."
    "Devices|41|Incendiary Field III" = "Deploys a visible 5m fire field for 18 seconds. Every 3 seconds, enemies inside take 18 fire DMG plus PER scaling."
    "Devices|43|Remote Charge III" = "Arms a visible charge that detonates after 3 seconds for 20 fire DMG plus PER scaling in a 5m blast and Knockdown."
    "Devices|44|Killzone Beacon" = "Plants a killzone beacon for 45 seconds. Every 3 seconds, it triggers one 22 physical DMG pulse and one 14 electrical DMG pulse against hostile targets within 12m; the electrical pulse inflicts Shock for 45 seconds."
    "Devices|66|Flamethrower I" = "Deals 10 fire DMG plus PER scaling to hostile targets in a cone."
    "Devices|67|Wrist Rocket I" = "Deals 12 fire DMG plus PER scaling to one target."
    "Devices|70|Flamethrower II" = "Deals 14 fire DMG plus PER scaling to hostile targets in a cone and inflicts Burning for 12 seconds."
    "Devices|71|Rail Dart I" = "Fires a dart that deals 12 physical DMG plus PER scaling and inflicts Bleed for 12 seconds."
    "Devices|73|Wrist Rocket II" = "Deals 16 fire DMG plus PER scaling to one target and Knockdown for 2 seconds."
    "Devices|75|Cryo Sprayer I" = "Deals 10 ice DMG plus PER scaling to hostile targets in a cone and inflicts Hobble for 5 seconds."
    "Devices|76|Flamethrower III" = "Deals 18 fire DMG plus PER scaling to hostile targets in a cone and inflicts Burning for 12 seconds."
    "Devices|77|Rail Dart II" = "Fires a dart that deals 16 physical DMG plus PER scaling and inflicts Bleed for 12 seconds."
    "Devices|78|Wrist Rocket III" = "Deals 20 fire DMG plus PER scaling to one target and Knockdown for 3 seconds."
    "Devices|81|Cryo Sprayer II" = "Deals 14 ice DMG plus PER scaling to hostile targets in a cone and immobilizes affected enemies for 2 seconds."
    "Devices|82|Overload Barrage" = "Unleashes three PER-scaling attacks at your primary target's location: an 18 fire DMG area burst plus Burning for 45 seconds, a 20 fire DMG single-target hit plus Knockdown for 3 seconds, and an 18 sonic DMG area burst that interrupts activation and reduces Accuracy by 10% for 45 seconds."
    "Lightsaber|22|Guardian's Challenge" = "Enemies in a cone take weapon DMG +35 and generate increased enmity toward you."
    "Lightsaber|24|Thunderous Challenge" = "Enemies in a line take weapon DMG +35 and generate increased enmity toward you. If this hits two or more enemies, your next successful Attack Deflection within 8 seconds restores 3 FP and generates increased enmity."
    "Lightsaber|25|Guardian Master" = "For 45 seconds, successful Attack Deflections restore 4 FP, refresh Deflective Presence, and generate +50% enmity. Your Attack Deflection cap increases by +10 while this lasts."
    "Leadership|28|Charge Order I" = "Nearby party members gain +10% movement speed and +30 Mobility Resistance. SOC scaling can raise these to +12% movement speed and +40 Mobility Resistance."
    "Leadership|35|Charge Order II" = "Nearby party members gain +15% movement speed and +50 Mobility Resistance. SOC scaling can raise these to +18% movement speed and +65 Mobility Resistance."
    "Leadership|45|Steady Formation I" = "Nearby party members gain +3% evasion chance, +30 Mind Resistance, and +30 Mobility Resistance. SOC scaling can raise these to +4% evasion chance, +40 Mind Resistance, and +40 Mobility Resistance."
    "Leadership|51|Steady Formation II" = "Nearby party members gain +5% evasion chance, +50 Mind Resistance, and +50 Mobility Resistance. SOC scaling can raise these to +6% evasion chance, +65 Mind Resistance, and +65 Mobility Resistance."
    "Lightsaber|33|Focused Stance" = "While active, against Sundered targets, Lightsaber Offense attacks have +10% Accuracy and +8% critical hit chance. Versatile Strike lengthens an existing Sunder duration by 6 seconds, up to 45 seconds. Area Lightsaber Offense damage is reduced by 15%."
    "First Aid|21|Treatment Kit III" = "Removes Bleed, Poison, Toxin, Burn, Shock, and Disease from a single target and grants 50% Fire Resistance, 50% Poison Resistance, 50% Electrical Resistance, 50% Ice Resistance, and 50% Trauma Resistance for 8 seconds."
    "First Aid|32|Antitoxin I" = "Grants 50% Poison Resistance and 50% Disease Resistance for 2 minutes and removes one Poison or Toxin effect. Consumes a stim pack."
    "First Aid|43|Emergency Cocktail" = "Restores 25% of maximum STM, removes one Poison or Toxin effect, then for 45 seconds restores 1 STM every 3 seconds, grants temporary HP equal to 12% of maximum HP plus WIL scaling, reduces damage taken by 12%, and grants 50% Poison Resistance and 50% Disease Resistance."
}

$additionalRequirementsClearByRow = @{
    "General|10|Dual Wield I" = $true
    "General|11|Dual Wield II" = $true
    "General|12|Dual Wield III" = $true
}

$notesRewriteByRow = @{
    "General|10|Dual Wield I" = "Mixed weapon types are valid; legal off-hand behavior is handled by the combat system."
    "General|11|Dual Wield II" = "Mixed weapon types are valid; legal off-hand behavior is handled by the combat system."
    "General|12|Dual Wield III" = "Mixed weapon types are valid; legal off-hand behavior is handled by the combat system."
    "Force|8|Force Push I" = "Universal kinetic pressure. Lower direct damage than Dark Ravager attacks because it also controls movement. No affinity scaling."
    "Force|15|Force Push II" = "Replacement tier: selected target receives the full lower-rank effect. Universal kinetic pressure. No affinity scaling."
    "Force|22|Force Push III" = "Replacement tier: selected target receives the full lower-rank effect. Universal kinetic pressure. No affinity scaling."
    "Force|40|Pacify I" = "Light debuff, not direct damage."
    "Force|45|Pacify II" = "Replacement tier: selected target receives the full lower-rank effect. Light debuff, not direct damage."
    "Force|51|Pacify III" = "Replacement tier: selected target receives the full lower-rank effect. Light debuff, not direct damage."
    "Lightsaber|22|Guardian's Challenge" = "Replacement tier: lower-rank active ability feat is removed when Guardian's Challenge II is learned."
    "Lightsaber|24|Thunderous Challenge" = "Formerly Thunderous Challenge. Folded into the Guardian's Challenge active line so this tree has one challenge button instead of two similar challenge buttons."
}

function ConvertTo-RowKeyMap {
    param([hashtable]$Map)

    $rowKeyMap = @{}
    foreach ($entry in $Map.GetEnumerator()) {
        $parts = $entry.Key.Split("|")
        $rowKeyMap["$($parts[0])|$($parts[1])"] = $entry.Value
    }

    return $rowKeyMap
}

$renameByRowKey = ConvertTo-RowKeyMap $renameByRow
$budgetDescriptionByRowKey = ConvertTo-RowKeyMap $budgetDescriptionByRow
$descriptionRewriteByRowKey = ConvertTo-RowKeyMap $descriptionRewriteByRow
$additionalRequirementsClearByRowKey = ConvertTo-RowKeyMap $additionalRequirementsClearByRow
$notesRewriteByRowKey = ConvertTo-RowKeyMap $notesRewriteByRow

$workbookFullPath = Resolve-RepoPath $BibleWorkbookPath
if (!(Test-Path $workbookFullPath)) {
    throw "Workbook '$workbookFullPath' was not found."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$modifiedSheets = @{}
$updatedRows = [System.Collections.Generic.List[object]]::new()
$budgetUpdatedRows = [System.Collections.Generic.List[object]]::new()

$zip = [System.IO.Compression.ZipFile]::Open($workbookFullPath, [System.IO.Compression.ZipArchiveMode]::Update)
try {
    $sharedStrings = [System.Collections.Generic.List[string]]::new()
    if ($null -ne $zip.GetEntry("xl/sharedStrings.xml")) {
        [xml]$sharedStringsXml = Read-ZipEntryText -Zip $zip -EntryPath "xl/sharedStrings.xml"
        $sharedStringNamespace = [System.Xml.XmlNamespaceManager]::new($sharedStringsXml.NameTable)
        $sharedStringNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

        foreach ($sharedString in $sharedStringsXml.SelectNodes("//d:si", $sharedStringNamespace)) {
            $texts = [System.Collections.Generic.List[string]]::new()
            foreach ($textNode in $sharedString.SelectNodes(".//d:t", $sharedStringNamespace)) {
                $texts.Add($textNode.InnerText) | Out-Null
            }

            $sharedStrings.Add(($texts -join "")) | Out-Null
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

    $sheetPathsByName = @{}
    foreach ($sheet in $workbookXml.SelectNodes("//d:sheets/d:sheet", $workbookNamespace)) {
        $relationshipId = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
        $sheetPathsByName[$sheet.GetAttribute("name")] = $relationshipsById[$relationshipId]
    }

    $tabs = @(
        $targetBaseKeys
        $renameByRow.Keys
        $descriptionRewriteByRow.Keys
        $additionalRequirementsClearByRow.Keys
        $notesRewriteByRow.Keys
    ) |
        ForEach-Object { $_.Split("|")[0] } |
        Sort-Object -Unique

    foreach ($tab in $tabs) {
        if (!$sheetPathsByName.ContainsKey($tab)) {
            throw "Workbook sheet '$tab' was not found."
        }

        $sheetPath = $sheetPathsByName[$tab]
        [xml]$worksheetXml = Read-ZipEntryText -Zip $zip -EntryPath $sheetPath
        $worksheetNamespace = [System.Xml.XmlNamespaceManager]::new($worksheetXml.NameTable)
        $worksheetNamespace.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

        $headerRowNumber = 0
        $columnByHeader = @{}

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

            $rowNumber = [int]$rowNumberText
            if ($headerRowNumber -eq 0 -and (($cells.Values -join "|") -match "Perk Name|PerkName")) {
                $headerRowNumber = $rowNumber
                foreach ($cellEntry in $cells.GetEnumerator()) {
                    $canonicalHeader = Get-CanonicalManifestHeader $cellEntry.Value
                    if (![string]::IsNullOrWhiteSpace($canonicalHeader) -and !$columnByHeader.ContainsKey($canonicalHeader)) {
                        $columnByHeader[$canonicalHeader] = $cellEntry.Key
                    }
                }
                continue
            }

            if ($headerRowNumber -eq 0 -or $rowNumber -le $headerRowNumber) {
                continue
            }

            $perkName = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "PerkName"
            if ([string]::IsNullOrWhiteSpace($perkName)) {
                continue
            }

            $style = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "Style"
            $targetKey = Get-TargetKey -Tab $tab -Style $style -PerkName $perkName
            $rewriteKey = "$tab|$rowNumber|$perkName"
            $rowKey = Get-RowKey -Tab $tab -Row $rowNumber
            $isRename = $renameByRowKey.ContainsKey($rowKey)
            $isBudgetTarget = $targetBaseKeySet.ContainsKey($targetKey) -or $budgetDescriptionByRowKey.ContainsKey($rowKey)
            $isDescriptionRewrite = $descriptionRewriteByRowKey.ContainsKey($rowKey)
            $isAdditionalRequirementsClear = $additionalRequirementsClearByRowKey.ContainsKey($rowKey)
            $isNotesRewrite = $notesRewriteByRowKey.ContainsKey($rowKey)

            if (!$isRename -and !$isBudgetTarget -and !$isDescriptionRewrite -and !$isAdditionalRequirementsClear -and !$isNotesRewrite) {
                continue
            }

            foreach ($requiredHeader in @("PerkName", "Type", "Description")) {
                if (!$columnByHeader.ContainsKey($requiredHeader)) {
                    throw "Workbook sheet '$tab' is missing required column '$requiredHeader'."
                }
            }

            if ($isRename) {
                $perkName = $renameByRowKey[$rowKey]
                Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["PerkName"] -Text $perkName
            }

            $currentType = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "Type"
            $currentDescription = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "Description"

            $hasDescriptionUpdate = $isBudgetTarget -or $isDescriptionRewrite
            if ($isBudgetTarget) {
                if (!$budgetDescriptionByRowKey.ContainsKey($rowKey)) {
                    throw "Missing budget trait description for '$rowKey'."
                }

                $newDescription = $budgetDescriptionByRowKey[$rowKey]
            }
            elseif ($isDescriptionRewrite) {
                $newDescription = $descriptionRewriteByRowKey[$rowKey]
            }
            else {
                $newDescription = Get-TraitDescription `
                    -Style $style `
                    -PerkName $perkName `
                    -Type $currentType `
                    -Description $currentDescription
            }

            if ($hasDescriptionUpdate) {
                Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["Description"] -Text $newDescription
            }

            if ($isAdditionalRequirementsClear) {
                if (!$columnByHeader.ContainsKey("AdditionalRequirements")) {
                    throw "Workbook sheet '$tab' is missing required column 'AdditionalRequirements'."
                }

                Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["AdditionalRequirements"] -Text ""
            }

            if ($isNotesRewrite) {
                if (!$columnByHeader.ContainsKey("Notes")) {
                    throw "Workbook sheet '$tab' is missing required column 'Notes'."
                }

                Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["Notes"] -Text $notesRewriteByRowKey[$rowKey]
            }

            if ($isBudgetTarget) {
                foreach ($requiredHeader in @("FP", "STM", "CastingTime", "CooldownTime", "Notes")) {
                    if (!$columnByHeader.ContainsKey($requiredHeader)) {
                        throw "Workbook sheet '$tab' is missing required column '$requiredHeader'."
                    }
                }

                $currentFP = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "FP"
                $currentSTM = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "STM"
                $currentCastingTime = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "CastingTime"
                $currentCooldownTime = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "CooldownTime"
                $currentNotes = Get-MappedCellValue -Cells $cells -ColumnByHeader $columnByHeader -Header "Notes"

                $newNotes = Get-TraitNotes `
                    -Notes $currentNotes `
                    -FP $currentFP `
                    -STM $currentSTM `
                    -CastingTime $currentCastingTime `
                    -CooldownTime $currentCooldownTime

                Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["Type"] -Text "Trait"
                Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["FP"] -Text "-"
                Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["STM"] -Text "-"
                Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["CastingTime"] -Text "-"
                Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["CooldownTime"] -Text "-"
                Set-CellText -WorksheetXml $worksheetXml -RowNode $rowNode -ColumnIndex $columnByHeader["Notes"] -Text $newNotes

                $budgetUpdatedRows.Add([pscustomobject]@{
                    Tab = $tab
                    Row = $rowNumber
                    Style = $style
                    RowKey = $rowKey
                    PerkName = $perkName
                }) | Out-Null
            }

            $updatedRows.Add([pscustomobject]@{
                Tab = $tab
                Row = $rowNumber
                Style = $style
                PerkName = $perkName
            }) | Out-Null
            $modifiedSheets[$sheetPath] = $worksheetXml
        }
    }

    $foundKeys = @{}
    foreach ($row in $budgetUpdatedRows) {
        $foundKeys[$row.RowKey] = $true
    }

    $missingKeys = @(
        $budgetDescriptionByRowKey.Keys |
            Where-Object { !$foundKeys.ContainsKey($_) } |
            Sort-Object
    )

    if ($missingKeys.Count -gt 0) {
        throw "Missing active-budget target rows: $($missingKeys -join '; ')"
    }

    if ($foundKeys.Count -ne $budgetDescriptionByRowKey.Count) {
        throw "Expected to update $($budgetDescriptionByRowKey.Count) active-budget rows, but found $($foundKeys.Count)."
    }

    foreach ($entry in $modifiedSheets.GetEnumerator()) {
        Write-ZipEntryXml -Zip $zip -EntryPath $entry.Key -Xml $entry.Value
    }
}
finally {
    $zip.Dispose()
}

$updatedRows |
    Sort-Object Tab, Row |
    Format-Table Tab, Row, Style, PerkName -AutoSize

Write-Host "Updated $($updatedRows.Count) Combat Upgrade Bible rows in '$workbookFullPath'."
