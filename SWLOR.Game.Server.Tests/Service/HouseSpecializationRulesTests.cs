using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.HouseSpecializationService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// House specializations (Player City Revamp, "House specializations"): the 30-day timer that stays with the house, who may change
/// it, the Storehouse adding and removing exactly its own storage beside furniture, the Greenhouse plot limit and the Workshop bonus.
/// </summary>
public class HouseSpecializationRulesTests
{
    private static readonly DateTime Start = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static WorldProperty House(int layoutStorage = 60, int furnitureStorage = 0, string owner = "owner")
    {
        return new WorldProperty
        {
            Id = "house",
            PropertyType = PropertyType.House,
            OwnerPlayerId = owner,
            ItemStorageCount = layoutStorage + furnitureStorage
        };
    }

    private static HouseSpecializationRefusal Check(
        WorldProperty house,
        HouseSpecializationType type,
        SkillType skill = SkillType.Invalid,
        string playerId = "owner",
        DateTime? now = null,
        bool inCity = true)
    {
        return HouseSpecializationRules.CheckChange(house, inCity, playerId, type, skill, now ?? Start);
    }

    #region Who and when

    [Test]
    public void TheFirstSpecialization_IsFree()
    {
        Check(House(), HouseSpecializationType.Storehouse).Should().Be(HouseSpecializationRefusal.None);
    }

    [Test]
    public void OnlyTheOwnerChangesIt()
    {
        var house = House();

        Check(house, HouseSpecializationType.Greenhouse, playerId: "guest").Should().Be(HouseSpecializationRefusal.NotTheOwner);
        Check(house, HouseSpecializationType.Greenhouse, playerId: "").Should().Be(HouseSpecializationRefusal.NotTheOwner);
    }

    [Test]
    public void OnlyAHouseInACityCanHaveOne()
    {
        var apartment = House();
        apartment.PropertyType = PropertyType.Apartment;

        Check(apartment, HouseSpecializationType.Greenhouse).Should().Be(HouseSpecializationRefusal.NotAHouse);
        Check(null, HouseSpecializationType.Greenhouse).Should().Be(HouseSpecializationRefusal.NotAHouse);
        Check(House(), HouseSpecializationType.Greenhouse, inCity: false).Should().Be(HouseSpecializationRefusal.NotInCity);
    }

    [Test]
    public void TheChoiceHasToBeOneOfTheThree()
    {
        Check(House(), HouseSpecializationType.None).Should().Be(HouseSpecializationRefusal.NotASpecialization);
    }

    [Test]
    public void AChangeWaitsThirtyDays()
    {
        var house = House();
        HouseSpecializationRules.Apply(house, HouseSpecializationType.Greenhouse, SkillType.Invalid, 60, Start);

        Check(house, HouseSpecializationType.Storehouse, now: Start.AddDays(29).AddHours(23)).Should().Be(HouseSpecializationRefusal.TooSoon);
        Check(house, HouseSpecializationType.Storehouse, now: Start.AddDays(30)).Should().Be(HouseSpecializationRefusal.None);
        HouseSpecializationRules.GetNextChangeUtc(house.HouseSpecializationChangedUtc).Should().Be(Start.AddDays(30));
    }

    [Test]
    public void TheTimerStaysWithTheHouseWhenItChangesHands()
    {
        var house = House(owner: "first");
        HouseSpecializationRules.Apply(house, HouseSpecializationType.Greenhouse, SkillType.Invalid, 60, Start);

        house.OwnerPlayerId = "second";

        Check(house, HouseSpecializationType.Storehouse, playerId: "second", now: Start.AddDays(10)).Should().Be(HouseSpecializationRefusal.TooSoon);
        Check(house, HouseSpecializationType.Storehouse, playerId: "first", now: Start.AddDays(40)).Should().Be(HouseSpecializationRefusal.NotTheOwner);
        Check(house, HouseSpecializationType.Storehouse, playerId: "second", now: Start.AddDays(31)).Should().Be(HouseSpecializationRefusal.None);
        house.HouseSpecialization.Should().Be(HouseSpecializationType.Greenhouse, "the type stays with the house");
    }

    [Test]
    public void ChoosingTheSameTypeAgainChangesNothing()
    {
        var house = House();
        HouseSpecializationRules.Apply(house, HouseSpecializationType.Greenhouse, SkillType.Invalid, 60, Start);

        Check(house, HouseSpecializationType.Greenhouse, now: Start.AddDays(90)).Should().Be(HouseSpecializationRefusal.Unchanged);
    }

