using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Tests.Service.Line;

/// <summary>
/// Covers a bite and its fight from the cast's pick of a fish to the end: what a bite spends and pays, how each ending and each way
/// of leaving settles, and that nothing a fight does changes the bait, the XP or the fishing point.
/// </summary>
public class FishingFightLifecycleTests
{
    private const uint Player = 11;
    private const uint Rod = 22;
    private const uint Point = 33;

    private FakeFishingFightWorld _world;

    [OneTimeSetUp]
    public void LoadTheXpChart()
    {
        Skill.CacheXPChartData();
    }

    [SetUp]
    public void SetUp()
    {
        _world = new FakeFishingFightWorld();
    }

    private static FishingBite Bite(
        int level = 0,
        int rank = 50,
        FishRarity rarity = FishRarity.Common,
        FishingRodType rod = FishingRodType.LuShang,
        int baitRemaining = 5,
        int pointAttempts = 7) =>
        new(
            Player,
            Rod,
            Point,
            new FishAttribute("Test Fish", "test_fish", level, true, rarity: rarity),
            rod,
            rank,
            baitRemaining,
            "Little Worm",
            pointAttempts,
            new Vector3(10f, 20f, 0f));

    private FishingFightLifecycle Lifecycle(params int[] rolls) =>
        new(_world, new ScriptedMinigameRandom(rolls.Length == 0 ? new[] { 99, 99, 99, 99, 99, 99, 99, 99 } : rolls));

    /// <summary>A fish no angler can fail to land: it is far below the rank, so three Reels always land it.</summary>
    private static FishingBite EasyFish() => Bite(level: 0, rank: 50);

    /// <summary>A hopeless fish: any Reel against its first move snaps the line and any Give Line against a Run lets it escape.</summary>
    private static FishingBite HopelessFish() => Bite(level: 80, rank: 0, rod: FishingRodType.Bamboo);

    private static void Reel(FishingFight fight)
    {
        fight.Engine.Perform(LineActionIds.Reel);
    }

    [Test]
    public void ABite_SpendsABait_TakesAFishingPointBite_PaysTheXp_AndOpensTheFight()
    {
        var lifecycle = Lifecycle();

        var fight = lifecycle.Bite(Bite(level: 20, rank: 20, baitRemaining: 5, pointAttempts: 7, rod: FishingRodType.GlassFiber));

        fight.Should().NotBeNull();
        _world.Bait[Rod].Should().Be(4);
        _world.PointAttempts[Point].Should().Be(6);
        _world.ExhaustedPoints.Should().BeEmpty();
        _world.Xp.Should().Equal(Skill.GetDeltaXP(0));
        _world.Opened.Should().Equal(fight);
        _world.Busy.Should().BeTrue();
        _world.FishCreated.Should().BeEmpty("the fish is landed by the fight, not by the bite");
        _world.Messages.Should().Contain(FishingFightLifecycle.BiteMessage);
        _world.Messages.Should().Contain("Bait Remaining: 4x Little Worm");
        _world.Log.Should().ContainInOrder("bait", "message", "message", "attempts", "xp", "busy", "open");
    }

    [Test]
    public void TheXpForABite_IsTodaysDeltaXpOfTheFishAgainstTheRankAtTheBite()
    {
        foreach (var (level, rank) in new[] { (0, 0), (20, 20), (22, 20), (18, 20), (35, 10), (0, 50), (52, 50) })
        {
            _world = new FakeFishingFightWorld();

            Lifecycle().Bite(Bite(level, rank, rod: FishingRodType.Bamboo));

            _world.Xp.Should().Equal(new[] { Skill.GetDeltaXP(level - rank) }, $"level {level}, rank {rank}");
        }
    }

    [Test]
    public void BaitNeverGoesBelowZero_AndTheLastBaitIsStillSpent()
    {
        Lifecycle().Bite(Bite(baitRemaining: 1));
        _world.Bait[Rod].Should().Be(0);

        _world = new FakeFishingFightWorld();
        Lifecycle().Bite(Bite(baitRemaining: 0));
        _world.Bait[Rod].Should().Be(0);
    }

