using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Tests.Service.Line;

/// <summary>
/// Covers Quick Reel, the choice at the start of a bite (owner decision, October 2026): today's catch roll at a fixed Willpower score, no
/// fight time, today's XP for the bite and nothing more, and bait spent as before.
/// </summary>
public class FishingQuickReelTests
{
    private const uint Player = 11;
    private const uint Rod = 22;
    private const uint Point = 33;

    private FakeFishingFightWorld _world;
    private DieRandom _random;

    /// <summary>A source of chance whose D100 is whatever the test sets, so Quick Reel's roll is chosen and the fight's forecast is not.</summary>
    private sealed class DieRandom : IMinigameRandom
    {
        public int Roll { get; set; } = 99;

        public int D100() => Roll;

        public int Next(int minInclusive, int maxExclusive) => Math.Clamp(Roll, minInclusive, maxExclusive - 1);
    }

    [OneTimeSetUp]
    public void LoadTheXpChart()
    {
        Skill.CacheXPChartData();
    }

    [SetUp]
    public void SetUp()
    {
        _world = new FakeFishingFightWorld();
        _random = new DieRandom();
    }

    private FishingFightLifecycle Lifecycle() => new(_world, _random);

    private static FishingBite Bite(int level, int rank, FishRarity rarity = FishRarity.Common) =>
        new(
            Player,
            Rod,
            Point,
            new FishAttribute("Test Fish", "test_fish", level, true, rarity: rarity),
            FishingRodType.LuShang,
            rank,
            5,
            "Little Worm",
            7,
            new Vector3(10f, 20f, 0f));

    [TestCase(10, 10, 10, 60)]
    [TestCase(10, 10, 14, 68)]
    [TestCase(10, 10, 12, 64)]
    [TestCase(20, 22, 12, 44)]
    [TestCase(0, 5, 10, 10)]
    [TestCase(20, 10, 20, 95)]
    [TestCase(0, 10, 0, 0)]
    [TestCase(50, 80, 20, 0)]
    public void TheCatchChance_IsTodaysFormula_KeptBetweenZeroAndNinetyFive(int rank, int fishLevel, int willpowerScore, int expected)
    {
        FishingQuickReel.CatchChancePercent(rank, fishLevel, willpowerScore).Should().Be(expected);
    }

    [Test]
    public void TheCatchChance_UsesTheReferenceWillpowerScore_UnlessAnotherIsGiven()
    {
        FishingQuickReel.ReferenceWillpowerScore.Should().Be(10, "today's catch roll for a character with no attribute investment (owner decision, October 2026)");

        foreach (var (rank, level) in new[] { (0, 0), (10, 10), (20, 22), (50, 52), (3, 12), (50, 0) })
        {
            FishingQuickReel.CatchChancePercent(rank, level).Should().Be(
                FishingQuickReel.CatchChancePercent(rank, level, FishingQuickReel.ReferenceWillpowerScore), $"rank {rank}, level {level}");
        }
    }

    [Test]
    public void TheChanceTheWindowShows_IsTheChanceTheRollUses_ForTheBitesRankAndFish()
    {
        var fight = Lifecycle().Bite(Bite(level: 22, rank: 20));

        FishingFightLifecycle.QuickReelChance(fight).Should().Be(40);
        FishingFightLifecycle.QuickReelChance(fight).Should().Be(FishingQuickReel.CatchChancePercent(20, 22));
    }

    [Test]
    public void ARollAtOrUnderTheChance_LandsTheFish()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(Bite(level: 20, rank: 20));
        _random.Roll = 60;

        var result = lifecycle.QuickReel(fight);

