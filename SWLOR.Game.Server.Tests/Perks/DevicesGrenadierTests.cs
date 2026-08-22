using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
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

public class DevicesGrenadierTests
{
    [Test]
    public void GrenadeRadiusCalculation_UsesTenthsAsMeterFractions()
    {
        DeviceAbilityEffects.CalculateBlastRadius(3f, 10).Should().Be(4f);
        DeviceAbilityEffects.CalculateBlastRadius(3f, 20).Should().Be(5f);
        DeviceAbilityEffects.CalculateBlastRadius(3f, 30).Should().Be(6f);
    }

    [Test]
    public void AbilityTargetingDetail_OnlyAppliesDynamicSizeWhenFeatIsKnown()
    {
        var targeting = new AbilityTargetingDetail(
            Spell.FragGrenade1,
            AbilityTargetingShapeType.Sphere,
            3f,
            0f,
            AbilityTargetingFlags.HarmsEnemies,
            (_, baseSize) => baseSize + 1.25f);

        targeting.ResolveSizeX(0, false).Should().Be(3f);
        targeting.ResolveSizeX(0, true).Should().Be(4.25f);
    }

    [Test]
    public void DevicesGrenadierStatusEffects_MatchCombatBible()
    {
        new FlashGrenade1StatusEffect().StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(-8);

        var adhesiveSlow = new AdhesiveGrenadeStatusEffect();
        adhesiveSlow.StatGroup.Stats[StatType.MovementSpeedPercentAdjustment].Should().Be(-50);
        adhesiveSlow.ResistanceType.Should().Be(ResistanceType.Mobility);

        new AdhesiveGrenadeStatusEffect(55)
            .StatGroup.Stats[StatType.MovementSpeedPercentAdjustment]
            .Should()
            .Be(-55);
    }

    [Test]
    public void DevicesGrenadierAbilities_MatchCombatBible()
    {
        var flashGrenade = new FlashGrenadeAbilityDefinition().BuildAbilities()[FeatType.FlashGrenade1];
        AssertAbility(flashGrenade, "Flash Grenade", 1, RecastGroup.FlashGrenade, 15f, 1f, 2, "explosives");
        AssertTargeting(flashGrenade, Spell.FlashGrenade1, 4f);

        var adhesiveGrenade = new AdhesiveGrenadeAbilityDefinition().BuildAbilities();
        AssertAbility(adhesiveGrenade[FeatType.AdhesiveGrenade1], "Adhesive Grenade I", 1, RecastGroup.AdhesiveGrenade, 45f, 1f, 4, "explosives");
        AssertTargeting(adhesiveGrenade[FeatType.AdhesiveGrenade1], Spell.AdhesiveGrenade1, 4f);
        AssertAbility(adhesiveGrenade[FeatType.AdhesiveGrenade2], "Adhesive Grenade II", 2, RecastGroup.AdhesiveGrenade, 45f, 1f, 5, "explosives");
        AssertTargeting(adhesiveGrenade[FeatType.AdhesiveGrenade2], Spell.AdhesiveGrenade2, 4f);

        var clusterGrenade = new ClusterGrenadeAbilityDefinition().BuildAbilities()[FeatType.ClusterGrenade1];
        AssertAbility(clusterGrenade, "Cluster Grenade", 1, RecastGroup.ClusterGrenade, 24f, 1f, 5, "explosives");
        AssertTargeting(clusterGrenade, Spell.ClusterGrenade1, 3f);

        var disruptionPulse = new DisruptionPulseAbilityDefinition().BuildAbilities()[FeatType.DisruptionPulse1];
        AssertAbility(disruptionPulse, "Disruption Pulse", 1, RecastGroup.DisruptionPulse, 24f, 1.5f, 4, "explosives");
        disruptionPulse.MaxRange.Should().Be(12f);
        AssertTargeting(disruptionPulse, Spell.DisruptionPulse1, 5f);
    }

