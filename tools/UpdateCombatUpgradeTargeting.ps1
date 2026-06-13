[CmdletBinding()]
param(
    [string]$Feat2daPath = "SWLOR_Haks\swlor2_2da\feat.2da",
    [string]$Spells2daPath = "SWLOR_Haks\swlor2_2da\spells.2da",
    [int]$GeneratedFeatStart = 2000,
    [int]$GeneratedFeatEnd = 2578
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

function Get-HeaderLineIndex {
    param([string[]]$Lines)

    for ($i = 1; $i -lt $Lines.Count; $i++) {
        if (![string]::IsNullOrWhiteSpace($Lines[$i])) {
            return $i
        }
    }

    throw "Could not locate 2DA header line."
}

function Convert-ToStringList {
    param([string]$Line)

    $list = [System.Collections.Generic.List[string]]::new()
    $list.AddRange([string[]]($Line.Trim() -split "\s+"))

    return ,$list
}

function Format-2DARow {
    param(
        [string[]]$Tokens,
        [int[]]$Widths
    )

    $parts = for ($i = 0; $i -lt $Tokens.Count; $i++) {
        $width = if ($i -lt $Widths.Count) { $Widths[$i] } else { 8 }
        $Tokens[$i].PadRight($width)
    }

    return ($parts -join "").TrimEnd()
}

function Get-RowNumber {
    param([System.Collections.Generic.IList[string]]$Tokens)

    $rowNumber = 0
    if ($Tokens.Count -eq 0 -or ![int]::TryParse($Tokens[0], [ref]$rowNumber)) {
        return $null
    }

    return $rowNumber
}

function Set-TokenByHeader {
    param(
        [System.Collections.Generic.IList[string]]$Tokens,
        [string[]]$Headers,
        [string]$Header,
        [string]$Value
    )

    $headerIndex = [array]::IndexOf($Headers, $Header)
    if ($headerIndex -lt 0) {
        throw "Could not find column '$Header'."
    }

    $Tokens[$headerIndex + 1] = $Value
}

function Get-TokenByHeader {
    param(
        [System.Collections.Generic.IList[string]]$Tokens,
        [string[]]$Headers,
        [string]$Header
    )

    $headerIndex = [array]::IndexOf($Headers, $Header)
    if ($headerIndex -lt 0) {
        throw "Could not find column '$Header'."
    }

    return $Tokens[$headerIndex + 1]
}

function New-Rule {
    param(
        [string]$Range,
        [string]$TargetType,
        [string]$HostileSetting,
        [string]$TargetSelf = "****",
        [string]$TargetShape = "****",
        [string]$TargetSizeX = "****",
        [string]$TargetSizeY = "****",
        [string]$TargetFlags = "****",
        [string]$HostileFeat = "****"
    )

    return [pscustomobject]@{
        Range = $Range
        TargetType = $TargetType
        HostileSetting = $HostileSetting
        TargetSelf = $TargetSelf
        TargetShape = $TargetShape
        TargetSizeX = $TargetSizeX
        TargetSizeY = $TargetSizeY
        TargetFlags = $TargetFlags
        HostileFeat = $HostileFeat
    }
}

$selfRule = New-Rule -Range "P" -TargetType "0x01" -HostileSetting "0" -TargetSelf "1"
$friendlyCreatureRule = New-Rule -Range "M" -TargetType "0x03" -HostileSetting "0"
$hostileCreatureRule = New-Rule -Range "M" -TargetType "0x02" -HostileSetting "1" -HostileFeat "1"

function New-SelfSphereRule {
    param([string]$Radius = "5")

    return New-Rule -Range "P" -TargetType "0x01" -HostileSetting "1" -TargetSelf "1" -TargetShape "sphere" -TargetSizeX $Radius -TargetFlags "17"
}

function New-GroundSphereRule {
    param([string]$Radius = "5")

    return New-Rule -Range "M" -TargetType "0x3E" -HostileSetting "1" -TargetShape "sphere" -TargetSizeX $Radius -TargetFlags "1" -HostileFeat "1"
}

function New-OriginConeRule {
    param(
        [string]$Length,
        [string]$Width
    )

    return New-Rule -Range "M" -TargetType "0x3E" -HostileSetting "1" -TargetShape "cone" -TargetSizeX $Length -TargetSizeY $Width -TargetFlags "17" -HostileFeat "1"
}

function New-OriginLineRule {
    param(
        [string]$Length,
        [string]$Width
    )

    return New-Rule -Range "M" -TargetType "0x3E" -HostileSetting "1" -TargetShape "rectangle" -TargetSizeX $Length -TargetSizeY $Width -TargetFlags "17" -HostileFeat "1"
}

function New-TargetSphereRule {
    param([string]$Radius = "5")

    return New-Rule -Range "M" -TargetType "0x02" -HostileSetting "1" -TargetShape "sphere" -TargetSizeX $Radius -TargetFlags "1" -HostileFeat "1"
}

function New-FriendlySelfSphereRule {
    param([string]$Radius = "5")

    return New-Rule -Range "P" -TargetType "0x01" -HostileSetting "0" -TargetSelf "1" -TargetShape "sphere" -TargetSizeX $Radius -TargetFlags "17"
}

function New-FriendlyGroundSphereRule {
    param([string]$Radius = "5")

    return New-Rule -Range "M" -TargetType "0x3E" -HostileSetting "0" -TargetShape "sphere" -TargetSizeX $Radius -TargetFlags "1"
}

function Get-AbilityTargetingByLabel {
    param([string]$AbilityDefinitionRoot)

    $targeting = @{}
    if (!(Test-Path $AbilityDefinitionRoot)) {
        return $targeting
    }

    $createPattern = '(?s)\.Create\(FeatType\.(?<feat>[A-Za-z0-9_]+),\s*PerkType\.[^)]+\)(?<chain>.*?);'
    foreach ($file in Get-ChildItem $AbilityDefinitionRoot -Recurse -Filter "*AbilityDefinition.cs") {
        $text = [System.IO.File]::ReadAllText($file.FullName)
        foreach ($match in [regex]::Matches($text, $createPattern)) {
            $chain = $match.Groups["chain"].Value
            $targeting[$match.Groups["feat"].Value] = [pscustomobject]@{
                IsHostile = $chain -match '\.IsHostileAbility\('
                IsArea = $chain -match '\.IsAreaAbility\('
                RequiresTarget = $chain -match '\.RequiresTarget\('
            }
        }
    }

    return $targeting
}

function Get-DefaultRule {
    param(
        [string]$Label,
        [hashtable]$AbilityTargetingByLabel
    )

    if (!$AbilityTargetingByLabel.ContainsKey($Label)) {
        return $hostileCreatureRule
    }

    $targeting = $AbilityTargetingByLabel[$Label]
    if ($targeting.IsHostile) {
        if ($targeting.IsArea) {
            return New-GroundSphereRule
        }

        return $hostileCreatureRule
    }

    if ($targeting.IsArea) {
        return New-FriendlySelfSphereRule
    }

    if ($targeting.RequiresTarget) {
        return $friendlyCreatureRule
    }

    return $selfRule
}

$rulesByLabel = @{}

foreach ($label in @(
    "AbsoluteDefense1",
    "AdamantineGuard1",
    "AngerStrike1",
    "BastionStance1",
    "BerserkerStance1",
    "BerserkerStance2",
    "BlazingSpikes1",
    "Bloodlust1",
    "BombardierStance1",
    "BreakerReversal1",
    "BrutalAssault1",
    "ChargeOrder1",
    "ChargeOrder2",
    "CalmingStance1",
    "Centering1",
    "Centering2",
    "CobraStance1",
    "CoordinatedFocus1",
    "CoordinatedFocus2",
    "CoordinatedFocus3",
    "ConduitStance1",
    "CripplingShot1",
    "CripplingShot2",
    "CripplingShot3",
    "CrusherStance1",
    "CycloneStance1",
    "DeadeyeStance1",
    "DeadlyPrecision1",
    "DebilitatingStance1",
    "DefensiveStance1",
    "DefensiveStance2",
    "DisablingStrike1",
    "DisablingStrike2",
    "DisablingStrike3",
    "DuelistStance1",
    "EssenceHunter1",
    "EvasiveCombat1",
    "EvasiveCombat2",
    "ExplosiveToss1",
    "ExplosiveToss2",
    "ExplosiveToss3",
    "ExplosiveToss4",
    "FieldRecovery1",
    "FieldRecovery2",
    "FerocityStance1",
    "FinalForm1",
    "FlankingStance1",
    "FocusedStance1",
    "ForceCapacitor1",
    "ForceLens1",
    "FortressStrike1",
    "FortressStrike2",
    "FortressStrike3",
    "GuardianMaster1",
    "GuardianSResolve1",
    "GuardedChannel1",
    "GuardedChannel2",
    "GuardedChannel3",
    "GuardingStep1",
    "GuardCounter1",
    "GuardCounter2",
    "GuardCounter3",
    "GunfighterStance1",
    "GunslingerFocus1",
    "HackingBlade1",
    "HackingBlade2",
    "HackingBlade3",
    "Hamstring1",
    "Hamstring2",
    "Hamstring3",
    "ImpenetrableGuard1",
    "ImprovedAttentiveness1",
    "InfiniteConduit1",
    "InterruptionStrike1",
    "InterruptionStrike2",
    "Invincible1",
    "IronWallStance1",
    "KillZone1",
    "LegSweep1",
    "LegSweep2",
    "LegSweep3",
    "PerceptiveStance1",
    "PiercingToss1",
    "PiercingToss2",
    "PiercingToss3",
    "PinningToss1",
    "PinningToss2",
    "PinningToss3",
    "Purify1",
    "Rampart1",
    "RallyingStandard1",
    "RallyingStandard2",
    "RippleSlash1",
    "SaberRend1",
    "SaberRend2",
    "SecondWind1",
    "SentinelGuard1",
    "SentinelStance1",
    "ShelterCircle1",
    "ShieldBash1",
    "ShieldBash2",
    "ShieldBash3",
    "ShieldWall1",
    "SideAssault1",
    "SideAssault2",
    "SideAssault3",
    "SkirmisherStance1",
    "Slam1",
    "Slam2",
    "Slam3",
    "SnapRoll1",
    "SnapRoll2",
    "SniperStance1",
    "SoulAscension1",
    "SoulDevourer1",
    "SoulSacrifice1",
    "SoulStorm1",
    "SoulStrike1",
    "SoulStrike2",
    "SoulStrike3",
    "SpotterStance1",
    "StaticPalm1",
    "StaticPalm2",
    "StaticPalm3",
    "SteadyFormation1",
    "SteadyFormation2",
    "StrikingCobra1",
    "StrikingCobra2",
    "StrikingCobra3",
    "SurgeStrike1",
    "TacticalEscape1",
    "TacticalEscape2",
    "TempestStance1",
    "ToxicCoating1",
    "ToxicCoating2",
    "ToxicRush1",
    "TranquilizerShot1",
    "TranquilizerShot2",
    "TwinGuardStance1",
    "UnmovingCenter1",
    "VersatileStrike1",
    "VersatileStrike2",
    "VersatileStrike3",
    "WatchfulPresence1",
    "WatchfulPresence2",
    "WatchfulPresence3"
)) {
    $rulesByLabel[$label] = $selfRule
}

foreach ($label in @(
    "BladeVortex1",
    "BladeVortex2",
    "CircleSlash1",
    "CircleSlash2",
    "CircleSlash3",
    "ConduitFlare1",
    "CripplingDefense1",
    "CreepingTerror3",
    "Decoy1",
    "EdgeOfDarkness1",
    "EclipseOfResolve1",
    "Flash1",
    "FractureFocus2",
    "ForceGyre1",
    "ForceMaelstrom1",
    "GroundQuake1",
    "GroundQuake2",
    "Incapacitate1",
    "IronElbows1",
    "NightmareField1",
    "Pacify2",
    "Pacify3",
    "PunishingStrike1",
    "SaberCyclone1",
    "SpinningWhirl1",
    "SpinningWhirl2",
    "SpinningWhirl3",
    "StormRelease1",
    "SweepingGuard1",
    "TempestBloom1",
    "TempestRelease1",
    "WhirlwindAssault1",
    "WhirlwindAssault2",
    "Worldbreaker1"
)) {
    $rulesByLabel[$label] = New-SelfSphereRule
}

$rulesByLabel["TwinIntercept1"] = $friendlyCreatureRule
$rulesByLabel["DeadManSHand1"] = New-TargetSphereRule
$rulesByLabel["RicochetToss1"] = New-TargetSphereRule -Radius "5"
$rulesByLabel["RicochetToss2"] = New-TargetSphereRule -Radius "5"
$rulesByLabel["RicochetShot1"] = $hostileCreatureRule
$rulesByLabel["DuelistSChallenge1"] = $hostileCreatureRule

foreach ($label in @(
    "Backstab1",
    "Backstab2",
    "Backstab3",
    "CrossCut1",
    "CrossCut2",
    "CrossCut3",
    "CrossCut4",
    "DoubleStrike1",
    "DoubleStrike2",
    "DoubleStrike3",
    "DoubleStrike4",
    "MarkedForDeath1",
    "RiotBlade1",
    "RiotBlade2",
    "RiotBlade3"
)) {
    $rulesByLabel[$label] = $hostileCreatureRule
}

foreach ($label in @(
    "CreepingTerror2",
    "ForceGrip3",
    "ForceLightning1",
    "ForceLightning2"
)) {
    $rulesByLabel[$label] = New-TargetSphereRule
}

foreach ($label in @(
    "ClusterStorm1",
    "ConcussiveToss1",
    "ConcussiveToss2",
    "DisruptionField1",
    "FireburstToss1",
    "FlashToss1",
    "FlashToss2",
    "Forcebane1",
    "RainOfSteel1",
    "SaturationToss1",
    "SerpentSEclipse1",
    "SmokeRound1",
    "SystemicShutdown1"
)) {
    $rulesByLabel[$label] = New-GroundSphereRule
}

foreach ($label in @(
    "ArcStrike1",
    "CascadeFailure1",
    "CoveringClaws1",
    "FanTheHammer1",
    "FanTheHammer2",
    "GuardianSChallenge1",
    "HamperingBarrage1",
    "LastWord1",
    "MaelstromArc1",
    "MaelstromArc2",
    "OverwhelmingStrike1",
    "PointBlankBurst1",
    "SavageCleave1",
    "SoulBurst1",
    "StasisVolley1",
    "SweepingFlank1",
    "TotalForceDenial1",
    "TranqCone1",
    "TranqCone2",
    "VenomSplash1"
)) {
    $rulesByLabel[$label] = New-OriginConeRule -Length "5" -Width "5"
}

$rulesByLabel["TranqCone1"] = New-OriginConeRule -Length "8" -Width "6"
$rulesByLabel["TranqCone2"] = New-OriginConeRule -Length "10" -Width "7"

foreach ($label in @(
    "CoveringStrike1",
    "Earthshatter1",
    "FractureStrike1",
    "LineBreaker1",
    "PinningFire2",
    "SweepingAdvance1",
    "SuppressiveLine1",
    "ThunderousChallenge1"
)) {
    $rulesByLabel[$label] = New-OriginLineRule -Length "8" -Width "2.5"
}

foreach ($label in @(
    "Flamethrower1",
    "Flamethrower2",
    "Flamethrower3",
    "CryoSprayer1",
    "CryoSprayer2",
    "IceBreath1",
    "IceBreath2",
    "IceBreath3",
    "PoisonBreath1",
    "PoisonBreath2",
    "PoisonBreath3",
    "ForcePush3"
)) {
    $rulesByLabel[$label] = New-OriginConeRule -Length "6" -Width "5"
}

$rulesByLabel["ForcePush2"] = New-OriginLineRule -Length "8" -Width "2.5"

$rulesByLabel["KoltoMist1"] = New-FriendlySelfSphereRule -Radius "3"
$rulesByLabel["KoltoMist2"] = New-FriendlySelfSphereRule -Radius "3"
$rulesByLabel["BrutalAssault1"] = New-FriendlySelfSphereRule -Radius "5"
$rulesByLabel["TauntingDeflection1"] = New-SelfSphereRule -Radius "5"
$rulesByLabel["GuardianSInfluence1"] = New-FriendlySelfSphereRule -Radius "5"
$rulesByLabel["SaberStorm1"] = New-SelfSphereRule -Radius "5"
$rulesByLabel["SoulStorm1"] = New-FriendlySelfSphereRule -Radius "5"
$rulesByLabel["Rampart1"] = New-FriendlySelfSphereRule -Radius "5"
$rulesByLabel["CourageousResolve1"] = New-FriendlySelfSphereRule -Radius "5"
$rulesByLabel["ForceLens1"] = New-FriendlySelfSphereRule -Radius "5"

foreach ($label in @(
    "ChargeOrder1",
    "ChargeOrder2",
    "CoordinatedFocus1",
    "CoordinatedFocus2",
    "CoordinatedFocus3",
    "FieldRecovery1",
    "FieldRecovery2",
    "RallyingStandard1",
    "RallyingStandard2",
    "SteadyFormation1",
    "SteadyFormation2",
    "WatchfulPresence1",
    "WatchfulPresence2",
    "WatchfulPresence3"
)) {
    $rulesByLabel[$label] = New-FriendlySelfSphereRule -Radius "5"
}

foreach ($label in @(
    "ForceSanctuary1",
    "RayshieldScreen1",
    "RayshieldScreen2",
    "EmergencyBunker1"
)) {
    $rulesByLabel[$label] = New-FriendlyGroundSphereRule -Radius "4"
}

$rulesByLabel["BlasterBeacon1"] = New-GroundSphereRule -Radius "12"
$rulesByLabel["BlasterBeacon2"] = New-GroundSphereRule -Radius "12"
$rulesByLabel["BlasterBeacon3"] = New-GroundSphereRule -Radius "14"
$rulesByLabel["ShockBeacon1"] = New-GroundSphereRule -Radius "10"
$rulesByLabel["ShockBeacon2"] = New-GroundSphereRule -Radius "12"
$rulesByLabel["IncendiaryField1"] = New-GroundSphereRule -Radius "5"
$rulesByLabel["IncendiaryField2"] = New-GroundSphereRule -Radius "5"
$rulesByLabel["IncendiaryField3"] = New-GroundSphereRule -Radius "5"
$rulesByLabel["PulseRelay1"] = New-Rule -Range "P" -TargetType "0x01" -HostileSetting "****" -TargetSelf "1" -TargetShape "sphere" -TargetSizeX "10" -TargetFlags "17"
$rulesByLabel["PulseRelay2"] = New-Rule -Range "P" -TargetType "0x01" -HostileSetting "****" -TargetSelf "1" -TargetShape "sphere" -TargetSizeX "10" -TargetFlags "17"
$rulesByLabel["DisruptionPulse1"] = New-GroundSphereRule -Radius "5"
$rulesByLabel["SignalJammer1"] = New-GroundSphereRule -Radius "5"
$rulesByLabel["RemoteCharge1"] = New-GroundSphereRule -Radius "5"
$rulesByLabel["RemoteCharge2"] = New-GroundSphereRule -Radius "5"
$rulesByLabel["RemoteCharge3"] = New-GroundSphereRule -Radius "5"
$rulesByLabel["KillzoneBeacon1"] = New-GroundSphereRule -Radius "12"
$rulesByLabel["FragGrenade1"] = New-GroundSphereRule -Radius "3"
$rulesByLabel["FragGrenade2"] = New-GroundSphereRule -Radius "3"
$rulesByLabel["FragGrenade3"] = New-GroundSphereRule -Radius "3"
$rulesByLabel["ConcussionGrenade1"] = New-GroundSphereRule -Radius "3"
$rulesByLabel["ConcussionGrenade2"] = New-GroundSphereRule -Radius "3"
$rulesByLabel["ConcussionGrenade3"] = New-GroundSphereRule -Radius "3"
$rulesByLabel["IonGrenade1"] = New-GroundSphereRule -Radius "3"
$rulesByLabel["IonGrenade2"] = New-GroundSphereRule -Radius "3"
$rulesByLabel["FlashGrenade1"] = New-GroundSphereRule -Radius "4"
$rulesByLabel["FlashGrenade2"] = New-GroundSphereRule -Radius "4"
$rulesByLabel["AdhesiveGrenade1"] = New-GroundSphereRule -Radius "4"
$rulesByLabel["AdhesiveGrenade2"] = New-GroundSphereRule -Radius "4"
$rulesByLabel["ClusterGrenade1"] = New-GroundSphereRule -Radius "2"
$rulesByLabel["ThermalDetonator1"] = New-GroundSphereRule -Radius "5"
$rulesByLabel["RainOfSteel1"] = New-GroundSphereRule -Radius "8"
$rulesByLabel["IonLance1"] = New-OriginLineRule -Length "8" -Width "2.5"
$rulesByLabel["IonLance2"] = New-OriginLineRule -Length "8" -Width "2.5"
$rulesByLabel["IonLance3"] = New-OriginLineRule -Length "8" -Width "2.5"
$rulesByLabel["ThrowLightsaber1"] = New-OriginLineRule -Length "15" -Width "2.5"
$rulesByLabel["ThrowLightsaber2"] = New-OriginLineRule -Length "15" -Width "2.5"
$rulesByLabel["ThrowLightsaber3"] = New-OriginLineRule -Length "15" -Width "2.5"
$rulesByLabel["RadiantLance1"] = New-OriginLineRule -Length "8" -Width "2.5"
$rulesByLabel["RadiantLance2"] = New-OriginLineRule -Length "8" -Width "2.5"
$rulesByLabel["RadiantLance3"] = New-OriginLineRule -Length "8" -Width "2.5"
$rulesByLabel["ForceJudgment2"] = New-TargetSphereRule -Radius "5"
$rulesByLabel["ForceJudgment3"] = New-TargetSphereRule -Radius "5"

$abilityTargetingByLabel = Get-AbilityTargetingByLabel (Resolve-RepoPath "SWLOR.Game.Server\Feature\AbilityDefinition")

$featPath = Resolve-RepoPath $Feat2daPath
$spellsPath = Resolve-RepoPath $Spells2daPath

$featLines = [System.Collections.Generic.List[string]]::new()
$featLines.AddRange([System.IO.File]::ReadAllLines($featPath))
$featHeaderIndex = Get-HeaderLineIndex $featLines.ToArray()
$featHeaders = $featLines[$featHeaderIndex].Trim() -split "\s+"
$featColumnWidths = @(
    7, 49, 11, 14, 19, 17, 9, 9, 9, 9, 9, 9, 13, 14, 14, 15, 15, 19,
    11, 8, 10, 12, 10, 13, 13, 13, 13, 13, 13, 13, 11, 18, 12, 20, 49,
    18, 14, 11, 16, 11, 13, 13, 12
)

$spellsLines = [System.Collections.Generic.List[string]]::new()
$spellsLines.AddRange([System.IO.File]::ReadAllLines($spellsPath))
$spellsHeaderIndex = Get-HeaderLineIndex $spellsLines.ToArray()
$spellsHeaders = $spellsLines[$spellsHeaderIndex].Trim() -split "\s+"
$spellColumnWidths = @(
    7, 36, 11, 19, 9, 8, 7, 12, 13, 19, 7, 9, 8, 10, 9, 11, 9, 11,
    18, 18, 18, 19, 19, 19, 11, 11, 17, 17, 17, 19, 7, 19, 15, 15,
    19, 18, 17, 14, 14, 14, 14, 14, 14, 11, 9, 11, 12, 19, 20, 13,
    17, 12, 11, 11, 19, 13, 13, 13, 12
)

$spellLineByFeatId = @{}
for ($i = $spellsHeaderIndex + 1; $i -lt $spellsLines.Count; $i++) {
    $tokens = Convert-ToStringList $spellsLines[$i]
    $rowNumber = Get-RowNumber $tokens
    if ($null -eq $rowNumber) {
        continue
    }

    $featId = Get-TokenByHeader $tokens $spellsHeaders "FeatID"
    if ($featId -ne "****") {
        $spellLineByFeatId[$featId] = $i
    }
}

$updated = 0
$selfCount = 0
$targetCount = 0
$areaCount = 0

for ($i = $featHeaderIndex + 1; $i -lt $featLines.Count; $i++) {
    $featTokens = Convert-ToStringList $featLines[$i]
    $rowNumber = Get-RowNumber $featTokens
    if ($null -eq $rowNumber -or $rowNumber -lt $GeneratedFeatStart -or $rowNumber -gt $GeneratedFeatEnd) {
        continue
    }

    $label = Get-TokenByHeader $featTokens $featHeaders "LABEL"
    if ($label -eq "****") {
        continue
    }

    $spellId = Get-TokenByHeader $featTokens $featHeaders "SPELLID"
    if ($spellId -eq "****" -or !$spellLineByFeatId.ContainsKey($rowNumber.ToString())) {
        throw "Feat row $rowNumber ($label) does not have a matching spell row."
    }

    $rule = if ($rulesByLabel.ContainsKey($label)) { $rulesByLabel[$label] } else { Get-DefaultRule $label $abilityTargetingByLabel }

    Set-TokenByHeader $featTokens $featHeaders "TARGETSELF" $rule.TargetSelf
    Set-TokenByHeader $featTokens $featHeaders "HostileFeat" $rule.HostileFeat
    $featLines[$i] = Format-2DARow $featTokens.ToArray() $featColumnWidths

    $spellLineIndex = $spellLineByFeatId[$rowNumber.ToString()]
    $spellTokens = Convert-ToStringList $spellsLines[$spellLineIndex]

    if ($spellTokens[0] -ne $spellId) {
        throw "Feat row $rowNumber ($label) points to spell $spellId but matching FeatID row is spell $($spellTokens[0])."
    }

    Set-TokenByHeader $spellTokens $spellsHeaders "Range" $rule.Range
    Set-TokenByHeader $spellTokens $spellsHeaders "TargetType" $rule.TargetType
    Set-TokenByHeader $spellTokens $spellsHeaders "HostileSetting" $rule.HostileSetting
    Set-TokenByHeader $spellTokens $spellsHeaders "TargetShape" $rule.TargetShape
    Set-TokenByHeader $spellTokens $spellsHeaders "TargetSizeX" $rule.TargetSizeX
    Set-TokenByHeader $spellTokens $spellsHeaders "TargetSizeY" $rule.TargetSizeY
    Set-TokenByHeader $spellTokens $spellsHeaders "TargetFlags" $rule.TargetFlags
    $spellsLines[$spellLineIndex] = Format-2DARow $spellTokens.ToArray() $spellColumnWidths

    $updated++
    if ($rule.TargetSelf -eq "1") {
        $selfCount++
    }
    elseif ($rule.TargetShape -ne "****") {
        $areaCount++
    }
    else {
        $targetCount++
    }
}

[System.IO.File]::WriteAllLines($featPath, $featLines)
[System.IO.File]::WriteAllLines($spellsPath, $spellsLines)

Write-Host "Updated $updated generated combat feat spell targeting rows."
Write-Host "Self or no-click activations: $selfCount"
Write-Host "Area/directional activations: $areaCount"
Write-Host "Direct target activations: $targetCount"