        result.Ending.Should().Be(LineEnding.Landed);
        result.Message.Should().Be("You landed a Test Fish!");
        _world.FishCreated.Should().Equal("test_fish");
        _world.Messages.Should().Contain("You landed a Test Fish!");
        fight.Settled.Should().BeTrue();
        fight.Result.Should().BeSameAs(result);
        _world.Busy.Should().BeFalse();
        lifecycle.GetActive(Player).Should().BeNull();
    }

    [Test]
    public void ALandedQuickReel_PlaysTheLandingClip_AndAMissPlaysNone()
    {
        var landed = Lifecycle();
        var landedFight = landed.Bite(Bite(level: 20, rank: 20));
        _random.Roll = 1;
        landed.QuickReel(landedFight);
        _world.Animations.Should().Equal("sw_landfish");

        _world = new FakeFishingFightWorld();
        var missed = Lifecycle();
        var missedFight = missed.Bite(Bite(level: 20, rank: 20));
        _random.Roll = 100;
        missed.QuickReel(missedFight);
        _world.Animations.Should().BeEmpty();
    }

    [Test]
    public void ARollOverTheChance_LosesTheFish()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(Bite(level: 20, rank: 20));
        _random.Roll = 61;

        var result = lifecycle.QuickReel(fight);

        result.Ending.Should().Be(LineEnding.Escaped);
        result.Message.Should().Be(FishingFightLifecycle.QuickReelMissedMessage);
        _world.FishCreated.Should().BeEmpty();
        _world.Messages.Should().Contain(message => message.Contains(FishingFightLifecycle.QuickReelMissedMessage));
        fight.Settled.Should().BeTrue();
        _world.Busy.Should().BeFalse();
    }

    [Test]
    public void TheChanceStopsAtNinetyFive_AndAHopelessFishIsNeverLanded()
    {
        var easy = Lifecycle();
        var easyFight = easy.Bite(Bite(level: 0, rank: 50));
        _random.Roll = 96;
        easy.QuickReel(easyFight).Ending.Should().Be(LineEnding.Escaped, "a roll of 96 beats the 95 percent cap");

        _world = new FakeFishingFightWorld();
        _random = new DieRandom();
        var capped = Lifecycle();
        var cappedFight = capped.Bite(Bite(level: 0, rank: 50));
        _random.Roll = 95;
        capped.QuickReel(cappedFight).Ending.Should().Be(LineEnding.Landed);

        _world = new FakeFishingFightWorld();
        _random = new DieRandom();
        var hopeless = Lifecycle();
        var hopelessFight = hopeless.Bite(Bite(level: 80, rank: 0));
        _random.Roll = 1;
        hopeless.QuickReel(hopelessFight).Ending.Should().Be(LineEnding.Escaped, "no roll of 1 to 100 is at or under 0");
        _world.FishCreated.Should().BeEmpty();
    }

    [Test]
    public void QuickReel_PaysTodaysXpForTheBiteAndNothingMore_HoweverLongItIsLeft()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(Bite(level: 20, rank: 20));
        var expected = Skill.GetDeltaXP(0);
        _world.Xp.Should().Equal(new[] { expected }, "the bite pays today's XP at once");
        _world.UtcNow = _world.UtcNow.AddSeconds(LineRules.XpFullTurns * LineRules.TurnSeconds * 10);
        _random.Roll = 1;

        lifecycle.QuickReel(fight).Ending.Should().Be(LineEnding.Landed);

        _world.Xp.Should().Equal(new[] { expected }, "Quick Reel carries none of the fight's multiplied XP");
        fight.XpOnFinish.Should().Be(0);
        lifecycle.Settle(fight).Should().BeNull("the fight is settled, so nothing more is paid");
        _world.Xp.Should().Equal(new[] { expected });
    }

    [Test]
    public void QuickReel_LeavesTheBaitAndThePointsBiteAsTheBiteSpentThem()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(Bite(level: 20, rank: 20));
        var bait = _world.Bait[Rod];
        var attempts = _world.PointAttempts[Point];
        var messages = _world.Messages.Count;
        _random.Roll = 1;

        lifecycle.QuickReel(fight);

        bait.Should().Be(4);
        attempts.Should().Be(6);
        _world.Bait[Rod].Should().Be(bait);
        _world.PointAttempts[Point].Should().Be(attempts);
        _world.ExhaustedPoints.Should().BeEmpty();
        _world.Messages.Skip(messages).Should().HaveCount(1, "only the result is new");
    }

    [Test]
    public void QuickReel_NeedsNoTurnClock_AndTakesNoTime()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(Bite(level: 20, rank: 20));
        _random.Roll = 1;

        lifecycle.QuickReel(fight).Should().NotBeNull("the first choice of a bite is free of the turn clock");

        lifecycle.SecondsUntilNextTurn(fight).Should().Be(0);
        _world.ScheduledCount.Should().BeLessThanOrEqualTo(1, "the only check is the one the bite scheduled");
    }

    [Test]
    public void QuickReel_IsOnlyAChoiceBeforeTheFirstMove()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(Bite(level: 0, rank: 50));
        lifecycle.CanQuickReel(fight).Should().BeTrue();

        fight.Engine.Perform(LineActionIds.Reel);

        lifecycle.CanQuickReel(fight).Should().BeFalse("a fight in progress cannot fall back on the quick roll, which would let the angler take the better of the two");
        _random.Roll = 1;
        lifecycle.QuickReel(fight).Should().BeNull();
        fight.Settled.Should().BeFalse();
        _world.FishCreated.Should().BeEmpty();
        _world.Busy.Should().BeTrue();
    }

    [Test]
    public void QuickReel_CannotBeUsedTwice_OrAfterTheFightIsOver()
    {
        var lifecycle = Lifecycle();
        var landed = lifecycle.Bite(Bite(level: 20, rank: 20));
        _random.Roll = 1;
        lifecycle.QuickReel(landed).Ending.Should().Be(LineEnding.Landed);

        lifecycle.QuickReel(landed).Should().BeNull("a second click does nothing");
        lifecycle.CanQuickReel(landed).Should().BeFalse();
        _world.FishCreated.Should().HaveCount(1);

        var abandoned = lifecycle.Bite(Bite(level: 20, rank: 20));
        lifecycle.Abandon(abandoned, FishingFightEnd.WindowClosed);
        lifecycle.QuickReel(abandoned).Should().BeNull("a fight the angler left cannot be reeled in");
        _world.FishCreated.Should().HaveCount(1);
    }

    [Test]
    public void QuickReel_ChoosesOnlyTheBiteItIsGivenTo()
    {
        var lifecycle = Lifecycle();
        var first = lifecycle.Bite(Bite(level: 20, rank: 20));
        var second = lifecycle.Bite(Bite(level: 20, rank: 20));
        _random.Roll = 1;

        lifecycle.QuickReel(first).Should().BeNull("a new bite replaced the first fight");
        lifecycle.QuickReel(second).Should().NotBeNull();
    }

    [Test]
    public void QuickReel_NeedsAFight()
    {
        var lifecycle = Lifecycle();

        var act = () => lifecycle.QuickReel(null);

        act.Should().Throw<ArgumentNullException>();
        lifecycle.CanQuickReel(null).Should().BeFalse();
    }

    [Test]
    public void QuickReel_OnARareFish_RollsTheSameFormula()
    {
        var lifecycle = Lifecycle();
        var fight = lifecycle.Bite(Bite(level: 52, rank: 50, rarity: FishRarity.Rare));

        FishingFightLifecycle.QuickReelChance(fight).Should().Be(40, "40 - 20 + 2 x 10: a rare fish is no easier and no harder for the quick roll");
        _random.Roll = 40;
        lifecycle.QuickReel(fight).Ending.Should().Be(LineEnding.Landed);
    }
}
