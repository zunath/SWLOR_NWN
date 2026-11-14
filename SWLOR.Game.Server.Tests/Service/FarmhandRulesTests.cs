using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.FarmingDefinition;
using SWLOR.Game.Server.Service.FarmhandService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Player City Revamp, "Farmhands": the hoppers, which tiers a droid handles, and the catch-up replay that brings a house or
/// corral up to date. The replay lists every event that fell due since the last time, sorts them by time and applies them in
/// order against the hoppers.
/// </summary>
public class FarmhandRulesTests
{
    private const string Owner = "owner";

    private static readonly DateTime T0 = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Dictionary<CropType, CropDefinition> Crops = new CropListDefinition().BuildCrops();
    private static readonly Dictionary<LivestockType, LivestockDefinition> Animals = new LivestockListDefinition().BuildLivestock();

    private static CropDefinition GetCrop(CropType type) => Crops.GetValueOrDefault(type);

    private static LivestockDefinition GetAnimal(LivestockType type) => Animals.GetValueOrDefault(type);

    private static Farmhand Hand(int tier, FarmhandTargetType target = FarmhandTargetType.Corral, DateTime? processed = null) => new()
    {
        TargetId = "post",
        TargetType = target,
        OwnerPlayerId = Owner,
        DroidName = "Fennec",
        DroidTier = tier,
        LastProcessedUtc = processed ?? T0,
    };

    private static Livestock Animal(
        LivestockType type = LivestockType.Warocas,
        double placedHours = 0,
        int feedings = 0,
        double? fedHours = null,
        double? collectedHours = null,
        double? penaltyUntilHours = null,
        string owner = Owner,
        string id = null)
    {
        var animal = new Livestock
        {
            OwnerPlayerId = owner,
            CorralId = "post",
            Species = type,
            PlacedUtc = T0.AddHours(placedHours),
            Feedings = feedings,
            LastFedUtc = fedHours is { } fed ? T0.AddHours(fed) : null,
            LastCollectedUtc = collectedHours is { } collected ? T0.AddHours(collected) : null,
            NoProductsUntilUtc = penaltyUntilHours is { } penalty ? T0.AddHours(penalty) : null,
            LastProcessedUtc = T0.AddHours(Math.Min(placedHours, 0)),
        };
        if (id != null)
            animal.Id = id;
        return animal;
    }

    private static GardenPlot Plot(CropType crop, double plantedHours = 0, bool tended = false, string planter = Owner)
    {
        var definition = Crops[crop];
        var planted = T0.AddHours(plantedHours);
        var ripe = FarmingRules.GetRipeUtc(planted, definition.GrowHours);
        return new GardenPlot
        {
            PlotId = "plot-" + crop,
            PlanterPlayerId = planter,
            Crop = crop,
            PlantedUtc = planted,
            TendReadyUtc = FarmingRules.GetTendReadyUtc(planted, definition.GrowHours),
            RipeUtc = ripe,
            WitherUtc = FarmingRules.GetWitherUtc(ripe),
            Tended = tended,
            LastProcessedUtc = planted,
        };
    }

    private static FarmhandReplayResult Replay(Farmhand hand, IEnumerable<Livestock> animals, IEnumerable<GardenPlot> plots, double hoursLater) =>
        FarmhandRules.Replay(hand, animals, plots, GetAnimal, GetCrop, T0.AddHours(hoursLater));

    private static List<FarmEventType> Types(FarmhandReplayResult result) => result.Events.Select(e => e.Type).ToList();

    private static List<double> Hours(FarmhandReplayResult result, FarmEventType type) =>
        result.Events.Where(e => e.Type == type).Select(e => (e.TimeUtc - T0).TotalHours).ToList();

    // Hoppers

    [Test]
    public void EachHopperHoldsTwentyItems()
    {
        FarmhandRules.HopperCapacity.Should().Be(20);
        var feed = new Dictionary<CropType, int> { [CropType.ViscaranOrange] = 12, [CropType.Tomato] = 5 };
        var output = new Dictionary<string, int> { ["manure_t1"] = 20 };

        FarmhandRules.CountFeed(feed).Should().Be(17);
        FarmhandRules.GetFeedRoom(feed).Should().Be(3);
        FarmhandRules.GetOutputRoom(output).Should().Be(0);
        FarmhandRules.GetOutputRoom(new Dictionary<string, int>()).Should().Be(20);
    }

