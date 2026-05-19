using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition.Devices;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
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
    public void DevicesFieldSupportAndAssaultGadgetsBibleManifest_ContainsBatch()
    {
        var root = FindRepositoryRoot();
        var manifest = File.ReadAllText((root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv").FullName);
        var perkNames = new[]
        {
            "Deflector Shield I", "Capacitor Rig I", "Weapon Jam I", "Power Cell I",
            "Deflector Shield II", "Rayshield Screen I", "Capacitor Rig II", "Dampening Field I",
            "Weapon Jam II", "Power Cell II", "Deflector Shield III", "Rayshield Screen II",
            "Dampening Field II", "Group Deflector", "Capacitor Rig III", "Power Cell III",
            "Emergency Bunker", "Flamethrower I", "Wrist Rocket I", "Sonic Burst I",
            "Gadget Harness I", "Flamethrower II", "Rail Dart I", "Gadget Harness II",
            "Wrist Rocket II", "Sonic Burst II", "Cryo Sprayer I", "Flamethrower III",
            "Rail Dart II", "Wrist Rocket III", "Sonic Burst III", "Gadget Harness III",
            "Cryo Sprayer II", "Overload Barrage"
        };

        foreach (var perkName in perkNames)
        {
            manifest.Should().Contain($"\"{perkName}\"");
        }
    }

    [Test]
    public void DevicesFieldSupportPerkLevels_MatchCombatBible()
    {
        var perks = BuildDevicesFieldSupportPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.DeflectorShield], "Deflector Shield", 1, 2, null, FeatType.DeflectorShield1,
            "Grants one ally 35 temporary HP plus 6% of the target's maximum HP for 45 seconds.");
        AssertPerkLevel(perks[PerkType.CapacitorRig], "Capacitor Rig", 1, 2, 5, null,
            "Deflector Shield, Group Deflector, and Emergency Bunker grant 10% more temporary HP.",
            (StatType.DeviceShieldTemporaryHPPercentAdjustment, 10));
        AssertPerkLevel(perks[PerkType.WeaponJam], "Weapon Jam", 1, 3, 8, FeatType.WeaponJam1,
            "Reduce one target's physical and Force ability Accuracy by 6% for 18 seconds.");
        AssertPerkLevel(perks[PerkType.PowerCell], "Power Cell", 1, 3, 12, FeatType.PowerCell1,
            "Restores 10% of maximum STM to one ally and increases physical and Force ability Accuracy by 4% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.DeflectorShield], "Deflector Shield", 2, 3, 15, FeatType.DeflectorShield2,
            "Grants one ally 65 temporary HP plus 9% of the target's maximum HP for 45 seconds.");
        AssertPerkLevel(perks[PerkType.RayshieldScreen], "Rayshield Screen", 1, 3, 18, FeatType.RayshieldScreen1,
            "Places a 4m screen for 15 seconds. Allies inside take 10% less ranged physical damage.");
        AssertPerkLevel(perks[PerkType.CapacitorRig], "Capacitor Rig", 2, 2, 22, null,
            "Deflector Shield, Group Deflector, and Emergency Bunker grant 20% more temporary HP.",
            (StatType.DeviceShieldTemporaryHPPercentAdjustment, 20));
        AssertPerkLevel(perks[PerkType.DampeningField], "Dampening Field", 1, 4, 25, FeatType.DampeningField1,
            "One ally takes 10% less physical and force damage for 10 seconds.");
        AssertPerkLevel(perks[PerkType.WeaponJam], "Weapon Jam", 2, 3, 28, FeatType.WeaponJam2,
            "Reduce one target's physical and Force ability Accuracy by 10% for 18 seconds.");
        AssertPerkLevel(perks[PerkType.PowerCell], "Power Cell", 2, 4, 30, FeatType.PowerCell2,
            "Restores 18% of maximum STM to one ally and increases physical and Force ability Accuracy by 6% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.DeflectorShield], "Deflector Shield", 3, 3, 35, FeatType.DeflectorShield3,
            "Grants one ally 100 temporary HP plus 12% of the target's maximum HP for 45 seconds.");
        AssertPerkLevel(perks[PerkType.RayshieldScreen], "Rayshield Screen", 2, 3, 38, FeatType.RayshieldScreen2,
            "Places a 4m screen for 18 seconds. Allies inside take 15% less ranged physical damage.");
        AssertPerkLevel(perks[PerkType.DampeningField], "Dampening Field", 2, 4, 40, FeatType.DampeningField2,
            "One ally takes 15% less physical and force damage for 10 seconds.");
        AssertPerkLevel(perks[PerkType.GroupDeflector], "Group Deflector", 1, 4, 42, FeatType.GroupDeflector1,
            "Nearby allies gain 70 temporary HP plus 8% of each target's maximum HP for 30 seconds.");
        AssertPerkLevel(perks[PerkType.CapacitorRig], "Capacitor Rig", 3, 4, 45, null,
            "Deflector Shield, Group Deflector, and Emergency Bunker grant 30% more temporary HP and last 10 seconds longer.",
            (StatType.DeviceShieldTemporaryHPPercentAdjustment, 30),
            (StatType.DeviceShieldDurationBonusSeconds, 10));
        AssertPerkLevel(perks[PerkType.PowerCell], "Power Cell", 3, 3, 48, FeatType.PowerCell3,
            "Restores 18% of maximum STM to nearby allies and increases physical and Force ability Accuracy by 6% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.EmergencyBunker], "Emergency Bunker", 1, 5, 50, FeatType.EmergencyBunker1,
            "Deploys a shield bunker for 15 seconds. Allies inside gain 120 temporary HP plus 10% of each target's maximum HP and take 20% less ranged physical damage.");
    }

    [Test]
    public void DevicesAssaultGadgetsPerkLevels_MatchCombatBible()
    {
        var perks = BuildDevicesAssaultGadgetsPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.Flamethrower], "Flamethrower", 1, 2, null, FeatType.Flamethrower1,
            "Deals fire DMG plus PER scaling to hostile targets in a cone.");
        AssertPerkLevel(perks[PerkType.WristRocket], "Wrist Rocket", 1, 2, 5, FeatType.WristRocket1,
            "Deals fire DMG plus PER scaling to one target.");
        AssertPerkLevel(perks[PerkType.SonicBurst], "Sonic Burst", 1, 3, 8, FeatType.SonicBurst1,
            "Deals 10 sonic DMG to nearby hostile targets and interrupts activation.");
        AssertPerkLevel(perks[PerkType.GadgetHarness], "Gadget Harness", 1, 3, 12, null,
            "Assault Gadget abilities gain +5% Accuracy and +5% critical chance.",
            (StatType.AssaultGadgetAccuracyPercentAdjustment, 5),
            (StatType.AssaultGadgetCriticalRatePercentAdjustment, 5));
        AssertPerkLevel(perks[PerkType.Flamethrower], "Flamethrower", 2, 3, 15, FeatType.Flamethrower2,
            "Deals increased fire DMG plus PER scaling to hostile targets in a cone and attempts to inflict Burning.");
        AssertPerkLevel(perks[PerkType.RailDart], "Rail Dart", 1, 3, 18, FeatType.RailDart1,
            "Fires a dart that deals physical DMG plus PER scaling and attempts to inflict Bleed.");
        AssertPerkLevel(perks[PerkType.GadgetHarness], "Gadget Harness", 2, 2, 22, null,
            "Assault Gadget abilities gain +10% Accuracy and +10% critical chance.",
            (StatType.AssaultGadgetAccuracyPercentAdjustment, 10),
            (StatType.AssaultGadgetCriticalRatePercentAdjustment, 10));
        AssertPerkLevel(perks[PerkType.WristRocket], "Wrist Rocket", 2, 4, 25, FeatType.WristRocket2,
            "Deals increased fire DMG plus PER scaling to one target and knock down for 2 seconds.");
        AssertPerkLevel(perks[PerkType.SonicBurst], "Sonic Burst", 2, 3, 28, FeatType.SonicBurst2,
            "Deals 14 sonic DMG to nearby hostile targets, interrupts activation, and reduces Accuracy by 6% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.CryoSprayer], "Cryo Sprayer", 1, 4, 30, FeatType.CryoSprayer1,
            "Deals ice DMG plus PER scaling to hostile targets in a cone and slows movement for 5 seconds.");
        AssertPerkLevel(perks[PerkType.Flamethrower], "Flamethrower", 3, 3, 35, FeatType.Flamethrower3,
            "Deals high fire DMG plus PER scaling to hostile targets in a cone and attempts to inflict Burning.");
        AssertPerkLevel(perks[PerkType.RailDart], "Rail Dart", 2, 3, 38, FeatType.RailDart2,
            "Fires a dart that deals high physical DMG plus PER scaling and attempts to inflict Bleed.");
        AssertPerkLevel(perks[PerkType.WristRocket], "Wrist Rocket", 3, 4, 40, FeatType.WristRocket3,
            "Deals high fire DMG plus PER scaling to one target and knock down for 3 seconds.");
        AssertPerkLevel(perks[PerkType.SonicBurst], "Sonic Burst", 3, 4, 42, FeatType.SonicBurst3,
            "Deals 18 sonic DMG to nearby hostile targets, interrupts activation, and reduces Accuracy by 10% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.GadgetHarness], "Gadget Harness", 3, 4, 45, null,
            "Assault Gadget abilities gain +15% Accuracy, +15% critical chance, and +10% damage.",
            (StatType.AssaultGadgetAccuracyPercentAdjustment, 15),
            (StatType.AssaultGadgetCriticalRatePercentAdjustment, 15),
            (StatType.AssaultGadgetDamagePercentAdjustment, 10));
        AssertPerkLevel(perks[PerkType.CryoSprayer], "Cryo Sprayer", 2, 3, 48, FeatType.CryoSprayer2,
            "Deals high ice DMG plus PER scaling to hostile targets in a cone and immobilize for 2 seconds.");
        AssertPerkLevel(perks[PerkType.OverloadBarrage], "Overload Barrage", 1, 5, 50, FeatType.OverloadBarrage1,
            "Unleashes three attacks at your primary target's location: 18 fire DMG in a 5m burst plus Burn for 12 seconds, 20 fire DMG to the primary target plus 3-second knockdown, and 18 sonic DMG in a 5m burst that interrupts activation and reduces Accuracy by 10% for 12 seconds.");
    }

    [Test]
    public void DevicesFieldSupportAndAssaultGadgetsAbilities_MatchCombatBible()
    {
        var deflectorShield = new DeflectorShieldAbilityDefinition().BuildAbilities();
        AssertAbility(deflectorShield[FeatType.DeflectorShield1], "Deflector Shield I", 1, RecastGroup.DeflectorShield, 45f, 1f, 3, false, false, true, true);
        AssertAbility(deflectorShield[FeatType.DeflectorShield2], "Deflector Shield II", 2, RecastGroup.DeflectorShield, 45f, 1f, 4, false, false, true, true);
        AssertAbility(deflectorShield[FeatType.DeflectorShield3], "Deflector Shield III", 3, RecastGroup.DeflectorShield, 45f, 1f, 6, false, false, true, true);

        var weaponJam = new WeaponJamAbilityDefinition().BuildAbilities();
        AssertAbility(weaponJam[FeatType.WeaponJam1], "Weapon Jam I", 1, RecastGroup.WeaponJam, 24f, 1f, 4, true, false, true, true);
        AssertAbility(weaponJam[FeatType.WeaponJam2], "Weapon Jam II", 2, RecastGroup.WeaponJam, 24f, 1f, 5, true, false, true, true);

        var powerCell = new PowerCellAbilityDefinition().BuildAbilities();
        AssertAbility(powerCell[FeatType.PowerCell1], "Power Cell I", 1, RecastGroup.PowerCell, 45f, 1f, 4, false, false, true, true);
        AssertAbility(powerCell[FeatType.PowerCell2], "Power Cell II", 2, RecastGroup.PowerCell, 45f, 1f, 5, false, false, true, true);
        AssertAbility(powerCell[FeatType.PowerCell3], "Power Cell III", 3, RecastGroup.PowerCell, 60f, 1.5f, 7, false, true, false, false);

        var rayshield = new RayshieldScreenAbilityDefinition().BuildAbilities();
        AssertAbility(rayshield[FeatType.RayshieldScreen1], "Rayshield Screen I", 1, RecastGroup.RayshieldScreen, 75f, 1.5f, 5, false, true, false, false);
        AssertAbility(rayshield[FeatType.RayshieldScreen2], "Rayshield Screen II", 2, RecastGroup.RayshieldScreen, 75f, 1.5f, 6, false, true, false, false);

        var dampening = new DampeningFieldAbilityDefinition().BuildAbilities();
        AssertAbility(dampening[FeatType.DampeningField1], "Dampening Field I", 1, RecastGroup.DampeningField, 60f, 1f, 5, false, false, true, true);
        AssertAbility(dampening[FeatType.DampeningField2], "Dampening Field II", 2, RecastGroup.DampeningField, 60f, 1f, 7, false, false, true, true);

        var groupDeflector = new GroupDeflectorAbilityDefinition().BuildAbilities();
        AssertAbility(groupDeflector[FeatType.GroupDeflector1], "Group Deflector", 1, RecastGroup.GroupDeflector, 90f, 1.5f, 8, false, true, false, false);

        var emergencyBunker = new EmergencyBunkerAbilityDefinition().BuildAbilities();
        AssertAbility(emergencyBunker[FeatType.EmergencyBunker1], "Emergency Bunker", 1, RecastGroup.EmergencyBunker, 180f, 2f, 10, false, true, false, false);

        var flamethrower = new FlamethrowerAbilityDefinition().BuildAbilities();
        AssertAbility(flamethrower[FeatType.Flamethrower1], "Flamethrower I", 1, RecastGroup.Flamethrower, 12f, 1f, 3, true, true, false, false);
        AssertAbility(flamethrower[FeatType.Flamethrower2], "Flamethrower II", 2, RecastGroup.Flamethrower, 12f, 1f, 4, true, true, false, false);
        AssertAbility(flamethrower[FeatType.Flamethrower3], "Flamethrower III", 3, RecastGroup.Flamethrower, 12f, 1f, 5, true, true, false, false);

        var wristRocket = new WristRocketAbilityDefinition().BuildAbilities();
        AssertAbility(wristRocket[FeatType.WristRocket1], "Wrist Rocket I", 1, RecastGroup.WristRocket, 18f, 0.5f, 2, true, false, true, true);
        AssertAbility(wristRocket[FeatType.WristRocket2], "Wrist Rocket II", 2, RecastGroup.WristRocket, 18f, 0.5f, 4, true, false, true, true);
        AssertAbility(wristRocket[FeatType.WristRocket3], "Wrist Rocket III", 3, RecastGroup.WristRocket, 18f, 0.5f, 5, true, false, true, true);
        wristRocket[FeatType.WristRocket1].ImpactAnimationType.Should().Be(Animation.CastOutAnimation);
        wristRocket[FeatType.WristRocket2].ImpactAnimationType.Should().Be(Animation.CastOutAnimation);
        wristRocket[FeatType.WristRocket3].ImpactAnimationType.Should().Be(Animation.CastOutAnimation);

        var sonicBurst = new SonicBurstAbilityDefinition().BuildAbilities();
        AssertAbility(sonicBurst[FeatType.SonicBurst1], "Sonic Burst I", 1, RecastGroup.SonicBurst, 30f, 1f, 4, true, true, false, false);
        AssertAbility(sonicBurst[FeatType.SonicBurst2], "Sonic Burst II", 2, RecastGroup.SonicBurst, 30f, 1f, 5, true, true, false, false);
        AssertAbility(sonicBurst[FeatType.SonicBurst3], "Sonic Burst III", 3, RecastGroup.SonicBurst, 30f, 1f, 6, true, true, false, false);

        var railDart = new RailDartAbilityDefinition().BuildAbilities();
        AssertAbility(railDart[FeatType.RailDart1], "Rail Dart I", 1, RecastGroup.RailDart, 18f, 1f, 3, true, false, true, true);
        AssertAbility(railDart[FeatType.RailDart2], "Rail Dart II", 2, RecastGroup.RailDart, 18f, 1f, 4, true, false, true, true);

        var cryoSprayer = new CryoSprayerAbilityDefinition().BuildAbilities();
        AssertAbility(cryoSprayer[FeatType.CryoSprayer1], "Cryo Sprayer I", 1, RecastGroup.CryoSprayer, 24f, 1f, 5, true, true, false, false);
        AssertAbility(cryoSprayer[FeatType.CryoSprayer2], "Cryo Sprayer II", 2, RecastGroup.CryoSprayer, 24f, 1f, 7, true, true, false, false);

        var overload = new OverloadBarrageAbilityDefinition().BuildAbilities();
        AssertAbility(overload[FeatType.OverloadBarrage1], "Overload Barrage", 1, RecastGroup.OverloadBarrage, 120f, 1.5f, 10, true, false, true, true);
    }

    [Test]
    public void DevicesFieldSupportAndAssaultGadgetsStatuses_MatchCombatBible()
    {
        new WeaponJam1StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(-6);
        new WeaponJam2StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(-10);
        new PowerCell1StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(4);
        new PowerCell2StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(6);
        new PowerCell3StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(6);
        new RayshieldScreen1StatusEffect().StatGroup.Stats[StatType.RangedPhysicalDamageTakenPercentAdjustment].Should().Be(-10);
        new RayshieldScreen2StatusEffect().StatGroup.Stats[StatType.RangedPhysicalDamageTakenPercentAdjustment].Should().Be(-15);
        new DampeningField1StatusEffect().StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(-10);
        new DampeningField1StatusEffect().StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-10);
        new DampeningField2StatusEffect().StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(-15);
        new DampeningField2StatusEffect().StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-15);
        new EmergencyBunker1StatusEffect().StatGroup.Stats[StatType.RangedPhysicalDamageTakenPercentAdjustment].Should().Be(-20);

        new SonicBurst2StatusEffect().StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-6);
        new SonicBurst3StatusEffect().StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-10);

        CombatDamageType.Sonic.GetDetails().NWScriptDamageType.Should().Be(DamageType.Sonic);
    }

    [Test]
    public void DevicesFieldSupportAndAssaultGadgetsSources_IncludeBibleBehavior()
    {
        var root = FindRepositoryRoot();
        var deflector = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "DeflectorShieldAbilityDefinition.cs").FullName);
        deflector.Should().Contain("ApplyShieldTemporaryHP(activator, friendly, 35, 6, 45f)");
        deflector.Should().Contain("ApplyShieldTemporaryHP(activator, friendly, 65, 9, 45f)");
        deflector.Should().Contain("ApplyShieldTemporaryHP(activator, friendly, 100, 12, 45f)");
        deflector.Should().Contain("TemporaryHitPointEffects.ApplyFlatWithBarrierVisual(target, amount, duration)");
        deflector.Should().Contain("DeviceAbilityEffects.ApplyCapacitorRigBonus");
        deflector.Should().NotContain("HealPercent");
        deflector.Should().NotContain("EffectHeal");

        var groupDeflector = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "GroupDeflectorAbilityDefinition.cs").FullName);
        groupDeflector.Should().Contain("ApplyShieldTemporaryHP(activator, friendly, 70, 8, 30f)");
        groupDeflector.Should().Contain("DeviceAbilityEffects.ApplyCapacitorRigDurationBonus");
        groupDeflector.Should().NotContain("HealPercent");

        var emergencyBunker = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "EmergencyBunkerAbilityDefinition.cs").FullName);
        emergencyBunker.Should().Contain("(friendly, remainingDuration) => ApplyBunkerTemporaryHP(activator, friendly, remainingDuration)");
        emergencyBunker.Should().Contain("DeviceAbilityEffects.ApplyCapacitorRigBonus");

        var areaEffects = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "AbilityAreaEffects.cs").FullName);
        areaEffects.Should().Contain("Action<uint, float> onFirstApplication = null");
        areaEffects.Should().Contain("firstApplications.Add(friendly)");
        areaEffects.Should().Contain("durationSeconds - pulseDelay");

        var temporaryHitPointEffects = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "TemporaryHitPointEffects.cs").FullName);
        temporaryHitPointEffects.Should().Contain("Vfx_Dur_Aura_Pulse_Cyan_Blue");
        temporaryHitPointEffects.Should().Contain("RemoveEffectByTag(target, BarrierVisualEffectTag)");

        var deviceEffects = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "DeviceAbilityEffects.cs").FullName);
        deviceEffects.Should().Contain("AssaultGadgetAccuracyPercentAdjustment");
        deviceEffects.Should().Contain("AssaultGadgetCriticalRatePercentAdjustment");
        deviceEffects.Should().Contain("AssaultGadgetDamagePercentAdjustment");

        var railDart = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "RailDartAbilityDefinition.cs").FullName);
        railDart.Should().Contain("CombatDamageType.Physical");
        railDart.Should().NotContain("CombatDamageType.Fire");

        var sonicBurst = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "SonicBurstAbilityDefinition.cs").FullName);
        sonicBurst.Should().Contain("CombatDamageType.Sonic");
        sonicBurst.Should().Contain("typeof(SonicBurst2StatusEffect)");
        sonicBurst.Should().Contain("typeof(SonicBurst3StatusEffect)");
        sonicBurst.Should().Contain("afterSuccessfulHit: InterruptActivation");

        var overload = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "OverloadBarrageAbilityDefinition.cs").FullName);
        overload.Should().Contain("typeof(BurnStatusEffect)");
        overload.Should().Contain("typeof(KnockdownStatusEffect)");
        overload.Should().Contain("typeof(SonicBurst3StatusEffect)");
        overload.Should().Contain("CombatDamageType.Sonic");
    }

    [Test]
    public void DevicesFieldSupportAndAssaultGadgetsFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");
        var feats = new[]
        {
            (FeatType.DeflectorShield1, "ife_dflctrshld1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.WeaponJam1, "ife_wpnjm1", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.PowerCell1, "ife_pwrcll1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.DeflectorShield2, "ife_dflctrshld2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.RayshieldScreen1, "ife_ryshldscrn1", "M", "0x3E", "0", "sphere", "4", "****", "1", "****"),
            (FeatType.DampeningField1, "ife_dmpnngfld1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.WeaponJam2, "ife_wpnjm2", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.PowerCell2, "ife_pwrcll2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.DeflectorShield3, "ife_dflctrshld3", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.RayshieldScreen2, "ife_ryshldscrn2", "M", "0x3E", "0", "sphere", "4", "****", "1", "****"),
            (FeatType.DampeningField2, "ife_dmpnngfld2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.GroupDeflector1, "ife_grpdflctr1", "P", "0x01", "0", "sphere", "5", "****", "17", "1"),
            (FeatType.PowerCell3, "ife_pwrcll3", "P", "0x01", "0", "sphere", "5", "****", "17", "1"),
            (FeatType.EmergencyBunker1, "ife_mrgncybnkr1", "M", "0x3E", "0", "sphere", "4", "****", "1", "****"),
            (FeatType.Flamethrower1, "ife_flmthrwr1", "M", "0x3E", "1", "cone", "6", "5", "17", "****"),
            (FeatType.WristRocket1, "ife_wrstrckt1", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.SonicBurst1, "ife_sncburst1", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.Flamethrower2, "ife_flmthrwr2", "M", "0x3E", "1", "cone", "6", "5", "17", "****"),
            (FeatType.RailDart1, "ife_rldrt1", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.WristRocket2, "ife_wrstrckt2", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.SonicBurst2, "ife_sncburst2", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.CryoSprayer1, "ife_cryspryr1", "M", "0x3E", "1", "cone", "6", "5", "17", "****"),
            (FeatType.Flamethrower3, "ife_flmthrwr3", "M", "0x3E", "1", "cone", "6", "5", "17", "****"),
            (FeatType.RailDart2, "ife_rldrt2", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.WristRocket3, "ife_wrstrckt3", "M", "0x02", "1", "****", "****", "****", "****", "****"),
            (FeatType.SonicBurst3, "ife_sncburst3", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.CryoSprayer2, "ife_cryspryr2", "M", "0x3E", "1", "cone", "6", "5", "17", "****"),
            (FeatType.OverloadBarrage1, "ife_ovldbarr1", "M", "0x02", "1", "****", "****", "****", "****", "****")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags, targetSelf) in feats)
        {
            var featRow = featRows[(int)featType];
            var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            spellRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

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
            PerkType.RayshieldScreen or
            PerkType.DampeningField or
            PerkType.GroupDeflector or
            PerkType.EmergencyBunker => PerkCategoryType.DevicesFieldSupport,

            PerkType.Flamethrower or
            PerkType.WristRocket or
            PerkType.SonicBurst or
            PerkType.GadgetHarness or
            PerkType.RailDart or
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
            "RayshieldScreen",
            "DampeningField",
            "GroupDeflector",
            "EmergencyBunker"
        };

        return BuildPerksWithout2daLookup(definition, methodNames);
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
            "RailDart",
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
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "swlor2_2da", "feat.2da")))
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
