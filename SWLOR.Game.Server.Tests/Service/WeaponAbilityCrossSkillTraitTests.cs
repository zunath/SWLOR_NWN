using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

public class WeaponAbilityCrossSkillTraitTests
{
    private static readonly DateTime Now = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
    private static PairedRangedTraitConfiguration Config => new()
    {
        MomentumMaximum = 3, MomentumDurationSeconds = 12, MomentumDamagePerCharge = 4,
        MomentumStaminaRestorePerCharge = 1, MomentumStaminaRestoreMaximum = 3, MomentumRestoreCooldownSeconds = 12,
        MovementNextAbilityAccuracy = 5, MovementAccuracyDurationSeconds = 8, MovementAccuracyCooldownSeconds = 10,
        DirectDamageNextAbilityStaminaReduction = 2, DirectDamageCostWindowSeconds = 10, DirectDamageCostCooldownSeconds = 10
    };

    [TestCase(SkillType.Rifle)]
    [TestCase(SkillType.Vibroblade)]
    [TestCase(SkillType.HeavyVibroblade)]
    [TestCase(SkillType.Saberstaff)]
    [TestCase(SkillType.TwinPistol)]
    public void AnyWeaponSkillRequiresAnActualPositivePayment(SkillType skill)
    {
        WeaponAbilityTraitPolicy.IsEligibleSpender(true, skill, 1).Should().BeTrue();
        WeaponAbilityTraitPolicy.IsEligibleSpender(true, skill, 0).Should().BeFalse();
        WeaponAbilityTraitPolicy.IsEligibleSpender(false, skill, 4).Should().BeFalse();
    }

    [Test]
    public void SupportCostsQualifyButCraftingAndFreeActionsDoNot()
    {
        WeaponAbilityTraitPolicy.IsEligibleCostReduction(SkillType.FirstAid, 1).Should().BeTrue();
        WeaponAbilityTraitPolicy.IsEligibleCostReduction(SkillType.Leadership, 2).Should().BeTrue();
        WeaponAbilityTraitPolicy.IsEligibleCostReduction(SkillType.Force, 2).Should().BeTrue();
        WeaponAbilityTraitPolicy.IsEligibleCostReduction(SkillType.Smithery, 10).Should().BeFalse();
        WeaponAbilityTraitPolicy.IsEligibleCostReduction(SkillType.FirstAid, 0).Should().BeFalse();
        WeaponAbilityTraitPolicy.IsEligibleSpender(true, SkillType.Force, 10).Should().BeFalse();
    }

    [Test]
    public void ZeroPaymentPreservesFlowButConsumesCapturedAccuracy()
    {
        var state = new PairedRangedTraitState();
        state.RecordMovementHit(Now, Config);
        var activation = new WeaponAbilityTraitActivation(state.CaptureAbility(Now, Config, true, true), 4);
        activation.SetPaidStamina(0);
        state.CommitAbility(Now, Config, activation.Snapshot).Should().BeTrue();
        state.Momentum.Should().Be(1);
        state.MovementAccuracyExpires.Should().Be(default);
        activation.Snapshot.MomentumSpent.Should().Be(0);
        activation.Snapshot.AccuracyAdjustment.Should().Be(5);
    }

    [Test]
    public void CapturedChargesDoNotSpendRewardsEarnedAfterTheCastStarted()
    {
        var state = new PairedRangedTraitState();
        state.RecordMovementHit(Now, Config);
        var captured = state.CaptureAbility(Now, Config, true, true);
        state.RecordMovementHit(Now.AddSeconds(1), Config);
        state.CommitAbility(Now.AddSeconds(1), Config, captured).Should().BeTrue();
        state.Momentum.Should().Be(1);
        state.CommitAbility(Now.AddSeconds(1), Config, captured).Should().BeFalse();
        state.CompleteAbility(Now.AddSeconds(1), Config, captured, true, 0, false, 6, 0).Should().Be(1);
        state.CompleteAbility(Now.AddSeconds(1), Config, captured, true, 0, false, 6, 0).Should().Be(0);
    }

    [Test]
    public void EmptySourceSnapshotCannotConsumeItsOwnMovementReward()
    {
        var state = new PairedRangedTraitState();
        var source = state.CaptureAbility(Now, Config, true, true);
        state.CommitAbility(Now, Config, source).Should().BeTrue();
        state.RecordMovementHit(Now, Config);
        source.MomentumSpent.Should().Be(0);
        state.Momentum.Should().Be(1);
    }

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(12)]
    public void RepeatedTargetsTicksAndWavesShareOneBudgetPerWeapon(int budget)
    {
        var activation = new WeaponAbilityTraitActivation(new(Guid.NewGuid(), true, true, 1, 5), budget)
            { IsCommitted = true };
        var totals = new Dictionary<uint, int>();
        foreach (var weapon in new uint[] { 40, 41 })
            for (var wave = 0; wave < 3; wave++)
                for (var target = 0; target < 100; target++)
                    totals[weapon] = totals.GetValueOrDefault(weapon) +
                        WeaponImpactDamagePartition.Partition(activation.TryTakeDamageBonus(weapon, wave, 3), wave, 3);
        totals[40].Should().Be(budget);
        totals[41].Should().Be(budget);
        activation.TryTakeDamageBonus(40).Should().Be(0, "a different profile cannot restart a used weapon budget");
    }

    [Test]
    public void SingleWeaponProfileDoesNotInventAnOffhandBudgetOrRepeatOnLaterHits()
    {
        var activation = new WeaponAbilityTraitActivation(new(Guid.NewGuid(), true, true, 3, 5), 4)
            { IsCommitted = true, PrimaryWeapon = 40 };
        activation.TryTakeDamageBonus(activation.PrimaryWeapon).Should().Be(12);
        activation.TryTakeDamageBonus(activation.PrimaryWeapon).Should().Be(0);
    }

    [Test]
    public void MovementCountsActualCompletedTravelAndConsumesOneOpportunity()
    {
        var window = new WeaponMovementWindow();
        window.Observe(Now, 1, Vector3.Zero);
        window.TryConsumeForHit(1, Vector3.Zero).Should().BeFalse("a projected destination is not movement");
        window.Observe(Now, 1, new Vector3(1, 0, 0));
        window.Observe(Now, 1, new Vector3(1, 1.1f, 0));
        window.TryConsumeForHit(1, new Vector3(1, 1.1f, 0)).Should().BeTrue();
        window.TryConsumeForHit(1, new Vector3(1, 1.1f, 0)).Should().BeFalse();
        window.Observe(Now, 1, new Vector3(8, 1.1f, 0), walking: false);
        window.TryConsumeForHit(1, new Vector3(8, 1.1f, 0)).Should().BeTrue("another skill's same-area reposition qualifies");
        window.Observe(Now, 2, new Vector3(80, 80, 0));
        window.TryConsumeForHit(2, new Vector3(80, 80, 0)).Should().BeFalse("cross-area travel resets the window");
    }

    [Test]
    public void DuplicateOrdinaryRollCannotConsumeLaterMovement()
    {
        var window = new WeaponMovementWindow();
        window.Observe(Now, 1, Vector3.Zero);
        window.TryRememberOrdinaryHit(100).Should().BeTrue();
        window.Observe(Now, 1, new Vector3(3, 0, 0));
        window.TryRememberOrdinaryHit(100).Should().BeFalse();
        window.DistanceTravelled.Should().Be(3);
        window.TryRememberOrdinaryHit(101).Should().BeTrue();
        window.TryConsumeForHit(1, new Vector3(3, 0, 0)).Should().BeTrue();
    }
}
