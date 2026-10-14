using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Service;

public class PairedRangedTraitStateTests
{
    private static readonly DateTime Start = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    private static readonly PairedRangedTraitConfiguration Configuration = new()
    {
        OrdinaryDamagePercentAdjustment = 25,
        CompanionAccuracy = 5,
        OrdinaryHitRestoreCount = 3,
        OrdinaryHitStaminaRestore = 1,
        OrdinaryHitRestoreCooldownSeconds = 6,
        RepeatedTargetRequiredHits = 3,
        RepeatedTargetDamagePercent = 5,
        RepeatedTargetDurationSeconds = 6,
        MomentumMaximum = 3,
        MomentumDurationSeconds = 12,
        MomentumAccuracyPerCharge = 2,
        MomentumStaminaRestorePerCharge = 1,
        MomentumStaminaRestoreMaximum = 3,
        MomentumRestoreCooldownSeconds = 12,
        EvasionMomentumCooldownSeconds = 10,
        MovementNextAbilityAccuracy = 5,
        MovementAccuracyDurationSeconds = 8,
        MovementAccuracyCooldownSeconds = 10,
        DirectDamageNextAbilityStaminaReduction = 2,
        DirectDamageCostWindowSeconds = 10,
        DirectDamageCostCooldownSeconds = 10
    };

    [Test]
    public void OrdinarySequence_DeduplicatesHits_AndDoesNotBankResourceRecoveryDuringCooldown()
    {
        var state = new PairedRangedTraitState();
        Hit(state, 0, 1).Should().Be(0);
        Hit(state, 1, 2).Should().Be(0);
        Hit(state, 2, 3).Should().Be(1);
        state.RecordOrdinaryHit(Start.AddSeconds(2), Configuration, 10, 3).Should().Be(0);
        Hit(state, 3, 4).Should().Be(0);
        Hit(state, 4, 5).Should().Be(0);
        Hit(state, 5, 6).Should().Be(0);
        Hit(state, 8, 7).Should().Be(0, "cooldown hits cannot prefill a refund");
        Hit(state, 9, 8).Should().Be(0);
        Hit(state, 10, 9).Should().Be(1);
    }

    [Test]
    public void RepeatedTargetDamage_StartsAfterThreeHits_AndDoesNotFollowAnEnemySwitch()
    {
        var state = new PairedRangedTraitState();
        Hit(state, 0, 1);
        Hit(state, 1, 2);
        var third = state.RecordOrdinaryAttempt(Start.AddSeconds(2), Configuration, 10, 3, false);
        third.DamagePercentAdjustment.Should().Be(25);
        state.RecordOrdinaryHit(Start.AddSeconds(2), Configuration, 10, 3);
        state.GetOrdinarySnapshot(Start.AddSeconds(3), Configuration, 10, false).DamagePercentAdjustment.Should().Be(30);
        state.RecordOrdinaryAttempt(Start.AddSeconds(3), Configuration, 20, 4, false)
            .DamagePercentAdjustment.Should().Be(25);
        state.GetOrdinarySnapshot(Start.AddSeconds(3), Configuration, 10, false).DamagePercentAdjustment.Should().Be(25);
    }

    [Test]
    public void Builder_GrantsOneChargePerSuccessfulMovingActivation_WithThreeChargeCap()
    {
        var state = new PairedRangedTraitState();
        for (var i = 0; i < 5; i++)
        {
            var snapshot = state.BeginAbility(Start.AddSeconds(i), Configuration, true, false);
            state.CompleteAbility(Start.AddSeconds(i), Configuration, snapshot, true, 4, true, 8, 0);
            state.CompleteAbility(Start.AddSeconds(i), Configuration, snapshot, true, 4, true, 8, 0);
        }
        state.Momentum.Should().Be(3);
        state.GetOrdinarySnapshot(Start.AddSeconds(5), Configuration, 10, false).AccuracyAdjustment.Should().Be(6);
        state.GetOrdinarySnapshot(Start.AddSeconds(5), Configuration, 10, true).AccuracyAdjustment.Should().Be(11);
        var spender = state.BeginAbility(Start.AddSeconds(5), Configuration, true, true);
        spender.MomentumSpent.Should().Be(3);
        state.Momentum.Should().Be(0);
        state.CompleteAbility(Start.AddSeconds(5), Configuration, spender, false, 4, false, 8, 0).Should().Be(0);
        state.Momentum.Should().Be(0, "a miss cannot refund spent charges");
    }

    [TestCase(false, 4f)]
    [TestCase(true, 0f)]
    [TestCase(true, 1.99f)]
    public void EmptyOrBlockedBuilder_GeneratesNothing(bool anyHit, float distance)
    {
        var state = new PairedRangedTraitState();
        var snapshot = state.BeginAbility(Start, Configuration, true, false);
        state.CompleteAbility(Start, Configuration, snapshot, anyHit, distance, true, 8, 0);
        state.Momentum.Should().Be(0);
        state.MovementAccuracyExpires.Should().Be(default);
    }

