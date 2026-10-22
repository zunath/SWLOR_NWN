using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

namespace SWLOR.Game.Server.Tests.Service.Reaction;

/// <summary>Covers how a cycle pattern and length become the sequence of phases.</summary>
public class ReactionCycleTests
{
    private static readonly ReactionPhase[] InOrder =
    {
        ReactionPhase.Calm, ReactionPhase.Rising, ReactionPhase.Peak, ReactionPhase.Settling
    };

    private static IEnumerable<TestCaseData> EveryPatternAndLength() =>
        from pattern in Enum.GetValues<ReactionCyclePattern>()
        from length in Enumerable.Range(ReactionCycle.MinimumLength, ReactionCycle.MaximumLength - ReactionCycle.MinimumLength + 1)
        select new TestCaseData(pattern, length);

    [TestCaseSource(nameof(EveryPatternAndLength))]
    public void EveryCycle_RunsCalmRisingPeakSettlingInOrder_WithEachPhaseAtLeastOneTurn(ReactionCyclePattern pattern, int length)
    {
        var cycle = new ReactionCycle(pattern, length);

        cycle.Length.Should().Be(length);
        cycle.Phases.Should().HaveCount(length);
        InOrder.Should().OnlyContain(phase => cycle.TurnsIn(phase) >= 1);
        cycle.Phases.Distinct().Should().Equal(InOrder);
        cycle.Phases.Select((phase, index) => index == 0 || phase >= cycle.Phases[index - 1]).Should().OnlyContain(ordered => ordered);
    }

    [TestCase(ReactionCyclePattern.Brew, 4, 1, 1, 1, 1)]
    [TestCase(ReactionCyclePattern.Brew, 6, 2, 1, 1, 2)]
    [TestCase(ReactionCyclePattern.Brew, 8, 3, 2, 1, 2)]
    [TestCase(ReactionCyclePattern.Surge, 5, 1, 1, 2, 1)]
    [TestCase(ReactionCyclePattern.Surge, 6, 1, 2, 2, 1)]
    [TestCase(ReactionCyclePattern.Surge, 8, 2, 2, 3, 1)]
    [TestCase(ReactionCyclePattern.Trigger, 5, 1, 2, 1, 1)]
    [TestCase(ReactionCyclePattern.Trigger, 6, 2, 2, 1, 1)]
    [TestCase(ReactionCyclePattern.Trigger, 8, 2, 3, 1, 2)]
    public void ThePatternDecidesWhichPhasesGetTheExtraTurns(
        ReactionCyclePattern pattern, int length, int calm, int rising, int peak, int settling)
    {
        var cycle = new ReactionCycle(pattern, length);

        cycle.TurnsIn(ReactionPhase.Calm).Should().Be(calm);
        cycle.TurnsIn(ReactionPhase.Rising).Should().Be(rising);
        cycle.TurnsIn(ReactionPhase.Peak).Should().Be(peak);
        cycle.TurnsIn(ReactionPhase.Settling).Should().Be(settling);
    }

    [Test]
    public void PoisonsAndTrapsUseDifferentCycles()
    {
        var brew = new ReactionCycle(ReactionCyclePattern.Brew, 6).Phases;
        var surge = new ReactionCycle(ReactionCyclePattern.Surge, 6).Phases;
        var trigger = new ReactionCycle(ReactionCyclePattern.Trigger, 6).Phases;

        brew.Should().NotEqual(surge);
        brew.Should().NotEqual(trigger);
        surge.Should().NotEqual(trigger);
    }

    [Test]
    public void PhaseAt_WrapsAroundTheCycleInBothDirections()
    {
        var cycle = new ReactionCycle(ReactionCyclePattern.Brew, 6);

        cycle.PhaseAt(0).Should().Be(ReactionPhase.Calm);
        cycle.PhaseAt(3).Should().Be(ReactionPhase.Peak);
        cycle.PhaseAt(6).Should().Be(cycle.PhaseAt(0));
        cycle.PhaseAt(9).Should().Be(cycle.PhaseAt(3));
        cycle.PhaseAt(-1).Should().Be(cycle.PhaseAt(5));
    }

    [Test]
    public void FirstAndLastIndexMarkTheTurnsOfAPhase()
    {
        var cycle = new ReactionCycle(ReactionCyclePattern.Surge, 8);

        cycle.FirstIndexOf(ReactionPhase.Peak).Should().Be(4);
        cycle.LastIndexOf(ReactionPhase.Peak).Should().Be(6);
        cycle.FirstIndexOf(ReactionPhase.Calm).Should().Be(0);
    }

    [Test]
    public void AnAttemptMayStartOnlyInCalmOrRising()
    {
        foreach (var pattern in Enum.GetValues<ReactionCyclePattern>())
        {
            for (var length = ReactionCycle.MinimumLength; length <= ReactionCycle.MaximumLength; length++)
            {
                var cycle = new ReactionCycle(pattern, length);

                cycle.StartPositionCount.Should().Be(cycle.TurnsIn(ReactionPhase.Calm) + cycle.TurnsIn(ReactionPhase.Rising));
                Enumerable.Range(0, cycle.StartPositionCount).Select(cycle.PhaseAt)
                    .Should().OnlyContain(phase => phase == ReactionPhase.Calm || phase == ReactionPhase.Rising);
            }
        }
    }

    [TestCase(3)]
    [TestCase(9)]
    [TestCase(0)]
    [TestCase(-5)]
    public void ALengthOutsideFourToEightIsRefused(int length)
    {
        var act = () => new ReactionCycle(ReactionCyclePattern.Brew, length);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void AnUnknownPatternIsRefused()
    {
        var act = () => new ReactionCycle((ReactionCyclePattern)99, 6);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
