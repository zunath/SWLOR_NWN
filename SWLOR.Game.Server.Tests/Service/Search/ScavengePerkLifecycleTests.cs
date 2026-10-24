using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.ScavengeService;
using static SWLOR.Game.Server.Tests.Service.Search.SearchTestData;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>
/// Covers how a visit to a scavenge site carries the scavenger's Treasure Seeker perks: the Jackpot weight that only the first Dig
/// Deeper find of a board gets, the dig and find weights of every other find, and rebuilding the board's rules with the perks and style
/// as they are now (<see cref="ScavengeLifecycle.RefreshEffects"/>) until the first action.
/// </summary>
public class ScavengePerkLifecycleTests
{
    private const uint Player = 100;
    private const uint Site = 500;
    private const int SiteLevel = 3;
    private const int DeltaXp = 600;

    // A rank-0 scavenger: the layout takes one roll for each promising spot, and these pick the first spots, so the top left and the top
    // are promising. The center is ordinary ground (8%); the first dig there risks 30% and finds 14%, the second risks 50% and finds
    // 10%, and a roll of 1 finds on every dig.
    private const int Rank = 0;

    private static readonly int PromisingCount = SearchSetup.ForRank(Rank, SiteLevel).PromisingSpots;
    private static readonly int[] LayoutRolls = Enumerable.Range(0, PromisingCount).ToArray();

    private FakeScavengeWorld _world;

    [SetUp]
    public void SetUp()
    {
        _world = new FakeScavengeWorld();
    }

    /// <summary>The ruby is rare and first, so a table roll under its weight picks it: ruby 1 and ordinary fiber 20.</summary>
    private static LootTable RubyFirst() => new()
    {
        new LootTableItem("ruby", 1, 1, true),
        new LootTableItem("fiberp_ruined", 1, 20, false)
    };

    private static ScavengeRequest Request(SearchEffects effects, LootTable table = null, uint site = Site) =>
        new(Player, site, SiteLevel, "TEST_TABLE", table ?? RubyFirst(), Rank, DeltaXp, 0, 0, effects);

    /// <summary>A lifecycle whose dice are the layout rolls and then the rolls given, one for each chance the visit takes.</summary>
    private ScavengeLifecycle Lifecycle(params int[] rolls) =>
        new(_world, new ScriptedMinigameRandom(LayoutRolls.Concat(rolls).ToArray()));

    private void NextTurn() => _world.UtcNow = _world.UtcNow.AddSeconds(SearchRules.ActionSeconds);

