using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Pins the Forgemaster and Artisan perk lines: the schedule, the prices, that every perk grants a stat the Plasma Forge reads,
/// and what a crafter who owns every purchase up to a rank gets.
/// </summary>
public class SmitheryPerkLineTests
{
    private static readonly PerkType[] Forgemaster = ForgePerkLines.Forgemaster.ToArray();

    private static readonly PerkType[] Artisan = ForgePerkLines.Artisan.ToArray();

    private static readonly int[] ScheduleRanks = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
    private static readonly int[] SchedulePrices = { 1, 1, 2, 2, 3, 3, 3, 3, 3, 4 };

    private static Dictionary<PerkType, PerkDetail> Perks() => new SmitheryPerkDefinition().BuildPerks();

    private static IEnumerable<(PerkType Perk, int Level, int Rank, int Price)> Purchases(IEnumerable<PerkType> line)
    {
        var perks = Perks();
        foreach (var type in line)
        {
            foreach (var (level, detail) in perks[type].PerkLevels.OrderBy(pair => pair.Key))
            {
                var rank = ForgePerkLines.RequiredRank(detail);
                yield return (type, level, rank, detail.Price);
            }
        }
    }

    private static ForgePerkStats StatsAt(int rank, params IReadOnlyList<PerkType>[] lines) =>
        ForgePerkLines.StatsAt(Perks(), lines.SelectMany(line => line), rank);

    /// <summary>The adjustments an effect sets, without the zero entries every stat group starts with.</summary>
    private static Dictionary<StatType, int> Granted(StatusEffectBase effect) =>
        effect.StatGroup.Stats.Where(pair => pair.Value != 0).ToDictionary(pair => pair.Key, pair => pair.Value);

    private static IEnumerable<StatType> ForgeStats() =>
        Enum.GetValues<StatType>().Where(stat => stat.ToString().StartsWith("Forge", StringComparison.Ordinal));

    [TestCase(nameof(Forgemaster))]
    [TestCase(nameof(Artisan))]
    public void EachLine_HasTenPurchasesAtTheCraftingScheduleCostingTwentyFiveSp(string line)
    {
        var purchases = Purchases(line == nameof(Forgemaster) ? Forgemaster : Artisan).OrderBy(purchase => purchase.Rank).ToList();

        purchases.Select(purchase => purchase.Rank).Should().Equal(ScheduleRanks);
        purchases.Select(purchase => purchase.Price).Should().Equal(SchedulePrices);
        purchases.Sum(purchase => purchase.Price).Should().Be(25);
    }

    [Test]
    public void TheStyleIsTheRankTwentyPurchase_AndTheCapstoneIsTheRankFiftyOneWithNoQuestRequirement()
    {
        var perks = Perks();
        foreach (var style in new[] { PerkType.HotForgingStyle, PerkType.ColdWorkingStyle })
        {
            perks[style].PerkLevels.Should().ContainSingle();
            perks[style].PerkLevels[1].Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank.Should().Be(ForgePerkRules.StyleRank);
            perks[style].PerkLevels[1].Price.Should().Be(2);
        }

        foreach (var capstone in new[] { PerkType.SmitheryRedLine, PerkType.SmitheryPerfectTemper })
        {
            perks[capstone].PerkLevels.Should().ContainSingle();
            perks[capstone].PerkLevels[1].Requirements.Should().ContainSingle(requirement => requirement is PerkRequirementSkill);
            perks[capstone].PerkLevels[1].Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank.Should().Be(50);
            perks[capstone].PerkLevels[1].Price.Should().Be(4);
        }
    }

    [Test]
    public void EveryPerkIsASmitheryPerkRequiringOnlySmitheryRank_AndEitherStyleMayBeOwnedWithTheOther()
    {
        var perks = Perks();

        perks.Keys.Should().BeEquivalentTo(Forgemaster.Concat(Artisan));
        foreach (var (type, detail) in perks)
        {
            detail.Category.Should().Be(PerkCategoryType.Smithery, type.ToString());
            detail.IsActive.Should().BeTrue();
            detail.GroupType.Should().Be(PerkGroupType.Player);
            detail.Name.Should().NotBeNullOrWhiteSpace();
            foreach (var level in detail.PerkLevels.Values)
            {
                level.Requirements.Should().ContainSingle().Which.Should().BeOfType<PerkRequirementSkill>();
                level.Requirements.OfType<PerkRequirementSkill>().Single().Type.Should().Be(SkillType.Smithery);
            }
        }

        // Smithery is not activity-split, so no style locks out the other: a crafter toggles between the styles they own.
        perks.Values.SelectMany(detail => detail.PerkLevels.Values).SelectMany(level => level.Requirements)
            .Should().NotContain(requirement => requirement is PerkRequirementCannotHavePerk);
    }

