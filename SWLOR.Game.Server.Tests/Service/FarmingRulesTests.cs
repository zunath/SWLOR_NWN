using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Player City Revamp, "Farming": the tier gate, crop cap, tending window, yields, wither time, Seed Saver and XP.</summary>
public class FarmingRulesTests
{
    private static readonly DateTime Planted = new(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc);

    private static GardenPlot PlotOf(int growHours, bool tended = false, bool fertilized = false, DateTime? planted = null)
    {
        var start = planted ?? Planted;
        var ripe = FarmingRules.GetRipeUtc(start, growHours);
        return new GardenPlot
        {
            PlotId = "plot",
            PlanterPlayerId = "planter",
            PlanterCdKey = "KEY",
            Crop = CropType.Tomato,
            PlantedUtc = start,
            TendReadyUtc = FarmingRules.GetTendReadyUtc(start, growHours),
            RipeUtc = ripe,
            WitherUtc = FarmingRules.GetWitherUtc(ripe),
            Tended = tended,
            Fertilized = fertilized
        };
    }

    // Tiers and the Crop Management gate

    [TestCase(1, 5, 12)]
    [TestCase(2, 15, 16)]
    [TestCase(3, 25, 20)]
    [TestCase(4, 35, 24)]
    [TestCase(5, 45, 24)]
    public void EachTierHasItsCropLevelAndGrowTime(int tier, int level, int hours)
    {
        FarmingRules.GetTierLevel(tier).Should().Be(level);
        FarmingRules.GetTierGrowHours(tier).Should().Be(hours);
    }

    [TestCase(0)]
    [TestCase(6)]
    public void ATierOutsideOneToFiveIsRefused(int tier)
    {
        var level = () => FarmingRules.GetTierLevel(tier);
        var hours = () => FarmingRules.GetTierGrowHours(tier);

        level.Should().Throw<ArgumentOutOfRangeException>();
        hours.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(5, 5)]
    [TestCase(9, 5)]
    [TestCase(-1, 0)]
    public void CropManagementOpensTheTiersUpToItsRank(int rank, int highest)
    {
        FarmingRules.GetHighestTier(rank).Should().Be(highest);
    }

    [Test]
    public void ACropNeedsItsTierToBeOpen()
    {
        FarmingRules.CanPlantTier(0, 1).Should().BeFalse("no Crop Management plants nothing");
        FarmingRules.CanPlantTier(2, 1).Should().BeTrue();
        FarmingRules.CanPlantTier(2, 2).Should().BeTrue();
        FarmingRules.CanPlantTier(2, 3).Should().BeFalse();
        FarmingRules.CanPlantTier(5, 5).Should().BeTrue();
        FarmingRules.CanPlantTier(5, 0).Should().BeFalse("tier 0 is not a tier");
    }

    // The crop cap

    [Test]
    public void ACdKeyGrowsAtMostTenCrops()
    {
        FarmingRules.MaxCropsPerCdKey.Should().Be(10);
        FarmingRules.HasRoomForCrop(0).Should().BeTrue();
        FarmingRules.HasRoomForCrop(9).Should().BeTrue();
        FarmingRules.HasRoomForCrop(10).Should().BeFalse();
        FarmingRules.HasRoomForCrop(11).Should().BeFalse();
    }

    [Test]
    public void TheCountIncludesRipeAndGrowingCropsAndLeavesOutWitheredOnes()
    {
        var growing = PlotOf(12);
        var ripe = PlotOf(12, planted: Planted.AddHours(-20));
        var withered = PlotOf(12, planted: Planted.AddDays(-10));
        var now = Planted.AddHours(1);

        FarmingRules.GetStage(growing, now).Should().Be(GardenPlotStage.Growing);
        FarmingRules.GetStage(ripe, now).Should().Be(GardenPlotStage.Ripe);
        FarmingRules.GetStage(withered, now).Should().Be(GardenPlotStage.Withered);
        FarmingRules.CountGrowingCrops(new[] { growing, ripe, withered }, now).Should().Be(2);
        FarmingRules.CountGrowingCrops(Array.Empty<GardenPlot>(), now).Should().Be(0);
    }

    // Timing

    [TestCase(12, 3)]
    [TestCase(16, 4)]
    [TestCase(20, 5)]
    [TestCase(24, 6)]
    public void ACropCanBeTendedAfterAQuarterOfItsGrowTime(int growHours, int tendHours)
    {
        FarmingRules.GetTendReadyUtc(Planted, growHours).Should().Be(Planted.AddHours(tendHours));
    }

