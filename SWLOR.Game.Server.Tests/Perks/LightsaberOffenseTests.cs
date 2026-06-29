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
    public void LightsaberOffenseAbilities_MatchCombatBible()
    {
        var versatileStrike = new VersatileStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(versatileStrike[FeatType.VersatileStrike1], "Versatile Strike I", 1, RecastGroup.VersatileStrike, 45f, 0f, 3, null, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(versatileStrike[FeatType.VersatileStrike2], "Versatile Strike II", 2, RecastGroup.VersatileStrike, 45f, 0f, 5, null, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(versatileStrike[FeatType.VersatileStrike3], "Versatile Strike III", 3, RecastGroup.VersatileStrike, 45f, 0f, 8, null, true, false, true, false, AbilityActivationType.Weapon);

        var ferocityStance = new FerocityStanceAbilityDefinition().BuildAbilities()[FeatType.FerocityStance1];
        AssertAbility(ferocityStance, "Ferocity Stance", 1, RecastGroup.FerocityStance, 180f, 2f, null, null, false, false, false, false, AbilityActivationType.Casted);

        var legSlash = new LegSlashAbilityDefinition().BuildAbilities()[FeatType.LegSlash1];
        AssertAbility(legSlash, "Leg Slash", 1, RecastGroup.LegSlash, 60f, 1f, 9, null, true, true, true, false, AbilityActivationType.Casted);

        var focusedStance = new FocusedStanceAbilityDefinition().BuildAbilities()[FeatType.FocusedStance1];
        AssertAbility(focusedStance, "Focused Stance", 1, RecastGroup.FocusedStance, 180f, 2f, null, null, false, false, false, false, AbilityActivationType.Casted);

        var brutalAssault = new BrutalAssaultAbilityDefinition().BuildAbilities()[FeatType.BrutalAssault1];
        AssertAbility(brutalAssault, "Brutal Assault", 1, RecastGroup.BrutalAssault, 300f, 2f, 7, null, false, false, false, true, AbilityActivationType.Casted);
        brutalAssault.AnimationType.Should().Be(Animation.FollowMe);

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
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.VersatileStrike1, "ife_versstrk1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.FerocityStance1, "ife_ferocstnc1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.VersatileStrike2, "ife_versstrk2", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.LegSlash1, "ife_legslsh1", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.FocusedStance1, "ife_focusstnc1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.BrutalAssault1, "ife_brutaslt1", "0x01", "0", "sphere", "5", "****", "17"),
            (FeatType.VersatileStrike3, "ife_versstrk3", "0x01", "0", "****", "****", "****", "****"),
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
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();

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
