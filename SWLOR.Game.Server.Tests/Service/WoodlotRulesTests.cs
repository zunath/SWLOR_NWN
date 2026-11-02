using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Player City Revamp, "Farming for other crafts": the woodlot rules. A sapling grows 3 days in an outdoor city plot and yields 4
/// logs of its tier, flat. These tests pin what applies to a sapling of what crops get (tending, fertilizer, Bountiful Harvest, a
/// Greenhouse, Home Ground and pests: none of them), and what a sapling shares with crops (the tier gate and the 10-crop cap).
/// </summary>
public class WoodlotRulesTests
{
    private static readonly DateTime Planted = new(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc);

    private static GardenPlot SaplingPlot(CropType crop = CropType.AncientWoodSapling, bool tended = false, bool fertilized = false)
    {
        var ripe = FarmingRules.GetRipeUtc(Planted, WoodlotRules.GrowHours);
        return new GardenPlot
        {
            PlotId = "plot",
            PlanterPlayerId = "planter",
            PlanterCdKey = "KEY",
            Crop = crop,
            PlantedUtc = Planted,
            TendReadyUtc = FarmingRules.GetTendReadyUtc(Planted, WoodlotRules.GrowHours),
            RipeUtc = ripe,
            WitherUtc = FarmingRules.GetWitherUtc(ripe),
            Tended = tended,
            Fertilized = fertilized
        };
    }

    // The saplings

    [Test]
    public void TheFiveSaplingsAreTheLastFiveCropTypes_OneForEachTreeTier()
    {
        WoodlotRules.SaplingTypes.Should().Equal(
            CropType.WoodSapling, CropType.FineWoodSapling, CropType.AncientWoodSapling, CropType.AraciaWoodSapling, CropType.HyphaeWoodSapling);
        WoodlotRules.SaplingTypes.Select(type => (int)type).Should().Equal(27, 28, 29, 30, 31);

        foreach (var type in Enum.GetValues<CropType>().Where(type => type != CropType.Invalid))
            WoodlotRules.IsSapling(type).Should().Be((int)type >= 27, type.ToString());

        WoodlotRules.IsSapling(CropType.Invalid).Should().BeFalse();
    }

    [TestCase(1, "sapling_wood", "wood", "HARVESTING_TREE")]
    [TestCase(2, "sapling_fine", "fine_wood", "HARVESTING_OAK_TREE")]
    [TestCase(3, "sapling_ancient", "ancient_wood", "HARVESTING_ANCIENT_TREE")]
    [TestCase(4, "sapling_aracia", "aracia_wood", "HARVESTING_ARACIA_TREE")]
    [TestCase(5, "sapling_hyphae", "hyphae_wood", "HARVESTING_HYPHAE_TREE")]
    public void EachTierHasItsSaplingItsLogsAndItsTree(int tier, string sapling, string wood, string treeTable)
    {
        WoodlotRules.GetSaplingResref(tier).Should().Be(sapling);
        WoodlotRules.GetWoodResref(tier).Should().Be(wood);
        WoodlotRules.GetTreeTable(tier).Should().Be(treeTable);
        WoodlotRules.GetTier(WoodlotRules.SaplingTypes[tier - 1]).Should().Be(tier);
        WoodlotRules.IsSaplingResref(sapling).Should().BeTrue();
        WoodlotRules.IsSaplingResref(sapling.ToUpperInvariant()).Should().BeTrue();
        WoodlotRules.IsSaplingResref(wood).Should().BeFalse();
    }

