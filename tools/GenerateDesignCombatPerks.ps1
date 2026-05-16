[CmdletBinding()]
param(
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\CombatUpgradeBiblePerkManifest.csv",
    [string]$PerkTypePath = "SWLOR.Game.Server\Service\PerkService\PerkType.cs",
    [string]$FeatTypePath = "SWLOR.NWN.API\NWScript\Enum\FeatType.cs",
    [string]$RecastGroupPath = "SWLOR.Game.Server\Service\AbilityService\RecastGroup.cs",
    [string]$Feat2daPath = "SWLOR_Haks\swlor2_2da\feat.2da",
    [string]$Spells2daPath = "SWLOR_Haks\swlor2_2da\spells.2da",
    [string]$ClsFeatFightPath = "SWLOR_Haks\swlor2_2da\CLS_FEAT_FIGHT.2da"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ScopedTabs = @("Beast Mastery", "Devices", "First Aid", "Force", "Leadership")
$ActiveTypes = @("Combat", "Stance", "Toggle")

function Resolve-RepoPath {
    param([string]$Path)
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return Join-Path $RepoRoot $Path
}

function ConvertTo-Identifier {
    param([string]$Name)
    $cleanName = $Name -replace "['’]", ""
    $parts = @([regex]::Split($cleanName.Trim(), "[^A-Za-z0-9]+") | Where-Object { ![string]::IsNullOrWhiteSpace($_) })
    $identifier = ($parts | ForEach-Object {
        if ($_ -cmatch "^[A-Z0-9]+$") {
            $_
        }
        elseif ($_.Length -eq 1) {
            $_.ToUpperInvariant()
        }
        else {
            $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1)
        }
    }) -join ""
    if ([string]::IsNullOrWhiteSpace($identifier)) {
        throw "Unable to build identifier from '$Name'."
    }
    if ($identifier[0] -match "[0-9]") {
        $identifier = "Perk$identifier"
    }
    return $identifier
}

function Get-BaseName {
    param([string]$Name)
    return ([regex]::Replace($Name.Trim(), "\s+(I|II|III|IV|V)$", "")).Trim()
}

function Get-RomanRank {
    param([string]$Name)
    if ($Name -match "\s+(I|II|III|IV|V)$") {
        switch ($Matches[1]) {
            "I" { return 1 }
            "II" { return 2 }
            "III" { return 3 }
            "IV" { return 4 }
            "V" { return 5 }
        }
    }
    return 1
}

function Get-FeatIdentifier {
    param([string]$Name)
    $baseName = Get-BaseName $Name
    $rank = Get-RomanRank $Name
    return "$(ConvertTo-Identifier $baseName)$rank"
}

function ConvertTo-CSharpString {
    param([string]$Text)
    if ($null -eq $Text) { return "" }
    return $Text.Replace("\", "\\").Replace('"', '\"')
}

function ConvertTo-Seconds {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -eq "-") { return 0 }
    if ($Value -match "Instant") { return 0 }
    if ($Value -match "([0-9]+(?:\.[0-9]+)?)\s*seconds?") { return [float]$Matches[1] }
    if ($Value -match "([0-9]+(?:\.[0-9]+)?)\s*minutes?") { return [float]$Matches[1] * 60 }
    if ($Value -match "^[0-9]+(?:\.[0-9]+)?$") { return [float]$Value }
    return 0
}

function ConvertTo-Int {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -eq "-") { return 0 }
    return [int][Math]::Round([decimal]$Value)
}

function Get-SkillType {
    param([string]$Tab)
    switch ($Tab) {
        "Beast Mastery" { return "BeastMastery" }
        "Devices" { return "Devices" }
        "First Aid" { return "FirstAid" }
        "Force" { return "Force" }
        "Leadership" { return "Leadership" }
        default { return "Invalid" }
    }
}

function Get-Category {
    param($Row)
    switch ($Row.Tab) {
        "Beast Mastery" { return "Beast$($Row.Style.Replace(' ', ''))" }
        "Devices" { return "Devices" }
        "First Aid" { return "FirstAid" }
        "Force" {
            if ($Row.Style -like "Light*") { return "ForceLight" }
            if ($Row.Style -like "Dark*") { return "ForceDark" }
            return "ForceUniversal"
        }
        "Leadership" {
            switch ($Row.Style) {
                "Vanguard Command" { return "LeadershipVanguardCommand" }
                "Field Steward" { return "LeadershipFieldSteward" }
                default { return "Leadership" }
            }
        }
        default { return "Invalid" }
    }
}

function Get-BeastRole {
    param([string]$Style)
    switch ($Style) {
        "Damage" { return "Damage" }
        "Tank" { return "Tank" }
        "Balanced" { return "Balanced" }
        "Bruiser" { return "Bruiser" }
        "Evasion" { return "Evasion" }
        "Force" { return "Force" }
        default { return "Invalid" }
    }
}

function Read-EnumMap {
    param([string]$Path)
    $map = [ordered]@{}
    foreach ($line in [System.IO.File]::ReadAllLines($Path)) {
        if ($line -match "^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(-?[0-9]+)\s*,") {
            $map[$Matches[1]] = [int]$Matches[2]
        }
    }
    return $map
}

function Get-NextEnumValue {
    param([hashtable]$Map)
    return (($Map.Values | Measure-Object -Maximum).Maximum + 1)
}

function Update-PerkType {
    param([object[]]$Rows)
    $path = Resolve-RepoPath $PerkTypePath
    $map = Read-EnumMap $path
    $next = Get-NextEnumValue $map
    $missing = New-Object System.Collections.Generic.List[string]

    foreach ($group in ($Rows | Group-Object BaseIdentifier | Sort-Object Name)) {
        if ($map.Contains($group.Name)) { continue }
        $missing.Add("        $($group.Name) = $next,") | Out-Null
        $map[$group.Name] = $next
        $next++
    }

    if ($missing.Count -le 0) { return $map }

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.AddRange([System.IO.File]::ReadAllLines($path))
    $insertIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "IDs\s+[0-9]+\+\s+are free") {
            $insertIndex = $i
            break
        }
    }
    if ($insertIndex -lt 0) { throw "Could not locate PerkType free-id marker." }

    $lines.RemoveAt($insertIndex)
    foreach ($entry in $missing) {
        $lines.Insert($insertIndex, $entry)
        $insertIndex++
    }
    $lines.Insert($insertIndex, "        // IDs $next+ are free.")
    [System.IO.File]::WriteAllLines($path, $lines)
    return $map
}

function Update-FeatType {
    param([object[]]$Rows)
    $path = Resolve-RepoPath $FeatTypePath
    $map = Read-EnumMap $path
    $next = Get-NextEnumValue $map
    $missing = New-Object System.Collections.Generic.List[string]

    foreach ($row in ($Rows | Where-Object { $ActiveTypes -contains $_.Type } | Sort-Object Row)) {
        if ($map.Contains($row.FeatIdentifier)) { continue }
        $missing.Add("        $($row.FeatIdentifier) = $next,") | Out-Null
        $map[$row.FeatIdentifier] = $next
        $row.FeatId = $next
        $next++
    }
    foreach ($row in ($Rows | Where-Object { $ActiveTypes -contains $_.Type })) {
        if ($null -eq $row.FeatId) {
            $row.FeatId = [int]$map[$row.FeatIdentifier]
        }
    }

    if ($missing.Count -le 0) { return $map }

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.AddRange([System.IO.File]::ReadAllLines($path))
    $closingBraces = New-Object System.Collections.Generic.List[int]
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim() -eq "}") {
            $closingBraces.Add($i) | Out-Null
        }
    }
    if ($closingBraces.Count -lt 2) { throw "Could not locate FeatType enum closing brace." }
    $insertIndex = $closingBraces[$closingBraces.Count - 2]
    foreach ($entry in $missing) {
        $lines.Insert($insertIndex, $entry)
        $insertIndex++
    }
    [System.IO.File]::WriteAllLines($path, $lines)
    return $map
}

function Get-RecastShortName {
    param([string]$Name)

    $shortNames = @{
        "Adhesive Grenade" = "Adhesive Gren"
        "Courageous Resolve" = "Courage Res."
        "Bastion of Light" = "Light Bastion"
        "Bolster Resolve" = "Bolster Res."
        "Circle of Harmony" = "Harmony Circle"
        "Cluster Grenade" = "Cluster Gren"
        "Comprehend Speech" = "Comp. Speech"
        "Concussion Grenade" = "Concuss Gren"
        "Coordinated Strike" = "Coord. Strike"
        "Creeping Terror" = "Creep Terror"
        "Dampening Field" = "Damp Field"
        "Decisive Command" = "Decisive Cmd"
        "Deflector Shield" = "Deflect Shield"
        "Devouring Strike" = "Devour Strike"
        "Distracting Feint" = "Distract Feint"
        "Dominate Weak Mind" = "Dom Weak Mind"
        "Eclipse of Resolve" = "Eclipse Res."
        "Emergency Bunker" = "Emerg. Bunker"
        "Emergency Cocktail" = "Emerg Cocktail"
        "Emergency Sealant" = "Emerg Sealant"
        "Emergency Triage" = "Emerg Triage"
        "Evasive Challenge" = "Evas Challenge"
        "Evasive Maneuver" = "Evas Maneuver"
        "Force-Bonded Beast" = "Bonded Beast"
        "Force Intercept" = "Force Interc."
        "Force Lightning" = "Force Lightng"
        "Force Maelstrom" = "Force Maelstr"
        "Force Sanctuary" = "Force Sanct."
        "Group Deflector" = "Group Deflect"
        "Rayshield Screen" = "Rayshield Scrn"
        "Hunger of the Dark" = "Dark Hunger"
        "Incendiary Field" = "Incend Field"
        "Killzone Beacon" = "Killzone Bcn"
        "Last Stand of the Light" = "Light's Stand"
        "Maintenance Pulse" = "Maint. Pulse"
        "Nightmare Field" = "Nightmare Fld"
        "Overload Barrage" = "Overload Barr"
        "Pain Suppressant" = "Pain Suppress"
        "Predator's Mark" = "Predator Mark"
        "Press the Attack" = "Press Attack"
        "Reflective Barrier" = "Reflect Barr"
        "Thermal Detonator" = "Thermal Det."
        "Unbreakable Beast" = "Unbreak Beast"
        "Untouchable Instinct" = "Untouch Inst."
    }

    if ($Name.Length -le 14) { return $Name }
    if ($shortNames.ContainsKey($Name)) { return $shortNames[$Name] }

    throw "RecastGroup '$Name' needs a meaningful short name of 14 characters or fewer. Do not auto-truncate."
}

function Test-TriggersDarkForceConversion {
    param([object]$Row)

    $darkForceConversionPerks = @(
        "CreepingTerror",
        "DevouringStrike",
        "ForceChoke",
        "ForceDrain",
        "ForceLightning",
        "ForceMaelstrom",
        "ForceSpark"
    )

    return $darkForceConversionPerks -contains $Row.BaseIdentifier
}