    [Test]
    public void TheLastBiteOfAFishingPoint_ExhaustsItWithoutLeavingACount()
    {
        Lifecycle().Bite(Bite(pointAttempts: 1));

        _world.ExhaustedPoints.Should().Equal(Point);
        _world.PointAttempts.Should().BeEmpty();
    }

    [Test]
    public void TheFight_DoesNotNeedTheFishingPointToLast()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(Bite(pointAttempts: 1));
        _world.ExhaustedPoints.Should().Equal(Point);

        Reel(fight);
        Reel(fight);
        Reel(fight);
        var result = lifecycle.Settle(fight);

        result.Ending.Should().Be(LineEnding.Landed);
        _world.FishCreated.Should().Equal("test_fish");
    }

    [Test]
    public void WhenTheWindowCannotOpen_TheFishIsLost_ButTheBaitAndXpAreSpentAsToday()
    {
        _world.CanOpenWindow = false;

        var fight = Lifecycle().Bite(Bite(level: 20, rank: 20));

        fight.Should().BeNull();
        _world.Bait[Rod].Should().Be(4);
        _world.Xp.Should().Equal(Skill.GetDeltaXP(0));
        _world.PointAttempts[Point].Should().Be(6);
        _world.Busy.Should().BeFalse();
        _world.FishCreated.Should().BeEmpty();
        _world.Messages.Should().Contain(message => message.Contains(FishingFightLifecycle.LostMessage));
        _world.ScheduledCount.Should().Be(0);
    }

    [Test]
    public void ALandedFish_GoesToTheAngler_FreesThemToCastAgain_AndIsSettledOnce()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(EasyFish());
        var baitBefore = _world.Bait[Rod];
        var xpBefore = _world.Xp.ToList();
        Reel(fight);
        Reel(fight);
        Reel(fight);
        fight.Engine.Ending.Should().Be(LineEnding.Landed);

        var result = lifecycle.Settle(fight);

        result.Ending.Should().Be(LineEnding.Landed);
        result.Message.Should().Be("You landed a Test Fish!");
        _world.FishCreated.Should().Equal("test_fish");
        _world.Busy.Should().BeFalse();
        _world.Messages.Should().Contain("You landed a Test Fish!");
        lifecycle.GetActive(Player).Should().BeNull();
        lifecycle.IsActive(fight).Should().BeFalse();

        lifecycle.Settle(fight).Should().BeNull("a repeated settle does nothing");
        _world.FishCreated.Should().HaveCount(1);
        _world.Bait[Rod].Should().Be(baitBefore);
        _world.Xp.Should().Equal(xpBefore);
    }

    [Test]
    public void ASnappedLine_LosesTheFish_AndChangesNeitherTheBaitNorTheXp()
    {
        var lifecycle = Lifecycle(55, 99, 99);
        var fight = lifecycle.Bite(HopelessFish());
        fight.Engine.Move.Should().Be(FishMove.Thrash);
        var bait = _world.Bait[Rod];
        var xp = _world.Xp.ToList();

        Reel(fight);
        var result = lifecycle.Settle(fight);

        fight.Engine.Ending.Should().Be(LineEnding.Snapped);
        result.Ending.Should().Be(LineEnding.Snapped);
        result.Message.Should().Be(FishingFightLifecycle.SnappedMessage);
        _world.FishCreated.Should().BeEmpty();
        _world.Busy.Should().BeFalse();
        _world.Bait[Rod].Should().Be(bait);
        _world.Xp.Should().Equal(xp);
        _world.Messages.Should().Contain(message => message.Contains(FishingFightLifecycle.SnappedMessage));
    }

    [Test]
    public void AnEscape_LosesTheFish_AndChangesNeitherTheBaitNorTheXp()
    {
        var lifecycle = Lifecycle(0, 99, 99);
        var fight = lifecycle.Bite(HopelessFish());
        fight.Engine.Move.Should().Be(FishMove.Run);
        var bait = _world.Bait[Rod];
        var xp = _world.Xp.ToList();

        fight.Engine.Perform(LineActionIds.GiveLine);
        var result = lifecycle.Settle(fight);

        fight.Engine.Ending.Should().Be(LineEnding.Escaped);
        result.Ending.Should().Be(LineEnding.Escaped);
        result.Message.Should().Be(FishingFightLifecycle.EscapedMessage);
        _world.FishCreated.Should().BeEmpty();
        _world.Busy.Should().BeFalse();
        _world.Bait[Rod].Should().Be(bait);
        _world.Xp.Should().Equal(xp);
    }

    [Test]
    public void TheXpAndBait_AreTheSameWhetherTheFishIsLandedOrLost()
    {
        var landed = new FakeFishingFightWorld();
        var lifecycle = new FishingFightLifecycle(landed, new ScriptedMinigameRandom(99, 99, 99, 99));
        var fight = lifecycle.Bite(Bite(level: 20, rank: 20));
        fight.Engine.Should().NotBeNull();

        var lost = new FakeFishingFightWorld();
        var lostLifecycle = new FishingFightLifecycle(lost, new ScriptedMinigameRandom(99, 99, 99, 99));
        var lostFight = lostLifecycle.Bite(Bite(level: 20, rank: 20));
        lostLifecycle.Abandon(lostFight, FishingFightEnd.WindowClosed);

        lost.Xp.Should().Equal(landed.Xp);
        lost.Bait.Should().BeEquivalentTo(landed.Bait);
        lost.PointAttempts.Should().BeEquivalentTo(landed.PointAttempts);
    }

    [Test]
    public void SettlingAFightThatIsStillGoing_DoesNothing()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(EasyFish());

        lifecycle.Settle(fight).Should().BeNull();

        lifecycle.IsActive(fight).Should().BeTrue();
        _world.Busy.Should().BeTrue();
        _world.FishCreated.Should().BeEmpty();
    }

    [Test]
    public void ClosingTheWindow_LosesTheFish_AsAnEscape()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(EasyFish());
        _world.Log.Clear();

        lifecycle.Abandon(fight, FishingFightEnd.WindowClosed);

        fight.Settled.Should().BeTrue();
        lifecycle.IsActive(fight).Should().BeFalse();
        _world.Busy.Should().BeFalse();
        _world.FishCreated.Should().BeEmpty();
        _world.Messages.Should().Contain(message => message.Contains("You let the fish go."));
        _world.Log.Should().NotContain("close", "the player already closed the window");
    }

    [Test]
    public void AbandoningTwice_OrAfterTheFightEnded_ChangesNothingMore()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(EasyFish());
        Reel(fight);
        Reel(fight);
        Reel(fight);
        lifecycle.Settle(fight);
        var messages = _world.Messages.Count;

        lifecycle.Abandon(fight, FishingFightEnd.WindowClosed);
        lifecycle.Abandon(fight, FishingFightEnd.Left);
        lifecycle.Abandon(null, FishingFightEnd.Left);

        _world.Messages.Should().HaveCount(messages);
        _world.FishCreated.Should().HaveCount(1, "closing the window after landing the fish does not take it back");
    }

    [Test]
    public void ALateLandedAction_AfterTheFightWasAbandoned_CannotCreateTheFish()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(EasyFish());
        Reel(fight);
        Reel(fight);
        Reel(fight);
        lifecycle.Abandon(fight, FishingFightEnd.Moved);

        lifecycle.Settle(fight).Should().BeNull();

        _world.FishCreated.Should().BeEmpty();
    }

    [Test]
    public void TheFightChecksOnceASecond_WhileTheAnglerStaysWatchingAndStill()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(EasyFish());

        for (var second = 0; second < 5; second++)
        {
            _world.ScheduledCount.Should().Be(1);
            _world.RunNextScheduled();
        }

        lifecycle.IsActive(fight).Should().BeTrue();
        _world.Busy.Should().BeTrue();
        _world.ScheduledCount.Should().Be(1);
    }

    [Test]
    public void MovingAwayFromWhereTheyCast_LosesTheFishAndClosesTheWindow()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(EasyFish());
        _world.Position = new Vector3(10.5f, 20f, 0f);

        _world.RunNextScheduled();

        fight.Settled.Should().BeTrue();
        _world.Busy.Should().BeFalse();
        _world.WindowOpen.Should().BeFalse();
        _world.Messages.Should().Contain(message => message.Contains("You moved and the fish got away."));
        _world.ScheduledCount.Should().Be(0);
    }

    [Test]
    public void GoingIntoCombat_LosesTheFish()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(EasyFish());
        _world.IsInCombat = true;

        _world.RunNextScheduled();

        fight.Settled.Should().BeTrue();
        _world.Busy.Should().BeFalse();
        _world.WindowOpen.Should().BeFalse();
        _world.Messages.Should().Contain(message => message.Contains("The fight drew you away"));
    }

    [Test]
    public void Dying_LosesTheFishWithoutAMessage()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(EasyFish());
        var messages = _world.Messages.Count;
        _world.IsDead = true;

        _world.RunNextScheduled();

        fight.Settled.Should().BeTrue();
        _world.Busy.Should().BeFalse();
        _world.Messages.Should().HaveCount(messages);
    }

    [Test]
    public void LoggingOut_LosesTheFish()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(EasyFish());
        var messages = _world.Messages.Count;
        _world.IsValid = false;

        _world.RunNextScheduled();

        fight.Settled.Should().BeTrue();
        _world.Busy.Should().BeFalse();
        _world.FishCreated.Should().BeEmpty();
        _world.Messages.Should().HaveCount(messages);
        lifecycle.GetActive(Player).Should().BeNull();
    }

    [Test]
    public void AWindowThatIsNoLongerOpen_LosesTheFish()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(EasyFish());
        _world.WindowOpen = false;

        _world.RunNextScheduled();

        fight.Settled.Should().BeTrue();
        _world.Busy.Should().BeFalse();
        _world.Messages.Should().Contain(message => message.Contains("You let the fish go."));
    }

    [Test]
    public void TheChecksStopOnceTheFightIsSettled()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(EasyFish());
        Reel(fight);
        Reel(fight);
        Reel(fight);
        lifecycle.Settle(fight);
        _world.IsInCombat = true;
        var messages = _world.Messages.Count;

        _world.RunNextScheduled();

        _world.Messages.Should().HaveCount(messages);
        _world.ScheduledCount.Should().Be(0);
        _world.FishCreated.Should().HaveCount(1);
    }

    [Test]
    public void ANewBite_WhileAFightIsGoing_LosesTheOldFishQuietlyAndOpensTheNewFight()
    {
        var lifecycle = Lifecycle();
        var first = lifecycle.Bite(EasyFish());

        var second = lifecycle.Bite(EasyFish());

        first.Settled.Should().BeTrue();
        second.Settled.Should().BeFalse();
        lifecycle.GetActive(Player).Should().Be(second);
        _world.Busy.Should().BeTrue();
        _world.Opened.Should().Equal(first, second);
        _world.Messages.Should().NotContain(message => message.Contains(FishingFightLifecycle.LostMessage));
    }

    [Test]
    public void TheFightIsBuiltFromTheFishsLevelAndRarityAndTheRodsTier()
    {
        var lifecycle = Lifecycle();

        var fight = lifecycle.Bite(Bite(level: 52, rank: 50, rarity: FishRarity.Rare, rod: FishingRodType.LuShang));

        var setup = fight.Engine.Setup;
        setup.Rarity.Should().Be(FishRarity.Rare);
        setup.FishLevel.Should().Be(52);
        setup.Rank.Should().Be(50);
        setup.RodTier.Should().Be(4);
        setup.TensionCap.Should().Be(14);
        fight.Engine.TensionCap.Should().Be(14);
        fight.Fish.Name.Should().Be("Test Fish");
        fight.Player.Should().Be(Player);
    }

    [Test]
    public void ABiteWithoutAFish_IsRefused()
    {
        var act = () => Lifecycle().Bite(null);

        act.Should().Throw<ArgumentNullException>();
    }
}
