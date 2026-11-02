using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.FarmingDefinition;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Player City Revamp, "Farming: Livestock": feeding, growing up, wandering off, collecting, butchering, the caps, crating and the
/// predators' penalty.
/// </summary>
public class LivestockRulesTests
{
    private static readonly DateTime Placed = new(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc);

    private static readonly Dictionary<LivestockType, LivestockDefinition> Animals = new LivestockListDefinition().BuildLivestock();

    private static Livestock AnimalOf(
        LivestockType type = LivestockType.Warocas,
        int feedings = 0,
        double? fedHoursAfterPlaced = null,
        double? collectedHoursAfterPlaced = null,
        double? penaltyHoursAfterPlaced = null,
        string owner = "owner")
    {
        return new Livestock
        {
            OwnerPlayerId = owner,
            OwnerCdKey = "KEY",
            CorralId = "corral",
            Species = type,
            PlacedUtc = Placed,
            Feedings = feedings,
            LastFedUtc = fedHoursAfterPlaced is { } fed ? Placed.AddHours(fed) : null,
            LastCollectedUtc = collectedHoursAfterPlaced is { } collected ? Placed.AddHours(collected) : null,
            NoProductsUntilUtc = penaltyHoursAfterPlaced is { } penalty ? Placed.AddHours(penalty) : null,
            LastProcessedUtc = Placed,
        };
    }

