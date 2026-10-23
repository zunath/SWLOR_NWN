using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.ScavengeService;
using static SWLOR.Game.Server.Tests.Service.Search.SearchTestData;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>
/// Covers a visit to a scavenge site from the moment a player opens it: the once-per-site rule, every action's XP, the loot of a find,
/// the pace, leaving early, the end of the board, and Quick Search.
/// </summary>
public class ScavengeLifecycleTests
{
    private const uint Player = 100;
    private const uint Site = 500;
    private const int SiteLevel = 3;
    private const int DeltaXp = 600;

    // A rank-0 scavenger: the layout takes one roll for each promising spot, and these pick the first spots, so the top left and the top
    // are promising (picking spot i with the i-th roll leaves the shuffle as it was).
    private const int Rank = 0;

    private static readonly int PromisingCount = SearchSetup.ForRank(Rank, SiteLevel).PromisingSpots;
    private static readonly int[] LayoutRolls = Enumerable.Range(0, PromisingCount).ToArray();

    private FakeScavengeWorld _world;

    [SetUp]
    public void SetUp()
    {
        _world = new FakeScavengeWorld();
    }

    private static LootTable Table(bool withRare = true)
    {
        var table = new LootTable
        {
            new LootTableItem("fiberp_ruined", 3, 50, false),
            new LootTableItem("fiberp_flawed", 1, 5, false)
        };
        if (withRare)
            table.Add(new LootTableItem("ruby", 1, 1, true));

        return table;
    }

    private static ScavengeRequest Request(uint player = Player, uint site = Site, int rank = Rank, int treasureHunter = 0, int creditFinder = 0, LootTable table = null) =>
        new(player, site, SiteLevel, "TEST_TABLE", table ?? Table(), rank, DeltaXp, treasureHunter, creditFinder);

    /// <summary>A lifecycle whose dice are the layout rolls and then the rolls given, one for each chance the visit takes.</summary>
    private ScavengeLifecycle Lifecycle(params int[] rolls) =>
        new(_world, new ScriptedMinigameRandom(LayoutRolls.Concat(rolls).ToArray()));

    private void NextTurn() => _world.UtcNow = _world.UtcNow.AddSeconds(SearchRules.ActionSeconds);

    [Test]
    public void StartingAVisit_MarksTheSiteSearchedFirst_ThenOpensTheBoard()
    {
        var lifecycle = Lifecycle();

        var session = lifecycle.Start(Request());

        session.Should().NotBeNull();
        _world.Log.Should().Equal("mark", "open");
        _world.Searched.Should().Equal(Site);
        session.Mode.Should().Be(ScavengeMode.Board);
        session.Engine.Setup.Rank.Should().Be(Rank);
        session.Engine.Setup.SiteLevel.Should().Be(SiteLevel);
        lifecycle.IsActive(session).Should().BeTrue();
        lifecycle.IsInUse(Site).Should().BeTrue();
        lifecycle.GetActive(Player).Should().BeSameAs(session);
        _world.ScheduledCount.Should().Be(1, "the first check is scheduled");
    }

    [Test]
    public void ASiteIsSearchedForGood_EvenWhenTheVisitIsLeftAtOnce()
    {
        var lifecycle = Lifecycle();
        var session = lifecycle.Start(Request());

        lifecycle.Abandon(session, ScavengeEnd.WindowClosed);

        _world.Searched.Should().Equal(new[] { Site }, "it was marked when the visit began, and nothing unmarks it");
        lifecycle.IsInUse(Site).Should().BeFalse();
        session.Settled.Should().BeTrue();
    }

    [Test]
    public void AnotherScavengerAtTheSite_IsTurnedAway_AndTheSameScavengerGetsTheirVisitBack()
    {
        var lifecycle = Lifecycle();
        var first = lifecycle.Start(Request());

        var other = lifecycle.Start(Request(player: 101));
        var again = lifecycle.Start(Request());

        other.Should().BeNull();
        _world.Messages.Should().Equal(ScavengeLifecycle.SiteInUseMessage);
        again.Should().BeSameAs(first, "opening the site you are searching carries on with that visit");
        _world.Searched.Should().Equal(Site);
    }