    [Test]
    public void LoadingFeedStopsAtTheHoppersRoom()
    {
        var feed = new Dictionary<CropType, int> { [CropType.Tomato] = 15 };

        FarmhandRules.LoadFeed(feed, CropType.ViscaranOrange, 9).Should().Be(5);
        feed[CropType.ViscaranOrange].Should().Be(5);
        FarmhandRules.LoadFeed(feed, CropType.ViscaranOrange, 1).Should().Be(0);
        FarmhandRules.LoadFeed(feed, CropType.ViscaranOrange, -3).Should().Be(0);
        FarmhandRules.CountFeed(feed).Should().Be(20);
    }

    [Test]
    public void OneFeedingTakesTwoCropsOfTheAnimalsTierOrLower_TheLowestTierFirst()
    {
        var feed = new Dictionary<CropType, int>
        {
            [CropType.Tomato] = 4,
            [CropType.ViscaranPeas] = 1,
            [CropType.ViscaranOrange] = 1,
            [CropType.HutlarAcorn] = 2,
        };

        FarmhandRules.TryTakeFeed(feed, 3, GetCrop).Should().BeTrue();

        feed.Should().NotContainKey(CropType.ViscaranOrange, "the lowest tier goes first");
        feed.Should().NotContainKey(CropType.ViscaranPeas, "one more tier-2 crop made up the meal");
        feed[CropType.HutlarAcorn].Should().Be(2, "a tier-3 crop was not needed");
        feed[CropType.Tomato].Should().Be(4, "a tier-5 crop is too high for the animal");
    }

    [Test]
    public void ACropOfAHigherTierIsNotFeedAndAFeedingNeedsTwo()
    {
        var onlyTomatoes = new Dictionary<CropType, int> { [CropType.Tomato] = 20 };
        var oneOrange = new Dictionary<CropType, int> { [CropType.ViscaranOrange] = 1 };

        FarmhandRules.HasFeed(onlyTomatoes, 4, GetCrop).Should().BeFalse();
        FarmhandRules.TryTakeFeed(onlyTomatoes, 4, GetCrop).Should().BeFalse();
        onlyTomatoes[CropType.Tomato].Should().Be(20, "nothing was taken");
        FarmhandRules.HasFeed(oneOrange, 1, GetCrop).Should().BeFalse();
        FarmhandRules.TryTakeFeed(oneOrange, 1, GetCrop).Should().BeFalse();
        FarmhandRules.HasFeed(onlyTomatoes, 5, GetCrop).Should().BeTrue();
    }

    [TestCase(1, 1, true)]
    [TestCase(2, 1, true)]
    [TestCase(2, 2, true)]
    [TestCase(2, 3, false)]
    [TestCase(5, 5, true)]
    [TestCase(3, 0, false)]
    public void ADroidHandlesCropsAndAnimalsUpToItsOwnTier(int droidTier, int tier, bool handles)
    {
        FarmhandRules.Handles(droidTier, tier).Should().Be(handles);
    }

    [Test]
    public void AssigningChecksItsRefusalsInOrder()
    {
        FarmhandRules.CheckAssign(false, true, true, false, true).Should().Be(FarmhandRefusal.NotAPlayer);
        FarmhandRules.CheckAssign(true, false, true, false, true).Should().Be(FarmhandRefusal.NoTarget);
        FarmhandRules.CheckAssign(true, true, false, false, true).Should().Be(FarmhandRefusal.NoAccess);
        FarmhandRules.CheckAssign(true, true, true, true, true).Should().Be(FarmhandRefusal.AlreadyAssigned);
        FarmhandRules.CheckAssign(true, true, true, false, false).Should().Be(FarmhandRefusal.NoDroid);
        FarmhandRules.CheckAssign(true, true, true, false, true).Should().Be(FarmhandRefusal.None);
    }