function Update-RecastGroup {
    param([object[]]$Rows)
    $path = Resolve-RepoPath $RecastGroupPath
    $map = Read-EnumMap $path
    $next = Get-NextEnumValue $map
    $missing = New-Object System.Collections.Generic.List[string]

    foreach ($group in ($Rows | Where-Object { $ActiveTypes -contains $_.Type } | Group-Object BaseIdentifier | Sort-Object Name)) {
        if ($map.Contains($group.Name)) { continue }
        $displayName = $group.Group[0].BaseName
        $shortName = Get-RecastShortName $displayName
        $missing.Add("        [RecastGroup(""$(ConvertTo-CSharpString $displayName)"", ""$(ConvertTo-CSharpString $shortName)"", true)]") | Out-Null
        $missing.Add("        $($group.Name) = $next,") | Out-Null
        $map[$group.Name] = $next
        $next++
    }

    if ($missing.Count -le 0) { return $map }

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.AddRange([System.IO.File]::ReadAllLines($path))
    $classIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "public class RecastGroupAttribute") {
            $classIndex = $i
            break
        }
    }
    if ($classIndex -lt 0) { throw "Could not locate RecastGroupAttribute class." }

    $insertIndex = -1
    for ($i = $classIndex - 1; $i -ge 0; $i--) {
        if ($lines[$i].Trim() -eq "}") {
            $insertIndex = $i
            break
        }
    }
    if ($insertIndex -lt 0) { throw "Could not locate RecastGroup enum closing brace." }

    foreach ($entry in $missing) {
        $lines.Insert($insertIndex, $entry)
        $insertIndex++
    }
    [System.IO.File]::WriteAllLines($path, $lines)
    return $map
}

function Get-StatusEffects {
    param([string]$Description)
    $checks = [ordered]@{
        "Force Disruption" = "ForceDisruptionStatusEffect"
        "Force Warding" = "ForceWardingStatusEffect"
        "Force Erosion" = "ForceErosionStatusEffect"
        "Foggy Mind" = "FoggyMindStatusEffect"
        "Confuse" = "FoggyMindStatusEffect"
        "Vital Strike" = "VitalStrikeStatusEffect"
        "Hemorrhage" = "HemorrhageStatusEffect"
        "Disoriented" = "DisorientedStatusEffect"
        "Exhausted" = "ExhaustedStatusEffect"
        "Hamstring" = "HamstringStatusEffect"
        "Knock down" = "KnockdownStatusEffect"
        "Knockdown" = "KnockdownStatusEffect"
        "Immobilize" = "ImmobilizedStatusEffect"
        "Slows movement" = "HobbleStatusEffect"
        "Slow movement" = "HobbleStatusEffect"
        "Weakened" = "WeakenedStatusEffect"
        "Exposed" = "ExposedStatusEffect"
        "Stunned" = "StunnedStatusEffect"
        "Daze" = "DazedStatusEffect"
        "Poison" = "PoisonStatusEffect"
        "Bleed" = "BleedStatusEffect"
        "Sunder" = "SunderStatusEffect"
        "Blind" = "BlindStatusEffect"
        "Toxin" = "ToxinStatusEffect"
        "Dazed" = "DazedStatusEffect"
        "Hobble" = "HobbleStatusEffect"
        "Flash" = "FlashStatusEffect"
        "Burn" = "BurnStatusEffect"
        "Shock" = "ShockStatusEffect"
        "Freezing" = "FreezingStatusEffect"
    }

    $results = New-Object System.Collections.Generic.List[string]
    foreach ($entry in $checks.GetEnumerator()) {
        if ($Description -match [regex]::Escape($entry.Key)) {
            if (!$results.Contains($entry.Value)) {
                $results.Add($entry.Value) | Out-Null
            }
        }
    }
    return $results.ToArray()
}

function Test-IsCleanse {
    param([string]$Description)
    return $Description -match "(?i)\b(remove|removes|stops|cleanse|cleanses|purify|ailment|ailments)\b"
}

function Test-IsRevive {
    param([string]$Description)
    return $Description -match "(?i)\brevive|revives|resuscitation"
}

function Test-IsSupport {
    param($Row)
    $text = $Row.Description
    if ($Row.Tab -eq "First Aid" -or $Row.Tab -eq "Leadership") {
        if ($text -notmatch "(?i)enemy|enemies|marks one enemy|suffer") { return $true }
    }
    if ($text -match "(?i)^increases outgoing|^reduces .*damage taken|grants .*resistance") { return $true }
    return $text -match "(?i)party|ally|allies|self|restore|restores|healing|temporary hp|regeneration|damage reduction|less damage|shield|guard|resolve|warding|hasten|innervate|bolster|cleanse|revive|absorb|redirect|the beast gains|the beast takes|the beast deals|beast and its master|language|speech"
}

function Test-IsHostile {
    param($Row)
    if (Test-IsSupport $Row) {
        if ($Row.Description -match "(?i)marks one enemy|nearby enemies|all nearby enemies|enemies suffer|one enemy|target suffers|goads? (?:a|one|single)|next attack|target's next|hostile targets") { return $true }
        return $false
    }
    return $Row.Description -match "(?i)enemy|enemies|damage|dmg|strike|grenade|detonator|rocket|bite|claw|slam|breath|suffer|poison|weaken|flash|sunder|rend|choke|grip|exposed|force erosion|fracture focus|fracture|creeping terror|collapse will|knock down|knockdown|immobilize|confuse|interrupt|ability costs|goads?|hostile targets|beacon"
}

function Test-IsArea {
    param($Row)
    return $Row.Description -match "(?i)nearby|area|field|grenade|detonator|cone|breath|spray|radius|within|enemies|up to [0-9]+ targets|beacon"
}

function Test-IsParty {
    param($Row)
    return $Row.Description -match "(?i)party members|nearby party|nearby allies|allies"
}

function Test-IsSelf {
    param($Row)
    if (Test-IsArea $Row) { return $false }
    return $Row.Description -match "(?i)\bself\b|the beast gains|the beast takes|the beast deals|beast and its master|gain[s]? \+|restores .*maximum stm|restores .*maximum fp"
}

function Test-RequiresTarget {
    param($Row)
    if (Test-IsArea $Row) { return $false }
    if (Test-IsSelf $Row) { return $false }
    return (Test-IsHostile $Row) -or $Row.Description -match "(?i)single target|one target|one enemy|ally|target"
}

function Test-IsSelfCenteredHostileArea {
    param($Row)
    return (Test-IsHostile $Row) -and (Test-IsArea $Row) -and ($Row.Description -match "(?i)nearby enemies|all nearby enemies|enemies within|hostile targets within")
}

function Test-IsGoad {
    param($Row)
    return $Row.Description -match "(?i)\bgoad(?:s|ed)?\b"
}

function Test-AffectsBeastAndMaster {
    param($Row)
    return $Row.Description -match "(?i)beast and its master"
}

function Test-AffectsBeastMasterOnly {
    param($Row)
    return $Row.Description -match "(?i)(?:its|the beast's) master" -and !(Test-AffectsBeastAndMaster $Row)
}

function Get-TraitStatLines {
    param([string]$Description)
    $lines = New-Object System.Collections.Generic.List[string]
    $percent = 0
    if ($Description -match "([0-9]+)%") { $percent = [int]$Matches[1] }
    if ($percent -le 0) { return ,$lines.ToArray() }

    if ($Description -match "(?i)hit chance|accuracy") {
        $lines.Add("                .IncreasesStat(StatType.AccuracyPercentAdjustment, $percent)") | Out-Null
    }
    if ($Description -match "(?i)critical") {
        $lines.Add("                .IncreasesStat(StatType.CriticalRatePercentAdjustment, $percent)") | Out-Null
    }
    if ($Description -match "(?i)enmity") {
        $lines.Add("                .IncreasesStat(StatType.EnmityPercentAdjustment, $percent)") | Out-Null
    }
    if ($Description -match "(?i)less damage|damage reduction|damage taken") {
        $lines.Add("                .IncreasesStat(StatType.DamageTakenPercentAdjustment, -$percent)") | Out-Null
    }
    if ($Description -match "(?i)force damage") {
        $lines.Add("                .IncreasesStat(StatType.ForceDefensePercentAdjustment, $percent)") | Out-Null
    }
    if ($Description -match "(?i)confusion|daze|fear|mind") {
        $lines.Add("                .IncreasesStat(StatType.MindResistance, $percent)") | Out-Null
    }

    return ,$lines.ToArray()
}

function Add-RequirementLines {
    param([System.Collections.Generic.List[string]]$Lines, $Row)
    if ($Row.Tab -eq "Beast Mastery") {
        $level = ConvertTo-Int $Row.SkillRequirements
        if ($level -gt 0) {
            $Lines.Add("                .RequirementBeastLevel($level)") | Out-Null
        }
        $role = Get-BeastRole $Row.Style
        if ($role -ne "Invalid") {
            $Lines.Add("                .RequirementBeastRole(BeastRoleType.$role)") | Out-Null
        }
        return
    }

    if (![string]::IsNullOrWhiteSpace($Row.SkillRequirements) -and $Row.SkillRequirements -ne "-") {
        if ($Row.SkillRequirements -match "(.+?)\s+([0-9]+)") {
            $skill = Get-SkillType $Row.Tab
            $rank = [int]$Matches[2]
            $Lines.Add("                .RequirementSkill(SkillType.$skill, $rank)") | Out-Null
        }
    }

    if ($Row.CharacterType -eq "Standard") {
        $Lines.Add("                .RequirementCharacterType(CharacterType.Standard)") | Out-Null
    }
    elseif ($Row.CharacterType -eq "Force") {
        $Lines.Add("                .RequirementCharacterType(CharacterType.ForceSensitive)") | Out-Null
    }
}

