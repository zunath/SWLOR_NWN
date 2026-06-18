using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class TwinBladeDuelistTests
{
    [Test]
    public void TwinBladeDuelistAbilities_MatchCombatBible()
    {
        var splitGuardStrike = new SplitGuardStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(splitGuardStrike[FeatType.SplitGuardStrike1], "Split Guard Strike I", 1, RecastGroup.SplitGuardStrike, 30f, 0f, 3, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(splitGuardStrike[FeatType.SplitGuardStrike2], "Split Guard Strike II", 2, RecastGroup.SplitGuardStrike, 30f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(splitGuardStrike[FeatType.SplitGuardStrike3], "Split Guard Strike III", 3, RecastGroup.SplitGuardStrike, 30f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var feintingCut = new FeintingCutAbilityDefinition().BuildAbilities();
        AssertAbility(feintingCut[FeatType.FeintingCut1], "Feinting Cut I", 1, RecastGroup.FeintingCut, 45f, 0f, 4, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(feintingCut[FeatType.FeintingCut2], "Feinting Cut II", 2, RecastGroup.FeintingCut, 45f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(feintingCut[FeatType.FeintingCut3], "Feinting Cut III", 3, RecastGroup.FeintingCut, 45f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var duelistStance = new DuelistStanceAbilityDefinition().BuildAbilities()[FeatType.DuelistStance1];
        AssertAbility(duelistStance, "Duelist Stance", 1, RecastGroup.DuelistStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var bindingCross = new BindingCrossAbilityDefinition().BuildAbilities();
        AssertAbility(bindingCross[FeatType.BindingCross1], "Binding Cross I", 1, RecastGroup.BindingCross, 60f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(bindingCross[FeatType.BindingCross2], "Binding Cross II", 2, RecastGroup.BindingCross, 60f, 0f, 12, true, true, true, false, AbilityActivationType.Casted);

        var challenge = new DuelistsChallengeAbilityDefinition().BuildAbilities()[FeatType.DuelistsChallenge1];
        AssertAbility(challenge, "Duelist's Challenge", 1, RecastGroup.DuelistsChallenge, 120f, 0f, 12, true, true, true, false, AbilityActivationType.Casted);

        var finalForm = new FinalFormAbilityDefinition().BuildAbilities()[FeatType.FinalForm1];
        AssertAbility(finalForm, "Final Form", 1, RecastGroup.Capstone, 345f, 2f, 15, false, false, false, false, AbilityActivationType.Casted);
    }

    [Test]
    public void TwinBladeDuelistStatusEffects_MatchCombatBible()
    {
        var splitGuard1 = new SplitGuardStrikeStatusEffect(15);
        splitGuard1.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(15);
        var splitGuard2 = new SplitGuardStrikeStatusEffect(20);
        splitGuard2.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(20);
        var splitGuard3 = new SplitGuardStrikeStatusEffect(25);
        splitGuard3.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(25);

        var weakened1 = new WeakenedStatusEffect(10);
        weakened1.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);
        var weakened2 = new WeakenedStatusEffect();
        weakened2.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);
        var weakened3 = new WeakenedStatusEffect(20);
        weakened3.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-20);

        var duelistStance = new DuelistStanceStatusEffect();
        duelistStance.StatGroup.Stats[StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment].Should().Be(15);
        duelistStance.StatGroup.Stats[StatType.SingleTargetAbilityAttackDeflectionSkillType].Should().Be((int)SkillType.TwinBlade);
        duelistStance.StatGroup.Stats[StatType.SingleTargetAbilityAttackDeflection].Should().Be(10);
        duelistStance.StatGroup.Stats[StatType.SingleTargetAbilityAttackDeflectionDurationSeconds].Should().Be(6);
        duelistStance.StatGroup.Stats[StatType.TwinBladeAreaAbilityDamagePercentAdjustment].Should().Be(-15);
        duelistStance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);

        var challengeTarget = new DuelistsChallengeStatusEffect();
        challengeTarget.StatGroup.Stats[StatType.DamageToStatusSourcePercentAdjustment].Should().Be(20);
        challengeTarget.StatGroup.Stats[StatType.DamageTakenFromStatusSourcePercentAdjustment].Should().Be(20);
        var challengeSelf = new DuelistsChallengeSelfStatusEffect();
        challengeSelf.StatGroup.Stats[StatType.DefenseAgainstStatusSourcePercentAdjustment].Should().Be(20);

        var finalForm = new FinalFormStatusEffect();
        finalForm.StatGroup.Stats[StatType.SingleTargetPhysicalAbilityDamagePercentAdjustment].Should().Be(15);
        finalForm.StatGroup.Stats[StatType.AttackDeflection].Should().Be(15);
        finalForm.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);
        Stat.GetStatTypeCategory(StatType.SingleTargetPhysicalAbilityDamagePercentAdjustment).Should().Be(StatTypeCategory.BeneficialWhenPositive);
    }

    [Test]
    public void ReversalCutReady_RefreshesFromNonTemporaryStatValues()
    {
        var root = FindRepositoryRoot();
        var combatSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        var statSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Stat.cs").FullName);
        var impactBonus = ExtractMethod(combatSource, "public static int GetAbilityImpactBaseDamageBonus(").Replace("\r\n", "\n");
        var duelistImpactRiders = ExtractMethod(combatSource, "private static void ApplyTwinBladeDuelistImpactRiders(").Replace("\r\n", "\n");
        var reversalCutTrigger = ExtractMethod(combatSource, "private static bool AbilityMatchesReversalCutTrigger(");
        var reversalCutReady = ExtractMethod(combatSource, "private static void ApplyReversalCutReady(").Replace("\r\n", "\n");
        var nonTemporaryStatAdjustment = ExtractMethod(statSource, "public static int GetStatAdjustmentExcludingTemporaryModifiers(");
        var perks = BuildTwinBladeDuelistPerksWithout2daLookup();
        var reversalCut = perks[PerkType.ReversalCut].PerkLevels[1];

        reversalCutReady.Should().Contain(
            "Stat.GetStatAdjustmentExcludingTemporaryModifiers(\n                defender,\n                StatType.TwinBladeDuelistReversalCutDamageBonus)");
        reversalCutReady.Should().Contain(
            "Stat.GetStatAdjustmentExcludingTemporaryModifiers(\n                defender,\n                StatType.TwinBladeDuelistReversalCutDazedDurationSeconds)");
        reversalCutReady.Should().NotContain(
            "Stat.GetStatAdjustment(defender, StatType.TwinBladeDuelistReversalCutDamageBonus)");
        reversalCutReady.Should().NotContain(
            "Stat.GetStatAdjustment(defender, StatType.TwinBladeDuelistReversalCutDazedDurationSeconds)");

        nonTemporaryStatAdjustment.Should().Contain("StatusEffect.GetCreatureStatusEffects(creature).StatGroup.Stats[stat]");
        nonTemporaryStatAdjustment.Should().Contain("Perk.GetStatBonus(creature, stat)");
        nonTemporaryStatAdjustment.Should().NotContain("TemporaryStatModifier.GetStatAdjustment");

        reversalCut.StatBonuses
            .Should()
            .ContainSingle(x => x.Stat == StatType.TwinBladeDuelistReversalCutTriggerPrimaryPerkType && x.Calculate(0) == (int)PerkType.SplitGuardStrike);
        reversalCut.StatBonuses
            .Should()
            .ContainSingle(x => x.Stat == StatType.TwinBladeDuelistReversalCutTriggerSecondaryPerkType && x.Calculate(0) == (int)PerkType.FeintingCut);
        reversalCut.StatBonuses
            .Should()
            .ContainSingle(x => x.Stat == StatType.TwinBladeDuelistReversalCutTriggerTertiaryPerkType && x.Calculate(0) == (int)PerkType.BindingCross);

        impactBonus.Should().Contain("AbilityMatchesReversalCutTrigger(activator, ability)");
        duelistImpactRiders.Should().Contain("AbilityMatchesReversalCutTrigger(activator, ability)");
        reversalCutTrigger.Should().Contain("AbilityMatchesAnyPerkTypeStat(");
        reversalCutTrigger.Should().Contain("StatType.TwinBladeDuelistReversalCutTriggerPrimaryPerkType");
        reversalCutTrigger.Should().Contain("StatType.TwinBladeDuelistReversalCutTriggerSecondaryPerkType");
        reversalCutTrigger.Should().Contain("StatType.TwinBladeDuelistReversalCutTriggerTertiaryPerkType");
    }

    [Test]
    public void TwinBladeDuelistFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.SplitGuardStrike1, "ife_splitgrdstr1", "M", "0x02", "1"),
            (FeatType.SplitGuardStrike2, "ife_splitgrdstr2", "M", "0x02", "1"),
            (FeatType.SplitGuardStrike3, "ife_splitgrdstr3", "M", "0x02", "1"),
            (FeatType.FeintingCut1, "ife_feintcut1", "M", "0x02", "1"),
            (FeatType.FeintingCut2, "ife_feintcut2", "M", "0x02", "1"),
            (FeatType.FeintingCut3, "ife_feintcut3", "M", "0x02", "1"),
            (FeatType.DuelistStance1, "ife_duelstnc1", "P", "0x01", "0"),
            (FeatType.BindingCross1, "ife_bindcrs1", "M", "0x02", "1"),
            (FeatType.BindingCross2, "ife_bindcrs2", "M", "0x02", "1"),
            (FeatType.DuelistsChallenge1, "ife_duelchal1", "M", "0x02", "1"),
            (FeatType.FinalForm1, "ife_finalform1", "P", "0x01", "0")
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
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            abilityRow["Range"].Should().Be(range);
            abilityRow["TargetType"].Should().Be(targetType);
            abilityRow["HostileSetting"].Should().Be(hostileSetting);
            abilityRow["TargetShape"].Should().Be("****");
            abilityRow["TargetSizeX"].Should().Be("****");
            abilityRow["TargetSizeY"].Should().Be("****");
            abilityRow["TargetFlags"].Should().Be("****");
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
        perk.Category.Should().Be(PerkCategoryType.TwinBladeDuelist);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.TwinBlade, skillRank);

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
        ability.SkillType.Should().Be(SkillType.TwinBlade);
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

    private static Dictionary<PerkType, PerkDetail> BuildTwinBladeDuelistPerksWithout2daLookup()
    {
        var definition = new TwinBladePerkDefinition();
        var methodNames = new[]
        {
            "BindingCross",
            "CenterlineGuard",
            "DuelistsChallenge",
            "DuelistStance",
            "FeintingCut",
            "FinalForm",
            "GuardedFlow",
            "MirrorStep",
            "PerfectBalance",
            "PrecisionArc",
            "PunishingAngle",
            "ReversalCut",
            "SplitGuardStrike"
        };

        foreach (var methodName in methodNames)
        {
            typeof(TwinBladePerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(TwinBladePerkDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        signatureIndex.Should().BeGreaterThanOrEqualTo(0);

        var openBraceIndex = source.IndexOf('{', signatureIndex);
        openBraceIndex.Should().BeGreaterThanOrEqualTo(0);

        var depth = 0;
        for (var index = openBraceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                    return source.Substring(signatureIndex, index - signatureIndex + 1);
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
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
