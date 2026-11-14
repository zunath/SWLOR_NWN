using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Saturation Fire in space: a ship hit by enough area launcher abilities inside the window suffers Exposed.
/// </summary>
public class SpaceHitSequencesTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    private static SpaceHitSequenceState After(int count, double secondsAgo) =>
        new() { Count = count, LastHit = Now.AddSeconds(-secondsAgo) };

    [Test]
    public void AFirstHit_StartsTheCountAtOne()
    {
        SpaceHitSequences.NextCount(null, Now, 30).Should().Be(1);
    }

    [Test]
    public void AHitInsideTheWindow_AddsToTheCount()
    {
        SpaceHitSequences.NextCount(After(1, 12), Now, 30).Should().Be(2);
        SpaceHitSequences.NextCount(After(1, 30), Now, 30).Should().Be(2, "the window includes its last second");
    }

    [Test]
    public void AHitAfterTheWindow_StartsANewCount()
    {
        SpaceHitSequences.NextCount(After(1, 31), Now, 30).Should().Be(1);
    }

    [Test]
    public void TheStatusTriggers_WhenTheCountReachesTheRequirement()
    {
        SpaceHitSequences.IsTriggered(1, 2).Should().BeFalse();
        SpaceHitSequences.IsTriggered(2, 2).Should().BeTrue();
        SpaceHitSequences.IsTriggered(3, 2).Should().BeTrue();
        SpaceHitSequences.IsTriggered(5, 0).Should().BeFalse("a pilot without the perk never triggers it");
    }
}
