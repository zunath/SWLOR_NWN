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
/// The auto-attack effects part of <c>Combat</c>: what an auto-attack adds to its damage and to the attacker (the stored and proc
/// bonuses, the stamina and FP restores, the master restores, the accuracy penalty, the hamstring, the sundered target restore and the
/// source status stacks).
/// </summary>
[TestFixture]
public class CombatAutoAttackEffectsServiceTests : CombatHitEffectsTestBase
{
    [Test]
    public void ANonPositiveAutoAttackKeepsItsDamageAndTouchesNothing()
    {
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 0).Should().Be(0);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, -3, SkillType.Rifle).Should().Be(-3);
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

        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle).Should().Be(10 + 5 + 7 + 4 + 3 + 2);
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

        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle).Should().Be(10, "another skill's bonus");
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10).Should().Be(10, "no skill");
        Lines("Consume").Should().NotContain(line => line.Contains(StatType.NextSkillAutoAttackDamageBonus + " "));
    }

    [Test]
    public void TheSuppressedAutoAttackConsumesTheBonusesItWouldHaveAdded()
    {
        _temporary[StatType.NextSkillAutoAttackDamageBonusSkillType] = (int)SkillType.Rifle;
        _autoAttack.ConsumeSuppressedAutoAttackDamageBonuses(Attacker, SkillType.Rifle);

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

        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
        _log.Where(line => line.StartsWith("HitStamina") || line.StartsWith("FP") || line.StartsWith("Trigger")).Should().Equal(
            $"HitStamina {Attacker} {StatType.AutoAttackHitStaminaRestore} 1",
            $"HitStamina {Attacker} {StatType.AutoAttackStaminaRestore} 2",
            $"Trigger {Attacker} {StatType.AutoAttackFPRestore} 6",
            $"FP {Attacker} {StatType.AutoAttackFPRestore} 3",
            $"FP {Attacker} {StatType.SkillAutoAttackFPRestore} 4");

        _log.Clear();
        _triggerReady = false;
        Stat(Attacker, StatType.AutoAttackStaminaRestore, 0);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Pistol);
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

        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        _log.Should().NotContain(line => line.StartsWith("Stamina") || line.StartsWith("FP"), "no master");

        _master = Master;
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        _log.Where(line => line.StartsWith("Stamina") || line.StartsWith("FP")).Should().Equal(
            $"Stamina {Master} {StatType.AutoAttackMasterStaminaRestore} 2",
            $"FP {Master} {StatType.AutoAttackMasterFPRestore} 3");

        _log.Clear();
        Stat(Attacker, StatType.AutoAttackMasterStaminaRestore, 0);
        Stat(Attacker, StatType.AutoAttackMasterFPRestoreChance, 0);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
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
            var damage = _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
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
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
        Random.D100(1).Should().Be(none[0], "no configured chance draws nothing");
    }

    [Test]
    public void AnAutoAttackCanLowerTheTargetsAccuracyOnlyWithEveryValue()
    {
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentChance, 100);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustment, -10);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 6);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("Replace").Should().Equal($"Replace {Defender} {StatType.AccuracyPercentAdjustment} -10 6 {StatType.AutoAttackTargetAccuracyPercentAdjustment}");

        _log.Clear();
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 0);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 6);
        Stat(Attacker, StatType.AutoAttackTargetAccuracyPercentAdjustment, 0);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("Replace").Should().BeEmpty();
    }

    [Test]
    public void AnAutoAttackHamstringsAValidTargetOfTheConfiguredSkill()
    {
        Stat(Attacker, StatType.AutoAttackHamstringDurationSeconds, 4);
        Stat(Attacker, StatType.AutoAttackHamstringSkillType, (int)SkillType.Rifle);

        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(HamstringStatusEffect)} 4 {CombatDamageType.Physical}");

        _log.Clear();
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Pistol);
        _invalid.Add(Gone);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Gone, 10, SkillType.Rifle);
        Stat(Attacker, StatType.AutoAttackHamstringDurationSeconds, 0);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10, SkillType.Rifle);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void AnAutoAttackOnASunderedTargetRestoresFP()
    {
        Stat(Attacker, StatType.AutoAttackSunderedTargetFPRestore, 3);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("FP").Should().BeEmpty("the target is not sundered");

        _statuses.Add((Defender, typeof(SunderStatusEffect)));
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
        Lines("FP").Should().Equal($"FP {Attacker} {StatType.AutoAttackSunderedTargetFPRestore} 3");

        _log.Clear();
        _invalid.Add(Defender);
        _autoAttack.ApplyAutoAttackDamageModifiers(Attacker, Defender, 10);
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

        _autoAttack.ApplySourceStatusStackEffects(Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(InfectionStatusEffect)} stacks 1 12 {CombatDamageType.Poison}");

        _log.Clear();
        var existing = new InfectionStatusEffect(1);
        _effects[(Defender, typeof(InfectionStatusEffect), Attacker)] = existing;
        _autoAttack.ApplySourceStatusStackEffects(Attacker, Defender);
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
        _autoAttack.ApplySourceStatusStackEffects(Attacker, Defender);
        _log.Should().BeEmpty("only Infection stacks");

        Stat(Attacker, StatType.SourceStatusStackAppliedCategory, (int)StatusEffectCategory.Infection);
        _sourceApplied = false;
        _autoAttack.ApplySourceStatusStackEffects(Attacker, Defender);
        _sourceApplied = true;
        Stat(Attacker, StatType.SourceStatusStackDurationSeconds, 0);
        _autoAttack.ApplySourceStatusStackEffects(Attacker, Defender);
        _log.Should().BeEmpty();
    }
}
