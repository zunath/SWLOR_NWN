using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Sustained Fire in space: consecutive energy weapon hits against one target build DMG up to a cap.
/// </summary>
public class SpaceRepeatedHitsTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    private static SpaceRepeatedHitState Hit(SpaceRepeatedHitState state, uint target, int perHit, int maximum, double seconds) =>
        SpaceRepeatedHits.Advance(state, target, perHit, maximum, 30, Now.AddSeconds(seconds));

    [Test]
    public void MaximumStacks_IsTheCapDividedByTheBonusPerHit()
    {
        SpaceRepeatedHits.GetMaximumStacks(1, 5).Should().Be(5);
        SpaceRepeatedHits.GetMaximumStacks(2, 10).Should().Be(5);
        SpaceRepeatedHits.GetMaximumStacks(3, 15).Should().Be(5);
        SpaceRepeatedHits.GetMaximumStacks(0, 5).Should().Be(1);
    }

    [Test]
    public void ConsecutiveHitsOnTheSameTarget_BuildStacksUpToTheCap()
    {
        SpaceRepeatedHitState state = null;
        var bonuses = new List<int>();
        for (var i = 0; i < 7; i++)
        {
            state = Hit(state, 1u, 2, 10, i);
            bonuses.Add(SpaceRepeatedHits.GetBonus(state.Stacks, 2, 10));
        }

        bonuses.Should().Equal(2, 4, 6, 8, 10, 10, 10);
    }

    [Test]
    public void AHitOnANewTarget_StartsANewRun()
    {
        var state = Hit(Hit(Hit(null, 1u, 1, 5, 0), 1u, 1, 5, 1), 1u, 1, 5, 2);
        state.Stacks.Should().Be(3);

        Hit(state, 2u, 1, 5, 3).Stacks.Should().Be(1);
    }

    [Test]
    public void StacksExpireAfterTheirDuration()
    {
        var state = Hit(Hit(null, 1u, 1, 5, 0), 1u, 1, 5, 1);
        state.Stacks.Should().Be(2);

        Hit(state, 1u, 1, 5, 31).Stacks.Should().Be(3, "a hit 30 s after the last still continues the run");
        Hit(state, 1u, 1, 5, 32).Stacks.Should().Be(1, "a hit more than 30 s after the last starts a new run");
    }

    [Test]
    public void GetShare_KeepsTheFullBonusForOneOrTwoEnergyWeapons()
    {
        SpaceRepeatedHits.GetShare(15, 1).Should().Be(15);
        SpaceRepeatedHits.GetShare(15, 2).Should().Be(15);
        SpaceRepeatedHits.GetShare(15, 0).Should().Be(15, "a ship with no counted weapon is treated as one");
    }

    [Test]
    public void GetShare_HoldsSeveralEnergyWeaponsToTwoWeaponsWorth()
    {
        // Three lasers at full stacks: 15 x 2 / 3 = 10 each, 30 in all, as two ground weapons would.
        SpaceRepeatedHits.GetShare(15, 3).Should().Be(10);
        SpaceRepeatedHits.GetShare(3, 3).Should().Be(2);
        SpaceRepeatedHits.GetShare(5, 4).Should().Be(3, "2.5 rounds half away from zero");
        SpaceRepeatedHits.GetShare(1, 6).Should().Be(0);
    }
}
