using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class LightsaberOffenseTests
{
    [Test]
    public void LightsaberOffensePerkLevels_MatchCombatBible()
    {
        var perks = BuildLightsaberOffensePerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.VersatileStrike], "Versatile Strike", 1, 2, 5, FeatType.VersatileStrike1,
            "Your next attack deals weapon DMG + 10 to your target. Inflicts Sunder which reduces defense and force defense by 10% for 30 seconds.");
        AssertPerkLevel(perks[PerkType.FerocityStance], "Ferocity Stance", 1, 2, 8, FeatType.FerocityStance1,
            "While active, grants -20% to offhand weapon delay, +10% attack, and -20% to evasion.");
        AssertPerkLevel(perks[PerkType.VersatileStrike], "Versatile Strike", 2, 3, 10, FeatType.VersatileStrike2,
            "Your next attack deals weapon DMG + 25 to your target. Inflicts Sunder which reduces defense and force defense by 15% for 30 seconds.");
        AssertPerkLevel(perks[PerkType.Centering], "Centering", 1, 2, 12, FeatType.Centering1,
            "Reduces enmity by 25% and increases accuracy by 10% for 30 seconds.");
        AssertPerkLevel(perks[PerkType.LegSlash], "Leg Slash", 1, 2, 15, FeatType.LegSlash1,
            "You deal weapon DMG + 10 and inflict Disoriented, reducing Accuracy and Evasion by 15% for 20 seconds.");
        AssertPerkLevel(perks[PerkType.FocusedStance], "Focused Stance", 1, 2, 18, FeatType.FocusedStance1,
            "While active, gain +10% Attack.");
        AssertPerkLevel(perks[PerkType.BrutalAssault], "Brutal Assault", 1, 3, 20, FeatType.BrutalAssault1,
            "Allies within the area of effect (sphere) gain +10% critical hit chance for 1 minute. You do not receive this benefit.");
        AssertPerkLevel(perks[PerkType.SecondWind], "Second Wind", 1, 3, 22, FeatType.SecondWind1,
            "Restores 50% of max STM, increased by 1 percentage point per MGT to a maximum of 75%.");
        AssertPerkLevel(perks[PerkType.OverwhelmingStrike], "Overwhelming Strike", 1, 3, 25, FeatType.OverwhelmingStrike1,
            "You deal weapon DMG + 15 to all enemies in the area of effect (cone) in front of you. Inflicts Sunder which reduces defense and force defense by 15% for 30 seconds.");
        AssertPerkLevel(perks[PerkType.BrutalEfficiency], "Brutal Efficiency", 1, 3, 27, null,
            "Your attacks deal +15% damage to enemies afflicted by Sunder.",
            StatType.DamageToSunderedTargetPercentAdjustment);
        AssertPerkLevel(perks[PerkType.Purify], "Purify", 1, 2, 30, FeatType.Purify1,
            "One debuff is removed from you. A nearby enemy is inflicted with the removed debuff.");
        AssertPerkLevel(perks[PerkType.VersatileStrike], "Versatile Strike", 3, 3, 32, FeatType.VersatileStrike3,
            "Your next attack deals weapon DMG + 40 to your target. Inflicts Sunder which reduces defense and force defense by 20% for 30 seconds.");
        AssertPerkLevel(perks[PerkType.BladeBlitz], "Blade Blitz", 1, 3, 35, null,
            "After dealing a critical hit, your next auto-attack uses the default minimum delay.",
            StatType.CriticalNextAutoAttackNoDelayTriggerSkillType,
            StatType.CriticalNextAutoAttackNoDelaySkillType,
            StatType.CriticalNextAutoAttackNoDelayDurationSeconds);
        AssertPerkLevel(perks[PerkType.ArcStrike], "Arc Strike", 1, 3, 38, FeatType.ArcStrike1,
            "You deal weapon DMG + 20 to all enemies in the area of effect (cone) in front of you.");
        AssertPerkLevel(perks[PerkType.SurgeStrike], "Surge Strike", 1, 4, 40, FeatType.SurgeStrike1,
            "Your next attack deals weapon DMG + 15. Inflicts Force Disruption, preventing the target from using Force abilities for 8 seconds.");
        AssertPerkLevel(perks[PerkType.Centering], "Centering", 2, 3, 42, FeatType.Centering2,
            "Reduces enmity by 50% and increases accuracy by 20% for 30 seconds.");
        AssertPerkLevel(perks[PerkType.RippleSlash], "Ripple Slash", 1, 4, 45, FeatType.RippleSlash1,
            "Your next attack deals weapon DMG + 30 to your target. Inflicts Disoriented on nearby enemies, reducing Accuracy and Evasion by 15% for 20 seconds.");
        AssertPerkLevel(perks[PerkType.Overcharge], "Overcharge", 1, 4, 47, null,
            "Your Versatile Strike and Overwhelming Strike abilities now deal +10 DMG and increase their Sunder duration by 50%.",
            StatType.AbilityDamageFlatAdjustmentPerkType,
            StatType.AbilityDamageFlatAdjustmentSecondaryPerkType,
            StatType.AbilityDamageFlatAdjustment,
            StatType.AbilityStatusDurationPercentAdjustmentPerkType,
            StatType.AbilityStatusDurationPercentAdjustmentSecondaryPerkType,
            StatType.AbilityStatusDurationPercentAdjustment);
        AssertPerkLevel(perks[PerkType.SaberStorm], "Saber Storm", 1, 4, 50, FeatType.SaberStorm1,
            "Enemies within the area of effect (sphere) take weapon DMG + 30 and suffer Sunder, reducing physical and Force defense by 10% for 45 seconds.");
    }

    [Test]
    public void LightsaberOffenseAbilities_MatchCombatBible()
    {
        var versatileStrike = new VersatileStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(versatileStrike[FeatType.VersatileStrike1], "Versatile Strike I", 1, RecastGroup.VersatileStrike, 45f, 0f, 3, null, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(versatileStrike[FeatType.VersatileStrike2], "Versatile Strike II", 2, RecastGroup.VersatileStrike, 45f, 0f, 5, null, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(versatileStrike[FeatType.VersatileStrike3], "Versatile Strike III", 3, RecastGroup.VersatileStrike, 45f, 0f, 8, null, true, false, true, false, AbilityActivationType.Weapon);

        var ferocityStance = new FerocityStanceAbilityDefinition().BuildAbilities()[FeatType.FerocityStance1];
        AssertAbility(ferocityStance, "Ferocity Stance", 1, RecastGroup.FerocityStance, 180f, 2f, null, null, false, false, false, false, AbilityActivationType.Casted);

        var centering = new CenteredAbilityDefinition().BuildAbilities();
        AssertAbility(centering[FeatType.Centering1], "Centering I", 1, RecastGroup.Centering, 60f, 0f, 3, null, false, false, false, false, AbilityActivationType.Casted);
        AssertAbility(centering[FeatType.Centering2], "Centering II", 2, RecastGroup.Centering, 60f, 0f, 5, null, false, false, false, false, AbilityActivationType.Casted);

        var legSlash = new LegSlashAbilityDefinition().BuildAbilities()[FeatType.LegSlash1];
        AssertAbility(legSlash, "Leg Slash", 1, RecastGroup.LegSlash, 60f, 1f, 9, null, true, true, true, false, AbilityActivationType.Casted);

        var focusedStance = new FocusedStanceAbilityDefinition().BuildAbilities()[FeatType.FocusedStance1];
        AssertAbility(focusedStance, "Focused Stance", 1, RecastGroup.FocusedStance, 180f, 2f, null, null, false, false, false, false, AbilityActivationType.Casted);

        var brutalAssault = new BrutalAssaultAbilityDefinition().BuildAbilities()[FeatType.BrutalAssault1];
        AssertAbility(brutalAssault, "Brutal Assault", 1, RecastGroup.BrutalAssault, 300f, 2f, 7, null, false, false, false, true, AbilityActivationType.Casted);

        var secondWind = new SecondWindAbilityDefinition().BuildAbilities()[FeatType.SecondWind1];
        AssertAbility(secondWind, "Second Wind", 1, RecastGroup.SecondWind, 300f, 3f, null, 15, false, false, false, false, AbilityActivationType.Casted);

        var overwhelmingStrike = new OverwhelmingStrikeAbilityDefinition().BuildAbilities()[FeatType.OverwhelmingStrike1];
        AssertAbility(overwhelmingStrike, "Overwhelming Strike", 1, RecastGroup.OverwhelmingStrike, 90f, 0f, 10, null, true, false, false, true, AbilityActivationType.Casted);

        var purify = new PurifyAbilityDefinition().BuildAbilities()[FeatType.Purify1];
        AssertAbility(purify, "Purify", 1, RecastGroup.Purify, 30f, 2f, 4, null, false, false, false, false, AbilityActivationType.Casted);

        var arcStrike = new ArcStrikeAbilityDefinition().BuildAbilities()[FeatType.ArcStrike1];
        AssertAbility(arcStrike, "Arc Strike", 1, RecastGroup.ArcStrike, 30f, 0f, 8, null, true, false, false, true, AbilityActivationType.Casted);

        var surgeStrike = new SurgeStrikeAbilityDefinition().BuildAbilities()[FeatType.SurgeStrike1];
        AssertAbility(surgeStrike, "Surge Strike", 1, RecastGroup.SurgeStrike, 30f, 0f, 12, null, true, false, true, false, AbilityActivationType.Weapon);

        var rippleSlash = new RippleSlashAbilityDefinition().BuildAbilities()[FeatType.RippleSlash1];
        AssertAbility(rippleSlash, "Ripple Slash", 1, RecastGroup.RippleSlash, 120f, 0f, 10, null, true, false, false, true, AbilityActivationType.Weapon);

        var saberStorm = new SaberStormAbilityDefinition().BuildAbilities()[FeatType.SaberStorm1];
        AssertAbility(saberStorm, "Saber Storm", 1, RecastGroup.Capstone, 345f, 2f, 15, null, true, false, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void LightsaberOffenseStatusEffects_MatchCombatBible()
    {
        var ferocity = new FerocityStanceStatusEffect();
        ferocity.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(10);
        ferocity.StatGroup.Stats[StatType.OffhandAttackDelayReductionPercent].Should().Be(20);
        ferocity.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-20);

        var focused = new FocusedStanceStatusEffect();
        focused.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(10);

        var brutalAssault = new BrutalAssaultStatusEffect();
        brutalAssault.StatGroup.Stats[StatType.CriticalRatePercentAdjustment].Should().Be(10);

        var sunder10 = new SunderStatusEffect(10);
        sunder10.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-10);
        sunder10.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-10);

        var sunder15 = new SunderStatusEffect();
        sunder15.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-15);
        sunder15.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-15);

        var sunder20 = new SunderStatusEffect(20);
        sunder20.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-20);
        sunder20.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-20);

        var sunder25 = new SunderStatusEffect(25);
        sunder25.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-25);
        sunder25.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-25);

        var disoriented = new DisorientedStatusEffect();
        disoriented.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-15);
        disoriented.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-15);

        var forceDisruption = new ForceDisruptionStatusEffect(true);
        forceDisruption.StatGroup.Stats[StatType.ForceAbilityActivationDisabled].Should().Be(1);
    }

    [Test]
    public void LightsaberOffenseFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.VersatileStrike1, "ife_versstrk1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.FerocityStance1, "ife_ferocstnc1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.VersatileStrike2, "ife_versstrk2", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.Centering1, "ife_cent1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.LegSlash1, "ife_legslsh1", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.FocusedStance1, "ife_focusstnc1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.BrutalAssault1, "ife_brutaslt1", "0x01", "0", "sphere", "5", "****", "17"),
            (FeatType.SecondWind1, "ife_secwind1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.OverwhelmingStrike1, "ife_ovrwstrk1", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.Purify1, "ife_pur1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.VersatileStrike3, "ife_versstrk3", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ArcStrike1, "ife_arcstrk1", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.SurgeStrike1, "ife_srgstrk1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.Centering2, "ife_cent2", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.RippleSlash1, "ife_riplslsh1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SaberStorm1, "ife_sabrstrm1", "0x01", "1", "sphere", "5", "****", "17")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            spellRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            spellRow["TargetType"].Should().Be(targetType);
            spellRow["HostileSetting"].Should().Be(hostileSetting);
            spellRow["TargetShape"].Should().Be(targetShape);
            spellRow["TargetSizeX"].Should().Be(targetSizeX);
            spellRow["TargetSizeY"].Should().Be(targetSizeY);
            spellRow["TargetFlags"].Should().Be(targetFlags);
        }
    }

    private static void AssertPerkLevel(
        PerkDetail perk,
        string name,
        int level,
        int price,
        int skillRank,
        FeatType? grantedFeat,
        string description,
        params StatType[] statTypes)
    {
        perk.Name.Should().Be(name);
        perk.Category.Should().Be(PerkCategoryType.LightsaberOffense);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Lightsaber, skillRank);
        AssertCharacterRequirement(perkLevel, CharacterType.ForceSensitive);

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statTypes.Length > 0)
        {
            perkLevel.StatBonuses.Should().HaveCount(statTypes.Length);
            perkLevel.StatBonuses.Select(x => x.Stat).Should().Contain(statTypes);
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
        int? staminaCost,
        int? fpCost,
        bool isHostile,
        bool requiresTarget,
        bool isSingleTarget,
        bool isArea,
        AbilityActivationType activationType)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(activationType);
        ability.IsHostileAbility.Should().Be(isHostile);
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.IsSingleTargetAbility.Should().Be(isSingleTarget);
        ability.IsAreaAbility.Should().Be(isArea);
        ability.BreaksStealth.Should().BeTrue();

        if (staminaCost.HasValue)
        {
            ability.Requirements
                .OfType<AbilityRequirementStamina>()
                .Should()
                .ContainSingle()
                .Which
                .RequiredSTM
                .Should()
                .Be(staminaCost.Value);
        }
        else
        {
            ability.Requirements.OfType<AbilityRequirementStamina>().Should().BeEmpty();
        }

        if (fpCost.HasValue)
        {
            ability.Requirements
                .OfType<AbilityRequirementFP>()
                .Should()
                .ContainSingle()
                .Which
                .RequiredFP
                .Should()
                .Be(fpCost.Value);
        }
        else
        {
            ability.Requirements.OfType<AbilityRequirementFP>().Should().BeEmpty();
        }
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
            .GetField("_requiredCharacterType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(requirement)
            .Should()
            .Be(characterType);
    }

    private static Dictionary<PerkType, PerkDetail> BuildLightsaberOffensePerksWithout2daLookup()
    {
        var definition = new LightsaberPerkDefinition();
        var methodNames = new[]
        {
            "ArcStrike",
            "BladeBlitz",
            "BrutalAssault",
            "BrutalEfficiency",
            "Centering",
            "FerocityStance",
            "FocusedStance",
            "LegSlash",
            "Overcharge",
            "OverwhelmingStrike",
            "Purify",
            "RippleSlash",
            "SaberStorm",
            "SecondWind",
            "SurgeStrike",
            "VersatileStrike"
        };

        foreach (var methodName in methodNames)
        {
            typeof(LightsaberPerkDefinition)
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(LightsaberPerkDefinition)
            .GetField("_builder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
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
            for (var i = 0; i < header.Length && i + 1 < cells.Length; i++)
            {
                values[header[i]] = cells[i + 1];
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