    [Test]
    public void ACropRipensAfterItsGrowTimeAndWithersSeventyTwoHoursLater()
    {
        var ripe = FarmingRules.GetRipeUtc(Planted, 20);

        ripe.Should().Be(Planted.AddHours(20));
        FarmingRules.GetWitherUtc(ripe).Should().Be(Planted.AddHours(92));
        FarmingRules.WitherHours.Should().Be(72);
    }

    [Test]
    public void ACropMovesThroughItsStagesAtTheBoundaries()
    {
        var plot = PlotOf(12);

        FarmingRules.GetStage(null, Planted).Should().Be(GardenPlotStage.Empty);
        FarmingRules.GetStage(new GardenPlot(), Planted).Should().Be(GardenPlotStage.Empty, "a record with no crop is an empty plot");
        FarmingRules.GetStage(plot, Planted).Should().Be(GardenPlotStage.Growing);
        FarmingRules.GetStage(plot, Planted.AddHours(3).AddTicks(-1)).Should().Be(GardenPlotStage.Growing);
        FarmingRules.GetStage(plot, Planted.AddHours(3)).Should().Be(GardenPlotStage.ReadyToTend);
        FarmingRules.GetStage(plot, Planted.AddHours(12).AddTicks(-1)).Should().Be(GardenPlotStage.ReadyToTend);
        FarmingRules.GetStage(plot, Planted.AddHours(12)).Should().Be(GardenPlotStage.Ripe);
        FarmingRules.GetStage(plot, Planted.AddHours(84).AddTicks(-1)).Should().Be(GardenPlotStage.Ripe);
        FarmingRules.GetStage(plot, Planted.AddHours(84)).Should().Be(GardenPlotStage.Withered);
    }

    [Test]
    public void ATendedCropStaysGrowingUntilItRipens()
    {
        var plot = PlotOf(12, tended: true);

        FarmingRules.GetStage(plot, Planted.AddHours(6)).Should().Be(GardenPlotStage.Growing);
        FarmingRules.GetStage(plot, Planted.AddHours(12)).Should().Be(GardenPlotStage.Ripe);
    }

    [Test]
    public void TheTimeLeftRunsToRipeWhileGrowingAndToWitherOnceRipe()
    {
        var plot = PlotOf(12);

        FarmingRules.GetTimeLeft(plot, Planted.AddHours(2)).Should().Be(TimeSpan.FromHours(10));
        FarmingRules.GetTimeLeft(plot, Planted.AddHours(5)).Should().Be(TimeSpan.FromHours(7));
        FarmingRules.GetTimeLeft(plot, Planted.AddHours(20)).Should().Be(TimeSpan.FromHours(64));
        FarmingRules.GetTimeLeft(plot, Planted.AddHours(100)).Should().Be(TimeSpan.Zero);
        FarmingRules.GetTimeLeft(null, Planted).Should().Be(TimeSpan.Zero);
    }

    // Yield

    [TestCase(false, false, 0, false, false, 3)]
    [TestCase(true, false, 0, false, false, 4)]
    [TestCase(false, true, 0, false, false, 4)]
    [TestCase(false, false, 1, false, false, 4)]
    [TestCase(false, false, 3, false, false, 6)]
    [TestCase(false, false, 0, true, false, 4)]
    [TestCase(false, false, 0, false, true, 4)]
    [TestCase(true, true, 3, true, true, 10)]
    public void AHarvestYieldsThreePlusTheBonuses(bool tended, bool fertilized, int bountiful, bool greenhouse, bool homeGround, int items)
    {
        FarmingRules.GetYield(tended, fertilized, bountiful, greenhouse, homeGround).Should().Be(items);
    }

    [Test]
    public void BountifulHarvestAddsAtMostThree_AndNegativeRanksAddNothing()
    {
        FarmingRules.GetYield(false, false, 4, false, false).Should().Be(6);
        FarmingRules.GetYield(false, false, -2, false, false).Should().Be(3);
    }

    // Seed Saver

    [TestCase(0, 0)]
    [TestCase(1, 15)]
    [TestCase(2, 30)]
    [TestCase(3, 30)]
    [TestCase(-1, 0)]
    public void SeedSaverReturnsTheSeedFifteenOrThirtyPercentOfTheTime(int rank, int percent)
    {
        FarmingRules.GetSeedSaverPercent(rank).Should().Be(percent);
    }