    [Test]
    public void ARankedPerkRestatesTheBenefitItsLowerRankGave_SoThePurchasedRankAloneIsComplete()
    {
        foreach (var type in new[] { PerkType.SmitheryHeatRetention, PerkType.SmitheryCleanContainment })
        {
            var levels = Perks()[type].PerkLevels.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList();
            levels.Should().HaveCount(2, type.ToString());
            levels[0].StatBonuses.Single().Stat.Should().Be(levels[1].StatBonuses.Single().Stat, type.ToString());
            levels[1].StatBonuses.Single().Calculate(0).Should().BeGreaterThan(levels[0].StatBonuses.Single().Calculate(0), type.ToString());
        }
    }

    [Test]
    public void OnlyTwoPerksAreRanked_SoTheLinesHaveNinePerksAndTenPurchasesEach()
    {
        var perks = Perks();

        perks.Where(pair => pair.Value.PerkLevels.Count > 1).Select(pair => pair.Key)
            .Should().BeEquivalentTo(new[] { PerkType.SmitheryHeatRetention, PerkType.SmitheryCleanContainment });
        Forgemaster.Should().HaveCount(9);
        Artisan.Should().HaveCount(9);
    }

    [Test]
    public void EveryDescriptionIsAsciiAndNamesThePlasmaForge()
    {
        foreach (var (type, detail) in Perks())
        {
            foreach (var level in detail.PerkLevels.Values)
            {
                level.Description.Should().NotBeNullOrWhiteSpace(type.ToString());
                level.Description.All(character => character < 128).Should().BeTrue(type.ToString());
                level.Description.Should().Contain("Plasma Forge", type.ToString());
            }

            detail.Name.All(character => character < 128).Should().BeTrue();
        }
    }

    [Test]
    public void ARankFiveCrafterWithBothLines_GetsOnlyTheFirstOfEachRankedPerk()
    {
        var stats = StatsAt(5, Forgemaster, Artisan);

        stats.Should().BeEquivalentTo(new ForgePerkStats
        {
            RampCpReduction = ForgePerkRules.PlasmaEfficiencyCpReduction,
            HoneIntegrityReduction = ForgePerkRules.SteadyHandsIntegrityReduction
        });
    }

    [Test]
    public void ARankTenCrafterWithBothLines_AddsTheFirstRankOfHeatRetentionAndCleanContainment()
    {
        var stats = StatsAt(10, Forgemaster, Artisan);

        stats.Should().BeEquivalentTo(new ForgePerkStats
        {
            RampCpReduction = ForgePerkRules.PlasmaEfficiencyCpReduction,
            HoneIntegrityReduction = ForgePerkRules.SteadyHandsIntegrityReduction,
            CoolingReduction = ForgePerkRules.HeatRetentionRank1,
            ContainmentChancePercent = ForgePerkRules.CleanContainmentRank1
        });
    }

    [Test]
    public void ARankFiftyCrafterWithBothLines_GetsEveryStatTheEngineReads_AndTheSecondRanksReplaceTheFirst()
    {
        var stats = StatsAt(50, Forgemaster, Artisan);

        stats.Should().BeEquivalentTo(new ForgePerkStats
        {
            RampCpReduction = ForgePerkRules.PlasmaEfficiencyCpReduction,
            CoolingReduction = ForgePerkRules.HeatRetentionRank2,
            OverdrivePressPercent = ForgePerkRules.OverdrivePressPercent,
            PressStreakPercent = ForgePerkRules.HammerRhythmPercent,
            PressOverheatIntegrityReductionPercent = ForgePerkRules.ScorchGuardReductionPercent,
            SurgePlasmaThermal = ForgePerkRules.SurgePlasmaThermal,
            RebondThermalReduction = ForgePerkRules.FastRebondThermalReduction,
            RebondCpReduction = ForgePerkRules.FastRebondCpReduction,
            RedLineCharges = ForgePerkRules.RedLineCharges,
            HoneIntegrityReduction = ForgePerkRules.SteadyHandsIntegrityReduction,
            ContainmentChancePercent = ForgePerkRules.CleanContainmentRank2,
            MicroHonePercent = ForgePerkRules.MicroHonePercent,
            QuenchEdgePercent = ForgePerkRules.SteadyQuenchEdgePercent,
            HoneHotEdgePercent = ForgePerkRules.CoolHandHotEdgePercent,
            TemperHoldActions = ForgePerkRules.TemperHoldActions,
            ContainmentHonePercent = ForgePerkRules.ContainmentFocusPercent,
            PerfectTemperPercent = ForgePerkRules.PerfectTemperPercent
        });
    }

