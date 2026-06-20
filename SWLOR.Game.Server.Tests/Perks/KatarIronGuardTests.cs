using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Katar;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class KatarIronGuardTests
{
    [Test]
    public void KatarIronGuardAbilities_MatchCombatBible()
    {
        var guardCounter = new GuardCounterAbilityDefinition().BuildAbilities();
        AssertAbility(guardCounter[FeatType.GuardCounter1], "Guard Counter I", 1, RecastGroup.GuardCounter, 30f, 0f, 3, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(guardCounter[FeatType.GuardCounter2], "Guard Counter II", 2, RecastGroup.GuardCounter, 30f, 0f, 5, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(guardCounter[FeatType.GuardCounter3], "Guard Counter III", 3, RecastGroup.GuardCounter, 45f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);

        var twinGuardStance = new TwinGuardStanceAbilityDefinition().BuildAbilities()[FeatType.TwinGuardStance1];
        AssertAbility(twinGuardStance, "Twin Guard Stance", 1, RecastGroup.TwinGuardStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var twinIntercept = new TwinInterceptAbilityDefinition().BuildAbilities()[FeatType.TwinIntercept1];
        AssertAbility(twinIntercept, "Twin Intercept", 1, RecastGroup.TwinIntercept, 120f, 0f, 10, false, true, true, false, AbilityActivationType.Casted);
        twinIntercept.MaxRange.Should().Be(6f);
        twinIntercept.CustomValidation.Should().NotBeNull();

        var whirlingGuard = new WhirlingGuardAbilityDefinition().BuildAbilities()[FeatType.WhirlingGuard1];
        AssertAbility(whirlingGuard, "Whirling Guard", 1, RecastGroup.WhirlingGuard, 120f, 0f, 12, false, false, false, false, AbilityActivationType.Casted);


        var ironWallStance = new IronWallStanceAbilityDefinition().BuildAbilities()[FeatType.IronWallStance1];
        AssertAbility(ironWallStance, "Iron Wall Stance", 1, RecastGroup.IronWallStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var adamantineGuard = new AdamantineGuardAbilityDefinition().BuildAbilities()[FeatType.AdamantineGuard1];
        AssertAbility(adamantineGuard, "Adamantine Guard", 1, RecastGroup.Capstone, 345f, 1f, 15, false, false, false, false, AbilityActivationType.Casted);
    }

    [Test]
    public void KatarIronGuardStatusEffects_MatchCombatBible()
    {
        var twinGuard = new TwinGuardStanceStatusEffect();
        twinGuard.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(15);
        twinGuard.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(20);
        twinGuard.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);
        twinGuard.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(0);

        var coveringClaws = new CoveringClawsStatusEffect();
        coveringClaws.StatGroup.Stats[StatType.EnmityToStatusSourcePercentAdjustment].Should().Be(25);

        var twinIntercept = new TwinInterceptStatusEffect();
        twinIntercept.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(15);
        twinIntercept.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(0);

        var whirlingGuard = new WhirlingGuardStatusEffect();
        whirlingGuard.StatGroup.Stats[StatType.Guard].Should().Be(20);
        whirlingGuard.StatGroup.Stats[StatType.GuardRetaliationDamage].Should().Be(8);

        var ironWall = new IronWallStanceStatusEffect();
        ironWall.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(25);
        ironWall.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(20);
        ironWall.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(30);
        ironWall.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-25);

        var adamantineGuard = new AdamantineGuardStatusEffect();
        adamantineGuard.StatGroup.Stats[StatType.Guard].Should().Be(25);
        adamantineGuard.StatGroup.Stats[StatType.GuardDamageReductionPercentAdjustment].Should().Be(20);
        adamantineGuard.StatGroup.Stats[StatType.GuardEnmityPercentAdjustment].Should().Be(75);
    }

    [Test]
    public void GuardTraining_GrantsPerpetualGuardBonuses()
    {
        var perk = BuildKatarIronGuardPerksWithout2daLookup()[PerkType.GuardTraining];

        AssertPerkLevel(
            perk,
            "Guard Training",
            1,
            2,
            2,
            FeatType.GuardTrainingTrait,
            "Grants a 15% chance to guard against physical attacks, reducing that hit's damage by 20% and generating extra enmity.",
            StatType.Guard);
        AssertPerpetualStatBonus(perk.PerkLevels[1], StatType.Guard, 15);

        AssertPerkLevel(
            perk,
            "Guard Training",
            2,
            2,
            15,
            null,
            "Guard chance increases to 25% and guarded hits restore 2 STM.",
            StatType.Guard,
            StatType.GuardStaminaRestore);
        AssertPerpetualStatBonus(perk.PerkLevels[2], StatType.Guard, 25);
        AssertPerpetualStatBonus(perk.PerkLevels[2], StatType.GuardStaminaRestore, 2);

        AssertPerkLevel(
            perk,
            "Guard Training",
            3,
            4,
            28,
            null,
            "Guard chance increases to 35% and guarded hits reduce physical damage by 30%.",
            StatType.Guard,
            StatType.GuardStaminaRestore,
            StatType.GuardDamageReductionPercentAdjustment);
        AssertPerpetualStatBonus(perk.PerkLevels[3], StatType.Guard, 35);
        AssertPerpetualStatBonus(perk.PerkLevels[3], StatType.GuardStaminaRestore, 2);
        AssertPerpetualStatBonus(perk.PerkLevels[3], StatType.GuardDamageReductionPercentAdjustment, 10);
    }

    [Test]
    public void GuardianReflexes_GrantsLowHPGuardWindow()
    {
        var perk = BuildKatarIronGuardPerksWithout2daLookup()[PerkType.GuardianReflexes];

        AssertPerkLevel(
            perk,
            "Guardian Reflexes",
            1,
            4,
            48,
            FeatType.GuardianReflexesTrait,
            "When reduced below 30% HP, gain +25% guard chance for 30 seconds. This can only trigger once every 3 minutes.",
            StatType.LowHPGuardThresholdPercent,
            StatType.LowHPGuard,
            StatType.LowHPGuardDurationSeconds,
            StatType.LowHPGuardCooldownSeconds);
        AssertPerpetualStatBonus(perk.PerkLevels[1], StatType.LowHPGuardThresholdPercent, 30);
        AssertPerpetualStatBonus(perk.PerkLevels[1], StatType.LowHPGuard, 25);
        AssertPerpetualStatBonus(perk.PerkLevels[1], StatType.LowHPGuardDurationSeconds, 30);
        AssertPerpetualStatBonus(perk.PerkLevels[1], StatType.LowHPGuardCooldownSeconds, 180);
    }

    [Test]
    public void KatarIronGuardFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.GuardCounter1, "ife_grdcntr1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.GuardCounter2, "ife_grdcntr2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.GuardCounter3, "ife_grdcntr3", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.TwinGuardStance1, "ife_twingrdstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.TwinIntercept1, "ife_twinintc1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.WhirlingGuard1, "ife_whirlgrd1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.IronWallStance1, "ife_ironwallstn1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.AdamantineGuard1, "ife_adamgrd1", "P", "0x01", "0", "****", "****", "****", "****")
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
        perk.Category.Should().Be(PerkCategoryType.KatarIronGuard);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Katar, skillRank);

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statTypes.Length > 0)
            perkLevel.StatBonuses.Select(x => x.Stat).Should().HaveCount(statTypes.Length).And.Contain(statTypes);
        else
            perkLevel.StatBonuses.Should().BeEmpty();
    }

    private static void AssertPerpetualStatBonus(PerkLevel level, StatType stat, int expectedValue)
    {
        level.StatBonuses
            .Should()
            .ContainSingle(x => x.Stat == stat)
            .Which
            .Calculate(0)
            .Should()
            .Be(expectedValue);
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
        ability.SkillType.Should().Be(SkillType.Katar);
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

    private static Dictionary<PerkType, PerkDetail> BuildKatarIronGuardPerksWithout2daLookup()
    {
        var definition = new KatarPerkDefinition();
        var methodNames = new[]
        {
            "AdamantineGuard",
            "BreakerReversal",
            "CoveringClaws",
            "GuardCounter",
            "GuardTraining",
            "GuardianReflexes",
            "ImpenetrableGrip",
            "IronElbows",
            "IronWallStance",
            "RedirectingGuard",
            "RetaliatoryFlow",
            "TwinGuardStance",
            "TwinIntercept",
            "WhirlingGuard"
        };

        foreach (var methodName in methodNames)
        {
            typeof(KatarPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(KatarPerkDefinition)
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
