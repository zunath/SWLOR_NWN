using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityImpactDamageBonusTests
{
    private static readonly Func<int, bool, Func<int>, int> Apply = typeof(Ability)
        .GetMethod("ApplyCombatImpactBaseDamageBonuses", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<Func<int, bool, Func<int>, int>>();

    [TestCase(20)]
    [TestCase(75)]
    [TestCase(150)]
    public void ControlOnlyImpact_DoesNotGainPassiveDamage(int bonus)
    {
        Apply(0, false, () => bonus).Should().Be(0);
    }

    [Test]
    public void ControlOnlyImpact_DoesNotConsumeDamageBonus()
    {
        Apply(0, false, () => throw new AssertionException("Damage bonus must remain available"))
            .Should().Be(0);
    }

    [TestCase(100, false, 175)]
    [TestCase(0, true, 75)]
    [TestCase(100, true, 175)]
    public void DamagingImpact_AppliesBonusOnce(int baseDamage, bool usesWeaponDamage, int expected)
    {
        var calls = 0;
        Apply(baseDamage, usesWeaponDamage, () =>
        {
            calls++;
            return 75;
        }).Should().Be(expected);
        calls.Should().Be(1);
    }
}