function Write-PerkDefinitions {
    param([object[]]$Rows)
    $rootDir = Resolve-RepoPath "SWLOR.Game.Server\Feature\PerkDefinition"
    New-Item -ItemType Directory -Force -Path $rootDir | Out-Null

    foreach ($role in @("Balanced", "Bruiser", "Damage", "Evasion", "Force", "Tank")) {
        $stalePath = Join-Path $rootDir "BeastMastery$($role)PerkDefinition.cs"
        if ([System.IO.File]::Exists($stalePath)) {
            [System.IO.File]::Delete($stalePath)
        }
    }

    foreach ($styleGroup in ($Rows | Group-Object { "$($_.Tab)|$($_.Style)" } | Sort-Object { [int]$_.Group[0].Row })) {
        $firstStyleRow = $styleGroup.Group[0]
        $isBeastRole = $firstStyleRow.Tab -eq "Beast Mastery"
        $dir = if ($isBeastRole) {
            Join-Path $rootDir "Beast"
        }
        else {
            $rootDir
        }
        New-Item -ItemType Directory -Force -Path $dir | Out-Null

        $className = if ($isBeastRole) {
            "Beast$(ConvertTo-Identifier $firstStyleRow.Style)PerkDefinition"
        }
        else {
            "$(ConvertTo-Identifier "$($firstStyleRow.Tab) $($firstStyleRow.Style)")PerkDefinition"
        }
        $path = Join-Path $dir "$className.cs"
        $namespace = if ($isBeastRole) {
            "SWLOR.Game.Server.Feature.PerkDefinition.Beast"
        }
        else {
            "SWLOR.Game.Server.Feature.PerkDefinition"
        }
        $groups = $styleGroup.Group | Group-Object BaseIdentifier | Sort-Object { [int]$_.Group[0].Row }
        $lines = [System.Collections.Generic.List[string]]::new()
        $lines.AddRange([string[]]@(
            "using System.Collections.Generic;",
            "using SWLOR.Game.Server.Enumeration;",
            "using SWLOR.Game.Server.Service.BeastMasteryService;",
            "using SWLOR.Game.Server.Service.PerkService;",
            "using SWLOR.Game.Server.Service.SkillService;",
            "using SWLOR.Game.Server.Service.StatService;",
            "using SWLOR.NWN.API.NWScript.Enum;",
            "",
            "namespace $namespace",
            "{",
            "    public sealed class $className : IPerkListDefinition",
            "    {",
            "        private readonly PerkBuilder _builder = new();",
            "",
            "        public Dictionary<PerkType, PerkDetail> BuildPerks()",
            "        {"
        ))
        foreach ($group in $groups) {
            $lines.Add("            $($group.Name)();") | Out-Null
        }
        $lines.AddRange([string[]]@(
            "",
            "            return _builder.Build();",
            "        }",
            ""
        ))

        foreach ($group in $groups) {
            $first = $group.Group[0]
            $lines.Add("        private void $($group.Name)()") | Out-Null
            $lines.Add("        {") | Out-Null
            $lines.Add("            _builder.Create(PerkCategoryType.$(Get-Category $first), PerkType.$($group.Name))") | Out-Null
            $lines.Add("                .Name(""$(ConvertTo-CSharpString $first.BaseName)"")") | Out-Null
            if ($first.Tab -eq "Beast Mastery") {
                $lines.Add("                .GroupType(PerkGroupType.Beast)") | Out-Null
            }

            $orderedRows = @($group.Group | Sort-Object { [int]$_.Row })
            for ($i = 0; $i -lt $orderedRows.Count; $i++) {
                $row = $orderedRows[$i]
                $levelLines = [System.Collections.Generic.List[string]]::new()
                $levelLines.Add("") | Out-Null
                $levelLines.Add("                .AddPerkLevel()") | Out-Null
                $levelLines.Add("                .Description(""$(ConvertTo-CSharpString $row.Description)"")") | Out-Null
                $levelLines.Add("                .Price($(ConvertTo-Int $row.Price))") | Out-Null
                Add-RequirementLines $levelLines $row
                if ($ActiveTypes -contains $row.Type) {
                    $levelLines.Add("                .GrantsFeat(FeatType.$($row.FeatIdentifier))") | Out-Null
                }
                elseif ($row.Type -eq "Trait") {
                    foreach ($statLine in (Get-TraitStatLines $row.Description)) {
                        $levelLines.Add($statLine) | Out-Null
                    }
                }

                for ($j = 0; $j -lt $levelLines.Count; $j++) {
                    $line = $levelLines[$j]
                    $isLastOverall = $i -eq $orderedRows.Count - 1 -and $j -eq $levelLines.Count - 1
                    if ($isLastOverall) {
                        $line = "$line;"
                    }
                    $lines.Add($line) | Out-Null
                }
            }
            $lines.Add("        }") | Out-Null
            $lines.Add("") | Out-Null
        }

        $lines.Add("    }") | Out-Null
        $lines.Add("}") | Out-Null
        [System.IO.File]::WriteAllLines($path, $lines)
    }
}

function Get-FirstPercent {
    param([string]$Text)
    if ($Text -match "([+-]?[0-9]+)%") { return [Math]::Abs([int]$Matches[1]) }
    return 0
}

function Get-PercentNear {
    param([string]$Text, [string]$Hint)
    $lower = $Text.ToLowerInvariant()
    $index = $lower.IndexOf($Hint.ToLowerInvariant())
    if ($index -lt 0) { return 0 }
    $start = [Math]::Max(0, $index - 60)
    $length = [Math]::Min($Text.Length - $start, 140)
    $segment = $Text.Substring($start, $length)
    $matches = [regex]::Matches($segment, "([+-]?[0-9]+)%")
    if ($matches.Count -gt 0) {
        $hintIndex = $index - $start
        $nearest = $matches |
            Sort-Object { [Math]::Abs($_.Index - $hintIndex) } |
            Select-Object -First 1
        return [Math]::Abs([int]$nearest.Groups[1].Value)
    }
    return 0
}

function Get-DurationSecondsFromDescription {
    param([string]$Description)
    if ($Description -match "(?i)(?:for|over|lasts?)\s+([0-9]+)\s+seconds?") { return [int]$Matches[1] }
    if ($Description -match "(?i)(?:for|over|lasts?)\s+([0-9]+)\s+minutes?") { return [int]$Matches[1] * 60 }
    if ($Description -match "(?i)30 minutes") { return 1800 }
    if ($Description -match "(?i)5 minutes") { return 300 }
    if ($Description -match "(?i)3 minutes") { return 180 }
    if ($Description -match "(?i)2 minutes") { return 120 }
    return 12
}

function Get-DamageType {
    param($Row)
    $text = $Row.Description.ToLowerInvariant()
    if ($text -match "fire|flame|incendiary|thermal") { return "Fire" }
    if ($text -match "poison|toxin|venom") { return "Poison" }
    if ($text -match "ice|cryo|cold") { return "Ice" }
    if ($text -match "shock|\bion\b|lightning|electrical") { return "Electrical" }
    if ($Row.Tab -eq "Force" -or $text.Contains("force")) { return "Force" }
    return "Physical"
}

function Get-AreaShape {
    param($Row)
    $text = $Row.Description.ToLowerInvariant()
    if ($text -match "cone|breath|spray|flamethrower") { return "Cone" }
    if ($text -match "line") { return "Line" }
    return "Sphere"
}

function Get-BaseDamage {
    param($Row)
    if (!(Test-IsHostile $Row)) { return 0 }
    $text = $Row.Description
    if ($text -match "(?i)([0-9]+)\s*(?:[A-Za-z]+\s+){0,3}DMG") { return [int]$Matches[1] }
    if ($text -match "(?i)marks one enemy|marked target|party members deal|target takes [0-9]+% more damage|less .*damage|damage reduction") { return 0 }
    if ($text -notmatch "(?i)dmg|deal[s]?\s+[0-9]+|strike|grenade|detonator|rocket|bite|claw|slam|breath|rend|spark|lightning|drain|choke|grip|flame|dart|burst|barrage|pulse|beacon") {
        return 0
    }
    $damage = 8 + ([int]$Row.LevelNumber * 4)
    if (Test-IsArea $Row) { $damage = [Math]::Max(6, $damage - 2) }
    if ($Row.CooldownTime -match "minutes") { $damage += 12 }
    return $damage
}

function Get-AreaRadius {
    param($Row, [string]$Shape)
    if ($Shape -ne "Sphere") {
        if ($Shape -eq "Line") { return "8f" }
        return "6f"
    }

    if ($Row.Description -match "(?i)within\s+([0-9]+)m") {
        return "$($Matches[1])f"
    }

    return "5f"
}

function Get-HealPercent {
    param($Row)
    $text = $Row.Description
    if ($text -notmatch "(?i)hp|healing|revive|revives|regeneration") { return 0 }
    if ($text -match "(?i)1 HP") { return 0 }
    if ($text -match "(?i)(costs?|sacrifice[s]?)\s+[^.]*HP") { return 0 }
    return Get-FirstPercent $text
}

function Get-TemporaryHPPercent {
    param($Row)
    if ($Row.Description -notmatch "(?i)temporary HP") { return 0 }
    $near = Get-PercentNear $Row.Description "temporary HP"
    if ($near -gt 0) { return $near }
    $first = Get-FirstPercent $Row.Description
    if ($first -gt 0) { return $first }
    return 15
}

function Get-ResourcePercent {
    param($Row, [string]$Resource)
    $description = $Row.Description
    $resourcePattern = if ($Resource -eq "STM") { "(?:STM|stamina)" } else { "FP" }
    if ($description -notmatch "(?i)restore") { return 0 }

    if ($description -match "(?i)restore[s]?\s+([0-9]+)%\s+(?:of\s+(?:maximum\s+)?)?$resourcePattern") {
        return [int]$Matches[1]
    }
    if ($description -match "(?i)restore[s]?.*?([0-9]+)%\s+(?:of\s+(?:maximum\s+)?)?$resourcePattern") {
        return [int]$Matches[1]
    }
    if ($description -match "(?i)restore[s]?\s+([0-9]+)%") {
        $afterRestore = $description.Substring($description.IndexOf($Matches[0], [System.StringComparison]::OrdinalIgnoreCase))
        if ($afterRestore -match "(?i)$resourcePattern") { return [int]$Matches[1] }
    }
    return 0
}

function Get-ResourceFlat {
    param($Row, [string]$Resource)
    $description = $Row.Description
    $resourcePattern = if ($Resource -eq "STM") { "(?:STM|stamina)" } else { "FP" }

    if ($description -match "(?i)restore[s]?\s+([0-9]+)\s+$resourcePattern\s+every\s+([0-9]+)\s+seconds?\s+for\s+([0-9]+)\s+seconds?") {
        $amount = [int]$Matches[1]
        $interval = [int]$Matches[2]
        $duration = [int]$Matches[3]
        if ($interval -le 0) { return $amount }
        return $amount * [Math]::Max(1, [int][Math]::Floor($duration / $interval))
    }

    if ($description -match "(?i)restore[s]?\s+([0-9]+)\s+$resourcePattern") {
        return [int]$Matches[1]
    }

    return 0
}

function Add-MapValue {
    param([hashtable]$Map, [string]$Key, [int]$Value)
    if ($Value -eq 0) { return }
    if ($Map.ContainsKey($Key)) { $Map[$Key] += $Value } else { $Map[$Key] = $Value }
}

function Get-SupportStats {
    param($Row)
    $map = @{}
    $text = $Row.Description.ToLowerInvariant()
    if ($text -match "deal|deals|damage dealt|outgoing damage|more damage|increases damage") {
        Add-MapValue $map "AttackPercentAdjustment" (Get-PercentNear $Row.Description "damage")
    }
    Add-MapValue $map "AccuracyPercentAdjustment" (Get-PercentNear $Row.Description "accuracy")
    Add-MapValue $map "AccuracyPercentAdjustment" (Get-PercentNear $Row.Description "hit chance")
    if ($text -match "critical damage") {
        $criticalDamage = if ($Row.Description -match "(?i)critical damage by\s+([0-9]+)%") { [int]$Matches[1] } else { Get-PercentNear $Row.Description "critical" }
        Add-MapValue $map "CriticalDamagePercentAdjustment" $criticalDamage
    }
    elseif ($text -match "critical") {
        Add-MapValue $map "CriticalRatePercentAdjustment" (Get-PercentNear $Row.Description "critical")
    }
    if ($text -match "shield deflection") {
        Add-MapValue $map "ShieldDeflection" (Get-PercentNear $Row.Description "deflection")
    }
    elseif ($text -match "attack deflection|\bdeflection\b") {
        Add-MapValue $map "AttackDeflection" (Get-PercentNear $Row.Description "deflection")
    }
    if ($text -match "guard chance") {
        Add-MapValue $map "Guard" (Get-PercentNear $Row.Description "guard chance")
    }
    Add-MapValue $map "MovementSpeedPercentAdjustment" (Get-PercentNear $Row.Description "haste")
    Add-MapValue $map "MovementSpeedPercentAdjustment" (Get-PercentNear $Row.Description "movement speed")
    Add-MapValue $map "EnmityPercentAdjustment" (Get-PercentNear $Row.Description "enmity")
    if ($text -match "increases damage taken") {
        Add-MapValue $map "DamageTakenPercentAdjustment" (Get-PercentNear $Row.Description "damage taken")
    }
    elseif ($text -match "damage reduction|less .*damage|reduces .*damage taken|damage taken|absorb|redirect") {
        Add-MapValue $map "DamageTakenPercentAdjustment" (-1 * (Get-FirstPercent $Row.Description))
    }
    if ($text -match "evasion" -and $text -notmatch "target|enemy|enemies|suffer") {
        Add-MapValue $map "EvasionPercentAdjustment" (Get-PercentNear $Row.Description "evasion")
    }
    if ($text -match "mind|fear|confusion|daze") {
        $resistance = if ($text -match "immune|immunity") { 100 } else { Get-FirstPercent $Row.Description }
        Add-MapValue $map "MindResistance" $resistance
    }
    if ($text -match "mobility|knockdown|forced movement") {
        $resistance = if ($text -match "immune|immunity") { 100 } else { Get-FirstPercent $Row.Description }
        Add-MapValue $map "MobilityResistance" $resistance
    }
    if ($text -match "bleed") {
        $resistance = if ($text -match "immune|immunity") { 100 } else { Get-PercentNear $Row.Description "bleed" }
        Add-MapValue $map "TraumaResistance" $resistance
    }
    if ($text -match "language|speech") {
        $ranks = 0
        if ($Row.Description -match "(?i)([0-9]+)\s+additional ranks") {
            $ranks = [int]$Matches[1]
        }
        Add-MapValue $map "LanguageComprehension" $ranks
    }
    return $map
}

