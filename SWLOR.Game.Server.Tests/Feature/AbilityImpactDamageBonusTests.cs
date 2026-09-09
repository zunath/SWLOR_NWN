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
    [TestCase(0, 0, false, false)]
    [TestCase(0, 75, false, true)]
    [TestCase(10, 0, false, true)]
    [TestCase(0, 0, true, true)]
    public void DamageCalculation_RecognizesCapturedDamageOnZeroBaseImpacts(
        int baseDamage, int capturedBonus, bool weapon, bool expected)
    {
        var hasDamage = typeof(Ability).GetMethod("HasCombatImpactDamage", BindingFlags.NonPublic | BindingFlags.Static)!
            .CreateDelegate<Func<int, int, bool, bool>>();
        hasDamage(baseDamage, capturedBonus, weapon).Should().Be(expected);
    }

    [Test]
    public void RepeatedPulseSnapshot_AppliesOnceAfterATargetIsImpacted()
    {
        var type = typeof(Ability).GetNestedType("TrackedAbilityImpact", BindingFlags.NonPublic)!;
        object Create(int damage = 0, int statusDamage = 0) => type.GetConstructors().Single().Invoke(new object[]
        {
            new AbilityDetail(), damage, 11, 13, 17, statusDamage, false, 19,
            Array.Empty<TelegraphGeometry>(), null
        });
        var source = Create(82, 7);
        var copy = type.GetMethod("CopyRepeatedDamageBonusesFrom")!;
        var complete = type.GetMethod("CompleteRepeatedDamageBonusImpact")!;
        int Damage(object impact) => (int)type.GetProperty("NextAbilityDamageBonus")!.GetValue(impact)!;
        var emptyPulse = Create();
        copy.Invoke(emptyPulse, new[] { source });
        Damage(emptyPulse).Should().Be(75, "a live next-attack status is not part of the scheduled snapshot");
        complete.Invoke(source, new object[] { false });
        var firstHitPulse = Create();
        copy.Invoke(firstHitPulse, new[] { source });
        Damage(firstHitPulse).Should().Be(75, "an empty pulse must retain the captured bonus");
        complete.Invoke(source, new object[] { true });
        var laterPulse = Create();
        copy.Invoke(laterPulse, new[] { source });
        Damage(laterPulse).Should().Be(0, "later pulses must not duplicate the consumed bonus");
    }

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

    [TestCase(false)]
    [TestCase(true)]
    public void ImpactPreparation_LeavesControlBonusesArmedAndConsumesOnceForDamage(bool scheduled)
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
            void DeclareDamage(int damage)
            {
                if (scheduled)
                    Ability.CaptureRepeatedAbilityImpact(caster, () => { }, baseDamage: damage);
                else
                    prepare(caster, damage);
            }
            DeclareDamage(0);
            calls.Should().Be(0, "control impacts must not invoke activation bonus consumers");
            DeclareDamage(100);
            DeclareDamage(100);
            calls.Should().Be(1, "multiple targets and phases share one consumption");
        }
        finally
        {
            impacts.Remove(caster);
        }
    }
}