    [Test]
    public void AWorkshopMovedToAnotherSkillIsAChange_AndWaitsLikeOne()
    {
        var house = House();
        HouseSpecializationRules.Apply(house, HouseSpecializationType.Workshop, SkillType.Fabrication, 60, Start);

        Check(house, HouseSpecializationType.Workshop, SkillType.Fabrication, now: Start.AddDays(90)).Should().Be(HouseSpecializationRefusal.Unchanged);
        Check(house, HouseSpecializationType.Workshop, SkillType.Smithery, now: Start.AddDays(5)).Should().Be(HouseSpecializationRefusal.TooSoon);
        Check(house, HouseSpecializationType.Workshop, SkillType.Smithery, now: Start.AddDays(30)).Should().Be(HouseSpecializationRefusal.None);
    }

    [Test]
    [TestCase(SkillType.Invalid)]
    [TestCase(SkillType.Gathering)]
    [TestCase(SkillType.Leadership)]
    public void AWorkshopNeedsACraftingSkill(SkillType skill)
    {
        Check(House(), HouseSpecializationType.Workshop, skill).Should().Be(HouseSpecializationRefusal.NeedsCraftingSkill);
    }

    [Test]
    [TestCase(SkillType.Smithery)]
    [TestCase(SkillType.Fabrication)]
    [TestCase(SkillType.Engineering)]
    [TestCase(SkillType.Agriculture)]
    public void EveryCraftingSkillCanBeAWorkshop(SkillType skill)
    {
        Check(House(), HouseSpecializationType.Workshop, skill).Should().Be(HouseSpecializationRefusal.None);
    }

    [Test]
    public void ARefusalHasAMessageForEveryReason()
    {
        foreach (var refusal in Enum.GetValues<HouseSpecializationRefusal>().Where(x => x != HouseSpecializationRefusal.None))
            HouseSpecializationRules.Describe(refusal, Start).Should().NotBeNullOrWhiteSpace(refusal.ToString());

        HouseSpecializationRules.Describe(HouseSpecializationRefusal.TooSoon, Start.AddDays(30)).Should().Contain("2026-12-02");
    }

    #endregion

    #region Storehouse

    [Test]
    [TestCase(40, 60)]
    [TestCase(60, 90)]
    [TestCase(80, 120)]
    public void AStorehouse_HoldsHalfAgainOfTheLayout(int layout, int expected)
    {
        var house = House(layout);

        HouseSpecializationRules.Apply(house, HouseSpecializationType.Storehouse, SkillType.Invalid, layout, Start);

        house.ItemStorageCount.Should().Be(expected);
        house.StorehouseBonus.Should().Be(expected - layout);
    }

    [Test]
    public void LeavingAStorehouse_TakesBackExactlyWhatItAdded()
    {
        var house = House(60);
        HouseSpecializationRules.Apply(house, HouseSpecializationType.Storehouse, SkillType.Invalid, 60, Start);

        HouseSpecializationRules.Apply(house, HouseSpecializationType.Greenhouse, SkillType.Invalid, 60, Start.AddDays(30));

        house.ItemStorageCount.Should().Be(60);
        house.StorehouseBonus.Should().Be(0);
    }

    [Test]
    public void FurnitureStorage_SurvivesEverySwitch_Placement_Retrieval_AndReload()
    {
        // A medium house (60) with a 10 item cabinet placed: 70.
        var house = House(60, furnitureStorage: 10);

        HouseSpecializationRules.Apply(house, HouseSpecializationType.Storehouse, SkillType.Invalid, 60, Start);
        house.ItemStorageCount.Should().Be(100, "60 + 10 from furniture + 30 from the Storehouse");

        // Placing more furniture adds to the count, as CreateStructure does.
        house.ItemStorageCount += 5;
        house.ItemStorageCount.Should().Be(105);

        // The record is saved and loaded as it is: the bonus is read back, not recomputed.
        var reloaded = new WorldProperty
        {
            PropertyType = house.PropertyType,
            OwnerPlayerId = house.OwnerPlayerId,
            ItemStorageCount = house.ItemStorageCount,
            StorehouseBonus = house.StorehouseBonus,
            HouseSpecialization = house.HouseSpecialization,
            HouseSpecializationChangedUtc = house.HouseSpecializationChangedUtc
        };

        // Leaving with the layout constant changed since: the recorded amount comes off, not a recomputed one.
        HouseSpecializationRules.Apply(reloaded, HouseSpecializationType.Workshop, SkillType.Engineering, 999, Start.AddDays(30));
        reloaded.ItemStorageCount.Should().Be(75, "60 layout + 15 of furniture; only the Storehouse's 30 left");

        // Retrieving the 10 item cabinet subtracts what it added, as ManageStructures does.
        reloaded.ItemStorageCount -= 10;
        reloaded.ItemStorageCount.Should().Be(65);

        // Back to a Storehouse and out again leaves furniture alone.
        HouseSpecializationRules.Apply(reloaded, HouseSpecializationType.Storehouse, SkillType.Invalid, 60, Start.AddDays(60));
        reloaded.ItemStorageCount.Should().Be(95);
        HouseSpecializationRules.Apply(reloaded, HouseSpecializationType.Greenhouse, SkillType.Invalid, 60, Start.AddDays(90));
        reloaded.ItemStorageCount.Should().Be(65);
    }

