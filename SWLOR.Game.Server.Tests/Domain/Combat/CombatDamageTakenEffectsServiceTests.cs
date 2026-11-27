using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The after-damage part of <c>Combat</c>: the damage taken handler and its riders, low HP effects, avoided attacks, melee
/// damage taken, deflection and the effects of an ability granted deflection.
/// </summary>
[TestFixture]
public class CombatDamageTakenEffectsServiceTests : CombatDamageTakenTestBase
{
    [Test]
    public void TheDamageHandlerRunsTheEffectsForTheEventsDefenderDamagerAndTotal()
    {
        _engine.SetReturn("OBJECT_SELF", Defender);
        _engine.SetReturn("GetLastDamager", Attacker);
        _engine.SetReturn("GetTotalDamageDealt", 12);

        _effects.ApplyDamageTakenEffects();
        Lines("RecentTaken").Should().Equal($"RecentTaken {Defender}");
        _engine.CallsTo("GetLastDamager").Should().ContainSingle().Which.Arguments[0].Should().Be(Defender);
    }

    [Test]
    public void DamageTakenRunsItsRidersInOrderAndEntersCombatAgainstAHostileDamager()
    {
        _hostile.Add((Attacker, Defender));
        Stat(Defender, StatType.DamageTakenAttackPercentAdjustment, 15);
        Stat(Defender, StatType.DamageTakenAttackDurationSeconds, 6);

        _effects.ApplyDamageTakenEffects(Defender, Attacker, 12);
        _log.Should().Equal(
            $"Replace {Defender} {StatType.AttackPercentAdjustment} 15 6 {StatType.DamageTakenAttackPercentAdjustment}",
            $"RecentTaken {Defender}",
            $"Entry {Defender} {Attacker}");
    }

    [Test]
    public void DamageTakenNeedsAValidLivingDefenderAndPositiveDamage()
    {
        _invalid.Add(Gone);
        _dead.Add(Other);
        Stat(Defender, StatType.DamageTakenAttackPercentAdjustment, 15);

        _effects.ApplyDamageTakenEffects(Gone, Attacker, 12);
        _effects.ApplyDamageTakenEffects(Defender, Attacker, 0);
        _effects.ApplyDamageTakenEffects(Other, Attacker, 12);
        _log.Should().BeEmpty();

        _effects.ApplyDamageTakenEffects(Defender, Attacker, 12);
        _log.Should().Equal($"RecentTaken {Defender}");
    }

    [Test]
    public void BreakerReversalArmsTheNextSkillAbilityWithItsRequiredCategoryAndCue()
    {
        Stat(Defender, StatType.DamageTakenNextSkillAbilitySkillType, (int)SkillType.Vibroblade);
        Stat(Defender, StatType.DamageTakenNextSkillAbilityDamageBonus, 8);
        Stat(Defender, StatType.DamageTakenNextSkillAbilityWindowSeconds, 6);
        Stat(Defender, StatType.DamageTakenNextSkillAbilityRequiredCategory, (int)StatusEffectCategory.Bleeding);
        _temporary[StatType.NextSkillAbilityDamageBonus] = 8;

        _effects.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("NextSkill").Should().Equal($"NextSkill {Defender} Vibroblade 8 0 6 0 {StatusEffectCategory.Bleeding}");
        Lines("Cue").Should().Equal($"Cue {Defender} True 8 {ReadyCue.Damage("Breaker Reversal", 8)}");

        _log.Clear();
        Stat(Defender, StatType.DamageTakenNextSkillAbilityWindowSeconds, 0);
        _effects.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("NextSkill").Should().BeEmpty();
    }