    // Tiers

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(3, 3)]
    [TestCase(5, 5)]
    [TestCase(9, 5)]
    public void CropManagementOpensAnimalTiersAsItOpensCropTiers(int rank, int highest)
    {
        LivestockRules.GetHighestTier(rank).Should().Be(highest);
        for (var tier = 1; tier <= 5; tier++)
            LivestockRules.CanRaiseTier(rank, tier).Should().Be(tier <= highest);
    }

    [TestCase(3, 1, true)]
    [TestCase(3, 3, true)]
    [TestCase(3, 4, false)]
    [TestCase(1, 2, false)]
    [TestCase(5, 5, true)]
    [TestCase(3, 0, false)]
    public void AnAnimalEatsCropsOfItsOwnTierOrLower(int animalTier, int cropTier, bool eats)
    {
        LivestockRules.CanEat(animalTier, cropTier).Should().Be(eats);
    }

    [TestCase(1, "manure_t1")]
    [TestCase(5, "manure_t5")]
    public void ManureIsOfTheAnimalsTier(int tier, string resref)
    {
        LivestockRules.GetManureResref(tier).Should().Be(resref);
    }

    // Feeding

    [Test]
    public void AnAnimalCanBeFedOnceEveryTwentyHours()
    {
        var animal = AnimalOf(feedings: 1, fedHoursAfterPlaced: 4);

        LivestockRules.CanFeedAt(animal, Placed.AddHours(23.99)).Should().BeFalse();
        LivestockRules.CanFeedAt(animal, Placed.AddHours(24)).Should().BeTrue();
        LivestockRules.GetNextFeedUtc(animal).Should().Be(Placed.AddHours(24));
        LivestockRules.CanFeedAt(AnimalOf(), Placed).Should().BeTrue("an animal never fed can eat at once");
        LivestockRules.GetNextFeedUtc(AnimalOf()).Should().BeNull();
    }

    [Test]
    public void FeedingCountsTheMealAndRemembersWhenItWas()
    {
        var animal = AnimalOf(feedings: 1, fedHoursAfterPlaced: 4);

        LivestockRules.ApplyFeeding(animal, Placed.AddHours(30));

        animal.Feedings.Should().Be(2);
        animal.LastFedUtc.Should().Be(Placed.AddHours(30));
    }

    [Test]
    public void CheckFeedGoesThroughItsRefusalsInOrder()
    {
        var animal = AnimalOf(feedings: 1, fedHoursAfterPlaced: 4);
        var now = Placed.AddHours(5);

        LivestockRules.CheckFeed(false, animal, now, true, 3, 2, 4).Should().Be(LivestockRefusal.NotAPlayer);
        LivestockRules.CheckFeed(true, null, now, true, 3, 2, 4).Should().Be(LivestockRefusal.NoAnimal);
        LivestockRules.CheckFeed(true, animal, now, false, 3, 2, 4).Should().Be(LivestockRefusal.NotTheOwner);
        LivestockRules.CheckFeed(true, animal, now, true, 3, 4, 1).Should().Be(LivestockRefusal.FeedTooSoon, "too soon comes before the crop checks");

        now = Placed.AddHours(30);
        LivestockRules.CheckFeed(true, animal, now, true, 3, 4, 1).Should().Be(LivestockRefusal.CropTierTooHigh);
        LivestockRules.CheckFeed(true, animal, now, true, 3, 2, 1).Should().Be(LivestockRefusal.NotEnoughCrops);
        LivestockRules.CheckFeed(true, animal, now, true, 3, 2, 2).Should().Be(LivestockRefusal.None);
    }

    // Growing up

    [Test]
    public void AYoungAnimalGrowsUpSeventyTwoHoursAfterPlacingIfItWasFedThreeTimesByThen()
    {
        var animal = AnimalOf(feedings: 3, fedHoursAfterPlaced: 60);

        LivestockRules.IsAdult(animal, Placed.AddHours(71.99)).Should().BeFalse();
        LivestockRules.IsAdult(animal, Placed.AddHours(72)).Should().BeTrue();
        LivestockRules.GetStage(animal, Placed.AddHours(72)).Should().Be(LivestockStage.Adult);
        LivestockRules.GetStage(animal, Placed.AddHours(10)).Should().Be(LivestockStage.Young);
    }

    [Test]
    public void ThreeFeedingsAreNotEnoughBeforeSeventyTwoHours_AndSeventyTwoHoursAreNotEnoughWithTwo()
    {
        var thirdMealEarly = AnimalOf(feedings: 3, fedHoursAfterPlaced: 40);
        var onlyTwo = AnimalOf(feedings: 2, fedHoursAfterPlaced: 60);

        LivestockRules.IsAdult(thirdMealEarly, Placed.AddHours(50)).Should().BeFalse("the third meal before 72 hours waits for the 72 hours");
        LivestockRules.IsAdult(onlyTwo, Placed.AddHours(100)).Should().BeFalse();
    }

    [Test]
    public void AnAnimalFedItsThirdTimeAfterSeventyTwoHoursGrowsUpAtThatFeeding()
    {
        var animal = AnimalOf(feedings: 2, fedHoursAfterPlaced: 60);
        var thirdMeal = Placed.AddHours(80);

        LivestockRules.IsAdult(animal, thirdMeal).Should().BeFalse();
        LivestockRules.ApplyFeeding(animal, thirdMeal);

        LivestockRules.IsAdult(animal, thirdMeal).Should().BeTrue();
    }

    [Test]
    public void GrowthIsDescribedForAYoungAnimalOnly()
    {
        LivestockRules.DescribeGrowth(AnimalOf(), Placed.AddHours(1)).Should().Contain("3 more feedings");
        LivestockRules.DescribeGrowth(AnimalOf(feedings: 3, fedHoursAfterPlaced: 60), Placed.AddHours(70)).Should().Be("Grows up in 2h 0m.");
        LivestockRules.DescribeGrowth(AnimalOf(feedings: 2, fedHoursAfterPlaced: 60), Placed.AddHours(80)).Should().Be("Grows up at its next 1 feeding.");
        LivestockRules.DescribeGrowth(AnimalOf(feedings: 3, fedHoursAfterPlaced: 60), Placed.AddHours(80)).Should().BeEmpty();
    }

    // Wandering off

    [Test]
    public void AnAnimalNeverFedWandersOffSeventyTwoHoursAfterPlacing()
    {
        var animal = AnimalOf();

        LivestockRules.GetWanderOffUtc(animal).Should().Be(Placed.AddHours(72));
        LivestockRules.HasWanderedOff(animal, Placed.AddHours(71.99)).Should().BeFalse();
        LivestockRules.HasWanderedOff(animal, Placed.AddHours(72)).Should().BeTrue();
    }

    [Test]
    public void AnAnimalWandersOffSeventyTwoHoursAfterItsLastMeal()
    {
        var animal = AnimalOf(feedings: 2, fedHoursAfterPlaced: 40);

        LivestockRules.GetWanderOffUtc(animal).Should().Be(Placed.AddHours(112));
        LivestockRules.HasWanderedOff(animal, Placed.AddHours(111)).Should().BeFalse();
        LivestockRules.HasWanderedOff(animal, Placed.AddHours(112)).Should().BeTrue();
    }

    [Test]
    public void AnAnimalThatWanderedOffRefusesEveryAction()
    {
        var animal = AnimalOf(feedings: 3, fedHoursAfterPlaced: 10);
        var now = Placed.AddHours(200);

        LivestockRules.CheckFeed(true, animal, now, true, 1, 1, 9).Should().Be(LivestockRefusal.NoAnimal);
        LivestockRules.CheckCollect(true, animal, now, true).Should().Be(LivestockRefusal.NoAnimal);
        LivestockRules.CheckButcher(true, animal, now, true).Should().Be(LivestockRefusal.NoAnimal);
        LivestockRules.CheckCrate(true, animal, now, true).Should().Be(LivestockRefusal.NoAnimal);
    }

    // Collecting

    [Test]
    public void CollectingNeedsAnAdultFedInTheLastTwentyFourHours()
    {
        var animal = AnimalOf(feedings: 3, fedHoursAfterPlaced: 60);

        LivestockRules.CheckCollect(true, animal, Placed.AddHours(61), true).Should().Be(LivestockRefusal.StillYoung);
        LivestockRules.CheckCollect(true, animal, Placed.AddHours(72), true).Should().Be(LivestockRefusal.None);
        LivestockRules.CheckCollect(true, animal, Placed.AddHours(84), true).Should().Be(LivestockRefusal.None, "24 hours after the meal still counts");
        LivestockRules.CheckCollect(true, animal, Placed.AddHours(84.01), true).Should().Be(LivestockRefusal.NotFedRecently);
    }

    [Test]
    public void CollectingIsOncePerTwentyHours()
    {
        var animal = AnimalOf(feedings: 3, fedHoursAfterPlaced: 70, collectedHoursAfterPlaced: 72);

        LivestockRules.CheckCollect(true, animal, Placed.AddHours(91.9), true).Should().Be(LivestockRefusal.CollectTooSoon);
        LivestockRules.CheckCollect(true, animal, Placed.AddHours(92), true).Should().Be(LivestockRefusal.None);
        LivestockRules.GetNextCollectUtc(animal).Should().Be(Placed.AddHours(92));
    }

    [Test]
    public void CollectingIsBlockedWhileThePredatorsPenaltyLasts()
    {
        var animal = AnimalOf(feedings: 3, fedHoursAfterPlaced: 80, penaltyHoursAfterPlaced: 90);

        LivestockRules.CheckCollect(true, animal, Placed.AddHours(85), true).Should().Be(LivestockRefusal.PredatorPenalty);
        LivestockRules.CheckCollect(true, animal, Placed.AddHours(90), true).Should().Be(LivestockRefusal.None, "the penalty ends at its time");
    }

    [Test]
    public void OnlyTheOwnerCollectsButchersOrCrates()
    {
        var animal = AnimalOf(feedings: 3, fedHoursAfterPlaced: 70);
        var now = Placed.AddHours(75);

        LivestockRules.CheckCollect(true, animal, now, false).Should().Be(LivestockRefusal.NotTheOwner);
        LivestockRules.CheckButcher(true, animal, now, false).Should().Be(LivestockRefusal.NotTheOwner);
        LivestockRules.CheckCrate(true, animal, now, false).Should().Be(LivestockRefusal.NotTheOwner);
        LivestockRules.CheckCollect(false, animal, now, true).Should().Be(LivestockRefusal.NotAPlayer);
    }

    [Test]
    public void ACollectionYieldsOneManureOfTheAnimalsTier_AndOneMilkFromAThune()
    {
        LivestockRules.GetCollectItems(Animals[LivestockType.Warocas]).Should().Equal(("manure_t1", 1));
        LivestockRules.GetCollectItems(Animals[LivestockType.Gimpassa]).Should().Equal(("manure_t2", 1));
        LivestockRules.GetCollectItems(Animals[LivestockType.Womprat]).Should().Equal(("manure_t4", 1));
        LivestockRules.GetCollectItems(Animals[LivestockType.Thune]).Should().Equal(("manure_t5", 1), ("milk", 1));
        LivestockRules.CountItems(LivestockRules.GetCollectItems(Animals[LivestockType.Thune])).Should().Be(2);
    }

    // Butchering

    [Test]
    public void OnlyAnAdultCanBeButchered()
    {
        var animal = AnimalOf(feedings: 3, fedHoursAfterPlaced: 60);

        LivestockRules.CheckButcher(true, animal, Placed.AddHours(70), true).Should().Be(LivestockRefusal.StillYoung);
        LivestockRules.CheckButcher(true, animal, Placed.AddHours(72), true).Should().Be(LivestockRefusal.None);
        LivestockRules.CheckButcher(true, animal, Placed.AddHours(500), true).Should().Be(LivestockRefusal.NoAnimal, "it wandered off long before");
    }

    [TestCase(0, 4)]
    [TestCase(1, 5)]
    [TestCase(2, 6)]
    [TestCase(3, 7)]
    [TestCase(9, 7)]
    [TestCase(-1, 4)]
    public void ButcheringYieldsFourMeatPlusBountifulHarvest(int rank, int meat)
    {
        LivestockRules.GetButcherItems(Animals[LivestockType.Gimpassa], rank).Should().Equal(("gimp_meat", meat));
    }

    [Test]
    public void ButcheringAddsOneSecondProductWhereTheTableListsOne()
    {
        LivestockRules.GetButcherItems(Animals[LivestockType.Warocas], 0).Should().Equal(("warocas_meat", 4), ("waro_leg", 1));
        LivestockRules.GetButcherItems(Animals[LivestockType.Womprat], 3).Should().Equal(("wompratmeat", 7), ("womp_innards", 1));
        LivestockRules.GetButcherItems(Animals[LivestockType.Thune], 0).Should().Equal(("thune_meat", 4), ("milk", 1));

        foreach (var type in new[] { LivestockType.ValleyCairnmog, LivestockType.Gimpassa, LivestockType.Klorslug, LivestockType.Aradile })
            LivestockRules.GetButcherItems(Animals[type], 0).Should().HaveCount(1, type.ToString());
    }

    [Test]
    public void ProductsPayAgricultureXpByTheSameFormulaAsCrops()
    {
        foreach (var definition in Animals.Values)
        {
            LivestockRules.GetProductXp(definition.Level, 1, 4).Should().Be(FarmingRules.GetHarvestXp(definition.Level, 1, 4), definition.Name);
            LivestockRules.GetProductXp(definition.Level, 1, 0).Should().Be(0);
        }

        LivestockRules.GetProductXp(25, 25, 4).Should().Be(4 * FarmingRules.GetXpPerItem(25, 25)).And.BeGreaterThan(0);
        LivestockRules.GetProductXp(25, 30, 4).Should().Be(0, "an animal five ranks below the farmer pays nothing, as a crop does");
    }

    // Caps

    [Test]
    public void PlacingGoesThroughItsRefusalsInOrder()
    {
        LivestockRules.CheckPlace(false, 0, true, 5, 1, 0, 0, 1).Should().Be(LivestockRefusal.NotAPlayer);
        LivestockRules.CheckPlace(true, -1, true, 5, 1, 0, 0, 1).Should().Be(LivestockRefusal.NoCorral);
        LivestockRules.CheckPlace(true, 0, false, 5, 1, 0, 0, 1).Should().Be(LivestockRefusal.NoAccess);
        LivestockRules.CheckPlace(true, 0, true, 2, 3, 6, 3, 1).Should().Be(LivestockRefusal.TierLocked);
        LivestockRules.CheckPlace(true, 0, true, 3, 3, 6, 3, 1).Should().Be(LivestockRefusal.CdKeyCap);
        LivestockRules.CheckPlace(true, 2, true, 3, 3, 5, 3, 1).Should().Be(LivestockRefusal.CorralOverLimit);
        LivestockRules.CheckPlace(true, 1, true, 3, 3, 5, 3, 1).Should().Be(LivestockRefusal.CorralFull);
        LivestockRules.CheckPlace(true, 1, true, 3, 3, 5, 2, 1).Should().Be(LivestockRefusal.None);
    }

    [Test]
    public void ACdKeyRaisesAtMostSixAnimals_AndACorralHoldsThree()
    {
        LivestockRules.MaxAnimalsPerCdKey.Should().Be(6);
        LivestockRules.AnimalsPerCorral.Should().Be(3);
        LivestockRules.CheckPlace(true, 0, true, 5, 1, 5, 0, 1).Should().Be(LivestockRefusal.None);
        LivestockRules.CheckPlace(true, 0, true, 5, 1, 6, 0, 1).Should().Be(LivestockRefusal.CdKeyCap);
        LivestockRules.CheckPlace(true, 0, true, 5, 1, 0, 2, 1).Should().Be(LivestockRefusal.None);
        LivestockRules.CheckPlace(true, 0, true, 5, 1, 0, 3, 1).Should().Be(LivestockRefusal.CorralFull);
    }

    [TestCase(1, 2)]
    [TestCase(2, 4)]
    [TestCase(3, 6)]
    [TestCase(5, 10)]
    [TestCase(0, 2)]
    public void ACityHasTwoCorralsForEachCityLevel_SoThirtyAnimalsAtMost(int level, int corrals)
    {
        LivestockRules.GetMaxCorrals(level).Should().Be(corrals);
    }

    [Test]
    public void ACityAtLevelFiveHoldsThirtyAnimals()
    {
        (LivestockRules.GetMaxCorrals(5) * LivestockRules.AnimalsPerCorral).Should().Be(30);
    }

    [Test]
    public void ACityThatDropsALevelStopsTakingAnimalsInItsNewestCorrals_AndTheAnimalsThereStay()
    {
        // The corrals are counted in the order they were placed: at level 2 the first four take animals.
        for (var index = 0; index < 4; index++)
            LivestockRules.IsCorralOpen(index, 2).Should().BeTrue();
        for (var index = 4; index < 6; index++)
            LivestockRules.IsCorralOpen(index, 2).Should().BeFalse();
        LivestockRules.IsCorralOpen(-1, 2).Should().BeFalse();

        // A closed corral refuses a new animal but not the tending of the ones in it.
        var animal = AnimalOf(feedings: 3, fedHoursAfterPlaced: 70);
        LivestockRules.CheckCollect(true, animal, Placed.AddHours(75), true).Should().Be(LivestockRefusal.None);
        LivestockRules.CheckButcher(true, animal, Placed.AddHours(75), true).Should().Be(LivestockRefusal.None);
        LivestockRules.CheckCrate(true, animal, Placed.AddHours(75), true).Should().Be(LivestockRefusal.None);
    }

    // The Corral structure

    [Test]
    public void ACorralStandsOnAPlanetCitysOpenGroundOnly()
    {
        LivestockRules.GetPlacementError(StructureType.Corral, false, 0, 1).Should().BeEmpty();
        LivestockRules.GetPlacementError(StructureType.Corral, false, 1, 1).Should().BeEmpty();
        LivestockRules.GetPlacementError(StructureType.Corral, false, 2, 1).Should().Contain("2 corrals");
        LivestockRules.GetPlacementError(StructureType.Corral, false, 9, 5).Should().BeEmpty();
        LivestockRules.GetPlacementError(StructureType.Corral, false, 10, 5).Should().Contain("10 corrals");
        LivestockRules.GetPlacementError(StructureType.Corral, true, 0, 5).Should().Contain("station deck");
        LivestockRules.GetPlacementError(StructureType.GardenPlot, true, 99, 1).Should().BeEmpty("only a Corral is checked");
    }

    [Test]
    public void ACorralCountsAgainstTheStructureLimitNotTheBuildingLimit()
    {
        LivestockRules.CountsAsStructure(StructureType.Corral).Should().BeTrue();
        LivestockRules.CountsAsStructure(StructureType.CityHall).Should().BeFalse();
        LivestockRules.CountsAsStructure(StructureType.DuelingRing).Should().BeFalse("the ring's limit is not this slice's to change");
    }

    [Test]
    public void ACorralCannotBePickedUpWhileItHoldsAnotherPlayersAnimals()
    {
        LivestockRules.GetRetrieveError(new[] { "mine", "mine" }, "mine").Should().BeEmpty();
        LivestockRules.GetRetrieveError(Array.Empty<string>(), "mine").Should().BeEmpty();
        LivestockRules.GetRetrieveError(new[] { "mine", "theirs" }, "mine").Should().Contain("Another player's animals");
        LivestockRules.GetRetrieveError(new[] { "theirs" }, "mine").Should().NotBeEmpty();
    }

    // Crating

    [Test]
    public void ACrateStoresEveryTimerAsTheTimeLeftOnIt()
    {
        var animal = AnimalOf(feedings: 3, fedHoursAfterPlaced: 70, collectedHoursAfterPlaced: 80, penaltyHoursAfterPlaced: 100);
        var now = Placed.AddHours(90);

        var crate = LivestockRules.Pack(animal, now);

        crate.Species.Should().Be(LivestockType.Warocas);
        crate.Feedings.Should().Be(3);
        crate.MaturitySeconds.Should().Be(0, "it is more than 72 hours old");
        crate.HungerSeconds.Should().Be((int)TimeSpan.FromHours(52).TotalSeconds, "72 hours after the meal at 70 hours is 142, and it is 90");
        crate.CollectSeconds.Should().Be((int)TimeSpan.FromHours(10).TotalSeconds, "collected at 80, so again at 100");
        crate.PenaltySeconds.Should().Be((int)TimeSpan.FromHours(10).TotalSeconds);
    }

    [Test]
    public void ACratedAnimalResumesWhereItStopped_WhateverTheDelay()
    {
        var animal = AnimalOf(feedings: 2, fedHoursAfterPlaced: 30, collectedHoursAfterPlaced: null, penaltyHoursAfterPlaced: 60);
        var crated = Placed.AddHours(40);
        var crate = LivestockRules.Pack(animal, crated);

        // A week later it is placed again by someone else.
        var again = crated.AddDays(7);
        var placed = LivestockRules.Unpack(crate, again, "buyer", "KEY2", "other-corral");

        placed.OwnerPlayerId.Should().Be("buyer");
        placed.OwnerCdKey.Should().Be("KEY2");
        placed.CorralId.Should().Be("other-corral");
        placed.Species.Should().Be(LivestockType.Warocas);
        placed.Feedings.Should().Be(2);
        LivestockRules.GetMaturityUtc(placed).Should().Be(again.Add(LivestockRules.GetMaturityUtc(animal) - crated));
        LivestockRules.GetWanderOffUtc(placed).Should().Be(again.Add(LivestockRules.GetWanderOffUtc(animal) - crated));
        placed.NoProductsUntilUtc.Should().Be(again.Add(animal.NoProductsUntilUtc!.Value - crated));
        placed.LastProcessedUtc.Should().Be(again);
        LivestockRules.GetNextFeedUtc(placed).Should().Be(again.Add(LivestockRules.GetNextFeedUtc(animal)!.Value - crated));
        LivestockRules.IsFedRecently(placed, again).Should().Be(LivestockRules.IsFedRecently(animal, crated));
    }

    [Test]
    public void CratingDoesNotClearAPenalty_AndDoesNotPassTimeForAnAnimal()
    {
        var animal = AnimalOf(feedings: 3, fedHoursAfterPlaced: 80, penaltyHoursAfterPlaced: 120);
        var crate = LivestockRules.Pack(animal, Placed.AddHours(90));

        var placed = LivestockRules.Unpack(crate, Placed.AddDays(30), "owner", "KEY", "corral");

        LivestockRules.HasPenalty(placed, Placed.AddDays(30)).Should().BeTrue("a crate keeps the penalty");
        LivestockRules.HasPenalty(placed, Placed.AddDays(30).AddHours(29.9)).Should().BeTrue();
        LivestockRules.HasPenalty(placed, Placed.AddDays(30).AddHours(30)).Should().BeFalse();
        LivestockRules.HasWanderedOff(placed, Placed.AddDays(30)).Should().BeFalse("it did not go hungry in the crate");
        LivestockRules.HasWanderedOff(placed, Placed.AddDays(30).AddHours(62)).Should().BeTrue("72 hours after its last meal, counting only the time out of the crate");
    }

    [Test]
    public void ANeverFedAnimalKeepsItsPlacementClock_AndAMealPinsTheHungerTimer()
    {
        var unfed = LivestockRules.Unpack(LivestockRules.Pack(AnimalOf(), Placed.AddHours(10)), Placed.AddHours(500), "o", "k", "c");
        unfed.LastFedUtc.Should().BeNull();
        unfed.Feedings.Should().Be(0);
        LivestockRules.GetWanderOffUtc(unfed).Should().Be(Placed.AddHours(500 + 62));

        var fed = LivestockRules.Unpack(LivestockRules.Pack(AnimalOf(feedings: 1, fedHoursAfterPlaced: 8), Placed.AddHours(10)), Placed.AddHours(500), "o", "k", "c");
        fed.LastFedUtc.Should().Be(Placed.AddHours(498));
        LivestockRules.CanFeedAt(fed, Placed.AddHours(500)).Should().BeFalse("it was fed 2 hours of its own time ago");
        LivestockRules.CanFeedAt(fed, Placed.AddHours(518)).Should().BeTrue();
    }

    [Test]
    public void ACrateRoundTripsThroughTheItemsLocals()
    {
        var animal = AnimalOf(LivestockType.Thune, feedings: 3, fedHoursAfterPlaced: 70, collectedHoursAfterPlaced: 80, penaltyHoursAfterPlaced: 100);
        var crate = LivestockRules.Pack(animal, Placed.AddHours(90));
        var locals = crate.ToLocals();

        locals.Keys.Should().BeEquivalentTo(new[]
        {
            LivestockCrate.SpeciesLocal, LivestockCrate.FeedingsLocal, LivestockCrate.MaturitySecondsLocal,
            LivestockCrate.HungerSecondsLocal, LivestockCrate.CollectSecondsLocal, LivestockCrate.PenaltySecondsLocal
        });
        locals.Keys.Should().OnlyContain(name => name.Length <= 32);

        LivestockCrate.TryFromLocals(name => locals.GetValueOrDefault(name), out var read).Should().BeTrue();
        read.Should().BeEquivalentTo(crate);
    }

    [Test]
    public void AnItemThatHoldsNoAnimalIsNotACrate()
    {
        LivestockCrate.TryFromLocals(_ => 0, out _).Should().BeFalse();
        LivestockCrate.TryFromLocals(name => name == LivestockCrate.SpeciesLocal ? 99 : 5, out _).Should().BeFalse();
    }

    [Test]
    public void ACratedAnimalDoesNotCountTowardTheCdKeysSix_BecauseItHasNoRecord()
    {
        // A crate is an item; only Livestock records are counted. Placing it needs room under the six like any placing.
        LivestockRules.CheckPlace(true, 0, true, 5, 1, 6, 0, 1).Should().Be(LivestockRefusal.CdKeyCap);
        LivestockRules.CheckPlace(true, 0, true, 5, 1, 5, 0, 1).Should().Be(LivestockRefusal.None);
    }

    [Test]
    public void AYoungAnimalIsPlacedWithNothingFedCollectedOrPenalized()
    {
        var animal = LivestockRules.PlaceYoung(LivestockType.Aradile, Placed, "o", "k", "c");

        animal.Feedings.Should().Be(0);
        animal.LastFedUtc.Should().BeNull();
        animal.LastCollectedUtc.Should().BeNull();
        animal.NoProductsUntilUtc.Should().BeNull();
        animal.PlacedUtc.Should().Be(Placed);
        animal.LastProcessedUtc.Should().Be(Placed);
    }

    // The predators

    [Test]
    public void ThePenaltyLastsSeventyTwoHours_AndARepeatRestartsItInsteadOfStacking()
    {
        var animal = AnimalOf(feedings: 3, fedHoursAfterPlaced: 80);

        LivestockRules.ApplyPenalty(animal, Placed.AddHours(80));
        animal.NoProductsUntilUtc.Should().Be(Placed.AddHours(152));

        LivestockRules.ApplyPenalty(animal, Placed.AddHours(100));
        animal.NoProductsUntilUtc.Should().Be(Placed.AddHours(172), "restarted from the second raid, not stacked");
        LivestockRules.GetPenaltyEndUtc(Placed).Should().Be(Placed.AddHours(72));
    }

    [Test]
    public void AHuntLastsThirtyMinutes_WithThreePredatorsAtEachCorralUpToTwelve()
    {
        LivestockRules.GetHuntEndUtc(Placed).Should().Be(Placed.AddMinutes(30));
        LivestockRules.GetHuntWaveSize(0).Should().Be(0);
        LivestockRules.GetHuntWaveSize(1).Should().Be(3);
        LivestockRules.GetHuntWaveSize(3).Should().Be(9);
        LivestockRules.GetHuntWaveSize(4).Should().Be(12);
        LivestockRules.GetHuntWaveSize(30).Should().Be(12);
    }

    [TestCase(1, "warocas")]
    [TestCase(2, "valley_cairnmog")]
    [TestCase(3, "korr_klorslug")]
    [TestCase(4, "womprat")]
    [TestCase(5, "pthune")]
    [TestCase(0, "warocas")]
    [TestCase(9, "pthune")]
    public void TheWaveIsWildKinOfTheCityLevelsTier(int cityLevel, string resref)
    {
        LivestockRules.GetPredatorResref(cityLevel).Should().Be(resref);
    }

    [Test]
    public void AHuntIsWonWhenEveryPredatorItSpawnedIsDown()
    {
        LivestockRules.IsHuntWon(6, 0).Should().BeTrue();
        LivestockRules.IsHuntWon(6, 1).Should().BeFalse();
        LivestockRules.IsHuntWon(0, 0).Should().BeFalse("a hunt that spawned nothing is not won");
    }

    // Text

    [Test]
    public void EveryRefusalHasAMessage_InPlainAscii()
    {
        foreach (var refusal in Enum.GetValues<LivestockRefusal>().Where(refusal => refusal != LivestockRefusal.None))
        {
            var text = LivestockRules.Describe(refusal);
            text.Should().NotBeNullOrWhiteSpace(refusal.ToString());
            text.Should().MatchRegex("^[\\x20-\\x7E]+$", refusal.ToString());
        }

        LivestockRules.Describe(LivestockRefusal.None).Should().BeEmpty();
        LivestockRules.Describe(LivestockRefusal.CdKeyCap).Should().Contain("6 animals");
        LivestockRules.Describe(LivestockRefusal.CorralFull).Should().Contain("3 animals");
    }

    [Test]
    public void ResultMessagesAreAscii()
    {
        var messages = new[]
        {
            LivestockRules.DescribeFed("Warocas", 2),
            LivestockRules.DescribePlaced("Warocas", false),
            LivestockRules.DescribePlaced("Warocas", true),
            LivestockRules.DescribeCrated("Warocas", false),
            LivestockRules.DescribeCrated("Warocas", true),
            LivestockRules.DescribeProducts("collected from", "Warocas", new[] { ("Manure", 1) }, 0),
            LivestockRules.DescribeProducts("butchered", "Thune", new[] { ("Thune Meat", 5), ("Milk", 1) }, 2),
            LivestockRules.DescribeHunger(AnimalOf(), Placed.AddHours(2)),
        };

        messages.Should().OnlyContain(text => System.Text.RegularExpressions.Regex.IsMatch(text, "^[\\x20-\\x7E]+$"));
        messages[6].Should().Contain("5 Thune Meat").And.Contain("2 did not fit").And.Contain("/overflow");
        messages[4].Should().Contain("/overflow");
    }

    [Test]
    public void RefusalsThatCarryATimeBuildTheirOwnMessage()
    {
        var result = LivestockResult.Refused(LivestockRefusal.CollectTooSoon, "in 3h 0m");

        result.Succeeded.Should().BeFalse();
        result.Refusal.Should().Be(LivestockRefusal.CollectTooSoon);
        result.Message.Should().Be("in 3h 0m");
        LivestockResult.Success("done").Succeeded.Should().BeTrue();
        LivestockResult.Refused(LivestockRefusal.StillYoung).Message.Should().Be("The animal is still young.");
    }
}