    [Test]
    public void AScavengerWhoStartsAnotherVisit_EndsTheFirstOne()
    {
        var lifecycle = Lifecycle(0, 1);
        var first = lifecycle.Start(Request());

        var second = lifecycle.Start(Request(site: 501));

        first.Settled.Should().BeTrue();
        first.Result.End.Should().Be(ScavengeEnd.Replaced);
        lifecycle.GetActive(Player).Should().BeSameAs(second);
        lifecycle.IsInUse(Site).Should().BeFalse();
        lifecycle.IsInUse(501).Should().BeTrue();
    }

    [Test]
    public void ASearchThatFinds_PaysFullXpAtOnce_AndRollsTheTableForTheFind()
    {
        // The top left is promising (45%): roll 1 finds. The table roll picks the first entry, and the quantity roll picks 2.
        var lifecycle = Lifecycle(1, 0, 2);
        var session = lifecycle.Start(Request());

        var step = lifecycle.Act(session, SearchAt(0));

        step.Performed.Should().BeTrue();
        step.Result.Should().BeNull();
        _world.Xp.Should().Equal(SearchRules.AttemptXp(DeltaXp, SiteLevel, true));
        session.XpPaid.Should().Be(SearchRules.AttemptXp(DeltaXp, SiteLevel, true));
        session.Finds.Should().Equal(new ScavengeFind("fiberp_ruined", 2, false));
        _world.Created.Should().BeEmpty("what a site gave goes in its inventory when the visit ends");
    }

    [Test]
    public void ASearchThatFindsNothing_PaysAQuarterOfTheXp_AndRollsNoLoot()
    {
        var lifecycle = Lifecycle(100);
        var session = lifecycle.Start(Request());

        lifecycle.Act(session, SearchAt(0)).Performed.Should().BeTrue();

        _world.Xp.Should().Equal(SearchRules.AttemptXp(DeltaXp, SiteLevel, false));
        SearchRules.AttemptXp(DeltaXp, SiteLevel, false).Should().Be(
            CraftMinigameXp.ScaleBaseXp(DeltaXp / 4, SearchRules.XpMultiplierForLevel(SiteLevel)));
        session.Finds.Should().BeEmpty();
    }

    [Test]
    public void ASiftPaysTheQuarterOfAnActionThatFindsNothing_AndFindsNothing()
    {
        var lifecycle = Lifecycle();
        var session = lifecycle.Start(Request());

        lifecycle.Act(session, Sift(Center)).Performed.Should().BeTrue();

        _world.Xp.Should().Equal(SearchRules.AttemptXp(DeltaXp, SiteLevel, false));
        session.Finds.Should().BeEmpty();
        session.Engine.Last.Kind.Should().Be(SearchEventKind.Sift);
    }

    [Test]
    public void ADigThatFinds_RollsTheTableWithTheRareEntriesFavoured_AndPaysAsAnAttempt()
    {
        // The center is ordinary: roll 100 finds nothing. The dig rolls 1: a find. The fiber is 50 of the weight and the flawed fiber 5, so a table roll of 55 is the first point of the ruby, whatever its Dig Deeper multiplier.
        var lifecycle = Lifecycle(100, 1, 55, 1);
        var session = lifecycle.Start(Request());
        lifecycle.Act(session, SearchAt(Center));
        NextTurn();

        lifecycle.Act(session, Dig(Center)).Performed.Should().BeTrue();

        session.Finds.Should().Equal(new ScavengeFind("ruby", 1, true));
        _world.Xp.Should().Equal(SearchRules.AttemptXp(DeltaXp, SiteLevel, false), SearchRules.AttemptXp(DeltaXp, SiteLevel, true));
    }

    [Test]
    public void ADigThatSpoilsTheSpot_PaysTheQuarterAndFindsNothing()
    {
        var lifecycle = Lifecycle(100, 100);
        var session = lifecycle.Start(Request());
        lifecycle.Act(session, SearchAt(Center));
        NextTurn();

        lifecycle.Act(session, Dig(Center));

        session.Engine.IsSpoiled(Center).Should().BeTrue();
        session.Finds.Should().BeEmpty();
        _world.Xp.Should().Equal(Enumerable.Repeat(SearchRules.AttemptXp(DeltaXp, SiteLevel, false), 2));
    }