    /// <summary>The session's Jackpot flag, which is internal to the server assembly.</summary>
    private static bool JackpotSpent(ScavengeSession session) =>
        (bool)typeof(ScavengeSession)
            .GetProperty("JackpotSpent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .GetValue(session);

    private void Act(ScavengeLifecycle lifecycle, ScavengeSession session, string actionId, bool nextTurnFirst = true)
    {
        if (nextTurnFirst)
            NextTurn();

        lifecycle.Act(session, actionId).Performed.Should().BeTrue(actionId);
    }

    [Test]
    public void TheJackpotWeightGoesToTheFirstDigFindOnly_OnTopOfTheDigBonus_AndLaterDigsKeepOnlyTheDigBonus()
    {
        // The dig multiplier is m + 2 with the dig bonus, and m + 10 on the first find with the Jackpot's 8 on top. The ruby weighs 1,
        // so a table roll under the multiplier picks the ruby (it is first), and the ordinary fiber takes the rest.
        var multiplier = ScavengeLoot.DeeperWeightMultiplier;
        var later = multiplier + 2;
        var first = multiplier + 10;
        var effects = new SearchEffects(DeeperWeightBonus: 2, JackpotWeightBonus: 8);
        var lifecycle = Lifecycle(
            100,
            1, first - 1, 1,
            1, later, 1,
            1, later - 1, 1);
        var session = lifecycle.Start(Request(effects));

        Act(lifecycle, session, SearchAt(Center), nextTurnFirst: false);
        JackpotSpent(session).Should().BeFalse("a search that found nothing spends nothing");
        Act(lifecycle, session, Dig(Center));
        JackpotSpent(session).Should().BeTrue();
        Act(lifecycle, session, Dig(Center));
        Act(lifecycle, session, Dig(Center));

        // First find: the table roll m + 9 is under m + 10 (a ruby), though it is over the dig bonus's m + 2 alone.
        // Second find: the roll m + 2 is the first point past the dig bonus's ruby (an ordinary fiber), so no Jackpot leaked in.
        // Third find: the roll m + 1 is under m + 2 (a ruby), so the dig bonus still applies to later finds.
        session.Finds.Select(find => find.Resref).Should().Equal("ruby", "fiberp_ruined", "ruby");
        session.Finds.Should().OnlyContain(find => find.Deeper);
    }

    [Test]
    public void WithNoJackpotBonus_TheFirstDigFindRollsLikeTheRest()
    {
        var multiplier = ScavengeLoot.DeeperWeightMultiplier;
        var later = multiplier + 2;
        var effects = new SearchEffects(DeeperWeightBonus: 2);
        var lifecycle = Lifecycle(
            100,
            1, multiplier + 9, 1,
            1, later, 1);
        var session = lifecycle.Start(Request(effects));

        Act(lifecycle, session, SearchAt(Center), nextTurnFirst: false);
        Act(lifecycle, session, Dig(Center));
        Act(lifecycle, session, Dig(Center));

        session.Finds.Select(find => find.Resref).Should().Equal("fiberp_ruined", "fiberp_ruined");
    }

    [Test]
    public void AnOrdinaryFind_DoesNotSpendTheJackpot()
    {
        // The top left is promising (29%): a roll of 1 finds, the table roll picks the fiber (the ruby takes only roll 0 of 21), and the
        // quantity is 1. The center search misses, and the first dig there finds with a roll under the Jackpot multiplier: a ruby.
        var jackpotMultiplier = ScavengeLoot.DeeperWeightMultiplier + 8;
        var lifecycle = Lifecycle(
            1, 19, 1,
            100,
            1, jackpotMultiplier - 1, 1);
        var session = lifecycle.Start(Request(new SearchEffects(JackpotWeightBonus: 8)));

        Act(lifecycle, session, SearchAt(TopLeft), nextTurnFirst: false);
        JackpotSpent(session).Should().BeFalse("only a Dig Deeper find spends the Jackpot");
        Act(lifecycle, session, SearchAt(Center));
        Act(lifecycle, session, Dig(Center));

        session.Finds.Should().Equal(
            new ScavengeFind("fiberp_ruined", 1, false),
            new ScavengeFind("ruby", 1, true));
    }

    [Test]
    public void ADigThatFindsNothingOrSpoilsTheSpot_DoesNotSpendTheJackpot()
    {
        // The center: a miss, a dig that finds nothing (roll 20 of 15 to 70), then a dig that spoils it (roll 100 of 51 to 100 at the
        // second dig's 50% risk). The left spot: a miss, then a dig that finds, whose table roll is under the Jackpot multiplier.
        var jackpotMultiplier = ScavengeLoot.DeeperWeightMultiplier + 8;
        var lifecycle = Lifecycle(
            100,
            20,
            100,
            100,
            1, jackpotMultiplier - 1, 1);
        var session = lifecycle.Start(Request(new SearchEffects(JackpotWeightBonus: 8)));

        Act(lifecycle, session, SearchAt(Center), nextTurnFirst: false);
        Act(lifecycle, session, Dig(Center));
        Act(lifecycle, session, Dig(Center));
        session.Engine.IsSpoiled(Center).Should().BeTrue();
        session.Finds.Should().BeEmpty();
        JackpotSpent(session).Should().BeFalse();
        Act(lifecycle, session, SearchAt(Left));
        Act(lifecycle, session, Dig(Left));

        session.Finds.Should().Equal(new ScavengeFind("ruby", 1, true));
        JackpotSpent(session).Should().BeTrue();
    }

    [Test]
    public void EachBoardGetsItsOwnJackpot()
    {
        var jackpotMultiplier = ScavengeLoot.DeeperWeightMultiplier + 8;
        const uint otherSite = 501;
        var lifecycle = Lifecycle(
            100,
            1, jackpotMultiplier - 1, 1,
            0, 1,
            100,
            1, jackpotMultiplier - 1, 1);
        var effects = new SearchEffects(JackpotWeightBonus: 8);
        var first = lifecycle.Start(Request(effects));
        Act(lifecycle, first, SearchAt(Center), nextTurnFirst: false);
        Act(lifecycle, first, Dig(Center));
        lifecycle.Abandon(first, ScavengeEnd.WindowClosed);

        var second = lifecycle.Start(Request(effects, site: otherSite));
        Act(lifecycle, second, SearchAt(Center), nextTurnFirst: false);
        Act(lifecycle, second, Dig(Center));

        first.Finds.Should().Equal(new ScavengeFind("ruby", 1, true));
        second.Finds.Should().Equal(new ScavengeFind("ruby", 1, true));
        JackpotSpent(second).Should().BeTrue();
    }

    [Test]
    public void TheFindBonusWeightsOrdinaryFindsOnly_AndADigKeepsItsOwnMultiplier()
    {
        // With a find bonus of 1 an ordinary find weighs the ruby 2 against 20: a table roll of 1 picks it, where it would pick the
        // fiber without the bonus. A dig ignores it: the dig multiplier alone weighs the ruby m, and a table roll of m picks the fiber.
        var multiplier = ScavengeLoot.DeeperWeightMultiplier;
        var lifecycle = Lifecycle(
            1, 1, 1,
            100,
            1, multiplier, 1);
        var session = lifecycle.Start(Request(new SearchEffects(FindWeightBonus: 1)));

        Act(lifecycle, session, SearchAt(TopLeft), nextTurnFirst: false);
        Act(lifecycle, session, SearchAt(Center));
        Act(lifecycle, session, Dig(Center));

        session.Finds.Should().Equal(
            new ScavengeFind("ruby", 1, false),
            new ScavengeFind("fiberp_ruined", 1, true));
    }

    [Test]
    public void StartingAVisitWithPerks_BuildsTheBoardWithThem()
    {
        var effects = new SearchEffects(DigRiskPercent: 85, SearchChanceDelta: 1, DeeperWeightBonus: 2);
        var lifecycle = Lifecycle();

        var session = lifecycle.Start(Request(effects));

        session.Engine.Setup.Effects.Should().BeSameAs(effects);
        session.Engine.Setup.OrdinaryChance.Should().Be(SearchRules.OrdinaryChance + 1);
        session.Engine.Setup.DigRisk(0).Should().Be(SearchSetup.ForRank(Rank, SiteLevel, effects).DigRisk(0));
        session.Engine.Setup.DigRisk(0).Should().BeLessThan(SearchSetup.ForRank(Rank, SiteLevel).DigRisk(0));
        session.Request.Effects.Should().BeSameAs(effects);
    }

    [Test]
    public void RefreshingTheEffectsBeforeTheFirstAction_RebuildsTheRules_AndKeepsTheHiddenLayout()
    {
        var random = new ScriptedMinigameRandom(4, 4);
        var lifecycle = new ScavengeLifecycle(_world, random);
        var table = RubyFirst();
        var session = lifecycle.Start(Request(null, table));
        var layout = session.Engine.Board.Promising;
        var before = session.Engine;
        var effects = new SearchEffects(DigRiskPercent: 85, SearchChanceDelta: 1, JackpotWeightBonus: 8);

        var refreshed = lifecycle.RefreshEffects(session, effects);

        refreshed.Should().BeTrue();
        random.Calls.Should().Be(PromisingCount, "the layout is kept, not rolled again");
        session.Engine.Should().NotBeSameAs(before);
        session.Engine.Board.Promising.Should().Be(layout);
        SearchBoard.PopCount(layout).Should().Be(session.Engine.Setup.PromisingSpots);
        session.Engine.Setup.Effects.Should().BeSameAs(effects);
        session.Engine.Setup.OrdinaryChance.Should().Be(SearchRules.OrdinaryChance + 1);
        session.Engine.Setup.DigRisk(0).Should().BeLessThan(before.Setup.DigRisk(0));
        session.Request.Effects.Should().BeSameAs(effects);
        session.Request.Table.Should().BeSameAs(table, "only the perks change");
        session.Request.GatheringRank.Should().Be(Rank);
        session.Request.SiteLevel.Should().Be(SiteLevel);
        session.Request.DeltaXp.Should().Be(DeltaXp);
        session.Engine.Turn.Should().Be(0);
        session.Engine.SearchesLeft.Should().Be(session.Engine.Setup.Searches);
        lifecycle.GetActive(Player).Should().BeSameAs(session);
        lifecycle.IsInUse(Site).Should().BeTrue();
        lifecycle.CanQuickSearch(session).Should().BeTrue("the scavenger has not acted yet");
    }

    [Test]
    public void ARefreshCanBeRepeatedUntilTheFirstAction_AndTheLayoutNeverChanges()
    {
        var random = new ScriptedMinigameRandom(4, 4);
        var lifecycle = new ScavengeLifecycle(_world, random);
        var session = lifecycle.Start(Request(null));
        var layout = session.Engine.Board.Promising;

        lifecycle.RefreshEffects(session, new SearchEffects(SearchChanceDelta: 1)).Should().BeTrue();
        lifecycle.RefreshEffects(session, new SearchEffects(SearchChanceDelta: -3)).Should().BeTrue();
        lifecycle.RefreshEffects(session, null).Should().BeTrue();

        session.Engine.Board.Promising.Should().Be(layout);
        session.Engine.Setup.Effects.Should().Be(SearchEffects.None, "no effects means the plain board");
        session.Engine.Setup.OrdinaryChance.Should().Be(SearchRules.OrdinaryChance);
        session.Request.Effects.Should().BeNull();
        random.Calls.Should().Be(PromisingCount);
    }

    [Test]
    public void TheBoardRefreshedWithPerks_UsesThemOnTheFirstSearch()
    {
        // The center is ordinary ground: 8%, or 9% after a refresh with a point more. A roll of 9 finds only on the refreshed board.
        var roll = SearchRules.OrdinaryChance + 1;
        var lifecycle = Lifecycle(roll, 19, 1);
        var session = lifecycle.Start(Request(null));
        lifecycle.RefreshEffects(session, new SearchEffects(SearchChanceDelta: 1)).Should().BeTrue();

        lifecycle.Act(session, SearchAt(Center)).Performed.Should().BeTrue();

        session.Engine.Last.Result.Should().Be(SearchEventResult.Found);
        session.Finds.Should().Equal(new ScavengeFind("fiberp_ruined", 1, false));
    }

    [Test]
    public void TheBoardRefreshedWithTheJackpot_GivesItToTheFirstDigFind()
    {
        var jackpotMultiplier = ScavengeLoot.DeeperWeightMultiplier + 8;
        var lifecycle = Lifecycle(100, 1, jackpotMultiplier - 1, 1);
        var session = lifecycle.Start(Request(null));
        lifecycle.RefreshEffects(session, new SearchEffects(JackpotWeightBonus: 8)).Should().BeTrue();

        Act(lifecycle, session, SearchAt(Center), nextTurnFirst: false);
        Act(lifecycle, session, Dig(Center));

        session.Finds.Should().Equal(new ScavengeFind("ruby", 1, true));
    }

    [Test]
    public void ARefreshAfterAnAction_ChangesNothing_AndSaysSo()
    {
        var lifecycle = Lifecycle(100);
        var session = lifecycle.Start(Request(null));
        Act(lifecycle, session, SearchAt(Center), nextTurnFirst: false);
        var engine = session.Engine;
        var request = session.Request;

        var refreshed = lifecycle.RefreshEffects(session, new SearchEffects(JackpotWeightBonus: 8, SearchChanceDelta: 5));

        refreshed.Should().BeFalse("the visit keeps the perks it began with");
        session.Engine.Should().BeSameAs(engine);
        session.Request.Should().BeSameAs(request);
        session.Engine.Setup.Effects.Should().Be(SearchEffects.None);
        session.Engine.SearchesSpent.Should().Be(1, "the action stays spent");
    }

    [Test]
    public void ARefreshAfterQuickSearch_ChangesNothing_AndSaysSo()
    {
        // The same rolls as the lifecycle tests' Quick Search: 50 misses the second attempt, the d20 is 12, the DC rise is 2, the table
        // roll picks the first entry and the quantity is 2.
        var table = new LootTable
        {
            new LootTableItem("fiberp_ruined", 3, 50, false),
            new LootTableItem("fiberp_flawed", 1, 5, false),
            new LootTableItem("ruby", 1, 1, true)
        };
        var lifecycle = Lifecycle(50, 12, 2, 0, 2);
        var session = lifecycle.Start(Request(null, table));
        var engine = session.Engine;
        lifecycle.QuickSearch(session).Should().NotBeNull();

        var refreshed = lifecycle.RefreshEffects(session, new SearchEffects(JackpotWeightBonus: 8));

        refreshed.Should().BeFalse();
        session.Engine.Should().BeSameAs(engine);
        session.Request.Effects.Should().BeNull();
        session.Mode.Should().Be(ScavengeMode.Quick);
    }

    [Test]
    public void ARefreshOfAVisitThatIsOver_ChangesNothing()
    {
        var lifecycle = Lifecycle();
        var session = lifecycle.Start(Request(null));
        lifecycle.Abandon(session, ScavengeEnd.WindowClosed);
        var engine = session.Engine;

        lifecycle.RefreshEffects(session, new SearchEffects(SearchChanceDelta: 5)).Should().BeFalse();

        session.Engine.Should().BeSameAs(engine);
        session.Engine.Setup.Effects.Should().Be(SearchEffects.None);
    }

    [Test]
    public void ARefreshNeedsASession()
    {
        var lifecycle = Lifecycle();

        var noSession = () => lifecycle.RefreshEffects(null, SearchEffects.None);

        noSession.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void QuickSearchIgnoresThePerksTheBoardUses()
    {
        // The table roll of 3 is the ruby only with a find bonus of 5 (the ruby weighs 6 then, against 1): Quick Search rolls the table as
        // it always did and picks the fiber.
        var lifecycle = Lifecycle(50, 12, 2, 3, 1);
        var session = lifecycle.Start(Request(new SearchEffects(FindWeightBonus: 5, DeeperWeightBonus: 5, JackpotWeightBonus: 8)));

        var result = lifecycle.QuickSearch(session);

        result.Should().NotBeNull();
        result.Finds.Should().Equal(new ScavengeFind("fiberp_ruined", 1, false));
    }
}
