using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.ClerkRecoveryService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The City Hall clerk's recovery rules (Player City Revamp, "Farming", "Livestock" and "Recovery"): who cannot reach their crop or
/// animal, which actions each allows, the 7-day rule for an absent owner, and the marks and overflow record ids.
/// </summary>
public class ClerkRecoveryRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static GardenPlot CropOf(DateTime planted, int growHours, bool tended = false)
    {
        var ripe = FarmingRules.GetRipeUtc(planted, growHours);
        return new GardenPlot
        {
            PlotId = "plot",
            PlanterPlayerId = "planter",
            Crop = CropType.Tomato,
            PlantedUtc = planted,
            TendReadyUtc = FarmingRules.GetTendReadyUtc(planted, growHours),
            RipeUtc = ripe,
            WitherUtc = FarmingRules.GetWitherUtc(ripe),
            Tended = tended
        };
    }

    private static Livestock AnimalOf(DateTime placed, int feedings = 0, double? fedHoursAfterPlaced = null, double? collectedHoursAfterPlaced = null)
    {
        return new Livestock
        {
            OwnerPlayerId = "owner",
            CorralId = "corral",
            Species = LivestockType.Warocas,
            PlacedUtc = placed,
            Feedings = feedings,
            LastFedUtc = fedHoursAfterPlaced is { } fed ? placed.AddHours(fed) : null,
            LastCollectedUtc = collectedHoursAfterPlaced is { } collected ? placed.AddHours(collected) : null,
            LastProcessedUtc = placed
        };
    }

    #region Who cannot reach

    [TestCase(true, true, true)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(false, false, false)]
    public void APlanterReachesTheirCrop_OnlyWithGardenPermissionAndCitizenship(bool permission, bool citizen, bool reaches)
    {
        // Permission removed, the house changed hands (their record is gone), or they left the city: each is a way to lose one of the two.
        ClerkRecoveryRules.CanReachCrop(permission, citizen).Should().Be(reaches);
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public void AnOwnerReachesTheirCorral_OnlyAsACitizenOfItsCity(bool citizen, bool reaches)
    {
        ClerkRecoveryRules.CanReachAnimal(citizen).Should().Be(reaches);
    }

    #endregion

    #region Offered actions

    [Test]
    public void ARipeCrop_OffersHarvestAndClear()
    {
        var crop = CropOf(Now.AddHours(-13), 12);

        ClerkRecoveryRules.GetCropActions(crop, Now).Should().Equal(ClerkRecoveryAction.Harvest, ClerkRecoveryAction.Clear);
    }

    [Test]
    public void AGrowingCrop_OffersOnlyClear()
    {
        var crop = CropOf(Now.AddHours(-2), 12);

        FarmingRules.GetStage(crop, Now).Should().Be(GardenPlotStage.Growing);
        ClerkRecoveryRules.GetCropActions(crop, Now).Should().Equal(ClerkRecoveryAction.Clear);
    }

    [Test]
    public void ACropReadyToTend_StillOffersOnlyClear_SinceTheClerkDoesNotTend()
    {
        var crop = CropOf(Now.AddHours(-4), 12);

        FarmingRules.GetStage(crop, Now).Should().Be(GardenPlotStage.ReadyToTend);
        ClerkRecoveryRules.GetCropActions(crop, Now).Should().Equal(ClerkRecoveryAction.Clear);
    }

    [Test]
    public void AWitheredCrop_AnAbsentRecord_AndAnEmptyPlot_OfferNothing()
    {
        ClerkRecoveryRules.GetCropActions(CropOf(Now.AddHours(-12 - FarmingRules.WitherHours - 1), 12), Now).Should().BeEmpty();
        ClerkRecoveryRules.GetCropActions(null, Now).Should().BeEmpty();
        ClerkRecoveryRules.GetCropActions(new GardenPlot(), Now).Should().BeEmpty();
    }

    [Test]
    public void AYoungAnimal_OffersOnlyCrate()
    {
        var animal = AnimalOf(Now.AddHours(-10), feedings: 1, fedHoursAfterPlaced: 1);

        ClerkRecoveryRules.GetAnimalActions(animal, Now).Should().Equal(ClerkRecoveryAction.Crate);
    }

    [Test]
    public void AnAdultFedRecently_OffersCollectButcherAndCrate()
    {
        var animal = AnimalOf(Now.AddHours(-80), feedings: 4, fedHoursAfterPlaced: 70);

        ClerkRecoveryRules.GetAnimalActions(animal, Now).Should().Equal(
            ClerkRecoveryAction.Collect,
            ClerkRecoveryAction.Butcher,
            ClerkRecoveryAction.Crate);
    }

    [Test]
    public void AnAdultNotFedRecently_OffersButcherAndCrateButNotCollect()
    {
        var animal = AnimalOf(Now.AddHours(-80), feedings: 4, fedHoursAfterPlaced: 40);

        ClerkRecoveryRules.GetAnimalActions(animal, Now).Should().Equal(ClerkRecoveryAction.Butcher, ClerkRecoveryAction.Crate);
    }

    [Test]
    public void AnAdultCollectedFromTooRecently_OffersNoCollect()
    {
        var animal = AnimalOf(Now.AddHours(-80), feedings: 4, fedHoursAfterPlaced: 70, collectedHoursAfterPlaced: 75);

        ClerkRecoveryRules.GetAnimalActions(animal, Now).Should().NotContain(ClerkRecoveryAction.Collect);
    }

    [Test]
    public void AnAnimalThatWanderedOff_OffersNothing()
    {
        var animal = AnimalOf(Now.AddHours(-(LivestockRules.WanderOffHours + 1)));

        ClerkRecoveryRules.GetAnimalActions(animal, Now).Should().BeEmpty();
        ClerkRecoveryRules.GetAnimalActions(null, Now).Should().BeEmpty();
    }

    #endregion

    #region Absent owners

    [Test]
    public void AnOwnerWhoLeftMoreThanSevenDaysAgo_IsAbsent()
    {
        ClerkRecoveryRules.IsAbsent(false, false, Now.AddDays(-7).AddSeconds(-1), Now).Should().BeTrue();
        ClerkRecoveryRules.IsAbsent(false, false, Now.AddDays(-30), Now).Should().BeTrue();
    }

    [Test]
    public void AnOwnerWhoLeftSevenDaysAgoOrLess_IsNotYetAbsent()
    {
        ClerkRecoveryRules.IsAbsent(false, false, Now.AddDays(-7), Now).Should().BeFalse("it is more than 7 days that counts");
        ClerkRecoveryRules.IsAbsent(false, false, Now.AddDays(-6), Now).Should().BeFalse();
        ClerkRecoveryRules.IsAbsent(false, false, Now, Now).Should().BeFalse();
    }

    [Test]
    public void ACitizen_IsNeverAbsent()
    {
        ClerkRecoveryRules.IsAbsent(true, false, Now.AddDays(-30), Now).Should().BeFalse("a returned citizen's old departure no longer counts");
        ClerkRecoveryRules.IsAbsent(true, false, null, Now).Should().BeFalse();
    }

    [Test]
    public void ADepartureNeverRecorded_CannotBeShownToBeOverSevenDays()
    {
        ClerkRecoveryRules.IsAbsent(false, false, null, Now).Should().BeFalse();
    }

    [Test]
    public void ADeletedCharacter_IsAbsentAtOnce_SinceItCannotComeBack()
    {
        ClerkRecoveryRules.IsAbsent(false, true, null, Now).Should().BeTrue();
        ClerkRecoveryRules.IsAbsent(false, true, Now, Now).Should().BeTrue();
    }

    [Test]
    public void TheSevenDays_AreTheRulesConstant()
    {
        ClerkRecoveryRules.AbsentDays.Should().Be(7);
    }

    [Test]
    public void ADeparture_IsRecordedPerCity_AndEndsWhenTheOwnerRejoins()
    {
        var departures = ClerkRecoveryRules.RecordDeparture(null, "city-a", Now);
        departures = ClerkRecoveryRules.RecordDeparture(departures, "city-b", Now.AddDays(1));

        departures["city-a"].Should().Be(Now);
        departures["city-b"].Should().Be(Now.AddDays(1));

        departures = ClerkRecoveryRules.RecordDeparture(departures, "city-a", Now.AddDays(10));
        departures["city-a"].Should().Be(Now.AddDays(10), "a second departure counts from the latest");

        departures = ClerkRecoveryRules.ClearDeparture(departures, "city-a");
        departures.Should().NotContainKey("city-a").And.ContainKey("city-b");
    }

    [Test]
    public void ARecordWithNoDepartures_LoadsEmptyAndIsFilledIn()
    {
        ClerkRecoveryRules.ClearDeparture(null, "city").Should().BeEmpty();
        ClerkRecoveryRules.RecordDeparture(null, " ", Now).Should().BeEmpty("a blank city id records nothing");
    }

    [Test]
    public void SendingAnAbsentOwnersAnimalsAway_IsAMayorOnlyPower()
    {
        ClerkRecoveryRules.SendAwayPower.Should().Be(CityPower.GrantPermissions);

        CityPowerRules.RoleHoldsPower(CityRole.Mayor, ClerkRecoveryRules.SendAwayPower).Should().BeTrue();
        foreach (var role in new[] { CityRole.ActingMayor, CityRole.Officer, CityRole.DepartedMayor, CityRole.None })
            CityPowerRules.RoleHoldsPower(role, ClerkRecoveryRules.SendAwayPower).Should().BeFalse(role.ToString());

        CityPowerRules.GetPermissionFor(ClerkRecoveryRules.SendAwayPower).Should().BeNull("no permission a citizen can be granted gives it");
    }

    #endregion

    #region Marks and overflow records

    [Test]
    public void AHarvestMark_CarriesTheItemsAndTheSeedWhenItCameBack()
    {
        var withSeed = ClerkRecoveryRules.BuildHarvestMark("m", "planter", Now, "tomato", 5, "tomato_seed", true);
        var withoutSeed = ClerkRecoveryRules.BuildHarvestMark("m", "planter", Now, "tomato", 5, "tomato_seed", false);

        withSeed.Action.Should().Be(ClerkRecoveryAction.Harvest);
        withSeed.PlayerId.Should().Be("planter");
        withSeed.AtUtc.Should().Be(Now);
        withSeed.Items.Should().Equal(new Dictionary<string, int> { ["tomato"] = 5, ["tomato_seed"] = 1 });
        withoutSeed.Items.Should().Equal(new Dictionary<string, int> { ["tomato"] = 5 });
    }

    [Test]
    public void AProductMark_MergesRepeatedItemsAndSkipsEmptyOnes()
    {
        var mark = ClerkRecoveryRules.BuildProductMark("m", ClerkRecoveryAction.Butcher, "owner", Now, new[]
        {
            ("meat", 4),
            ("meat", 2),
            ("innards", 1),
            ("none", 0),
            (" ", 3)
        });

        mark.Action.Should().Be(ClerkRecoveryAction.Butcher);
        mark.Items.Should().Equal(new Dictionary<string, int> { ["meat"] = 6, ["innards"] = 1 });
    }

    [Test]
    public void ACrateMark_CarriesTheAnimalAsItStood()
    {
        var crate = LivestockRules.Pack(AnimalOf(Now.AddHours(-30), feedings: 2, fedHoursAfterPlaced: 20), Now);

        var mark = ClerkRecoveryRules.BuildCrateMark("m", "owner", Now, crate);

        mark.Action.Should().Be(ClerkRecoveryAction.Crate);
        mark.Crate.Should().BeSameAs(crate);
        mark.Items.Should().BeEmpty();
    }

    [Test]
    public void TheItemsOfAMark_AreNumberedByResref_NotByTheOrderTheyWereAdded()
    {
        var first = ClerkRecoveryRules.BuildProductMark("m", ClerkRecoveryAction.Collect, "owner", Now, new[] { ("b", 1), ("a", 2) });
        var second = ClerkRecoveryRules.BuildProductMark("m", ClerkRecoveryAction.Collect, "owner", Now, new[] { ("a", 2), ("b", 1) });

        ClerkRecoveryRules.GetItems(first).Select(pair => pair.Key).Should().Equal("a", "b");
        ClerkRecoveryRules.GetItems(second).Select(pair => pair.Key).Should().Equal("a", "b");
    }

    [Test]
    public void TheOverflowRecordIds_NeverChange_AndNeverCollide()
    {
        var prefix = ClerkRecoveryRules.GetOverflowRecordPrefix("mark", 0);

        ClerkRecoveryRules.GetOverflowRecordPrefix("mark", 0).Should().Be(prefix);
        ClerkRecoveryRules.GetOverflowRecordPrefix("mark", 1).Should().NotBe(prefix);
        ClerkRecoveryRules.GetOverflowRecordPrefix("other", 0).Should().NotBe(prefix);
        ClerkRecoveryRules.GetOverflowRecordId(prefix, 0).Should().Be(ClerkRecoveryRules.GetOverflowRecordId(prefix, 0));
        ClerkRecoveryRules.GetOverflowRecordId(prefix, 1).Should().NotBe(ClerkRecoveryRules.GetOverflowRecordId(prefix, 0));
    }

    #endregion

    #region Text

    [Test]
    public void TheClerksText_IsPlainAscii()
    {
        var crop = CropOf(Now.AddHours(-13), 12);
        var animal = AnimalOf(Now.AddHours(-80), feedings: 4, fedHoursAfterPlaced: 70);
        var texts = new[]
        {
            ClerkRecoveryRules.RecoverResponse,
            ClerkRecoveryRules.ListHeader,
            ClerkRecoveryRules.NothingHeader,
            ClerkRecoveryRules.NotOffered,
            ClerkRecoveryRules.CanReachCropText,
            ClerkRecoveryRules.CanReachAnimalText,
            ClerkRecoveryRules.NoCratesText,
            ClerkRecoveryRules.SendAwayNoPower,
            ClerkRecoveryRules.SendAwayNone,
            ClerkRecoveryRules.DescribeCropTitle("Tomato", GardenPlotStage.Ripe),
            ClerkRecoveryRules.DescribeCrop("Tomato", "the city's gardens", crop, Now),
            ClerkRecoveryRules.DescribeAnimalTitle("Warocas", animal, Now),
            ClerkRecoveryRules.DescribeAnimal("Warocas", animal, Now),
            ClerkRecoveryRules.DescribeRemoteHarvest("Tomato", 5, true),
            ClerkRecoveryRules.DescribeRemoteProducts("collected from", "Warocas", new[] { ("Manure", 1) }),
            ClerkRecoveryRules.DescribeRemoteCrated("Warocas"),
            ClerkRecoveryRules.DescribeSentAway("Warocas", "Port Nova"),
            ClerkRecoveryRules.DescribeSentAwayResult(2, "Port Nova")
        };

        foreach (var text in texts)
            text.Should().MatchRegex("^[\\x20-\\x7E\\n]+$");
    }

    [Test]
    public void ARemoteRecovery_SaysItWentToOverflowStorage_AndNeverThatItDidNotFit()
    {
        ClerkRecoveryRules.DescribeRemoteHarvest("Tomato", 4, false).Should().Contain("overflow storage").And.Contain("/overflow").And.NotContain("did not fit");
        ClerkRecoveryRules.DescribeRemoteHarvest("Tomato", 4, true).Should().Contain("and its seed");
        ClerkRecoveryRules.DescribeRemoteProducts("butchered", "Warocas", new[] { ("Meat", 4) }).Should().Contain("4 Meat").And.Contain("overflow storage");
        ClerkRecoveryRules.DescribeRemoteCrated("Warocas").Should().Contain("overflow storage");
    }

    [Test]
    public void TheCropText_SaysWhenItRipensOrWithers()
    {
        ClerkRecoveryRules.DescribeCrop("Tomato", "the city's gardens", CropOf(Now.AddHours(-2), 12), Now).Should().Contain("ripens in");
        ClerkRecoveryRules.DescribeCrop("Tomato", string.Empty, CropOf(Now.AddHours(-13), 12), Now).Should().Contain("ripe and withers in");
    }

    #endregion
}