function Get-HostileStats {
    param($Row)
    $map = @{}
    $text = $Row.Description.ToLowerInvariant()
    if ($text -match "party members deal|target takes|marked target|increase .*damage taken|damage taken by") {
        Add-MapValue $map "DamageTakenPercentAdjustment" (Get-FirstPercent $Row.Description)
    }
    if ($text -match "outgoing .*damage|damage dealt") {
        Add-MapValue $map "AttackPercentAdjustment" (-1 * (Get-PercentNear $Row.Description "damage"))
    }
    if ($text -match "hit chance|accuracy") {
        Add-MapValue $map "AccuracyPercentAdjustment" (-1 * (Get-PercentNear $Row.Description "hit chance"))
        Add-MapValue $map "AccuracyPercentAdjustment" (-1 * (Get-PercentNear $Row.Description "accuracy"))
        if ($Row.Description -match "(?i)-\s*([0-9]+)\s+Accuracy") {
            Add-MapValue $map "Accuracy" (-1 * [int]$Matches[1])
        }
    }
    if ($text -match "evasion") {
        Add-MapValue $map "EvasionPercentAdjustment" (-1 * (Get-PercentNear $Row.Description "evasion"))
        if ($Row.Description -match "(?i)-\s*([0-9]+)\s+Evasion") {
            Add-MapValue $map "Evasion" (-1 * [int]$Matches[1])
        }
    }
    if ($text -match "ability costs|fp.*cost|fp and stm") {
        Add-MapValue $map "FPCostPercentAdjustment" (Get-FirstPercent $Row.Description)
    }
    return $map
}

function Get-SelfStats {
    param($Row)
    $map = @{}
    $text = $Row.Description.ToLowerInvariant()

    if ($text -match "the beast gains|beast gains|\bgains\s+\+") {
        Add-MapValue $map "MovementSpeedPercentAdjustment" (Get-PercentNear $Row.Description "haste")
        Add-MapValue $map "MovementSpeedPercentAdjustment" (Get-PercentNear $Row.Description "movement speed")
        Add-MapValue $map "AccuracyPercentAdjustment" (Get-PercentNear $Row.Description "hit chance")
        if ($text -match "critical") {
            Add-MapValue $map "CriticalRatePercentAdjustment" (Get-PercentNear $Row.Description "critical")
        }
        Add-MapValue $map "EvasionPercentAdjustment" (Get-PercentNear $Row.Description "evasion")
        Add-MapValue $map "EnmityPercentAdjustment" (Get-PercentNear $Row.Description "enmity")
    }

    if ($text -match "the beast takes|beast takes|beast to take|beast gains .*damage reduction|beast and its master take") {
        Add-MapValue $map "DamageTakenPercentAdjustment" (-1 * (Get-FirstPercent $Row.Description))
    }

    return $map
}

function ConvertTo-StatDictionaryCode {
    param([hashtable]$Map)
    if ($Map.Count -le 0) { return "new Dictionary<StatType, int>()" }
    $entries = $Map.GetEnumerator() | Sort-Object Name | ForEach-Object { "[StatType.$($_.Name)] = $($_.Value)" }
    return "new Dictionary<StatType, int> { $($entries -join ', ') }"
}

function Test-HasStats {
    param([hashtable]$Map)
    if ($null -eq $Map) { return $false }
    return @($Map.GetEnumerator() | Where-Object { $_.Value -ne 0 }).Count -gt 0
}

function ConvertTo-StatusArrayCode {
    param([string[]]$Statuses)
    $Statuses = @($Statuses | Where-Object { ![string]::IsNullOrWhiteSpace($_) })
    if ($Statuses.Count -le 0) { return "Array.Empty<Type>()" }
    return "new[] { $(($Statuses | ForEach-Object { "typeof($_)" }) -join ', ') }"
}

function Get-PrimaryStatusCode {
    param([string[]]$Statuses)
    $Statuses = @($Statuses | Where-Object { ![string]::IsNullOrWhiteSpace($_) })
    if ($Statuses.Count -le 0) { return "null" }
    return "typeof($($Statuses[0]))"
}

function Get-CleanseCode {
    param($Row)
    if ($Row.Description -match "(?i)all|major|any resistance group") {
        return "StatusEffectCleanseType.Purify | StatusEffectCleanseType.TreatmentKit1 | StatusEffectCleanseType.TreatmentKit2"
    }
    if ($Row.PerkName -match "Treatment Kit I$") {
        return "StatusEffectCleanseType.TreatmentKit1 | StatusEffectCleanseType.Purify"
    }
    return "StatusEffectCleanseType.Purify | StatusEffectCleanseType.TreatmentKit1 | StatusEffectCleanseType.TreatmentKit2"
}

function Get-AbilityDefinitionFolder {
    param([string]$Tab)
    switch ($Tab) {
        "Beast Mastery" { return "Beastmaster" }
        "Devices" { return "Devices" }
        "First Aid" { return "FirstAid" }
        "Force" { return "Force" }
        "Leadership" { return "Leadership" }
        default { throw "No ability definition folder mapped for '$Tab'." }
    }
}

function Get-GeneratedStatusEffectName {
    param($Row)
    return "$($Row.FeatIdentifier)StatusEffect"
}

function Get-GeneratedSelfStatusEffectName {
    param($Row)
    return "$($Row.FeatIdentifier)SelfStatusEffect"
}

function Get-TargetVisualEffect {
    param($Row, [string]$DamageType, [string[]]$Statuses)
    $text = $Row.Description.ToLowerInvariant()
    $statusText = ($Statuses -join " ").ToLowerInvariant()

    if ($DamageType -eq "Fire" -or $statusText.Contains("burn")) { return "VisualEffect.Vfx_Com_Hit_Fire" }
    if ($DamageType -eq "Ice" -or $statusText.Contains("freezing")) { return "VisualEffect.Vfx_Com_Hit_Frost" }
    if ($DamageType -eq "Electrical" -or $statusText.Contains("shock")) { return "VisualEffect.Vfx_Com_Hit_Electrical" }
    if ($DamageType -eq "Poison" -or $statusText.Contains("poison") -or $statusText.Contains("toxin")) { return "VisualEffect.Vfx_Imp_Poison_S" }
    if ($text -match "sonic|concussion|flash|sound|howl") { return "VisualEffect.Vfx_Imp_Sonic" }
    if ($text -match "mind|terror|fear|morale|resolve") { return "VisualEffect.Vfx_Imp_Head_Mind" }
    if ($DamageType -eq "Force" -or $text -match "dark|drain|choke|grip|erosion") { return "VisualEffect.Vfx_Imp_Pulse_Negative" }
    if ($text -match "bleed|rend|bite|claw") { return "VisualEffect.Vfx_Com_Blood_Crt_Red" }
    if ($text -match "sunder|weaken|exposed|breach") { return "VisualEffect.Vfx_Imp_Breach" }
    if ($text -match "grenade|detonator|rocket|charge|barrage|slam") { return "VisualEffect.Vfx_Imp_Dust_Explosion" }
    return "VisualEffect.Vfx_Com_Chunk_Red_Small"
}

function Get-AreaVisualEffect {
    param($Row, [string]$DamageType, [string[]]$Statuses)
    $text = $Row.Description.ToLowerInvariant()
    $statusText = ($Statuses -join " ").ToLowerInvariant()

    if ($DamageType -eq "Fire" -or $statusText.Contains("burn")) { return "VisualEffect.Fnf_Fireball" }
    if ($DamageType -eq "Ice" -or $statusText.Contains("freezing")) { return "VisualEffect.Vfx_Fnf_Icestorm" }
    if ($DamageType -eq "Electrical" -or $statusText.Contains("shock")) { return "VisualEffect.Vfx_Fnf_Electric_Explosion" }
    if ($DamageType -eq "Poison" -or $statusText.Contains("poison") -or $statusText.Contains("toxin")) { return "VisualEffect.Vfx_Fnf_Gas_Explosion_Acid" }
    if ($text -match "sonic|concussion|flash|sound|howl") { return "VisualEffect.Vfx_Fnf_Sound_Burst" }
    if ($text -match "mind|terror|fear|morale|resolve") { return "VisualEffect.Vfx_Fnf_Mass_Mind_Affecting" }
    if ($DamageType -eq "Force" -or $text -match "dark|drain|choke|grip|erosion") { return "VisualEffect.Vfx_Fnf_Howl_Mind" }
    if ($text -match "grenade|detonator|rocket|charge|barrage|slam") { return "VisualEffect.Vfx_Fnf_Screen_Shake" }
    return "VisualEffect.None"
}

function Get-SupportVisualEffect {
    param($Row, [hashtable]$Stats, [int]$HealPercent, [int]$TemporaryHPPercent, [int]$StaminaPercent, [int]$FPPercent)
    $text = $Row.Description.ToLowerInvariant()

    if ($HealPercent -gt 0) { return "VisualEffect.Vfx_Imp_Healing_M" }
    if ($TemporaryHPPercent -gt 0 -or $text -match "shield|guard|ward|barrier|deflector|screen") { return "VisualEffect.Vfx_Imp_Ac_Bonus" }
    if ($StaminaPercent -gt 0 -or $FPPercent -gt 0 -or $text -match "restore") { return "VisualEffect.Vfx_Imp_Restoration" }
    if ($text -match "haste|hasten|speed|evasion") { return "VisualEffect.Vfx_Imp_Haste" }
    if (Test-HasStats $Stats) { return "VisualEffect.Vfx_Imp_Holy_Aid" }
    return "VisualEffect.None"
}

