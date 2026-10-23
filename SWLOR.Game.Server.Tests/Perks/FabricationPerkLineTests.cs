using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using static SWLOR.Game.Server.Tests.Perks.FabricationPerkLines;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Covers the structure of Fabrication's Builder and Researcher lines (ten purchases at the schedule's ranks and prices, a style at
/// rank 20 that excludes its alternative, a capstone at rank 50) and the numbers each line gives the engines that read it.
/// </summary>
public class FabricationPerkLineTests
{
    private static readonly int[] Ranks = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
    private static readonly int[] Prices = { 1, 1, 2, 2, 3, 3, 3, 3, 3, 4 };

    private static readonly PerkType[][] Lines = { BuilderPerks, ResearcherPerks };

    private static IReadOnlyDictionary<PerkType, PerkDetail> LineOf(PerkType[] perks) =>
        perks == BuilderPerks ? Builder() : Researcher();

    /// <summary>Each purchase of a line as (required Fabrication rank, SP price), the two styles counted as one purchase.</summary>
    private static List<(int Rank, int Price)> Purchases(PerkType[] perks, PerkType skippedStyle)
    {
        var line = LineOf(perks);
        var purchases = new List<(int, int)>();
        foreach (var type in perks.Where(type => type != skippedStyle))
        {
            foreach (var level in line[type].PerkLevels.OrderBy(pair => pair.Key).Select(pair => pair.Value))
            {
                purchases.Add((level.Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank, level.Price));
            }
        }

        return purchases.OrderBy(purchase => purchase.Item1).ToList();
    }

    [Test]
    public void TheBuilderLineIsTenPurchasesAtTheSchedulesRanksAndPrices_TheStyleCountedOnce()
    {
        var purchases = Purchases(BuilderPerks, PerkType.FineTolerance);

        purchases.Select(purchase => purchase.Rank).Should().Equal(Ranks);
        purchases.Select(purchase => purchase.Price).Should().Equal(Prices);
        purchases.Sum(purchase => purchase.Price).Should().Be(25);
    }

    [Test]
    public void TheResearcherLineIsTenPurchasesAtTheSchedulesRanksAndPrices_TheStyleCountedOnce()
    {
        var purchases = Purchases(ResearcherPerks, PerkType.Intuitive);

        purchases.Select(purchase => purchase.Rank).Should().Equal(Ranks);
        purchases.Select(purchase => purchase.Price).Should().Equal(Prices);
        purchases.Sum(purchase => purchase.Price).Should().Be(25);
    }

    [Test]
    public void EachLinesAlternativeStylesCostTheSameAndOpenAtRankTwenty()
    {
        foreach (var (line, first, second) in new[]
                 {
                     (Builder(), PerkType.ProductionRun, PerkType.FineTolerance),
                     (Researcher(), PerkType.Methodical, PerkType.Intuitive)
                 })
        {
            foreach (var style in new[] { first, second })
            {
                line[style].PerkLevels.Should().HaveCount(1);
                line[style].PerkLevels[1].Price.Should().Be(2);
                line[style].PerkLevels[1].Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank.Should().Be(20);
            }
        }
    }

    [Test]
    public void EveryPerkOfBothLinesIsAnActiveFabricationPerkWithAFabricationRankAndNoOtherRequirementButItsStylesExclusion()
    {
        foreach (var perks in Lines)
        {
            var line = LineOf(perks);
            line.Keys.Should().BeEquivalentTo(perks, "the definition builds exactly the line's perks");

            foreach (var (type, detail) in line)
            {
                detail.Category.Should().Be(PerkCategoryType.Fabrication, type.ToString());
                detail.IsActive.Should().BeTrue(type.ToString());
                detail.Name.Should().NotBeNullOrWhiteSpace();
                detail.IconResref.Should().NotBeNullOrWhiteSpace(type.ToString());
                detail.IconResref.Should().NotBe("default_perk", type.ToString());

                foreach (var level in detail.PerkLevels.Values)
                {
                    level.Requirements.OfType<PerkRequirementSkill>().Should().ContainSingle(requirement => requirement.Type == SkillType.Fabrication);
                    level.Requirements.Where(requirement => requirement is not PerkRequirementSkill and not PerkRequirementCannotHavePerk)
                        .Should().BeEmpty("the line has no quest or other prerequisite, and the capstone no quest");
                    level.StatBonuses.Should().NotBeEmpty($"{type} grants stat adjustments the engine reads");
                    level.GrantedFeats.Should().BeEmpty();
                }
            }
        }
    }