    [Test]
    public void TheXpOfAnAttemptIsPaidWhetherOrNotTheXpIsZero_ButAZeroIsNeverSent()
    {
        var lifecycle = Lifecycle(100);
        var session = lifecycle.Start(new ScavengeRequest(Player, Site, SiteLevel, "T", Table(), Rank, 0, 0, 0));

        lifecycle.Act(session, SearchAt(Center));

        _world.Xp.Should().BeEmpty("a site far below the scavenger's rank pays nothing");
    }

    [Test]
    public void TheLastSearchSettlesTheVisit_PuttingEveryFindInTheSite_AndOpeningItForTheScavenger()
    {
        // Five searches of ordinary ground, spots 4 to 8: spots 4 and 6 find (roll 1 of 10%), the others do not.
        var lifecycle = Lifecycle(
            1, 0, 2,
            100,
            1, 52, 1,
            100,
            100);
        var session = lifecycle.Start(Request());

        ScavengeStep last = null;
        foreach (var spot in new[] { 4, 5, 6, 7, 8 })
        {
            last = lifecycle.Act(session, SearchAt(spot));
            last.Performed.Should().BeTrue();
            NextTurn();
        }

        last.Result.Should().NotBeNull("the fifth search spends the last one");
        last.Result.End.Should().Be(ScavengeEnd.Completed);
        session.Settled.Should().BeTrue();
        _world.Created.Should().Equal((Site, "fiberp_ruined", 2), (Site, "fiberp_flawed", 1));
        _world.BonusLoot.Should().Equal(new[] { (Site, Player), (Site, Player) }, "the extra loot tables roll once for every search that found");
        _world.Opened.Should().Equal((Site, 0d));
        _world.Destroyed.Should().BeEmpty();
        _world.WindowOpen.Should().BeFalse("the board closes when the visit ends");
        _world.Messages.Should().ContainSingle().Which.Should().StartWith("You finish searching. You found 2x Item fiberp_ruined, 1x Item fiberp_flawed.");
        lifecycle.IsInUse(Site).Should().BeFalse();
        session.Result.Finds.Should().HaveCount(2);
    }

    [Test]
    public void ARareFindDoesNotRollTheSitesExtraLootTables()
    {
        var lifecycle = Lifecycle(100, 1, 55, 1);
        var session = lifecycle.Start(Request());
        lifecycle.Act(session, SearchAt(Center));
        NextTurn();
        lifecycle.Act(session, Dig(Center));

        lifecycle.Abandon(session, ScavengeEnd.WindowClosed);

        _world.Created.Should().Equal((Site, "ruby", 1));
        _world.BonusLoot.Should().BeEmpty();
    }

    [Test]
    public void AVisitThatFoundNothing_RemovesTheSite_AndNeverOpensIt()
    {
        var lifecycle = Lifecycle(100, 100, 100, 100, 100);
        var session = lifecycle.Start(Request());

        foreach (var spot in new[] { 4, 5, 6, 7, 8 })
        {
            lifecycle.Act(session, SearchAt(spot));
            NextTurn();
        }

        _world.Created.Should().BeEmpty();
        _world.Opened.Should().BeEmpty();
        _world.Destroyed.Should().Equal((Site, 0d));
        _world.Messages.Should().ContainSingle().Which.Should().Be("You finish searching. You found nothing.");
    }

