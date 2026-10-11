using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Tests.Service;

public class LoopTerminationTests
{
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    [TestCase(float.NaN)]
    public void InvalidFacingIsRejectedWithoutSubtractionLoops(float facing)
    {
        Action normalize = () => GameMath.NormalizeDegrees(facing);
        normalize.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestCase(float.MaxValue)]
    [TestCase(-float.MaxValue)]
    [TestCase(380f)]
    [TestCase(-20f)]
    [TestCase(360f)]
    [TestCase(-float.Epsilon)]
    public void FiniteFacingNormalizesInConstantTime(float facing)
    {
        GameMath.NormalizeDegrees(facing).Should().BeInRange(0f, 359.99999f);
    }

    [Test]
    public void PulseDelaysPreserveIntervalAndDuration()
    {
        CombatAreaPulses.GetPulseDelays(10f, 3f).Should().Equal(3f, 6f, 9f);
        CombatAreaPulses.GetPulseDelays(6f, 3f).Should().Equal(3f, 6f);
    }

    [Test]
    public void ZoneRefreshStartsImmediatelyAndStopsBeforeExpiry()
    {
        CombatAreaPulses.GetRefreshPulseDelays(10f, 3f).Should().Equal(0f, 3f, 6f, 9f);
        CombatAreaPulses.GetRefreshPulseDelays(6f, 3f).Should().Equal(0f, 3f);
        CombatAreaPulses.GetRefreshPulseDelays(0.005f, 3f).Should().BeEmpty();
    }

    [TestCase(float.PositiveInfinity, 1f)]
    [TestCase(1f, float.PositiveInfinity)]
    [TestCase(float.NaN, 1f)]
    [TestCase(1f, 0f)]
    public void InvalidPulseTimingIsRejected(float duration, float interval)
    {
        Action schedule = () => CombatAreaPulses.GetPulseDelays(duration, interval).ToArray();
        schedule.Should().Throw<ArgumentOutOfRangeException>();
        Action refresh = () => CombatAreaPulses.GetRefreshPulseDelays(duration, interval).ToArray();
        refresh.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void UnrepresentablePulseCountFailsInsteadOfSpinningOnFloatPrecision()
    {
        Action schedule = () => CombatAreaPulses.GetPulseDelays(float.MaxValue, float.Epsilon).ToArray();
        schedule.Should().Throw<OverflowException>();
        Action refresh = () => CombatAreaPulses.GetRefreshPulseDelays(float.MaxValue, float.Epsilon).ToArray();
        refresh.Should().Throw<OverflowException>();
    }

    [Test]
    public void UnavailableDatabaseStopsAtTheDeadline()
    {
        var elapsed = TimeSpan.Zero;
        Action wait = () => DBStartupWait.Until(() => false, "Redis index Player", TimeSpan.FromMilliseconds(250),
            milliseconds => elapsed += TimeSpan.FromMilliseconds(milliseconds), () => elapsed);

        wait.Should().Throw<TimeoutException>().WithMessage("*Redis index Player*");
        elapsed.Should().Be(TimeSpan.FromMilliseconds(300));
    }

    [Test]
    public void DatabaseWaitReturnsAsSoonAsReady()
    {
        var polls = 0;
        var delays = 0;
        DBStartupWait.Until(() => ++polls == 3, "database connection", TimeSpan.FromMinutes(10),
            _ => delays++, () => TimeSpan.Zero);
        delays.Should().Be(2);
    }
}
