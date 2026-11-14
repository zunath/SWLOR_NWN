using System;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.World;

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
        tracker.GetPending().Should().Equal(("p1", "1>2@1", false));
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
    public void APassengerTheFlightLandedWithoutIsFollowedUntilDelivered()
    {
        var tracker = new ShuttleBoardingTracker();
        tracker.Start("p1", "1>2@1", Departure);
        var arrival = Departure.AddMinutes(5);

        tracker.StartDelivery("p1", "1>2@1", arrival);

        tracker.IsDelivering("p1").Should().BeTrue();
        tracker.IsBoarding("p1", "1>2@1").Should().BeFalse("the flight has landed; the passenger now goes to the destination");
        tracker.GetPending().Should().Equal(("p1", "1>2@1", true));
        tracker.TryStartRetry("p1", arrival.AddSeconds(1), out _).Should().BeFalse("the delivery jump gets time to load");
        tracker.TryStartRetry("p1", arrival + ShuttleBoardingTracker.RetryInterval, out var attempt).Should().BeTrue();
        attempt.Should().Be(2);

        tracker.Clear("p1");
        tracker.IsDelivering("p1").Should().BeFalse();
    }

    [Test]
    public void BoardingPassengersAreNotDelivering()
    {
        var tracker = new ShuttleBoardingTracker();
        tracker.Start("p1", "1>2@1", Departure);

        tracker.IsDelivering("p1").Should().BeFalse();
        tracker.IsDelivering("p2").Should().BeFalse();
        tracker.GetPending().Should().Equal(("p1", "1>2@1", false));
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