    [Test]
    public void TheTwoStylesOfALineExcludeEachOther_AndNoOtherPerkExcludesAnything()
    {
        static PerkType[] Excluded(PerkDetail detail) =>
            detail.PerkLevels.Values.SelectMany(level => level.Requirements.OfType<PerkRequirementCannotHavePerk>())
                .Select(requirement => (PerkType)typeof(PerkRequirementCannotHavePerk)
                    .GetField("_cannotHavePerkType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .GetValue(requirement)!)
                .Distinct()
                .ToArray();

        var builder = Builder();
        var researcher = Researcher();

        Excluded(builder[PerkType.ProductionRun]).Should().Equal(PerkType.FineTolerance);
        Excluded(builder[PerkType.FineTolerance]).Should().Equal(PerkType.ProductionRun);
        Excluded(researcher[PerkType.Methodical]).Should().Equal(PerkType.Intuitive);
        Excluded(researcher[PerkType.Intuitive]).Should().Equal(PerkType.Methodical);

        foreach (var type in BuilderPerks.Except(new[] { PerkType.ProductionRun, PerkType.FineTolerance }))
            Excluded(builder[type]).Should().BeEmpty(type.ToString());
        foreach (var type in ResearcherPerks.Except(new[] { PerkType.Methodical, PerkType.Intuitive }))
            Excluded(researcher[type]).Should().BeEmpty(type.ToString());
    }

    [Test]
    public void TheCapstonesAreTheRankFiftyPurchasesAtFourSP()
    {
        Builder()[PerkType.MasterAssembler].PerkLevels.Should().ContainSingle()
            .Which.Value.Price.Should().Be(4);
        Researcher()[PerkType.Breakthrough].PerkLevels.Should().ContainSingle()
            .Which.Value.Price.Should().Be(4);
        OwnedLevel(Builder()[PerkType.MasterAssembler], 49).Should().Be(0);
        OwnedLevel(Builder()[PerkType.MasterAssembler], 50).Should().Be(1);
        OwnedLevel(Researcher()[PerkType.Breakthrough], 49).Should().Be(0);
        OwnedLevel(Researcher()[PerkType.Breakthrough], 50).Should().Be(1);
    }

    [Test]
    public void TheNewIdsAreInsideTheRangesReservedForFabrication()
    {
        foreach (var type in BuilderPerks.Concat(ResearcherPerks))
            ((int)type).Should().BeInRange(864, 883, type.ToString());

        var stats = new[]
        {
            StatType.FabricatorRunPowerPercent, StatType.FabricatorPrecisionPowerPercent, StatType.FabricatorWearReduction,
            StatType.FabricatorRetoolCpReductionPercent, StatType.FabricatorServiceRestore, StatType.FabricatorServiceCpReduction,
            StatType.FabricatorDualFeedUnlocked, StatType.FabricatorPrimeWindowActions, StatType.FabricatorPrimeBonusPercent,
            StatType.FabricatorAssembleCapPoints, StatType.ExperimentTrials, StatType.ExperimentTestChancePercent,
            StatType.ExperimentRefineCpReduction, StatType.ExperimentRefineChancePercent,
            StatType.ExperimentTimeCutPerEvidencePercent, StatType.ExperimentPeerReviewUnlocked, StatType.ExperimentBreakthroughUnlocked
        };

        foreach (var stat in stats)
            ((int)stat).Should().BeInRange(1188, 1207, stat.ToString());

        stats.Select(stat => (int)stat).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void EveryStatTheLinesGrantIsBeneficialWhenPositive_AndTheUnlockFlagsAreMaximums()
    {
        var flags = new[]
        {
            StatType.FabricatorDualFeedUnlocked, StatType.ExperimentPeerReviewUnlocked, StatType.ExperimentBreakthroughUnlocked
        };

        foreach (var (type, detail) in Builder().Concat(Researcher()))
        {
            foreach (var bonus in detail.PerkLevels.Values.SelectMany(level => level.StatBonuses))
            {
                Stat.GetStatTypeCategory(bonus.Stat).Should().Be(StatTypeCategory.BeneficialWhenPositive, $"{type}: {bonus.Stat}");
                Stat.GetStatTypeAggregation(bonus.Stat).Should().Be(
                    flags.Contains(bonus.Stat) ? StatTypeAggregation.Maximum : StatTypeAggregation.Additive, $"{type}: {bonus.Stat}");
            }
        }
    }

    [Test]
    public void ARankedPerksLaterRankRestatesWhatTheEarlierRankGave()
    {
        foreach (var line in new[] { Builder(), Researcher() })
        {
            foreach (var (type, detail) in line.Where(pair => pair.Value.PerkLevels.Count > 1))
            {
                var levels = detail.PerkLevels.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList();
                for (var index = 1; index < levels.Count; index++)
                {
                    foreach (var stat in levels[index - 1].StatBonuses.Select(bonus => bonus.Stat))
                    {
                        var before = levels[index - 1].StatBonuses.Where(bonus => bonus.Stat == stat).Sum(bonus => bonus.Calculate(0));
                        var after = levels[index].StatBonuses.Where(bonus => bonus.Stat == stat).Sum(bonus => bonus.Calculate(0));
                        after.Should().BeGreaterThan(before, $"{type} rank {index + 1} must keep and extend {stat}");
                    }
                }
            }
        }
    }

    [Test]
    public void PlayerFacingTextIsAsciiPunctuation()
    {
        foreach (var (type, detail) in Builder().Concat(Researcher()))
        {
            detail.Name.Should().MatchRegex("^[\\x20-\\x7E]+$");
            foreach (var level in detail.PerkLevels.Values)
                level.Description.Should().MatchRegex("^[\\x20-\\x7E]+$", $"{type}'s description is ASCII");
        }
    }

    [Test]
    public void TheBuildersStatTotalsAtEachRankMatchTheDesign()
    {
        var line = Builder();

        FabricatorPerkStats.FromStatTotals(Totals(line, 4)).Should().Be(FabricatorPerkStats.None);
        FabricatorPerkStats.FromStatTotals(Totals(line, 5, PerkType.FineTolerance)).Should().Be(
            new FabricatorPerkStats(RetoolCpReductionPercent: 25));
        FabricatorPerkStats.FromStatTotals(Totals(line, 19)).Should().Be(
            new FabricatorPerkStats(WearReduction: 1, RetoolCpReductionPercent: 25, ServiceRestoreBonus: 10, ServiceCpReduction: 2));

        FabricatorPerkStats.FromStatTotals(Totals(line, 25, PerkType.FineTolerance)).Should().Be(new FabricatorPerkStats(
            RunPowerPercent: 60, PrecisionPowerPercent: -3, WearReduction: 1, RetoolCpReductionPercent: 25,
            ServiceRestoreBonus: 10, ServiceCpReduction: 2, DualFeedUnlocked: true));

        FabricatorPerkStats.FromStatTotals(Totals(line, 50, PerkType.ProductionRun)).Should().Be(new FabricatorPerkStats(
            RunPowerPercent: -25, PrecisionPowerPercent: 25, WearReduction: 1, RetoolCpReductionPercent: 50,
            ServiceRestoreBonus: 20, ServiceCpReduction: 4, DualFeedUnlocked: true, PrimeWindowBonusActions: 1,
            PrimeBonusPercentBonus: 25, AssembleCapBonusPoints: 15));
    }

    [Test]
    public void TheResearchersStatTotalsAtEachRankMatchTheDesign()
    {
        var line = Researcher();

        ExperimentTuning.FromStatTotals(Totals(line, 4)).Should().Be(ExperimentTuning.Default);
        ExperimentTuning.FromStatTotals(Totals(line, 10, PerkType.Intuitive, PerkType.Methodical)).Should().Be(
            ExperimentTuning.Default with { TestChanceBonusPercent = 6, RefineCp = 25 });

        // Methodical at rank 50: 6 + 1 (Broader Hypotheses) + 2 trials, 12 - 15 points of Test chance, every trait.
        ExperimentTuning.FromStatTotals(Totals(line, 50, PerkType.Intuitive)).Should().Be(new ExperimentTuning(
            Trials: 9, TestChanceBonusPercent: 12 - 15, RefineChanceBonus: 20, RefineCp: 25, TimeCutPerEvidence: 7,
            TimeCutCap: 25, PeerReview: true, Breakthrough: true));

        // Intuitive at rank 50: 6 + 1 - 1 trials, 12 + 20 points of Test chance.
        ExperimentTuning.FromStatTotals(Totals(line, 50, PerkType.Methodical)).Should().Be(new ExperimentTuning(
            Trials: 6, TestChanceBonusPercent: 12 + 20, RefineChanceBonus: 20, RefineCp: 25, TimeCutPerEvidence: 7,
            TimeCutCap: 25, PeerReview: true, Breakthrough: true));
    }

    [Test]
    public void NoPerkOfEitherLineRaisesTheTimeCutsCap()
    {
        foreach (var rank in new[] { 20, 50 })
        {
            ExperimentTuning.FromStatTotals(Totals(Researcher(), rank, PerkType.Intuitive)).TimeCutCap
                .Should().Be(ExperimentRules.TimeCutCapPercent);
        }
    }
}