    [TestCase(14, false)]
    [TestCase(15, true)]
    public void OverdrivePressOpensAtRank15(int rank, bool offered)
    {
        (StatsAt(rank, Forgemaster).OverdrivePressPercent > 0).Should().Be(offered);
    }

    [Test]
    public void TheForgemasterLineBuildsUpByRank()
    {
        StatsAt(10, Forgemaster).CoolingReduction.Should().Be(ForgePerkRules.HeatRetentionRank1);
        StatsAt(29, Forgemaster).CoolingReduction.Should().Be(ForgePerkRules.HeatRetentionRank1);
        StatsAt(30, Forgemaster).CoolingReduction.Should().Be(ForgePerkRules.HeatRetentionRank2);
        StatsAt(24, Forgemaster).PressStreakPercent.Should().Be(0);
        StatsAt(25, Forgemaster).PressStreakPercent.Should().Be(ForgePerkRules.HammerRhythmPercent);
        StatsAt(34, Forgemaster).PressOverheatIntegrityReductionPercent.Should().Be(0);
        StatsAt(35, Forgemaster).PressOverheatIntegrityReductionPercent.Should().Be(ForgePerkRules.ScorchGuardReductionPercent);
        StatsAt(39, Forgemaster).SurgePlasmaThermal.Should().Be(0);
        StatsAt(40, Forgemaster).SurgePlasmaThermal.Should().Be(ForgePerkRules.SurgePlasmaThermal);
        StatsAt(44, Forgemaster).RebondCpReduction.Should().Be(0);
        StatsAt(45, Forgemaster).RebondCpReduction.Should().Be(ForgePerkRules.FastRebondCpReduction);
        StatsAt(49, Forgemaster).RedLineCharges.Should().Be(0);
        StatsAt(50, Forgemaster).RedLineCharges.Should().Be(1);
        StatsAt(50, Forgemaster).PerfectTemperPercent.Should().Be(0, "it is the other line's capstone");
    }

    [Test]
    public void TheArtisanLineBuildsUpByRank()
    {
        StatsAt(10, Artisan).ContainmentChancePercent.Should().Be(ForgePerkRules.CleanContainmentRank1);
        StatsAt(24, Artisan).ContainmentChancePercent.Should().Be(ForgePerkRules.CleanContainmentRank1);
        StatsAt(25, Artisan).ContainmentChancePercent.Should().Be(ForgePerkRules.CleanContainmentRank2);
        StatsAt(14, Artisan).MicroHonePercent.Should().Be(0);
        StatsAt(15, Artisan).MicroHonePercent.Should().Be(ForgePerkRules.MicroHonePercent);
        StatsAt(29, Artisan).QuenchEdgePercent.Should().Be(0);
        StatsAt(30, Artisan).QuenchEdgePercent.Should().Be(ForgePerkRules.SteadyQuenchEdgePercent);
        StatsAt(34, Artisan).HoneHotEdgePercent.Should().Be(0);
        StatsAt(35, Artisan).HoneHotEdgePercent.Should().Be(ForgePerkRules.CoolHandHotEdgePercent);
        StatsAt(39, Artisan).TemperHoldActions.Should().Be(0);
        StatsAt(40, Artisan).TemperHoldActions.Should().Be(ForgePerkRules.TemperHoldActions);
        StatsAt(44, Artisan).ContainmentHonePercent.Should().Be(0);
        StatsAt(45, Artisan).ContainmentHonePercent.Should().Be(ForgePerkRules.ContainmentFocusPercent);
        StatsAt(49, Artisan).PerfectTemperPercent.Should().Be(0);
        StatsAt(50, Artisan).PerfectTemperPercent.Should().Be(ForgePerkRules.PerfectTemperPercent);
    }