    [Test]
    public void TheFirstActionIsFree_ButTheNextWaitsForThePace()
    {
        var lifecycle = Lifecycle(100, 100);
        var session = lifecycle.Start(Request());

        lifecycle.Act(session, SearchAt(Center)).Performed.Should().BeTrue();
        var tooSoon = lifecycle.Act(session, SearchAt(Right));
        _world.UtcNow = _world.UtcNow.AddSeconds(SearchRules.ActionSeconds - 1);
        var stillTooSoon = lifecycle.Act(session, SearchAt(Right));

        tooSoon.Performed.Should().BeFalse();
        tooSoon.Message.Should().Be("Take your time. Wait 6 more seconds before your next action.");
        stillTooSoon.Message.Should().Be("Take your time. Wait 1 more second before your next action.");
        session.Engine.SearchesSpent.Should().Be(1, "a refused action spends nothing");

        _world.UtcNow = _world.UtcNow.AddSeconds(1);
        lifecycle.Act(session, SearchAt(Right)).Performed.Should().BeTrue();
        lifecycle.SecondsUntilNextAction(session).Should().Be(SearchRules.ActionSeconds);
    }

    [Test]
    public void AnActionTheBoardRefuses_ChangesNothing_AndDoesNotStartThePace()
    {
        var lifecycle = Lifecycle();
        var session = lifecycle.Start(Request());

        var step = lifecycle.Act(session, Dig(Center));

        step.Performed.Should().BeFalse();
        step.Message.Should().Be("Search the spot first.");
        lifecycle.SecondsUntilNextAction(session).Should().Be(0);
        _world.Xp.Should().BeEmpty();
    }

    [Test]
    public void LeavingEarly_KeepsTheFindsSoFar_AndLosesTheSearchesNotSpent()
    {
        var lifecycle = Lifecycle(1, 0, 3);
        var session = lifecycle.Start(Request());
        lifecycle.Act(session, SearchAt(0));

        lifecycle.Abandon(session, ScavengeEnd.WindowClosed);

        session.Result.End.Should().Be(ScavengeEnd.WindowClosed);
        session.Engine.Status.Should().Be(MinigameStatus.InProgress);
        session.Engine.SearchesLeft.Should().Be(session.Engine.Setup.Searches - 1);
        _world.Created.Should().Equal((Site, "fiberp_ruined", 3));
        _world.Opened.Should().HaveCount(1);
        _world.Messages.Should().ContainSingle().Which.Should().StartWith("You stop searching. You found 3x Item fiberp_ruined");
        _world.Log.Should().NotContain("close", "the window is already closing");
    }

    [Test]
    public void AVisitCanBeSettledOnce_AndALaterActionOrAbandonChangesNothing()
    {
        var lifecycle = Lifecycle(1, 0, 3);
        var session = lifecycle.Start(Request());
        lifecycle.Act(session, SearchAt(0));
        lifecycle.Abandon(session, ScavengeEnd.WindowClosed);
        var created = _world.Created.Count;
        var messages = _world.Messages.Count;

        lifecycle.Abandon(session, ScavengeEnd.Left);
        var step = lifecycle.Act(session, SearchAt(Center));

        step.Performed.Should().BeFalse();
        _world.Created.Should().HaveCount(created);
        _world.Messages.Should().HaveCount(messages);
        session.Result.End.Should().Be(ScavengeEnd.WindowClosed);
    }

    [TestCase(ScavengeEnd.Moved)]
    [TestCase(ScavengeEnd.Combat)]
    [TestCase(ScavengeEnd.Died)]
    [TestCase(ScavengeEnd.Left)]
    [TestCase(ScavengeEnd.WindowClosed)]
    public void TheScavengerLeaving_EndsTheVisit_WhetherTheyMoveFightDieLogOutOrCloseTheWindow(ScavengeEnd reason)
    {
        var lifecycle = Lifecycle();
        var session = lifecycle.Start(Request());
        switch (reason)
        {
            case ScavengeEnd.Moved:
                _world.Distance = ScavengeLifecycle.MaxDistance + 1;
                break;
            case ScavengeEnd.Combat:
                _world.IsInCombat = true;
                break;
            case ScavengeEnd.Died:
                _world.IsDead = true;
                break;
            case ScavengeEnd.Left:
                _world.IsValid = false;
                break;
            default:
                _world.WindowOpen = false;
                break;
        }

        _world.RunNextScheduled();

        session.Settled.Should().BeTrue();
        session.Result.End.Should().Be(reason);
        lifecycle.IsInUse(Site).Should().BeFalse();
        _world.ScheduledCount.Should().Be(0, "an ended visit stops checking");
    }