    [Test]
    public void ReversalCutStoresItsBonusesForTheWindow()
    {
        Stat(Defender, StatType.TwinBladeDuelistReversalCut, 1);
        Stat(Defender, StatType.TwinBladeDuelistReversalCutWindowSeconds, 4);
        _permanent[(Defender, StatType.TwinBladeDuelistReversalCutDamageBonus)] = 9;
        _permanent[(Defender, StatType.TwinBladeDuelistReversalCutDazedDurationSeconds)] = 2;

        _effects.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("Replace").Should().Equal(
            $"Replace {Defender} {StatType.TwinBladeDuelistReversalCutDamageBonus} 9 4 {StatType.TwinBladeDuelistReversalCut}",
            $"Replace {Defender} {StatType.TwinBladeDuelistReversalCutDazedDurationSeconds} 2 4 {StatType.TwinBladeDuelistReversalCut}");

        _log.Clear();
        _permanent.Clear();
        _effects.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("Replace").Should().BeEmpty();

        Stat(Defender, StatType.TwinBladeDuelistReversalCutWindowSeconds, 0);
        _permanent[(Defender, StatType.TwinBladeDuelistReversalCutDamageBonus)] = 9;
        _effects.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("Replace").Should().BeEmpty();
    }

    [Test]
    public void AHitFromARecentTargetCanReadyTheNextAbilityWithoutDelay()
    {
        Stat(Defender, StatType.DamageTakenRecentTargetNextAbilityNoDelayChance, 100);
        Stat(Defender, StatType.DamageTakenRecentTargetWindowSeconds, 6);
        Stat(Defender, StatType.DamageTakenRecentTargetNextAbilityNoDelaySkillType, (int)SkillType.Rifle);
        _recent[(Defender, Attacker)] = _now.AddSeconds(-5);

        _effects.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("NextAbilityNoDelay").Should().Equal($"NextAbilityNoDelay {Defender} Rifle 6 0");

        _log.Clear();
        _recent[(Defender, Attacker)] = _now.AddSeconds(-7);
        _effects.ApplyDamageTakenEffects(Defender, Attacker, 5);
        _recent.Clear();
        _effects.ApplyDamageTakenEffects(Defender, Attacker, 5);
        Lines("NextAbilityNoDelay").Should().BeEmpty();
    }

    [Test]
    public void TheRecentTargetRollIsOneDrawAgainstTheChance()
    {
        Stat(Defender, StatType.DamageTakenRecentTargetWindowSeconds, 6);
        Stat(Defender, StatType.DamageTakenRecentTargetNextAbilityNoDelaySkillType, (int)SkillType.Rifle);
        _recent[(Defender, Attacker)] = _now;
        var roll = Rolls(7, 2);
        Stat(Defender, StatType.DamageTakenRecentTargetNextAbilityNoDelayChance, roll[0] - 1);

        PrivateOf(_effects, "ApplyRecentDamageTargetHitEffects", Defender, Attacker);
        Lines("NextAbilityNoDelay").Should().BeEmpty();
        Random.D100(1).Should().Be(roll[1], "the recent target effect drew once");
    }

    [Test]
    public void TheRecentTargetEffectNeedsTwoDifferentValidCreaturesAndItsStats()
    {
        _invalid.Add(Gone);
        _recent[(Defender, Attacker)] = _now;
        Stat(Defender, StatType.DamageTakenRecentTargetNextAbilityNoDelayChance, 100);
        Stat(Defender, StatType.DamageTakenRecentTargetWindowSeconds, 6);

        PrivateOf(_effects, "ApplyRecentDamageTargetHitEffects", Defender, Gone);
        PrivateOf(_effects, "ApplyRecentDamageTargetHitEffects", Gone, Attacker);
        PrivateOf(_effects, "ApplyRecentDamageTargetHitEffects", Defender, Defender);
        PrivateOf(_effects, "ApplyRecentDamageTargetHitEffects", Defender, Attacker);
        _log.Should().BeEmpty("the skill selector is missing");
    }

    // ------------------------------------------------------------------ low HP

