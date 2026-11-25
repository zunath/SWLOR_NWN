using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The critical hit effects part of <c>Combat</c>: what a critical hit sets off, its sequence table, and the riders of a non-critical ability.
/// </summary>
[TestFixture]
public class CombatCriticalHitEffectsServiceTests : CombatHitEffectsTestBase
{
    [Test]
    public void ANonCriticalDoesNothingAndAZeroDamageCriticalOnlyEarnsLimitedHaste()
    {
        Stat(Attacker, StatType.CriticalHitLimitedHastePercentAdjustment, 20);
        Stat(Attacker, StatType.CriticalHitLimitedHasteDurationSeconds, 6);
        Stat(Attacker, StatType.CriticalHitLimitedHasteAttackCount, 2);
        Stat(Attacker, StatType.CriticalHitLimitedHasteStatusEffectIcon, (int)EffectIconType.Haste);
        Stat(Attacker, StatType.CriticalStaminaRestore, 3);

        Critical(rating: 0);
        _log.Should().BeEmpty();
        Critical(damage: 0);
        _log.Should().Equal($"Apply {Attacker}>{Attacker} {nameof(LimitedHasteStatusEffect)} haste 6");

        _log.Clear();
        Stat(Attacker, StatType.CriticalHitLimitedHasteTriggerSkillType, (int)SkillType.Rifle);
        Critical(damage: 0, skill: SkillType.Pistol);
        Stat(Attacker, StatType.CriticalHitLimitedHasteTriggerSkillType, 0);
        Stat(Attacker, StatType.CriticalHitLimitedHasteStatusEffectIcon, 0);
        Critical(damage: 0);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ACriticalRunsItsRidersInOrder()
    {
        _sunderedCritical = 5;
        Stat(Attacker, StatType.CriticalStaminaRestore, 3);
        Stat(Attacker, StatType.CriticalStaminaRestoreSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.CriticalStaminaRestoreCooldownSeconds, 8);
        Stat(Attacker, StatType.CriticalPoisonedTargetStaminaRestore, 1);
        Stat(Attacker, StatType.CriticalMarkedTargetStaminaRestore, 2);
        _statuses.Add((Defender, typeof(PoisonStatusEffect)));
        _sourceStatuses.Add((Defender, typeof(MarkingTossStatusEffect), Attacker));
        Stat(Attacker, StatType.CriticalTargetFPLossPercentOfDamage, 50);
        Stat(Attacker, StatType.CriticalTargetStaminaLossPercentOfDamage, 25);
        Stat(Attacker, StatType.CriticalHPPercentOfDamageRestore, 10);
        Stat(Attacker, StatType.CriticalHPPercentOfDamageRestoreCooldownSeconds, 6);
        Stat(Attacker, StatType.CriticalAccuracyPercentAdjustment, 5);
        Stat(Attacker, StatType.CriticalAccuracyDurationSeconds, 6);
        Stat(Attacker, StatType.CriticalTargetEvasionPercentAdjustment, -5);
        Stat(Attacker, StatType.CriticalTargetEvasionDurationSeconds, 6);
        Stat(Attacker, StatType.CriticalTargetDefensePercentAdjustment, -10);
        Stat(Attacker, StatType.CriticalTargetDefenseDurationSeconds, 6);
        Stat(Attacker, StatType.SingleTargetCriticalTargetDefensePercentAdjustment, -4);
        Stat(Attacker, StatType.SingleTargetCriticalTargetDefenseDurationSeconds, 3);

        Critical(singleTarget: true, skill: SkillType.Rifle);
        FloatingText().Should().ContainSingle().Which.Should().Contain("Weak Points");
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.CriticalStaminaRestore} 8",
            $"HitStamina {Attacker} {StatType.CriticalStaminaRestore} 3",
            $"HitStamina {Attacker} {StatType.CriticalPoisonedTargetStaminaRestore} 1",
            $"HitStamina {Attacker} {StatType.CriticalMarkedTargetStaminaRestore} 2",
            $"LoseFP {Defender} {StatType.CriticalTargetFPLossPercentOfDamage} 10",
            $"LoseStamina {Defender} {StatType.CriticalTargetStaminaLossPercentOfDamage} 5",
            $"Trigger {Attacker} {StatType.CriticalHPPercentOfDamageRestore} 6",
            $"Heal {Attacker} 20 10 False 6",
            $"Replace {Attacker} {StatType.AccuracyPercentAdjustment} 5 6 {StatType.CriticalAccuracyPercentAdjustment}",
            $"Replace {Defender} {StatType.EvasionPercentAdjustment} -5 6 {StatType.CriticalTargetEvasionPercentAdjustment}",
            $"Apply {Attacker}>{Defender} {nameof(ExposedStatusEffect)} exposed 6 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} {nameof(ExposedStatusEffect)} exposed 3 {CombatDamageType.Physical}");
    }

    [Test]
    public void ACriticalSkipsTheRidersItIsNotConfiguredFor()
    {
        Stat(Attacker, StatType.CriticalStaminaRestore, 3);
        Stat(Attacker, StatType.CriticalStaminaRestoreSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.CriticalPoisonedTargetStaminaRestore, 1);
        Stat(Attacker, StatType.CriticalMarkedTargetStaminaRestore, 2);
        Stat(Attacker, StatType.CriticalHPPercentOfDamageRestore, 10);
        Stat(Attacker, StatType.CriticalAccuracyPercentAdjustment, 5);
        Stat(Attacker, StatType.CriticalTargetEvasionPercentAdjustment, -5);
        Stat(Attacker, StatType.CriticalTargetDefensePercentAdjustment, -10);
        Stat(Attacker, StatType.SingleTargetCriticalTargetDefensePercentAdjustment, -4);
        _triggerReady = false;

        Critical(skill: SkillType.Pistol, singleTarget: true);
        FloatingText().Should().BeEmpty();
        _log.Should().Equal($"Trigger {Attacker} {StatType.CriticalHPPercentOfDamageRestore} 0");
        _log.Clear();
        _triggerReady = true;
        Critical(skill: SkillType.Rifle);
        Lines("HitStamina").Should().Equal($"HitStamina {Attacker} {StatType.CriticalStaminaRestore} 3");
    }

    [Test]
    public void AStaffCriticalAddsItsDefenseReductionAndTheLongerDuration()
    {
        Stat(Attacker, StatType.CriticalTargetDefensePercentAdjustment, -10);
        Stat(Attacker, StatType.CriticalTargetDefenseDurationSeconds, 4);
        Stat(Attacker, StatType.StaffCriticalTargetDefensePercentAdjustment, -5);
        Stat(Attacker, StatType.StaffCriticalTargetDefenseDurationSeconds, 9);

        Critical(skill: SkillType.Staff);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(ExposedStatusEffect)} exposed 9 {CombatDamageType.Physical}");
        _log.Clear();
        Critical(skill: SkillType.Rifle);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(ExposedStatusEffect)} exposed 4 {CombatDamageType.Physical}");
    }

    [Test]
    public void ACriticalGrantsTheNextAbilityItsDamageAndDefenseIgnore()
    {
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonusPerkType, (int)PerkType.DoubleShot);
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonus, 6);
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonusDurationSeconds, 10);
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonusCooldownSeconds, 12);
        Stat(Attacker, StatType.CriticalNextSkillAbilityDefenseIgnorePercentAdjustment, 25);
        Stat(Attacker, StatType.CriticalNextSkillAbilityDefenseIgnoreDurationSeconds, 8);
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonusTriggerSkillType, (int)SkillType.Rifle);

        Critical(skill: SkillType.Rifle);
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.CriticalNextAbilityDamageBonus} 12",
            $"NextAbilityDamage {Attacker} {PerkType.DoubleShot} 6 10",
            $"Replace {Attacker} {StatType.NextSkillAbilitySkillType} {(int)SkillType.Rifle} 8 {StatType.NextSkillAbilitySkillType}",
            $"Replace {Attacker} {StatType.NextSkillAbilityDefenseIgnorePercentAdjustment} 25 8 {StatType.NextSkillAbilitySkillType}",
            $"Replace {Attacker} {StatType.NextSkillAbilityRequiredCategory} 0 8 {StatType.NextSkillAbilitySkillType}");

        _log.Clear();
        Critical();
        _log.Should().BeEmpty("the trigger needs its skill, and the defense ignore needs a skill to grant it to");
        _triggerReady = false;
        Stat(Attacker, StatType.CriticalNextSkillAbilityDefenseIgnoreDurationSeconds, 0);
        Critical(skill: SkillType.Rifle);
        _log.Should().Equal($"Trigger {Attacker} {StatType.CriticalNextAbilityDamageBonus} 12");
        _log.Clear();
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonusTriggerSkillType, (int)SkillType.Pistol);
        Critical(skill: SkillType.Rifle);
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonusTriggerSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.CriticalNextAbilityDamageBonus, 0);
        Critical(skill: SkillType.Rifle);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ACriticalGrantsAnUndelayedNextAutoAttackForItsSkillOrAny()
    {
        Stat(Attacker, StatType.CriticalNextAutoAttackNoDelayDurationSeconds, 4);
        Stat(Attacker, StatType.CriticalNextAutoAttackNoDelayCooldownSeconds, 9);
        Critical(skill: SkillType.Pistol);
        _log.Should().Equal($"Trigger {Attacker} {StatType.CriticalNextAutoAttackNoDelaySkillType} 9", $"NoDelay {Attacker} 4");

        _log.Clear();
        Stat(Attacker, StatType.CriticalNextAutoAttackNoDelaySkillType, (int)SkillType.Pistol);
        Critical(skill: SkillType.Pistol);
        _log.Should().Equal($"Trigger {Attacker} {StatType.CriticalNextAutoAttackNoDelaySkillType} 9", $"NoDelay {Attacker} {SkillType.Pistol} 4");

        _log.Clear();
        _triggerReady = false;
        Critical(skill: SkillType.Pistol);
        Stat(Attacker, StatType.CriticalNextAutoAttackNoDelayTriggerSkillType, (int)SkillType.Rifle);
        Critical(skill: SkillType.Pistol);
        Stat(Attacker, StatType.CriticalNextAutoAttackNoDelayTriggerSkillType, 0);
        Stat(Attacker, StatType.CriticalNextAutoAttackNoDelayDurationSeconds, 0);
        Critical(skill: SkillType.Pistol);
        Lines("NoDelay").Should().BeEmpty();
    }

    [Test]
    public void ACriticalBesideTheTargetRollsItsStaminaChance()
    {
        Stat(Attacker, StatType.CriticalSideAttackStaminaRestoreChance, 100);
        Stat(Attacker, StatType.CriticalSideAttackStaminaRestore, 2);
        Critical();
        Lines("HitStamina").Should().BeEmpty("not beside the target");
        _beside = true;
        Critical();
        Lines("HitStamina").Should().Equal($"HitStamina {Attacker} {StatType.CriticalSideAttackStaminaRestore} 2");

        _log.Clear();
        Stat(Attacker, StatType.CriticalSideAttackStaminaRestoreChance, 1);
        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] > 1);
        var rolls = Rolls(seed, 2);
        Critical();
        Random.D100(1).Should().Be(rolls[1], "one roll");
        Lines("HitStamina").Should().BeEmpty();
        Stat(Attacker, StatType.CriticalSideAttackStaminaRestore, 0);
        rolls = Rolls(seed, 1);
        Critical();
        Random.D100(1).Should().Be(rolls[0], "nothing to restore draws nothing");
    }

    [Test]
    public void ACriticalExtendsTheAttackersBleedsOnItsCooldown()
    {
        Stat(Attacker, StatType.CriticalBleedingStatusDurationExtensionSeconds, 3);
        Stat(Attacker, StatType.CriticalBleedingStatusDurationExtensionCooldownSeconds, 7);
        Critical();
        _log.Should().BeEmpty("the target has none of the attacker's bleeds");

        _sourceStatuses.Add((Defender, typeof(HemorrhageStatusEffect), Attacker));
        Critical();
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.CriticalBleedingStatusDurationExtensionSeconds} 7",
            $"Extend {Defender} {nameof(BleedStatusEffect)} {Attacker} 3",
            $"Extend {Defender} {nameof(HemorrhageStatusEffect)} {Attacker} 3");

        _log.Clear();
        _triggerReady = false;
        _sourceStatuses.Add((Defender, typeof(BleedStatusEffect), Attacker));
        Critical();
        _log.Should().Equal($"Trigger {Attacker} {StatType.CriticalBleedingStatusDurationExtensionSeconds} 7");
        _log.Clear();
        _invalid.Add(Defender);
        Critical();
        _log.Should().BeEmpty();
    }

    [Test]
    public void ACriticalSequenceRestoresStaminaWhenEnoughLandWithinTheWindow()
    {
        Stat(Attacker, StatType.CriticalHitSequenceCountRequired, 3);
        Stat(Attacker, StatType.CriticalHitSequenceWindowSeconds, 5);
        Stat(Attacker, StatType.CriticalHitSequenceStaminaRestore, 4);

        Critical();
        _now = _now.AddSeconds(5);
        Critical();
        Lines("HitStamina").Should().BeEmpty();
        _now = _now.AddSeconds(4);
        Critical();
        Lines("HitStamina").Should().Equal($"HitStamina {Attacker} {StatType.CriticalHitSequenceStaminaRestore} 4");

        _log.Clear();
        Critical();
        Critical();
        _now = _now.AddSeconds(6);
        Critical();
        Lines("HitStamina").Should().BeEmpty("the window lapsed, so the sequence started again");
        _service.ForgetHitEffectState(Attacker);
        Critical();
        Critical();
        Lines("HitStamina").Should().BeEmpty("forgetting the attacker cleared its count");
        Critical();
        Lines("HitStamina").Should().ContainSingle();

        _log.Clear();
        Stat(Attacker, StatType.CriticalHitSequenceStaminaRestore, 0);
        Critical();
        Critical();
        Critical();
        Lines("HitStamina").Should().BeEmpty();
    }

    [Test]
    public void ACriticalCanHastenAndHideTheAttacker()
    {
        Stat(Attacker, StatType.CriticalHitSelfEvasionPercentAdjustment, 10);
        Stat(Attacker, StatType.CriticalHitSelfEvasionDurationSeconds, 4);
        Stat(Attacker, StatType.CriticalHitSelfHastePercentAdjustment, 15);
        Stat(Attacker, StatType.CriticalHitSelfHasteDurationSeconds, 5);
        Critical();
        Lines("Replace").Should().Equal(
            $"Replace {Attacker} {StatType.EvasionPercentAdjustment} 10 4 {StatType.CriticalHitSelfEvasionPercentAdjustment}",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 15 5 {StatType.CriticalHitSelfHastePercentAdjustment}");
        _log.Clear();
        Stat(Attacker, StatType.CriticalHitSelfEvasionDurationSeconds, 0);
        Stat(Attacker, StatType.CriticalHitSelfHasteDurationSeconds, 0);
        Critical();
        Lines("Replace").Should().BeEmpty();
    }

    // ------------------------------------------------------------------ non-critical ability

    [Test]
    public void ANonCriticalRangedAbilityStacksTheNextOnesCriticalRateAndShowsItsTracker()
    {
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment, 5);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateMax, 20);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateWindowSeconds, 30);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateRangedOnly, 1);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateTrackerEffectIconType, (int)EffectIconType.Haste);
        _temporary[StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment] = 10;

        _critical.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle);
        _log.Should().Equal(
            $"Replace {Attacker} {StatType.PersistentNextSkillAbilityCriticalRateSkillType} {(int)SkillType.Rifle} 30 {StatType.PersistentNextSkillAbilityCriticalRateSkillType}",
            $"Replace {Attacker} {StatType.PersistentNextSkillAbilityCriticalRateRangedOnly} 1 30 {StatType.PersistentNextSkillAbilityCriticalRateSkillType}",
            $"AddCapped {Attacker} {StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment} 5 30 20 {StatType.PersistentNextSkillAbilityCriticalRateSkillType} 1 True",
            $"Remove {Attacker} {nameof(CriticalRateStackTrackerStatusEffect)} False",
            $"Apply {Attacker}>{Attacker} {nameof(CriticalRateStackTrackerStatusEffect)} tracker 30");
        FloatingText().Should().ContainSingle().Which.Should().Contain("Next ranged ability +10% Critical Rate");

        _log.Clear();
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateTrackerEffectIconType, 0);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateRangedOnly, 0);
        _critical.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle);
        Lines("Remove").Should().ContainSingle();
        Lines("Apply").Should().BeEmpty("no tracker icon");

        _log.Clear();
        _temporary.Remove(StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment);
        _critical.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle);
        Lines("Remove").Should().BeEmpty("nothing stored");
    }

    [Test]
    public void ANonCriticalAbilityNeedsItsSkillAndEveryValue()
    {
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment, 5);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateMax, 20);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateWindowSeconds, 0);

        _critical.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle);
        _critical.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Pistol);
        _critical.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Invalid);
        _invalid.Add(Gone);
        _critical.ApplyNonCriticalAbilityEffects(Gone, Defender, SkillType.Rifle);
        _log.Should().BeEmpty();
    }
}