    [Test]
    public void EvasionCharge_PreservesItsTenSecondGateAcrossEquipmentAndCombatResets()
    {
        var state = new PairedRangedTraitState();
        state.RecordAccuracyEvasionMiss(Start, Configuration);
        state.Momentum.Should().Be(1);
        state.ClearTransientState();
        state.RecordAccuracyEvasionMiss(Start.AddSeconds(1), Configuration);
        state.Momentum.Should().Be(0);
        state.RecordAccuracyEvasionMiss(Start.AddSeconds(10), Configuration);
        state.Momentum.Should().Be(1);
        state.RecordAccuracyEvasionMiss(Start.AddSeconds(20), Configuration with { MomentumMaximum = 0 });
        state.Momentum.Should().Be(0, "the evasion trait cannot create Momentum without its charge capacity");
    }

    [Test]
    public void Stance_ExtendsOnlyNewChargeGains_AndLeavingClampsRemainingDuration()
    {
        var state = new PairedRangedTraitState();
        state.RecordAccuracyEvasionMiss(Start, Configuration);
        var stance = Configuration with { MomentumExtendedDurationSeconds = 18 };
        state.Reconcile(Start.AddSeconds(1), stance);
        state.MomentumExpires.Should().Be(Start.AddSeconds(12), "entering stance alone does not refresh charges");
        state.RecordAccuracyEvasionMiss(Start.AddSeconds(10), stance);
        state.MomentumExpires.Should().Be(Start.AddSeconds(28));
        state.Reconcile(Start.AddSeconds(11), Configuration);
        state.MomentumExpires.Should().Be(Start.AddSeconds(23));
        state.Reconcile(Start.AddSeconds(12), stance);
        state.MomentumExpires.Should().Be(Start.AddSeconds(23), "toggling back cannot extend existing charges");
    }

    [Test]
    public void SpenderRefund_IsOncePerCast_CappedByPaidStamina_AndDoesNotRestoreMomentum()
    {
        var state = new PairedRangedTraitState();
        Gain(state, 0);
        Gain(state, 1);
        Gain(state, 2);
        var spender = state.BeginAbility(Start.AddSeconds(3), Configuration, true, true);
        state.CompleteAbility(Start.AddSeconds(3), Configuration, spender, true, 0, false, 2, 0).Should().Be(2);
        state.CompleteAbility(Start.AddSeconds(3), Configuration, spender, true, 0, false, 8, 0).Should().Be(0);
        Gain(state, 4);
        var second = state.BeginAbility(Start.AddSeconds(5), Configuration, true, true);
        state.CompleteAbility(Start.AddSeconds(5), Configuration, second, true, 0, false, 8, 0).Should().Be(0);
        state.Momentum.Should().Be(0);
    }

    [Test]
    public void ChangingAngles_CannotBenefitItsSource_AndDoesNotRefreshInsideItsGrantCooldown()
    {
        var state = new PairedRangedTraitState();
        var source = state.BeginAbility(Start, Configuration, true, false);
        source.AccuracyAdjustment.Should().Be(0);
        state.CompleteAbility(Start, Configuration, source, true, 4, true, 8, 0);
        var next = state.BeginAbility(Start.AddSeconds(1), Configuration, true, true);
        next.AccuracyAdjustment.Should().Be(5);
        state.CompleteAbility(Start.AddSeconds(1), Configuration, next, true, 4, false, 8, 0);
        state.BeginAbility(Start.AddSeconds(2), Configuration, true, true).AccuracyAdjustment.Should().Be(0);
    }

    [Test]
    public void CostDiscount_IsOneCharge_Expires_AndRetainsGrantGateWhenPairIsRemoved()
    {
        var state = new PairedRangedTraitState();
        state.RecordDirectHostileDamage(Start, Configuration);
        state.GetAbilityStaminaCostAdjustment(Start.AddSeconds(1), Configuration).Should().Be(-2);
        state.ConsumeAbilityStaminaDiscount(Start.AddSeconds(1), Configuration).Should().Be(-2);
        state.ConsumeAbilityStaminaDiscount(Start.AddSeconds(1), Configuration).Should().Be(0);
        state.ClearTransientState();
        state.RecordDirectHostileDamage(Start.AddSeconds(2), Configuration);
        state.GetAbilityStaminaCostAdjustment(Start.AddSeconds(2), Configuration).Should().Be(0);
        state.RecordDirectHostileDamage(Start.AddSeconds(10), Configuration);
        state.GetAbilityStaminaCostAdjustment(Start.AddSeconds(20), Configuration).Should().Be(0);
    }

    [Test]
    public void TwinSuns_SixActualAttemptsConsumeCharges_IncludingMisses_LastAttemptKeepsItsBonus()
    {
        var state = new PairedRangedTraitState();
        var cast = state.BeginAbility(Start, Configuration, false, false);
        state.CompleteAbility(Start, Configuration, cast, false, 0, false, 15, 0, 6, 25, 12);
        for (var i = 1; i <= 6; i++)
        {
            var shot = state.RecordOrdinaryAttempt(Start.AddSeconds(i), Configuration, 10, i, false);
            shot.DamagePercentAdjustment.Should().Be(50);
            var attempts = state.BonusAttempts;
            state.RecordOrdinaryAttempt(Start.AddSeconds(i), Configuration, 10, i, false);
            state.BonusAttempts.Should().Be(attempts, "duplicate observer callbacks cannot spend another charge");
        }
        state.GetOrdinarySnapshot(Start.AddSeconds(7), Configuration, 10, false).DamagePercentAdjustment.Should().Be(25);
    }