    [Test]
    public void ARollOfOneToOneHundredReturnsTheSeedWhenItIsAtOrUnderThePercent()
    {
        FarmingRules.ReturnsSeed(15, 1).Should().BeTrue();
        FarmingRules.ReturnsSeed(15, 15).Should().BeTrue();
        FarmingRules.ReturnsSeed(15, 16).Should().BeFalse();
        FarmingRules.ReturnsSeed(30, 30).Should().BeTrue();
        FarmingRules.ReturnsSeed(30, 31).Should().BeFalse();
        FarmingRules.ReturnsSeed(0, 1).Should().BeFalse("no Seed Saver never returns the seed");
        FarmingRules.ReturnsSeed(30, 0).Should().BeFalse("a roll is 1 to 100");

        Enumerable.Range(1, 100).Count(roll => FarmingRules.ReturnsSeed(15, roll)).Should().Be(15);
        Enumerable.Range(1, 100).Count(roll => FarmingRules.ReturnsSeed(30, roll)).Should().Be(30);
    }

    // XP

    [Test]
    public void HarvestXpPerItemIsTheLevelDifferenceChartFishingAndHarvestingUse()
    {
        foreach (var (level, rank) in new[] { (5, 5), (5, 1), (15, 10), (25, 20), (35, 40), (45, 50), (45, 30) })
            FarmingRules.GetXpPerItem(level, rank).Should().Be(Skill.GetDeltaXP(level - rank), $"crop level {level} at rank {rank}");

        FarmingRules.GetXpPerItem(25, 25).Should().BeGreaterThan(FarmingRules.GetXpPerItem(25, 28), "a crop below the farmer's rank pays less");
        FarmingRules.GetXpPerItem(25, 29).Should().Be(76).And.BeGreaterThan(0, "the last rank with any XP");
        FarmingRules.GetXpPerItem(25, 30).Should().Be(0, "five ranks past the crop pays nothing, as in harvesting");
        FarmingRules.GetXpPerItem(5, 50).Should().Be(0, "a crop the farmer has long outgrown pays nothing");
    }

    [Test]
    public void AHarvestPaysItsXpForEveryItem_AndATendPaysHalfOfOne()
    {
        var one = FarmingRules.GetXpPerItem(25, 20);

        one.Should().BeGreaterThan(0);
        FarmingRules.GetHarvestXp(25, 20, 7).Should().Be(one * 7);
        FarmingRules.GetHarvestXp(25, 20, 0).Should().Be(0);
        FarmingRules.GetHarvestXp(25, 20, -3).Should().Be(0);
        FarmingRules.GetTendXp(25, 20).Should().Be(one * FarmingRules.TendXpPercent / 100);
        FarmingRules.GetTendXp(25, 20).Should().BeLessThan(one, "tending pays a smaller amount than an item");
    }

    // Plot limits

    [Test]
    public void AHouseHoldsFivePlotsAndAGreenhouseTen_ACityOnlyItsStructureLimit()
    {
        FarmingRules.GetPlotLimit(PropertyType.House, false).Should().Be(5);
        FarmingRules.GetPlotLimit(PropertyType.House, true).Should().Be(10);
        FarmingRules.GetPlotLimit(PropertyType.City, false).Should().Be(FarmingRules.NoPlotLimit);
        FarmingRules.GetPlotLimit(PropertyType.Apartment, false).Should().Be(0);
        FarmingRules.GetPlotLimit(PropertyType.Starship, false).Should().Be(0);
    }

    // Planting

    private static FarmingRefusal Plant(
        bool isPlayer = true,
        bool plotExists = true,
        GardenPlot plot = null,
        bool permission = true,
        int rank = 5,
        int tier = 3,
        int growing = 0,
        bool seed = true) =>
        FarmingRules.CheckPlant(isPlayer, plotExists, plot, Planted, permission, rank, tier, growing, seed);

    [Test]
    public void ACharacterWithEverythingCanPlant()
    {
        Plant().Should().Be(FarmingRefusal.None);
        Plant(tier: 5, rank: 5, growing: 9).Should().Be(FarmingRefusal.None, "nine crops leave room for a tenth");
    }