    [Test]
    public void AClickBehindALeftVisit_CannotBeatTheCheck()
    {
        var lifecycle = Lifecycle(100);
        var session = lifecycle.Start(Request());
        _world.Distance = ScavengeLifecycle.MaxDistance + 1;

        var step = lifecycle.Act(session, SearchAt(Center));

        step.Performed.Should().BeFalse();
        session.Result.End.Should().Be(ScavengeEnd.Moved);
        _world.Xp.Should().BeEmpty();
    }

    [Test]
    public void ADeadScavenger_LeavesTheirFindsInTheSite_WithoutOpeningItOrTalkingToThem()
    {
        var lifecycle = Lifecycle(1, 0, 3);
        var session = lifecycle.Start(Request());
        lifecycle.Act(session, SearchAt(0));
        _world.IsDead = true;

        lifecycle.Abandon(session, ScavengeEnd.Died);

        _world.Created.Should().Equal((Site, "fiberp_ruined", 3));
        _world.Opened.Should().BeEmpty();
        _world.Destroyed.Should().BeEmpty();
        _world.Messages.Should().BeEmpty("nothing is said on dying");
    }

    [Test]
    public void ALoggedOutScavenger_LeavesTheirFindsInTheSite_AndRollsNoExtraLoot()
    {
        var lifecycle = Lifecycle(1, 0, 3);
        var session = lifecycle.Start(Request());
        lifecycle.Act(session, SearchAt(0));
        _world.IsValid = false;

        lifecycle.Abandon(session, ScavengeEnd.Left);

        _world.Created.Should().Equal((Site, "fiberp_ruined", 3));
        _world.BonusLoot.Should().BeEmpty();
        _world.Opened.Should().BeEmpty();
        _world.Messages.Should().BeEmpty();
    }

    [Test]
    public void AWindowThatCannotOpen_LeavesTheVisitToQuickSearch_SoTheSiteIsNotWasted()
    {
        _world.CanOpenWindow = false;
        // The second attempt does not happen (50 of 10%), the d20 is 15 against DC 6, the d3 is 1, then the loot rolls.
        var lifecycle = Lifecycle(50, 15, 1, 0, 1);

        var session = lifecycle.Start(Request());

        session.Mode.Should().Be(ScavengeMode.Quick);
        session.Settled.Should().BeTrue();
        session.Result.End.Should().Be(ScavengeEnd.QuickSearch);
        _world.Created.Should().HaveCount(1);
        _world.Searched.Should().Equal(Site);
    }

    [Test]
    public void AWindowThatThrowsWhenItOpens_EndsTheVisit_AndRethrows()
    {
        _world.OpeningTheWindowThrows = true;
        var lifecycle = Lifecycle();

        var start = () => lifecycle.Start(Request());

        start.Should().Throw<InvalidOperationException>();
        lifecycle.IsInUse(Site).Should().BeFalse();
        lifecycle.GetActive(Player).Should().BeNull();
    }

    [Test]
    public void QuickSearch_ResolvesTheSiteByTheOldRules_AtTheReferenceModifier()
    {
        // Rank 0 is Hard Look 1: a 10% chance of a second attempt, and 50 misses it. The d20 is 12 against DC 6: a success paying the
        // site's full XP with no multiplier. The loot picks the first entry for 2.
        var lifecycle = Lifecycle(50, 12, 2, 0, 2);
        var session = lifecycle.Start(Request());

        lifecycle.CanQuickSearch(session).Should().BeTrue();
        var result = lifecycle.QuickSearch(session);

        result.Should().NotBeNull();
        result.End.Should().Be(ScavengeEnd.QuickSearch);
        session.Mode.Should().Be(ScavengeMode.Quick);
        _world.SkillChecks.Should().Equal("Search *success*: (12 vs. DC: 6)");
        _world.Xp.Should().Equal(DeltaXp);
        session.XpPaid.Should().Be(DeltaXp);
        _world.Created.Should().Equal((Site, "fiberp_ruined", 2));
        _world.BonusLoot.Should().HaveCount(1);
        _world.Opened.Should().Equal((Site, SearchRules.QuickSearchSecondsPerAttempt));
        _world.WindowOpen.Should().BeFalse();
    }