    [Test]
    public void TheStylePerksGrantNoStat_BecauseTheirStatusEffectCarriesTheStyle()
    {
        var perks = Perks();

        perks[PerkType.HotForgingStyle].PerkLevels[1].StatBonuses.Should().BeEmpty();
        perks[PerkType.ColdWorkingStyle].PerkLevels[1].StatBonuses.Should().BeEmpty();
        StatsAt(50, new[] { PerkType.HotForgingStyle, PerkType.ColdWorkingStyle }).Should().BeEquivalentTo(ForgePerkStats.None);
    }

    [Test]
    public void TheStyleStatusEffectsCarryWhatTheirPerksDescribe()
    {
        var hot = Granted(new HotForgingStatusEffect());
        var cold = Granted(new ColdWorkingStatusEffect());

        // Neither style costs anything, and a zero is not granted at all.
        hot.Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.ForgePressOutputPercent] = 20
        });
        cold.Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.ForgeRangeShift] = -5,
            [StatType.ForgeHoneOutputPercent] = 4
        });
        ForgeStyles.Description(ForgeStyle.HotForging).Should().Be("Hydro-Press gains 20% more Shape.");
        ForgeStyles.Description(ForgeStyle.ColdWorking).Should().Be("The working range moves down 5 Thermal. Vibro-Hone gains 4% more Finish.");
        ForgeStyles.Describe(5, 25, -5).Should().Be("The working range moves up 5 Thermal. Hydro-Press gains 25% more Shape and Vibro-Hone gains 5% less Finish.");
        ForgeStyles.Describe(0, 0, 0).Should().BeEmpty();
        new HotForgingStatusEffect().Name.Should().Be(ForgeStyles.Name(ForgeStyle.HotForging));
        new ColdWorkingStatusEffect().Name.Should().Be(ForgeStyles.Name(ForgeStyle.ColdWorking));
        new HotForgingStatusEffect().Icon.Should().NotBe(new ColdWorkingStatusEffect().Icon);
    }

    [Test]
    public void BothStylesAreCraftingStyleStatusEffects_SoNoDispelPurgeCleanseOrDeathTakesThem()
    {
        foreach (var effect in new StatusEffectBase[] { new HotForgingStatusEffect(), new ColdWorkingStatusEffect() })
        {
            effect.SourceType.Should().Be(StatusEffectSourceType.CraftingStyle, effect.Name);
            StatusEffectSelectors.IsCraftingStyle(effect).Should().BeTrue(effect.Name);
            effect.ActivationType.Should().Be(StatusEffectActivationType.Passive, effect.Name);
            effect.PersistsOnLogout.Should().BeFalse(effect.Name);
            (effect.Categories & StatusEffectCategory.Buff).Should().Be(StatusEffectCategory.Buff, effect.Name);
        }
    }

    [Test]
    public void EachStyleIsKnownToTheStyleSelectionWithItsSkillPerkAndEffect()
    {
        ForgeStyles.All.Should().Equal(ForgeStyle.HotForging, ForgeStyle.ColdWorking);
        ForgeStyles.PerkOf(ForgeStyle.HotForging).Should().Be(PerkType.HotForgingStyle);
        ForgeStyles.PerkOf(ForgeStyle.ColdWorking).Should().Be(PerkType.ColdWorkingStyle);
        ForgeStyles.PerkOf(ForgeStyle.None).Should().Be(PerkType.Invalid);
        ForgeStyles.StatusEffectOf(ForgeStyle.None).Should().BeNull();
        ForgeStyles.Skill.Should().Be(SkillType.Smithery);
        ForgeStyles.StatsOf(ForgeStyle.None).Should().BeEmpty();
    }

    [Test]
    public void AStyleAddsToThePerksOfTheLines_AndTheTwoStylesFavorDifferentMeters()
    {
        var both = Forgemaster.Concat(Artisan).ToList();
        var hot = ForgePerkLines.StatsAt(Perks(), both, 50, ForgeStyle.HotForging);
        var cold = ForgePerkLines.StatsAt(Perks(), both, 50, ForgeStyle.ColdWorking);
        ForgePerkLines.StatsAt(Perks(), both, 19, ForgeStyle.HotForging).RangeShift.Should().Be(0, "a style opens at rank 20");

        hot.RangeShift.Should().Be(0);
        hot.PressOutputPercent.Should().Be(20);
        hot.HoneOutputPercent.Should().Be(0);
        cold.RangeShift.Should().Be(-5);
        cold.PressOutputPercent.Should().Be(0);
        cold.HoneOutputPercent.Should().Be(4);
        hot.OverdrivePressPercent.Should().Be(cold.OverdrivePressPercent, "the lines' perks do not depend on the style");
    }

    [Test]
    public void EveryForgeStat_IsReadByThePerkStats_IsBeneficialWhenPositive_AndAddsUp()
    {
        var forgeStats = ForgeStats().ToList();

        forgeStats.Should().HaveCount(20);
        forgeStats.Select(stat => (int)stat).Should().BeEquivalentTo(Enumerable.Range(1208, 20));
        foreach (var stat in forgeStats)
        {
            ForgePerkStats.FromStats(read => read == stat ? 7 : 0).Should().NotBeEquivalentTo(ForgePerkStats.None, $"{stat} must change the perk stats");
            Stat.GetStatTypeCategory(stat).Should().Be(StatTypeCategory.BeneficialWhenPositive, stat.ToString());
            Stat.GetStatTypeAggregation(stat).Should().Be(StatTypeAggregation.Additive, stat.ToString());
        }
    }

    [Test]
    public void EveryStatTheLinesGrantIsGrantedByAPerkOrAStyle()
    {
        var granted = Perks().Values.SelectMany(detail => detail.PerkLevels.Values).SelectMany(level => level.StatBonuses).Select(bonus => bonus.Stat)
            .Concat(Granted(new HotForgingStatusEffect()).Keys)
            .Concat(Granted(new ColdWorkingStatusEffect()).Keys)
            .Distinct()
            .ToList();

        granted.Should().BeEquivalentTo(ForgeStats());
    }

    [Test]
    public void TheNewIdsAreInsideTheRangesTheCoordinatorReserved()
    {
        ((int)PerkType.SmitheryPlasmaEfficiency).Should().Be(884);
        Forgemaster.Concat(Artisan).Select(perk => (int)perk).Should().OnlyContain(id => id >= 884 && id <= 903);
        ForgeStats().Select(stat => (int)stat).Should().OnlyContain(id => id >= 1208 && id <= 1227);
        ((int)SWLOR.NWN.API.NWScript.Enum.EffectIconType.HotForgingStatusEffect).Should().Be(513);
        ((int)SWLOR.NWN.API.NWScript.Enum.EffectIconType.ColdWorkingStatusEffect).Should().Be(514);
    }

    [Test]
    public void EveryPerkNameIsUniqueInTheSmitheryTree()
    {
        var names = Perks().Values.Select(detail => detail.Name).ToList();

        names.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void TheMagnitudesInThePerkDescriptionsAreTheOnesTheRulesFileHolds()
    {
        var perks = Perks();

        perks[PerkType.SmitheryPlasmaEfficiency].PerkLevels[1].Description.Should().Be("In the Plasma Forge, Ramp Plasma costs 2 less CP, to a minimum of 1.");
        perks[PerkType.SmitheryHeatRetention].PerkLevels[2].Description.Should().Be(
            "In the Plasma Forge, the piece loses 2 less Thermal after every action, to a minimum of 1 lost.");
        perks[PerkType.SmitheryHammerRhythm].PerkLevels[1].Description.Should().Contain("4% more Shape, 8% after two in a row and 12% after three or more");
        perks[PerkType.SmitheryCleanContainment].PerkLevels[2].Description.Should().Contain("20 percentage points more actions (35% becomes 55%)");
        perks[PerkType.SmitherySteadyQuench].PerkLevels[1].Description.Should().Contain("40% of its center bonus at the edge of the working range instead of 30%");
        perks[PerkType.SmitheryCoolHand].PerkLevels[1].Description.Should().Contain("falls to 70% of its best result at the hot edge instead of 60%");
        perks[PerkType.SmitheryContainmentFocus].PerkLevels[1].Description.Should().Contain("+60% instead of +50%");
        perks[PerkType.SmitherySurgePlasma].PerkLevels[1].Description.Should().Contain("1.5 times a Ramp Plasma's CP");
    }
}