    [Test]
    public void DevicesGrenadierPerks_MatchCombatBible()
    {
        var perks = BuildDevicesGrenadierPerksWithout2daLookup();

        AssertPerkLevel(
            perks[PerkType.BlastRadius],
            "Blast Radius",
            1,
            3,
            5,
            FeatType.BlastRadiusTrait,
            "Grenade abilities, Remote Charge, and Overload Barrage gain +1m blast radius.",
            (StatType.BlastRadiusBonusTenths, 10));
        AssertPerkLevel(
            perks[PerkType.BlastRadius],
            "Blast Radius",
            2,
            4,
            22,
            null,
            "Grenade abilities, Remote Charge, and Overload Barrage gain +2m blast radius.",
            (StatType.BlastRadiusBonusTenths, 20));
        AssertPerkLevel(
            perks[PerkType.BlastRadius],
            "Blast Radius",
            3,
            5,
            45,
            null,
            "Grenade abilities, Remote Charge, and Overload Barrage gain +3m blast radius, and Flash Grenade and Adhesive Grenade non-save effect strength increases by 5%.",
            (StatType.BlastRadiusBonusTenths, 30),
            (StatType.GrenadeControlPotencyBonus, 5));

        AssertPerkLevel(
            perks[PerkType.FlashGrenade],
            "Flash Grenade",
            1,
            3,
            12,
            FeatType.FlashGrenade1,
            "Attempts to inflict Flash, reducing physical and Force ability hit chance by 8% for 30 seconds in a 4m blast. Affects up to 5 targets. Consumes explosives.");
        AssertPerkLevel(
            perks[PerkType.AdhesiveGrenade],
            "Adhesive Grenade",
            1,
            4,
            25,
            FeatType.AdhesiveGrenade1,
            "Slows enemies in a 4m blast for 6 seconds. Affects up to 3 targets. Consumes explosives.");
        AssertPerkLevel(
            perks[PerkType.AdhesiveGrenade],
            "Adhesive Grenade",
            2,
            4,
            42,
            FeatType.AdhesiveGrenade2,
            "Slows enemies in a 4m blast for 12 seconds. Affects up to 5 targets. Consumes explosives.");
        AssertPerkLevel(
            perks[PerkType.ClusterGrenade],
            "Cluster Grenade",
            1,
            4,
            30,
            FeatType.ClusterGrenade1,
            "Throws three adjacent grenades within 3m of the target point. Each grenade deals 18 fire DMG plus PER scaling in a 2m blast, and overlapping blasts can hit the same enemy. Consumes explosives.");
        AssertPerkLevel(
            perks[PerkType.DisruptionPulse],
            "Disruption Pulse",
            1,
            4,
            35,
            FeatType.DisruptionPulse1,
            "Emits a 5m disruption pulse at a target point within 12m, dealing 18 electrical DMG plus PER scaling to enemies and reducing physical and Force ability Accuracy by 6% for 12 seconds. Consumes explosives.");
    }