    [Test]
    public void SwitchingFromStorehouseToStorehouse_IsRefusedSoItNeverDoubleCounts()
    {
        var house = House(40);
        HouseSpecializationRules.Apply(house, HouseSpecializationType.Storehouse, SkillType.Invalid, 40, Start);

        Check(house, HouseSpecializationType.Storehouse, now: Start.AddDays(60)).Should().Be(HouseSpecializationRefusal.Unchanged);
    }

    [Test]
    public void ALeftStorehouseNeverGoesBelowZero()
    {
        var house = House(40);
        house.HouseSpecialization = HouseSpecializationType.Storehouse;
        house.StorehouseBonus = 500;

        HouseSpecializationRules.Apply(house, HouseSpecializationType.Greenhouse, SkillType.Invalid, 40, Start);

        house.ItemStorageCount.Should().Be(0);
    }

    [Test]
    public void OverTheLimitAfterLeaving_DepositsStop_AndWithdrawalsAlwaysWork()
    {
        var house = House(60);
        HouseSpecializationRules.Apply(house, HouseSpecializationType.Storehouse, SkillType.Invalid, 60, Start);
        var stored = 85;
        HouseSpecializationRules.CanDeposit(stored, house.ItemStorageCount).Should().BeTrue();

        HouseSpecializationRules.Apply(house, HouseSpecializationType.Workshop, SkillType.Smithery, 60, Start.AddDays(30));

        house.ItemStorageCount.Should().Be(60);
        HouseSpecializationRules.CanDeposit(stored, house.ItemStorageCount).Should().BeFalse("85 stored is over the 60 limit");
        HouseSpecializationRules.CanWithdraw().Should().BeTrue();

        // Withdrawals bring it back under; a deposit works again only below the limit.
        HouseSpecializationRules.CanDeposit(60, house.ItemStorageCount).Should().BeFalse();
        HouseSpecializationRules.CanDeposit(59, house.ItemStorageCount).Should().BeTrue();
    }

    #endregion

    #region Greenhouse

    [Test]
    public void AGreenhouse_HoldsTenPlots_AHouseFive()
    {
        FarmingRules.GetPlotLimit(PropertyType.House, true).Should().Be(10);
        FarmingRules.GetPlotLimit(PropertyType.House, false).Should().Be(5);
    }

    [Test]
    public void LeavingAGreenhouse_LeavesTheFirstFivePlotsPlantable_AndNotTheRest()
    {
        var plots = Enumerable.Range(1, 8).Select(index => $"plot-{index}").ToList();

        for (var index = 0; index < 5; index++)
            HouseSpecializationRules.IsPlotAboveLimit(plots, plots[index], false).Should().BeFalse(plots[index]);
        for (var index = 5; index < 8; index++)
            HouseSpecializationRules.IsPlotAboveLimit(plots, plots[index], false).Should().BeTrue(plots[index]);
    }

    [Test]
    public void AGreenhouse_PlantsEveryPlot()
    {
        var plots = Enumerable.Range(1, 10).Select(index => $"plot-{index}").ToList();

        plots.Any(plot => HouseSpecializationRules.IsPlotAboveLimit(plots, plot, true)).Should().BeFalse();
    }

    [Test]
    public void APlotThatIsNotListed_IsNeverBlocked()
    {
        HouseSpecializationRules.IsPlotAboveLimit(new List<string> { "a" }, "elsewhere", false).Should().BeFalse();
        HouseSpecializationRules.IsPlotAboveLimit(null, "a", false).Should().BeFalse();
    }

    [Test]
    public void PickingUpAnEarlierPlot_LetsALaterOneBeReplanted()
    {
        var plots = Enumerable.Range(1, 6).Select(index => $"plot-{index}").ToList();
        HouseSpecializationRules.IsPlotAboveLimit(plots, "plot-6", false).Should().BeTrue();

        plots.Remove("plot-2");

        HouseSpecializationRules.IsPlotAboveLimit(plots, "plot-6", false).Should().BeFalse("it is now the fifth plot");
    }

    [Test]
    public void ARefusedPlanting_ExplainsWhy()
    {
        var refusal = FarmingRules.CheckPlant(true, true, null, Start, true, 5, 1, 0, true, siteRefusal: FarmingRefusal.PlotAboveLimit);

        refusal.Should().Be(FarmingRefusal.PlotAboveLimit);
        FarmingRules.Describe(refusal).Should().Contain("Greenhouse");
    }

