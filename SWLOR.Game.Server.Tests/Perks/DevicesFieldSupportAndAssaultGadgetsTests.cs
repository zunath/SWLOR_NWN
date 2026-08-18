using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.Devices;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class DevicesFieldSupportAndAssaultGadgetsTests
{
    [Test]
    public void AssaultGadgetTwins_MatchForceBaseDamage()
    {
        foreach (var fieldName in new[] { "Rank1BaseDamage", "Rank2BaseDamage", "Rank3BaseDamage" })
        {
            GetAbilityConstant<int>(typeof(ArcProjectorAbilityDefinition), fieldName)
                .Should().Be(GetAbilityConstant<int>(typeof(ThrowRockAbilityDefinition), fieldName));
            GetAbilityConstant<int>(typeof(IonLanceAbilityDefinition), fieldName)
                .Should().Be(GetAbilityConstant<int>(typeof(RadiantLanceAbilityDefinition), fieldName));
        }
    }

    [Test]
    public void DevicesFieldSupportAndAssaultGadgetsBibleManifest_ContainsBatch()
    {
        var root = FindRepositoryRoot();
        var manifest = File.ReadAllText((root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv").FullName);
        var perkNames = new[]
        {
            "Deflector Shield I", "Power Surge", "Weapon Jam", "Power Cell I",
            "Deflector Shield II", "Rayshield Screen I", "Dampening Field I",
            "Power Cell II", "Deflector Shield III", "Overclock Routine", "Rayshield Screen II",
            "Dampening Field II", "Group Deflector", "Power Cell III",
            "Emergency Bunker", "Flamethrower I", "Wrist Rocket I", "Sonic Burst I",
            "Gadget Harness", "Arc Projector I", "Flamethrower II", "Ion Lance I",
            "Rail Dart I", "Tactical Uplink",
            "Wrist Rocket II", "Sonic Burst II", "Cryo Sprayer", "Flamethrower III",
            "Arc Projector II", "Ion Lance II", "Rail Dart II", "Wrist Rocket III",
            "Sonic Burst III", "Arc Projector III", "Ion Lance III", "Overload Barrage"
        };

        foreach (var perkName in perkNames)
        {
            manifest.Should().Contain($"\"{perkName}\"");
        }
    }
    [Test]
    public void DevicesFieldSupportAndAssaultGadgetsStatuses_MatchCombatBible()
    {
        new WeaponJam1StatusEffect().StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(-6);
        new WeaponJam1StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(0);
        new PowerCell1StatusEffect().StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(4);
        new PowerCell1StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(0);
        new PowerCell2StatusEffect().StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(6);
        new PowerCell2StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(0);
        new PowerCell3StatusEffect().StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(6);
        new PowerCell3StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(0);
        new PowerSurgeStatusEffect().StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(6);
        new PowerSurgeStatusEffect().StatGroup.Stats[StatType.CriticalRatePercentAdjustment].Should().Be(6);
        new RayshieldScreen1StatusEffect().StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(8);
        new RayshieldScreen1StatusEffect().StatGroup.Stats[StatType.RangedPhysicalDamageTakenPercentAdjustment].Should().Be(0);
        new RayshieldScreen2StatusEffect().StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(12);
        new RayshieldScreen2StatusEffect().StatGroup.Stats[StatType.RangedPhysicalDamageTakenPercentAdjustment].Should().Be(0);
        new DampeningField1StatusEffect().StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(-6);
        new DampeningField1StatusEffect().StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-6);
        new DampeningField2StatusEffect().StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(-10);
        new DampeningField2StatusEffect().StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-10);
        new OverclockRoutineStatusEffect().StatGroup.Stats[StatType.CombatReadinessPercent].Should().Be(4);
        new EmergencyBunker1StatusEffect().StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(-15);
        new EmergencyBunker1StatusEffect().StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-15);
        new EmergencyBunker1StatusEffect().StatGroup.Stats[StatType.RangedPhysicalDamageTakenPercentAdjustment].Should().Be(0);
        new TacticalUplinkStatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustmentSkillType].Should().Be((int)SkillType.Devices);
        new TacticalUplinkStatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(5);
        new TacticalUplinkStatusEffect().StatGroup.Stats[StatType.AbilityCriticalRatePercentAdjustmentSkillType].Should().Be((int)SkillType.Devices);
        new TacticalUplinkStatusEffect().StatGroup.Stats[StatType.AbilityCriticalRatePercentAdjustment].Should().Be(5);

        new SonicBurst2StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(0);
        new SonicBurst2StatusEffect().StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-6);
        new SonicBurst3StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(0);
        new SonicBurst3StatusEffect().StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-10);

        CombatDamageType.Sonic.GetDetails().NWScriptDamageType.Should().Be(DamageType.Sonic);
    }

    [Test]
    public void AssaultGadgetWeaponDamageEquivalent_MatchesPistolDamageTiers()
    {
        DeviceAbilityEffects.CalculateAssaultGadgetWeaponDamageEquivalent(0).Should().Be(6);
        DeviceAbilityEffects.CalculateAssaultGadgetWeaponDamageEquivalent(9).Should().Be(6);
        DeviceAbilityEffects.CalculateAssaultGadgetWeaponDamageEquivalent(10).Should().Be(10);
        DeviceAbilityEffects.CalculateAssaultGadgetWeaponDamageEquivalent(19).Should().Be(10);
        DeviceAbilityEffects.CalculateAssaultGadgetWeaponDamageEquivalent(20).Should().Be(15);
        DeviceAbilityEffects.CalculateAssaultGadgetWeaponDamageEquivalent(29).Should().Be(15);
        DeviceAbilityEffects.CalculateAssaultGadgetWeaponDamageEquivalent(30).Should().Be(19);
        DeviceAbilityEffects.CalculateAssaultGadgetWeaponDamageEquivalent(39).Should().Be(19);
        DeviceAbilityEffects.CalculateAssaultGadgetWeaponDamageEquivalent(40).Should().Be(24);
        DeviceAbilityEffects.CalculateAssaultGadgetWeaponDamageEquivalent(49).Should().Be(24);
        DeviceAbilityEffects.CalculateAssaultGadgetWeaponDamageEquivalent(50).Should().Be(28);
    }

    [Test]
    public void Flamethrower_UsesImpactDamageBeforeCosmeticAnimationCanClearIt()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "FlamethrowerAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");

        source.Should().Contain("Ability.ApplyTelegraphedCombatImpact(");
        source.Should().Contain("PlayFlamethrowerVisualEffect(activator);");
        source.Should().Contain("EffectVisualEffect(FlamethrowerVisualEffect)");
        source.Should().NotContain("ActionPlayAnimation(Animation.CastOutAnimation, 1f, 2.1f)");
        source.Should().NotContain("playImpactAnimation: false");
    }

    [Test]
    public void ReportedAssaultGadgetEffects_AreStaticallyWiredForRetest()
    {
        var root = FindRepositoryRoot();
        var perks = BuildDevicesAssaultGadgetsPerksWithout2daLookup();
        var gadgetHarness = perks[PerkType.GadgetHarness].PerkLevels[1];

        AssertStatBonus(gadgetHarness, StatType.AssaultGadgetAccuracyPercentAdjustment, 8);
        AssertStatBonus(gadgetHarness, StatType.AssaultGadgetCriticalRatePercentAdjustment, 8);

        var assaultAbilityFiles = new[]
        {
            "ArcProjectorAbilityDefinition.cs",
            "CryoSprayerAbilityDefinition.cs",
            "FlamethrowerAbilityDefinition.cs",
            "IonLanceAbilityDefinition.cs",
            "OverloadBarrageAbilityDefinition.cs",
            "RailDartAbilityDefinition.cs",
            "SonicBurstAbilityDefinition.cs",
            "WristRocketAbilityDefinition.cs"
        };
        foreach (var fileName in assaultAbilityFiles)
        {
            var source = File.ReadAllText(
                (root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / fileName).FullName);
            source.Should().Contain(
                "DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator)",
                $"{fileName} must consume Gadget Harness and Tactical Uplink critical chance");
        }

        var sonicBurst = File.ReadAllText(
            (root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "SonicBurstAbilityDefinition.cs").FullName);
        sonicBurst.Should().Contain("InterruptActivation(hitTarget);");
        sonicBurst.Should().Contain("AssignCommand(target, () => ClearAllActions());");

        var flamethrower = File.ReadAllText(
            (root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "FlamethrowerAbilityDefinition.cs").FullName);
        flamethrower.Should().Contain("typeof(BurnStatusEffect)");
        flamethrower.Split("typeof(BurnStatusEffect)").Should().HaveCount(3,
            "Flamethrower II and III each apply the documented 12-second Burn");

        var railDart = File.ReadAllText(
            (root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "RailDartAbilityDefinition.cs").FullName);
        railDart.Split("typeof(BleedStatusEffect)").Should().HaveCount(4,
            "all three Rail Dart ranks apply the documented 12-second Bleed");

        perks[PerkType.Flamethrower].PerkLevels[2].Description.Should().Contain("Burn for 12 seconds");
        perks[PerkType.Flamethrower].PerkLevels[3].Description.Should().Contain("Burn for 12 seconds");
        perks[PerkType.RailDart].PerkLevels.Values.Should().OnlyContain(
            level => level.Description.Contains("Bleed for 12 seconds", StringComparison.Ordinal));
    }

    [Test]
    public void AssaultGadgetCriticalChance_IsAdditiveAndObservable()
    {
        Combat.CalculateAbilityCriticalChance(0).Should().Be(5);
        Combat.CalculateAbilityCriticalChance(8).Should().Be(13,
            "Gadget Harness adds eight points to the five-percent ability baseline");
        Combat.CalculateAbilityCriticalChance(13).Should().Be(18,
            "Gadget Harness and Tactical Uplink stack additively");
        Combat.CalculateAbilityCriticalChance(100).Should().Be(50);
        Combat.CalculateAbilityCriticalChance(-100).Should().Be(5);

        var root = FindRepositoryRoot();
        var characterSheet = File.ReadAllText(
            (root / "SWLOR.Game.Server" / "Feature" / "GuiDefinition" / "ViewModel" / "CharacterSheetViewModel.cs").FullName);
        var ability = File.ReadAllText(
            (root / "SWLOR.Game.Server" / "Service" / "Ability.cs").FullName);
        var combat = File.ReadAllText(
            (root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);

        characterSheet.Should().Contain("Assault Gadget Crit");
        ability.Split("Combat.SendAbilityCriticalHitFeedback(").Should().HaveCount(3,
            "player and NPC-scaled ability criticals both need visible combat feedback");
        combat.Should().Contain("PlayerName.GetColoredDisplayName(observer, attacker)");
        combat.Should().Contain("critically hits");
    }

    [Test]
    public void IonLance_ResolvesLineDamageImmediatelyAfterCastCompletion()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "IonLanceAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");

        source.Should().Contain("CombatImpactAreaShape.Line,\n                0f,");
        source.Should().Contain("damageType: CombatDamageType.Electrical");
        source.Should().Contain("afterSuccessfulHit: hitTarget => ApplyIonLanceHitEffects(activator, hitTarget)");
    }

    [Test]
    public void AssaultGadgets_RenderDamageTypeVisuals()
    {
        var root = FindRepositoryRoot();
        var arcProjector = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "ArcProjectorAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");
        var ionLance = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "IonLanceAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");
        var railDart = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "RailDartAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");
        var cryoSprayer = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "CryoSprayerAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");
        var deviceEffects = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "DeviceAbilityEffects.cs").FullName)
            .Replace("\r\n", "\n");

        deviceEffects.Should().Contain("EffectBeam(VisualEffect.Vfx_Beam_Silent_Lightning, activator, BodyNode.Hand)");
        deviceEffects.Should().Contain("EffectVisualEffect(VisualEffect.Vfx_Imp_Lightning_S)");
        arcProjector.Should().Contain("DeviceAbilityEffects.ApplyElectricArcVisual(activator, target)");
        ionLance.Should().Contain("DeviceAbilityEffects.ApplyElectricArcVisual(activator, target)");
        railDart.Should().Contain("targetVisualEffect: VisualEffect.Vfx_Imp_Wallspike");
        cryoSprayer.Should().Contain("areaVisualEffect: VisualEffect.Vfx_Fnf_Icestorm");
    }

    [Test]
    public void EmergencyBunker_RendersVisibleAreaMarker()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "EmergencyBunkerAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");

        source.Should().Contain("EmergencyBunkerAreaMarkerVisualEffect = VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White");
        source.Should().Contain("EmergencyBunkerRadiusMeters = 8f");
        source.Should().Contain("EmergencyBunkerAreaMarkerVisualEffectScale = 4f");
        source.Should().Contain("EmergencyBunkerAreaMarkerVisualEffect,\n                EmergencyBunkerAreaMarkerVisualEffectScale");
    }

    [Test]
    public void PowerCellIII_TargetsAnAllyAndCentersTheAreaOnThatAlly()
    {
        var ability = new PowerCellAbilityDefinition().BuildAbilities()[FeatType.PowerCell3];

        ability.RequiresTarget.Should().BeTrue();
        ability.HasExplicitMaxRange.Should().BeTrue();
        ability.MaxRange.Should().Be(15f);
        ability.CustomValidation.Should().NotBeNull();
        ability.Targeting.Flags.Should().HaveFlag(AbilityTargetingFlags.HelpsAllies);
        ability.Targeting.Flags.Should().NotHaveFlag(AbilityTargetingFlags.OriginOnSelf);

        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "PowerCellAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");
        source.Should().Contain("GetPowerCell3Targets(activator, target, targetLocation)");
        source.Should().Contain("GetFriendlyTargetsNearLocation(\n                         activator,\n                         targetLocation,");
        source.Should().NotContain("GetFriendlyTargets(activator, activator, true)");
    }

    [Test]
    public void DevicesFieldSupportAndAssaultGadgetsFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");
        var feats = new[]
        {
            (FeatType.DeflectorShield1, "ife_dflctrshld1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.WeaponJam1, "ife_wpnjm1", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.PowerCell1, "ife_pwrcll1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.DeflectorShield2, "ife_dflctrshld2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.PowerCell2, "ife_pwrcll2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.DeflectorShield3, "ife_dflctrshld3", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.GroupDeflector1, "ife_grpdflctr1", "P", "0x01", "0", "sphere", "5", "****", "17", "1"),
            (FeatType.PowerCell3, "ife_pwrcll3", "M", "0x03", "0", "sphere", "5", "****", "4", "****"),
            (FeatType.EmergencyBunker1, "ife_mrgncybnkr1", "M", "0x3E", "0", "sphere", "8", "****", "1", "****"),
            (FeatType.Flamethrower1, "ife_flmthrwr1", "M", "0x3E", "1", "cone", "6", "5", "17", "****"),
            (FeatType.WristRocket1, "ife_wrstrckt1", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.SonicBurst1, "ife_sncburst1", "M", "0x3E", "1", "sphere", "5", "****", "17", "1"),
            (FeatType.Flamethrower2, "ife_flmthrwr2", "M", "0x3E", "1", "cone", "6", "5", "17", "****"),
            (FeatType.RailDart1, "ife_rldrt1", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.WristRocket2, "ife_wrstrckt2", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.SonicBurst2, "ife_sncburst2", "M", "0x3E", "1", "sphere", "5", "****", "17", "1"),
            (FeatType.CryoSprayer1, "ife_cryspryr1", "M", "0x3E", "1", "cone", "6", "5", "17", "****"),
            (FeatType.Flamethrower3, "ife_flmthrwr3", "M", "0x3E", "1", "cone", "6", "5", "17", "****"),
            (FeatType.RailDart2, "ife_rldrt2", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.WristRocket3, "ife_wrstrckt3", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.SonicBurst3, "ife_sncburst3", "M", "0x3E", "1", "sphere", "5", "****", "17", "1"),
            (FeatType.OverloadBarrage1, "ife_ovldbarr1", "M", "0x02", "1", "****", "****", "****", "****", "****")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags, targetSelf) in feats)
        {
            var featRow = featRows[(int)featType];
            var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            spellRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();

            spellRow["Range"].Should().Be(range);
            spellRow["TargetType"].Should().Be(targetType);
            spellRow["HostileSetting"].Should().Be(hostileSetting);
            spellRow["TargetShape"].Should().Be(targetShape);
            spellRow["TargetSizeX"].Should().Be(targetSizeX);
            spellRow["TargetSizeY"].Should().Be(targetSizeY);
            spellRow["TargetFlags"].Should().Be(targetFlags);
            featRow["TARGETSELF"].Should().Be(targetSelf);
        }
    }

    private static void AssertPerkLevel(
        PerkDetail perk,
        string name,
        int level,
        int price,
        int? skillRank,
        FeatType? grantedFeat,
        string description,
        params (StatType Stat, int Value)[] statBonuses)
    {
        perk.Name.Should().Be(name);
        var expectedCategory = perk.Type switch
        {
            PerkType.DeflectorShield or
            PerkType.CapacitorRig or
            PerkType.WeaponJam or
            PerkType.PowerCell or
            PerkType.PowerSurge or
            PerkType.RayshieldScreen or
            PerkType.DampeningField or
            PerkType.OverclockRoutine or
            PerkType.GroupDeflector or
            PerkType.EmergencyBunker => PerkCategoryType.DevicesFieldSupport,

            PerkType.Flamethrower or
            PerkType.WristRocket or
            PerkType.SonicBurst or
            PerkType.GadgetHarness or
            PerkType.ArcProjector or
            PerkType.IonLance or
            PerkType.RailDart or
            PerkType.TacticalUplink or
            PerkType.CryoSprayer or
            PerkType.OverloadBarrage => PerkCategoryType.DevicesAssaultGadgets,

            _ => throw new AssertionException($"Unexpected Devices perk type {perk.Type}.")
        };
        perk.Category.Should().Be(expectedCategory);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertCharacterRequirement(perkLevel, CharacterType.Standard);

        if (skillRank.HasValue)
            AssertSkillRequirement(perkLevel, SkillType.Devices, skillRank.Value);
        else
            perkLevel.Requirements.OfType<PerkRequirementSkill>().Should().BeEmpty();

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statBonuses.Length > 0)
        {
            perkLevel.StatBonuses.Should().HaveCount(statBonuses.Length);
            foreach (var (stat, value) in statBonuses)
            {
                AssertStatBonus(perkLevel, stat, value);
            }
        }
        else
        {
            perkLevel.StatBonuses.Should().BeEmpty();
        }
    }

    private static void AssertAbility(
        AbilityDetail ability,
        string name,
        int level,
        RecastGroup recastGroup,
        float recastSeconds,
        float activationSeconds,
        int staminaCost,
        bool isHostile,
        bool isArea,
        bool isSingleTarget,
        bool requiresTarget)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Devices);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(AbilityActivationType.Casted);
        ability.IsHostileAbility.Should().Be(isHostile);
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.IsSingleTargetAbility.Should().Be(isSingleTarget);
        ability.IsAreaAbility.Should().Be(isArea);
        ability.BreaksStealth.Should().BeTrue();

        ability.Requirements
            .OfType<AbilityRequirementStamina>()
            .Should()
            .ContainSingle()
            .Which
            .RequiredSTM
            .Should()
            .Be(staminaCost);
        ability.Requirements.OfType<AbilityRequirementFP>().Should().BeEmpty();
    }

    private static void AssertSkillRequirement(PerkLevel level, SkillType skill, int rank)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementSkill>()
            .Should()
            .ContainSingle()
            .Which;

        requirement.Type.Should().Be(skill);
        requirement.RequiredRank.Should().Be(rank);
    }

    private static void AssertCharacterRequirement(PerkLevel level, CharacterType characterType)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementCharacterType>()
            .Should()
            .ContainSingle()
            .Which;

        typeof(PerkRequirementCharacterType)
            .GetField("_requiredCharacterType", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(requirement)
            .Should()
            .Be(characterType);
    }

    private static void AssertStatBonus(PerkLevel level, StatType statType, int value)
    {
        level.StatBonuses
            .Should()
            .ContainSingle(x => x.Stat == statType)
            .Which
            .Calculate(0)
            .Should()
            .Be(value);
    }

    private static Dictionary<PerkType, PerkDetail> BuildDevicesFieldSupportPerksWithout2daLookup()
    {
        var definition = new DevicesFieldSupportPerkDefinition();
        var methodNames = new[]
        {
            "DeflectorShield",
            "CapacitorRig",
            "WeaponJam",
            "PowerCell",
            "PowerSurge",
            "RayshieldScreen",
            "DampeningField",
            "OverclockRoutine",
            "GroupDeflector",
            "EmergencyBunker"
        };

        return BuildPerksWithout2daLookup(definition, methodNames);
    }

    private static T GetAbilityConstant<T>(Type abilityDefinitionType, string fieldName)
    {
        var field = abilityDefinitionType.GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static);

        field.Should().NotBeNull($"{abilityDefinitionType.Name} should declare {fieldName}");
        field!.IsLiteral.Should().BeTrue();
        return (T)field.GetRawConstantValue()!;
    }

    private static Dictionary<PerkType, PerkDetail> BuildDevicesAssaultGadgetsPerksWithout2daLookup()
    {
        var definition = new DevicesAssaultGadgetsPerkDefinition();
        var methodNames = new[]
        {
            "Flamethrower",
            "WristRocket",
            "SonicBurst",
            "GadgetHarness",
            "ArcProjector",
            "IonLance",
            "RailDart",
            "TacticalUplink",
            "CryoSprayer",
            "OverloadBarrage"
        };

        return BuildPerksWithout2daLookup(definition, methodNames);
    }

    private static Dictionary<PerkType, PerkDetail> BuildPerksWithout2daLookup<T>(T definition, IEnumerable<string> methodNames)
    {
        foreach (var methodName in methodNames)
        {
            typeof(T)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(T)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    private static Dictionary<int, Dictionary<string, string>> Read2da(PathInfo path)
    {
        var lines = File.ReadAllLines(path.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var index = 0; index < header.Length && index + 1 < cells.Length; index++)
            {
                values[header[index]] = cells[index + 1];
            }

            result[row] = values;
        }

        return result;
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "sw_2da", "feat.2da")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}
