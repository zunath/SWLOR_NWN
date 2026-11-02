using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using static SWLOR.Game.Server.Tests.Perks.AgriculturePerkLines;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Covers the structure of Agriculture's Chef and Angler lines (ten purchases at the schedule's ranks and prices, a style at rank 20 that
/// excludes its alternative, a capstone at rank 50) and the numbers each line gives the engines that read it.
/// </summary>
public class AgriculturePerkLineTests
{
    private static readonly int[] Ranks = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
    private static readonly int[] Prices = { 1, 1, 2, 2, 3, 3, 3, 3, 3, 4 };

    private static readonly PerkType[][] Lines = { ChefPerks, AnglerPerks };

    private static IReadOnlyDictionary<PerkType, PerkDetail> LineOf(PerkType[] perks) => perks == ChefPerks ? Chef() : Angler();

    /// <summary>Each purchase of a line as (required Agriculture rank, SP price), the two styles counted as one purchase.</summary>
    private static List<(int Rank, int Price)> Purchases(PerkType[] perks, PerkType skippedStyle)
    {
        var line = LineOf(perks);
        var purchases = new List<(int, int)>();
        foreach (var type in perks.Where(type => type != skippedStyle))
        {
            foreach (var level in line[type].PerkLevels.OrderBy(pair => pair.Key).Select(pair => pair.Value))
                purchases.Add((level.Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank, level.Price));
        }

        return purchases.OrderBy(purchase => purchase.Item1).ToList();
    }

    [Test]
    public void TheChefLineIsTenPurchasesAtTheSchedulesRanksAndPrices_TheStyleCountedOnce()
    {
        var purchases = Purchases(ChefPerks, PerkType.FlashCooking);

        purchases.Select(purchase => purchase.Rank).Should().Equal(Ranks);
        purchases.Select(purchase => purchase.Price).Should().Equal(Prices);
        purchases.Sum(purchase => purchase.Price).Should().Be(25);
    }

    [Test]
    public void TheAnglerLineIsTenPurchasesAtTheSchedulesRanksAndPrices_TheStyleCountedOnce()
    {
        var purchases = Purchases(AnglerPerks, PerkType.AggressiveAngler);

        purchases.Select(purchase => purchase.Rank).Should().Equal(Ranks);
        purchases.Select(purchase => purchase.Price).Should().Equal(Prices);
        purchases.Sum(purchase => purchase.Price).Should().Be(25);
    }