    [Test]
    public void ACropAlreadyGrowingInAnAboveLimitPlot_KeepsItsOccupiedAnswer()
    {
        var plot = new GardenPlot
        {
            Crop = CropType.Tomato,
            PlantedUtc = Start,
            RipeUtc = Start.AddHours(12),
            WitherUtc = Start.AddHours(84)
        };

        FarmingRules.CheckPlant(true, true, plot, Start.AddHours(1), true, 5, 1, 0, true, siteRefusal: FarmingRefusal.PlotAboveLimit)
            .Should().Be(FarmingRefusal.PlotOccupied);
        FarmingRules.CheckHarvest(true, plot, Start.AddHours(13), true).Should().Be(FarmingRefusal.None, "crops finish and can be harvested");
    }

    #endregion

    #region Workshop

    [Test]
    public void AWorkshop_AddsItsBonus_OnlyForItsSkill()
    {
        HouseSpecializationRules.GetWorkshopBonus(HouseSpecializationType.Workshop, SkillType.Fabrication, SkillType.Fabrication)
            .Should().Be(HouseSpecializationRules.WorkshopBonus);
        HouseSpecializationRules.GetWorkshopBonus(HouseSpecializationType.Workshop, SkillType.Fabrication, SkillType.Smithery).Should().Be(0);
        HouseSpecializationRules.GetWorkshopBonus(HouseSpecializationType.Storehouse, SkillType.Fabrication, SkillType.Fabrication).Should().Be(0);
        HouseSpecializationRules.GetWorkshopBonus(HouseSpecializationType.Workshop, SkillType.Invalid, SkillType.Invalid).Should().Be(0);
    }

    [Test]
    public void TheWorkshopBonus_IsOneConstant_ProposedAtFive()
    {
        HouseSpecializationRules.WorkshopBonus.Should().Be(5);
    }

    [Test]
    public void ACraftThatIsOpen_KeepsTheBonusItOpenedWith()
    {
        var session = new CraftSessionRecord { Skill = SkillType.Fabrication, WorkshopBonus = 5 };

        // The owner changed the house to a Storehouse mid-craft: the live bonus is 0, the craft keeps 5.
        HouseSpecializationRules.ResolveWorkshopBonus(session, SkillType.Fabrication, liveBonus: 0).Should().Be(5);
    }

    [Test]
    public void ACraftOpenedWithoutAWorkshop_IsNotGivenOneMidCraft()
    {
        var session = new CraftSessionRecord { Skill = SkillType.Fabrication, WorkshopBonus = 0 };

        HouseSpecializationRules.ResolveWorkshopBonus(session, SkillType.Fabrication, liveBonus: 5).Should().Be(0);
    }

    [Test]
    public void ACraftSavedBeforeWorkshopsExisted_HasNone()
    {
        var session = new CraftSessionRecord { Skill = SkillType.Fabrication, WorkshopBonus = null };

        HouseSpecializationRules.ResolveWorkshopBonus(session, SkillType.Fabrication, liveBonus: 5).Should().Be(0);
    }

    [Test]
    public void AnotherSkillIsNotHeldByTheOpenCraft()
    {
        var session = new CraftSessionRecord { Skill = SkillType.Fabrication, WorkshopBonus = 5 };

        HouseSpecializationRules.ResolveWorkshopBonus(session, SkillType.Smithery, liveBonus: 0).Should().Be(0);
        HouseSpecializationRules.ResolveWorkshopBonus(session, SkillType.Smithery, liveBonus: 5).Should().Be(5);
    }

    [Test]
    public void WithoutAnOpenCraft_TheHouseTheCrafterStandsInDecides()
    {
        HouseSpecializationRules.ResolveWorkshopBonus(null, SkillType.Fabrication, liveBonus: 5).Should().Be(5);
        HouseSpecializationRules.ResolveWorkshopBonus(null, SkillType.Fabrication, liveBonus: 0).Should().Be(0);
    }

    #endregion

    #region Text

    [Test]
    public void TheSpecializationReadsAsPlainText()
    {
        HouseSpecializationRules.Describe(HouseSpecializationType.Workshop, SkillType.Fabrication).Should().Be("Workshop (Fabrication)");
        HouseSpecializationRules.Describe(HouseSpecializationType.Storehouse, SkillType.Invalid).Should().Be("Storehouse");
        HouseSpecializationRules.Describe(HouseSpecializationType.Greenhouse, SkillType.Invalid).Should().Be("Greenhouse");
        HouseSpecializationRules.Describe(HouseSpecializationType.None, SkillType.Invalid).Should().Be("None");
    }

    [Test]
    public void EveryEffectText_IsPlainAscii()
    {
        foreach (var type in Enum.GetValues<HouseSpecializationType>())
            HouseSpecializationRules.DescribeEffect(type).All(c => c < 128).Should().BeTrue(type.ToString());
    }

    #endregion
}