function Get-StatusEffectIcon {
    param($Row, [hashtable]$Stats, [bool]$IsHostile)
    $keys = @($Stats.Keys)
    $values = @($Stats.Values)

    if ($IsHostile) {
        if ($keys -contains "DamageTakenPercentAdjustment" -or $keys -contains "DamageTakenFlatAdjustment") { return "EffectIconType.DamageIncrease" }
        if ($keys -contains "AttackPercentAdjustment") { return "EffectIconType.DamageDecrease" }
        if ($keys -contains "EvasionPercentAdjustment") { return "EffectIconType.MovementSpeedDecrease" }
        return "EffectIconType.AttackDecrease"
    }

    if ($keys -contains "DamageTakenPercentAdjustment") { return "EffectIconType.DamageReduction" }
    if (@($keys | Where-Object { $_ -match "Resistance$" }).Count -gt 0) {
        if (@($values | Where-Object { $_ -ge 100 }).Count -gt 0) { return "EffectIconType.Immunity" }
        return "EffectIconType.SpellResistanceIncrease"
    }
    if ($keys -contains "AttackDeflection" -or $keys -contains "ShieldDeflection" -or $keys -contains "Guard" -or $keys -contains "EvasionPercentAdjustment") { return "EffectIconType.DamageResistance" }
    if ($keys -contains "CriticalRatePercentAdjustment" -or $keys -contains "AttackPercentAdjustment") { return "EffectIconType.DamageIncrease" }
    return "EffectIconType.AttackIncrease"
}

function Get-StatusEffectResistance {
    param($Row, [string]$DamageType)
    $text = $Row.Description.ToLowerInvariant()

    switch ($DamageType) {
        "Fire" { return "ResistanceType.Fire" }
        "Poison" { return "ResistanceType.Poison" }
        "Electrical" { return "ResistanceType.Electrical" }
        "Ice" { return "ResistanceType.Ice" }
        "Force" { return "ResistanceType.Disruption" }
    }

    if ($text -match "mind|fear|confusion|daze|morale|resolve") { return "ResistanceType.Mind" }
    if ($text -match "mobility|knockdown|hobble|hamstring") { return "ResistanceType.Mobility" }
    return "ResistanceType.Trauma"
}

function ConvertTo-StatusStatLines {
    param([hashtable]$Map)
    return @(
        $Map.GetEnumerator() |
            Where-Object { $_.Value -ne 0 } |
            Sort-Object Name |
            ForEach-Object { "            StatGroup.Stats[StatType.$($_.Name)] = $($_.Value);" }
    )
}

function Write-CombatPerkStatusEffectDefinitions {
    param([object[]]$Rows)
    $dir = Resolve-RepoPath "SWLOR.Game.Server\Feature\StatusEffectDefinition"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null

    $statusInfos = New-Object System.Collections.Generic.List[object]

    foreach ($row in ($Rows | Where-Object { $ActiveTypes -contains $_.Type } | Sort-Object { [int]$_.Row })) {
        $isHostile = Test-IsHostile $row
        $stats = if ($isHostile) { Get-HostileStats $row } else { Get-SupportStats $row }
        if (Test-HasStats $stats) {
            $stackKind = if ($isHostile) { "Target" } else { "Support" }
            $statusInfos.Add([pscustomobject]@{
                Row = $row
                ClassName = Get-GeneratedStatusEffectName $row
                Stats = $stats
                IsHostile = $isHostile
                StackGroup = "$($row.BaseIdentifier)|$stackKind"
            }) | Out-Null
        }

        if ($isHostile) {
            $selfStats = Get-SelfStats $row
            if (Test-HasStats $selfStats) {
                $statusInfos.Add([pscustomobject]@{
                    Row = $row
                    ClassName = Get-GeneratedSelfStatusEffectName $row
                    Stats = $selfStats
                    IsHostile = $false
                    StackGroup = "$($row.BaseIdentifier)|Self"
                }) | Out-Null
            }
        }
    }

    foreach ($statusInfo in ($statusInfos | Sort-Object { [int]$_.Row.Row }, ClassName)) {
        $row = $statusInfo.Row
        $stats = $statusInfo.Stats
        $isHostile = [bool]$statusInfo.IsHostile
        $className = $statusInfo.ClassName
        $damageType = Get-DamageType $row
        $path = Join-Path $dir "$className.cs"
        $sameGroup = @($statusInfos |
            Where-Object { $_.StackGroup -eq $statusInfo.StackGroup -and $_.ClassName -ne $className } |
            Sort-Object { [int]$_.Row.LevelNumber })
        $morePowerful = @($sameGroup | Where-Object { [int]$_.Row.LevelNumber -gt [int]$row.LevelNumber } | ForEach-Object { $_.ClassName })
        $lessPowerful = @($sameGroup | Where-Object { [int]$_.Row.LevelNumber -lt [int]$row.LevelNumber } | ForEach-Object { $_.ClassName })
        $lines = [System.Collections.Generic.List[string]]::new()
        $lines.AddRange([string[]]@(
            "using System.Collections.Generic;",
            "using SWLOR.Game.Server.Service.CombatService;",
            "using SWLOR.Game.Server.Service.StatService;",
            "using SWLOR.Game.Server.Service.StatusEffectService;",
            "using SWLOR.NWN.API.NWScript.Enum;",
            "",
            "namespace SWLOR.Game.Server.Feature.StatusEffectDefinition",
            "{",
            "    public sealed class $className : StatusEffectBase",
            "    {",
            "        public override string Name => ""$(ConvertTo-CSharpString $row.PerkName)"";",
            "        public override EffectIconType Icon => $(Get-StatusEffectIcon $row $stats $isHostile);"
        ))
        if ($isHostile) {
            $lines.Add("        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;") | Out-Null
            $lines.Add("        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;") | Out-Null
            $lines.Add("        public override ResistanceType ResistanceType => $(Get-StatusEffectResistance $row $damageType);") | Out-Null
        }
        $lines.Add("        public override bool PersistsOnLogout => false;") | Out-Null
        if ($morePowerful.Count -gt 0) {
            $lines.Add("        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>") | Out-Null
            $lines.Add("        {") | Out-Null
            foreach ($typeName in $morePowerful) {
                $lines.Add("            typeof($typeName),") | Out-Null
            }
            $lines.Add("        };") | Out-Null
        }
        if ($lessPowerful.Count -gt 0) {
            $lines.Add("        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>") | Out-Null
            $lines.Add("        {") | Out-Null
            foreach ($typeName in $lessPowerful) {
                $lines.Add("            typeof($typeName),") | Out-Null
            }
            $lines.Add("        };") | Out-Null
        }
        $lines.Add("") | Out-Null
        $lines.Add("        public $className()") | Out-Null
        $lines.Add("        {") | Out-Null
        foreach ($statLine in (ConvertTo-StatusStatLines $stats)) {
            $lines.Add($statLine) | Out-Null
        }
        $lines.Add("        }") | Out-Null
        $lines.Add("    }") | Out-Null
        $lines.Add("}") | Out-Null
        [System.IO.File]::WriteAllLines($path, $lines)
    }
}

function Add-BuilderChain {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        $Row,
        [bool]$IsHostile,
        [bool]$IsArea,
        [bool]$RequiresTarget,
        [float]$Cast,
        [float]$Cooldown,
        [int]$FP,
        [int]$STM,
        [string]$Skill,
        [string]$ImpactActionName
    )

    $calls = [System.Collections.Generic.List[string]]::new()
    $calls.Add(".Create(FeatType.$($Row.FeatIdentifier), PerkType.$($Row.BaseIdentifier))") | Out-Null
    $calls.Add(".Name(""$(ConvertTo-CSharpString $Row.PerkName)"")") | Out-Null
    $calls.Add(".Level($($Row.LevelNumber))") | Out-Null
    $calls.Add(".HasActivationDelay(${Cast}f)") | Out-Null
    if ($Cooldown -gt 0) {
        $calls.Add(".HasRecastDelay(RecastGroup.$($Row.BaseIdentifier), ${Cooldown}f)") | Out-Null
    }
    $calls.Add(".SkillType(SkillType.$Skill)") | Out-Null
    if ($IsArea) {
        $calls.Add(".IsAreaAbility()") | Out-Null
    }
    else {
        $calls.Add(".IsSingleTargetAbility()") | Out-Null
    }
    if ($RequiresTarget) {
        $calls.Add(".RequiresTarget()") | Out-Null
    }
    if ($RequiresTarget -and !$IsHostile -and !$IsArea) {
        $calls.Add(".HasCustomValidation((activator, target, _, _) => SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.ValidateFriendlyTarget(activator, target))") | Out-Null
    }
    $calls.Add(".HasImpactAction($ImpactActionName)") | Out-Null
    $calls.Add(".IsCastedAbility()") | Out-Null
    if ($IsHostile) {
        $calls.Add(".IsHostileAbility()") | Out-Null
    }
    if (Test-TriggersDarkForceConversion $Row) {
        $calls.Add(".TriggersDarkForceConversion()") | Out-Null
    }
    $calls.Add(".BreaksStealth()") | Out-Null
    if ($FP -gt 0) {
        $calls.Add(".RequirementFP($FP)") | Out-Null
    }
    if ($STM -gt 0) {
        $calls.Add(".RequirementStamina($STM)") | Out-Null
    }

    $Lines.Add("            builder") | Out-Null
    for ($i = 0; $i -lt $calls.Count; $i++) {
        $suffix = if ($i -eq $calls.Count - 1) { ";" } else { "" }
        $Lines.Add("                $($calls[$i])$suffix") | Out-Null
    }
}

