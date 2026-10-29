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
        CadenceMaximum = 3, CadenceDurationSeconds = 12, CadenceChainWindowSeconds = 6, CadenceDamagePerCharge = 2,
        CadenceStaminaRestorePerCharge = 1, CadenceStaminaRestoreMaximum = 3, CadenceRestoreCooldownSeconds = 12,
        CadenceNextAbilityAccuracy = 5, CadenceAccuracyDurationSeconds = 8, CadenceAccuracyCooldownSeconds = 10,
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
    public void ZeroPaymentPreservesCadenceButConsumesCapturedAccuracy()
    {
        var state = new PairedRangedTraitState();
        state.RecordAbilityHit(Now, Config, Guid.NewGuid(), 1);
        var activation = new WeaponAbilityTraitActivation(state.CaptureAbility(Now, Config, true, true), 4);
        activation.SetPaidStamina(0);
        state.CommitAbility(Now, Config, activation.Snapshot).Should().BeTrue();
        state.Cadence.Should().Be(1);
        state.CadenceAccuracyExpires.Should().Be(default);
        activation.Snapshot.CadenceSpent.Should().Be(0);
        activation.Snapshot.AccuracyAdjustment.Should().Be(5);
    }

    [Test]
    public void CapturedChargesDoNotSpendRewardsEarnedAfterTheCastStarted()
    {
        var state = new PairedRangedTraitState();
        state.RecordAbilityHit(Now, Config, Guid.NewGuid(), 1);
        var captured = state.CaptureAbility(Now, Config, true, true);
        state.RecordAbilityHit(Now.AddSeconds(1), Config, Guid.NewGuid(), 1);
        state.CommitAbility(Now.AddSeconds(1), Config, captured).Should().BeTrue();
        state.Cadence.Should().Be(1);
        state.CommitAbility(Now.AddSeconds(1), Config, captured).Should().BeFalse();
        state.CompleteAbility(Now.AddSeconds(1), Config, captured, true, 6, 0).Should().Be(1);
        state.CompleteAbility(Now.AddSeconds(1), Config, captured, true, 6, 0).Should().Be(0);
    }

    [Test]
    public void ASpenderThatChainsGainsFreshCadenceAfterSpending()
    {
        var state = new PairedRangedTraitState();
        state.RecordAbilityHit(Now, Config, Guid.NewGuid(), 2);
        var spender = state.CaptureAbility(Now.AddSeconds(1), Config, false, true);
        state.CommitAbility(Now.AddSeconds(1), Config, spender).Should().BeTrue();
        state.RecordAbilityHit(Now.AddSeconds(1), Config, spender.ActivationId, 1);
        spender.CadenceSpent.Should().Be(2);
        state.Cadence.Should().Be(1, "the spend happens at payment; the chained hit then grants one new charge");
    }

    [Test]
    public void LeadShotEarnsOne_AndTempoShotEarnsTwo_WithoutAChain()
    {
        Activation(cadenceOnHit: 1, chained: false).CadenceEarnedOnHit.Should().Be(1);
        Activation(cadenceOnHit: 2, chained: false).CadenceEarnedOnHit.Should().Be(2);
    }

    [Test]
    public void AChainEarnsOne_AndDoesNotStackWithTheAbilitysOwnGrant()
    {
        Activation(cadenceOnHit: 0, chained: true).CadenceEarnedOnHit.Should().Be(1);
        Activation(cadenceOnHit: 1, chained: true).CadenceEarnedOnHit.Should().Be(1);
        Activation(cadenceOnHit: 2, chained: true).CadenceEarnedOnHit.Should().Be(2);
        Activation(cadenceOnHit: 0, chained: false).CadenceEarnedOnHit.Should().Be(0,
            "an unchained ability without its own grant earns nothing");
    }

    [Test]
    public void AQueuedChainedHit_LandingAfterTheWindow_EarnsOnlyTheAbilitysOwnGrant()
    {
        var state = new PairedRangedTraitState();
        var config = Config with { CadenceChainWindowSeconds = 6 };
        state.RecordAbilityUse(Now, config, 1);
        state.RecordAbilityUse(Now.AddSeconds(2), config, 2, out var landsBy).Should().BeTrue();
        landsBy.Should().Be(Now.AddSeconds(6), "the chain counts from the previous ability");
        var queued = Activation(cadenceOnHit: 0, chained: true);
        queued.ChainLandsBy = landsBy;
        queued.CadenceEarnedOnHitAt(Now.AddSeconds(6)).Should().Be(1, "the landing window is inclusive");
        queued.CadenceEarnedOnHitAt(Now.AddSeconds(20)).Should().Be(0, "a hit queued past the window no longer chains");
        var tempo = Activation(cadenceOnHit: 2, chained: true);
        tempo.ChainLandsBy = landsBy;
        tempo.CadenceEarnedOnHitAt(Now.AddSeconds(20)).Should().Be(2, "the ability's own grant does not depend on the chain");
    }

    [TestCase(2, 0, 3, 6)]
    [TestCase(2, 1, 3, 9)]
    [TestCase(2, 3, 3, 15)]
    [TestCase(4, 3, 3, 21)]
    [TestCase(4, 2, 1, 6)]
    public void SteadyCadenceAddsItsBonusToEveryChargeSpent(int perCharge, int steadyCadence, int spent, int expected)
    {
        // PairedRangedTraits captures (ability override ?? generic per-charge) + Steady Cadence; Final Salvo overrides with 4.
        var activation = new WeaponAbilityTraitActivation(new(Guid.NewGuid(), false, true, spent, 0), perCharge + steadyCadence)
            { IsCommitted = true };
        activation.TryTakeDamageBonus(40).Should().Be(expected);
    }

    [Test]
    public void EmptySourceSnapshotCannotConsumeItsOwnHitReward()
    {
        var state = new PairedRangedTraitState();
        var source = state.CaptureAbility(Now, Config, true, true);
        state.CommitAbility(Now, Config, source).Should().BeTrue();
        state.RecordAbilityHit(Now, Config, source.ActivationId, 1);
        source.CadenceSpent.Should().Be(0);
        state.Cadence.Should().Be(1);
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

    private static WeaponAbilityTraitActivation Activation(int cadenceOnHit, bool chained) =>
        new(new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), false, false, 0, 0), 0)
        {
            CadenceOnHit = cadenceOnHit,
            ContinuesChain = chained,
        };
}