    [Test]
    public void LowHPEffectsRunInOrderWhenTheHitCrossesEachThreshold()
    {
        _players.Add(Defender);
        _hitPoints[Defender] = (30, 200);
        Stat(Defender, StatType.LowHPPhysicalDefenseThresholdPercent, 50);
        Stat(Defender, StatType.LowHPPhysicalDefensePercentAdjustment, 10);
        Stat(Defender, StatType.LowHPPhysicalDefenseDurationSeconds, 6);
        Stat(Defender, StatType.LowHPPhysicalDefenseCooldownSeconds, 60);
        Stat(Defender, StatType.LowHPEvasionThresholdPercent, 50);
        Stat(Defender, StatType.LowHPEvasionPercentAdjustment, 15);
        Stat(Defender, StatType.LowHPEvasionDurationSeconds, 7);
        Stat(Defender, StatType.LowHPNextAbilityNoStaminaCostThresholdPercent, 50);
        Stat(Defender, StatType.LowHPNextAbilityNoStaminaCostSkillType, (int)SkillType.Rifle);
        Stat(Defender, StatType.LowHPNextAbilityNoStaminaCostDurationSeconds, 8);
        Stat(Defender, StatType.LowHPTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPTemporaryHPPercent, 10);
        Stat(Defender, StatType.LowHPTemporaryHPDurationSeconds, 9);
        Stat(Defender, StatType.LowHPNoSaveTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPNoSaveTemporaryHPPercent, 5);
        Stat(Defender, StatType.LowHPNoSaveTemporaryHPDurationSeconds, 10);
        Stat(Defender, StatType.LowHPGuardThresholdPercent, 50);
        Stat(Defender, StatType.LowHPGuard, 20);
        Stat(Defender, StatType.LowHPGuardDurationSeconds, 11);

        _effects.ApplyLowHPDamageTakenEffects(Defender, 10);
        _log.Where(line => !line.StartsWith("Crossed", StringComparison.Ordinal)).Should().Equal(
            $"Trigger {Defender} {StatType.LowHPPhysicalDefensePercentAdjustment} 60",
            $"Apply {Defender}>{Defender} Unbreakable 6",
            $"Trigger {Defender} {StatType.LowHPEvasionPercentAdjustment} 0",
            $"Replace {Defender} {StatType.EvasionPercentAdjustment} 15 7 {StatType.LowHPEvasionPercentAdjustment}",
            $"Trigger {Defender} {StatType.LowHPNextAbilityNoStaminaCostSkillType} 0",
            $"Replace {Defender} {StatType.NextAbilityNoStaminaCostSkillType} {(int)SkillType.Rifle} 8 {StatType.NextAbilityNoStaminaCostSkillType}",
            $"Trigger {Defender} {StatType.LowHPTemporaryHPPercent} 0",
            $"Attribute {Defender} {StatType.LowHPTemporaryHPPercent}",
            $"Trigger {Defender} {StatType.LowHPNoSaveTemporaryHPPercent} 0",
            $"Attribute {Defender} {StatType.LowHPNoSaveTemporaryHPPercent}",
            $"Trigger {Defender} {StatType.LowHPGuard} 0",
            $"Apply {Defender}>{Defender} GuardianReflexes 11");
        Lines("Crossed").Should().HaveCount(6).And.AllBe($"Crossed {Defender} 10 50");
        FloatingText().Should().Equal($"{Defender} {ColorToken.Combat("Guardian Reflexes")}");
        Visuals().Last().Should().Be(VisualEffect.Vfx_Imp_Ac_Bonus);
    }

    [Test]
    public void LowHPEffectsStopAtTheThresholdOrTheTrigger()
    {
        Stat(Defender, StatType.LowHPPhysicalDefenseThresholdPercent, 50);
        Stat(Defender, StatType.LowHPPhysicalDefensePercentAdjustment, 10);
        Stat(Defender, StatType.LowHPPhysicalDefenseDurationSeconds, 6);
        Stat(Defender, StatType.LowHPEvasionThresholdPercent, 50);
        Stat(Defender, StatType.LowHPEvasionPercentAdjustment, 15);
        Stat(Defender, StatType.LowHPEvasionDurationSeconds, 7);
        Stat(Defender, StatType.LowHPNextAbilityNoStaminaCostThresholdPercent, 50);
        Stat(Defender, StatType.LowHPNextAbilityNoStaminaCostSkillType, (int)SkillType.Rifle);
        Stat(Defender, StatType.LowHPNextAbilityNoStaminaCostDurationSeconds, 8);
        Stat(Defender, StatType.LowHPTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPTemporaryHPPercent, 10);
        Stat(Defender, StatType.LowHPTemporaryHPDurationSeconds, 9);
        Stat(Defender, StatType.LowHPNoSaveTemporaryHPThresholdPercent, 50);
        Stat(Defender, StatType.LowHPNoSaveTemporaryHPPercent, 5);
        Stat(Defender, StatType.LowHPNoSaveTemporaryHPDurationSeconds, 10);
        Stat(Defender, StatType.LowHPGuardThresholdPercent, 50);
        Stat(Defender, StatType.LowHPGuard, 20);
        Stat(Defender, StatType.LowHPGuardDurationSeconds, 11);

        _crossed = false;
        _effects.ApplyLowHPDamageTakenEffects(Defender, 10);
        Lines("Trigger").Should().BeEmpty();

        _crossed = true;
        _triggerReady = false;
        _effects.ApplyLowHPDamageTakenEffects(Defender, 10);
        Lines("Trigger").Should().HaveCount(6);
        _log.Where(line => !line.StartsWith("Crossed", StringComparison.Ordinal) && !line.StartsWith("Trigger", StringComparison.Ordinal))
            .Should().BeEmpty();
        FloatingText().Should().BeEmpty();
    }

