using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
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

        AssertPerkLevel(perks[PerkType.BlasterBeacon], "Blaster Beacon", 1, 2, null, FeatType.BlasterBeacon1,
            "Plants a targeting beacon for 18 seconds. Every 3 seconds, one hostile target within 12m is hit by an automated ranged energy pulse for DMG plus PER scaling.");
        AssertPerkLevel(perks[PerkType.BeaconTargeting], "Beacon Targeting", 1, 2, 5, null,
            "Beacon pulses gain +5% Accuracy and +5% critical chance.",
            (StatType.BeaconPulseAccuracyPercentAdjustment, 5),
            (StatType.BeaconPulseCriticalRatePercentAdjustment, 5));
        AssertPerkLevel(perks[PerkType.IncendiaryField], "Incendiary Field", 1, 3, 8, FeatType.IncendiaryField1,
            "Deploys a visible fire field for 12 seconds. Enemies inside take fire DMG plus PER scaling every 3 seconds.");
        AssertPerkLevel(perks[PerkType.RemoteCharge], "Remote Charge", 1, 3, 12, FeatType.RemoteCharge1,
            "Arms a visible charge at your target location that detonates after 3 seconds for fire DMG plus PER scaling.");
        AssertPerkLevel(perks[PerkType.BlasterBeacon], "Blaster Beacon", 2, 3, 15, FeatType.BlasterBeacon2,
            "Plants a targeting beacon for 21 seconds. Every 3 seconds, one hostile target within 12m is hit by an increased automated ranged energy pulse for DMG plus PER scaling.");
        AssertPerkLevel(perks[PerkType.MaintenancePulse], "Maintenance Pulse", 1, 3, 18, FeatType.MaintenancePulse1,
            "Restores 12% of maximum HP to one friendly droid or mechanical ally. If you have an active Field Engineer beacon or field, its duration is extended by 3 seconds.");
        AssertPerkLevel(perks[PerkType.BeaconTargeting], "Beacon Targeting", 2, 2, 22, null,
            "Beacon pulses gain +10% Accuracy, +10% critical chance, and +5% damage.",
            (StatType.BeaconPulseAccuracyPercentAdjustment, 10),
            (StatType.BeaconPulseCriticalRatePercentAdjustment, 10),
            (StatType.BeaconPulseDamagePercentAdjustment, 5));
        AssertPerkLevel(perks[PerkType.ShockBeacon], "Shock Beacon", 1, 4, 25, FeatType.ShockBeacon1,
            "Plants a shock beacon for 15 seconds. Every 3 seconds, one hostile target within 10m is hit by an electrical pulse and suffers Shock.");
        AssertPerkLevel(perks[PerkType.IncendiaryField], "Incendiary Field", 2, 3, 28, FeatType.IncendiaryField2,
            "Deploys a visible fire field for 15 seconds. Enemies inside take increased fire DMG plus PER scaling every 3 seconds.");
        AssertPerkLevel(perks[PerkType.RemoteCharge], "Remote Charge", 2, 4, 30, FeatType.RemoteCharge2,
            "Arms a visible charge that detonates after 3 seconds for fire DMG plus PER scaling and knock down.");
        AssertPerkLevel(perks[PerkType.BlasterBeacon], "Blaster Beacon", 3, 3, 35, FeatType.BlasterBeacon3,
            "Plants a targeting beacon for 24 seconds. Every 3 seconds, one hostile target within 14m is hit by a high automated ranged energy pulse for DMG plus PER scaling.");
        AssertPerkLevel(perks[PerkType.MaintenancePulse], "Maintenance Pulse", 2, 3, 38, FeatType.MaintenancePulse2,
            "Restores high HP to one friendly droid or mechanical ally and removes Shock. If you have an active Field Engineer beacon or field, its duration is extended by 5 seconds.");
        AssertPerkLevel(perks[PerkType.ShockBeacon], "Shock Beacon", 2, 4, 40, FeatType.ShockBeacon2,
            "Plants a shock beacon for 18 seconds. Every 3 seconds, one hostile target within 12m is hit by an increased electrical pulse and suffers Shock.");
        AssertPerkLevel(perks[PerkType.IncendiaryField], "Incendiary Field", 3, 4, 42, FeatType.IncendiaryField3,
            "Deploys a visible fire field for 18 seconds. Enemies inside take high fire DMG plus PER scaling every 3 seconds.");
        AssertPerkLevel(perks[PerkType.BeaconTargeting], "Beacon Targeting", 3, 4, 45, null,
            "Beacon pulses gain +15% Accuracy, +15% critical chance, +10% damage, and +2m pulse range.",
            (StatType.BeaconPulseAccuracyPercentAdjustment, 15),
            (StatType.BeaconPulseCriticalRatePercentAdjustment, 15),
            (StatType.BeaconPulseDamagePercentAdjustment, 10),
            (StatType.BeaconPulseRangeBonusMeters, 2));
        AssertPerkLevel(perks[PerkType.RemoteCharge], "Remote Charge", 3, 3, 48, FeatType.RemoteCharge3,
            "Arms a visible charge that detonates after 3 seconds for heavy fire DMG and knock down.");
        AssertPerkLevel(perks[PerkType.KillzoneBeacon], "Killzone Beacon", 1, 5, 50, FeatType.KillzoneBeacon1,
            "Plants a killzone beacon for 18 seconds. Every 3 seconds, it triggers one energy pulse and one shock pulse against hostile targets within 12m.");
    }

    [Test]
    public void DevicesFieldEngineerAbilities_MatchCombatBible()
    {
        var blasterBeacon = new BlasterBeaconAbilityDefinition().BuildAbilities();
        AssertAbility(blasterBeacon[FeatType.BlasterBeacon1], "Blaster Beacon I", 1, RecastGroup.BlasterBeacon, 45f, 1.5f, 3, true, true, false, false);
        AssertAbility(blasterBeacon[FeatType.BlasterBeacon2], "Blaster Beacon II", 2, RecastGroup.BlasterBeacon, 45f, 1.5f, 4, true, true, false, false);
        AssertAbility(blasterBeacon[FeatType.BlasterBeacon3], "Blaster Beacon III", 3, RecastGroup.BlasterBeacon, 45f, 1.5f, 6, true, true, false, false);

        var incendiaryField = new IncendiaryFieldAbilityDefinition().BuildAbilities();
        AssertAbility(incendiaryField[FeatType.IncendiaryField1], "Incendiary Field I", 1, RecastGroup.IncendiaryField, 60f, 1.5f, 4, true, true, false, false);
        AssertAbility(incendiaryField[FeatType.IncendiaryField2], "Incendiary Field II", 2, RecastGroup.IncendiaryField, 60f, 1.5f, 5, true, true, false, false);
        AssertAbility(incendiaryField[FeatType.IncendiaryField3], "Incendiary Field III", 3, RecastGroup.IncendiaryField, 60f, 1.5f, 7, true, true, false, false);

        var remoteCharge = new RemoteChargeAbilityDefinition().BuildAbilities();
        AssertAbility(remoteCharge[FeatType.RemoteCharge1], "Remote Charge I", 1, RecastGroup.RemoteCharge, 30f, 1f, 4, true, true, false, false);
        AssertAbility(remoteCharge[FeatType.RemoteCharge2], "Remote Charge II", 2, RecastGroup.RemoteCharge, 30f, 1f, 5, true, true, false, false);
        AssertAbility(remoteCharge[FeatType.RemoteCharge3], "Remote Charge III", 3, RecastGroup.RemoteCharge, 30f, 1f, 7, true, true, false, false);

        var maintenancePulse = new MaintenancePulseAbilityDefinition().BuildAbilities();
        AssertAbility(maintenancePulse[FeatType.MaintenancePulse1], "Maintenance Pulse I", 1, RecastGroup.MaintenancePulse, 18f, 1f, 3, false, false, true, true);
        AssertAbility(maintenancePulse[FeatType.MaintenancePulse2], "Maintenance Pulse II", 2, RecastGroup.MaintenancePulse, 18f, 1f, 4, false, false, true, true);

        var shockBeacon = new ShockBeaconAbilityDefinition().BuildAbilities();
        AssertAbility(shockBeacon[FeatType.ShockBeacon1], "Shock Beacon I", 1, RecastGroup.ShockBeacon, 75f, 1.5f, 5, true, true, false, false);
        AssertAbility(shockBeacon[FeatType.ShockBeacon2], "Shock Beacon II", 2, RecastGroup.ShockBeacon, 75f, 1.5f, 6, true, true, false, false);

        var killzoneBeacon = new KillzoneBeaconAbilityDefinition().BuildAbilities();
        AssertAbility(killzoneBeacon[FeatType.KillzoneBeacon1], "Killzone Beacon", 1, RecastGroup.KillzoneBeacon, 120f, 2f, 9, true, true, false, false);
    }

    [Test]
    public void DevicesFieldEngineerSources_IncludeBibleBehavior()
    {
        var root = FindRepositoryRoot();
        var effects = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "DeviceAbilityEffects.cs").FullName);
        effects.Should().Contain("BeaconPulseAccuracyPercentAdjustment");
        effects.Should().Contain("BeaconPulseCriticalRatePercentAdjustment");
        effects.Should().Contain("BeaconPulseDamagePercentAdjustment");
        effects.Should().Contain("BeaconPulseRangeBonusMeters");
        effects.Should().Contain("ExtendActiveFieldEngineerPulses");
        effects.Should().Contain("ScheduleNextFieldEngineerPulse");

        var maintenance = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "MaintenancePulseAbilityDefinition.cs").FullName);
        maintenance.Should().Contain(".IsSingleTargetAbility()");
        maintenance.Should().Contain(".RequiresTarget()");
        maintenance.Should().Contain("ValidateMaintenanceTarget");
        maintenance.Should().Contain("Droid.IsDroid");
        maintenance.Should().Contain("RacialType.Construct");
        maintenance.Should().Contain("ApplyMaintenancePulse(activator, target, 12, 3f, false)");
        maintenance.Should().Contain("ApplyMaintenancePulse(activator, target, 20, 5f, true)");
        maintenance.Should().Contain("DeviceAbilityEffects.ExtendActiveFieldEngineerPulses(activator, extensionSeconds)");
        maintenance.Should().NotContain("ScaleDirectEffect");

        var remoteCharge = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "RemoteChargeAbilityDefinition.cs").FullName);
        remoteCharge.Should().Contain("DetonateRemoteCharge(activator, target, targetLocation, 10, null)");
        remoteCharge.Should().Contain("DetonateRemoteCharge(activator, target, targetLocation, 14, typeof(KnockdownStatusEffect))");
        remoteCharge.Should().Contain("DetonateRemoteCharge(activator, target, targetLocation, 20, typeof(KnockdownStatusEffect))");
        remoteCharge.Should().Contain("CombatImpactAreaShape.Sphere");
        remoteCharge.Should().Contain("3f");
        remoteCharge.Should().Contain("CombatDamageType.Fire");
        remoteCharge.Should().Contain("typeof(KnockdownStatusEffect)");

        var killzoneBeacon = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "KillzoneBeaconAbilityDefinition.cs").FullName);
        killzoneBeacon.Should().Contain("22");
        killzoneBeacon.Should().Contain("14");
        killzoneBeacon.Should().Contain("CombatDamageType.Electrical");
        killzoneBeacon.Should().Contain("typeof(ShockStatusEffect)");
    }

    [Test]
    public void DevicesFieldEngineerFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.BlasterBeacon1, "ife_blstrbcn1", "M", "0x3E", "1", "sphere", "12", "****", "1", "****"),
            (FeatType.IncendiaryField1, "ife_ncndryfld1", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.RemoteCharge1, "ife_rmtchrg1", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.BlasterBeacon2, "ife_blstrbcn2", "M", "0x3E", "1", "sphere", "12", "****", "1", "****"),
            (FeatType.MaintenancePulse1, "ife_mntnncpls1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.ShockBeacon1, "ife_shokbcn1", "M", "0x3E", "1", "sphere", "10", "****", "1", "****"),
            (FeatType.IncendiaryField2, "ife_ncndryfld2", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.RemoteCharge2, "ife_rmtchrg2", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.BlasterBeacon3, "ife_blstrbcn3", "M", "0x3E", "1", "sphere", "14", "****", "1", "****"),
            (FeatType.MaintenancePulse2, "ife_mntnncpls2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.ShockBeacon2, "ife_shokbcn2", "M", "0x3E", "1", "sphere", "12", "****", "1", "****"),
            (FeatType.IncendiaryField3, "ife_ncndryfld3", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.RemoteCharge3, "ife_rmtchrg3", "M", "0x3E", "1", "sphere", "5", "****", "1", "****"),
            (FeatType.KillzoneBeacon1, "ife_kllznbcn1", "M", "0x3E", "1", "sphere", "12", "****", "1", "****")
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
        perk.Category.Should().Be(PerkCategoryType.Devices);

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

    private static Dictionary<PerkType, PerkDetail> BuildDevicesFieldEngineerPerksWithout2daLookup()
    {
        var definition = new DevicesFieldEngineerPerkDefinition();
        var methodNames = new[]
        {
            "BeaconTargeting",
            "BlasterBeacon",
            "IncendiaryField",
            "KillzoneBeacon",
            "MaintenancePulse",
            "RemoteCharge",
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
