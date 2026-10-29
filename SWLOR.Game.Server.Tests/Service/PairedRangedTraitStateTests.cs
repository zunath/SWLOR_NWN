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
        OrdinaryHitRestoreCount = 3,
        OrdinaryHitStaminaRestore = 1,
        OrdinaryHitRestoreCooldownSeconds = 6,
        RepeatedTargetRequiredHits = 3,
        RepeatedTargetDamagePercent = 5,
        RepeatedTargetDurationSeconds = 6,
        CadenceMaximum = 3,
        CadenceDurationSeconds = 12,
        CadenceChainWindowSeconds = 6,
        CadenceAccuracyPerCharge = 2,
        CadenceDamagePerCharge = 2,
        CadenceStaminaRestorePerCharge = 1,
        CadenceStaminaRestoreMaximum = 3,
        CadenceRestoreCooldownSeconds = 12,
        EvasionCadenceCooldownSeconds = 10,
        CadenceNextAbilityAccuracy = 5,
        CadenceAccuracyDurationSeconds = 8,
        CadenceAccuracyCooldownSeconds = 10,
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
    public void OrdinaryHitRestore_CountsALandedHitOnAnEarlierTarget_WithoutExtendingItsStreak()
    {
        var state = new PairedRangedTraitState();
        Hit(state, 0, 1);
        state.RecordOrdinaryAttempt(Start.AddSeconds(1), Configuration, 10, 2);
        state.RecordOrdinaryAttempt(Start.AddSeconds(1), Configuration, 20, 3);
        state.RecordOrdinaryHit(Start.AddSeconds(1), Configuration, 10, 2).Should().Be(0);
        state.RecordOrdinaryHit(Start.AddSeconds(1), Configuration, 20, 3).Should().Be(1,
            "every landed ordinary hit counts toward the restore regardless of which enemy it struck");
        state.HasRepeatedTargetBonus(Start.AddSeconds(1), Configuration).Should().BeFalse();
    }

    [Test]
    public void RepeatedTargetDamage_StartsAfterThreeHits_AndDoesNotFollowAnEnemySwitch()
    {
        var state = new PairedRangedTraitState();
        Hit(state, 0, 1);
        Hit(state, 1, 2);
        var third = state.RecordOrdinaryAttempt(Start.AddSeconds(2), Configuration, 10, 3);
        third.DamagePercentAdjustment.Should().Be(25);
        state.RecordOrdinaryHit(Start.AddSeconds(2), Configuration, 10, 3);
        state.GetOrdinarySnapshot(Start.AddSeconds(3), Configuration, 10).DamagePercentAdjustment.Should().Be(30);
        state.RecordOrdinaryAttempt(Start.AddSeconds(3), Configuration, 20, 4)
            .DamagePercentAdjustment.Should().Be(25);
        state.GetOrdinarySnapshot(Start.AddSeconds(3), Configuration, 10).DamagePercentAdjustment.Should().Be(25);
    }

    private const int LeadShot = 1;
    private const int ClosingShot = 2;
    private const int TempoShot = 3;

    [Test]
    public void Chain_ADifferentAbilityWithinTheWindowContinuesIt_IncludingTheWindowEdge()
    {
        var state = new PairedRangedTraitState();
        state.RecordAbilityUse(Start, Configuration, TempoShot).Should().BeFalse("the first ability has nothing to chain from");
        state.RecordAbilityUse(Start.AddSeconds(3), Configuration, LeadShot).Should().BeTrue();
        state.RecordAbilityUse(Start.AddSeconds(9), Configuration, ClosingShot).Should().BeTrue("the six-second window is inclusive");
    }

    [Test]
    public void Chain_TheSameAbilityOrAnExpiredWindowDoesNotContinueIt()
    {
        var state = new PairedRangedTraitState();
        state.RecordAbilityUse(Start, Configuration, LeadShot);
        state.RecordAbilityUse(Start.AddSeconds(1), Configuration, LeadShot).Should().BeFalse("repeating one ability is not a chain");
        state.RecordAbilityUse(Start.AddSeconds(7.01), Configuration, ClosingShot).Should().BeFalse("the window counts from the latest use");
        state.RecordAbilityUse(Start.AddSeconds(8), Configuration, LeadShot).Should().BeTrue(
            "a different ability after an expired window still chains from the latest one");
    }

    [Test]
    public void Chain_ResetsWithTransientState_AndNeedsTheChainWindowTrait()
    {
        var state = new PairedRangedTraitState();
        state.RecordAbilityUse(Start, Configuration, TempoShot);
        state.ClearTransientState();
        state.RecordAbilityUse(Start.AddSeconds(1), Configuration, LeadShot).Should().BeFalse();

        var untrained = new PairedRangedTraitState();
        var noWindow = Configuration with { CadenceChainWindowSeconds = 0 };
        untrained.RecordAbilityUse(Start, noWindow, TempoShot);
        untrained.RecordAbilityUse(Start.AddSeconds(1), noWindow, LeadShot).Should().BeFalse();
    }

    [Test]
    public void AbilityHit_GrantsOncePerActivation_HoweverManyHandsTargetsOrWavesLand()
    {
        var state = new PairedRangedTraitState();
        var activation = Guid.NewGuid();
        for (var impact = 0; impact < 6; impact++)
            state.RecordAbilityHit(Start, Configuration, activation, 1);
        state.Cadence.Should().Be(1);
        state.RecordAbilityHit(Start, Configuration, Guid.NewGuid(), 0);
        state.Cadence.Should().Be(1, "an activation that earns nothing grants nothing");
    }

    [Test]
    public void AbilityHit_CapsAtThree_RefreshesDuration_AndGrantsAccuracyPerCharge()
    {
        var state = new PairedRangedTraitState();
        state.RecordAbilityHit(Start, Configuration, Guid.NewGuid(), 2);
        state.CadenceExpires.Should().Be(Start.AddSeconds(12));
        state.RecordAbilityHit(Start.AddSeconds(5), Configuration, Guid.NewGuid(), 2);
        state.Cadence.Should().Be(3);
        state.CadenceExpires.Should().Be(Start.AddSeconds(17), "every gain refreshes all charges");
        state.GetOrdinarySnapshot(Start.AddSeconds(5), Configuration, 10).AccuracyAdjustment.Should().Be(6);
        state.Reconcile(Start.AddSeconds(17), Configuration);
        state.Cadence.Should().Be(0);
    }

    [Test]
    public void OrdinaryHitsAndAbilityCompletion_NeverGrantCadence()
    {
        var state = new PairedRangedTraitState();
        Hit(state, 0, 1);
        Hit(state, 1, 2);
        var cast = state.BeginAbility(Start.AddSeconds(2), Configuration, true, false);
        state.CompleteAbility(Start.AddSeconds(2), Configuration, cast, true, 8, 0);
        state.Cadence.Should().Be(0);
        state.CadenceAccuracyExpires.Should().Be(default);
    }

    [Test]
    public void Spender_SpendsEveryCharge_AndAMissCannotRefundThem()
    {
        var state = new PairedRangedTraitState();
        state.RecordAbilityHit(Start, Configuration, Guid.NewGuid(), 3);
        var spender = state.BeginAbility(Start.AddSeconds(1), Configuration, false, true);
        spender.CadenceSpent.Should().Be(3);
        state.Cadence.Should().Be(0);
        state.CompleteAbility(Start.AddSeconds(1), Configuration, spender, false, 8, 0).Should().Be(0);
        state.Cadence.Should().Be(0, "a miss cannot refund spent charges");
    }

    [Test]
    public void EvasionCharge_PreservesItsTenSecondGateAcrossEquipmentAndCombatResets()
    {
        var state = new PairedRangedTraitState();
        state.RecordAccuracyEvasionMiss(Start, Configuration);
        state.Cadence.Should().Be(1);
        state.ClearTransientState();
        state.RecordAccuracyEvasionMiss(Start.AddSeconds(1), Configuration);
        state.Cadence.Should().Be(0);
        state.RecordAccuracyEvasionMiss(Start.AddSeconds(10), Configuration);
        state.Cadence.Should().Be(1);
        state.RecordAccuracyEvasionMiss(Start.AddSeconds(20), Configuration with { CadenceMaximum = 0 });
        state.Cadence.Should().Be(0, "the evasion trait cannot create Cadence without its charge capacity");
    }

    [Test]
    public void Stance_ExtendsOnlyNewChargeGains_AndLeavingClampsRemainingDuration()
    {
        var state = new PairedRangedTraitState();
        state.RecordAccuracyEvasionMiss(Start, Configuration);
        var stance = Configuration with { CadenceExtendedDurationSeconds = 18 };
        state.Reconcile(Start.AddSeconds(1), stance);
        state.CadenceExpires.Should().Be(Start.AddSeconds(12), "entering stance alone does not refresh charges");
        state.RecordAccuracyEvasionMiss(Start.AddSeconds(10), stance);
        state.CadenceExpires.Should().Be(Start.AddSeconds(28));
        state.Reconcile(Start.AddSeconds(11), Configuration);
        state.CadenceExpires.Should().Be(Start.AddSeconds(23));
        state.Reconcile(Start.AddSeconds(12), stance);
        state.CadenceExpires.Should().Be(Start.AddSeconds(23), "toggling back cannot extend existing charges");
    }

    [Test]
    public void SpenderRefund_IsOncePerCast_CappedByPaidStamina_AndDoesNotRestoreCadence()
    {
        var state = new PairedRangedTraitState();
        Gain(state, 0);
        Gain(state, 1);
        Gain(state, 2);
        var spender = state.BeginAbility(Start.AddSeconds(3), Configuration, true, true);
        state.CompleteAbility(Start.AddSeconds(3), Configuration, spender, true, 2, 0).Should().Be(2);
        state.CompleteAbility(Start.AddSeconds(3), Configuration, spender, true, 8, 0).Should().Be(0);
        Gain(state, 4);
        var second = state.BeginAbility(Start.AddSeconds(5), Configuration, true, true);
        state.CompleteAbility(Start.AddSeconds(5), Configuration, second, true, 8, 0).Should().Be(0);
        state.Cadence.Should().Be(0);
    }

    [Test]
    public void ChangingAngles_IsGrantedByAnyCadenceGain_OnceUntilItsCooldown()
    {
        var state = new PairedRangedTraitState();
        state.BeginAbility(Start, Configuration, true, false).AccuracyAdjustment.Should().Be(0);
        state.RecordAbilityHit(Start, Configuration, Guid.NewGuid(), 1);
        state.CadenceAccuracyExpires.Should().Be(Start.AddSeconds(8), "an ability-hit gain arms Changing Angles");
        state.BeginAbility(Start.AddSeconds(1), Configuration, true, true).AccuracyAdjustment.Should().Be(5);
        state.CadenceAccuracyExpires.Should().Be(default, "the next paid weapon ability consumes it");
        state.RecordAbilityHit(Start.AddSeconds(2), Configuration, Guid.NewGuid(), 1);
        state.BeginAbility(Start.AddSeconds(3), Configuration, true, true).AccuracyAdjustment.Should().Be(0,
            "a gain inside the ten-second cooldown does not re-arm it");
        state.RecordAccuracyEvasionMiss(Start.AddSeconds(10), Configuration);
        state.BeginAbility(Start.AddSeconds(10.5), Configuration, true, true).AccuracyAdjustment.Should().Be(5,
            "an evasion gain after the cooldown also arms it");
    }

    [Test]
    public void ChangingAngles_NeedsItsTrait()
    {
        var state = new PairedRangedTraitState();
        state.RecordAbilityHit(Start, Configuration with { CadenceNextAbilityAccuracy = 0 }, Guid.NewGuid(), 1);
        state.Cadence.Should().Be(1);
        state.CadenceAccuracyExpires.Should().Be(default);
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
        state.CompleteAbility(Start, Configuration, cast, false, 15, 0, 6, 25, 12);
        for (var i = 1; i <= 6; i++)
        {
            var shot = state.RecordOrdinaryAttempt(Start.AddSeconds(i), Configuration, 10, i);
            shot.DamagePercentAdjustment.Should().Be(50);
            var attempts = state.BonusAttempts;
            state.RecordOrdinaryAttempt(Start.AddSeconds(i), Configuration, 10, i);
            state.BonusAttempts.Should().Be(attempts, "duplicate observer callbacks cannot spend another charge");
        }
        state.GetOrdinarySnapshot(Start.AddSeconds(7), Configuration, 10).DamagePercentAdjustment.Should().Be(25);
    }

    [Test]
    public void CapturedSpender_FreezesPowerThroughExpiry_AndPreservesChargesEarnedDuringTheCast()
    {
        var state = new PairedRangedTraitState();
        Gain(state, 0);
        Gain(state, 1);
        var captured = state.CaptureAbility(Start.AddSeconds(12.5), Configuration, true, true);
        captured.CadenceSpent.Should().Be(2);
        state.Cadence.Should().Be(2, "capture alone cannot spend resources before successful payment");
        Gain(state, 13.5); // The original charges expire at 13 seconds; this is a new identity.
        state.Cadence.Should().Be(1);
        state.CommitAbility(Start.AddSeconds(14.5), Configuration, captured).Should().BeTrue();
        captured.CadenceSpent.Should().Be(2, "the impact uses the payload captured at cast start");
        state.Cadence.Should().Be(1, "a charge earned after capture cannot replace an expired captured charge");
        state.CommitAbility(Start.AddSeconds(14.5), Configuration, captured).Should().BeFalse();
        state.Cadence.Should().Be(1, "duplicate commit cannot spend the later charge");
    }

    [Test]
    public void CapturedSpender_ConsumesOnlyExistingCapturedChargeIdentities()
    {
        var state = new PairedRangedTraitState();
        Gain(state, 0);
        Gain(state, 1);
        var captured = state.CaptureAbility(Start.AddSeconds(2), Configuration, true, true);
        Gain(state, 3);
        state.Cadence.Should().Be(3);
        state.CommitAbility(Start.AddSeconds(4), Configuration, captured).Should().BeTrue();
        state.Cadence.Should().Be(1);
        captured.CadenceSpent.Should().Be(2);
    }

    [Test]
    public void CapturedChangingAngles_SurvivesExpiry_AndNeverConsumesANewerAccuracyCharge()
    {
        var state = new PairedRangedTraitState();
        var configuration = Configuration with { CadenceAccuracyDurationSeconds = 2, CadenceAccuracyCooldownSeconds = 1 };
        state.RecordAbilityHit(Start, configuration, Guid.NewGuid(), 1);
        var captured = state.CaptureAbility(Start.AddSeconds(1.5), configuration, true, false);
        captured.AccuracyAdjustment.Should().Be(5);
        state.RecordAbilityHit(Start.AddSeconds(2.5), configuration, Guid.NewGuid(), 1);
        state.CommitAbility(Start.AddSeconds(3.5), configuration, captured).Should().BeTrue();
        captured.AccuracyAdjustment.Should().Be(5, "expiry while casting cannot alter a frozen accuracy payload");
        state.CaptureAbility(Start.AddSeconds(3.5), configuration, true, false).AccuracyAdjustment.Should().Be(5,
            "committing an older activation cannot consume a newer gain's accuracy");
    }

    [Test]
    public void InterruptedOrInvalidatedCapture_ConsumesNothing_AndCannotCompleteLater()
    {
        var state = new PairedRangedTraitState();
        Gain(state, 0);
        var interrupted = state.CaptureAbility(Start.AddSeconds(1), Configuration, true, true);
        state.Cadence.Should().Be(1);
        state.CaptureAbility(Start.AddSeconds(2), Configuration, true, false).AccuracyAdjustment.Should().Be(5,
            "an uncommitted cast leaves its accuracy charge intact");
        state.CompleteAbility(Start.AddSeconds(2), Configuration, interrupted, true, 8, 0).Should().Be(0,
            "an unpaid activation cannot grant resources or effects");
        state.ClearTransientState();
        Gain(state, 3);
        state.CommitAbility(Start.AddSeconds(4), Configuration, interrupted).Should().BeFalse();
        state.Cadence.Should().Be(1, "combat/death resets invalidate pending snapshots without consuming later charges");
    }

    private static int Hit(PairedRangedTraitState state, double seconds, long rollId, uint target = 10)
    {
        var now = Start.AddSeconds(seconds);
        state.RecordOrdinaryAttempt(now, Configuration, target, rollId);
        return state.RecordOrdinaryHit(now, Configuration, target, rollId);
    }

    private static void Gain(PairedRangedTraitState state, double seconds)
    {
        state.RecordAbilityHit(Start.AddSeconds(seconds), Configuration, Guid.NewGuid(), 1);
    }
}
