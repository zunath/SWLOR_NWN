using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.ShuttleService;

namespace SWLOR.Game.Server.Tests.Service;

public sealed class ShuttleMissedBoardingTrackerTests
{
    [Test]
    public void OnlyTheFirstMissedDepartureNotifiesThePlayer()
    {
        var tracker = new ShuttleMissedBoardingTracker();

        tracker.RecordMiss("p1").Should().BeTrue();
        for (var i = 0; i < 10; i++)
            tracker.RecordMiss("p1").Should().BeFalse();
    }

    [Test]
    public void MissedPlayersAreSilencedForBoardingCalls()
    {
        var tracker = new ShuttleMissedBoardingTracker();
        tracker.RecordMiss("p1");

        tracker.HasMissed("p1").Should().BeTrue();
        tracker.HasMissed("p2").Should().BeFalse();
    }

    [Test]
    public void ReturningToTheTerminalRearmsMessages()
    {
        var tracker = new ShuttleMissedBoardingTracker();
        tracker.RecordMiss("p1");

        tracker.Clear("p1");

        tracker.HasMissed("p1").Should().BeFalse();
        tracker.RecordMiss("p1").Should().BeTrue();
    }
}