    [Test]
    public void PlantingIsRefusedForEachMissingThing()
    {
        Plant(isPlayer: false).Should().Be(FarmingRefusal.NotAPlayer);
        Plant(plotExists: false).Should().Be(FarmingRefusal.NoPlot);
        Plant(plot: PlotOf(12)).Should().Be(FarmingRefusal.PlotOccupied);
        Plant(permission: false).Should().Be(FarmingRefusal.NoPermission);
        Plant(rank: 2, tier: 3).Should().Be(FarmingRefusal.TierLocked);
        Plant(rank: 0, tier: 1).Should().Be(FarmingRefusal.TierLocked);
        Plant(growing: 10).Should().Be(FarmingRefusal.CropCap);
        Plant(seed: false).Should().Be(FarmingRefusal.NoSeed);
    }

    [Test]
    public void PlantingChecksItsReasonsInOrder()
    {
        Plant(isPlayer: false, plotExists: false, permission: false, rank: 0, growing: 10, seed: false).Should().Be(FarmingRefusal.NotAPlayer);
        Plant(plotExists: false, permission: false, rank: 0, growing: 10, seed: false).Should().Be(FarmingRefusal.NoPlot);
        Plant(plot: PlotOf(12), permission: false, rank: 0, growing: 10, seed: false).Should().Be(FarmingRefusal.PlotOccupied);
        Plant(permission: false, rank: 0, growing: 10, seed: false).Should().Be(FarmingRefusal.NoPermission);
        Plant(rank: 0, growing: 10, seed: false).Should().Be(FarmingRefusal.TierLocked);
        Plant(growing: 10, seed: false).Should().Be(FarmingRefusal.CropCap);
    }

    [Test]
    public void AWitheredCropStillHoldsItsPlotUntilItIsRemoved()
    {
        var withered = PlotOf(12, planted: Planted.AddDays(-10));

        Plant(plot: withered).Should().Be(FarmingRefusal.PlotOccupied);
    }

    // Tending

    private static FarmingRefusal Tend(GardenPlot plot, DateTime now, bool isPlanter = true, bool permission = true, bool isPlayer = true) =>
        FarmingRules.CheckTend(isPlayer, plot, now, isPlanter, permission);

    [Test]
    public void ACropCanBeTendedOnceAfterAQuarterOfItsGrowTime()
    {
        var plot = PlotOf(12);

        Tend(plot, Planted.AddHours(2)).Should().Be(FarmingRefusal.TooYoungToTend);
        Tend(plot, Planted.AddHours(3)).Should().Be(FarmingRefusal.None);
        Tend(plot, Planted.AddHours(12)).Should().Be(FarmingRefusal.None, "a crop can still be tended once it is ripe");
        Tend(PlotOf(12, tended: true), Planted.AddHours(5)).Should().Be(FarmingRefusal.AlreadyTended);
    }

    [Test]
    public void ThePlanterAndAnyoneWithGardenPermissionCanTendButNobodyElse()
    {
        var plot = PlotOf(12);
        var ready = Planted.AddHours(4);

        Tend(plot, ready, isPlanter: true, permission: false).Should().Be(FarmingRefusal.None, "the planter tends their own crop");
        Tend(plot, ready, isPlanter: false, permission: true).Should().Be(FarmingRefusal.None);
        Tend(plot, ready, isPlanter: false, permission: false).Should().Be(FarmingRefusal.NoPermission);
        Tend(plot, ready, isPlayer: false).Should().Be(FarmingRefusal.NotAPlayer);
    }

    [Test]
    public void NothingCanBeTendedInAnEmptyPlotOrAWitheredCrop()
    {
        Tend(null, Planted).Should().Be(FarmingRefusal.PlotEmpty);
        Tend(PlotOf(12), Planted.AddHours(100)).Should().Be(FarmingRefusal.Withered);
    }

    // Harvesting and clearing

    [Test]
    public void OnlyThePlanterHarvestsAndOnlyARipeCrop()
    {
        var plot = PlotOf(12);

        FarmingRules.CheckHarvest(true, plot, Planted.AddHours(11), true).Should().Be(FarmingRefusal.NotRipe);
        FarmingRules.CheckHarvest(true, plot, Planted.AddHours(12), true).Should().Be(FarmingRefusal.None);
        FarmingRules.CheckHarvest(true, plot, Planted.AddHours(12), false).Should().Be(FarmingRefusal.NotThePlanter,
            "garden permission does not let anyone else take the harvest");
        FarmingRules.CheckHarvest(true, null, Planted, true).Should().Be(FarmingRefusal.PlotEmpty);
        FarmingRules.CheckHarvest(true, plot, Planted.AddHours(100), true).Should().Be(FarmingRefusal.Withered);
        FarmingRules.CheckHarvest(false, plot, Planted.AddHours(12), true).Should().Be(FarmingRefusal.NotAPlayer);
    }

