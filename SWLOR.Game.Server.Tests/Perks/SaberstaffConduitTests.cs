using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class SaberstaffConduitTests
{
    [Test]
    public void SaberstaffConduitAbilities_MatchCombatBible()
    {
        var focusedArc = new FocusedArcAbilityDefinition().BuildAbilities();
        AssertAbility(focusedArc[FeatType.FocusedArc1], "Focused Arc I", 1, RecastGroup.FocusedArc, 30f, 0f, 3, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(focusedArc[FeatType.FocusedArc2], "Focused Arc II", 2, RecastGroup.FocusedArc, 30f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(focusedArc[FeatType.FocusedArc3], "Focused Arc III", 3, RecastGroup.FocusedArc, 30f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var guardedChannel = new GuardedChannelAbilityDefinition().BuildAbilities();
        AssertAbility(guardedChannel[FeatType.GuardedChannel1], "Guarded Channel I", 1, RecastGroup.GuardedChannel, 60f, 0f, 6, false, false, false, false, AbilityActivationType.Casted);
        AssertAbility(guardedChannel[FeatType.GuardedChannel2], "Guarded Channel II", 2, RecastGroup.GuardedChannel, 60f, 0f, 8, false, false, false, false, AbilityActivationType.Casted);
        AssertAbility(guardedChannel[FeatType.GuardedChannel3], "Guarded Channel III", 3, RecastGroup.GuardedChannel, 120f, 0f, 12, false, false, false, false, AbilityActivationType.Casted);

        var conduitStance = new ConduitStanceAbilityDefinition().BuildAbilities()[FeatType.ConduitStance1];
        AssertAbility(conduitStance, "Conduit Stance", 1, RecastGroup.ConduitStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var severFocus = new SeverFocusAbilityDefinition().BuildAbilities();
        AssertAbility(severFocus[FeatType.SeverFocus1], "Sever Focus I", 1, RecastGroup.SeverFocus, 45f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(severFocus[FeatType.SeverFocus2], "Sever Focus II", 2, RecastGroup.SeverFocus, 45f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var forceCapacitor = new ForceCapacitorAbilityDefinition().BuildAbilities()[FeatType.ForceCapacitor1];
        AssertAbility(forceCapacitor, "Force Capacitor", 1, RecastGroup.ForceCapacitor, 180f, 0f, 10, false, false, false, false, AbilityActivationType.Casted);

        var infiniteConduit = new InfiniteConduitAbilityDefinition().BuildAbilities()[FeatType.InfiniteConduit1];
        AssertAbility(infiniteConduit, "Infinite Conduit", 1, RecastGroup.Capstone, 345f, 2f, 15, false, false, false, false, AbilityActivationType.Casted);
    }

    [Test]
    public void SaberstaffConduitStatusEffects_MatchCombatBible()
    {
        var conduitStance = new ConduitStanceStatusEffect();
        conduitStance.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(15);
        conduitStance.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(15);
        conduitStance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);

        var guarded1 = new GuardedChannelStatusEffect(20, 20);
        guarded1.StatGroup.Stats[StatType.AttackDeflection].Should().Be(20);
        guarded1.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(20);
        var guarded2 = new GuardedChannelStatusEffect(30, 30);
        guarded2.StatGroup.Stats[StatType.AttackDeflection].Should().Be(30);
        guarded2.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(30);
        var guarded3 = new GuardedChannelStatusEffect(40, 35);
        guarded3.StatGroup.Stats[StatType.AttackDeflection].Should().Be(40);
        guarded3.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(35);

        var forceLensAlly = new ForceLensStatusEffect();
        forceLensAlly.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(15);
        var forceLensSelf = new ForceLensStatusEffect();
        forceLensSelf.ApplyEffect(1, 1, 45);
        forceLensSelf.StatGroup.Stats.ContainsKey(StatType.ForceDefensePercentAdjustment).Should().BeFalse();
        forceLensSelf.StatGroup.Stats[StatType.AttackDeflection].Should().Be(8);

        var forceCapacitor = new ForceCapacitorStatusEffect();
        forceCapacitor.StatGroup.Stats[StatType.AbilityStaminaCostFPRestorePercentSkillType].Should().Be((int)SkillType.Saberstaff);
        forceCapacitor.StatGroup.Stats[StatType.AbilityStaminaCostFPRestorePercent].Should().Be(25);
        forceCapacitor.StatGroup.Stats[StatType.AbilityFPCostStaminaRestorePercentSkillType].Should().Be((int)SkillType.Force);
        forceCapacitor.StatGroup.Stats[StatType.AbilityFPCostStaminaRestorePercent].Should().Be(25);

        var infiniteConduit = new InfiniteConduitStatusEffect();
        infiniteConduit.StatGroup.Stats[StatType.AutoAttackFPRestore].Should().Be(2);
        infiniteConduit.StatGroup.Stats[StatType.SkillAbilityStaminaCostFlatAdjustment].Should().Be(-2);

        var fracturedFocus = new FracturedFocusStatusEffect();
        fracturedFocus.StatGroup.Stats[StatType.FPCostPercentAdjustment].Should().Be(100);
    }

    [Test]
    public void ResourceRestoreFromAbilityCost_RoundsUpAndRequiresPositiveCost()
    {
        InvokeCombatPrivateStatic<int>("CalculateResourceRestoreFromCost", 0, 25).Should().Be(0);
        InvokeCombatPrivateStatic<int>("CalculateResourceRestoreFromCost", 3, 25).Should().Be(1);
        InvokeCombatPrivateStatic<int>("CalculateResourceRestoreFromCost", 6, 25).Should().Be(2);
        InvokeCombatPrivateStatic<int>("CalculateResourceRestoreFromCost", 10, 25).Should().Be(3);
    }

    [Test]
    public void SaberstaffConduitFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.FocusedArc1, "ife_focusarc1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.FocusedArc2, "ife_focusarc2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.FocusedArc3, "ife_focusarc3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.GuardedChannel1, "ife_grdedchan1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.GuardedChannel2, "ife_grdedchan2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.GuardedChannel3, "ife_grdedchan3", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ConduitStance1, "ife_condstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SeverFocus1, "ife_sevfoc1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SeverFocus2, "ife_sevfoc2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceCapacitor1, "ife_forcecap1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.InfiniteConduit1, "ife_infcond1", "P", "0x01", "0", "****", "****", "****", "****")
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
        perk.Category.Should().Be(PerkCategoryType.SaberstaffConduit);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Saberstaff, skillRank);

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
        ability.SkillType.Should().Be(SkillType.Saberstaff);
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

    private static T InvokeCombatPrivateStatic<T>(string methodName, params object[] args)
    {
        return (T)typeof(Combat)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, args)!;
    }

    private static Dictionary<PerkType, PerkDetail> BuildSaberstaffConduitPerksWithout2daLookup()
    {
        var definition = new SaberstaffPerkDefinition();
        var methodNames = new[]
        {
            "BalancedAttunement",
            "ConduitFlare",
            "ConduitStance",
            "ConduitTraining",
            "EnergizedForms",
            "FocusedArc",
            "ForceCapacitor",
            "ForceLens",
            "GuardedChannel",
            "InfiniteConduit",
            "SeverFocus"
        };

        foreach (var methodName in methodNames)
        {
            typeof(SaberstaffPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(SaberstaffPerkDefinition)
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
