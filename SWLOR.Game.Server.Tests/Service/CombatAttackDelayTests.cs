using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class CombatAttackDelayTests
{
    [Test]
    public void CalculateAttackDelayMilliseconds_UsesSingleWeaponDelay()
    {
        var delay = Combat.CalculateAttackDelayMilliseconds(210, 0, 0, 0);

        delay.Should().Be(3500);
        Combat.CalculateEffectiveAttackDelay(delay).Should().Be(1750);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_DualWieldCountsDefaultDelayOnce()
    {
        var delay = Combat.CalculateAttackDelayMilliseconds(210, 210, 0, 0);

        delay.Should().Be(5250);
        Combat.CalculateEffectiveAttackDelay(delay).Should().Be(3500);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_AppliesOffhandReductionBeforeCombiningDualWieldDelay()
    {
        var delay = Combat.CalculateAttackDelayMilliseconds(210, 210, 0, 30);

        delay.Should().Be(4200);
        Combat.CalculateEffectiveAttackDelay(delay).Should().Be(2450);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_SubtractsDefaultDelayFromHigherAttackerDelay()
    {
        var attackerDelay = Combat.BaseAttackDelayMilliseconds + 2500;

        var effectiveDelay = Combat.CalculateEffectiveAttackDelay(attackerDelay);

        effectiveDelay.Should().Be(2500);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_AllowsDelaysBelowDefaultAfterBaselineSubtraction()
    {
        var attackerDelay = Combat.BaseAttackDelayMilliseconds + 1250;

        var effectiveDelay = Combat.CalculateEffectiveAttackDelay(attackerDelay);

        effectiveDelay.Should().Be(1250);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_FastestWeaponDelayCanBenefitFromHaste()
    {
        var unmodifiedDelay = Combat.CalculateAttackDelayMilliseconds(220, 0, 0, 0);
        var hastenOneDelay = Combat.CalculateAttackDelayMilliseconds(220, 0, 15, 0);
        var hastenTwoDelay = Combat.CalculateAttackDelayMilliseconds(220, 0, 25, 0);

        Combat.CalculateEffectiveAttackDelay(unmodifiedDelay).Should().BeGreaterThan(Combat.BaseAttackDelayMilliseconds);
        Combat.CalculateEffectiveAttackDelay(hastenOneDelay).Should().BeGreaterThan(Combat.MinimumAttackDelayMilliseconds);
        Combat.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().BeGreaterThan(Combat.MinimumAttackDelayMilliseconds);
        Combat.CalculateEffectiveAttackDelay(hastenOneDelay).Should().BeLessThan(Combat.CalculateEffectiveAttackDelay(unmodifiedDelay));
        Combat.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().BeLessThan(Combat.CalculateEffectiveAttackDelay(hastenOneDelay));
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_NegativeHasteIncreasesDelay()
    {
        var unmodifiedDelay = Combat.CalculateAttackDelayMilliseconds(210, 0, 0, 0);
        var slowedDelay = Combat.CalculateAttackDelayMilliseconds(210, 0, -10, 0);

        unmodifiedDelay.Should().Be(3500);
        slowedDelay.Should().Be(3850);
        Combat.CalculateEffectiveAttackDelay(slowedDelay)
            .Should()
            .BeGreaterThan(Combat.CalculateEffectiveAttackDelay(unmodifiedDelay));
    }

    [Test]
    public void NaturalWeaponDelay_UsesFastestWeaponDelayAndBenefitsFromHaste()
    {
        var naturalWeaponTypes = new[]
        {
            BaseItem.CreatureSlashWeapon,
            BaseItem.CreaturePierceWeapon,
            BaseItem.CreatureBludgeonWeapon,
            BaseItem.CreatureSlashPierceWeapon
        };

        foreach (var naturalWeaponType in naturalWeaponTypes)
        {
            WeaponDelay.GetWeaponDelay(naturalWeaponType).Should().Be(24);
        }

        var unmodifiedDelay = Combat.CalculateAttackDelayMilliseconds(240, 0, 0, 0);
        var hastenOneDelay = Combat.CalculateAttackDelayMilliseconds(240, 0, 15, 0);
        var hastenTwoDelay = Combat.CalculateAttackDelayMilliseconds(240, 0, 25, 0);

        Combat.CalculateEffectiveAttackDelay(unmodifiedDelay).Should().Be(2250);
        Combat.CalculateEffectiveAttackDelay(hastenOneDelay).Should().Be(1650);
        Combat.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().Be(1250);
    }

    [Test]
    public void LegacySlingPistolDelay_UsesPistolDelay()
    {
        WeaponDelay.GetWeaponDelay(BaseItem.Sling).Should().Be(25);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_ClampsReducedDualWieldDelayToAbsoluteMinimum()
    {
        var delay = Combat.CalculateAttackDelayMilliseconds(210, 210, 45, 30);

        Combat.CalculateEffectiveAttackDelay(delay).Should().Be(Combat.MinimumAttackDelayMilliseconds);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_UsesDefaultDelayWhenAttackerDelayIsSameOrLower()
    {
        var attackerDelays = new[]
        {
            0,
            Combat.BaseAttackDelayMilliseconds - 1,
            Combat.BaseAttackDelayMilliseconds
        };

        foreach (var attackerDelay in attackerDelays)
        {
            var effectiveDelay = Combat.CalculateEffectiveAttackDelay(attackerDelay);

            effectiveDelay.Should().Be(Combat.BaseAttackDelayMilliseconds);
        }
    }

    [Test]
    public void MinimumAttackDelay_SupportsMaxAttacksPerSwingWithoutOverflow()
    {
        Combat.MinimumAttackDelayMilliseconds.Should().Be(584);
        (Combat.BaseAttackDelayMilliseconds / (float)Combat.MinimumAttackDelayMilliseconds)
            .Should()
            .BeLessThanOrEqualTo(Combat.MaxAttacksPerSwing);
    }

    [Test]
    public void CalculateAttackSwingDelay_FloorsAtBaseDelay()
    {
        Combat.CalculateAttackSwingDelay(584).Should().Be(Combat.BaseAttackDelayMilliseconds);
        Combat.CalculateAttackSwingDelay(Combat.BaseAttackDelayMilliseconds).Should().Be(Combat.BaseAttackDelayMilliseconds);
        Combat.CalculateAttackSwingDelay(2500).Should().Be(2500);
    }

    [Test]
    public void CalculateAttacksPerSwing_ResolvesOneAttackWhenDelayAtOrAboveSwingFloor()
    {
        foreach (var effectiveDelay in new[] { Combat.BaseAttackDelayMilliseconds, 2500, 5000 })
        {
            var attacks = Combat.CalculateAttacksPerSwing(effectiveDelay, 0f, out var attackDebt);

            attacks.Should().Be(1);
            attackDebt.Should().Be(0f);
        }
    }

    [Test]
    public void CalculateAttacksPerSwing_ResolvesTwoAttacksWhenDelayIsHalfSwingFloor()
    {
        var attacks = Combat.CalculateAttacksPerSwing(Combat.BaseAttackDelayMilliseconds / 2, 0f, out var attackDebt);

        attacks.Should().Be(2);
        attackDebt.Should().BeApproximately(0f, 0.01f);
    }

    [Test]
    public void CalculateAttacksPerSwing_CarriesFractionalAttacksBetweenSwings()
    {
        // 1000ms delay = 1.75 attacks per 1750ms swing; long-run average must match.
        const int effectiveDelay = 1000;
        var attackDebt = 0f;
        var totalAttacks = 0;
        const int swings = 100;

        for (var i = 0; i < swings; i++)
        {
            totalAttacks += Combat.CalculateAttacksPerSwing(effectiveDelay, attackDebt, out attackDebt);
        }

        var expectedAttacks = swings * (Combat.BaseAttackDelayMilliseconds / (float)effectiveDelay);
        totalAttacks.Should().BeCloseTo((int)expectedAttacks, 2);
    }

    // A no-delay buff (Venom Tempo, Fast Strikes, the Lightsaber critical no-delay, Last Word) is
    // applied by lowering the effective delay to MinimumAttackDelayMilliseconds. That is a no-op for
    // a build already at that floor, so ConsumeAttacksPerSwing guarantees the buff is worth an extra
    // attack regardless of how fast the attacker already swings.

    [TestCase(1750, TestName = "NoDelayBuff_GrantsExtraAttack_SlowBuild")]
    [TestCase(3500, TestName = "NoDelayBuff_GrantsExtraAttack_DualWieldBuild")]
    [TestCase(875, TestName = "NoDelayBuff_GrantsExtraAttack_HastedBuild")]
    [TestCase(750, TestName = "NoDelayBuff_GrantsExtraAttack_HeavilyHastedBuild")]
    public void ConsumeAttacksPerSwing_NoDelayBuffAlwaysBeatsTheUnbuffedSwing(int unbuffedDelay)
    {
        const uint attacker = 0x7F000001;
        Combat.ClearAttackSwingDebt(attacker);
        var unbuffed = Combat.ConsumeAttacksPerSwing(attacker, unbuffedDelay, unbuffedDelay, false);

        Combat.ClearAttackSwingDebt(attacker);
        var buffed = Combat.ConsumeAttacksPerSwing(
            attacker,
            Combat.MinimumAttackDelayMilliseconds,
            unbuffedDelay,
            true);

        Combat.ClearAttackSwingDebt(attacker);

        buffed.Should().BeGreaterThan(
            unbuffed,
            "a no-delay buff must be worth at least one extra attack even when the attacker is " +
            "already at the attack-delay floor");
        buffed.Should().BeLessThanOrEqualTo(Combat.MaxAttacksPerSwing);
    }

    [Test]
    public void ConsumeAttacksPerSwing_AtTheDelayFloor_NoDelayBuffIsNotANoOp()
    {
        // Regression: a heavily hasted or dual-wielding Vibroknife build sits at the floor already,
        // so overriding the delay with the same floor value used to change nothing at all.
        // Both delays are genuinely equal here, which is what the real attack path produces for this
        // build - so the buff state has to travel as its own flag rather than be inferred.
        const uint attacker = 0x7F000002;
        Combat.ClearAttackSwingDebt(attacker);
        var unbuffed = Combat.ConsumeAttacksPerSwing(
            attacker,
            Combat.MinimumAttackDelayMilliseconds,
            Combat.MinimumAttackDelayMilliseconds,
            false);

        Combat.ClearAttackSwingDebt(attacker);
        var buffed = Combat.ConsumeAttacksPerSwing(
            attacker,
            Combat.MinimumAttackDelayMilliseconds,
            Combat.MinimumAttackDelayMilliseconds,
            true);

        Combat.ClearAttackSwingDebt(attacker);

        unbuffed.Should().Be(2);
        buffed.Should().Be(3);
    }

    [Test]
    public void ConsumeAttacksPerSwing_WithoutABuff_IsUnchanged()
    {
        // The guarantee must only engage when a no-delay buff is actually active, so ordinary swings
        // keep their existing attack counts and debt accounting.
        const uint attacker = 0x7F000003;
        foreach (var delay in new[] { 3500, 1750, 875, Combat.MinimumAttackDelayMilliseconds })
        {
            Combat.ClearAttackSwingDebt(attacker);
            var viaOverload = Combat.ConsumeAttacksPerSwing(attacker, delay, delay, false);

            Combat.ClearAttackSwingDebt(attacker);
            var viaOriginal = Combat.ConsumeAttacksPerSwing(attacker, delay);

            Combat.ClearAttackSwingDebt(attacker);
            viaOverload.Should().Be(viaOriginal, $"delay {delay} should be unaffected by the guarantee");
        }
    }

    [TestCase(2, 1, 1, 1)]
    [TestCase(3, 1, 2, 2)]
    [TestCase(3, 2, 1, 2)]
    [TestCase(3, 2, 3, 3)]
    public void CapAttacksPerSwingForLimitedAttackEffect_DoesNotOverscheduleCharges(
        int acceleratedAttacks,
        int baselineAttacks,
        int remainingAttacks,
        int expectedAttacks)
    {
        Combat.CapAttacksPerSwingForLimitedAttackEffect(
                acceleratedAttacks,
                baselineAttacks,
                remainingAttacks)
            .Should().Be(expectedAttacks);
    }

    [TestCase(2, 1, 1, 2)]
    [TestCase(3, 2, 1, 3)]
    [TestCase(4, 1, 3, 3)]
    public void CapAttacksPerSwingForLimitedNoDelay_PreservesTheGuaranteedExtraRoll(
        int acceleratedAttacks,
        int baselineAttacks,
        int remainingAttacks,
        int expectedAttacks)
    {
        Combat.CapAttacksPerSwingForLimitedAttackEffect(
                acceleratedAttacks,
                baselineAttacks,
                0,
                remainingAttacks)
            .Should().Be(expectedAttacks);
    }

    [Test]
    public void ConsumeAttacksPerSwing_DropsAcceleratedDebtWhenTheSwingConsumesEveryRemainingCharge()
    {
        const uint attacker = 0x7F000004;
        const int acceleratedDelay = 750;
        const int baselineDelay = 1750;
        const int remainingCharges = 2;
        Combat.ClearAttackSwingDebt(attacker);

        var attacks = Combat.ConsumeAttacksPerSwing(
            attacker,
            acceleratedDelay,
            baselineDelay,
            false,
            baselineDelay,
            remainingCharges);

        var debts = (Dictionary<uint, float>)typeof(Combat)
            .GetField("_attackSwingDebts", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
        try
        {
            attacks.Should().Be(2);
            debts.Should().NotContainKey(attacker,
                "both matching rolls consume the two remaining charges, so accelerated debt must expire with the effect");
        }
        finally
        {
            Combat.ClearAttackSwingDebt(attacker);
        }
    }

    [Test]
    public void ConsumeAttacksPerSwing_DoesNotScheduleAnAcceleratedRollBeyondTheFinalCharge()
    {
        const uint attacker = 0x7F000007;
        const int acceleratedDelay = 875;
        const int baselineDelay = 1750;
        Combat.ClearAttackSwingDebt(attacker);

        var debts = (Dictionary<uint, float>)typeof(Combat)
            .GetField("_attackSwingDebts", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

        try
        {
            // One carried roll makes this swing worth three accelerated rolls but two baseline
            // rolls. The one remaining charge is consumed by the first baseline roll, so it must
            // not add a third roll to the count passed to ResolveAttack.
            debts[attacker] = 1f;
            var attacks = Combat.ConsumeAttacksPerSwing(
                attacker,
                acceleratedDelay,
                baselineDelay,
                false,
                baselineDelay,
                1);

            attacks.Should().Be(2);
            debts.Should().NotContainKey(attacker,
                "the uncharged third roll must not be pre-scheduled and limited-speed debt expires with the charge");
        }
        finally
        {
            Combat.ClearAttackSwingDebt(attacker);
        }
    }

    [Test]
    public void ConsumeAttacksPerSwing_FinalLimitedNoDelayChargeStillGrantsAnExtraFloorRoll()
    {
        const uint attacker = 0x7F000009;
        Combat.ClearAttackSwingDebt(attacker);

        try
        {
            var attacks = Combat.ConsumeAttacksPerSwing(
                attacker,
                Combat.MinimumAttackDelayMilliseconds,
                Combat.MinimumAttackDelayMilliseconds,
                true,
                Combat.MinimumAttackDelayMilliseconds,
                0,
                1);

            attacks.Should().Be(Combat.MaxAttacksPerSwing,
                "the final Dead Man's Hand charge guarantees one extra roll even when baseline cadence is already at the floor");
        }
        finally
        {
            Combat.ClearAttackSwingDebt(attacker);
        }
    }

    [Test]
    public void NativeAttackAction_StillCapsLimitedChargesWhenAnArmedNoDelayOverlaps()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Native",
            "OnAIActionAttackObject.cs"));
        var countStart = source.IndexOf(
            "var limitedDelayReductionRemainingAttacks = hasLimitedAttackDelayReduction",
            System.StringComparison.Ordinal);
        var countEnd = source.IndexOf(
            "// The delay the attacker would have without a no-delay buff.",
            countStart,
            System.StringComparison.Ordinal);

        countStart.Should().BeGreaterThanOrEqualTo(0);
        countEnd.Should().BeGreaterThan(countStart);
        var countSetup = source[countStart..countEnd];
        countSetup.Should().Contain("limitedAttackDelayReductionRemainingAttacks");
        countSetup.Should().Contain("limitedAttackNoDelayRemainingAttacks");
        countSetup.Should().NotContain("if (hasArmedNoDelay)",
            "an armed timing effect must not disable the cap that protects concurrently active limited charges");
    }

    [Test]
    public void ConsumeAttacksPerSwing_PreservesAcceleratedDebtWhenChargesRemainAfterTheSwing()
    {
        const uint attacker = 0x7F000005;
        const int acceleratedDelay = 750;
        const int baselineDelay = 1750;
        const int remainingCharges = 3;
        Combat.ClearAttackSwingDebt(attacker);

        var attacks = Combat.ConsumeAttacksPerSwing(
            attacker,
            acceleratedDelay,
            baselineDelay,
            false,
            baselineDelay,
            remainingCharges);

        var debts = (Dictionary<uint, float>)typeof(Combat)
            .GetField("_attackSwingDebts", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
        Combat.CalculateAttacksPerSwing(acceleratedDelay, 0f, out var expectedAcceleratedDebt);

        try
        {
            attacks.Should().Be(2);
            debts[attacker].Should().BeApproximately(expectedAcceleratedDebt, 0.0001f,
                "two matching rolls leave one charge active, so its accelerated debt still belongs to the live effect");
        }
        finally
        {
            Combat.ClearAttackSwingDebt(attacker);
        }
    }

    [Test]
    public void ConsumeAttacksPerSwing_DropsAcceleratedDebtAccumulatedBeforeTheFinalCharge()
    {
        const uint attacker = 0x7F000006;
        const int acceleratedDelay = 1400;
        const int baselineDelay = 1750;
        Combat.ClearAttackSwingDebt(attacker);

        var debts = (Dictionary<uint, float>)typeof(Combat)
            .GetField("_attackSwingDebts", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

        try
        {
            var firstSwingAttacks = Combat.ConsumeAttacksPerSwing(
                attacker,
                acceleratedDelay,
                baselineDelay,
                false,
                baselineDelay,
                2);
            firstSwingAttacks.Should().Be(1);
            debts[attacker].Should().BeApproximately(0.25f, 0.0001f,
                "the first accelerated one-roll swing carries fractional debt while one charge remains");

            var finalSwingAttacks = Combat.ConsumeAttacksPerSwing(
                attacker,
                acceleratedDelay,
                baselineDelay,
                false,
                baselineDelay,
                1);
            finalSwingAttacks.Should().Be(1);
            debts.Should().NotContainKey(attacker,
                "the parallel baseline ledger must replace debt accumulated over earlier limited-speed swings");
        }
        finally
        {
            Combat.ClearAttackSwingDebt(attacker);
        }
    }

    [Test]
    public void ConsumeAttacksPerSwing_SuppressedLimitedSpeedCalculatesFromBaselineDebt()
    {
        const uint attacker = 0x7F000008;
        const int acceleratedDelay = 1000;
        const int baselineDelay = 1400;
        Combat.ClearAttackSwingDebt(attacker);

        var debts = (Dictionary<uint, float>)typeof(Combat)
            .GetField("_attackSwingDebts", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

        try
        {
            var acceleratedAttacks = Combat.ConsumeAttacksPerSwing(
                attacker,
                acceleratedDelay,
                baselineDelay,
                false,
                baselineDelay,
                2);
            acceleratedAttacks.Should().Be(1);
            debts[attacker].Should().BeApproximately(0.75f, 0.0001f);

            var suppressedAttacks = Combat.ConsumeAttacksPerSwing(
                attacker,
                baselineDelay,
                baselineDelay,
                false,
                baselineDelay,
                0);

            suppressedAttacks.Should().Be(1,
                "Signal Jammer must not turn accelerated debt into a second roll on the suppressed swing");
            debts[attacker].Should().BeApproximately(0.5f, 0.0001f,
                "only debt earned at the unsuppressed baseline cadence may survive the final charge");
        }
        finally
        {
            Combat.ClearAttackSwingDebt(attacker);
        }
    }

    [Test]
    public void CalculateAttacksPerSwing_CapsAttacksAtMaxPerSwing()
    {
        var attacks = Combat.CalculateAttacksPerSwing(Combat.MinimumAttackDelayMilliseconds, 5f, out var attackDebt);

        attacks.Should().Be(Combat.MaxAttacksPerSwing);
        attackDebt.Should().BeLessThanOrEqualTo(Combat.MaxAttacksPerSwing);
    }

    [Test]
    public void CalculateAttacksPerSwing_MinimumDelayAveragesToMaxAttacksPerSwing()
    {
        var attackDebt = 0f;
        var totalAttacks = 0;
        const int swings = 60;

        for (var i = 0; i < swings; i++)
        {
            totalAttacks += Combat.CalculateAttacksPerSwing(Combat.MinimumAttackDelayMilliseconds, attackDebt, out attackDebt);
        }

        var expectedAttacks = swings * (Combat.BaseAttackDelayMilliseconds / (float)Combat.MinimumAttackDelayMilliseconds);
        totalAttacks.Should().BeCloseTo((int)expectedAttacks, 2);
    }

    [Test]
    public void ConsumeAttacksPerSwing_TracksDebtPerAttacker()
    {
        const uint attackerOne = 100;
        const uint attackerTwo = 200;
        const int effectiveDelay = 1000;

        Combat.ClearAttackSwingDebt(attackerOne);
        Combat.ClearAttackSwingDebt(attackerTwo);

        Combat.ConsumeAttacksPerSwing(attackerOne, effectiveDelay).Should().Be(1);
        Combat.ConsumeAttacksPerSwing(attackerTwo, effectiveDelay).Should().Be(1);
        Combat.ConsumeAttacksPerSwing(attackerOne, effectiveDelay).Should().Be(2);
        Combat.ConsumeAttacksPerSwing(attackerTwo, effectiveDelay).Should().Be(2);

        Combat.ClearAttackSwingDebt(attackerOne);
        Combat.ClearAttackSwingDebt(attackerTwo);
    }

    [Test]
    public void ClearAttackSwingDebt_ResetsStoredDebt()
    {
        const uint attacker = 300;
        const int effectiveDelay = 1000;

        Combat.ClearAttackSwingDebt(attacker);

        Combat.ConsumeAttacksPerSwing(attacker, effectiveDelay).Should().Be(1);
        Combat.ClearAttackSwingDebt(attacker);
        Combat.ConsumeAttacksPerSwing(attacker, effectiveDelay).Should().Be(1);

        Combat.ClearAttackSwingDebt(attacker);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_UsesFastestFloorWhenNoDelayAttackIsQueued()
    {
        var attackerDelay = Combat.BaseAttackDelayMilliseconds + 2000;

        var effectiveDelay = Combat.CalculateEffectiveAttackDelay(attackerDelay, true);

        effectiveDelay.Should().Be(Combat.MinimumAttackDelayMilliseconds);
    }

    [Test]
    public void CanConsumeNextAbilityNoDelay_RequiresHostileAbility()
    {
        Combat.CanConsumeNextAbilityNoDelay(new AbilityDetail
        {
            IsHostileAbility = true,
            SkillType = SkillType.Pistol
        })
            .Should()
            .BeTrue();

        Combat.CanConsumeNextAbilityNoDelay(new AbilityDetail
        {
            IsHostileAbility = false
        })
            .Should()
            .BeFalse();

        Combat.CanConsumeNextAbilityNoDelay(new AbilityDetail
        {
            IsHostileAbility = true,
            SkillType = SkillType.Invalid
        })
            .Should()
            .BeFalse("an equipped weapon must not turn a skillless cast into a weapon attack");
    }

    [Test]
    public void ConsumeNextAbilityDelayReduction_ReturnsNothingWhenUnarmedOrNonHostile()
    {
        const uint creature = 310;

        Combat.ConsumeNextAbilityDelayReductionPercent(creature, new AbilityDetail
        {
            IsHostileAbility = true,
            SkillType = SkillType.Pistol
        }).Should().Be(0);

        Combat.ConsumeNextAbilityDelayReductionPercent(creature, new AbilityDetail
        {
            IsHostileAbility = false,
            SkillType = SkillType.Pistol
        }).Should().Be(0);
    }

    [Test]
    public void ConsumeNextAbilityDelayReduction_RejectsSkilllessHostileCasts()
    {
        var skilllessHostileAbility = new AbilityDetail
        {
            IsHostileAbility = true,
            SkillType = SkillType.Invalid
        };

        Combat.ConsumeNextAbilityDelayReductionPercent(310, skilllessHostileAbility)
            .Should()
            .Be(0);
    }

    [Test]
    public void WeaponAbilitiesDeclareSkillsWhileSkilllessCastsStayOutsideWeaponTimingEffects()
    {
        var abilities = typeof(IAbilityListDefinition).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract &&
                           !type.IsInterface &&
                           typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type =>
                ((IAbilityListDefinition)System.Activator.CreateInstance(type)!)
                .BuildAbilities()
                .Select(pair => (Definition: type.Name, Feat: pair.Key, Detail: pair.Value)))
            .ToArray();

        var skilllessWeaponAbilities = abilities
            .Where(ability => ability.Detail.ActivationType == AbilityActivationType.Weapon &&
                              ability.Detail.SkillType == SkillType.Invalid)
            .Select(ability => $"{ability.Definition}:{ability.Feat}")
            .ToArray();
        skilllessWeaponAbilities.Should().BeEmpty(
            "weapon timing effects require an explicitly declared weapon skill");

        var skilllessHostileCasts = abilities
            .Where(ability => ability.Detail.IsHostileAbility &&
                              ability.Detail.ActivationType != AbilityActivationType.Weapon &&
                              ability.Detail.SkillType == SkillType.Invalid)
            .ToArray();
        skilllessHostileCasts.Should().NotBeEmpty("Provoke-style hostile casts are intentionally skillless");
        skilllessHostileCasts.Should().OnlyContain(ability =>
            !Combat.CanConsumeNextAbilityNoDelay(ability.Detail));
    }

    [Test]
    public void ConsumeNextAbilityDelayReduction_PreservesArmedBuffWhileRangedNoDelayStatusApplies()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Service", "Combat.cs"));
        var method = source[source.IndexOf(
            "public static int ConsumeNextAbilityDelayReductionPercent(uint creature, AbilityDetail ability)",
            System.StringComparison.Ordinal)..];
        method = method[..method.IndexOf(
            "private static int ConsumeNextAbilityDelayReductionPercent(uint creature, SkillType skillType)",
            System.StringComparison.Ordinal)];

        var rangedStatusGuard = method.IndexOf("if (hasRangedStatusNoDelay)", System.StringComparison.Ordinal);
        var consumingCall = method.IndexOf(
            "var temporaryReductionPercent = ConsumeNextAbilityDelayReductionPercent(creature, skillType);",
            System.StringComparison.Ordinal);

        rangedStatusGuard.Should().BeGreaterThanOrEqualTo(0);
        method[(rangedStatusGuard)..consumingCall].Should().Contain("return 100;");
        consumingCall.Should().BeGreaterThan(rangedStatusGuard,
            "the ranged status must return before the temporary next-ability arm can be consumed");
    }

    [Test]
    public void ConsumeNextAutoAttackNoDelay_ChecksSuppressionBeforeReadingArmedState()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Service",
            "Combat.cs"));
        var methodStart = source.IndexOf(
            "public static bool ConsumeNextAutoAttackNoDelay(uint creature, SkillType skillType)",
            System.StringComparison.Ordinal);
        var methodEnd = source.IndexOf(
            "public static int ConsumeNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType)",
            methodStart,
            System.StringComparison.Ordinal);
        methodStart.Should().BeGreaterThanOrEqualTo(0);
        methodEnd.Should().BeGreaterThan(methodStart);

        var method = source[methodStart..methodEnd];
        var suppressionGuard = method.IndexOf(
            "if (IsAttackDelayReductionSuppressed(creature))",
            System.StringComparison.Ordinal);
        var armedStateRead = method.IndexOf(
            "StatType.NextAutoAttackNoDelayAllSkills",
            System.StringComparison.Ordinal);
        suppressionGuard.Should().BeGreaterThanOrEqualTo(0);
        armedStateRead.Should().BeGreaterThan(suppressionGuard,
            "suppressed timing must return before an armed all-skills or skill-specific modifier can be consumed");
    }

    [Test]
    public void ConsumeNextAutoAttackNoDelay_PreservesArmedBuffWhileRangedNoDelayStatusApplies()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Service",
            "Combat.cs"));
        var methodStart = source.IndexOf(
            "public static bool ConsumeNextAutoAttackNoDelay(uint creature, SkillType skillType)",
            System.StringComparison.Ordinal);
        var methodEnd = source.IndexOf(
            "public static int ConsumeNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType)",
            methodStart,
            System.StringComparison.Ordinal);
        methodStart.Should().BeGreaterThanOrEqualTo(0);
        methodEnd.Should().BeGreaterThan(methodStart);

        var method = source[methodStart..methodEnd];
        var rangedStatusGuard = method.IndexOf(
            "if (appliesToRangedStatus)",
            System.StringComparison.Ordinal);
        var firstConsume = method.IndexOf(
            "TemporaryStatModifier.Consume(",
            System.StringComparison.Ordinal);

        rangedStatusGuard.Should().BeGreaterThanOrEqualTo(0);
        firstConsume.Should().BeGreaterThan(rangedStatusGuard);
        method[rangedStatusGuard..firstConsume].Should().Contain("return true;");
    }

    [Test]
    public void LimitedAttackSpeedBonuses_AreSuppressedByAttackDelayReductionSuppression()
    {
        const uint creature = 311;
        var creatureEffects = GetCreatureEffects();

        try
        {
            var tracker = new CreatureStatusEffect();
            creatureEffects[creature] = tracker;
            tracker.Add(new LimitedHasteStatusEffect(
                20,
                2,
                SkillType.Pistol,
                EffectIconType.ReloadTempoStatusEffect,
                new AbilityImpactSummary()));

            Combat.ConsumeNextAbilityDelayReductionPercent(creature, new AbilityDetail
            {
                IsHostileAbility = true,
                SkillType = SkillType.Pistol
            }).Should().Be(20);

            tracker.Add(new DeadMansHandStatusEffect());
            StatusEffect.TryGetLimitedAttackNoDelay(
                    creature,
                    SkillType.Pistol,
                    out var noDelayRemainingAttacks)
                .Should()
                .BeTrue();
            noDelayRemainingAttacks.Should().Be(3);
            Combat.HasNextAutoAttackNoDelay(creature, SkillType.Pistol).Should().BeTrue();
            Combat.ConsumeNextAbilityDelayReductionPercent(creature, new AbilityDetail
            {
                IsHostileAbility = true,
                SkillType = SkillType.Pistol
            }).Should().Be(100);

            var signalJammer = new SignalJammerStatusEffect();
            tracker.Add(signalJammer);
            signalJammer.StatGroup.Stats[StatType.AttackDelayReductionSuppressed].Should().Be(1);
            Stat.GetStatTypeCategory(StatType.AttackDelayReductionSuppressed)
                .Should().Be(StatTypeCategory.BeneficialWhenNegative);
            Stat.GetStatTypeAggregation(StatType.AttackDelayReductionSuppressed)
                .Should().Be(StatTypeAggregation.Maximum);

            StatusEffect.TryGetLimitedAttackDelayReduction(
                    creature,
                    SkillType.Pistol,
                    out var reductionPercent,
                    out var remainingAttacks)
                .Should()
                .BeFalse();
            reductionPercent.Should().Be(0);
            remainingAttacks.Should().Be(0);
            StatusEffect.TryGetLimitedAttackNoDelay(
                    creature,
                    SkillType.Pistol,
                    out noDelayRemainingAttacks)
                .Should()
                .BeFalse();
            noDelayRemainingAttacks.Should().Be(0);
            Combat.HasNextAutoAttackNoDelay(creature, SkillType.Pistol).Should().BeFalse();
            Combat.ConsumeNextAutoAttackNoDelay(creature, SkillType.Pistol).Should().BeFalse();
            Combat.ConsumeNextAbilityDelayReductionPercent(creature, new AbilityDetail
            {
                IsHostileAbility = true,
                SkillType = SkillType.Pistol
            }).Should().Be(0);
        }
        finally
        {
            creatureEffects.Remove(creature);
        }
    }

    [Test]
    public void ReloadTempo_DeclaresLimitedHasteForTheNextTwoAttacks()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Feature", "PerkDefinition", "PistolPerkDefinition.cs"));
        var reloadTempo = source[source.IndexOf("private void ReloadTempo()", StringComparison.Ordinal)..];
        reloadTempo = reloadTempo[..reloadTempo.IndexOf("private void ", 1, StringComparison.Ordinal)];

        reloadTempo.Should().Contain("gain +20% Haste for your next two attacks");
        reloadTempo.Should().Contain(".IncreasesStat(StatType.CriticalHitLimitedHastePercentAdjustment, 20)");
        reloadTempo.Should().Contain(".IncreasesStat(StatType.CriticalHitLimitedHasteDurationSeconds, 30)");
        reloadTempo.Should().Contain(".IncreasesStat(StatType.CriticalHitLimitedHasteAttackCount, 2)");
        reloadTempo.Should().Contain(
            ".IncreasesStat(StatType.CriticalHitLimitedHasteStatusEffectIcon, (int)EffectIconType.ReloadTempoStatusEffect)");

        var combat = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Service", "Combat.cs"));
        var grant = combat[combat.IndexOf(
            "private static void ApplyCriticalHitLimitedHaste(",
            StringComparison.Ordinal)..combat.IndexOf(
            "private static void ApplyCriticalNextAutoAttackNoDelay(",
            StringComparison.Ordinal)];
        grant.Should().Contain("SkillType.Invalid,",
            "the triggering critical is Pistol-scoped, but the two promised Haste charges apply to any attacks");
    }

    [Test]
    public void ReloadTempo_TriggersBeforeThePositiveCriticalDamageGuard()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Service", "Combat.cs"));
        var method = source[source.IndexOf(
            "public static void ApplyCriticalHitEffects(",
            StringComparison.Ordinal)..];
        method = method[..method.IndexOf(
            "private static void ApplyCriticalHitSelfEffects(",
            StringComparison.Ordinal)];

        var limitedHasteTrigger = method.IndexOf(
            "ApplyCriticalHitLimitedHaste(attacker, skillType);",
            StringComparison.Ordinal);
        var positiveDamageGuard = method.IndexOf("if (damage <= 0)", StringComparison.Ordinal);

        limitedHasteTrigger.Should().BeGreaterThanOrEqualTo(0);
        limitedHasteTrigger.Should().BeLessThan(positiveDamageGuard,
            "a fully mitigated critical still earns Reload Tempo's next-two-attacks Haste");

        var damageRoll = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Native", "GetDamageRoll.cs"));
        var landedDamageHandlingStart = damageRoll.IndexOf(
            "if (isLandedAttack)",
            StringComparison.Ordinal);
        var landedDamageHandlingEnd = damageRoll.IndexOf(
            "ProfilerPlugin.PopPerfScope();",
            landedDamageHandlingStart,
            StringComparison.Ordinal);
        var landedDamageHandling = damageRoll[landedDamageHandlingStart..landedDamageHandlingEnd];
        landedDamageHandling.Should().Contain("Combat.ApplyCriticalHitEffects(");
        landedDamageHandling.Should().NotContain("if (isLandedAttack && totalDamage > 0)",
            "a fully mitigated landed native critical must still notify critical-hit effects");
    }

    [Test]
    public void WeaponDelayMigration_CoversLivePlayerInventoryAndSerializedItems()
    {
        var root = FindRepositoryRoot();
        var playerMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "PlayerMigration",
            "_14_MigrateResistanceItemProperties.cs"));
        var serverMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "StoredItemDataMigration.cs"));
        var weaponDelayMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "SerializedItemWeaponDamageTypeMigration.cs"));

        playerMigrationSource.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateObject(player);");
        serverMigrationSource.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateObject(obj)");
        weaponDelayMigrationSource.Should().Contain("ItemPropertyType.Delay");
        weaponDelayMigrationSource.Should().Contain("WeaponDelay.GetWeaponDelay(baseItem)");
        weaponDelayMigrationSource.Should().Contain("BuildWeaponBaseItemTypes");
        weaponDelayMigrationSource.Should().Contain("StaffBaseItemTypes");
        weaponDelayMigrationSource.Should().Contain("[\"t_knife\"] = 22");
        weaponDelayMigrationSource.Should().Contain("[\"t_shuriken\"] = 22");
        weaponDelayMigrationSource.Should().Contain("[\"t_rifle\"] = 30");
        weaponDelayMigrationSource.Should().Contain("[\"t_twinblade\"] = 29");
        weaponDelayMigrationSource.Should().Contain("[\"byyskwarriorswor\"] = 22");
        weaponDelayMigrationSource.Should().Contain("[\"sith_blade\"] = 22");
        weaponDelayMigrationSource.Should().Contain("[\"wswss002\"] = 22");
        weaponDelayMigrationSource.Should().Contain("GetHasInventory(obj)");
        weaponDelayMigrationSource.Should().Contain("GetItemInSlot((InventorySlot)index, creature)");
    }

    [Test]
    public void ModuleWeaponDelayProperties_AreNormalized()
    {
        var root = FindRepositoryRoot();
        var moduleRoot = Path.Combine(root.FullName, "Module");
        var files = Directory.EnumerateFiles(Path.Combine(moduleRoot, "uti"), "*.json")
            .Concat(Directory.EnumerateFiles(Path.Combine(moduleRoot, "git"), "*.json"));
        var findings = new List<string>();

        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            InspectWeaponDelays(document.RootElement, Path.GetRelativePath(root.FullName, file), string.Empty, findings);
        }

        findings.Should().BeEmpty(string.Join("\n", findings.Take(25)));
    }

    [Test]
    public void ModuleShieldItems_DoNotHaveDelayProperties()
    {
        var root = FindRepositoryRoot();
        var moduleRoot = Path.Combine(root.FullName, "Module");
        var files = Directory.EnumerateFiles(Path.Combine(moduleRoot, "uti"), "*.json")
            .Concat(Directory.EnumerateFiles(Path.Combine(moduleRoot, "git"), "*.json"))
            .Concat(Directory.EnumerateFiles(Path.Combine(moduleRoot, "utc"), "*.json"));
        var findings = new List<string>();

        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            InspectShieldDelays(document.RootElement, Path.GetRelativePath(root.FullName, file), string.Empty, findings);
        }

        findings.Should().BeEmpty(string.Join("\n", findings.Take(25)));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }

    private static Dictionary<uint, CreatureStatusEffect> GetCreatureEffects()
    {
        return (Dictionary<uint, CreatureStatusEffect>)typeof(StatusEffect)
            .GetField("_creatureEffects", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
    }

    private static readonly IReadOnlyDictionary<int, int> WeaponDelayCostByBaseItem = BuildWeaponDelayCostByBaseItem();
    private static readonly IReadOnlySet<int> ShieldBaseItems = SWLOR.Game.Server.Service.Item.ShieldBaseItemTypes
        .Select(x => (int)x)
        .ToHashSet();

    private static IReadOnlyDictionary<int, int> BuildWeaponDelayCostByBaseItem()
    {
        var delays = new Dictionary<int, int>();
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.VibrobladeBaseItemTypes, 23);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.KatarBaseItemTypes, 22);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.TwinBladeBaseItemTypes, 29);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.VibroknifeBaseItemTypes, 22);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.StaffBaseItemTypes, 27);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.RifleBaseItemTypes, 30);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.HeavyVibrobladeBaseItemTypes, 30);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.PistolBaseItemTypes, 25);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.LightsaberBaseItemTypes, 24);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.SpearBaseItemTypes, 28);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.ThrowingWeaponBaseItemTypes, 22);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.SaberstaffBaseItemTypes, 29);
        AddWeaponDelays(delays, SWLOR.Game.Server.Service.Item.CreatureBaseItemTypes, 24);

        return delays;
    }

    private static void AddWeaponDelays(
        Dictionary<int, int> delays,
        IEnumerable<BaseItem> baseItems,
        int delayCost)
    {
        foreach (var baseItem in baseItems)
            delays[(int)baseItem] = delayCost;
    }

    private static void InspectWeaponDelays(
        JsonElement element,
        string file,
        string path,
        ICollection<string> findings)
    {
        InspectItemDelays(element, file, path, findings, InspectWeaponDelay);
    }

    private static void InspectShieldDelays(
        JsonElement element,
        string file,
        string path,
        ICollection<string> findings)
    {
        InspectItemDelays(element, file, path, findings, InspectShieldDelay);
    }

    private static void InspectItemDelays(
        JsonElement element,
        string file,
        string path,
        ICollection<string> findings,
        Action<int, JsonElement, string, ICollection<string>> inspectItemDelay)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (TryGetWrappedInt(element, "BaseItem", out var baseItem) &&
                    TryGetWrappedValue(element, "PropertiesList", out var propertiesList))
                {
                    inspectItemDelay(baseItem, propertiesList, $"{file}:{path}", findings);
                }

                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name == "__struct_id")
                        continue;

                    InspectItemDelays(
                        property.Value,
                        file,
                        string.IsNullOrWhiteSpace(path) ? property.Name : $"{path}.{property.Name}",
                        findings,
                        inspectItemDelay);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    InspectItemDelays(item, file, $"{path}[{index}]", findings, inspectItemDelay);
                    index++;
                }
                break;
        }
    }

    private static void InspectWeaponDelay(
        int baseItem,
        JsonElement propertiesList,
        string findingPath,
        ICollection<string> findings)
    {
        if (!WeaponDelayCostByBaseItem.TryGetValue(baseItem, out var expectedDelayCost))
            return;

        var delayCosts = GetDelayCostValues(propertiesList).ToList();
        if (delayCosts.Count == 0)
        {
            findings.Add($"{findingPath} missing weapon Delay");
        }
        else if (delayCosts.Any(x => x != expectedDelayCost))
        {
            findings.Add($"{findingPath} weapon Delay [{string.Join(", ", delayCosts)}] should be {expectedDelayCost}");
        }
    }

    private static void InspectShieldDelay(
        int baseItem,
        JsonElement propertiesList,
        string findingPath,
        ICollection<string> findings)
    {
        if (!ShieldBaseItems.Contains(baseItem))
            return;

        var delayCosts = GetDelayCostValues(propertiesList).ToList();
        if (delayCosts.Count > 0)
        {
            findings.Add($"{findingPath} shield Delay [{string.Join(", ", delayCosts)}] should be removed");
        }
    }

    private static IEnumerable<int> GetDelayCostValues(JsonElement propertiesList)
    {
        if (propertiesList.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var property in propertiesList.EnumerateArray())
        {
            if (TryGetWrappedInt(property, "PropertyName", out var propertyName) &&
                propertyName == 98 &&
                TryGetWrappedInt(property, "CostTable", out var costTable) &&
                costTable == 52 &&
                TryGetWrappedInt(property, "CostValue", out var costValue))
            {
                yield return costValue;
            }
        }
    }

    private static bool TryGetWrappedValue(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var wrapper) ||
            wrapper.ValueKind != JsonValueKind.Object ||
            !wrapper.TryGetProperty("value", out value))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetWrappedInt(JsonElement element, string propertyName, out int value)
    {
        value = 0;
        return TryGetWrappedValue(element, propertyName, out var wrapperValue) &&
               wrapperValue.ValueKind == JsonValueKind.Number &&
               wrapperValue.TryGetInt32(out value);
    }
}