    [Test]
    public void LowHPEffectsNeedAValidLivingDefenderAPositiveHitAndTheirStats()
    {
        _invalid.Add(Gone);
        _dead.Add(Other);

        _effects.ApplyLowHPDamageTakenEffects(Gone, 10);
        _effects.ApplyLowHPDamageTakenEffects(Other, 10);
        _effects.ApplyLowHPDamageTakenEffects(Defender, 0);
        _effects.ApplyLowHPDamageTakenEffects(Defender, 10);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AProtectorsLowHPGuardReadsTheProtectedTargetsThresholdAndItsOwnStats()
    {
        Stat(Ally, StatType.LowHPGuardThresholdPercent, 40);
        Stat(Ally, StatType.LowHPGuard, 25);
        Stat(Ally, StatType.LowHPGuardDurationSeconds, 9);
        Stat(Ally, StatType.LowHPGuardCooldownSeconds, 30);

        _effects.ApplyLowHPGuardEffectFromProtectedTarget(Ally, Defender, 15);
        _log.Should().Equal(
            $"Crossed {Defender} 15 40",
            $"Trigger {Ally} {StatType.LowHPGuard} 30",
            $"Apply {Ally}>{Ally} GuardianReflexes 9");
        FloatingText().Should().BeEmpty("the protector is not a player");

        _log.Clear();
        _invalid.Add(Gone);
        _effects.ApplyLowHPGuardEffectFromProtectedTarget(Gone, Defender, 15);
        _effects.ApplyLowHPGuardEffectFromProtectedTarget(Ally, Gone, 15);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AnAvoidedAttackEntersCombatAndGrantsItsCuedAbilityBonuses()
    {
        Sources(Defender, StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment, new Dictionary<StatType, int>
        {
            [StatType.AvoidedAttackNextSkillAbilitySkillType] = (int)SkillType.Rifle,
            [StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment] = -3,
            [StatType.AvoidedAttackNextSkillAbilityWindowSeconds] = 6,
            [StatType.AvoidedAttackNextSkillAbilityRangedOnly] = 1,
        });
        Sources(Defender, StatType.AvoidedAttackNextSkillAbilityDamageBonus, new Dictionary<StatType, int>
        {
            [StatType.AvoidedAttackNextSkillAbilitySkillType] = (int)SkillType.Pistol,
            [StatType.AvoidedAttackNextSkillAbilityDamageBonus] = 5,
            [StatType.AvoidedAttackNextSkillAbilityWindowSeconds] = 4,
        });

        _effects.TrackAvoidedAttack(Defender, Attacker);
        _log.Should().Equal(
            $"Entry {Defender} {Attacker}",
            $"CuedStamina {Defender} Rifle -3 6 source0:{(int)StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment} True True",
            $"NextSkill {Defender} Pistol 5 0 4 0 None",
            $"NoDelay {Defender} 0");
    }

    [Test]
    public void AnAvoidedAttackRollsItsStaminaRestoreThenItsSingleRestoreInThatOrder()
    {
        Stat(Defender, StatType.AvoidedAttackStaminaRestoreChance, 100);
        Stat(Defender, StatType.AvoidedAttackStaminaRestore, 3);
        Stat(Defender, StatType.AvoidedAttackStaminaRestoreCooldownSeconds, 10);
        Stat(Defender, StatType.AvoidedAttackSingleStaminaRestoreChance, 100);
        Stat(Defender, StatType.AvoidedAttackSingleStaminaRestore, 2);
        var rolls = Rolls(5, 3);

        _effects.TrackAvoidedAttack(Defender, Attacker);
        _log.Skip(1).Take(4).Should().Equal(
            $"Trigger {Defender} {StatType.AvoidedAttackStaminaRestore} 10",
            $"Stamina {Defender} {StatType.AvoidedAttackStaminaRestore} 3",
            $"ConsumeStat {Defender} {StatType.AvoidedAttackSingleStaminaRestore}",
            $"Stamina {Defender} {StatType.AvoidedAttackSingleStaminaRestore} 2");
        Random.D100(1).Should().Be(rolls[2], "the avoided attack drew twice");
    }

    [Test]
    public void AnAvoidedAttackDrawsNothingForUnconfiguredChancesAndStopsOnAFailedRollOrTrigger()
    {
        var rolls = Rolls(9, 3);
        _effects.TrackAvoidedAttack(Defender, Attacker);
        Random.D100(1).Should().Be(rolls[0], "nothing was configured, so nothing was drawn");

        _log.Clear();
        rolls = Rolls(9, 3);
        Stat(Defender, StatType.AvoidedAttackStaminaRestoreChance, rolls[0] - 1);
        Stat(Defender, StatType.AvoidedAttackStaminaRestore, 3);
        Stat(Defender, StatType.AvoidedAttackSingleStaminaRestoreChance, rolls[1] - 1);
        Stat(Defender, StatType.AvoidedAttackSingleStaminaRestore, 2);
        _effects.TrackAvoidedAttack(Defender, Attacker);
        Lines("Stamina").Should().BeEmpty();
        Lines("Trigger").Should().BeEmpty();
        Random.D100(1).Should().Be(rolls[2]);

        _log.Clear();
        Stat(Defender, StatType.AvoidedAttackStaminaRestoreChance, 100);
        Stat(Defender, StatType.AvoidedAttackSingleStaminaRestoreChance, 0);
        _triggerReady = false;
        _effects.TrackAvoidedAttack(Defender, Attacker);
        Lines("Stamina").Should().BeEmpty();
        Lines("Trigger").Should().ContainSingle();
    }

    [Test]
    public void AnAvoidedAttackRefreshesTheRangedDeflectionItsAbilityGrantsAndItsRiders()
    {
        Stat(Defender, StatType.AvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds, 8);
        Stat(Defender, StatType.AbilityUsedRangedDeflection, 12);
        Stat(Defender, StatType.AbilityUsedRangedDeflectionStatusEffectIcon, (int)EffectIconType.ACIncrease);
        _deflectionSources[StatType.AbilityUsedRangedDeflection] = DeflectionSource.Ranged;
        _deflectionSources[StatType.AbilityGrantedAttackDeflectionFPRestore] = DeflectionSource.Ranged;
        Stat(Defender, StatType.AbilityGrantedAttackDeflectionFPRestore, 3);
        _restoredFP = 3;
        Stat(Defender, StatType.AvoidedAttackNextAutoAttackNoDelaySkillType, (int)SkillType.Pistol);
        Stat(Defender, StatType.AvoidedAttackNextAutoAttackNoDelayDurationSeconds, 5);
        Stat(Defender, StatType.AvoidedAttackAccuracyPercentAdjustment, 7);
        Stat(Defender, StatType.AvoidedAttackAccuracyDurationSeconds, 6);

        _effects.TrackAvoidedAttack(Defender, Attacker);
        _log.Should().Equal(
            $"Entry {Defender} {Attacker}",
            $"Apply {Defender}>{Defender} RangedDeflection 8",
            $"Trigger {Defender} {StatType.AbilityGrantedAttackDeflectionFPRestore} 0",
            $"Attribute {Defender} {StatType.AbilityGrantedAttackDeflectionFPRestore}",
            $"RestoreFP {Defender} 3",
            $"RestoredFP {Defender}",
            $"NoDelay {Defender} Pistol 5",
            $"Replace {Defender} {StatType.AccuracyPercentAdjustment} 7 6 {StatType.AvoidedAttackAccuracyPercentAdjustment}");
    }

    [Test]
    public void TheRangedDeflectionRefreshNeedsADeflectionAnIconAndAnAppliedStatus()
    {
        Stat(Defender, StatType.AvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds, 8);
        _effects.TrackAvoidedAttack(Defender, Attacker);
        Stat(Defender, StatType.AbilityUsedRangedDeflection, 12);
        _effects.TrackAvoidedAttack(Defender, Attacker);
        Lines("Apply").Should().BeEmpty();

        Stat(Defender, StatType.AbilityUsedRangedDeflectionStatusEffectIcon, (int)EffectIconType.ACIncrease);
        // The FP rider is ready, so only the rejected status keeps it from triggering.
        _deflectionSources[StatType.AbilityUsedRangedDeflection] = DeflectionSource.Ranged;
        _deflectionSources[StatType.AbilityGrantedAttackDeflectionFPRestore] = DeflectionSource.Ranged;
        Stat(Defender, StatType.AbilityGrantedAttackDeflectionFPRestore, 3);
        _restoredFP = 3;
        _statusApplies = false;
        _effects.TrackAvoidedAttack(Defender, Attacker);
        Lines("Apply").Should().ContainSingle();
        Lines("Trigger").Should().BeEmpty();
        Lines("RestoreFP").Should().BeEmpty();

        _log.Clear();
        _invalid.Add(Gone);
        _effects.TrackAvoidedAttack(Gone, Attacker);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ melee damage taken

    [Test]
    public void AMeleeHitRollsThePoisonBackThenTheStaminaRestoreAndEvasion()
    {
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamageChance, 100);
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamage, 6);
        Stat(Defender, StatType.MeleeDamageTakenStaminaRestoreChance, 100);
        Stat(Defender, StatType.MeleeDamageTakenStaminaRestore, 2);
        Stat(Defender, StatType.MeleeDamageTakenEvasionPercentAdjustment, 5);
        Stat(Defender, StatType.MeleeDamageTakenEvasionDurationSeconds, 4);
        var rolls = Rolls(3, 3);

        _effects.ApplyMeleeDamageTakenEffects(Defender, Attacker);
        _log.Should().Equal(
            $"Attribute {Defender} {StatType.MeleeDamageTakenPoisonDamage}",
            $"Triggered {Defender} {Attacker} 6 Poison Invalid False null False",
            $"Stamina {Defender} {StatType.MeleeDamageTakenStaminaRestore} 2",
            $"Replace {Defender} {StatType.EvasionPercentAdjustment} 5 4 {StatType.MeleeDamageTakenEvasionPercentAdjustment}");
        Visuals().Should().Equal(VisualEffect.Vfx_Imp_Poison_S);
        EffectTargets().Should().Equal(Attacker);
        Random.D100(1).Should().Be(rolls[2], "the poison and the restore drew once each");
    }

    [Test]
    public void APoisonBackScaledByAnAbilityGrowsWithTheDefendersScore()
    {
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamageChance, 100);
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamage, 6);
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamageScalingAbility, (int)AbilityType.Agility + 1);
        _triggered = _ => 0;

        _effects.ApplyMeleeDamageTakenEffects(Defender, Attacker);
        var expected = SWLOR.Game.Server.Domain.Combat.AbilityEffectScaling.ScaleDirectEffect(
            6, 10 + (int)AbilityType.Agility, source: Defender);
        Lines("Triggered").Should().Equal($"Triggered {Defender} {Attacker} {expected} Poison Invalid False null False");
        Visuals().Should().BeEmpty("nothing was dealt");
    }

    [Test]
    public void AMeleeHitStopsAtAFailedRollAndSkipsUnconfiguredRiders()
    {
        var rolls = Rolls(4, 3);
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamageChance, rolls[0] - 1);
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamage, 6);
        Stat(Defender, StatType.MeleeDamageTakenStaminaRestoreChance, rolls[1] - 1);
        Stat(Defender, StatType.MeleeDamageTakenStaminaRestore, 2);

        _effects.ApplyMeleeDamageTakenEffects(Defender, Attacker);
        _log.Should().BeEmpty();
        Random.D100(1).Should().Be(rolls[2]);

        Stat(Defender, StatType.MeleeDamageTakenStaminaRestoreChance, 100);
        Stat(Defender, StatType.MeleeDamageTakenStaminaRestore, 0);
        Stat(Defender, StatType.MeleeDamageTakenPoisonDamage, 0);
        _effects.ApplyMeleeDamageTakenEffects(Defender, Attacker);
        _log.Should().BeEmpty();

        _invalid.Add(Gone);
        _effects.ApplyMeleeDamageTakenEffects(Gone, Attacker);
        _effects.ApplyMeleeDamageTakenEffects(Defender, Gone);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ deflection

    [Test]
    public void ADeflectionIsRecordedAndCanGuardTheNearestAlly()
    {
        _deflectionSources[StatType.DeflectionNearbyAllyGuard] = DeflectionSource.Melee;
        Stat(Defender, StatType.DeflectionNearbyAllyGuard, 15);
        Stat(Defender, StatType.DeflectionNearbyAllyGuardDurationSeconds, 6);
        _hitPoints[Ally] = (10, 10);
        _hitPoints[Other] = (10, 10);
        _hitPoints[Defender] = (10, 10);
        _party.UnionWith(new[] { Ally, Other });
        _distances[Ally] = 4f;
        _distances[Other] = 2f;
        _inShape.AddRange(new[] { Defender, Ally, Other });

        _effects.TrackDeflection(Defender, DeflectionSource.Melee);
        Deflections()[(Defender, DeflectionSource.Melee)].Should().Be(_now);
        _log.Should().Equal($"Replace {Other} {StatType.Guard} 15 6 {StatType.DeflectionNearbyAllyGuard}");
    }

    [Test]
    public void TheDeflectionGuardNeedsItsSourceItsStatsAndAnAlly()
    {
        _deflectionSources[StatType.DeflectionNearbyAllyGuard] = DeflectionSource.Melee;
        _effects.TrackDeflection(Defender, DeflectionSource.Ranged);
        _effects.TrackDeflection(Defender, DeflectionSource.Melee);
        Stat(Defender, StatType.DeflectionNearbyAllyGuard, 15);
        Stat(Defender, StatType.DeflectionNearbyAllyGuardDurationSeconds, 6);
        _effects.TrackDeflection(Defender, DeflectionSource.Melee);
        _log.Should().BeEmpty();

        _invalid.Add(Gone);
        _effects.TrackDeflection(Gone, DeflectionSource.Melee);
        _effects.TrackDeflection(Defender, DeflectionSource.None);
        Deflections().Keys.Should().BeEquivalentTo(new[] { (Defender, DeflectionSource.Ranged), (Defender, DeflectionSource.Melee) });
    }

    [Test]
    public void AnAbilityGrantedDeflectionRestoresFPOnceItsTriggerIsReady()
    {
        _deflectionSources[StatType.AbilityGrantedAttackDeflectionFPRestore] = DeflectionSource.Melee;
        Stat(Attacker, StatType.AbilityGrantedAttackDeflectionFPRestore, 4);
        Stat(Attacker, StatType.AbilityGrantedAttackDeflectionFPRestoreCooldownSeconds, 9);

        _effects.ApplyAbilityGrantedAttackDeflectionEffects(Attacker, DeflectionSource.Ranged);
        _log.Should().BeEmpty();
        _effects.ApplyAbilityGrantedAttackDeflectionEffects(Attacker, DeflectionSource.Melee);
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.AbilityGrantedAttackDeflectionFPRestore} 9",
            $"Attribute {Attacker} {StatType.AbilityGrantedAttackDeflectionFPRestore}",
            $"RestoreFP {Attacker} 4");

        _log.Clear();
        _triggerReady = false;
        _effects.ApplyAbilityGrantedAttackDeflectionEffects(Attacker, DeflectionSource.Melee);
        _log.Should().Equal($"Trigger {Attacker} {StatType.AbilityGrantedAttackDeflectionFPRestore} 9");

        _log.Clear();
        Stat(Attacker, StatType.AbilityGrantedAttackDeflectionFPRestore, 0);
        _effects.ApplyAbilityGrantedAttackDeflectionEffects(Attacker, DeflectionSource.Melee);
        _log.Should().BeEmpty();
    }

    [TestCase(DeflectionSource.Melee, StatType.MeleeDeflectionFPRestore)]
    [TestCase(DeflectionSource.Ranged, StatType.DeflectionFPRestore)]
    public void AnAbilityUsedDeflectionGrantsItsDeflectionAndItsFPRestoreOfTheSameSource(DeflectionSource source, StatType restoreTarget)
    {
        Stat(Attacker, StatType.AbilityUsedAttackDeflection, 12);
        Stat(Attacker, StatType.AbilityUsedAttackDeflectionDurationSeconds, 6);
        Stat(Attacker, StatType.AbilityUsedAttackDeflectionFPRestore, 2);
        _deflectionSources[StatType.AbilityUsedAttackDeflection] = source;
        _deflectionSources[StatType.AbilityUsedAttackDeflectionFPRestore] = source;
        _grantedDeflections[StatType.AbilityUsedAttackDeflection] = StatType.RangedDeflection;

        _effects.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds,
            StatType.AbilityUsedAttackDeflectionFPRestore);
        _log.Take(2).Should().Equal(
            $"Replace {Attacker} {StatType.RangedDeflection} 12 6 {StatType.AbilityUsedAttackDeflection}",
            $"Replace {Attacker} {restoreTarget} 2 6 {StatType.AbilityUsedAttackDeflectionFPRestore}");
        _log.Should().HaveCount(2, "the granted deflection FP restore is not configured");
    }

    [Test]
    public void AnAbilityUsedDeflectionSkipsAnFPRestoreOfAnotherOrUnknownSource()
    {
        Stat(Attacker, StatType.AbilityUsedAttackDeflection, 12);
        Stat(Attacker, StatType.AbilityUsedAttackDeflectionDurationSeconds, 6);
        Stat(Attacker, StatType.AbilityUsedAttackDeflectionFPRestore, 2);
        _deflectionSources[StatType.AbilityUsedAttackDeflection] = DeflectionSource.None;
        _deflectionSources[StatType.AbilityUsedAttackDeflectionFPRestore] = DeflectionSource.None;
        _grantedDeflections[StatType.AbilityUsedAttackDeflection] = StatType.RangedDeflection;

        _effects.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds,
            StatType.AbilityUsedAttackDeflectionFPRestore);
        _deflectionSources[StatType.AbilityUsedAttackDeflectionFPRestore] = DeflectionSource.Melee;
        _effects.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds,
            StatType.AbilityUsedAttackDeflectionFPRestore);
        _effects.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds);
        Lines("Replace").Should().HaveCount(3).And.AllBe($"Replace {Attacker} {StatType.RangedDeflection} 12 6 {StatType.AbilityUsedAttackDeflection}");
    }

    [Test]
    public void AnAbilityUsedDeflectionNeedsItsStatsAndAGrantedStat()
    {
        _effects.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds);
        Stat(Attacker, StatType.AbilityUsedAttackDeflection, 12);
        _effects.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds);
        Stat(Attacker, StatType.AbilityUsedAttackDeflectionDurationSeconds, 6);
        _effects.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ARangedAbilityUsedDeflectionAlsoRunsTheGrantedDeflectionRiders()
    {
        Stat(Attacker, StatType.AbilityUsedAttackDeflection, 12);
        Stat(Attacker, StatType.AbilityUsedAttackDeflectionDurationSeconds, 6);
        _deflectionSources[StatType.AbilityUsedAttackDeflection] = DeflectionSource.Ranged;
        _deflectionSources[StatType.AbilityGrantedAttackDeflectionFPRestore] = DeflectionSource.Ranged;
        _grantedDeflections[StatType.AbilityUsedAttackDeflection] = StatType.RangedDeflection;
        Stat(Attacker, StatType.AbilityGrantedAttackDeflectionFPRestore, 3);

        _effects.ApplyAbilityUsedAttackDeflection(Attacker, StatType.AbilityUsedAttackDeflection, StatType.AbilityUsedAttackDeflectionDurationSeconds);
        _log.Should().Equal(
            $"Replace {Attacker} {StatType.RangedDeflection} 12 6 {StatType.AbilityUsedAttackDeflection}",
            $"Trigger {Attacker} {StatType.AbilityGrantedAttackDeflectionFPRestore} 0",
            $"Attribute {Attacker} {StatType.AbilityGrantedAttackDeflectionFPRestore}",
            $"RestoreFP {Attacker} 3");
    }
}