    [Test]
    public void ThePlanterCanAlwaysClearTheirOwnCrop_AtAnyStage()
    {
        var plot = PlotOf(12);

        foreach (var hours in new[] { 0, 5, 12, 50, 100 })
            FarmingRules.CheckClear(true, plot, Planted.AddHours(hours), true).Should().Be(FarmingRefusal.None, $"{hours} hours in");

        FarmingRules.CheckClear(true, plot, Planted.AddHours(5), false).Should().Be(FarmingRefusal.NotThePlanter);
        FarmingRules.CheckClear(true, null, Planted, true).Should().Be(FarmingRefusal.PlotEmpty);
        FarmingRules.CheckClear(false, plot, Planted, true).Should().Be(FarmingRefusal.NotAPlayer);
    }

    // Text

    [Test]
    public void EveryRefusalHasAnAsciiMessage_AndNoneHasNone()
    {
        FarmingRules.Describe(FarmingRefusal.None).Should().BeEmpty();

        foreach (var refusal in Enum.GetValues<FarmingRefusal>().Where(refusal => refusal != FarmingRefusal.None))
        {
            var text = FarmingRules.Describe(refusal);
            text.Should().NotBeNullOrWhiteSpace(refusal.ToString());
            text.Should().MatchRegex("^[\\x20-\\x7E]+$", refusal.ToString());
            FarmingResult.Refused(refusal).Message.Should().Be(text);
            FarmingResult.Refused(refusal).Succeeded.Should().BeFalse();
        }

        FarmingResult.Success("ok").Succeeded.Should().BeTrue();
    }

    [Test]
    public void TheCropCapMessageNamesTheCap()
    {
        FarmingRules.Describe(FarmingRefusal.CropCap).Should().Contain("10");
    }

    [TestCase(0, "under a minute")]
    [TestCase(30, "under a minute")]
    [TestCase(60, "1m")]
    [TestCase(45 * 60, "45m")]
    [TestCase(60 * 60, "1h 0m")]
    [TestCase(5 * 3600 + 12 * 60, "5h 12m")]
    [TestCase(26 * 3600, "1d 2h")]
    [TestCase(-100, "under a minute")]
    public void ADurationReadsAsDaysHoursOrMinutes(int seconds, string text)
    {
        FarmingRules.FormatDuration(TimeSpan.FromSeconds(seconds)).Should().Be(text);
    }

    [Test]
    public void TheHarvestMessageNamesTheItemsOverflowAndSeed()
    {
        FarmingRules.DescribeHarvest("Tomato", 7, 0, false, false).Should().Be("You harvested the Tomato for 7 items.");
        FarmingRules.DescribeHarvest("Tomato", 1, 0, false, false).Should().Be("You harvested the Tomato for 1 item.");
        FarmingRules.DescribeHarvest("Tomato", 7, 2, true, false).Should()
            .Contain("2 did not fit").And.Contain("/overflow").And.EndWith("The seed came back.");
        FarmingRules.DescribeHarvest("Tomato", 3, 0, true, true).Should().EndWith("The seed came back, and is in your overflow storage.");
        FarmingRules.DescribeHarvest("Tomato", 3, 1, true, true).Should().MatchRegex("^[\\x20-\\x7E]+$");
    }

    [Test]
    public void TheOtherMessagesNameTheCrop()
    {
        FarmingRules.DescribePlanted("Tomato", 24).Should().Be("You planted Tomato. It will be ripe in 1d 0h.");
        FarmingRules.DescribePlanted("Orange", 12).Should().Be("You planted Orange. It will be ripe in 12h 0m.");
        FarmingRules.DescribeTended("Tomato").Should().Contain("Tomato").And.Contain("1 more item");
        FarmingRules.DescribeCleared("Tomato").Should().Contain("Tomato");
    }

    [Test]
    public void EveryStageHasAName()
    {
        foreach (var stage in Enum.GetValues<GardenPlotStage>())
            FarmingRules.DescribeStage(stage).Should().NotBeNullOrWhiteSpace(stage.ToString());
    }
}