function Add-HostileImpactCase {
    param([System.Collections.Generic.List[string]]$Lines, $Row, [hashtable]$HostileStats)
    $skill = Get-SkillType $Row.Tab
    $statuses = @(Get-StatusEffects $Row.Description | Where-Object { ![string]::IsNullOrWhiteSpace($_) })
    if (Test-HasStats $HostileStats) {
        $statuses += (Get-GeneratedStatusEffectName $Row)
    }

    $primaryStatus = Get-PrimaryStatusCode $statuses
    $additionalStatuses = if ($statuses.Count -gt 1) {
        ConvertTo-StatusArrayCode @($statuses | Select-Object -Skip 1)
    }
    else {
        "Array.Empty<Type>()"
    }
    $shape = Get-AreaShape $Row
    $damageType = Get-DamageType $Row
    $baseDamage = Get-BaseDamage $Row
    $duration = Get-DurationSecondsFromDescription $Row.Description
    $radius = Get-AreaRadius $Row $shape
    $width = if ($shape -eq "Sphere") { "0f" } elseif ($shape -eq "Line") { "2.5f" } else { "5f" }
    $targetVisual = Get-TargetVisualEffect $Row $damageType $statuses
    $areaVisual = Get-AreaVisualEffect $Row $damageType $statuses
    $selfStats = Get-SelfStats $Row
    $tempHpPercent = Get-TemporaryHPPercent $Row
    $hasSelfImpact = (Test-HasStats $selfStats) -or $tempHpPercent -gt 0
    $isGoad = Test-IsGoad $Row
    $centerOnActivator = if (Test-IsSelfCenteredHostileArea $Row) { "true" } else { "!GetIsObjectValid(target)" }
    $hasCombatImpact = $baseDamage -gt 0 -or $primaryStatus -ne "null" -or $additionalStatuses -ne "Array.Empty<Type>()"

    if (Test-IsArea $Row) {
        if ($hasCombatImpact) {
            $Lines.AddRange([string[]]@(
                "                    Ability.ApplyTelegraphedCombatImpact(",
                "                        activator,",
                "                        target,",
                "                        targetLocation,",
                "                        SkillType.$skill,",
                "                        $baseDamage,",
                "                        $duration,",
                "                        $primaryStatus,",
                "                        CombatImpactAreaShape.$shape,",
                "                        0f,",
                "                        $radius,",
                "                        $width,",
                "                        $additionalStatuses,",
                "                        centerOnActivator: $centerOnActivator,",
                "                        damageType: CombatDamageType.$damageType,",
                "                        targetVisualEffect: $targetVisual,",
                "                        areaVisualEffect: $areaVisual);"
            ))
        }
        if ($isGoad) {
            if ($hasCombatImpact) {
                $Lines.Add("") | Out-Null
            }
            $Lines.AddRange([string[]]@(
                "                    foreach (var hostile in GetHostileTargets(activator, target, targetLocation, $centerOnActivator, $radius))",
                "                    {",
                "                        ApplyGoad(activator, hostile);",
                "                    }"
            ))
        }
        if ($hasSelfImpact) {
            $Lines.Add("") | Out-Null
        }
    }
    else {
        if ($hasCombatImpact) {
            $Lines.AddRange([string[]]@(
                "                    Ability.ApplyCombatImpact(",
                "                        activator,",
                "                        target,",
                "                        targetLocation,",
                "                        SkillType.$skill,",
                "                        $baseDamage,",
                "                        $duration,",
                "                        $primaryStatus,",
                "                        false,",
                "                        $additionalStatuses,",
                "                        damageType: CombatDamageType.$damageType,",
                "                        targetVisualEffect: $targetVisual);"
            ))
        }
        if ($isGoad) {
            if ($hasCombatImpact) {
                $Lines.Add("") | Out-Null
            }
            $Lines.Add("                    ApplyGoad(activator, target);") | Out-Null
        }
        if ($hasSelfImpact) {
            $Lines.Add("") | Out-Null
        }
    }

    if (Test-HasStats $selfStats) {
        $selfTargets = if (Test-AffectsBeastAndMaster $Row) { "GetBeastAndMasterTargets(activator)" } else { "new[] { activator }" }
        $Lines.AddRange([string[]]@(
            "                    foreach (var friendly in $selfTargets)",
            "                    {",
            "                        StatusEffect.ApplyStatusEffect(activator, friendly, typeof($(Get-GeneratedSelfStatusEffectName $Row)), ${duration}f);",
            "                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), friendly);",
            "                    }"
        ))
    }
    if ($tempHpPercent -gt 0) {
        $Lines.Add("                    ApplyTemporaryHP(activator, $tempHpPercent, ${duration}f);") | Out-Null
    }
}

function Add-CleanseImpactCase {
    param([System.Collections.Generic.List[string]]$Lines, $Row)
    $isParty = Test-IsParty $Row
    $cleanseCode = Get-CleanseCode $Row
    $statuses = @(Get-StatusEffects $Row.Description | Where-Object { ![string]::IsNullOrWhiteSpace($_) })
    $cleanseStatuses = ConvertTo-StatusArrayCode $statuses
    $Lines.AddRange([string[]]@(
        "                    foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, $($isParty.ToString().ToLowerInvariant())))",
        "                    {",
        "                        foreach (var statusEffect in $cleanseStatuses)",
        "                        {",
        "                            StatusEffect.RemoveStatusEffect(friendly, statusEffect, false);",
        "                        }",
        "",
        "                        StatusEffect.RemoveFirstCleanseableStatusEffect(friendly, $cleanseCode, false);",
        "                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);",
        "                    }"
    ))
}

function Add-ReviveImpactCase {
    param([System.Collections.Generic.List[string]]$Lines, $Row)
    $skill = Get-SkillType $Row.Tab
    $healPercent = Get-HealPercent $Row
    $tempHpPercent = Get-TemporaryHPPercent $Row
    $Lines.AddRange([string[]]@(
        "                    if (!GetIsObjectValid(target))",
        "                        return;",
        ""
    ))
    if ($healPercent -gt 0) {
        $Lines.Add("                    HealPercent(activator, target, SkillType.$skill, $healPercent);") | Out-Null
    }
    else {
        $Lines.Add("                    ApplyEffectToObject(DurationType.Instant, EffectHeal(1), target);") | Out-Null
    }
    if ($tempHpPercent -gt 0) {
        $Lines.Add("                    ApplyTemporaryHP(target, $tempHpPercent, 10f);") | Out-Null
    }
    $Lines.Add("                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Raise_Dead), target);") | Out-Null
}

function Add-SupportImpactCase {
    param([System.Collections.Generic.List[string]]$Lines, $Row, [hashtable]$SupportStats)
    $skill = Get-SkillType $Row.Tab
    $isParty = Test-IsParty $Row
    $duration = Get-DurationSecondsFromDescription $Row.Description
    $healPercent = Get-HealPercent $Row
    $tempHpPercent = Get-TemporaryHPPercent $Row
    $staminaPercent = Get-ResourcePercent $Row "STM"
    $fpPercent = Get-ResourcePercent $Row "FP"
    $staminaFlat = Get-ResourceFlat $Row "STM"
    $fpFlat = Get-ResourceFlat $Row "FP"
    $visual = Get-SupportVisualEffect $Row $SupportStats $healPercent $tempHpPercent $staminaPercent $fpPercent
    $targetExpression = if (Test-AffectsBeastAndMaster $Row) {
        "GetBeastAndMasterTargets(activator)"
    }
    elseif (Test-AffectsBeastMasterOnly $Row) {
        "GetBeastMasterTargets(activator)"
    }
    else {
        "SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, $($isParty.ToString().ToLowerInvariant()))"
    }

    if ($Row.BaseIdentifier -eq "EmergencyCocktail") {
        $Lines.AddRange([string[]]@(
            "                    foreach (var friendly in $targetExpression)",
            "                    {",
            "                        HealPercent(activator, friendly, SkillType.$skill, 25);",
            "                        StatusEffect.ApplyStatusEffect(activator, friendly, typeof(PainSuppressant2StatusEffect), ${duration}f);",
            "                        StatusEffect.RemoveFirstStatusEffect(friendly, new[] { typeof(PoisonStatusEffect), typeof(ToxinStatusEffect) }, false);",
            "                        StatusEffect.ApplyStatusEffect(activator, friendly, typeof(Antitoxin1StatusEffect), ${duration}f);",
            "                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), friendly);",
            "                    }"
        ))
        return
    }

    $Lines.AddRange([string[]]@(
        "                    foreach (var friendly in $targetExpression)",
        "                    {"
    ))
    if ($healPercent -gt 0) {
        $Lines.Add("                        HealPercent(activator, friendly, SkillType.$skill, $healPercent);") | Out-Null
    }
    if ($tempHpPercent -gt 0) {
        $Lines.Add("                        ApplyTemporaryHP(friendly, $tempHpPercent, ${duration}f);") | Out-Null
    }
    if ($staminaPercent -gt 0) {
        $Lines.Add("                        Stat.RestoreStamina(friendly, PercentOf(Stat.GetMaxStamina(friendly), $staminaPercent));") | Out-Null
    }
    if ($staminaFlat -gt 0) {
        $Lines.Add("                        Stat.RestoreStamina(friendly, $staminaFlat);") | Out-Null
    }
    if ($fpPercent -gt 0) {
        $Lines.Add("                        Stat.RestoreFP(friendly, PercentOf(Stat.GetMaxFP(friendly), $fpPercent));") | Out-Null
    }
    if ($fpFlat -gt 0) {
        $Lines.Add("                        Stat.RestoreFP(friendly, $fpFlat);") | Out-Null
    }
    if (Test-HasStats $SupportStats) {
        $Lines.Add("                        StatusEffect.ApplyStatusEffect(activator, friendly, typeof($(Get-GeneratedStatusEffectName $Row)), ${duration}f);") | Out-Null
    }
    if ($visual -ne "VisualEffect.None") {
        $Lines.Add("                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect($visual), friendly);") | Out-Null
    }
    $Lines.Add("                    }") | Out-Null
}

function Add-BeastTargetHelpers {
    param([System.Collections.Generic.List[string]]$Lines)
    $Lines.AddRange([string[]]@(
        "",
        "        private static IEnumerable<uint> GetBeastAndMasterTargets(uint activator)",
        "        {",
        "            yield return activator;",
        "",
        "            var master = GetMaster(activator);",
        "            if (GetIsObjectValid(master))",
        "                yield return master;",
        "        }",
        "",
        "        private static IEnumerable<uint> GetBeastMasterTargets(uint activator)",
        "        {",
        "            var master = GetMaster(activator);",
        "            yield return GetIsObjectValid(master) ? master : activator;",
        "        }"
    ))
}

function Add-HostileTargetHelper {
    param([System.Collections.Generic.List[string]]$Lines)
    $Lines.AddRange([string[]]@(
        "",
        "        private static IEnumerable<uint> GetHostileTargets(uint activator, uint target, Location targetLocation, bool centerOnActivator, float radius)",
        "        {",
        "            var location = centerOnActivator || !GetIsObjectValid(target)",
        "                ? GetLocation(activator)",
        "                : GetLocation(target);",
        "            if (!GetIsObjectValid(GetAreaFromLocation(location)) && GetIsObjectValid(GetAreaFromLocation(targetLocation)))",
        "                location = targetLocation;",
        "",
        "            var creature = GetFirstObjectInShape(Shape.Sphere, radius, location, true);",
        "            while (GetIsObjectValid(creature))",
        "            {",
        "                if (creature != activator && GetIsReactionTypeHostile(creature, activator))",
        "                    yield return creature;",
        "",
        "                creature = GetNextObjectInShape(Shape.Sphere, radius, location, true);",
        "            }",
        "        }"
    ))
}

function Add-GoadHelper {
    param([System.Collections.Generic.List[string]]$Lines)
    $Lines.AddRange([string[]]@(
        "",
        "        private static void ApplyGoad(uint activator, uint target)",
        "        {",
        "            if (!GetIsObjectValid(target) || !GetIsReactionTypeHostile(target, activator))",
        "                return;",
        "",
        "            var enmity = Stat.ScaleEffect(700, GetAbilityScore(activator, AbilityType.Vitality));",
        "            Enmity.ModifyEnmity(activator, target, enmity);",
        "            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Odd), target);",
        "        }"
    ))
}

function Add-HealHelper {
    param([System.Collections.Generic.List[string]]$Lines)
    $Lines.AddRange([string[]]@(
        "",
        "        private static void HealPercent(uint activator, uint target, SkillType skill, int percent)",
        "        {",
        "            var ability = skill == SkillType.Leadership",
        "                ? AbilityType.Social",
        "                : AbilityType.Willpower;",
        "            var baseAmount = PercentOf(GetMaxHitPoints(target), percent);",
        "            var amount = Stat.ScaleEffect(baseAmount, GetAbilityScore(activator, ability), 0.005f);",
        "",
        "            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);",
        "            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);",
        "        }"
    ))
}

function Add-TemporaryHPHelper {
    param([System.Collections.Generic.List[string]]$Lines)
    $Lines.AddRange([string[]]@(
        "",
        "        private static void ApplyTemporaryHP(uint target, int percent, float durationSeconds)",
        "        {",
        "            ApplyEffectToObject(",
        "                DurationType.Temporary,",
        "                EffectTemporaryHitpoints(PercentOf(GetMaxHitPoints(target), percent)),",
        "                target,",
        "                durationSeconds);",
        "        }"
    ))
}