    [Test]
    public void CapturedSpender_FreezesPowerThroughExpiry_AndPreservesChargesEarnedDuringTheCast()
    {
        var state = new PairedRangedTraitState();
        Gain(state, 0);
        Gain(state, 1);
        var captured = state.CaptureAbility(Start.AddSeconds(12.5), Configuration, true, true);
        captured.MomentumSpent.Should().Be(2);
        state.Momentum.Should().Be(2, "capture alone cannot spend resources before successful payment");
        Gain(state, 13.5); // The original charges expire at 13 seconds; this is a new identity.
        state.Momentum.Should().Be(1);
        state.CommitAbility(Start.AddSeconds(14.5), Configuration, captured).Should().BeTrue();
        captured.MomentumSpent.Should().Be(2, "the impact uses the payload captured at cast start");
        state.Momentum.Should().Be(1, "a charge earned after capture cannot replace an expired captured charge");
        state.CommitAbility(Start.AddSeconds(14.5), Configuration, captured).Should().BeFalse();
        state.Momentum.Should().Be(1, "duplicate commit cannot spend the later charge");
    }

    [Test]
    public void CapturedSpender_ConsumesOnlyExistingCapturedChargeIdentities()
    {
        var state = new PairedRangedTraitState();
        Gain(state, 0);
        Gain(state, 1);
        var captured = state.CaptureAbility(Start.AddSeconds(2), Configuration, true, true);
        Gain(state, 3);
        state.Momentum.Should().Be(3);
        state.CommitAbility(Start.AddSeconds(4), Configuration, captured).Should().BeTrue();
        state.Momentum.Should().Be(1);
        captured.MomentumSpent.Should().Be(2);
    }

    [Test]
    public void CapturedChangingAngles_SurvivesExpiry_AndNeverConsumesANewerAccuracyCharge()
    {
        var state = new PairedRangedTraitState();
        var configuration = Configuration with { MovementAccuracyDurationSeconds = 2, MovementAccuracyCooldownSeconds = 1 };
        var source = state.BeginAbility(Start, configuration, true, false);
        state.CompleteAbility(Start, configuration, source, true, 4, true, 8, 0);
        var captured = state.CaptureAbility(Start.AddSeconds(1.5), configuration, true, false);
        captured.AccuracyAdjustment.Should().Be(5);
        var newerSource = state.BeginAbility(Start.AddSeconds(2.5), configuration, true, false);
        state.CompleteAbility(Start.AddSeconds(2.5), configuration, newerSource, true, 4, true, 8, 0);
        state.CommitAbility(Start.AddSeconds(3.5), configuration, captured).Should().BeTrue();
        captured.AccuracyAdjustment.Should().Be(5, "expiry while casting cannot alter a frozen accuracy payload");
        state.CaptureAbility(Start.AddSeconds(3.5), configuration, true, false).AccuracyAdjustment.Should().Be(5,
            "committing an older activation cannot consume a newer movement reward");
    }

    [Test]
    public void InterruptedOrInvalidatedCapture_ConsumesNothing_AndCannotCompleteLater()
    {
        var state = new PairedRangedTraitState();
        Gain(state, 0);
        var interrupted = state.CaptureAbility(Start.AddSeconds(1), Configuration, true, true);
        state.Momentum.Should().Be(1);
        state.CaptureAbility(Start.AddSeconds(2), Configuration, true, false).AccuracyAdjustment.Should().Be(5,
            "an uncommitted cast leaves its accuracy charge intact");
        state.CompleteAbility(Start.AddSeconds(2), Configuration, interrupted, true, 0, false, 8, 0).Should().Be(0,
            "an unpaid activation cannot grant resources or effects");
        state.ClearTransientState();
        Gain(state, 3);
        state.CommitAbility(Start.AddSeconds(4), Configuration, interrupted).Should().BeFalse();
        state.Momentum.Should().Be(1, "combat/death resets invalidate pending snapshots without consuming later charges");
    }

    private static int Hit(PairedRangedTraitState state, double seconds, long rollId, uint target = 10)
    {
        var now = Start.AddSeconds(seconds);
        state.RecordOrdinaryAttempt(now, Configuration, target, rollId, false);
        return state.RecordOrdinaryHit(now, Configuration, target, rollId);
    }

    private static void Gain(PairedRangedTraitState state, double seconds)
    {
        var now = Start.AddSeconds(seconds);
        var cast = state.BeginAbility(now, Configuration, true, false);
        state.CompleteAbility(now, Configuration, cast, true, 4, true, 8, 0);
    }
}