    [Test]
    public void EveryFarmhandRefusalHasAnAsciiMessage()
    {
        foreach (var refusal in Enum.GetValues<FarmhandRefusal>().Where(refusal => refusal != FarmhandRefusal.None))
            FarmhandRules.Describe(refusal).Should().NotBeNullOrWhiteSpace(refusal.ToString()).And.MatchRegex("^[\\x20-\\x7E]+$");
    }

    // A supplied corral left several days

    [Test]
    public void ASuppliedCorralLeftFourDays_FeedsGrowsAndCollectsInTimeOrder()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        var animal = Animal();

        var result = Replay(hand, new[] { animal }, Array.Empty<GardenPlot>(), 96);

        // Fed as soon as it could eat, then every 20 hours: 0, 20, 40, 60 and 80.
        Hours(result, FarmEventType.Fed).Should().Equal(0, 20, 40, 60, 80);
        // 72 hours after placing, with the 3 feedings it needed long before.
        Hours(result, FarmEventType.Matured).Should().Equal(72);
        // The first collection the moment it grew up, the next 20 hours later, while it was fed within the last 24.
        Hours(result, FarmEventType.Collected).Should().Equal(72, 92);

        animal.Feedings.Should().Be(5);
        animal.LastFedUtc.Should().Be(T0.AddHours(80));
        animal.LastCollectedUtc.Should().Be(T0.AddHours(92));
        LivestockRules.IsAdult(animal, T0.AddHours(96)).Should().BeTrue();
        hand.FeedHopper[CropType.ViscaranOrange].Should().Be(10, "five meals of two crops each");
        hand.OutputHopper.Should().Equal(new Dictionary<string, int> { ["manure_t1"] = 2 });
        hand.LastProcessedUtc.Should().Be(T0.AddHours(96));
        animal.LastProcessedUtc.Should().Be(T0.AddHours(96));
        result.WanderedOff.Should().BeEmpty();
        result.Animals.Should().ContainSingle().Which.Should().BeSameAs(animal);
    }

    [Test]
    public void TheReplayAppliesItsEventsInTimeOrder_ThenByTypeWhenTheyTie()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        var first = Animal(LivestockType.Warocas);
        var second = Animal(LivestockType.Warocas, placedHours: 5);

        var result = Replay(hand, new[] { first, second }, new[] { Plot(CropType.ViscaranOrange), Plot(CropType.Tomato) }, 100);

        result.Events.Select(e => e.TimeUtc).Should().BeInAscendingOrder();
        result.Events.Count.Should().BeGreaterThan(12);

        // At 72 hours the first animal grows up and is collected from in the same moment: growing up comes first.
        var atSeventyTwo = result.Events.Where(e => e.TimeUtc == T0.AddHours(72) && e.SubjectId == first.Id).Select(e => e.Type).ToList();
        atSeventyTwo.Should().Equal(FarmEventType.Matured, FarmEventType.Collected);
    }

    [Test]
    public void TheHopperIsSharedByTheAnimalsOfACorral_SoOrderDecidesWhoEats()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 2;
        var early = Animal(LivestockType.Warocas, placedHours: 0, id: "early");
        var late = Animal(LivestockType.Warocas, placedHours: 0, id: "late");

        var result = Replay(hand, new[] { late, early }, Array.Empty<GardenPlot>(), 1);

        result.Events.Where(e => e.Type == FarmEventType.Fed).Should().ContainSingle("there was feed for one meal only");
        result.Events.Single(e => e.Type == FarmEventType.Fed).SubjectId.Should().Be("early", "ties at the same moment go in a fixed order");
        hand.FeedHopper.Should().BeEmpty();
    }

    // Feed running out partway

    [Test]
    public void WhenFeedRunsOutPartway_TheAnimalStopsEatingAndWandersOffSeventyTwoHoursAfterItsLastMeal()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 5;
        var animal = Animal();

        var result = Replay(hand, new[] { animal }, Array.Empty<GardenPlot>(), 200);

        Hours(result, FarmEventType.Fed).Should().Equal(0, 20);
        Hours(result, FarmEventType.WanderedOff).Should().Equal(92);
        result.WanderedOff.Should().ContainSingle().Which.Should().BeSameAs(animal);
        result.Animals.Should().BeEmpty();
        Types(result).Should().NotContain(FarmEventType.Collected).And.NotContain(FarmEventType.Matured, "two meals never grow it up");
        hand.FeedHopper.Should().Equal(new Dictionary<CropType, int> { [CropType.ViscaranOrange] = 1 }, "one crop was left, too few for a meal");
    }

    [Test]
    public void AnAnimalWithNoFeedAtAllWandersOffSeventyTwoHoursAfterItWasPlaced()
    {
        var hand = Hand(5);
        var animal = Animal(placedHours: 0);

        var result = Replay(hand, new[] { animal }, Array.Empty<GardenPlot>(), 100);

        Types(result).Should().Equal(FarmEventType.WanderedOff);
        Hours(result, FarmEventType.WanderedOff).Should().Equal(72);
    }

    [Test]
    public void WhenTheFeedRunsOutAnAdultKeepsGivingProductsUntilItWasLastFedMoreThanADayAgo()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 2;
        var animal = Animal(placedHours: -100, feedings: 3, fedHours: -2, collectedHours: -30);

        var result = Replay(hand, new[] { animal }, Array.Empty<GardenPlot>(), 60);

        // Fed at the start (last meal 2 hours ago, but it may eat again only 20 hours after it: at hour 18).
        Hours(result, FarmEventType.Fed).Should().Equal(18);
        // Collected at the start, as its 20 hours were up; then 20 hours later, while fed within 24; then it goes without.
        Hours(result, FarmEventType.Collected).Should().Equal(0, 20, 40);
        // 72 hours after the meal at 18.
        animal.LastFedUtc.Should().Be(T0.AddHours(18));
        result.WanderedOff.Should().BeEmpty();
    }

    [Test]
    public void AnAdultWithNoFeedIsCollectedOnceMoreWhileItsLastMealIsWithinADay_ThenNotAgain()
    {
        var hand = Hand(5);
        var animal = Animal(placedHours: -100, feedings: 3, fedHours: -10);

        var result = Replay(hand, new[] { animal }, Array.Empty<GardenPlot>(), 30);

        Hours(result, FarmEventType.Collected).Should().Equal(0);
        hand.OutputHopper.Should().Equal(new Dictionary<string, int> { ["manure_t1"] = 1 });
    }

    [Test]
    public void ThePredatorsPenaltyHoldsBackCollectionUntilItEnds()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        var animal = Animal(placedHours: -100, feedings: 3, fedHours: -2, penaltyUntilHours: 30);

        var result = Replay(hand, new[] { animal }, Array.Empty<GardenPlot>(), 50);

        Hours(result, FarmEventType.Collected).Should().Equal(30, 50);
        hand.OutputHopper["manure_t1"].Should().Be(2);
    }

    [Test]
    public void AFullOutputHopperStopsCollecting_AndNothingIsLost()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        hand.OutputHopper["manure_t1"] = 19;
        var animal = Animal(placedHours: -100, feedings: 3, fedHours: -2, collectedHours: -30);

        var result = Replay(hand, new[] { animal }, Array.Empty<GardenPlot>(), 70);

        Hours(result, FarmEventType.Collected).Should().Equal(0);
        hand.OutputHopper["manure_t1"].Should().Be(20, "the hopper filled and the droid stopped");
        animal.LastCollectedUtc.Should().Be(T0, "only the collection that fit happened, and none after the hopper filled");
        animal.Feedings.Should().BeGreaterThan(3, "it still ate");
    }

    [Test]
    public void ACollectionThatDoesNotFitWholeWaitsForRoom()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.Tomato] = 20;
        hand.OutputHopper["manure_t1"] = 19;
        var thune = Animal(LivestockType.Thune, placedHours: -100, feedings: 3, fedHours: -2);

        var result = Replay(hand, new[] { thune }, Array.Empty<GardenPlot>(), 30);

        Types(result).Should().NotContain(FarmEventType.Collected, "manure and milk need two places and the hopper has one");
        hand.OutputHopper["manure_t1"].Should().Be(19);
    }

    [Test]
    public void AThuneYieldsManureAndMilkInTheHopper()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.Tomato] = 20;
        var thune = Animal(LivestockType.Thune, placedHours: -100, feedings: 3, fedHours: -2);

        Replay(hand, new[] { thune }, Array.Empty<GardenPlot>(), 1);

        hand.OutputHopper.Should().Equal(new Dictionary<string, int> { ["manure_t5"] = 1, ["milk"] = 1 });
    }

    // The droid's tier

    [Test]
    public void ADroidFeedsAndCollectsOnlyAnimalsUpToItsTier_WhileTheRestStillWanderOff()
    {
        var hand = Hand(2);
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        hand.FeedHopper[CropType.Tomato] = 20;
        var warocas = Animal(LivestockType.Warocas);
        var womprat = Animal(LivestockType.Womprat);

        var result = Replay(hand, new[] { warocas, womprat }, Array.Empty<GardenPlot>(), 80);

        result.Events.Where(e => e.Type == FarmEventType.Fed).Select(e => e.SubjectId).Distinct().Should().Equal(warocas.Id);
        womprat.Feedings.Should().Be(0);
        result.WanderedOff.Should().ContainSingle().Which.Should().BeSameAs(womprat);
        Hours(result, FarmEventType.WanderedOff).Should().Equal(72);
    }

    [Test]
    public void AnAnimalCannotLiveOnCropsOfAHigherTierThanItsOwn()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.Tomato] = 20;
        var warocas = Animal(LivestockType.Warocas);

        var result = Replay(hand, new[] { warocas }, Array.Empty<GardenPlot>(), 80);

        Types(result).Should().Equal(FarmEventType.WanderedOff);
        hand.FeedHopper[CropType.Tomato].Should().Be(20);
    }

    [Test]
    public void ADroidWorksOnlyItsOwnersAnimalsAndCrops()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        var mine = Animal();
        var theirs = Animal(owner: "someone-else");
        var myPlot = Plot(CropType.ViscaranOrange);
        var theirPlot = Plot(CropType.ViscaranLemon, planter: "someone-else");

        var result = Replay(hand, new[] { mine, theirs }, new[] { myPlot, theirPlot }, 20);

        theirs.Feedings.Should().Be(0);
        theirs.LastProcessedUtc.Should().Be(T0, "another owner's animal is not the droid's to touch");
        theirPlot.Tended.Should().BeFalse();
        result.Animals.Should().ContainSingle().Which.Should().BeSameAs(mine);
        result.Plots.Should().ContainSingle().Which.Should().BeSameAs(myPlot);
        myPlot.Tended.Should().BeTrue();
    }

    // A house revisited after a wither deadline

    [Test]
    public void AHouseRevisitedAfterTheWitherDeadline_TendedTheCropWhenItBecameReady_ThenLostIt()
    {
        var hand = Hand(3, FarmhandTargetType.House);
        var orange = Plot(CropType.ViscaranOrange);

        var result = Replay(hand, Array.Empty<Livestock>(), new[] { orange }, 100);

        // Tier 1 grows in 12 hours: ready to tend at 3, ripe at 12, withered 72 hours later at 84. A droid never harvests.
        Hours(result, FarmEventType.Tended).Should().Equal(3);
        Hours(result, FarmEventType.Ripened).Should().Equal(12);
        Hours(result, FarmEventType.Withered).Should().Equal(84);
        Types(result).Should().Equal(FarmEventType.Tended, FarmEventType.Ripened, FarmEventType.Withered);
        orange.Tended.Should().BeTrue("it was tended the moment it became ready, before it withered");
        result.Withered.Should().ContainSingle().Which.Should().BeSameAs(orange);
        result.Plots.Should().BeEmpty();
        hand.LastProcessedUtc.Should().Be(T0.AddHours(100));
    }

    [Test]
    public void AHouseRevisitedBeforeTheWitherDeadlineFindsATendedRipeCrop()
    {
        var hand = Hand(3, FarmhandTargetType.House);
        var orange = Plot(CropType.ViscaranOrange);

        var result = Replay(hand, Array.Empty<Livestock>(), new[] { orange }, 80);

        Types(result).Should().Equal(FarmEventType.Tended, FarmEventType.Ripened);
        orange.Tended.Should().BeTrue();
        result.Withered.Should().BeEmpty();
        result.Plots.Should().ContainSingle();
        orange.LastProcessedUtc.Should().Be(T0.AddHours(80));
    }

    [Test]
    public void ADroidTendsEachCropAsItBecomesReady_AndSkipsOnesItsTierCannotHandle()
    {
        var hand = Hand(2, FarmhandTargetType.House);
        var orange = Plot(CropType.ViscaranOrange);
        var peas = Plot(CropType.ViscaranPeas, plantedHours: 4);
        var tomato = Plot(CropType.Tomato);

        var result = Replay(hand, Array.Empty<Livestock>(), new[] { tomato, peas, orange }, 10);

        result.Events.Where(e => e.Type == FarmEventType.Tended).Select(e => e.SubjectId).Should().Equal(orange.Id, peas.Id);
        Hours(result, FarmEventType.Tended).Should().Equal(3, 8);
        tomato.Tended.Should().BeFalse("a tier-5 crop is beyond a tier-2 droid");
    }

    [Test]
    public void ACropAlreadyTendedIsNotTendedAgain()
    {
        var hand = Hand(5, FarmhandTargetType.House);
        var orange = Plot(CropType.ViscaranOrange, tended: true);

        var result = Replay(hand, Array.Empty<Livestock>(), new[] { orange }, 10);

        Types(result).Should().NotContain(FarmEventType.Tended);
    }

    [Test]
    public void ACropThatWitheredBeforeTheDroidWasPostedIsStillReportedWithered_AndNeverTended()
    {
        var hand = Hand(5, FarmhandTargetType.House, processed: T0.AddHours(90));
        var old = Plot(CropType.ViscaranOrange, plantedHours: -1);
        old.LastProcessedUtc = T0.AddHours(-1);

        var result = FarmhandRules.Replay(hand, Array.Empty<Livestock>(), new[] { old }, GetAnimal, GetCrop, T0.AddHours(100));

        Types(result).Should().Equal(FarmEventType.Withered);
        old.Tended.Should().BeFalse();
    }

    // Processing twice

    [Test]
    public void ReplayingAnEmptySpanDoesNothing()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        var animal = Animal();

        var result = Replay(hand, new[] { animal }, Array.Empty<GardenPlot>(), 0);

        result.Ran.Should().BeFalse();
        result.Events.Should().BeEmpty();
        animal.Feedings.Should().Be(0);
        hand.LastProcessedUtc.Should().Be(T0);
        hand.FeedHopper[CropType.ViscaranOrange].Should().Be(20);
    }

    [Test]
    public void ABackwardsSpanDoesNothing()
    {
        var hand = Hand(5, processed: T0.AddHours(10));
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        var animal = Animal();

        var result = Replay(hand, new[] { animal }, Array.Empty<GardenPlot>(), 5);

        result.Ran.Should().BeFalse();
        animal.Feedings.Should().Be(0);
        hand.LastProcessedUtc.Should().Be(T0.AddHours(10));
    }

    [Test]
    public void ProcessingTwiceInARowDoesNothingNew()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        var animal = Animal();
        var plot = Plot(CropType.ViscaranOrange);
        Replay(hand, new[] { animal }, new[] { plot }, 96);
        var feed = hand.FeedHopper.ToDictionary(pair => pair.Key, pair => pair.Value);
        var output = hand.OutputHopper.ToDictionary(pair => pair.Key, pair => pair.Value);
        var feedings = animal.Feedings;

        var again = Replay(hand, new[] { animal }, Array.Empty<GardenPlot>(), 96);

        again.Ran.Should().BeFalse();
        again.Events.Should().BeEmpty();
        hand.FeedHopper.Should().Equal(feed);
        hand.OutputHopper.Should().Equal(output);
        animal.Feedings.Should().Be(feedings);
    }

    [Test]
    public void AShortSpanAfterAReplayHasNothingDueToApply()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        var animal = Animal();
        Replay(hand, new[] { animal }, Array.Empty<GardenPlot>(), 96);

        var again = Replay(hand, new[] { animal }, Array.Empty<GardenPlot>(), 97);

        again.Ran.Should().BeTrue("an hour went by");
        again.Events.Should().BeEmpty("the next meal is not due until hour 100");
        hand.LastProcessedUtc.Should().Be(T0.AddHours(97));
    }

    [TestCase(1)]
    [TestCase(7)]
    [TestCase(24)]
    public void EnteringOftenNeitherSpeedsADroidUpNorRepeatsAnAction(int sliceHours)
    {
        var whole = Scene();
        Step(whole, 200);

        var sliced = Scene();
        for (var hour = sliceHours; hour < 200; hour += sliceHours)
            Step(sliced, hour);
        Step(sliced, 200);

        sliced.Hand.FeedHopper.Should().Equal(whole.Hand.FeedHopper);
        sliced.Hand.OutputHopper.Should().Equal(whole.Hand.OutputHopper);
        sliced.Animals.Select(animal => animal.Id).Should().Equal(whole.Animals.Select(animal => animal.Id));
        for (var index = 0; index < whole.Animals.Count; index++)
        {
            sliced.Animals[index].Feedings.Should().Be(whole.Animals[index].Feedings, $"animal {index}");
            sliced.Animals[index].LastFedUtc.Should().Be(whole.Animals[index].LastFedUtc, $"animal {index}");
            sliced.Animals[index].LastCollectedUtc.Should().Be(whole.Animals[index].LastCollectedUtc, $"animal {index}");
        }

        sliced.Plots.Select(plot => plot.Tended).Should().Equal(whole.Plots.Select(plot => plot.Tended));
        sliced.Plots.Select(plot => plot.Id).Should().Equal(whole.Plots.Select(plot => plot.Id));
    }

    /// <summary>A corral with three animals and a house plot or two, fed from a hopper that runs out partway.</summary>
    private static (Farmhand Hand, List<Livestock> Animals, List<GardenPlot> Plots) Scene()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 12;
        hand.FeedHopper[CropType.ViscaranApple] = 3;
        var animals = new List<Livestock>
        {
            Animal(LivestockType.Warocas, id: "a"),
            Animal(LivestockType.Gimpassa, placedHours: 3, id: "b"),
            Animal(LivestockType.Warocas, placedHours: -100, feedings: 3, fedHours: -5, collectedHours: -40, id: "c"),
        };
        var plots = new List<GardenPlot> { Plot(CropType.ViscaranOrange), Plot(CropType.ViscaranApple, plantedHours: 30) };
        return (hand, animals, plots);
    }

    /// <summary>Replays to the hour, then does what the service does with the result: removes the animals and crops that are gone.</summary>
    private static void Step((Farmhand Hand, List<Livestock> Animals, List<GardenPlot> Plots) scene, double hours)
    {
        var result = Replay(scene.Hand, scene.Animals, scene.Plots, hours);
        scene.Animals.RemoveAll(result.WanderedOff.Contains);
        scene.Plots.RemoveAll(result.Withered.Contains);
    }

    // The replay's own limits

    [Test]
    public void ALongAbsenceStaysWithinTheReplayGuard()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        var animals = new[] { Animal(id: "a"), Animal(id: "b"), Animal(id: "c") };

        var result = Replay(hand, animals, Array.Empty<GardenPlot>(), 24 * 365 * 5);

        result.Events.Count.Should().BeLessThan(FarmhandRules.MaxReplaySteps);
        result.WanderedOff.Count.Should().Be(3, "five years without enough feed lose every animal");
        hand.LastProcessedUtc.Should().Be(T0.AddHours(24 * 365 * 5));
    }

    [Test]
    public void AnAnimalThatTheDefinitionsDoNotKnowIsLeftAlone()
    {
        var hand = Hand(5);
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        var unknown = Animal(LivestockType.Invalid);

        var result = Replay(hand, new[] { unknown }, Array.Empty<GardenPlot>(), 200);

        result.Events.Should().BeEmpty();
        result.Animals.Should().BeEmpty();
    }

    [Test]
    public void TheFarmhandRecordIsKeyedApartFromItsPost()
    {
        Farmhand.KeyFor("abc").Should().Be("farmhand:abc").And.NotBe("abc");
        var hand = new Farmhand();
        hand.FeedHopper.Should().BeEmpty();
        hand.OutputHopper.Should().BeEmpty();
        hand.EntityType.Should().Be(nameof(Farmhand));
    }
}
