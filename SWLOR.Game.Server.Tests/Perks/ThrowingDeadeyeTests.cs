using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Throwing;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class ThrowingDeadeyeTests
{
    [Test]
    public void ThrowingDeadeyeAbilities_MatchCombatBible()
    {
        var piercingToss = new PiercingTossAbilityDefinition().BuildAbilities();
        AssertAbility(piercingToss[FeatType.PiercingToss1], "Piercing Toss I", 1, RecastGroup.PiercingToss, 30f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(piercingToss[FeatType.PiercingToss2], "Piercing Toss II", 2, RecastGroup.PiercingToss, 30f, 0f, 5, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(piercingToss[FeatType.PiercingToss3], "Piercing Toss III", 3, RecastGroup.PiercingToss, 30f, 0f, 7, true, false, true, false, AbilityActivationType.Weapon);

        var pinningToss = new PinningTossAbilityDefinition().BuildAbilities();
        AssertAbility(pinningToss[FeatType.PinningToss1], "Pinning Toss I", 1, RecastGroup.PinningToss, 30f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(pinningToss[FeatType.PinningToss2], "Pinning Toss II", 2, RecastGroup.PinningToss, 30f, 0f, 6, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(pinningToss[FeatType.PinningToss3], "Pinning Toss III", 3, RecastGroup.PinningToss, 30f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);

        var deadeyeStance = new DeadeyeStanceAbilityDefinition().BuildAbilities()[FeatType.DeadeyeStance1];
        AssertAbility(deadeyeStance, "Deadeye Stance", 1, RecastGroup.DeadeyeStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted, expectedMaxRange: 5f);

        var severingToss = new SeveringTossAbilityDefinition().BuildAbilities()[FeatType.SeveringToss1];
        AssertAbility(severingToss, "Severing Toss", 1, RecastGroup.SeveringToss, 60f, 0f, 10, true, true, true, false, AbilityActivationType.Casted);

        var finishingToss = new FinishingTossAbilityDefinition().BuildAbilities()[FeatType.FinishingToss1];
        AssertAbility(finishingToss, "Finishing Toss", 1, RecastGroup.FinishingToss, 90f, 0f, 10, true, true, true, false, AbilityActivationType.Casted);

        var perfectThrow = new PerfectThrowAbilityDefinition().BuildAbilities()[FeatType.PerfectThrow1];
        AssertAbility(perfectThrow, "Perfect Throw", 1, RecastGroup.Capstone, 345f, 1f, 15, true, true, true, false, AbilityActivationType.Casted);
    }

    [Test]
    public void ThrowingDeadeyeStatusEffects_MatchCombatBible()
    {
        var deadeyeStance = new DeadeyeStanceStatusEffect();
        deadeyeStance.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(15);
        deadeyeStance.StatGroup.Stats[StatType.CriticalRatePercentAdjustment].Should().Be(15);
        deadeyeStance.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-20);

        var markingToss = new MarkingTossStatusEffect();
        markingToss.StatGroup.Stats[StatType.ThrowingDamageTakenPercentAdjustment].Should().Be(10);
        markingToss.Categories.Should().HaveFlag(StatusEffectCategory.Debuff);
        markingToss.ResistanceType.Should().Be(ResistanceType.Trauma);

        var disoriented = new DisorientedStatusEffect();
        disoriented.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-15);
        disoriented.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-15);

        var pinningToss3Disoriented = new DisorientedStatusEffect(15);
        pinningToss3Disoriented.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-15);
        pinningToss3Disoriented.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-30);
        pinningToss3Disoriented.Clone().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-30);

        var hemorrhage = new HemorrhageStatusEffect();
        hemorrhage.StatGroup.Stats[StatType.DamageTakenPercentAdjustment].Should().Be(10);
        hemorrhage.Categories.Should().HaveFlag(StatusEffectCategory.Bleeding);
        hemorrhage.Categories.Should().HaveFlag(StatusEffectCategory.Debuff);
    }
    [Test]
    public void ThrowingDeadeyeFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.PiercingToss1, "ife_pierctoss1", "P", "0x01", "0"),
            (FeatType.PinningToss1, "ife_pintoss1", "P", "0x01", "0"),
            (FeatType.DeadeyeStance1, "ife_eyestnc1", "P", "0x01", "0"),
            (FeatType.PiercingToss2, "ife_pierctoss2", "P", "0x01", "0"),
            (FeatType.PinningToss2, "ife_pintoss2", "P", "0x01", "0"),
            (FeatType.PiercingToss3, "ife_pierctoss3", "P", "0x01", "0"),
            (FeatType.PinningToss3, "ife_pintoss3", "P", "0x01", "0"),
            (FeatType.SeveringToss1, "ife_sevtoss1", "M", "0x02", "1"),
            (FeatType.FinishingToss1, "ife_fintoss1", "M", "0x02", "1"),
            (FeatType.PerfectThrow1, "ife_perfthrow1", "M", "0x02", "1")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting) in feats)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            abilityRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();

            abilityRow["Range"].Should().Be(range);
            abilityRow["TargetType"].Should().Be(targetType);
            abilityRow["HostileSetting"].Should().Be(hostileSetting);
            abilityRow["TargetShape"].Should().Be("****");
            abilityRow["TargetSizeX"].Should().Be("****");
            abilityRow["TargetFlags"].Should().Be("****");
            abilityRow["TargetSizeY"].Should().Be("****");
        }

        featRows[(int)FeatType.PiercingToss1]["CATEGORY"].Should().Be("10");
        featRows[(int)FeatType.PiercingToss2]["CATEGORY"].Should().Be("10");
        featRows[(int)FeatType.PiercingToss3]["CATEGORY"].Should().Be("10");
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
        perk.Category.Should().Be(PerkCategoryType.ThrowingDeadeye);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Throwing, skillRank);

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statTypes.Length > 0)
            perkLevel.StatBonuses.Select(x => x.Stat).Should().HaveCount(statTypes.Length).And.Contain(statTypes);
        else
            perkLevel.StatBonuses.Should().BeEmpty();
    }

    private static void AssertAbility(
        AbilityDetail ability,
        string name,
        int level,
        RecastGroup recastGroup,
        float recastSeconds,
        float activationSeconds,
        int? staminaCost,
        bool isHostile,
        bool requiresTarget,
        bool isSingleTarget,
        bool isArea,
        AbilityActivationType activationType,
        float expectedMaxRange = 20f)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Throwing);
        ability.MaxRange.Should().Be(expectedMaxRange);
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

    private static Dictionary<PerkType, PerkDetail> BuildThrowingDeadeyePerksWithout2daLookup()
    {
        var definition = new ThrowingPerkDefinition();
        var methodNames = new[]
        {
            "BleedersEye",
            "DeadeyeMastery",
            "DeadeyeStance",
            "DeepWound",
            "FinishingToss",
            "MarkedTempo",
            "MarkingToss",
            "PerfectThrow",
            "PiercingToss",
            "PinningToss",
            "ReturningGrip",
            "RicochetToss",
            "SeveringToss"
        };

        foreach (var methodName in methodNames)
        {
            typeof(ThrowingPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(ThrowingPerkDefinition)
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
