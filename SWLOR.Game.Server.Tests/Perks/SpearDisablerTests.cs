using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Spear;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class SpearDisablerTests
{
    [Test]
    public void SpearDisablerAbilities_MatchCombatBible()
    {
        var disablingStrike = new DisablingStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(disablingStrike[FeatType.DisablingStrike1], "Disabling Strike I", 1, RecastGroup.DisablingStrike, 30f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(disablingStrike[FeatType.DisablingStrike2], "Disabling Strike II", 2, RecastGroup.DisablingStrike, 30f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(disablingStrike[FeatType.DisablingStrike3], "Disabling Strike III", 3, RecastGroup.DisablingStrike, 30f, 0f, 16, true, false, true, false, AbilityActivationType.Weapon);

        var interruptionStrike = new InterruptionStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(interruptionStrike[FeatType.InterruptionStrike1], "Interruption Strike I", 1, RecastGroup.InterruptionStrike, 30f, 0f, 5, true, false, true, false, AbilityActivationType.Casted);
        AssertAbility(interruptionStrike[FeatType.InterruptionStrike2], "Interruption Strike II", 2, RecastGroup.InterruptionStrike, 30f, 0f, 8, true, false, true, false, AbilityActivationType.Casted);

        var perceptiveStance = new PerceptiveStanceAbilityDefinition().BuildAbilities()[FeatType.PerceptiveStance1];
        AssertAbility(perceptiveStance, "Perceptive Stance", 1, RecastGroup.PerceptiveStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var forceSuppression = new ForceSuppressionAbilityDefinition().BuildAbilities()[FeatType.ForceSuppression1];
        AssertAbility(forceSuppression, "Force Suppression", 1, RecastGroup.ForceSuppression, 30f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);

        var disruptionField = new DisruptionFieldAbilityDefinition().BuildAbilities()[FeatType.DisruptionField1];
        AssertAbility(disruptionField, "Disruption Field", 1, RecastGroup.DisruptionField, 180f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var totalForceDenial = new TotalForceDenialAbilityDefinition().BuildAbilities()[FeatType.TotalForceDenial1];
        AssertAbility(totalForceDenial, "Total Force Denial", 1, RecastGroup.TotalForceDenial, 300f, 2f, 14, true, false, false, true, AbilityActivationType.Casted);

    }

    [Test]
    public void SpearDisablerStatusEffects_MatchCombatBible()
    {
        var perceptiveStance = new PerceptiveStanceStatusEffect();
        perceptiveStance.StatGroup.Stats[StatType.CriticalRatePercentAdjustment].Should().Be(10);
        perceptiveStance.StatGroup.Stats[StatType.CriticalDamagePercentAdjustment].Should().Be(15);

        var forceErosion = new ForceErosionStatusEffect();
        forceErosion.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-10);

        var foggyMind = new FoggyMindStatusEffect(2);
        foggyMind.StatGroup.Stats[StatType.ActivationDelayFlatAdjustment].Should().Be(2);

        var forceSuppression = new ForceSuppressionStatusEffect();
        forceSuppression.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(-15);

        var forceNullification = new ForceDisruptionStatusEffect(true);
        forceNullification.StatGroup.Stats[StatType.ForceAbilityActivationDisabled].Should().Be(1);

        var fracturedFocus = new FracturedFocusStatusEffect();
        fracturedFocus.StatGroup.Stats[StatType.FPCostPercentAdjustment].Should().Be(100);

        var forcebane = new ForcebaneStatusEffect();
        forcebane.StatGroup.Stats[StatType.FPRestorePercentAdjustment].Should().Be(-75);
    }

    [Test]
    public void SpearDisablerFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.DisablingStrike1, "ife_disabstrk1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.DisablingStrike2, "ife_disabstrk2", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.DisablingStrike3, "ife_disabstrk3", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.InterruptionStrike1, "ife_intrstrk1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.InterruptionStrike2, "ife_intrstrk2", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.PerceptiveStance1, "ife_percpstnc1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ForceSuppression1, "ife_forcesup1", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.DisruptionField1, "ife_disrpfld1", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.TotalForceDenial1, "ife_totforceden1", "0x3E", "1", "cone", "5", "5", "17"),
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            abilityRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

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
        perk.Category.Should().Be(PerkCategoryType.SpearDisabler);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Spear, skillRank);

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
        ability.SkillType.Should().Be(SkillType.Spear);
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

    private static Dictionary<PerkType, PerkDetail> BuildSpearDisablerPerksWithout2daLookup()
    {
        var definition = new SpearPerkDefinition();
        var methodNames = new[]
        {
            "DisablingStrike",
            "DisruptionExpert",
            "DisruptionField",
            "ErosionStrike",
            "ForceNullification",
            "ForcePiercing",
            "ForceSuppression",
            "ForceWarding",
            "Forcebane",
            "FractureStrike",
            "InterruptionStrike",
            "PerceptiveStance",
            "TotalForceDenial"
        };

        foreach (var methodName in methodNames)
        {
            typeof(SpearPerkDefinition)
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(SpearPerkDefinition)
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
