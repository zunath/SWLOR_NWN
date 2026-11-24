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
/// The hit effects stay-part of <c>Combat</c>: the riders of an auto-attack, of a critical and of a non-critical ability, the effects of
/// defeating an enemy, and the order in which a direct hit runs the steps of the dealt effects part.
/// </summary>
[TestFixture]
public class CombatHitEffectsServiceTests : CombatHitEffectsTestBase
{
    // ------------------------------------------------------------------ auto-attack damage

    [Test]
    public void ANonPositiveAutoAttackKeepsItsDamageAndTouchesNothing()
    {
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 0).Should().Be(0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, -3, SkillType.Rifle).Should().Be(-3);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AnAutoAttackAddsItsStoredProcAndNextAttackBonusesAndTheDirectDamageBonus()
    {
        _temporary[StatType.CurrentAutoAttackDamageBonus] = 5;
        _temporary[StatType.NextSkillAutoAttackDamageBonusSkillType] = (int)SkillType.Rifle;
        _temporary[StatType.NextSkillAutoAttackDamageBonus] = 4;
        _temporary[StatType.NextAutoAttackDamageBonus] = 3;
        _directBonus = 2;
        Sources(Attacker, StatType.AutoAttackDamageBonus,
            new Dictionary<StatType, int> { [StatType.AutoAttackDamageBonus] = 7, [StatType.AutoAttackDamageBonusChance] = 100 },
            new Dictionary<StatType, int> { [StatType.AutoAttackDamageBonus] = 9, [StatType.AutoAttackDamageBonusChance] = 0 });

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle).Should().Be(10 + 5 + 7 + 4 + 3 + 2);
        Lines("Consume").Should().Equal(
            $"Consume {Attacker} {StatType.CurrentAutoAttackDamageBonus} {StatType.CurrentAutoAttackDamageBonus}",
            $"Consume {Attacker} {StatType.NextSkillAutoAttackDamageBonus} {StatType.NextSkillAutoAttackDamageBonusSkillType}",
            $"Consume {Attacker} {StatType.NextSkillAutoAttackDamageBonusSkillType} {StatType.NextSkillAutoAttackDamageBonusSkillType}",
            $"Consume {Attacker} {StatType.NextAutoAttackDamageBonus} {StatType.NextAutoAttackDamageBonus}");
    }

    [Test]
    public void TheNextSkillAutoAttackBonusNeedsItsSkill()
    {
        _temporary[StatType.NextSkillAutoAttackDamageBonusSkillType] = (int)SkillType.Pistol;
        _temporary[StatType.NextSkillAutoAttackDamageBonus] = 4;

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle).Should().Be(10, "another skill's bonus");
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10).Should().Be(10, "no skill");
        Lines("Consume").Should().NotContain(line => line.Contains(StatType.NextSkillAutoAttackDamageBonus + " "));
    }

    [Test]
    public void TheSuppressedAutoAttackConsumesTheBonusesItWouldHaveAdded()
    {
        _temporary[StatType.NextSkillAutoAttackDamageBonusSkillType] = (int)SkillType.Rifle;
        _service.ConsumeSuppressedAutoAttackDamageBonuses(Attacker, SkillType.Rifle);

        Lines("Consume").Should().Equal(
            $"Consume {Attacker} {StatType.CurrentAutoAttackDamageBonus} {StatType.CurrentAutoAttackDamageBonus}",
            $"Consume {Attacker} {StatType.NextSkillAutoAttackDamageBonus} {StatType.NextSkillAutoAttackDamageBonusSkillType}",
            $"Consume {Attacker} {StatType.NextSkillAutoAttackDamageBonusSkillType} {StatType.NextSkillAutoAttackDamageBonusSkillType}",
            $"Consume {Attacker} {StatType.NextAutoAttackDamageBonus} {StatType.NextAutoAttackDamageBonus}");
    }

    [Test]
    public void AnAutoAttackRestoresStaminaAndFPInTheirOrder()
    {
        Stat(Attacker, StatType.AutoAttackHitStaminaRestore, 1);
        Stat(Attacker, StatType.AutoAttackStaminaRestoreChance, 100);
        Stat(Attacker, StatType.AutoAttackStaminaRestore, 2);
        Stat(Attacker, StatType.AutoAttackFPRestore, 3);
        Stat(Attacker, StatType.AutoAttackFPRestoreCooldownSeconds, 6);
        Stat(Attacker, StatType.SkillAutoAttackFPRestore, 4);
        Stat(Attacker, StatType.SkillAutoAttackFPRestoreSkillType, (int)SkillType.Rifle);

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
        _log.Where(line => line.StartsWith("HitStamina") || line.StartsWith("FP") || line.StartsWith("Trigger")).Should().Equal(
            $"HitStamina {Attacker} {StatType.AutoAttackHitStaminaRestore} 1",
            $"HitStamina {Attacker} {StatType.AutoAttackStaminaRestore} 2",
            $"Trigger {Attacker} {StatType.AutoAttackFPRestore} 6",
            $"FP {Attacker} {StatType.AutoAttackFPRestore} 3",
            $"FP {Attacker} {StatType.SkillAutoAttackFPRestore} 4");

        _log.Clear();
        _triggerReady = false;
        Stat(Attacker, StatType.AutoAttackStaminaRestore, 0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Pistol);
        _log.Where(line => line.StartsWith("HitStamina") || line.StartsWith("FP")).Should().Equal(
            $"HitStamina {Attacker} {StatType.AutoAttackHitStaminaRestore} 1");
    }

    [Test]
    public void AnAutoAttackRestoresItsMastersStaminaAndFPOnTheirChances()
    {
        Stat(Attacker, StatType.AutoAttackMasterStaminaRestoreChance, 100);
        Stat(Attacker, StatType.AutoAttackMasterStaminaRestore, 2);
        Stat(Attacker, StatType.AutoAttackMasterFPRestoreChance, 100);
        Stat(Attacker, StatType.AutoAttackMasterFPRestore, 3);

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        _log.Should().NotContain(line => line.StartsWith("Stamina") || line.StartsWith("FP"), "no master");

        _master = Master;
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        _log.Where(line => line.StartsWith("Stamina") || line.StartsWith("FP")).Should().Equal(
            $"Stamina {Master} {StatType.AutoAttackMasterStaminaRestore} 2",
            $"FP {Master} {StatType.AutoAttackMasterFPRestore} 3");

        _log.Clear();
        Stat(Attacker, StatType.AutoAttackMasterStaminaRestore, 0);
        Stat(Attacker, StatType.AutoAttackMasterFPRestoreChance, 0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        _log.Should().NotContain(line => line.StartsWith("Stamina") || line.StartsWith("FP"));
    }

    [Test]
    public void TheAutoAttackChancesDrawInTheirOrderAndOnlyWhenConfigured()
    {
        // Proc bonus, stamina, the master's stamina and FP, then the accuracy penalty: one roll each, in this order.
        Sources(Attacker, StatType.AutoAttackDamageBonus, new Dictionary<StatType, int> { [StatType.AutoAttackDamageBonus] = 7, [StatType.AutoAttackDamageBonusChance] = 50 });
        Stat(Attacker, StatType.AutoAttackStaminaRestoreChance, 50);
        Stat(Attacker, StatType.AutoAttackStaminaRestore, 2);
        _master = Master;
        Stat(Attacker, StatType.AutoAttackMasterStaminaRestoreChance, 50);
        Stat(Attacker, StatType.AutoAttackMasterStaminaRestore, 2);
        Stat(Attacker, StatType.AutoAttackMasterFPRestoreChance, 50);
        Stat(Attacker, StatType.AutoAttackMasterFPRestore, 3);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentChance, 50);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustment, -10);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 6);

        for (var seed = 1; seed <= 20; seed++)
        {
            _log.Clear();
            var rolls = Rolls(seed, 6);
            var damage = _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
            Random.D100(1).Should().Be(rolls[5], "five chances drew five rolls");
            damage.Should().Be(rolls[0] <= 50 ? 17 : 10);
            _log.Contains($"HitStamina {Attacker} {StatType.AutoAttackStaminaRestore} 2").Should().Be(rolls[1] <= 50);
            _log.Contains($"Stamina {Master} {StatType.AutoAttackMasterStaminaRestore} 2").Should().Be(rolls[2] <= 50);
            _log.Contains($"FP {Master} {StatType.AutoAttackMasterFPRestore} 3").Should().Be(rolls[3] <= 50);
            _log.Contains($"Replace {Defender} {StatType.AccuracyPercentAdjustment} -10 6 {StatType.AutoAttackTargetAccuracyPercentAdjustment}")
                .Should().Be(rolls[4] <= 50);
        }

        _stats.Clear();
        _sources.Clear();
        var none = Rolls(2, 1);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
        Random.D100(1).Should().Be(none[0], "no configured chance draws nothing");
    }

    [Test]
    public void AnAutoAttackCanLowerTheTargetsAccuracyOnlyWithEveryValue()
    {
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentChance, 100);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustment, -10);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 6);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("Replace").Should().Equal($"Replace {Defender} {StatType.AccuracyPercentAdjustment} -10 6 {StatType.AutoAttackTargetAccuracyPercentAdjustment}");

        _log.Clear();
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 6);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustment, 0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("Replace").Should().BeEmpty();
    }

    [Test]
    public void AnAutoAttackHamstringsAValidTargetOfTheConfiguredSkill()
    {
        Stat(Attacker, StatType.AutoAttackHamstringDurationSeconds, 4);
        Stat(Attacker, StatType.AutoAttackHamstringSkillType, (int)SkillType.Rifle);

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(HamstringStatusEffect)} 4 {CombatDamageType.Physical}");

        _log.Clear();
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Pistol);
        _invalid.Add(Gone);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Gone, 10, SkillType.Rifle);
        Stat(Attacker, StatType.AutoAttackHamstringDurationSeconds, 0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void AnAutoAttackOnASunderedTargetRestoresFP()
    {
        Stat(Attacker, StatType.AutoAttackSunderedTargetFPRestore, 3);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("FP").Should().BeEmpty("the target is not sundered");

        _statuses.Add((Defender, typeof(SunderStatusEffect)));
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("FP").Should().Equal($"FP {Attacker} {StatType.AutoAttackSunderedTargetFPRestore} 3");

        _log.Clear();
        _invalid.Add(Defender);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("FP").Should().BeEmpty();
    }

    [Test]
    public void TheSourceStatusStackInfectsThenAddsAStackAndRefreshes()
    {
        Stat(Attacker, StatType.SourceStatusStackRequiredCategory, (int)StatusEffectCategory.Venom);
        Stat(Attacker, StatType.SourceStatusStackAppliedCategory, (int)StatusEffectCategory.Infection);
        Stat(Attacker, StatType.SourceStatusStackMaximum, 3);
        Stat(Attacker, StatType.SourceStatusStackDurationSeconds, 12);
        _sourceApplied = true;

        _service.ApplySourceStatusStackEffects(Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(InfectionStatusEffect)} stacks 1 12 {CombatDamageType.Poison}");

        _log.Clear();
        var existing = new InfectionStatusEffect(1);
        _effects[(Defender, typeof(InfectionStatusEffect), Attacker)] = existing;
        _service.ApplySourceStatusStackEffects(Attacker, Defender);
        existing.Stacks.Should().Be(2);
        Lines("Refresh").Should().Equal($"Refresh {Defender} {nameof(InfectionStatusEffect)} {Attacker} 12 {CombatDamageType.Poison}");
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void TheSourceStatusStackNeedsEveryValueAndAnInfectionCategory()
    {
        _sourceApplied = true;
        Stat(Attacker, StatType.SourceStatusStackRequiredCategory, (int)StatusEffectCategory.Venom);
        Stat(Attacker, StatType.SourceStatusStackAppliedCategory, (int)StatusEffectCategory.Bleeding);
        Stat(Attacker, StatType.SourceStatusStackMaximum, 3);
        Stat(Attacker, StatType.SourceStatusStackDurationSeconds, 12);
        _service.ApplySourceStatusStackEffects(Attacker, Defender);
        _log.Should().BeEmpty("only Infection stacks");

        Stat(Attacker, StatType.SourceStatusStackAppliedCategory, (int)StatusEffectCategory.Infection);
        _sourceApplied = false;
        _service.ApplySourceStatusStackEffects(Attacker, Defender);
        _sourceApplied = true;
        Stat(Attacker, StatType.SourceStatusStackDurationSeconds, 0);
        _service.ApplySourceStatusStackEffects(Attacker, Defender);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ auto-attack cycles

    [Test]
    public void TheCycleWithoutARadiusAddsItsDamageOnTheAttackThatCompletesIt()
    {
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 3);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 12);
        Stat(Attacker, StatType.AutoAttackCycleDamageSkillType, (int)SkillType.Vibroblade);

        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(12);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0, "the cycle starts again");
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Rifle).Should().Be(0, "another skill does not advance it");

        _invalid.Add(Gone);
        _service.ConsumeAutoAttackCycleDamageBonus(Gone, SkillType.Vibroblade).Should().Be(0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Invalid).Should().Be(0);

        Stat(Attacker, StatType.AutoAttackCycleRadiusMeters, 5);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0, "a radius cycle hits a nearby enemy instead");
        Stat(Attacker, StatType.AutoAttackCycleRadiusMeters, 0);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 12);
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade).Should().Be(0);
    }

    [Test]
    public void TheMeleeCycleAddsItsDamageForAnyWeaponSkillAlongsideTheSkillCycle()
    {
        Stat(Attacker, StatType.MeleeAutoAttackCycleRequiredCount, 2);
        Stat(Attacker, StatType.MeleeAutoAttackCycleDamage, 5);
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 2);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 7);

        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(12);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Force).Should().Be(0, "Force is not a weapon skill and opens a new skill cycle");
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(7, "the skill cycle completes; the melee cycle restarted");

        Stat(Attacker, StatType.AutoAttackCycleRadiusMeters, 3);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(5, "only the melee cycle when the skill cycle has a radius");
        Stat(Attacker, StatType.MeleeAutoAttackCycleDamage, 0);
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar).Should().Be(0);
    }

    [Test]
    public void TheRadiusCycleHitsTheNearestOtherEnemyWithTriggeredDamage()
    {
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 2);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 9);
        Stat(Attacker, StatType.AutoAttackCycleRadiusMeters, 4);
        _nearest = Other;
        _queued = true;

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        Lines("Nearest").Should().BeEmpty("the first attack only counts");
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        _log.Should().ContainInOrder(
            $"Nearest {Attacker} {Defender} 4 {Defender}",
            $"Attribute {Attacker} {StatType.AutoAttackCycleDamage}",
            $"TypeDealt {Attacker} 9 {CombatDamageType.Physical}",
            $"Queue {Attacker} {Other} 9 {CombatDamageType.Physical.GetNWScriptDamageType()}");

        _log.Clear();
        _nearest = NWScript.OBJECT_INVALID;
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        Lines("Nearest").Should().ContainSingle();
        Lines("Queue").Should().BeEmpty("no enemy nearby");

        _log.Clear();
        _invalid.Add(Gone);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Gone, 10, SkillType.Vibroblade);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Stat(Attacker, StatType.AutoAttackCycleDamageSkillType, (int)SkillType.Rifle);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Vibroblade);
        Lines("Nearest").Should().BeEmpty();
    }

    [Test]
    public void TheSourceStatusCycleHitsTheAfflictedTargetAndResetsWhenTheStatusIsGone()
    {
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleRequiredCategory, (int)StatusEffectCategory.Bleeding);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleRequiredCount, 2);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamage, 6);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamageType, (int)CombatDamageType.Fire);
        _sourceApplied = true;
        _queued = true;

        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _sourceApplied = false;
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _sourceApplied = true;
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        Lines("Queue").Should().BeEmpty("losing the status reset the count");
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _log.Should().ContainInOrder(
            $"Attribute {Attacker} {StatType.SourceStatusAutoAttackCycleDamage}",
            $"TypeDealt {Attacker} 6 {CombatDamageType.Fire}",
            $"Queue {Attacker} {Defender} 6 {CombatDamageType.Fire.GetNWScriptDamageType()}");

        _log.Clear();
        _invalid.Add(Gone);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Gone, 10, SkillType.Katar);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamageType, 0);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        Lines("Queue").Should().BeEmpty();
    }

    // ------------------------------------------------------------------ landed damage


    [Test]
    public void TriggeredAndPeriodicDamageOnlyTrackActivity()
    {
        _directTrait = true;
        Hit(10, delivery: CombatDamageDeliveryType.Triggered);
        Hit(10, delivery: CombatDamageDeliveryType.DamageOverTime);
        _log.Should().Equal($"Activity {Attacker}", $"Recent {Attacker} {Defender}", $"Activity {Attacker}", $"Recent {Attacker} {Defender}");
    }

    [Test]
    public void ADirectHitRunsItsRidersInOrder()
    {
        _stats[(Attacker, StatType.DamageDealtHPPercentRestore)] = 5;
        _stats[(Attacker, StatType.PhysicalDamageDealtHPPercentRestore)] = 6;

        Hit(10, SkillType.Rifle);
        _log.Should().ContainInOrder(
            $"Beast {Attacker} {Defender}",
            $"Activity {Attacker}",
            $"Recent {Attacker} {Defender}",
            $"FirstAttack {Attacker}",
            $"Pressure {Attacker} {Defender} {SkillType.Rifle}",
            $"AutoSuppression {Attacker} {Defender} {SkillType.Rifle} {CombatDamageType.Physical}",
            $"Consume {Attacker} {StatType.NextDamageDealtBleedDurationSeconds} {StatType.NextDamageDealtBleedDurationSeconds}",
            $"RangedSuppression {Attacker} {Defender} {SkillType.Rifle} {CombatDamageType.Physical}",
            $"Heal {Attacker} 10 5 False 0",
            $"Heal {Attacker} 10 6 False 0");

        _log.Clear();
        Hit(10, SkillType.Force, CombatDamageType.Force, ability: true);
        Lines("AutoSuppression").Should().BeEmpty("an ability does not stack auto-attack Suppression");
        Lines("Heal").Should().Equal($"Heal {Attacker} 10 5 False 0");
    }

    [Test]
    public void AnAbilityImpactAlreadyDamagedThisTargetRunsNoSecondSetOfRiders()
    {
        var weapon = new WeaponShotProfile(501, SkillType.Pistol, 10, CombatDamageType.Physical, 90, 90, AbilityType.Perception, "1");
        var activation = new WeaponImpactActivation();
        using var profile = new WeaponImpactProfile(Attacker, weapon, activation);

        Hit(10, SkillType.Pistol, ability: true);
        Lines("FirstAttack").Should().ContainSingle();
        _log.Clear();
        Hit(10, SkillType.Pistol, ability: true);
        _log.Should().Equal($"Beast {Attacker} {Defender}", $"Activity {Attacker}", $"Recent {Attacker} {Defender}");
        _log.Clear();
        Hit(10, SkillType.Pistol);
        Lines("FirstAttack").Should().ContainSingle("an auto-attack is not limited by the ability's impact");
    }


    [Test]
    public void AnAutoAttackSplashHitsTheTargetAndItsNeighboursOnTheNextFrame()
    {
        Sources(Attacker, StatType.AutoAttackSplashDamage,
            new Dictionary<StatType, int>
            {
                [StatType.AutoAttackSplashDamage] = 8, [StatType.AutoAttackSplashChance] = 100,
                [StatType.AutoAttackSplashRadiusMeters] = 4, [StatType.AutoAttackSplashMaximumTargets] = 3
            },
            new Dictionary<StatType, int> { [StatType.AutoAttackSplashDamage] = 8, [StatType.AutoAttackSplashChance] = 0 });
        _nearby.Add(Other);
        _nearby.Add(Ally);
        _queued = true;

        Hit(10, SkillType.Vibroblade);
        Lines("Queue").Should().BeEmpty("the splash waits a frame");
        _engine.CallsTo("DelayCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(0f);

        _log.Clear();
        _dead.Add(Ally);
        RunDelayed();
        _log.Should().ContainInOrder(
            $"Attribute {Attacker} {StatType.AutoAttackSplashDamage}",
            $"Queue {Attacker} {Defender} 8 {CombatDamageType.Physical.GetNWScriptDamageType()}",
            $"Queue {Attacker} {Other} 8 {CombatDamageType.Physical.GetNWScriptDamageType()}");
        Lines("Queue").Should().HaveCount(2, "a dead neighbour takes no damage");
        _engine.CallsTo("ApplyEffectAtLocation").Should().ContainSingle();
    }

    [Test]
    public void ASplashSkipsADeadTargetAGoneAttackerAFriendAndAnAbility()
    {
        Sources(Attacker, StatType.AutoAttackSplashDamage, new Dictionary<StatType, int>
        {
            [StatType.AutoAttackSplashDamage] = 8, [StatType.AutoAttackSplashChance] = 100,
            [StatType.AutoAttackSplashRadiusMeters] = 4, [StatType.AutoAttackSplashMaximumTargets] = 2
        });
        _queued = true;

        Hit(10, SkillType.Vibroblade);
        _dead.Add(Defender);
        _engine.ClearCalls();
        Hit(10, SkillType.Vibroblade);
        RunDelayed();
        Lines("Queue").Should().BeEmpty("the target died before the splash");

        _engine.ClearCalls();
        _dead.Clear();
        Hit(10, SkillType.Vibroblade);
        _invalid.Add(Attacker);
        RunDelayed();
        Lines("Queue").Should().BeEmpty("the attacker is gone");
        _engine.CallsTo("ApplyEffectAtLocation").Should().BeEmpty();

        _invalid.Clear();
        _engine.ClearCalls();
        _hostile = false;
        Hit(10, SkillType.Vibroblade);
        _hostile = true;
        Hit(10, SkillType.Vibroblade, ability: true);
        Hit(10, SkillType.Force);
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
        _invalid.Add(Defender);
        Hit(10, SkillType.Vibroblade);
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
    }














    [Test]
    public void FrenzySlashRefreshesTheHasteItAlreadyHas()
    {
        Stat(Attacker, StatType.FrenzySlashHasteRefreshDurationSeconds, 6);
        Hit(10);
        Lines("RefreshModifier").Should().Equal($"RefreshModifier {Attacker} {StatType.AttackDelayReductionPercent} 6 {StatType.AttackDelayReductionPercent}");
    }

    // ------------------------------------------------------------------ criticals


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

        _service.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle);
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
        _service.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle);
        Lines("Remove").Should().ContainSingle();
        Lines("Apply").Should().BeEmpty("no tracker icon");

        _log.Clear();
        _temporary.Remove(StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment);
        _service.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle);
        Lines("Remove").Should().BeEmpty("nothing stored");
    }

    [Test]
    public void ANonCriticalAbilityNeedsItsSkillAndEveryValue()
    {
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment, 5);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateMax, 20);
        Stat(Attacker, StatType.NonCriticalAbilityNextSkillAbilityCriticalRateWindowSeconds, 0);

        _service.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Rifle);
        _service.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Pistol);
        _service.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Invalid);
        _invalid.Add(Gone);
        _service.ApplyNonCriticalAbilityEffects(Gone, Defender, SkillType.Rifle);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ defeated enemies

    [Test]
    public void DefeatingAnEnemyRestoresHealsAndBuffsInOrder()
    {
        Stat(Attacker, StatType.DefeatedEnemyStaminaRestore, 2);
        Stat(Attacker, StatType.DefeatedEnemyFPRestore, 3);
        Stat(Attacker, StatType.DefeatedEnemyHPPercentRestore, 5);
        Stat(Attacker, StatType.DefeatedEnemyAttackPercentAdjustment, 10);
        Stat(Attacker, StatType.DefeatedEnemyAttackDurationSeconds, 6);
        Stat(Attacker, StatType.DefeatedEnemyAttackDelayReductionPercent, 12);
        Stat(Attacker, StatType.DefeatedEnemyAttackDelayReductionDurationSeconds, 7);
        Stat(Attacker, StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment, 8);
        Stat(Attacker, StatType.DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds, 9);
        _party.AddRange(new[] { Attacker, Ally });

        _service.ApplyDefeatedEnemyEffects(Attacker);
        _log.Should().Equal(
            $"Attribute {Attacker} {CombatRecordSourceKind.OnKill}",
            $"RestoreStamina {Attacker} 2",
            $"RestoreFP {Attacker} 3",
            $"HealMax {Attacker} 5",
            $"Replace {Attacker} {StatType.AttackPercentAdjustment} 10 6 {StatType.DefeatedEnemyAttackPercentAdjustment}",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 12 7 {StatType.DefeatedEnemyAttackDelayReductionPercent}",
            $"Party {Attacker} 5",
            $"Replace {Ally} {StatType.PhysicalDefensePercentAdjustment} 8 9 {StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment}");

        _log.Clear();
        _dead.Add(Attacker);
        _service.ApplyDefeatedEnemyEffects(Attacker);
        _invalid.Add(Gone);
        _service.ApplyDefeatedEnemyEffects(Gone);
        _log.Should().BeEmpty();
        _dead.Clear();
        _stats.Clear();
        _service.ApplyDefeatedEnemyEffects(Attacker);
        _log.Should().Equal($"Attribute {Attacker} {CombatRecordSourceKind.OnKill}");
    }

    [Test]
    public void SoulAscensionFollowsAKillInsideTheHitPointSpendWindow()
    {
        Stat(Attacker, StatType.HeavyVibrobladeOffenseSoulAscension, 1);
        _service.ApplyDefeatedEnemyEffects(Attacker);
        Lines("Apply").Should().BeEmpty("no hit points were spent");
        _temporary[StatType.HeavyVibrobladeOffenseSoulAscension] = 1;
        _service.ApplyDefeatedEnemyEffects(Attacker);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Attacker} {nameof(SoulAscensionStatusEffect)} 30");
        _log.Clear();
        Stat(Attacker, StatType.HeavyVibrobladeOffenseSoulAscension, 0);
        _service.ApplyDefeatedEnemyEffects(Attacker);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void ADefeatedEnemysPoisonSpreadsFromItsSourceToTheNearestEnemy()
    {
        var effects = new CreatureStatusEffect { StatGroup = new StatGroup() };
        var poison = new PoisonStatusEffect();
        typeof(StatusEffectBase).GetProperty(nameof(StatusEffectBase.Source))!.SetValue(poison, Ally);
        effects.Add(new BleedStatusEffect());
        effects.Add(poison);
        _creatureEffects[Defender] = effects;
        Stat(Ally, StatType.PoisonedDefeatedEnemySpreadRadiusMeters, 6);
        Stat(Ally, StatType.PoisonedDefeatedEnemySpreadDurationSeconds, 12);
        _nearest = Other;

        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        _log.Should().ContainInOrder(
            $"Nearest {Ally} {Defender} 6 {Defender}",
            $"Apply {Ally}>{Other} {nameof(PoisonStatusEffect)} 12 {CombatDamageType.Physical}");

        _log.Clear();
        _invalid.Add(Ally);
        Stat(Attacker, StatType.PoisonedDefeatedEnemySpreadRadiusMeters, 6);
        Stat(Attacker, StatType.PoisonedDefeatedEnemySpreadDurationSeconds, 12);
        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Other} {nameof(PoisonStatusEffect)} 12 {CombatDamageType.Physical}");

        _log.Clear();
        _nearest = NWScript.OBJECT_INVALID;
        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        Stat(Attacker, StatType.PoisonedDefeatedEnemySpreadDurationSeconds, 0);
        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        _creatureEffects.Clear();
        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void ADefeatedBleedingEnemysBleedSpreadsToNearbyEnemies()
    {
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Stat(Attacker, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds, 8);
        _nearby.Add(Other);

        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Other} {nameof(BleedStatusEffect)} 8 {CombatDamageType.Physical}");

        _log.Clear();
        Stat(Attacker, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds, 0);
        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        _categories.Clear();
        Stat(Attacker, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds, 8);
        _service.ApplyDefeatedEnemyEffects(Attacker, Defender);
        _invalid.Add(Gone);
        Private("ApplyDefeatedBleedingEnemySpread", Gone, Defender);
        Private("ApplyDefeatedBleedingEnemySpread", Attacker, Gone);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void EveryRecentDamagerOfTheDefeatedGainsCruelMomentumAndStaleRecordsArePruned()
    {
        Stat(Attacker, StatType.CruelMomentum, 1);
        Stat(Other, StatType.CruelMomentum, 1);
        _recent[(Attacker, Defender)] = _now.AddSeconds(-6);
        _recent[(Other, Defender)] = _now.AddSeconds(-7);
        _recent[(Attacker, Ally)] = _now.AddSeconds(-30);

        _service.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.CruelMomentum} 10",
            $"FP {Attacker} {StatType.CruelMomentum} 2",
            $"Apply {Attacker}>{Attacker} {nameof(CruelMomentumStatusEffect)} 30");
        _recent.Keys.Should().BeEquivalentTo(new[] { (Attacker, Defender), (Attacker, Ally) }, "the stale record of the defeated is pruned");

        _log.Clear();
        _triggerReady = false;
        _service.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _log.Should().Equal($"Trigger {Attacker} {StatType.CruelMomentum} 10");
        _log.Clear();
        _dead.Add(Attacker);
        _service.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _dead.Clear();
        Stat(Attacker, StatType.CruelMomentum, 0);
        _service.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _invalid.Add(Gone);
        _service.ApplyRecentDamagerDefeatedEnemyEffects(Gone);
        _invalid.Add(Attacker);
        _service.ApplyRecentDamagerDefeatedEnemyEffects(Defender);
        _log.Should().BeEmpty();
    }

    [Test]
    public void ToxicPoisonSpreadsFromATargetToNearbyEnemies()
    {
        Stat(Attacker, StatType.KatarVenomCurrentPoisonSpreadRadiusMeters, 5);
        Stat(Attacker, StatType.KatarVenomCurrentPoisonSpreadDurationSeconds, 9);
        _nearby.Add(Other);

        _service.SpreadPoisonFromTarget(Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Other} {nameof(PoisonStatusEffect)} 9 {CombatDamageType.Poison}");
        _log.Clear();
        Stat(Attacker, StatType.KatarVenomCurrentPoisonSpreadDurationSeconds, 0);
        _service.SpreadPoisonFromTarget(Attacker, Defender);
        _log.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ triggered damage






    // ------------------------------------------------------------------ state

    [Test]
    public void ForgettingACreatureClearsEveryTableThatNamesIt()
    {
        Stat(Attacker, StatType.AutoAttackCycleRequiredCount, 5);
        Stat(Attacker, StatType.AutoAttackCycleDamage, 5);
        Stat(Attacker, StatType.MeleeAutoAttackCycleRequiredCount, 5);
        Stat(Attacker, StatType.MeleeAutoAttackCycleDamage, 5);
        Stat(Attacker, StatType.CriticalHitSequenceCountRequired, 5);
        Stat(Attacker, StatType.CriticalHitSequenceWindowSeconds, 5);
        Stat(Attacker, StatType.CriticalHitSequenceStaminaRestore, 5);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleRequiredCategory, (int)StatusEffectCategory.Bleeding);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleRequiredCount, 5);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamage, 5);
        Stat(Attacker, StatType.SourceStatusAutoAttackCycleDamageType, (int)CombatDamageType.Fire);
        _sourceApplied = true;
        _service.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Katar);
        Critical();
        _service.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Katar);
        _service.ApplyAutoAttackDamageModifiers(Attacker, Other, 10, SkillType.Katar);
        _service.ApplyAutoAttackDamageModifiers(Other, Attacker, 10, SkillType.Katar);
        Stat(Other, StatType.SourceStatusAutoAttackCycleRequiredCategory, (int)StatusEffectCategory.Bleeding);

        IDictionary Table(string name) => (IDictionary)typeof(CombatHitEffectsService)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_service)!;
        Table("_autoAttackCycleCounts").Count.Should().Be(1);
        Table("_meleeAutoAttackCycleCounts").Count.Should().Be(1);
        Table("_criticalHitSequenceStates").Count.Should().Be(1);
        Table("_sourceStatusAutoAttackCycleCounts").Count.Should().Be(2);

        _service.ForgetHitEffectState(Defender);
        Table("_sourceStatusAutoAttackCycleCounts").Count.Should().Be(1, "the record naming the defender as the target goes");
        _service.ForgetHitEffectState(Attacker);
        Table("_autoAttackCycleCounts").Count.Should().Be(0);
        Table("_meleeAutoAttackCycleCounts").Count.Should().Be(0);
        Table("_criticalHitSequenceStates").Count.Should().Be(0);
        Table("_sourceStatusAutoAttackCycleCounts").Count.Should().Be(0);
    }

    [Test]
    public void TheContainerBuildsThePartWithItsInjectedNeighbours()
    {
        _facades.Get<ICombatHitEffectsService>().Should().BeOfType<CombatHitEffectsService>();
    }

    [Test]
    public void TheContainerBuildsTheDealtAndTriggeredPartsAndTheBeastRecordIsTheCapstoneProof()
    {
        _facades.Get<ICombatDealtEffectsService>().Should().BeOfType<CombatDealtEffectsService>();
        _facades.Get<ICombatTriggeredDamageService>().Should().BeOfType<CombatTriggeredDamageService>();
        _facades.Get<IBeastDamageRecording>().Should().BeSameAs(_facades.Get<ICapstoneProofService>());
    }
}