    [TestCase(0)]
    [TestCase(6)]
    public void ATierOutsideOneToFiveIsRefused(int tier)
    {
        var sapling = () => WoodlotRules.GetSaplingResref(tier);
        var wood = () => WoodlotRules.GetWoodResref(tier);
        var table = () => WoodlotRules.GetTreeTable(tier);

        sapling.Should().Throw<ArgumentOutOfRangeException>();
        wood.Should().Throw<ArgumentOutOfRangeException>();
        table.Should().Throw<ArgumentOutOfRangeException>();
        WoodlotRules.GetTier(CropType.Tomato).Should().Be(0, "a crop is not a sapling");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    [TestCase("seed_orange")]
    [TestCase("sapling")]
    public void OnlyTheFiveSaplingItemsAreSaplingResrefs(string resref)
    {
        WoodlotRules.IsSaplingResref(resref).Should().BeFalse();
    }

    // Growing: 3 days, whatever the tier

    [Test]
    public void ASaplingGrowsThreeDays_AndWithersSeventyTwoHoursAfterItRipens()
    {
        WoodlotRules.GrowHours.Should().Be(72);
        WoodlotRules.GrowDays.Should().Be(3);

        var plot = SaplingPlot();

        plot.RipeUtc.Should().Be(Planted.AddHours(72));
        plot.WitherUtc.Should().Be(Planted.AddHours(144), "the same 72 hours as a crop");
        FarmingRules.GetStage(plot, Planted.AddHours(71)).Should().Be(GardenPlotStage.Growing);
        FarmingRules.GetStage(plot, Planted.AddHours(72)).Should().Be(GardenPlotStage.Ripe);
        FarmingRules.GetStage(plot, Planted.AddHours(143)).Should().Be(GardenPlotStage.Ripe);
        FarmingRules.GetStage(plot, Planted.AddHours(144)).Should().Be(GardenPlotStage.Withered);
    }

    // The yield is flat: nothing that adds to a crop adds to a sapling

    [Test]
    public void TheYieldIsFourLogsWhateverTheCropBonuses()
    {
        WoodlotRules.LogsPerHarvest.Should().Be(4);

        foreach (var tended in new[] { false, true })
        foreach (var fertilized in new[] { false, true })
        foreach (var greenhouse in new[] { false, true })
        foreach (var homeGround in new[] { false, true })
        foreach (var pests in new[] { false, true })
        foreach (var rank in new[] { -1, 0, 1, 2, 3, 9 })
        {
            WoodlotRules.GetYield(tended, fertilized, rank, greenhouse, homeGround, pests).Should().Be(
                4,
                $"tended {tended}, fertilized {fertilized}, Bountiful Harvest {rank}, Greenhouse {greenhouse}, Home Ground {homeGround}, pests {pests}");
        }
    }

    [Test]
    public void ACropWithEveryBonusYieldsTenWhereASaplingStaysAtFour()
    {
        FarmingRules.GetYield(true, true, 3, true, true).Should().Be(10);
        WoodlotRules.GetYield(true, true, 3, true, true, false).Should().Be(4);
        FarmingRules.GetYield(false, false, 0, false, false, pests: true).Should().Be(2, "pests take 1 from a crop");
        WoodlotRules.GetYield(false, false, 0, false, false, pests: true).Should().Be(4, "and nothing from a sapling");
    }

    // Tending: there is nothing to add to, so it is refused and never offered

    [Test]
    public void ASaplingCannotBeTended_AtAnyStageOrByAnyone()
    {
        var plot = SaplingPlot();

        foreach (var hours in new[] { 0, 17, 18, 40, 71, 72, 100 })
        {
            var now = Planted.AddHours(hours);
            FarmingRules.CheckTend(true, plot, now, isPlanter: true, hasPermission: true).Should().Be(FarmingRefusal.NeedsNoTending, $"{hours} h");
            FarmingRules.CheckTend(true, plot, now, isPlanter: false, hasPermission: true).Should().Be(FarmingRefusal.NeedsNoTending, $"{hours} h, a guest");
        }
    }

    [Test]
    public void ASaplingIsNeverReadyToTend_SoNoWindowOrFarmhandOffersIt()
    {
        var sapling = SaplingPlot();
        var crop = SaplingPlot(CropType.Tomato);

        FarmingRules.GetStage(sapling, sapling.TendReadyUtc.AddMinutes(1)).Should().Be(GardenPlotStage.Growing);
        FarmingRules.GetStage(crop, crop.TendReadyUtc.AddMinutes(1)).Should().Be(GardenPlotStage.ReadyToTend, "a crop of the same times is");
    }

    [Test]
    public void ACropsTendingIsUntouched()
    {
        var crop = SaplingPlot(CropType.Tomato);

        FarmingRules.CheckTend(true, crop, crop.TendReadyUtc, isPlanter: true, hasPermission: false).Should().Be(FarmingRefusal.None);
        FarmingRules.CheckTend(true, crop, crop.TendReadyUtc.AddMinutes(-1), isPlanter: true, hasPermission: false).Should().Be(FarmingRefusal.TooYoungToTend);
        FarmingRules.CheckTend(true, null, Planted, isPlanter: true, hasPermission: true).Should().Be(FarmingRefusal.PlotEmpty);
    }

    [Test]
    public void ATendedFlagOnASaplingChangesNothingEither()
    {
        // A farmhand or a stray record cannot add a log: the yield ignores the flag, and the stage never offers tending.
        FarmingRules.GetStage(SaplingPlot(tended: true), Planted.AddHours(30)).Should().Be(GardenPlotStage.Growing);
        WoodlotRules.GetYield(true, true, 3, true, true, false).Should().Be(WoodlotRules.GetYield(false, false, 0, false, false, false));
    }

    // Where a sapling can go: outdoor city plots, never houses or station decks

    [Test]
    public void ASaplingNeedsAnOutdoorPlotOfAPlanetCity()
    {
        WoodlotRules.GetSiteRefusal(true, PropertyType.City, parentIsStation: false).Should().Be(FarmingRefusal.None);
        WoodlotRules.GetSiteRefusal(true, PropertyType.House, parentIsStation: false).Should().Be(FarmingRefusal.OutdoorPlotOnly);
        WoodlotRules.GetSiteRefusal(true, PropertyType.House, parentIsStation: true).Should().Be(FarmingRefusal.OutdoorPlotOnly, "a house on a deck is still a house");
        WoodlotRules.GetSiteRefusal(true, PropertyType.City, parentIsStation: true).Should().Be(FarmingRefusal.NoOpenGroundOnStation);
        WoodlotRules.GetSiteRefusal(true, PropertyType.Invalid, parentIsStation: false).Should().Be(FarmingRefusal.OutdoorPlotOnly);
    }

    [Test]
    public void ACropGoesInAnyPlot_OnlyASaplingIsPickyAboutTheSite()
    {
        foreach (var parent in new[] { PropertyType.City, PropertyType.House })
        foreach (var station in new[] { false, true })
            WoodlotRules.GetSiteRefusal(false, parent, station).Should().Be(FarmingRefusal.None, $"{parent}, station {station}");
    }

    [Test]
    public void ThePlantCheckRefusesASaplingInAHouse_AfterThePermission()
    {
        var site = WoodlotRules.GetSiteRefusal(true, PropertyType.House, false);

        FarmingRules.CheckPlant(true, true, null, Planted, true, 5, 3, 0, true, site).Should().Be(FarmingRefusal.OutdoorPlotOnly);
        FarmingRules.CheckPlant(true, true, null, Planted, false, 5, 3, 0, true, site).Should().Be(FarmingRefusal.NoPermission, "permission comes first");
        FarmingRules.CheckPlant(true, true, null, Planted, true, 5, 3, 0, true, FarmingRefusal.None).Should().Be(FarmingRefusal.None);
    }

    [Test]
    public void TheSiteRefusalIsOptional_SoEveryCropCheckBehavesAsBefore()
    {
        FarmingRules.CheckPlant(true, true, null, Planted, true, 5, 3, 0, true).Should().Be(FarmingRefusal.None);
        FarmingRules.CheckPlant(true, true, null, Planted, true, 1, 3, 0, true).Should().Be(FarmingRefusal.TierLocked);
        FarmingRules.CheckPlant(true, true, null, Planted, true, 5, 3, FarmingRules.MaxCropsPerCdKey, true).Should().Be(FarmingRefusal.CropCap);
    }

    // What a sapling shares with crops: the tier gate and the cap

    [TestCase(0, 1, false)]
    [TestCase(1, 1, true)]
    [TestCase(1, 2, false)]
    [TestCase(2, 2, true)]
    [TestCase(2, 3, false)]
    [TestCase(5, 5, true)]
    public void CropManagementGatesASaplingsTier(int rank, int tier, bool allowed)
    {
        var refusal = FarmingRules.CheckPlant(true, true, null, Planted, true, rank, tier, 0, true, FarmingRefusal.None);

        (refusal == FarmingRefusal.None).Should().Be(allowed);
        if (!allowed)
            refusal.Should().Be(FarmingRefusal.TierLocked);
    }

    [Test]
    public void SaplingsCountTowardTheTenCropCap_WithCrops()
    {
        var saplings = Enumerable.Range(0, 4).Select(_ => SaplingPlot()).ToList();
        var crops = Enumerable.Range(0, 6).Select(_ => SaplingPlot(CropType.Tomato)).ToList();
        var now = Planted.AddHours(10);

        FarmingRules.CountGrowingCrops(saplings, now).Should().Be(4);
        FarmingRules.CountGrowingCrops(saplings.Concat(crops), now).Should().Be(FarmingRules.MaxCropsPerCdKey);
        FarmingRules.HasRoomForCrop(FarmingRules.CountGrowingCrops(saplings.Concat(crops), now)).Should().BeFalse("a tenth sapling or crop shares the 10");
        FarmingRules.CheckPlant(true, true, null, now, true, 5, 3, 10, true, FarmingRefusal.None).Should().Be(FarmingRefusal.CropCap);
    }

    [Test]
    public void AWitheredSaplingStopsCounting()
    {
        var plot = SaplingPlot();

        FarmingRules.CountGrowingCrops(new[] { plot }, plot.WitherUtc.AddMinutes(-1)).Should().Be(1);
        FarmingRules.CountGrowingCrops(new[] { plot }, plot.WitherUtc).Should().Be(0);
    }

    // The passive budget: 10 saplings, 4 logs every 3 days

    [Test]
    public void TenSaplingsMakeThirteenLogsADay_AndNoMoreWhateverIsPlanted()
    {
        WoodlotRules.GetLogsPerDay(10).Should().BeApproximately(13.33, 0.01, "the plan's 13 logs a day");
        WoodlotRules.GetLogsPerDay(1).Should().BeApproximately(4.0 / 3, 1e-9);
        WoodlotRules.GetLogsPerDay(0).Should().Be(0);
        WoodlotRules.GetLogsPerDay(25).Should().Be(WoodlotRules.GetLogsPerDay(10), "the cap is 10 crops and saplings together");
    }

    [Test]
    public void ASaplingsXpIsThatOfItsTierLevel_PerLog()
    {
        // Logs are harvested items like any crop's, paid by the same level-difference formula at the tier's level.
        FarmingRules.GetHarvestXp(25, 20, WoodlotRules.LogsPerHarvest).Should().Be(4 * Skill.GetDeltaXP(25 - 20));
        FarmingRules.GetHarvestXp(5, 50, WoodlotRules.LogsPerHarvest).Should().Be(0, "a sapling the farmer has outgrown pays nothing");
    }

    [Test]
    public void TheSeedLabelNamesASaplingASapling_AndACropsItemASeed()
    {
        new CropDefinition { Name = "Wood Tree", IsSapling = true }.GetSeedLabel().Should().Be("Wood Tree sapling");
        new CropDefinition { Name = "Tomato" }.GetSeedLabel().Should().Be("Tomato seed");
    }

    [Test]
    public void EveryDescriptionOfASaplingRefusalIsPlainAscii()
    {
        foreach (var refusal in new[] { FarmingRefusal.NeedsNoTending, FarmingRefusal.OutdoorPlotOnly, FarmingRefusal.NoOpenGroundOnStation })
            FarmingRules.Describe(refusal).Should().NotBeNullOrWhiteSpace().And.MatchRegex("^[\\x20-\\x7E]+$");
    }
}