function Add-PercentHelper {
    param([System.Collections.Generic.List[string]]$Lines)
    $Lines.AddRange([string[]]@(
        "",
        "        private static int PercentOf(int value, int percent)",
        "        {",
        "            return Math.Max(1, value * percent / 100);",
        "        }"
    ))
}

function Write-AbilityDefinitions {
    param([object[]]$Rows)

    foreach ($group in ($Rows | Where-Object { $ActiveTypes -contains $_.Type } | Group-Object BaseIdentifier | Sort-Object { [int]$_.Group[0].Row })) {
        $firstRow = $group.Group[0]
        $folderName = Get-AbilityDefinitionFolder $firstRow.Tab
        $dir = Resolve-RepoPath "SWLOR.Game.Server\Feature\AbilityDefinition\$folderName"
        New-Item -ItemType Directory -Force -Path $dir | Out-Null

        $className = "$($group.Name)AbilityDefinition"
        $path = Join-Path $dir "$className.cs"
        $lines = [System.Collections.Generic.List[string]]::new()
        $lines.AddRange([string[]]@(
            "using System;",
            "using System.Collections.Generic;",
            "using SWLOR.Game.Server.Feature.StatusEffectDefinition;",
            "using SWLOR.Game.Server.Service;",
            "using SWLOR.Game.Server.Service.AbilityService;",
            "using SWLOR.Game.Server.Service.CombatService;",
            "using SWLOR.Game.Server.Service.PerkService;",
            "using SWLOR.Game.Server.Service.SkillService;",
            "using SWLOR.Game.Server.Service.StatusEffectService;",
            "using SWLOR.NWN.API.Engine;",
            "using SWLOR.NWN.API.NWScript.Enum;",
            "using SWLOR.NWN.API.NWScript.Enum.Creature;",
            "using SWLOR.NWN.API.NWScript.Enum.VisualEffect;",
            "",
            "namespace SWLOR.Game.Server.Feature.AbilityDefinition.$folderName",
            "{",
            "    public sealed class $className : IAbilityListDefinition",
            "    {",
            "        public Dictionary<FeatType, AbilityDetail> BuildAbilities()",
            "        {"
        ))
        $lines.Add("            var builder = new AbilityBuilder();") | Out-Null
        $lines.Add("") | Out-Null
        foreach ($row in ($group.Group | Sort-Object { [int]$_.Row })) {
            $lines.Add("            $($row.FeatIdentifier)(builder);") | Out-Null
        }
        $lines.AddRange([string[]]@(
            "",
            "            return builder.Build();",
            "        }",
            ""
        ))

        $needsHealHelper = $false
        $needsTemporaryHPHelper = $false
        $needsPercentHelper = $false
        $needsBeastTargetHelpers = $false
        $needsHostileTargetHelper = $false
        $needsGoadHelper = $false

        foreach ($row in ($group.Group | Sort-Object { [int]$_.Row })) {
            $skill = Get-SkillType $row.Tab
            $cast = ConvertTo-Seconds $row.CastingTime
            $cooldown = ConvertTo-Seconds $row.CooldownTime
            $fp = ConvertTo-Int $row.FP
            $stm = ConvertTo-Int $row.STM
            $isHostile = Test-IsHostile $row
            $isArea = Test-IsArea $row
            $isCleanse = Test-IsCleanse $row.Description
            $isRevive = Test-IsRevive $row.Description
            $requiresTarget = Test-RequiresTarget $row
            $healPercent = Get-HealPercent $row
            $tempHpPercent = Get-TemporaryHPPercent $row
            $staminaPercent = Get-ResourcePercent $row "STM"
            $fpPercent = Get-ResourcePercent $row "FP"
            $supportStats = Get-SupportStats $row
            $selfStats = Get-SelfStats $row

            if ($isRevive -or $healPercent -gt 0 -or $row.BaseIdentifier -eq "EmergencyCocktail") {
                $needsHealHelper = $needsHealHelper -or ($healPercent -gt 0) -or ($row.BaseIdentifier -eq "EmergencyCocktail")
                $needsPercentHelper = $needsPercentHelper -or ($healPercent -gt 0) -or ($row.BaseIdentifier -eq "EmergencyCocktail")
            }
            if ($tempHpPercent -gt 0) {
                $needsTemporaryHPHelper = $true
                $needsPercentHelper = $true
            }
            if ($staminaPercent -gt 0 -or $fpPercent -gt 0) {
                $needsPercentHelper = $true
            }
            if ((Test-AffectsBeastAndMaster $row) -or (Test-AffectsBeastMasterOnly $row)) {
                $needsBeastTargetHelpers = $true
            }
            if ($isHostile -and (Test-HasStats $selfStats) -and (Test-AffectsBeastAndMaster $row)) {
                $needsBeastTargetHelpers = $true
            }
            if (Test-IsGoad $row) {
                $needsGoadHelper = $true
                if (Test-IsArea $row) {
                    $needsHostileTargetHelper = $true
                }
            }

            $impactActionName = "$($row.FeatIdentifier)ImpactAction"
            $lines.Add("        private static void $($row.FeatIdentifier)(AbilityBuilder builder)") | Out-Null
            $lines.Add("        {") | Out-Null
            Add-BuilderChain $lines $row $isHostile $isArea $requiresTarget $cast $cooldown $fp $stm $skill $impactActionName
            $lines.Add("        }") | Out-Null
            $lines.Add("") | Out-Null
        }

        foreach ($row in ($group.Group | Sort-Object { [int]$_.Row })) {
            $isHostile = Test-IsHostile $row
            $isCleanse = Test-IsCleanse $row.Description
            $isRevive = Test-IsRevive $row.Description
            $lines.Add("        private static void $($row.FeatIdentifier)ImpactAction(uint activator, uint target, int level, Location targetLocation)") | Out-Null
            $lines.Add("        {") | Out-Null
            $impactStartIndex = $lines.Count
            if ($isRevive) {
                Add-ReviveImpactCase $lines $row
            }
            elseif ($isCleanse) {
                Add-CleanseImpactCase $lines $row
            }
            elseif ($isHostile) {
                Add-HostileImpactCase $lines $row (Get-HostileStats $row)
            }
            else {
                Add-SupportImpactCase $lines $row (Get-SupportStats $row)
            }
            for ($lineIndex = $impactStartIndex; $lineIndex -lt $lines.Count; $lineIndex++) {
                $lines[$lineIndex] = $lines[$lineIndex] -replace "^                    ", "            "
            }
            $lines.Add("        }") | Out-Null
            $lines.Add("") | Out-Null
        }

        if ($needsBeastTargetHelpers) {
            Add-BeastTargetHelpers $lines
        }
        if ($needsHostileTargetHelper) {
            Add-HostileTargetHelper $lines
        }
        if ($needsGoadHelper) {
            Add-GoadHelper $lines
        }
        if ($needsHealHelper) {
            Add-HealHelper $lines
        }
        if ($needsTemporaryHPHelper) {
            Add-TemporaryHPHelper $lines
        }
        if ($needsPercentHelper) {
            Add-PercentHelper $lines
        }

        $lines.Add("    }") | Out-Null
        $lines.Add("}") | Out-Null
        [System.IO.File]::WriteAllLines($path, $lines)
    }
}

