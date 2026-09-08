using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityImpactDamageBonusTests
{
    private static readonly Func<int, SkillType, AbilityDetail, Func<int>, (int BaseDamage, bool DealsDamage)> Apply = typeof(Ability)
        .GetMethod("ResolveCombatImpactBaseDamage", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<Func<int, SkillType, AbilityDetail, Func<int>, (int, bool)>>();

    [TestCase(20)]
    [TestCase(75)]
    [TestCase(150)]
    public void ControlOnlyImpact_DoesNotGainPassiveDamage(int bonus)
    {
        Apply(0, SkillType.Mimicry, new AbilityDetail(), () => bonus).Should().Be((0, false));
    }

    [Test]
    public void ControlOnlyImpact_DoesNotConsumeDamageBonus()
    {
        Apply(0, SkillType.Force, null, () => throw new AssertionException("Damage bonus must remain available"))
            .Should().Be((0, false));
    }

    [TestCase(100, false, 175)]
    [TestCase(0, true, 75)]
    [TestCase(100, true, 175)]
    public void DamagingImpact_AppliesBonusOnce(int baseDamage, bool usesWeaponDamage, int expected)
    {
        var calls = 0;
        Apply(baseDamage, usesWeaponDamage ? SkillType.Vibroblade : SkillType.Mimicry, null, () =>
        {
            calls++;
            return 75;
        }).Should().Be((expected, true));
        calls.Should().Be(1);
    }

    [Test]
    public void QueuedNaturalWeaponImpact_PreservesDamageBonus()
    {
        var ability = new AbilityDetail { ActivationType = AbilityActivationType.Weapon };
        Apply(0, SkillType.BeastMastery, ability, () => 75).Should().Be((75, true));
    }

    [Test]
    public void DeferredDamageImpact_PreservesDamageBonus()
    {
        var ability = new AbilityDetail { DealsDeferredDamage = true };
        Apply(0, SkillType.Force, ability, () => 75).Should().Be((75, true));
    }
}
