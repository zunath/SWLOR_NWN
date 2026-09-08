using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Service.TelegraphService;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityImpactDamageBonusTests
{
    private static readonly Func<int, AbilityDetail, Func<int>, (int BaseDamage, bool DealsDamage)> Apply = typeof(Ability)
        .GetMethod("ResolveCombatImpactBaseDamage", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<Func<int, AbilityDetail, Func<int>, (int, bool)>>();

    [TestCase(20)]
    [TestCase(75)]
    [TestCase(150)]
    public void ControlOnlyImpact_DoesNotGainPassiveDamage(int bonus)
    {
        Apply(0, new AbilityDetail(), () => bonus).Should().Be((0, false));
    }

    [Test]
    public void ControlOnlyImpact_DoesNotConsumeDamageBonus()
    {
        Apply(0, null, () => throw new AssertionException("Damage bonus must remain available"))
            .Should().Be((0, false));
    }

    [TestCase(100, false, 175)]
    [TestCase(0, true, 75)]
    [TestCase(100, true, 175)]
    public void DamagingImpact_AppliesBonusOnce(int baseDamage, bool usesWeaponDamage, int expected)
    {
        var calls = 0;
        var ability = new AbilityDetail
        {
            ActivationType = usesWeaponDamage ? AbilityActivationType.Weapon : AbilityActivationType.Casted
        };
        Apply(baseDamage, ability, () =>
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
        Apply(0, ability, () => 75).Should().Be((75, true));
    }

    [Test]
    public void DeferredDamageImpact_PreservesDamageBonus()
    {
        var ability = new AbilityDetail { DealsDeferredDamage = true };
        Apply(0, ability, () => 75).Should().Be((75, true));
    }

    [Test]
    public void Flash_DoesNotConsumeDamageBonusesOrFirstStrikeCounts()
    {
        var flash = new FlashAbilityDefinition().BuildAbilities()[FeatType.Flash1];
        Apply(0, flash, () => throw new AssertionException("Flash must retain damage bonuses"))
            .Should().Be((0, false));
    }

    [TestCase(FeatType.ShieldBash1)]
    [TestCase(FeatType.ShieldBash2)]
    [TestCase(FeatType.ShieldBash3)]
    [TestCase(FeatType.ShieldBash4)]
    public void ShieldBash_PreservesWeaponDamageAndBonusEligibility(FeatType feat)
    {
        var bash = new ShieldBashAbilityDefinition().BuildAbilities()[feat];
        Apply(0, bash, () => 75).Should().Be((75, true));
    }

    [Test]
    public void ImpactPreparation_LeavesControlBonusesArmedAndConsumesOnceForDamage()
    {
        var impactType = typeof(Ability).GetNestedType("TrackedAbilityImpact", BindingFlags.NonPublic)!;
        var flash = new FlashAbilityDefinition().BuildAbilities()[FeatType.Flash1];
        var impact = impactType.GetConstructors().Single().Invoke(new object[]
        {
            flash, 0, 0, 0, 0, 0, true, 0, Array.Empty<TelegraphGeometry>(), null
        });
        var calls = 0;
        impactType.GetProperty("ResolveDamageBonuses")!.SetValue(impact, (Action)(() => calls++));
        var impacts = (System.Collections.IDictionary)typeof(Ability)
            .GetField("_trackedAbilityImpacts", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var prepare = typeof(Ability).GetMethod("PrepareCombatImpactDamageBonuses", BindingFlags.NonPublic | BindingFlags.Static)!
            .CreateDelegate<Action<uint, int>>();
        const uint caster = 0xFFFFFFFE;
        impacts.Add(caster, impact);
        try
        {
            prepare(caster, 0);
            calls.Should().Be(0, "control impacts must not invoke activation bonus consumers");
            prepare(caster, 100);
            prepare(caster, 100);
            calls.Should().Be(1, "multiple targets and phases share one consumption");
        }
        finally
        {
            impacts.Remove(caster);
        }
    }
}
