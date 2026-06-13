using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Pistol;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class PistolSkirmisherTests
{
    [Test]
    public void PistolSkirmisherAbilities_MatchCombatBible()
    {
        var disarmingShot = new DisarmingShotAbilityDefinition().BuildAbilities();
        AssertAbility(disarmingShot[FeatType.DisarmingShot1], "Disarming Shot I", 1, RecastGroup.DisarmingShot, 30f, 0f, 3, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(disarmingShot[FeatType.DisarmingShot2], "Disarming Shot II", 2, RecastGroup.DisarmingShot, 30f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(disarmingShot[FeatType.DisarmingShot3], "Disarming Shot III", 3, RecastGroup.DisarmingShot, 30f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var skirmisherStance = new SkirmisherStanceAbilityDefinition().BuildAbilities()[FeatType.SkirmisherStance1];
        AssertAbility(skirmisherStance, "Skirmisher Stance", 1, RecastGroup.SkirmisherStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var interruptingShot = new InterruptingShotAbilityDefinition().BuildAbilities();
        AssertAbility(interruptingShot[FeatType.InterruptingShot1], "Interrupting Shot I", 1, RecastGroup.InterruptingShot, 45f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(interruptingShot[FeatType.InterruptingShot2], "Interrupting Shot II", 2, RecastGroup.InterruptingShot, 45f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var pointBlankBurst = new PointBlankBurstAbilityDefinition().BuildAbilities()[FeatType.PointBlankBurst1];
        AssertAbility(pointBlankBurst, "Point Blank Burst", 1, RecastGroup.PointBlankBurst, 90f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var smokeRound = new SmokeRoundAbilityDefinition().BuildAbilities()[FeatType.SmokeRound1];
        AssertAbility(smokeRound, "Smoke Round", 1, RecastGroup.SmokeRound, 120f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var lastWord = new LastWordAbilityDefinition().BuildAbilities()[FeatType.LastWord1];
        AssertAbility(lastWord, "Last Word", 1, RecastGroup.Capstone, 345f, 1f, 15, true, false, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void PistolSkirmisherStatusEffects_MatchCombatBible()
    {
        var snapRoll1 = new SnapRollStatusEffect();
        snapRoll1.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(25);

        var snapRoll2 = new SnapRollStatusEffect(35);
        snapRoll2.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(35);

        var skirmisherStance = new SkirmisherStanceStatusEffect();
        skirmisherStance.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(15);
        skirmisherStance.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(-20);
        skirmisherStance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);

        new WeakenedStatusEffect(10).StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);
        new WeakenedStatusEffect().StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);
        new WeakenedStatusEffect(20).StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-20);

        var foggyMind = new FoggyMindStatusEffect(2);
        foggyMind.StatGroup.Stats[StatType.ActivationDelayFlatAdjustment].Should().Be(2);

        var lastWord = new LastWordStatusEffect();
        lastWord.StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(-10);
    }

    [Test]
    public void PistolSkirmisherFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.DisarmingShot1, "ife_disarmshot1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SkirmisherStance1, "ife_skirmstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.InterruptingShot1, "ife_intrshot1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.DisarmingShot2, "ife_disarmshot2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.InterruptingShot2, "ife_intrshot2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.PointBlankBurst1, "ife_ptblankburs1", "M", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.DisarmingShot3, "ife_disarmshot3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SmokeRound1, "ife_smokrnd1", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.LastWord1, "ife_lastword1", "M", "0x3E", "1", "cone", "5", "5", "17")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            abilityRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            abilityRow["Range"].Should().Be(range);
            abilityRow["TargetType"].Should().Be(targetType);
            abilityRow["HostileSetting"].Should().Be(hostileSetting);
            abilityRow["TargetShape"].Should().Be(targetShape);
            abilityRow["TargetSizeX"].Should().Be(targetSizeX);
            abilityRow["TargetSizeY"].Should().Be(targetSizeY);
            abilityRow["TargetFlags"].Should().Be(targetFlags);
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
        perk.Category.Should().Be(PerkCategoryType.PistolSkirmisher);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Pistol, skillRank);

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
        AbilityActivationType activationType)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Pistol);
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

    private static Dictionary<PerkType, PerkDetail> BuildPistolSkirmisherPerksWithout2daLookup()
    {
        var definition = new PistolPerkDefinition();
        var methodNames = new[]
        {
            "DisarmingShot",
            "DuelistsDistance",
            "EvasiveReload",
            "InterruptingShot",
            "KitingInstinct",
            "LastWord",
            "LowShot",
            "MobileFootwork",
            "PointBlankBurst",
            "RicochetShot",
            "SkirmishersNerve",
            "SkirmisherStance",
            "SmokeRound",
            "SnapRoll"
        };

        foreach (var methodName in methodNames)
        {
            typeof(PistolPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(PistolPerkDefinition)
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