    [Test]
    public void EachLinesAlternativeStylesCostTheSameAndOpenAtRankTwenty()
    {
        foreach (var (line, first, second) in new[]
                 {
                     (Chef(), PerkType.SlowCooking, PerkType.FlashCooking),
                     (Angler(), PerkType.PatientAngler, PerkType.AggressiveAngler)
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
    public void EveryPerkOfBothLinesIsAnActiveAgriculturePerkWithAnAgricultureRankAndNoOtherRequirementButItsStylesExclusion()
    {
        foreach (var perks in Lines)
        {
            var line = LineOf(perks);
            line.Keys.Should().BeEquivalentTo(perks, "the definition builds exactly the line's perks");

            foreach (var (type, detail) in line)
            {
                detail.Category.Should().Be(PerkCategoryType.Agriculture, type.ToString());
                detail.IsActive.Should().BeTrue(type.ToString());
                detail.Name.Should().NotBeNullOrWhiteSpace();
                detail.IconResref.Should().NotBeNullOrWhiteSpace(type.ToString());
                detail.IconResref.Should().NotBe("default_perk", type.ToString());

                foreach (var level in detail.PerkLevels.Values)
                {
                    level.Requirements.OfType<PerkRequirementSkill>().Should().ContainSingle(requirement => requirement.Type == SkillType.Agriculture);
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

        var chef = Chef();
        var angler = Angler();

        Excluded(chef[PerkType.SlowCooking]).Should().Equal(PerkType.FlashCooking);
        Excluded(chef[PerkType.FlashCooking]).Should().Equal(PerkType.SlowCooking);
        Excluded(angler[PerkType.PatientAngler]).Should().Equal(PerkType.AggressiveAngler);
        Excluded(angler[PerkType.AggressiveAngler]).Should().Equal(PerkType.PatientAngler);

        foreach (var type in ChefPerks.Except(new[] { PerkType.SlowCooking, PerkType.FlashCooking }))
            Excluded(chef[type]).Should().BeEmpty(type.ToString());
        foreach (var type in AnglerPerks.Except(new[] { PerkType.PatientAngler, PerkType.AggressiveAngler }))
            Excluded(angler[type]).Should().BeEmpty(type.ToString());
    }

    [Test]
    public void TheCapstonesAreTheRankFiftyPurchasesAtFourSP()
    {
        Chef()[PerkType.SignatureDish].PerkLevels.Should().ContainSingle().Which.Value.Price.Should().Be(4);
        Angler()[PerkType.TrophyCatch].PerkLevels.Should().ContainSingle().Which.Value.Price.Should().Be(4);
        OwnedLevel(Chef()[PerkType.SignatureDish], 49).Should().Be(0);
        OwnedLevel(Chef()[PerkType.SignatureDish], 50).Should().Be(1);
        OwnedLevel(Angler()[PerkType.TrophyCatch], 49).Should().Be(0);
        OwnedLevel(Angler()[PerkType.TrophyCatch], 50).Should().Be(1);
    }

    [Test]
    public void TheNewIdsAreInsideTheRangesReservedForAgriculture()
    {
        foreach (var type in ChefPerks.Concat(AnglerPerks))
            ((int)type).Should().BeInRange(904, 923, type.ToString());

        var stats = new[]
        {
            StatType.KitchenPantryPush, StatType.KitchenPushSpreadReduction, StatType.KitchenSeasonedPalate,
            StatType.KitchenSeasonedPalateSpreadPercent, StatType.KitchenLowHeatDonenessPercent, StatType.KitchenMediumDriftReductionPercent,
            StatType.KitchenHighHeatDriftReductionPercent, StatType.KitchenReducePercent,
            StatType.KitchenReduceCpReduction, StatType.KitchenPeakAromaChancePoints, StatType.KitchenStirCpReduction,
            StatType.KitchenStirDonenessPercent, StatType.KitchenSignatureDish, StatType.AnglerTensionCap, StatType.AnglerSetHookDrain,
            StatType.AnglerForecastTurns, StatType.AnglerGiveLineDistanceReduction, StatType.AnglerReelDistance, StatType.AnglerRareFishRungs
        };

        foreach (var stat in stats)
            ((int)stat).Should().BeInRange(1228, 1247, stat.ToString());

        stats.Select(stat => (int)stat).Should().OnlyHaveUniqueItems();
        stats.Should().HaveCount(19);
    }

    [Test]
    public void EveryStatTheLinesGrantIsBeneficialWhenPositive_AndTheUnlockFlagsAreMaximums()
    {
        var maximums = new[]
        {
            StatType.KitchenSeasonedPalate, StatType.KitchenSeasonedPalateSpreadPercent, StatType.KitchenReducePercent,
            StatType.KitchenSignatureDish, StatType.AnglerForecastTurns
        };

        foreach (var (type, detail) in Chef().Concat(Angler()))
        {
            foreach (var bonus in detail.PerkLevels.Values.SelectMany(level => level.StatBonuses))
            {
                Stat.GetStatTypeCategory(bonus.Stat).Should().Be(StatTypeCategory.BeneficialWhenPositive, $"{type}: {bonus.Stat}");
                Stat.GetStatTypeAggregation(bonus.Stat).Should().Be(
                    maximums.Contains(bonus.Stat) ? StatTypeAggregation.Maximum : StatTypeAggregation.Additive, $"{type}: {bonus.Stat}");
                bonus.Calculate(0).Should().BeGreaterThan(0, $"{type}: {bonus.Stat} is a benefit written as a positive number");
            }
        }
    }

    [Test]
    public void ARankedPerksLaterRankRestatesWhatTheEarlierRankGave_AndNeverGivesLess()
    {
        foreach (var line in new[] { Chef(), Angler() })
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
                        after.Should().BeGreaterThanOrEqualTo(before, $"{type} rank {index + 1} must keep {stat}");
                    }

                    levels[index].StatBonuses.Select(bonus => bonus.Calculate(0)).Sum().Should().BeGreaterThan(
                        levels[index - 1].StatBonuses.Select(bonus => bonus.Calculate(0)).Sum(), $"{type} rank {index + 1} must add something");
                }
            }
        }
    }

    [Test]
    public void PlayerFacingTextIsAsciiPunctuation()
    {
        foreach (var (type, detail) in Chef().Concat(Angler()))
        {
            detail.Name.Should().MatchRegex("^[\\x20-\\x7E]+$");
            foreach (var level in detail.PerkLevels.Values)
                level.Description.Should().MatchRegex("^[\\x20-\\x7E]+$", $"{type}'s description is ASCII");
        }
    }

    [Test]
    public void TheChefsStatTotalsAtEachRankMatchTheDesign()
    {
        var line = Chef();

        KitchenPerkStats.FromStatTotals(Totals(line, 4)).Should().Be(KitchenPerkStats.None);
        KitchenPerkStats.FromStatTotals(Totals(line, 5)).Should().Be(new KitchenPerkStats(PantryPushBonus: 1));
        KitchenPerkStats.FromStatTotals(Totals(line, 19)).Should().Be(new KitchenPerkStats(
            PantryPushBonus: 1, PushSpreadReduction: 5, SeasonedPalateOwned: true));

        KitchenPerkStats.FromStatTotals(Totals(line, 25, PerkType.FlashCooking), seasonedPalateOn: true).Should().Be(new KitchenPerkStats(
            PantryPushBonus: 1, PushSpreadReduction: 5, SeasonedPalateOwned: true, SeasonedPalateOn: true,
            LowHeatDonenessPercent: 80, MediumDriftReductionPercent: 25, ReducePercent: 20));

        KitchenPerkStats.FromStatTotals(Totals(line, 50, PerkType.SlowCooking), seasonedPalateOn: true).Should().Be(new KitchenPerkStats(
            PantryPushBonus: 1, PushSpreadReduction: 5, SeasonedPalateOwned: true, SeasonedPalateOn: true, SeasonedPalateSpreadPercent: 25,
            HighDriftReductionPercent: 15, ReducePercent: 35, ReduceCpReduction: 3,
            PeakAromaChancePoints: 10, StirCpReduction: 4, StirDonenessPercent: 50, SignatureDish: true));
    }

    [Test]
    public void TheAnglersStatTotalsAtEachRankMatchTheDesign()
    {
        var line = Angler();

        LineAnglerEffects.FromStatTotals(Totals(line, 4)).Should().Be(LineAnglerEffects.None);
        LineAnglerEffects.FromStatTotals(Totals(line, 5)).Should().Be(new LineAnglerEffects(TensionCapBonus: 2));
        LineAnglerEffects.FromStatTotals(Totals(line, 15)).Should().Be(
            new LineAnglerEffects(TensionCapBonus: 2, SetHookDrainBonus: 1, ForecastDepth: 2, ForecastTurns: 2));

        LineAnglerEffects.FromStatTotals(Totals(line, 25, PerkType.AggressiveAngler)).Should().Be(new LineAnglerEffects(
            TensionCapBonus: 5, SetHookDrainBonus: 1, ForecastDepth: 2, ForecastTurns: 2, GiveLineDistanceReduction: 1));

        LineAnglerEffects.FromStatTotals(Totals(line, 50, PerkType.AggressiveAngler)).Should().Be(new LineAnglerEffects(
            TensionCapBonus: 8, SetHookDrainBonus: 3, ForecastDepth: 2, ForecastTurns: 4, GiveLineDistanceReduction: 1, RareFishRungReduction: 4));
        LineAnglerEffects.FromStatTotals(Totals(line, 50, PerkType.PatientAngler)).Should().Be(new LineAnglerEffects(
            TensionCapBonus: 8, SetHookDrainBonus: 3, ForecastDepth: 2, ForecastTurns: 4, ReelDistanceBonus: 1, RareFishRungReduction: 4));
    }

    [Test]
    public void NoPerkOfEitherLineTouchesQuickReel()
    {
        // Quick Reel is today's roll at one fixed Willpower score; the Angler's stats have no reader there.
        var stats = AnglerPerks.SelectMany(type => Angler()[type].PerkLevels.Values.SelectMany(level => level.StatBonuses.Select(bonus => bonus.Stat)));

        stats.Should().OnlyContain(stat => stat.ToString().StartsWith("Angler"));
        FishingQuickReelNeverReadsAStat().Should().BeTrue();
    }

    private static bool FishingQuickReelNeverReadsAStat()
    {
        var source = File.ReadAllText(Path.Combine(FindRoot(), "SWLOR.Game.Server", "Service", "FishingService", "FishingQuickReel.cs"));
        return !source.Contains("StatType") && !source.Contains("Stat.");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!.FullName;
    }
}
