using System;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.ShuttleService;

namespace SWLOR.Game.Server.Tests.Service;

public sealed class ShuttleBoardingTrackerTests
{
    private static readonly DateTime Departure = new(2026, 11, 11, 22, 10, 0, DateTimeKind.Utc);

    [Test]
    public void BoardedPassengersAreFollowedUntilTheyReachTheirFlight()
    {
        var tracker = new ShuttleBoardingTracker();
        tracker.Start("p1", "1>2@1", Departure);

        tracker.IsBoarding("p1", "1>2@1").Should().BeTrue();
        tracker.IsBoarding("p1", "1>4@1").Should().BeFalse();
        tracker.IsBoarding("p2", "1>2@1").Should().BeFalse();
        tracker.GetPending().Should().Equal(("p1", "1>2@1"));
    }

    [Test]
    public void TheBoardingJumpGetsTimeToLoadBeforeItIsSentAgain()
    {
        var tracker = new ShuttleBoardingTracker();
        tracker.Start("p1", "1>2@1", Departure);

        tracker.TryStartRetry("p1", Departure.AddSeconds(2), out _).Should().BeFalse();
        tracker.TryStartRetry("p1", Departure + ShuttleBoardingTracker.RetryInterval - TimeSpan.FromMilliseconds(1), out _)
            .Should().BeFalse();
    }

    [Test]
    public void APassengerLeftAtTheTerminalIsSentAgainAtEveryRetryInterval()
    {
        var tracker = new ShuttleBoardingTracker();
        tracker.Start("p1", "1>2@1", Departure);
        var firstRetry = Departure + ShuttleBoardingTracker.RetryInterval;

        tracker.TryStartRetry("p1", firstRetry, out var attempt).Should().BeTrue();
        attempt.Should().Be(2);
        tracker.TryStartRetry("p1", firstRetry.AddSeconds(1), out _).Should().BeFalse();
        tracker.TryStartRetry("p1", firstRetry + ShuttleBoardingTracker.RetryInterval, out attempt).Should().BeTrue();
        attempt.Should().Be(3);
        tracker.IsBoarding("p1", "1>2@1").Should().BeTrue("a dropped jump keeps the passenger pending until they are aboard");
    }

    [Test]
    public void ClearingAPassengerStopsRetries()
    {
        var tracker = new ShuttleBoardingTracker();
        tracker.Start("p1", "1>2@1", Departure);

        tracker.Clear("p1");

        tracker.IsBoarding("p1", "1>2@1").Should().BeFalse();
        tracker.GetPending().Should().BeEmpty();
        tracker.TryStartRetry("p1", Departure.AddMinutes(1), out _).Should().BeFalse();
    }

    [Test]
    public void BoardingANewFlightReplacesTheOldOne()
    {
        var tracker = new ShuttleBoardingTracker();
        tracker.Start("p1", "1>2@1", Departure);
        tracker.Start("p1", "1>2@2", Departure.AddMinutes(2));

        tracker.IsBoarding("p1", "1>2@1").Should().BeFalse();
        tracker.IsBoarding("p1", "1>2@2").Should().BeTrue();
        tracker.TryStartRetry("p1", Departure.AddMinutes(2).AddSeconds(1), out _).Should().BeFalse();
    }
}
