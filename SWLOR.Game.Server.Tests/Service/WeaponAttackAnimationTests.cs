using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class WeaponAttackAnimationTests
{
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void EverySwingHasAnExclusiveSlotWithinTheExistingCycle(int count)
    {
        var duration = WeaponAttackAnimation.PlaybackDuration(1750);
        duration.Should().BeLessThan(Combat.BaseAttackDelayMilliseconds);
        var seen = new System.Collections.Generic.HashSet<int>();
        for (var elapsed = 0; elapsed < duration; elapsed++)
        {
            var frame = WeaponAttackAnimation.FrameAt(elapsed, duration, count);
            seen.Add(frame);
            var remaining = WeaponAttackAnimation.RemainingFrameDuration(elapsed, duration, count);
            remaining.Should().BePositive();
            WeaponAttackAnimation.FrameAt(elapsed + remaining - 1, duration, count).Should().Be(frame);
            WeaponAttackAnimation.FrameAt(elapsed + remaining, duration, count).Should().Be(frame + 1);
        }
        seen.Should().BeEquivalentTo(Enumerable.Range(0, count));
        WeaponAttackAnimation.FrameAt(duration, duration, count).Should().Be(count, "the cycle ends in a ready pose");
        WeaponAttackAnimation.RemainingFrameDuration(duration, duration, count).Should().Be(0);
    }

    [Test]
    public void OrdinaryPairGetsTwoConsecutiveAnimations()
    {
        WeaponAttackAnimation.RemainingFrameDuration(0, 1650, 2).Should().Be(825);
        WeaponAttackAnimation.FrameAt(824, 1650, 2).Should().Be(0);
        WeaponAttackAnimation.FrameAt(825, 1650, 2).Should().Be(1);
        WeaponAttackAnimation.RemainingFrameDuration(900, 1650, 2).Should().Be(750);
    }

    [TestCase(2, new byte[] { 1, 2 })]
    [TestCase(5, new byte[] { 1, 2, 1, 2, 1 })]
    [TestCase(6, new byte[] { 1, 2, 1, 2, 1, 2 })]
    public void HastePresentationAlternatesHandsWithoutLosingOrChangingRolls(int count, byte[] expected)
    {
        var rolls = Enumerable.Range(0, count).Select(i => Roll((byte)(i < (count + 1) / 2 ? 1 : 2), (byte)i)).ToArray();
        var ordered = WeaponAttackAnimation.OrderForPlayback(rolls);
        ordered.Select(roll => roll.Weapon).Should().Equal(expected);
        ordered.Should().BeEquivalentTo(rolls);
        rolls.Select(roll => roll.Result).Should().Equal(Enumerable.Range(0, count).Select(i => (byte)i));
    }

    [Test]
    public void NaturalWeaponOrderAndShortNativeAnimationsArePreserved()
    {
        var rolls = new[] { Roll(3, 1), Roll(4, 2), Roll(5, 3) };
        WeaponAttackAnimation.OrderForPlayback(rolls).Should().Equal(rolls);
        WeaponAttackAnimation.PlaybackDuration(1000).Should().Be(1000);
    }

    private static WeaponAttackAnimation.Roll Roll(byte hand, byte result) =>
        new(1, 1750, 123, 1750, 14, result, 0, 0, 0, hand, new short[32]);
}