function Write-BeastAI {
    param([object[]]$Rows)
    $path = Resolve-RepoPath "SWLOR.Game.Server\Feature\AIDefinition\BeastAIDefinition.cs"
    $activeBeastRows = @($Rows | Where-Object { $_.Tab -eq "Beast Mastery" -and $ActiveTypes -contains $_.Type } | Sort-Object Row)
    $selfFeats = @(
        $activeBeastRows |
            Where-Object { !(Test-IsHostile $_) -or (Test-IsSelfCenteredHostileArea $_) } |
            Group-Object BaseIdentifier |
            Sort-Object { [int]$_.Group[0].Row } |
            ForEach-Object { $_.Group | Sort-Object { [int]$_.LevelNumber } -Descending }
    )
    $targetFeats = @(
        $activeBeastRows |
            Where-Object { (Test-IsHostile $_) -and !(Test-IsSelfCenteredHostileArea $_) } |
            Group-Object BaseIdentifier |
            Sort-Object { [int]$_.Group[0].Row } |
            ForEach-Object { $_.Group | Sort-Object { [int]$_.LevelNumber } -Descending }
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.AddRange([string[]]@(
        "using SWLOR.NWN.API.NWScript.Enum;",
        "",
        "namespace SWLOR.Game.Server.Feature.AIDefinition",
        "{",
        "    public sealed class BeastAIDefinition : AIBase",
        "    {",
        "        private static readonly FeatType[] SelfTargetedFeats =",
        "        {"
    ))
    foreach ($row in $selfFeats) {
        $lines.Add("            FeatType.$($row.FeatIdentifier),") | Out-Null
    }
    $lines.AddRange([string[]]@(
        "        };",
        "",
        "        private static readonly FeatType[] EnemyTargetedFeats =",
        "        {"
    ))
    foreach ($row in $targetFeats) {
        $lines.Add("            FeatType.$($row.FeatIdentifier),") | Out-Null
    }
    $lines.AddRange([string[]]@(
        "        };",
        "",
        "        public override (FeatType, uint) DeterminePerkAbility()",
        "        {"
    ))
    $lines.AddRange([string[]]@(
        "            foreach (var feat in SelfTargetedFeats)",
        "            {",
        "                if (CheckIfCanUseFeat(Self, Self, feat))",
        "                    return (feat, Self);",
        "            }",
        "",
        "            foreach (var feat in EnemyTargetedFeats)",
        "            {",
        "                if (CheckIfCanUseFeat(Self, Target, feat))",
        "                    return (feat, Target);",
        "            }",
        ""
    ))
    $lines.Add("            return base.DeterminePerkAbility();") | Out-Null
    $lines.Add("        }") | Out-Null
    $lines.Add("    }") | Out-Null
    $lines.Add("}") | Out-Null
    [System.IO.File]::WriteAllLines($path, $lines)
}

function Get-HeaderLineIndex {
    param([string[]]$Lines)
    for ($i = 1; $i -lt $Lines.Count; $i++) {
        if (![string]::IsNullOrWhiteSpace($Lines[$i])) { return $i }
    }
    throw "Could not locate 2DA header."
}

function Convert-ToStringList {
    param([string]$Line)
    return ,([System.Collections.Generic.List[string]]@($Line.Trim() -split "\s+"))
}

function Get-RowNumber {
    param([System.Collections.Generic.IList[string]]$Tokens)
    $row = 0
    if ($Tokens.Count -eq 0 -or ![int]::TryParse($Tokens[0], [ref]$row)) { return $null }
    return $row
}

function Get-TokenByHeader {
    param([System.Collections.Generic.IList[string]]$Tokens, [string[]]$Headers, [string]$Header)
    $index = [array]::IndexOf($Headers, $Header)
    if ($index -lt 0) { throw "Could not find 2DA column '$Header'." }
    return $Tokens[$index + 1]
}

function Set-TokenByHeader {
    param([System.Collections.Generic.IList[string]]$Tokens, [string[]]$Headers, [string]$Header, [string]$Value)
    $index = [array]::IndexOf($Headers, $Header)
    if ($index -lt 0) { throw "Could not find 2DA column '$Header'." }
    $Tokens[$index + 1] = $Value
}

function Format-2DARow {
    param([string[]]$Tokens, [int[]]$Widths)
    $parts = for ($i = 0; $i -lt $Tokens.Count; $i++) {
        $width = if ($i -lt $Widths.Count) { $Widths[$i] } else { 8 }
        $Tokens[$i].PadRight($width)
    }
    return ($parts -join "").TrimEnd()
}

$FeatColumnWidths = @(
    7, 49, 11, 14, 19, 17, 9, 9, 9, 9, 9, 9, 13, 14, 14, 15, 15, 19,
    11, 8, 10, 12, 10, 13, 13, 13, 13, 13, 13, 13, 11, 18, 12, 20, 49,
    18, 14, 11, 16, 11, 13, 13, 12
)
$SpellColumnWidths = @(
    7, 36, 11, 19, 9, 8, 7, 12, 13, 19, 7, 9, 8, 10, 9, 11, 9, 11,
    18, 18, 18, 19, 19, 19, 11, 11, 17, 17, 17, 19, 7, 19, 15, 15,
    19, 18, 17, 14, 14, 14, 14, 14, 14, 11, 9, 11, 12, 19, 20, 13,
    17, 12, 11, 11, 19, 13, 13, 13, 12
)
$ClsColumnWidths = @(7, 49, 12, 7, 17, 8)

function Ensure-Feat2DARows {
    param([object[]]$Rows)
    $path = Resolve-RepoPath $Feat2daPath
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.AddRange([System.IO.File]::ReadAllLines($path))
    $headerIndex = Get-HeaderLineIndex $lines.ToArray()
    $headers = $lines[$headerIndex].Trim() -split "\s+"
    $expectedTokens = $headers.Count + 1
    $existingRows = @{}
    foreach ($line in $lines) {
        $tokens = Convert-ToStringList $line
        $rowNumber = Get-RowNumber $tokens
        if ($null -ne $rowNumber) { $existingRows[$rowNumber] = $true }
    }

    foreach ($row in ($Rows | Where-Object { $ActiveTypes -contains $_.Type } | Sort-Object FeatId)) {
        if ($existingRows.ContainsKey([int]$row.FeatId)) { continue }
        $tokens = [System.Collections.Generic.List[string]]::new()
        for ($i = 0; $i -lt $expectedTokens; $i++) { $tokens.Add("****") | Out-Null }
        $tokens[0] = ([int]$row.FeatId).ToString()
        Set-TokenByHeader $tokens $headers "LABEL" $row.FeatIdentifier
        Set-TokenByHeader $tokens $headers "FEAT" "****"
        Set-TokenByHeader $tokens $headers "DESCRIPTION" "****"
        Set-TokenByHeader $tokens $headers "ICON" "default_perk"
        Set-TokenByHeader $tokens $headers "GAINMULTIPLE" "0"
        Set-TokenByHeader $tokens $headers "EFFECTSSTACK" "0"
        Set-TokenByHeader $tokens $headers "ALLCLASSESCANUSE" "1"
        Set-TokenByHeader $tokens $headers "SPELLID" "****"
        Set-TokenByHeader $tokens $headers "TOOLSCATEGORIES" "6"
        Set-TokenByHeader $tokens $headers "MinLevel" "99"
        Set-TokenByHeader $tokens $headers "ReqAction" "0"
        $lines.Add((Format-2DARow $tokens.ToArray() $FeatColumnWidths)) | Out-Null
        $existingRows[[int]$row.FeatId] = $true
    }

    [System.IO.File]::WriteAllLines($path, $lines)
}

function Ensure-ClsFeatRows {
    param([object[]]$Rows)
    $path = Resolve-RepoPath $ClsFeatFightPath
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.AddRange([System.IO.File]::ReadAllLines($path))
    $existingFeatIds = @{}
    $maxRow = -1
    foreach ($line in $lines) {
        $tokens = Convert-ToStringList $line
        $rowNumber = Get-RowNumber $tokens
        if ($null -eq $rowNumber) { continue }
        if ($rowNumber -gt $maxRow) { $maxRow = $rowNumber }
        if ($tokens.Count -gt 2) { $existingFeatIds[$tokens[2]] = $true }
    }

    foreach ($row in ($Rows | Where-Object { $ActiveTypes -contains $_.Type } | Sort-Object FeatId)) {
        $featId = ([int]$row.FeatId).ToString()
        if ($existingFeatIds.ContainsKey($featId)) { continue }
        $maxRow++
        $tokens = @($maxRow.ToString(), $row.FeatIdentifier, $featId, "1", "99", "1")
        $lines.Add((Format-2DARow $tokens $ClsColumnWidths)) | Out-Null
        $existingFeatIds[$featId] = $true
    }
    [System.IO.File]::WriteAllLines($path, $lines)
}

function Update-GeneratedSpellTargeting {
    param([object[]]$Rows)
    $featPath = Resolve-RepoPath $Feat2daPath
    $spellPath = Resolve-RepoPath $Spells2daPath
    $featLines = [System.Collections.Generic.List[string]]::new()
    $featLines.AddRange([System.IO.File]::ReadAllLines($featPath))
    $spellLines = [System.Collections.Generic.List[string]]::new()
    $spellLines.AddRange([System.IO.File]::ReadAllLines($spellPath))
    $featHeaderIndex = Get-HeaderLineIndex $featLines.ToArray()
    $featHeaders = $featLines[$featHeaderIndex].Trim() -split "\s+"
    $spellHeaderIndex = Get-HeaderLineIndex $spellLines.ToArray()
    $spellHeaders = $spellLines[$spellHeaderIndex].Trim() -split "\s+"
    $spellLineByFeatId = @{}
    for ($i = $spellHeaderIndex + 1; $i -lt $spellLines.Count; $i++) {
        $tokens = Convert-ToStringList $spellLines[$i]
        $rowNumber = Get-RowNumber $tokens
        if ($null -eq $rowNumber) { continue }
        $featId = Get-TokenByHeader $tokens $spellHeaders "FeatID"
        if ($featId -ne "****") { $spellLineByFeatId[$featId] = $i }
    }

    $rowsByFeatId = @{}
    foreach ($row in ($Rows | Where-Object { $ActiveTypes -contains $_.Type })) {
        $rowsByFeatId[([int]$row.FeatId).ToString()] = $row
    }

    for ($i = $featHeaderIndex + 1; $i -lt $featLines.Count; $i++) {
        $featTokens = Convert-ToStringList $featLines[$i]
        $rowNumber = Get-RowNumber $featTokens
        if ($null -eq $rowNumber) { continue }
        $key = $rowNumber.ToString()
        if (!$rowsByFeatId.ContainsKey($key) -or !$spellLineByFeatId.ContainsKey($key)) { continue }

        $row = $rowsByFeatId[$key]
        $isHostile = Test-IsHostile $row
        $isArea = Test-IsArea $row
        $isSelf = Test-IsSelf $row
        $requiresTarget = Test-RequiresTarget $row
        $spellLineIndex = $spellLineByFeatId[$key]
        $spellTokens = Convert-ToStringList $spellLines[$spellLineIndex]

        if ($isSelf -or (!$requiresTarget -and !$isHostile)) {
            Set-TokenByHeader $featTokens $featHeaders "TARGETSELF" "1"
            Set-TokenByHeader $spellTokens $spellHeaders "Range" "P"
            Set-TokenByHeader $spellTokens $spellHeaders "TargetType" "0x01"
            Set-TokenByHeader $spellTokens $spellHeaders "HostileSetting" "0"
        }
        elseif ($isArea) {
            Set-TokenByHeader $featTokens $featHeaders "TARGETSELF" "****"
            Set-TokenByHeader $featTokens $featHeaders "HostileFeat" ($(if ($isHostile) { "1" } else { "****" }))
            Set-TokenByHeader $spellTokens $spellHeaders "Range" "M"
            Set-TokenByHeader $spellTokens $spellHeaders "TargetType" ($(if ($isHostile) { "0x3E" } else { "0x01" }))
            Set-TokenByHeader $spellTokens $spellHeaders "HostileSetting" ($(if ($isHostile) { "1" } else { "0" }))
            Set-TokenByHeader $spellTokens $spellHeaders "TargetShape" "sphere"
            Set-TokenByHeader $spellTokens $spellHeaders "TargetSizeX" "5"
            Set-TokenByHeader $spellTokens $spellHeaders "TargetFlags" ($(if ($isHostile) { "1" } else { "17" }))
        }
        else {
            Set-TokenByHeader $featTokens $featHeaders "TARGETSELF" "****"
            Set-TokenByHeader $featTokens $featHeaders "HostileFeat" ($(if ($isHostile) { "1" } else { "****" }))
            Set-TokenByHeader $spellTokens $spellHeaders "Range" "M"
            Set-TokenByHeader $spellTokens $spellHeaders "TargetType" ($(if ($isHostile) { "0x02" } else { "0x03" }))
            Set-TokenByHeader $spellTokens $spellHeaders "HostileSetting" ($(if ($isHostile) { "1" } else { "0" }))
        }

        $featLines[$i] = Format-2DARow $featTokens.ToArray() $FeatColumnWidths
        $spellLines[$spellLineIndex] = Format-2DARow $spellTokens.ToArray() $SpellColumnWidths
    }

    [System.IO.File]::WriteAllLines($featPath, $featLines)
    [System.IO.File]::WriteAllLines($spellPath, $spellLines)
}

$manifest = Import-Csv (Resolve-RepoPath $ManifestPath)
$rows = @(
    $manifest |
        Where-Object {
            $ScopedTabs -contains $_.Tab -and
            $_.DevStatus -eq "Design" -and
            (@("Combat", "Stance", "Toggle", "Trait") -contains $_.Type)
        } |
        Sort-Object { [int]$_.Row } |
        ForEach-Object {
            $_ | Add-Member -NotePropertyName BaseName -NotePropertyValue (Get-BaseName $_.PerkName) -Force
            $_ | Add-Member -NotePropertyName BaseIdentifier -NotePropertyValue (ConvertTo-Identifier (Get-BaseName $_.PerkName)) -Force
            $_ | Add-Member -NotePropertyName FeatIdentifier -NotePropertyValue (Get-FeatIdentifier $_.PerkName) -Force
            $_ | Add-Member -NotePropertyName LevelNumber -NotePropertyValue (Get-RomanRank $_.PerkName) -Force
            $_ | Add-Member -NotePropertyName FeatId -NotePropertyValue $null -Force
            $_
        }
)

$perkMap = Update-PerkType $rows
$featMap = Update-FeatType $rows
$recastMap = Update-RecastGroup $rows

foreach ($row in ($rows | Where-Object { $ActiveTypes -contains $_.Type })) {
    $row.FeatId = [int]$featMap[$row.FeatIdentifier]
}

Write-PerkDefinitions $rows
Write-CombatPerkStatusEffectDefinitions $rows
Write-AbilityDefinitions $rows
Write-BeastAI $rows
Ensure-Feat2DARows $rows
Ensure-ClsFeatRows $rows
Update-GeneratedSpellTargeting $rows

$activeCount = @($rows | Where-Object { $ActiveTypes -contains $_.Type }).Count
$traitCount = @($rows | Where-Object { $_.Type -eq "Trait" }).Count
$maxFeatId = ($rows | Where-Object { $ActiveTypes -contains $_.Type } | ForEach-Object { [int]$_.FeatId } | Measure-Object -Maximum).Maximum
Write-Host "Generated $activeCount active design combat perks and $traitCount design combat traits."
Write-Host "Highest generated feat id: $maxFeatId"
