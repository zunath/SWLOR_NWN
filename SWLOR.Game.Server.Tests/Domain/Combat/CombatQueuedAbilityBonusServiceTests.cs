using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The queued ability bonus part of <c>Combat</c>: the opening auto-attack, the queued weapon ability snapshots and the idle
/// activation bonuses.
/// </summary>
[TestFixture]
public class CombatQueuedAbilityBonusServiceTests : CombatAbilityBonusTestBase
{
    [Test]
    public void TheOpeningAutoAttackNeedsAValidWeaponAttackerTheRightSkillAndIdleTime()
    {
        _invalid.Add(Gone);
        _queued.PrepareOpeningAutoAttack(Gone, SkillType.Rifle).Should().Be(0);
        _queued.PrepareOpeningAutoAttack(Attacker, SkillType.Force).Should().Be(0, "not a weapon skill");

        Stat(Attacker, StatType.OpeningAutoAttackSkillType, (int)SkillType.Pistol);
        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 10);
        _queued.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle).Should().Be(0, "the opener is for pistols");

        Stat(Attacker, StatType.OpeningAutoAttackSkillType, 0);
        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 0);
        _queued.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle).Should().Be(0, "no idle time is configured");

        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 10);
        _lastCompletedActivity = _now.AddSeconds(-5);
        _queued.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle).Should().Be(0, "the attacker fought 5 seconds ago");
        _activityCalls.Calls.Should().NotContain(call => call.Member == nameof(ICombatActivityService.TrackCombatActivity));
    }

    [Test]
    public void AnOpeningAutoAttackAfterIdleTimeStoresItsDamageAndCriticalBonusesAndReturnsItsCriticalRate()
    {
        Stat(Attacker, StatType.OpeningAutoAttackSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 10);
        Stat(Attacker, StatType.OpeningAutoAttackDamageBonus, 4);
        Stat(Attacker, StatType.OpeningAutoAttackCriticalDamagePercentAdjustment, 25);
        Stat(Attacker, StatType.OpeningAutoAttackCriticalRatePercentAdjustment, 15);
        _lastCompletedActivity = _now.AddSeconds(-11);

        _queued.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle).Should().Be(15);

        _activityCalls.Calls.Where(call => call.Member == nameof(ICombatActivityService.TrackCombatActivity))
            .Select(call => call.Arguments[0]).Should().Equal(Attacker);
        Removed().Should().Equal($"{Attacker} {nameof(OpeningAttackReadyStatusEffect)}");
        Stored(Attacker, StatType.CurrentAutoAttackDamageBonus, StatType.CurrentAutoAttackDamageBonus).Should().Be(4);
        _queued.ConsumeOpeningAutoAttackCriticalDamageAdjustment(Attacker).Should().Be(25);
        _queued.ConsumeOpeningAutoAttackCriticalDamageAdjustment(Attacker).Should().Be(0, "consumed once");

        _now = _now.AddSeconds(7);
        Stored(Attacker, StatType.CurrentAutoAttackDamageBonus, StatType.CurrentAutoAttackDamageBonus).Should().Be(0, "it lasts 6 seconds");
    }

    [Test]
    public void AnOpeningAutoAttackWithNoStoredBonusesStoresNothingAndANeverActiveAttackerQualifies()
    {
        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 10);
        Stat(Attacker, StatType.OpeningAutoAttackCriticalRatePercentAdjustment, 5);

        _queued.PrepareOpeningAutoAttack(Attacker, SkillType.Rifle).Should().Be(5, "an attacker with no activity is idle");
        Stored(Attacker, StatType.CurrentAutoAttackDamageBonus, StatType.CurrentAutoAttackDamageBonus).Should().Be(0);
        Stored(Attacker, StatType.CurrentAutoAttackCriticalDamagePercentAdjustment,
            StatType.CurrentAutoAttackCriticalDamagePercentAdjustment).Should().Be(0);
    }

    [Test]
    public void AQueuedWeaponAbilityCapturesTheOpeningBonusesAtActivationAndClearsTheStalePerSwingPayload()
    {
        _invalid.Add(Gone);
        _queued.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Gone, SkillType.Rifle);
        _queued.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Invalid);
        _activityCalls.Calls.Should().BeEmpty();

        Store(Attacker, StatType.QueuedWeaponAbilityDamageBonus, 9, StatType.QueuedWeaponAbilityCriticalRateSkillType);
        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 10);
        Stat(Attacker, StatType.OpeningAutoAttackDamageBonus, 4);
        Stat(Attacker, StatType.OpeningAutoAttackCriticalDamagePercentAdjustment, 25);
        Stat(Attacker, StatType.OpeningAutoAttackCriticalRatePercentAdjustment, 15);
        // An earlier activation of the same skill this window adds to what this one captures.
        Store(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, (int)SkillType.Rifle,
            StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, 2, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        Store(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment, 5,
            StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, 40,
            StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);

        _queued.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Rifle);

        Stored(Attacker, StatType.QueuedWeaponAbilityDamageBonus, StatType.QueuedWeaponAbilityCriticalRateSkillType)
            .Should().Be(0, "the stale per-swing payload is discarded first");
        Stored(Attacker, StatType.CurrentAutoAttackDamageBonus, StatType.CurrentAutoAttackDamageBonus).Should().Be(0, "moved to the snapshot");
        _queued.ConsumeOpeningAutoAttackCriticalDamageAdjustment(Attacker).Should().Be(0, "moved to the snapshot");
        var group = StatType.QueuedWeaponAbilityActivationCriticalRateSkillType;
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be((int)SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, group).Should().Be(6);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment, group).Should().Be(20);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, group).Should().Be(40, "the larger critical damage wins");

        _now = _now.AddSeconds(29);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, group).Should().Be(6, "the snapshot lasts 30 seconds");
    }

    [Test]
    public void AQueuedActivationWithNothingToCaptureLeavesTheSnapshotAloneAndAnotherSkillReplacesIt()
    {
        var group = StatType.QueuedWeaponAbilityActivationCriticalRateSkillType;
        Store(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, (int)SkillType.Pistol, group);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, 2, group);

        _queued.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be((int)SkillType.Pistol, "nothing to capture");

        Stat(Attacker, StatType.OpeningAutoAttackIdleSeconds, 10);
        Stat(Attacker, StatType.OpeningAutoAttackCriticalDamagePercentAdjustment, 10);
        _queued.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(Attacker, SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be((int)SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, group).Should().Be(0, "a pistol snapshot does not carry over");
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, group).Should().Be(10);
    }

    [Test]
    public void ClearingTheOpeningModifiersConsumesBothCurrentBonusesAndTheAttemptBonuses()
    {
        Store(Attacker, StatType.CurrentAutoAttackDamageBonus, 3, StatType.CurrentAutoAttackDamageBonus);
        Store(Attacker, StatType.CurrentAutoAttackCriticalDamagePercentAdjustment, 30, StatType.CurrentAutoAttackCriticalDamagePercentAdjustment);
        _queued.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Rifle, 12);

        _queued.ClearOpeningAutoAttackModifiers(Attacker);

        Stored(Attacker, StatType.CurrentAutoAttackDamageBonus, StatType.CurrentAutoAttackDamageBonus).Should().Be(0);
        Stored(Attacker, StatType.CurrentAutoAttackCriticalDamagePercentAdjustment, StatType.CurrentAutoAttackCriticalDamagePercentAdjustment).Should().Be(0);
        _queued.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((0, 0, 0));
    }

    [Test]
    public void AQueuedAttemptCriticalRateIsStoredForSixSecondsAndAddsToAnAttemptOfTheSameSkill()
    {
        _invalid.Add(Gone);
        _queued.StoreQueuedWeaponAbilityCriticalRateBonus(Gone, SkillType.Rifle, 10);
        _queued.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Invalid, 10);
        _queued.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Rifle, 0);
        _queued.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((0, 0, 0));

        var group = StatType.QueuedWeaponAbilityCriticalRateSkillType;
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalRateSkillType, (int)SkillType.Rifle, group);
        Store(Attacker, StatType.QueuedWeaponAbilityDamageBonus, 2, group);
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment, 5, group);
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment, 4, group);
        _queued.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Rifle, 10);
        Stored(Attacker, StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment, group).Should().Be(14);
        Stored(Attacker, StatType.QueuedWeaponAbilityDamageBonus, group).Should().Be(2);
        Stored(Attacker, StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment, group).Should().Be(5);

        _queued.StoreQueuedWeaponAbilityCriticalRateBonus(Attacker, SkillType.Pistol, 3);
        Stored(Attacker, StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment, group).Should().Be(3, "another skill starts over");
        Stored(Attacker, StatType.QueuedWeaponAbilityDamageBonus, group).Should().Be(0);

        _now = _now.AddSeconds(7);
        Stored(Attacker, StatType.QueuedWeaponAbilityCriticalRateSkillType, group).Should().Be(0, "the attempt lasts 6 seconds");
    }

    [Test]
    public void AnActivationCriticalRateIsStoredOnlyWhenPositiveAndReplacesTheSnapshotSkill()
    {
        var group = StatType.QueuedWeaponAbilityActivationCriticalRateSkillType;
        _invalid.Add(Gone);
        _queued.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Gone, SkillType.Rifle, 10);
        _queued.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Invalid, 10);
        _queued.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Rifle, 0);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be(0);

        _queued.StoreQueuedWeaponAbilityActivationCriticalRateBonus(Attacker, SkillType.Rifle, 10);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be((int)SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment, group).Should().Be(10);
    }

    [Test]
    public void TheIdleBonusesOfAQueuedActivationAreStoredWhenThereAreAny()
    {
        var group = StatType.QueuedWeaponAbilityActivationCriticalRateSkillType;
        _invalid.Add(Gone);
        _queued.StoreQueuedWeaponAbilityActivationIdleBonuses(Gone, SkillType.Rifle);
        _queued.StoreQueuedWeaponAbilityActivationIdleBonuses(Attacker, SkillType.Invalid);
        _activityCalls.Calls.Should().BeEmpty();
        _queued.StoreQueuedWeaponAbilityActivationIdleBonuses(Attacker, SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be(0, "no idle bonus");

        _idleBonuses = (5, 12, 30);
        _queued.StoreQueuedWeaponAbilityActivationIdleBonuses(Attacker, SkillType.Rifle);
        _activityCalls.Calls.Last().Arguments.Should().Equal(Attacker, SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be((int)SkillType.Rifle);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, group).Should().Be(5);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment, group).Should().Be(12);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, group).Should().Be(30);
        _queued.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Rifle).Should().Be(1012);
        _queued.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Pistol).Should().Be(1000, "the idle accuracy is the rifle's");

        _queued.ClearQueuedWeaponAbilityActivationBonuses(Attacker);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, group).Should().Be(0);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, group).Should().Be(0);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment, group).Should().Be(0);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, group).Should().Be(0);
    }

    [Test]
    public void TheWeaponAbilityActivationIdleSnapshotKeepsItsValuesOnlyWhenIdleAndClearsTheOldOneFirst()
    {
        _queued.HasWeaponAbilityActivationIdleSnapshot(Attacker).Should().BeFalse();
        _queued.StoreWeaponAbilityActivationIdleBonuses(Attacker, true, 4, 9, 15);
        _queued.HasWeaponAbilityActivationIdleSnapshot(Attacker).Should().BeTrue();
        _queued.GetWeaponAbilityActivationIdleDamageBonus(Attacker).Should().Be(4);
        _queued.GetWeaponAbilityActivationIdleCriticalRateBonus(Attacker).Should().Be(9);
        _queued.GetWeaponAbilityActivationIdleDefenseIgnorePercent(Attacker).Should().Be(15);

        _queued.StoreWeaponAbilityActivationIdleBonuses(Attacker, false, 4, 9, 15);
        _queued.HasWeaponAbilityActivationIdleSnapshot(Attacker).Should().BeTrue("a snapshot is taken even when not idle");
        _queued.GetWeaponAbilityActivationIdleDamageBonus(Attacker).Should().Be(0);
        _queued.GetWeaponAbilityActivationIdleCriticalRateBonus(Attacker).Should().Be(0);
        _queued.GetWeaponAbilityActivationIdleDefenseIgnorePercent(Attacker).Should().Be(0);

        _invalid.Add(Attacker);
        _queued.StoreWeaponAbilityActivationIdleBonuses(Attacker, true, 4, 9, 15);
        _queued.HasWeaponAbilityActivationIdleSnapshot(Attacker).Should().BeFalse("cleared, and nothing stored for an invalid creature");

        _invalid.Remove(Attacker);
        _queued.StoreWeaponAbilityActivationIdleBonuses(Attacker, true, 4, 9, 15);
        _queued.ClearWeaponAbilityActivationIdleBonuses(Attacker);
        _queued.HasWeaponAbilityActivationIdleSnapshot(Attacker).Should().BeFalse();
        _queued.GetWeaponAbilityActivationIdleDamageBonus(Attacker).Should().Be(0);
    }

    [Test]
    public void TheQueuedActivationHitChanceAddsTheStatusCategoryAccuracyOfTheQueuedAbility()
    {
        Stat(Attacker, StatType.AbilityStatusCategoryBonusSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.AbilityStatusCategoryBonusRequiredCategory, (int)StatusEffectCategory.Control);
        Stat(Attacker, StatType.AbilityStatusCategoryHitChancePercentAdjustment, 7);
        var control = new AbilityDetail { ResolveImpactStatusCategories = _ => StatusEffectCategory.Control };
        var plain = new AbilityDetail();

        _queued.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Rifle, control).Should().Be(1007);
        _queued.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Rifle, plain).Should().Be(1000);
        _queued.GetQueuedWeaponAbilityActivationHitChanceAdjustment(Attacker, SkillType.Rifle).Should().Be(1000);
        _queriesCalls.Calls.Where(call => call.Member == nameof(ICombatQueriesService.GetPhysicalAndForceAbilityHitChanceAdjustment))
            .Select(call => call.Arguments[1]).Should().AllBeEquivalentTo(SkillType.Rifle);
    }

    [Test]
    public void TheQueuedWeaponAbilityBonusesAreConsumedFromTheAttemptAndTheActivationOfTheSameSkill()
    {
        var attempt = StatType.QueuedWeaponAbilityCriticalRateSkillType;
        var activation = StatType.QueuedWeaponAbilityActivationCriticalRateSkillType;
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalRateSkillType, (int)SkillType.Rifle, attempt);
        Store(Attacker, StatType.QueuedWeaponAbilityDamageBonus, 2, attempt);
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment, 4, attempt);
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment, 50, attempt);
        Store(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, (int)SkillType.Rifle, activation);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleDamageBonus, 3, activation);
        Store(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment, 8, activation);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment, 6, activation);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, 30, activation);

        _queued.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Pistol).Should().Be((0, 0, 0));
        _queued.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((5, 12, 50));
        Stored(Attacker, StatType.QueuedWeaponAbilityCriticalRateSkillType, attempt).Should().Be(0);
        Stored(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, activation).Should().Be(0);
        Stored(Attacker, StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment, activation).Should().Be(0);
        _queued.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((0, 0, 0));

        Store(Attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType, (int)SkillType.Rifle, activation);
        Store(Attacker, StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment, 30, activation);
        _queued.ConsumeQueuedWeaponAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((0, 0, 30), "the activation's critical damage alone");
    }

    [Test]
    public void ClearingTheQueuedAttemptBonusesConsumesAllFour()
    {
        var group = StatType.QueuedWeaponAbilityCriticalRateSkillType;
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalRateSkillType, (int)SkillType.Rifle, group);
        Store(Attacker, StatType.QueuedWeaponAbilityDamageBonus, 2, group);
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment, 4, group);
        Store(Attacker, StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment, 50, group);

        _queued.ClearQueuedWeaponAbilityAttemptBonuses(Attacker);

        foreach (var stat in new[] { StatType.QueuedWeaponAbilityCriticalRateSkillType, StatType.QueuedWeaponAbilityDamageBonus,
                     StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment, StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment })
            Stored(Attacker, stat, group).Should().Be(0, stat.ToString());
    }
}
