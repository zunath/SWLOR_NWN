using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class PairedWeaponTimingTests
{
    [TestCase(220)]
    [TestCase(230)]
    [TestCase(240)]
    public void OrdinaryHaste_PreservesTheUnhastedPerkAdvantage(int weaponDelay)
    {
        int Effective(int offhand, int haste, int perk) => Combat.CalculateEffectiveAttackDelay(
            Combat.CalculateAttackDelayMilliseconds(weaponDelay, offhand, haste, perk));
        foreach (var perk in new[] { 0, 10, 20, 30 })
        {
            var baseline = 2d * Effective(0, 0, 0) / Effective(weaponDelay, 0, perk);
            for (var haste = -50; haste <= 50; haste++)
                (2d * Effective(0, haste, 0) / Effective(weaponDelay, haste, perk))
                    .Should().BeApproximately(baseline, 0.003, $"Delay {weaponDelay}, haste {haste}, off-hand reduction {perk}");
        }
    }

    [TestCase(210, 210)]
    [TestCase(220, 240)]
    [TestCase(230, 230)]
    [TestCase(240, 220)]
    public void Haste_NeverSlowsAWeaponAtTheDelayFloor(int main, int off)
    {
        foreach (var offhand in new[] { 0, off })
        {
            var previous = int.MaxValue;
            for (var haste = -50; haste <= 50; haste++)
            {
                var delay = Combat.CalculateEffectiveAttackDelay(
                    Combat.CalculateAttackDelayMilliseconds(main, offhand, haste, 30));
                delay.Should().BeLessThanOrEqualTo(previous);
                delay.Should().BeGreaterThanOrEqualTo(Combat.MinimumAttackDelayMilliseconds);
                previous = delay;
            }
        }
    }

    [Test]
    public void MixedWeapons_UseASymmetricHasteReference()
    {
        for (var haste = -50; haste <= 50; haste++)
        {
            var forwardBase = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelayMilliseconds(220, 240, 0, 30));
            var reverseBase = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelayMilliseconds(240, 220, 0, 30));
            var forward = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelayMilliseconds(220, 240, haste, 30));
            var reverse = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelayMilliseconds(240, 220, haste, 30));
            (forward / (double)forwardBase).Should().BeApproximately(reverse / (double)reverseBase, 0.001);
        }
    }
}