    [Test]
    public void DevicesGrenadierFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.FragGrenade1, "ife_frggrnd1", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.ConcussionGrenade1, "ife_cncssngrnd1", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.FlashGrenade1, "ife_flashgrnd1", "M", "0x3E", "1", "sphere", "4", "****", "1"),
            (FeatType.FragGrenade2, "ife_frggrnd2", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.IonGrenade1, "ife_ngrnd1", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.AdhesiveGrenade1, "ife_dhsvgrnd1", "M", "0x3E", "1", "sphere", "4", "****", "1"),
            (FeatType.ConcussionGrenade2, "ife_cncssngrnd2", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.ClusterGrenade1, "ife_clstrgrnd1", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.IonGrenade2, "ife_ngrnd2", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.DisruptionPulse1, "ife_disrppls1", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.FragGrenade3, "ife_frggrnd3", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.AdhesiveGrenade2, "ife_dhsvgrnd2", "M", "0x3E", "1", "sphere", "4", "****", "1"),
            (FeatType.ThermalDetonator1, "ife_thrmldtntr1", "M", "0x3E", "1", "sphere", "5", "****", "1")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
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
        }
    }

    [Test]
    public void DevicesGrenadierImpactDefinitions_MatchAreaDamageContracts()
    {
        var root = FindRepositoryRoot();
        string ReadDeviceSource(string fileName)
        {
            return File.ReadAllText((
                root /
                "SWLOR.Game.Server" /
                "Feature" /
                "AbilityDefinition" /
                "Devices" /
                fileName).FullName).Replace("\r\n", "\n");
        }

        var flashGrenade = ReadDeviceSource("FlashGrenadeAbilityDefinition.cs");
        var adhesiveGrenade = ReadDeviceSource("AdhesiveGrenadeAbilityDefinition.cs");
        var ionGrenade = ReadDeviceSource("IonGrenadeAbilityDefinition.cs");
        var concussionGrenade = ReadDeviceSource("ConcussionGrenadeAbilityDefinition.cs");
        var clusterGrenade = ReadDeviceSource("ClusterGrenadeAbilityDefinition.cs");
        var disruptionPulse = ReadDeviceSource("DisruptionPulseAbilityDefinition.cs");
        var thermalDetonator = ReadDeviceSource("ThermalDetonatorAbilityDefinition.cs");

        var reportedAreaDamageSources = new[]
        {
            ionGrenade,
            concussionGrenade,
            clusterGrenade,
            disruptionPulse,
            thermalDetonator
        };

        reportedAreaDamageSources.Should().OnlyContain(source => !source.Contains("EffectDamage("));

        flashGrenade.Should().Contain("EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst)");
        flashGrenade.Should().Contain("areaVisualEffect: VisualEffect.None");

        adhesiveGrenade.Should().Contain("EffectVisualEffect(VisualEffect.Vfx_Fnf_Gas_Explosion_Grease)");
        adhesiveGrenade.Should().Contain("DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, 4f)");
        adhesiveGrenade.Should().NotContain("centerOnActivator: !GetIsObjectValid(target)");

        ionGrenade.Should().Contain("while (GetIsObjectValid(creature))");
        ionGrenade.Should().Contain("Ability.ApplyCombatImpact(");
        ionGrenade.Should().Contain("damageType: CombatDamageType.Electrical");
        ionGrenade.Should().Contain("damagePercentAdjustment: impactedTarget => IsDroid(impactedTarget) ? droidBonusPercent : 0");
        ionGrenade.Should().Contain("racialType == RacialType.Droid");
        ionGrenade.Should().Contain("racialType == RacialType.Construct");
        ionGrenade.Should().Contain("racialType == RacialType.Robot");
        ionGrenade.Should().NotContain("GameMath.PercentOf(baseDamage, droidBonusPercent)");

        concussionGrenade.Should().Contain("Ability.ApplyTelegraphedCombatImpact(");
        concussionGrenade.Should().Contain("DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, 3f)");
        concussionGrenade.Should().Contain("damageType: CombatDamageType.Electrical");

        clusterGrenade.Should().Contain("for (var grenadeIndex = 0; grenadeIndex < GrenadeCount; grenadeIndex++)");
        clusterGrenade.Should().Contain("ClusterTargetingRadius");
        clusterGrenade.Should().Contain("GetClusterBlastLocations(activator, location)");
        clusterGrenade.Should().Contain("HasHostileTargetInAnyBlast(activator, blastLocations, blastRadius)");
        clusterGrenade.Should().Contain("!hasAnyTargets && grenadeIndex == 0");
        clusterGrenade.Should().Contain("sendsNoTargetMessage: sendsNoTargetMessage");
        clusterGrenade.Should().Contain("EffectVisualEffect(VisualEffect.Fnf_Fireball)");
        clusterGrenade.Should().Contain("Ability.ApplyTelegraphedCombatImpact(");
        clusterGrenade.Should().Contain("damageType: CombatDamageType.Fire");

        disruptionPulse.Should().Contain("var impactLocation = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);");
        disruptionPulse.Should().Contain("DeviceAbilityEffects.ApplyBlastRadiusBonus");
        disruptionPulse.Should().Contain("var radius = DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, RadiusMeters);");
        disruptionPulse.Should().Contain("areaVisualEffect: VisualEffect.Vfx_Fnf_Electric_Explosion");
        disruptionPulse.Should().Contain("alwaysApplyAreaVisualEffect: true");
        disruptionPulse.Should().Contain("afterImpactAction: _ => DeviceAbilityEffects.ApplyDiagnosticSweep(activator, impactLocation, radius)");
        disruptionPulse.Should().NotContain("centerOnActivator: !GetIsObjectValid(target)");

        thermalDetonator.Should().Contain("SkillType.Devices,\n                60,");
        thermalDetonator.Should().Contain("typeof(BurnStatusEffect)");
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
        perk.Category.Should().Be(PerkCategoryType.DevicesGrenadier);

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
        string itemResref = null,
        int itemQuantity = 1)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Devices);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(AbilityActivationType.Casted);
        ability.IsHostileAbility.Should().BeTrue();
        ability.RequiresTarget.Should().BeFalse();
        ability.RequiresLocationTarget.Should().BeTrue();
        ability.IsSingleTargetAbility.Should().BeFalse();
        ability.IsAreaAbility.Should().BeTrue();
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

        if (itemResref == null)
        {
            ability.Requirements.OfType<AbilityRequirementItem>().Should().BeEmpty();
        }
        else
        {
            var itemRequirement = ability.Requirements
                .OfType<AbilityRequirementItem>()
                .Should()
                .ContainSingle()
                .Which;
            itemRequirement.ItemResref.Should().Be(itemResref);
            itemRequirement.Quantity.Should().Be(itemQuantity);
        }
    }

    private static void AssertTargeting(AbilityDetail ability, Spell spell, float radius)
    {
        ability.Targeting.Should().NotBeNull();
        var targeting = ability.Targeting;
        targeting.Spell.Should().Be(spell);
        targeting.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
        targeting.SizeX.Should().Be(radius);
        targeting.SizeY.Should().Be(0f);
        targeting.Flags.Should().Be(AbilityTargetingFlags.HarmsEnemies);
        targeting.SizeResolver.Should().NotBeNull();
        Assert.That(
            targeting.SizeResolver.Method,
            Is.EqualTo(((AbilityTargetingSizeResolver)DeviceAbilityEffects.ApplyBlastRadiusBonus).Method));
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

    private static Dictionary<PerkType, PerkDetail> BuildDevicesGrenadierPerksWithout2daLookup()
    {
        var definition = new DevicesGrenadierPerkDefinition();
        var methodNames = new[]
        {
            "AdhesiveGrenade",
            "BlastRadius",
            "ClusterGrenade",
            "ConcussionGrenade",
            "DisruptionPulse",
            "FlashGrenade",
            "FragGrenade",
            "IonGrenade",
            "ThermalDetonator"
        };

        foreach (var methodName in methodNames)
        {
            typeof(DevicesGrenadierPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(DevicesGrenadierPerkDefinition)
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