    [Test]
    public void QuickSearch_CanMakeASecondAttempt_AgainstAHigherDc_AndPaysAQuarterOnAFailure()
    {
        // 10 is within the 10% chance of a second attempt. The first d20 is 3 (fails DC 6), the d3 is 1 (DC 7), the second d20 is 5 (fails).
        var lifecycle = Lifecycle(10, 3, 1, 5, 1);
        var session = lifecycle.Start(Request());

        lifecycle.QuickSearch(session);

        _world.SkillChecks.Should().Equal("Search *failure*: (3 vs DC: 6)", "Search *failure*: (5 vs DC: 7)");
        _world.Xp.Should().Equal(DeltaXp / 4, DeltaXp / 4);
        _world.Created.Should().BeEmpty();
        _world.Destroyed.Should().Equal((Site, 2 * SearchRules.QuickSearchSecondsPerAttempt));
        _world.Opened.Should().BeEmpty();
    }

    [Test]
    public void QuickSearch_IsOnlyAChoiceBeforeTheFirstAction()
    {
        var lifecycle = Lifecycle(100);
        var session = lifecycle.Start(Request());
        lifecycle.Act(session, SearchAt(Center));

        lifecycle.CanQuickSearch(session).Should().BeFalse();
        lifecycle.QuickSearch(session).Should().BeNull();
        session.Mode.Should().Be(ScavengeMode.Board);
        session.Settled.Should().BeFalse();
    }

    [Test]
    public void QuickSearch_CannotBeRepeated_AndTheSiteStaysSearched()
    {
        var lifecycle = Lifecycle(50, 12, 2, 0, 2);
        var session = lifecycle.Start(Request());
        lifecycle.QuickSearch(session);
        var xp = _world.Xp.Count;

        lifecycle.QuickSearch(session).Should().BeNull();

        _world.Xp.Should().HaveCount(xp);
        _world.Searched.Should().Equal(Site);
    }

    [Test]
    public void QuickSearchTakesAsLongAsTodaysAttempts_ToTheSecond()
    {
        var lifecycle = Lifecycle(50, 12, 2, 0, 2);

        lifecycle.QuickSearch(lifecycle.Start(Request()));

        SearchRules.QuickSearchSecondsPerAttempt.Should().Be(2.0, "the old search played a two second animation for an attempt");
        _world.Opened.Single().Seconds.Should().Be(2.0);
    }

    [Test]
    public void TheScavengersTreasureHunterLevelWeightsTheRareEntryOfEveryFind()
    {
        // The ruby is first with a weight of 1, the ruined fiber 50. A table roll of 20 picks the fiber, unless Treasure Hunter 3 makes
        // the ruby's weight 31, which it then picks.
        var table = new LootTable { new LootTableItem("ruby", 1, 1, true), new LootTableItem("fiberp_ruined", 3, 50, false) };

        foreach (var (level, expected) in new[] { (0, "fiberp_ruined"), (3, "ruby") })
        {
            _world = new FakeScavengeWorld();
            var lifecycle = Lifecycle(1, 20, 1);
            var session = lifecycle.Start(Request(treasureHunter: level, table: table));

            lifecycle.Act(session, SearchAt(0));

            session.Finds.Single().Resref.Should().Be(expected, $"Treasure Hunter {level}");
        }
    }

    [Test]
    public void TheScavengersCreditfinderLevelRaisesAGoldFind()
    {
        var table = new LootTable { new LootTableItem(ScavengeLoot.GoldResref, 10, 50, false) };
        var lifecycle = Lifecycle(1, 0, 10);
        var session = lifecycle.Start(Request(creditFinder: 2, table: table));

        lifecycle.Act(session, SearchAt(0));

        session.Finds.Single().Should().Be(new ScavengeFind(ScavengeLoot.GoldResref, 14, false), "10 gold and 2 x 20% more");
    }
}
