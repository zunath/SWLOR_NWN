using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.TravelerService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The travelers' weekly rotation, the stock they carry and the allowance that goes with them, and the Traveler's Lodge
/// placement: rotation eligibility, the 4-week rule, Open Gates weighting, the hub fallback, a host lost mid-week, and
/// allowances that survive a move and a restart.
/// </summary>
public class TravelerRulesTests
{
    private static readonly DateTime Week = new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);

    private static TravelerHostCandidate Host(string id, bool openGates = false) => new(id, openGates);

    private static TravelerRotationInput Traveler(TravelerType type, params (string City, int WeeksAgo)[] visits) =>
        new(type, visits.Select(visit => new TravelerVisit(visit.City, Week.AddDays(-7 * visit.WeeksAgo))).ToList());

    private static Func<int, int> Rolls(params int[] values)
    {
        var index = 0;
        return max => Math.Min(values[index++ % values.Length], max - 1);
    }

    private static Func<int, int> Seeded(int seed)
    {
        var random = new System.Random(seed);
        return random.Next;
    }

    private static IReadOnlyDictionary<TravelerType, string> Plan(
        TravelerRotationInput[] travelers,
        TravelerHostCandidate[] hosts,
        Func<int, int> nextInt,
        string[] occupied = null)
    {
        return TravelerRules.PlanRotation(travelers, hosts, occupied ?? Array.Empty<string>(), Week, nextInt);
    }

    #region When a traveler moves

    [Test]
    public void ATravelerNeverPlaced_AndOneFromAnEarlierWeek_AreDueToMove()
    {
        TravelerRules.ShouldRotate(default, Week).Should().BeTrue();
        TravelerRules.ShouldRotate(Week.AddDays(-7), Week).Should().BeTrue();
    }

    [Test]
    public void ATravelerPlacedThisWeek_StaysUntilTheWeeklyBoundary()
    {
        TravelerRules.ShouldRotate(Week, Week).Should().BeFalse("a restart in the same week moves nobody");
        TravelerRules.ShouldRotate(Week, Week.AddDays(7)).Should().BeTrue();
    }

    [Test]
    public void TheRotationMoves_AtTheSameBoundaryTheStockAllowancesReset()
    {
        // One boundary for both: the traveler's week is the allowance's week.
        CityWeek.GetWeekStart(Week.AddDays(3).AddHours(5)).Should().Be(Week);
        TravelerRules.ShouldRotate(CityWeek.GetWeekStart(Week.AddDays(3)), CityWeek.GetWeekStart(Week.AddDays(7).AddMinutes(1))).Should().BeTrue();
    }

    #endregion

    #region The 4-week rule

    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(3, true)]
    [TestCase(4, true)]
    [TestCase(5, false)]
    [TestCase(9, false)]
    public void ACity_IsSkippedForFourWeeksAfterItHosts(int weeksAgo, bool skipped)
    {
        var history = new[] { new TravelerVisit("a", Week.AddDays(-7 * weeksAgo)) };

        TravelerRules.HostedWithinWindow(history, "a", Week).Should().Be(skipped);
    }

    [Test]
    public void AnotherCitysVisit_DoesNotCountAgainstACity()
    {
        var history = new[] { new TravelerVisit("b", Week.AddDays(-7)) };

        TravelerRules.HostedWithinWindow(history, "a", Week).Should().BeFalse();
        TravelerRules.HostedWithinWindow(null, "a", Week).Should().BeFalse();
    }

    [Test]
    public void ATraveler_NeverGoesBackToACityThatHostedItInTheLastFourWeeks()
    {
        var hosts = new[] { Host("a"), Host("b"), Host("c") };
        var traveler = Traveler(TravelerType.BeastTrader, ("a", 1), ("b", 4));

        for (var seed = 0; seed < 200; seed++)
            Plan(new[] { traveler }, hosts, Seeded(seed))[TravelerType.BeastTrader].Should().Be("c");
    }

    [Test]
    public void ACityThatHostedFiveWeeksAgo_CanHostAgain()
    {
        var plan = Plan(new[] { Traveler(TravelerType.Prospector, ("a", 5)) }, new[] { Host("a") }, Rolls(0));

        plan[TravelerType.Prospector].Should().Be("a");
    }

    [Test]
    public void ATraveler_AlwaysMovesToADifferentCity_ThanLastWeeks()
    {
        var hosts = new[] { Host("a"), Host("b") };

        for (var seed = 0; seed < 100; seed++)
            Plan(new[] { Traveler(TravelerType.FashionMerchant, ("a", 1)) }, hosts, Seeded(seed))[TravelerType.FashionMerchant].Should().Be("b");
    }

    #endregion

    #region Open Gates

    [Test]
    public void ACityWithOpenGates_CountsTwice()
    {
        TravelerRules.GetWeight(true).Should().Be(2);
        TravelerRules.GetWeight(false).Should().Be(1);
    }

    [TestCase(0, "gates")]
    [TestCase(1, "gates")]
    [TestCase(2, "plain")]
    public void TheRoll_FallsInTheOpenGatesCitysTwoSlotsOrThePlainCitysOne(int roll, string expected)
    {
        var hosts = new[] { Host("gates", true), Host("plain") };

        Plan(new[] { Traveler(TravelerType.BeastTrader) }, hosts, Rolls(roll))[TravelerType.BeastTrader].Should().Be(expected);
    }

    [Test]
    public void ACityWithOpenGates_IsTwiceAsLikelyToBeChosen()
    {
        var hosts = new[] { Host("gates", true), Host("plain") };
        var random = new System.Random(2026);
        var gates = 0;
        const int runs = 6000;

        for (var i = 0; i < runs; i++)
        {
            if (Plan(new[] { Traveler(TravelerType.BeastTrader) }, hosts, random.Next)[TravelerType.BeastTrader] == "gates")
                gates++;
        }

        ((double)gates / runs).Should().BeInRange(0.63, 0.70, "two of three slots belong to the Open Gates city");
    }

    [Test]
    public void OpenGates_OnlyWeighsCitiesThatQualify()
    {
        var hosts = new[] { Host("gates", true), Host("plain") };
        var traveler = Traveler(TravelerType.Prospector, ("gates", 2));

        for (var seed = 0; seed < 100; seed++)
            Plan(new[] { traveler }, hosts, Seeded(seed))[TravelerType.Prospector].Should().Be("plain");
    }

    #endregion

    #region Two travelers

    [Test]
    public void TwoTravelers_DoNotShareAHost_WhileAnotherCityQualifies()
    {
        var hosts = new[] { Host("a"), Host("b"), Host("c") };
        var travelers = new[]
        {
            Traveler(TravelerType.BeastTrader),
            Traveler(TravelerType.Prospector),
            Traveler(TravelerType.FashionMerchant),
            Traveler(TravelerType.InformationBroker)
        };

        for (var seed = 0; seed < 200; seed++)
        {
            var plan = Plan(travelers, hosts, Seeded(seed));
            var chosen = plan.Values.Where(host => host != TravelerRules.HubHostId).ToList();

            plan.Should().HaveCount(4);
            chosen.Should().HaveCount(4, "a traveler shares a host before it waits at the hub");
            chosen.Distinct().Should().HaveCount(3, "three cities host four travelers, so exactly one pair shares");
        }
    }

    [Test]
    public void EachOfThreeTravelers_GetsItsOwnCity_WhenThreeQualify()
    {
        var hosts = new[] { Host("a"), Host("b"), Host("c"), Host("d") };
        var travelers = new[]
        {
            Traveler(TravelerType.BeastTrader),
            Traveler(TravelerType.Prospector),
            Traveler(TravelerType.FashionMerchant)
        };

        for (var seed = 0; seed < 200; seed++)
            Plan(travelers, hosts, Seeded(seed)).Values.Distinct().Should().HaveCount(3);
    }

    [Test]
    public void AnotherTravelersCity_IsSharedBeforeTheHub_WhenItIsTheOnlyOneThatQualifies()
    {
        var hosts = new[] { Host("a"), Host("b") };
        var travelers = new[]
        {
            Traveler(TravelerType.BeastTrader),
            Traveler(TravelerType.Prospector, ("b", 2))
        };

        for (var seed = 0; seed < 100; seed++)
        {
            var plan = Plan(travelers, hosts, Seeded(seed));

            plan[TravelerType.Prospector].Should().Be("a", "the only city that did not host it recently");
            plan[TravelerType.BeastTrader].Should().NotBe(TravelerRules.HubHostId);
        }
    }

    [Test]
    public void TwoTravelers_ShareTheOnlyCity_InsteadOfWaitingAtTheHub()
    {
        var plan = Plan(
            new[] { Traveler(TravelerType.BeastTrader), Traveler(TravelerType.InformationBroker) },
            new[] { Host("only") },
            Rolls(0));

        plan[TravelerType.BeastTrader].Should().Be("only");
        plan[TravelerType.InformationBroker].Should().Be("only");
    }

    [Test]
    public void ATravelerThatIsNotMoving_KeepsItsHostToItself()
    {
        var hosts = new[] { Host("a"), Host("b") };

        for (var seed = 0; seed < 100; seed++)
            Plan(new[] { Traveler(TravelerType.Prospector) }, hosts, Seeded(seed), new[] { "a" })[TravelerType.Prospector].Should().Be("b");
    }

    [Test]
    public void TheOrderTravelersChooseIn_IsRandom_SoNoOneIsAlwaysTheOneWhoShares()
    {
        var hosts = new[] { Host("a"), Host("b"), Host("c") };
        var travelers = new[]
        {
            Traveler(TravelerType.BeastTrader),
            Traveler(TravelerType.Prospector),
            Traveler(TravelerType.FashionMerchant),
            Traveler(TravelerType.InformationBroker)
        };
        var sharing = new HashSet<TravelerType>();

        // Four travelers on three cities: one pair shares. Which pair depends on who chose last.
        for (var seed = 0; seed < 100; seed++)
        {
            var plan = Plan(travelers, hosts, Seeded(seed));
            foreach (var (type, host) in plan)
            {
                if (plan.Count(other => other.Value == host) > 1)
                    sharing.Add(type);
            }
        }

        sharing.Should().HaveCount(4, "every traveler ends up sharing under some seed");
    }

    #endregion

    #region The hub

    [Test]
    public void WithNoCityThatHasALodge_EveryTravelerWaitsAtTheHub()
    {
        var plan = Plan(
            new[] { Traveler(TravelerType.BeastTrader), Traveler(TravelerType.Prospector) },
            Array.Empty<TravelerHostCandidate>(),
            Rolls(0));

        plan.Values.Should().OnlyContain(host => host == TravelerRules.HubHostId);
        TravelerRules.HubHostId.Should().BeEmpty();
    }

    [Test]
    public void WhenEveryLodgeCityHostedItRecently_TheTravelerWaitsAtTheHub()
    {
        var traveler = Traveler(TravelerType.FashionMerchant, ("a", 1), ("b", 3));

        Plan(new[] { traveler }, new[] { Host("a"), Host("b") }, Rolls(0))[TravelerType.FashionMerchant].Should().Be(TravelerRules.HubHostId);
    }

    [Test]
    public void TheHub_IsAnExistingPublicWaypointInTheVelesStarportArea()
    {
        TravelerRules.HubWaypointTag.Should().Be("VISCARA_LANDING");
    }

    #endregion

    #region A host lost mid-week

    [Test]
    public void ATravelerWhoseHostLostItsLodge_MovesToTheHub()
    {
        TravelerRules.MustMoveToHub("a", new[] { "b", "c" }).Should().BeTrue();
    }

    [Test]
    public void ATravelerWhoseHostIsGone_MovesToTheHub()
    {
        TravelerRules.MustMoveToHub("a", Array.Empty<string>()).Should().BeTrue();
        TravelerRules.MustMoveToHub("a", null).Should().BeTrue();
    }

    [Test]
    public void ATravelerWhoseHostStillHasItsLodge_StaysPut()
    {
        TravelerRules.MustMoveToHub("a", new[] { "a", "b" }).Should().BeFalse();
    }

    [Test]
    public void ATravelerAlreadyAtTheHub_HasNothingToMove()
    {
        TravelerRules.MustMoveToHub(TravelerRules.HubHostId, new[] { "a" }).Should().BeFalse();
        TravelerRules.MustMoveToHub(string.Empty, Array.Empty<string>()).Should().BeFalse();
    }

    [Test]
    public void ACityThatLostItsLodgeMidWeek_StillCountsAsHavingHostedThatWeek()
    {
        // The week was recorded when the traveler arrived, and moving to the hub does not erase it.
        var history = TravelerRules.AppendVisit(Array.Empty<TravelerVisit>(), "a", Week);

        TravelerRules.HostedWithinWindow(history, "a", Week.AddDays(7)).Should().BeTrue();
        TravelerRules.HostedWithinWindow(history, "a", Week.AddDays(7 * 5)).Should().BeFalse();
    }

    #endregion

    #region History

    [Test]
    public void AHost_AddsAVisit_AndTheHubAddsNone()
    {
        TravelerRules.AppendVisit(Array.Empty<TravelerVisit>(), "a", Week).Should().ContainSingle(visit => visit.CityId == "a" && visit.WeekStartUtc == Week);
        TravelerRules.AppendVisit(Array.Empty<TravelerVisit>(), TravelerRules.HubHostId, Week).Should().BeEmpty();
    }

    [Test]
    public void VisitsOlderThanEightWeeks_AreDropped_AndNewerOnesKept()
    {
        var history = new[]
        {
            new TravelerVisit("old", Week.AddDays(-7 * 9)),
            new TravelerVisit("edge", Week.AddDays(-7 * 8)),
            new TravelerVisit("recent", Week.AddDays(-7))
        };

        TravelerRules.AppendVisit(history, "a", Week).Select(visit => visit.CityId).Should().Equal("edge", "recent", "a");
    }

    [Test]
    public void PlacingTheSameWeekTwice_LeavesOneVisit()
    {
        var once = TravelerRules.AppendVisit(Array.Empty<TravelerVisit>(), "a", Week);
        var twice = TravelerRules.AppendVisit(once, "b", Week);

        twice.Should().ContainSingle().Which.CityId.Should().Be("b");
    }

    [Test]
    public void TheHistory_KeepsLongerThanTheRepeatRuleNeeds()
    {
        TravelerRules.HistoryWeeksKept.Should().BeGreaterThan(TravelerRules.RepeatWindowWeeks);
    }

    [Test]
    public void ATravelersState_SurvivesARestart_AsItsOwnRecord()
    {
        var state = new TravelerState(TravelerType.Prospector)
        {
            HostCityId = "a",
            RotatedWeekStartUtc = Week,
            History = TravelerRules.AppendVisit(Array.Empty<TravelerVisit>(), "a", Week)
        };

        state.Id.Should().Be("Prospector");
        TravelerRules.ShouldRotate(state.RotatedWeekStartUtc, Week).Should().BeFalse();
        TravelerRules.ShouldRotate(state.RotatedWeekStartUtc, Week.AddDays(7)).Should().BeTrue();
        new TravelerState().HostCityId.Should().BeEmpty();
        new TravelerState().History.Should().BeEmpty();
    }

    #endregion

    #region Weekly stock

    private static TravelerDetail Pool(int picks, params string[] resrefs)
    {
        var detail = new TravelerDetail(TravelerType.BeastTrader) { Name = "Test" };
        var group = new TravelerStockGroup("Pool", picks);
        foreach (var resref in resrefs)
            group.Items.Add(new TravelerStockItem(resref, 3));

        detail.Groups.Add(group);
        return detail;
    }

    [Test]
    public void TheWeeklyStock_HasTheRightNumberOfDifferentItemsFromEachPool()
    {
        foreach (var detail in TravelerCatalog.All.Values)
        {
            var stock = TravelerRules.GetWeeklyStock(detail, Week);

            stock.Should().HaveCount(detail.Groups.Sum(group => Math.Min(group.Picks, group.Items.Count)));
            stock.Select(item => item.Resref).Should().OnlyHaveUniqueItems();

            foreach (var group in detail.Groups)
            {
                var inGroup = stock.Where(item => group.Items.Contains(item)).ToList();
                inGroup.Should().HaveCount(Math.Min(group.Picks, group.Items.Count), $"{detail.Key} {group.Name}");
            }
        }
    }

    [Test]
    public void TheWeeklyStock_IsTheSameAllWeek_AndAfterARestart()
    {
        var detail = TravelerCatalog.Get(TravelerType.Prospector);
        var monday = TravelerRules.GetWeeklyStock(detail, Week).Select(item => item.Resref).ToList();

        TravelerRules.GetWeeklyStock(detail, Week.AddDays(3).AddHours(14)).Select(item => item.Resref).Should().Equal(monday);
        TravelerRules.GetWeeklyStock(detail, Week.AddDays(6).AddHours(23)).Select(item => item.Resref).Should().Equal(monday);
    }

    [Test]
    public void TheWeeklyDraw_DoesNotDependOnTheProcess()
    {
        // The seed is a fixed hash of the traveler, the pool and the week, never string.GetHashCode, which differs for
        // each process. If this fails the weekly stock would change at every restart.
        var detail = Pool(3, "a", "b", "c", "d", "e", "f", "g");

        TravelerRules.GetWeeklyStock(detail, Week).Select(item => item.Resref).Should().Equal(ExpectedDraw);
    }

    private static readonly string[] ExpectedDraw = { "d", "b", "g" };

    [Test]
    public void TheWeeklyStock_ChangesFromWeekToWeek()
    {
        var detail = TravelerCatalog.Get(TravelerType.BeastTrader);
        var weeks = Enumerable.Range(0, 12)
            .Select(week => string.Join(",", TravelerRules.GetWeeklyStock(detail, Week.AddDays(7 * week)).Select(item => item.Resref).OrderBy(x => x)))
            .Distinct()
            .Count();

        weeks.Should().BeGreaterThan(6, "the stock rotates");
    }

    [Test]
    public void APoolSmallerThanItsPicks_CarriesAllOfIt()
    {
        TravelerRules.GetWeeklyStock(Pool(5, "a", "b"), Week).Select(item => item.Resref).Should().BeEquivalentTo("a", "b");
        TravelerRules.GetWeeklyStock(Pool(0, "a", "b"), Week).Should().BeEmpty();
        TravelerRules.GetWeeklyStock(null, Week).Should().BeEmpty();
    }

    [Test]
    public void AnItemOutsideThisWeeksStock_IsNotFound()
    {
        var detail = Pool(1, "a", "b", "c");
        var carried = TravelerRules.GetWeeklyStock(detail, Week).Single();

        TravelerRules.FindStockItem(detail, Week, carried.Resref).Should().Be(carried);
        TravelerRules.FindStockItem(detail, Week, "z").Should().BeNull();
        TravelerRules.FindStockItem(detail, Week, string.Empty).Should().BeNull();
    }

    #endregion

    #region Allowances

    /// <summary>The records the database would hold for travelers, keyed the way the entity builds its id.</summary>
    private sealed class FakeBook
    {
        public readonly Dictionary<string, TravelerStockAllowance> Records = new();

        public IStockAllowanceRecord Get(string key, string resref) => Records.GetValueOrDefault(TravelerStockAllowance.BuildId(key, resref));

        public void Sell(string key, string resref, int quantity, DateTime now)
        {
            var id = TravelerStockAllowance.BuildId(key, resref);
            if (!Records.TryGetValue(id, out var record))
                Records[id] = record = new TravelerStockAllowance(key, resref);

            StockAllowanceRules.RecordSale(record, quantity, CityWeek.GetWeekStart(now));
        }

        /// <summary>What the traveler's store puts on its shelf when it is created at this moment, wherever it is.</summary>
        public IReadOnlyList<CityStoreShelfEntry> CreateStore(TravelerDetail traveler, DateTime now) =>
            TravelerRules.PlanShelf(traveler, now, resref => Get(traveler.Key, resref));

        /// <summary>A restart: a new book from the saved records.</summary>
        public FakeBook Restart()
        {
            var book = new FakeBook();
            foreach (var (id, record) in Records)
                book.Records[id] = record;

            return book;
        }
    }

    private static int OnShelf(IReadOnlyList<CityStoreShelfEntry> shelf, string resref) =>
        shelf.FirstOrDefault(entry => entry.Resref == resref).Quantity;

    [Test]
    public void ANewShelf_HoldsEachItemUpToItsWeeklyLimit()
    {
        var detail = TravelerCatalog.Get(TravelerType.BeastTrader);
        var shelf = new FakeBook().CreateStore(detail, Week);

        shelf.Should().HaveCount(3);
        foreach (var entry in shelf)
            entry.Quantity.Should().Be(detail.Groups.Single().Items.Single(item => item.Resref == entry.Resref).WeeklyLimit);
    }

    [Test]
    public void ASale_ReducesTheShelf_AndASoldOutItemIsLeftOff()
    {
        var detail = TravelerCatalog.Get(TravelerType.BeastTrader);
        var book = new FakeBook();
        var item = book.CreateStore(detail, Week).OrderByDescending(entry => entry.Quantity).First();

        book.Sell(detail.Key, item.Resref, 1, Week.AddDays(1));
        OnShelf(book.CreateStore(detail, Week.AddDays(1)), item.Resref).Should().Be(item.Quantity - 1);

        book.Sell(detail.Key, item.Resref, item.Quantity - 1, Week.AddDays(2));
        book.CreateStore(detail, Week.AddDays(2)).Any(entry => entry.Resref == item.Resref).Should().BeFalse();
    }

    [Test]
    public void TheAllowance_FollowsTheTravelerToAnotherCity()
    {
        // The shelf is planned from the traveler's records alone: a store made in the next city after the move has no way
        // to know the old one, so nothing sold is refilled.
        var detail = TravelerCatalog.Get(TravelerType.BeastTrader);
        var book = new FakeBook();
        var item = book.CreateStore(detail, Week).First();
        book.Sell(detail.Key, item.Resref, 1, Week.AddDays(2));

        var inTheNextCity = book.CreateStore(detail, Week.AddDays(3));

        OnShelf(inTheNextCity, item.Resref).Should().Be(item.Quantity - 1);
    }

    [Test]
    public void TheAllowance_SurvivesARestart_AndAStoreBeingRecreated()
    {
        var detail = TravelerCatalog.Get(TravelerType.BeastTrader);
        var book = new FakeBook();
        var item = book.CreateStore(detail, Week).First();
        book.Sell(detail.Key, item.Resref, 2, Week.AddDays(2));

        var restarted = book.Restart();

        OnShelf(restarted.CreateStore(detail, Week.AddDays(2)), item.Resref).Should().Be(item.Quantity - 2);
        OnShelf(restarted.CreateStore(detail, Week.AddDays(2)), item.Resref).Should().Be(item.Quantity - 2, "creating the store again changes nothing");
    }

    [Test]
    public void TheAllowance_ResetsWhenTheWeekTurns()
    {
        var detail = TravelerCatalog.Get(TravelerType.Prospector);
        var book = new FakeBook();
        var item = book.CreateStore(detail, Week).First();
        book.Sell(detail.Key, item.Resref, item.Quantity, Week.AddDays(2));

        book.CreateStore(detail, Week.AddDays(6)).Any(entry => entry.Resref == item.Resref).Should().BeFalse();

        var nextWeek = TravelerRules.GetWeeklyStock(detail, Week.AddDays(7)).Select(stock => stock.Resref).ToList();
        foreach (var entry in book.CreateStore(detail, Week.AddDays(7)))
            entry.Quantity.Should().Be(detail.Groups.SelectMany(group => group.Items).Single(stock => stock.Resref == entry.Resref).WeeklyLimit);

        book.CreateStore(detail, Week.AddDays(7)).Select(entry => entry.Resref).Should().BeEquivalentTo(nextWeek);
    }

    [Test]
    public void OneTravelersSales_DoNotTouchAnotherTravelers()
    {
        var beast = TravelerCatalog.Get(TravelerType.BeastTrader);
        var book = new FakeBook();
        var item = book.CreateStore(beast, Week).First();
        book.Sell(TravelerType.Prospector.ToString(), item.Resref, 5, Week);

        OnShelf(book.CreateStore(beast, Week), item.Resref).Should().Be(item.Quantity);
    }

    #endregion

    #region The Lodge

    [Test]
    public void ACityWithNoLodge_MayPlaceOne_AndASecondIsRefused()
    {
        TravelerRules.GetLodgePlacementError(StructureType.TravelersLodge, 0).Should().BeEmpty();
        TravelerRules.GetLodgePlacementError(StructureType.TravelersLodge, 1).Should().Be("A city may have only 1 Traveler's Lodge.");
        TravelerRules.GetLodgePlacementError(StructureType.TravelersLodge, 3).Should().NotBeEmpty();
    }

    [Test]
    public void TheLodgeRule_LeavesOtherStructuresAlone()
    {
        TravelerRules.GetLodgePlacementError(StructureType.MarketStall, 5).Should().BeEmpty();
        TravelerRules.GetLodgePlacementError(StructureType.Corral, 5).Should().BeEmpty();
    }

    [TestCase(0f, 0f, 0f, 0f, 3.5f, 90f)]
    [TestCase(10f, 20f, 90f, 10f - 3.5f, 20f, 180f)]
    [TestCase(10f, 20f, 270f, 10f + 3.5f, 20f, 0f)]
    public void TheTraveler_StandsOnTheLodgesSideLookingOut(float lodgeX, float lodgeY, float lodgeFacing, float expectedX, float expectedY, float expectedFacing)
    {
        var placement = TravelerRules.GetLodgePlacement(lodgeX, lodgeY, lodgeFacing);

        placement.X.Should().BeApproximately(expectedX, 0.001f);
        placement.Y.Should().BeApproximately(expectedY, 0.001f);
        placement.FacingDegrees.Should().BeApproximately(expectedFacing, 0.001f);
    }

    #endregion

    #region What players read

    [TestCase("exotic beast trader", "an exotic beast trader")]
    [TestCase("rare-materials prospector", "a rare-materials prospector")]
    [TestCase("information broker", "an information broker")]
    [TestCase("fashion merchant", "a fashion merchant")]
    [TestCase("", "")]
    public void ATitle_GetsItsArticle(string title, string expected)
    {
        TravelerText.WithArticle(title).Should().Be(expected);
    }

    [Test]
    public void TheArrival_NamesTheTravelerTheTitleAndTheCity()
    {
        var detail = TravelerCatalog.Get(TravelerType.BeastTrader);
        var text = TravelerText.BuildArrival(detail, "Port Nova");

        text.Should().Contain(detail.Name).And.Contain("an exotic beast trader").And.Contain("Port Nova").And.Contain("Traveler's Lodge");
        TravelerText.BuildArrival(detail, " ").Should().Contain("the city");
    }

    [Test]
    public void TheLoginNotice_ListsEveryTravelerWithItsCityOrTheHub()
    {
        var notice = TravelerText.BuildLoginNotice(new[]
        {
            new TravelerLocation(TravelerType.BeastTrader, "Port Nova", "Viscara - Wildlands"),
            new TravelerLocation(TravelerType.Prospector, "Second City", string.Empty),
            new TravelerLocation(TravelerType.FashionMerchant, string.Empty, "Viscara - Veles - Starport"),
            new TravelerLocation(TravelerType.InformationBroker, string.Empty, string.Empty)
        });

        notice.Should().StartWith("Travelers this week:");
        notice.Split('\n').Should().HaveCount(5);
        notice.Should().Contain("visiting Port Nova (Viscara - Wildlands)")
            .And.Contain("visiting Second City")
            .And.Contain("waiting at Viscara - Veles - Starport")
            .And.Contain("waiting at the Veles starport");
    }

    [Test]
    public void WithNoTravelersPlaced_ThereIsNoLoginNotice()
    {
        TravelerText.BuildLoginNotice(Array.Empty<TravelerLocation>()).Should().BeEmpty();
        TravelerText.BuildLoginNotice(null).Should().BeEmpty();
    }

    [Test]
    public void TheBrokersNews_LeavesOutTheBrokerHimself()
    {
        var news = TravelerText.BuildOthersNews(TravelerType.InformationBroker, new[]
        {
            new TravelerLocation(TravelerType.BeastTrader, "Port Nova", string.Empty),
            new TravelerLocation(TravelerType.InformationBroker, "Second City", string.Empty)
        });

        news.Should().Contain("Port Nova").And.NotContain("Second City");
        TravelerText.BuildOthersNews(TravelerType.InformationBroker, Array.Empty<TravelerLocation>()).Should().Contain("not heard");
    }

    [Test]
    public void TheLodgeLostLine_SaysWhereTheTravelerWentAndUntilWhen()
    {
        var text = TravelerText.BuildLodgeLost(TravelerCatalog.Get(TravelerType.Prospector));

        text.Should().Contain("Traveler's Lodge is gone").And.Contain("until next week").And.Contain(TravelerRules.HubName);
    }

    [Test]
    public void EverythingPlayersRead_IsPlainAscii()
    {
        var detail = TravelerCatalog.Get(TravelerType.InformationBroker);
        var texts = new List<string>
        {
            TravelerText.BuildArrival(detail, "Port Nova"),
            TravelerText.BuildLodgeLost(detail),
            TravelerText.BuildLoginNotice(new[] { new TravelerLocation(TravelerType.BeastTrader, "Port Nova", "Area") }),
            TravelerText.BuildOthersNews(TravelerType.Prospector, Array.Empty<TravelerLocation>()),
            TravelerRules.GetLodgePlacementError(StructureType.TravelersLodge, 1)
        };

        texts.Should().OnlyContain(text => System.Text.RegularExpressions.Regex.IsMatch(text, "^[\\x20-\\x7E\\n]+$"));
    }

    #endregion
}
