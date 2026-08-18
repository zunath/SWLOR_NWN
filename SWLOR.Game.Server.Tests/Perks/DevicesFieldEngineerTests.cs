using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.Devices;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class DevicesFieldEngineerTests
{
    [Test]
    public void DevicesFieldEngineerPerkLevels_MatchCombatBible()
    {
        var perks = BuildDevicesFieldEngineerPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.BlasterBeacon], "Blaster Beacon", 1, 3, null, FeatType.BlasterBeacon1,
            "Plants a visible 12m targeting sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit by an automated ranged energy pulse for 3 physical DMG plus PER scaling.");
        AssertPerkLevel(perks[PerkType.BeaconTargeting], "Beacon Targeting", 1, 3, 5, FeatType.BeaconTargetingTrait,
            "Beacon pulses gain +4% damage and +1m pulse range.",
            (StatType.BeaconPulseDamagePercentAdjustment, 4),
            (StatType.BeaconPulseRangeBonusMeters, 1));
        AssertPerkLevel(perks[PerkType.IncendiaryField], "Incendiary Field", 1, 3, 8, FeatType.IncendiaryField1,
            "Deploys a visible 5m-radius fire field at the target location for 30 seconds. Enemies inside take 8 fire DMG plus PER scaling every 3 seconds.");
        AssertPerkLevel(perks[PerkType.RemoteCharge], "Remote Charge", 1, 3, 12, FeatType.RemoteCharge1,
            "Arms a visible charge at your target location that detonates after 3 seconds in a 5m-radius blast for 30 fire DMG plus PER scaling.");
        AssertPerkLevel(perks[PerkType.BlasterBeacon], "Blaster Beacon", 2, 3, 15, FeatType.BlasterBeacon2,
            "Plants a visible 12m targeting sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit by an automated ranged energy pulse for 6 physical DMG plus PER scaling.");
        AssertPerkLevel(perks[PerkType.SignalJammer], "Signal Jammer", 1, 4, 18, FeatType.SignalJammer1,
            "Deploys a signal jammer for 45 seconds. Hostile targets within 5m suffer -6% physical and Force ability Accuracy and cannot benefit from Haste while inside.");
        AssertPerkLevel(perks[PerkType.ShockBeacon], "Shock Beacon", 1, 4, 22, FeatType.ShockBeacon1,
            "Plants a visible 5m shock sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit for 10 electrical DMG plus PER scaling and suffers Shock.");
        AssertPerkLevel(perks[PerkType.IncendiaryField], "Incendiary Field", 2, 4, 25, FeatType.IncendiaryField2,
            "Deploys a visible 5m-radius fire field at the target location for 30 seconds. Enemies inside take 12 fire DMG plus PER scaling every 3 seconds.");
        AssertPerkLevel(perks[PerkType.RemoteCharge], "Remote Charge", 2, 4, 28, FeatType.RemoteCharge2,
            "Arms a visible charge at your target location that detonates after 3 seconds in a 5m-radius blast for 42 fire DMG plus PER scaling and inflicts Knockdown for 6 seconds.");
        AssertPerkLevel(perks[PerkType.BlasterBeacon], "Blaster Beacon", 3, 4, 30, FeatType.BlasterBeacon3,
            "Plants a visible 14m targeting sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit by an automated ranged energy pulse for 10 physical DMG plus PER scaling.");
        AssertPerkLevel(perks[PerkType.DiagnosticSweep], "Diagnostic Sweep", 1, 5, 35, FeatType.DiagnosticSweepTrait,
            "Field Engineer beacons, fields, charges, and jammers reveal hidden enemies in their affected area and reduce Evasion by 4% for 30 seconds.",
            (StatType.FieldEngineerAreaRevealHidden, 1),
            (StatType.FieldEngineerAreaEvasionPenaltyPercent, 4),
            (StatType.FieldEngineerAreaEvasionPenaltyDurationSeconds, 30));
        AssertPerkLevel(perks[PerkType.ShockBeacon], "Shock Beacon", 2, 5, 38, FeatType.ShockBeacon2,
            "Plants a visible 5m shock sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit for 14 electrical DMG plus PER scaling and suffers Shock.");
        AssertPerkLevel(perks[PerkType.BeaconTargeting], "Beacon Targeting", 2, 5, 42, null,
            "Beacon pulses gain +8% damage and +2m pulse range.",
            (StatType.BeaconPulseDamagePercentAdjustment, 8),
            (StatType.BeaconPulseRangeBonusMeters, 2));
        AssertPerkLevel(perks[PerkType.IncendiaryField], "Incendiary Field", 3, 5, 45, FeatType.IncendiaryField3,
            "Deploys a visible 5m-radius fire field at the target location for 30 seconds. Enemies inside take 16 fire DMG plus PER scaling every 3 seconds.");
        AssertPerkLevel(perks[PerkType.KillzoneBeacon], "Killzone Beacon", 1, 5, 50, FeatType.KillzoneBeacon1,
            "Plants a visible 12m killzone sphere for 45 seconds. Every 3 seconds, all hostile targets inside are hit by one 16 physical DMG plus PER scaling pulse and one 16 electrical DMG plus PER scaling shock pulse.");
    }

    [Test]
    public void DevicesFieldEngineerAbilities_MatchCombatBible()
    {
        var blasterBeacon = new BlasterBeaconAbilityDefinition().BuildAbilities();
        AssertAbility(blasterBeacon[FeatType.BlasterBeacon1], "Blaster Beacon I", 1, RecastGroup.BlasterBeacon, 45f, 1.5f, 3, true, true, false, false);
        AssertAbility(blasterBeacon[FeatType.BlasterBeacon2], "Blaster Beacon II", 2, RecastGroup.BlasterBeacon, 45f, 1.5f, 4, true, true, false, false);
        AssertAbility(blasterBeacon[FeatType.BlasterBeacon3], "Blaster Beacon III", 3, RecastGroup.BlasterBeacon, 45f, 1.5f, 6, true, true, false, false);
        AssertBeaconTargetingResolver(blasterBeacon[FeatType.BlasterBeacon1]);
        AssertBeaconTargetingResolver(blasterBeacon[FeatType.BlasterBeacon2]);
        AssertBeaconTargetingResolver(blasterBeacon[FeatType.BlasterBeacon3]);

        var incendiaryField = new IncendiaryFieldAbilityDefinition().BuildAbilities();
        AssertAbility(incendiaryField[FeatType.IncendiaryField1], "Incendiary Field I", 1, RecastGroup.IncendiaryField, 30f, 1.5f, 4, true, true, false, false);
        AssertAbility(incendiaryField[FeatType.IncendiaryField2], "Incendiary Field II", 2, RecastGroup.IncendiaryField, 30f, 1.5f, 5, true, true, false, false);
        AssertAbility(incendiaryField[FeatType.IncendiaryField3], "Incendiary Field III", 3, RecastGroup.IncendiaryField, 30f, 1.5f, 7, true, true, false, false);

        var remoteCharge = new RemoteChargeAbilityDefinition().BuildAbilities();
        AssertAbility(remoteCharge[FeatType.RemoteCharge1], "Remote Charge I", 1, RecastGroup.RemoteCharge, 18f, 1f, 4, true, true, false, false);
        AssertAbility(remoteCharge[FeatType.RemoteCharge2], "Remote Charge II", 2, RecastGroup.RemoteCharge, 18f, 1f, 5, true, true, false, false);

        var signalJammer = new SignalJammerAbilityDefinition().BuildAbilities();
        AssertAbility(signalJammer[FeatType.SignalJammer1], "Signal Jammer", 1, RecastGroup.SignalJammer, 24f, 1.5f, 4, true, true, false, false);

        var shockBeacon = new ShockBeaconAbilityDefinition().BuildAbilities();
        AssertAbility(shockBeacon[FeatType.ShockBeacon1], "Shock Beacon I", 1, RecastGroup.ShockBeacon, 36f, 1.5f, 5, true, true, false, false);
        AssertAbility(shockBeacon[FeatType.ShockBeacon2], "Shock Beacon II", 2, RecastGroup.ShockBeacon, 36f, 1.5f, 6, true, true, false, false);
        AssertBeaconTargetingResolver(shockBeacon[FeatType.ShockBeacon1]);
        AssertBeaconTargetingResolver(shockBeacon[FeatType.ShockBeacon2]);

        var killzoneBeacon = new KillzoneBeaconAbilityDefinition().BuildAbilities();
        AssertAbility(killzoneBeacon[FeatType.KillzoneBeacon1], "Killzone Beacon", 1, RecastGroup.Capstone, 90f, 2f, 15, true, true, false, false);
        AssertBeaconTargetingResolver(killzoneBeacon[FeatType.KillzoneBeacon1]);
    }

    [Test]
    public void DevicesFieldEngineerSources_IncludeBibleBehavior()
    {
        var root = FindRepositoryRoot();
        var effects = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "DeviceAbilityEffects.cs").FullName);
        effects.Should().Contain("BeaconPulseDamagePercentAdjustment");
        effects.Should().Contain("BeaconPulseRangeBonusMeters");
        effects.Should().Contain("ApplyBeaconPulseRangeBonus");
        effects.Should().Contain("resolvesHit: false");
        effects.Should().Contain("canCritical: false");
        effects.Should().NotContain("BeaconPulseAccuracyPercentAdjustment");
        effects.Should().NotContain("BeaconPulseCriticalRatePercentAdjustment");
        effects.Should().Contain("ApplyDiagnosticSweep");
        effects.Should().Contain("FieldEngineerAreaRevealHidden");
        effects.Should().Contain("FieldEngineerAreaEvasionPenaltyPercent");
        effects.Should().Contain("ScheduleNextFieldEngineerPulse");
        effects.Should().Contain("FieldEngineerPulseMarkerResref = \"_mdrn_pl_emitter\"");
        effects.Should().Contain("CreateObject(");
        effects.Should().Contain("ObjectType.Placeable");
        effects.Should().Contain("DestroyObject(emitter.MarkerObject)");
        effects.Should().Contain("CreateTemporaryFieldEngineerMarker");
        effects.Should().Contain("appliesBeaconPulseBonuses");
        effects.Should().Contain("CreatePersistentSphereIndicator");
        effects.Should().Contain("Telegraph.CancelTelegraph(emitter.AreaIndicatorId)");

        var killzoneBeaconSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "KillzoneBeaconAbilityDefinition.cs").FullName);
        killzoneBeaconSource.Should().Contain("showAreaIndicator: false",
            "the second damage channel must reuse the killzone's single persistent boundary");

        var signalJammer = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "SignalJammerAbilityDefinition.cs").FullName);
        signalJammer.Should().Contain("typeof(SignalJammerStatusEffect)");
        signalJammer.Replace("\r\n", "\n").Should().Contain("0,\n                3,\n                typeof(SignalJammerStatusEffect),\n                RadiusMeters,\n                DurationSeconds");
        signalJammer.Should().Contain("markerVisualEffect: VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue");

        var remoteCharge = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "RemoteChargeAbilityDefinition.cs").FullName);
        remoteCharge.Should().Contain("DetonateRemoteCharge(activator, target, targetLocation, 30, null)");
        remoteCharge.Should().Contain("DetonateRemoteCharge(activator, target, targetLocation, 42, typeof(KnockdownStatusEffect))");
        remoteCharge.Should().Contain("CreateTemporaryFieldEngineerMarker");
        remoteCharge.Should().Contain("RemoteChargeMarkerResref = \"_mdrn_pl_detonat\"",
            "an armed remote charge must use a placeable distinct from the beacon emitter");
        remoteCharge.Should().Contain("VisualEffect.Vfx_Dur_Aura_Pulse_Red_Orange");
        remoteCharge.Should().Contain("ApplyDiagnosticSweep");
        remoteCharge.Should().Contain("CombatImpactAreaShape.Sphere");
        remoteCharge.Should().Contain("3f");
        remoteCharge.Should().Contain("CombatDamageType.Fire");
        remoteCharge.Should().Contain("typeof(KnockdownStatusEffect)");
        remoteCharge.Should().Contain("alwaysApplyAreaVisualEffect: true");

        var incendiaryField = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "IncendiaryFieldAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");
        incendiaryField.Should().Contain("IncendiaryFieldTargetVisualEffect = VisualEffect.Vfx_Imp_Flame_S");
        incendiaryField.Should().Contain("EffectAreaOfEffect(AreaOfEffect.IncendiaryFieldCloud)");
        incendiaryField.Should().NotContain("Vfx_Dur_Aura_Fire",
            "the field visual is the live-server incendiary grenade fire fog cloud, not a body aura");
        incendiaryField.Should().Contain("DeployIncendiaryField(activator, target, targetLocation, 8)");
        incendiaryField.Should().Contain("DeployIncendiaryField(activator, target, targetLocation, 12)");
        incendiaryField.Should().Contain("DeployIncendiaryField(activator, target, targetLocation, 16)");
        incendiaryField.Should().NotContain("VisualEffect.Fnf_Fireball");

        var killzoneBeacon = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "KillzoneBeaconAbilityDefinition.cs").FullName);
        killzoneBeacon.Should().Contain("16");
        killzoneBeacon.Should().Contain("ScheduleAreaHostilePulses");
        killzoneBeacon.Should().Contain("CombatDamageType.Physical");
        killzoneBeacon.Should().Contain("CombatDamageType.Electrical");
        killzoneBeacon.Should().Contain("typeof(ShockStatusEffect)");
        killzoneBeacon.Should().Contain("CapstoneAbility.ActiveDurationSeconds");
        killzoneBeacon.Should().Contain("markerVisualEffect: VisualEffect.Vfx_Dur_Aura_Pulse_Red_Blue");
        killzoneBeacon.Should().Contain("markerVisualEffectScale: 4.8f");
        killzoneBeacon.Should().Contain("VisualEffect.Vfx_Imp_Lightning_M");
        killzoneBeacon.Should().Contain("VisualEffect.Vfx_Imp_Mirv_Electric");
        killzoneBeacon.Should().Contain("appliesBeaconPulseBonuses: true");

        var blasterBeacon = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "BlasterBeaconAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");
        blasterBeacon.Should().Contain("VisualEffect.Vfx_Imp_Flame_S");
        blasterBeacon.Should().Contain("DeviceAbilityEffects.ApplyBeaconPulseRangeBonus");
        blasterBeacon.Should().Contain("3,\n                0,\n                null,\n                12f,\n                30f,\n                CombatDamageType.Physical");
        blasterBeacon.Should().Contain("6,\n                0,\n                null,\n                12f,\n                30f,\n                CombatDamageType.Physical");
        blasterBeacon.Should().Contain("10,\n                0,\n                null,\n                14f,\n                30f,\n                CombatDamageType.Physical");
        blasterBeacon.Should().Contain("markerVisualEffectScale: 4.8f");
        blasterBeacon.Should().Contain("markerVisualEffectScale: 5.6f");

        var shockBeacon = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "ShockBeaconAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");
        shockBeacon.Should().Contain("DeviceAbilityEffects.ApplyBeaconPulseRangeBonus");
        shockBeacon.Should().Contain("10,\n                6,\n                typeof(ShockStatusEffect),\n                5f,\n                30f,\n                CombatDamageType.Electrical");
        shockBeacon.Should().Contain("14,\n                6,\n                typeof(ShockStatusEffect),\n                5f,\n                30f,\n                CombatDamageType.Electrical");
        shockBeacon.Should().Contain("markerVisualEffectScale: 2f");
    }

    [Test]
    public void IncendiaryFieldPersistentVfx_IsVisualOnlyFireCloud()
    {
        var root = FindRepositoryRoot();
        var persistentVfx = Read2da(root / "SWLOR_Haks" / "sw_2da" / "vfx_persistent.2da");
        var row = persistentVfx[(int)AreaOfEffect.IncendiaryFieldCloud];

        row["LABEL"].Should().Be("AOE_INCENDIARY_FIELD_CLOUD");
        row["SHAPE"].Should().Be("C");
        row["RADIUS"].Should().Be("5");
        row["ONENTER"].Should().Be("****",
            "the cloud must not run the base game fire cloud enter script; damage comes from the scheduled pulses");
        row["ONEXIT"].Should().Be("****");
        row["HEARTBEAT"].Should().Be("****",
            "the cloud must not run the base game fire cloud heartbeat script; damage comes from the scheduled pulses");
        row["MODEL01"].Should().Be("vps_fogfire");
        row["MODEL02"].Should().Be("vps_fogfire");
        row["MODEL03"].Should().Be("vps_fogfire");
    }

    [Test]
    public void DevicesFieldEngineerBeaconTargetSelection_RequiresLineOfSight()
    {
        var root = FindRepositoryRoot();
        var effects = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "DeviceAbilityEffects.cs").FullName);

        effects.Should().Contain("GetFirstObjectInShape(Shape.Sphere, radius, location, true, ObjectType.Creature)");
        effects.Should().Contain("GetNextObjectInShape(Shape.Sphere, radius, location, true, ObjectType.Creature)");
        effects.Should().Contain("!GetIsDead(creature)");
        effects.Should().Contain("GetCurrentHitPoints(creature) > 0");
        effects.Should().NotContain("GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth)");
    }

    [Test]
    public void DevicesFieldEngineerFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.BlasterBeacon1, "ife_blstrbcn1", "M", "0x3E", "1", "sphere", "12", "****", "1", "****"),
            (FeatType.IncendiaryField1, "ife_ncndryfld1", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.RemoteCharge1, "ife_rmtchrg1", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.BlasterBeacon2, "ife_blstrbcn2", "M", "0x3E", "1", "sphere", "12", "****", "1", "****"),
            (FeatType.SignalJammer1, "ife_sgnljam1", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.ShockBeacon1, "ife_shokbcn1", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.IncendiaryField2, "ife_ncndryfld2", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.RemoteCharge2, "ife_rmtchrg2", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.BlasterBeacon3, "ife_blstrbcn3", "M", "0x3E", "1", "sphere", "14", "****", "1", "****"),
            (FeatType.ShockBeacon2, "ife_shokbcn2", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.IncendiaryField3, "ife_ncndryfld3", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.KillzoneBeacon1, "ife_kllznbcn1", "M", "0x3E", "1", "sphere", "12", "****", "1", "****")
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

    [Test]
    public void DevicesFieldEngineerFeatAndAbilityDescriptions_MatchCombatBible()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");
        var tlkEntries = ReadTlkEntries(root / "SWLOR_Haks" / "sw_tlk" / "sw_tlk.tlk.json");
        const int CustomTlkOffset = 16777216;
        var descriptions = new[]
        {
            (FeatType.BlasterBeacon1, "Plants a visible 12m targeting sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit by an automated ranged energy pulse for 3 physical DMG plus PER scaling."),
            (FeatType.BlasterBeacon2, "Plants a visible 12m targeting sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit by an automated ranged energy pulse for 6 physical DMG plus PER scaling."),
            (FeatType.BlasterBeacon3, "Plants a visible 14m targeting sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit by an automated ranged energy pulse for 10 physical DMG plus PER scaling."),
            (FeatType.RemoteCharge1, "Arms a visible charge at your target location that detonates after 3 seconds in a 5m-radius blast for 30 fire DMG plus PER scaling."),
            (FeatType.RemoteCharge2, "Arms a visible charge at your target location that detonates after 3 seconds in a 5m-radius blast for 42 fire DMG plus PER scaling and inflicts Knockdown for 6 seconds."),
            (FeatType.ShockBeacon1, "Plants a visible 5m shock sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit for 10 electrical DMG plus PER scaling and suffers Shock."),
            (FeatType.ShockBeacon2, "Plants a visible 5m shock sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit for 14 electrical DMG plus PER scaling and suffers Shock."),
            (FeatType.KillzoneBeacon1, "Plants a visible 12m killzone sphere for 45 seconds. Every 3 seconds, all hostile targets inside are hit by one 16 physical DMG plus PER scaling pulse and one 16 electrical DMG plus PER scaling shock pulse.")
        };

        foreach (var (featType, expectedDescription) in descriptions)
        {
            var featRow = featRows[(int)featType];
            var featDescriptionId = int.Parse(featRow["DESCRIPTION"]) - CustomTlkOffset;
            tlkEntries[featDescriptionId].Should().Be(expectedDescription);

            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var abilityDescriptionId = int.Parse(abilityRow["SpellDesc"]) - CustomTlkOffset;
            tlkEntries[abilityDescriptionId].Should().Be(expectedDescription);
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
        perk.Category.Should().Be(PerkCategoryType.DevicesFieldEngineer);

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

    private static void AssertBeaconTargetingResolver(AbilityDetail ability)
    {
        ability.Targeting.Should().NotBeNull();
        ability.Targeting.SizeResolver.Should().NotBeNull();
        var expectedResolver = (AbilityTargetingSizeResolver)DeviceAbilityEffects.ApplyBeaconPulseRangeBonus;

        Assert.That(ability.Targeting.SizeResolver!.Method, Is.EqualTo(expectedResolver.Method));
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

    private static Dictionary<PerkType, PerkDetail> BuildDevicesFieldEngineerPerksWithout2daLookup()
    {
        var definition = new DevicesFieldEngineerPerkDefinition();
        var methodNames = new[]
        {
            "BeaconTargeting",
            "BlasterBeacon",
            "DiagnosticSweep",
            "IncendiaryField",
            "KillzoneBeacon",
            "RemoteCharge",
            "SignalJammer",
            "ShockBeacon"
        };

        foreach (var methodName in methodNames)
        {
            typeof(DevicesFieldEngineerPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(DevicesFieldEngineerPerkDefinition)
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

    private static Dictionary<int, string> ReadTlkEntries(PathInfo path)
    {
        var tlk = JsonSerializer.Deserialize<TlkFile>(File.ReadAllText(path.FullName))!;
        return tlk.Entries.ToDictionary(entry => entry.Id, entry => entry.Text);
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

    private sealed record TlkFile([property: JsonPropertyName("entries")] TlkEntry[] Entries);

    private sealed record TlkEntry(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("text")] string Text);
}
