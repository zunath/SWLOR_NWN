using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The damage roll modifiers of <c>Combat</c>: the critical roll and its mitigation, the outgoing and target-status
/// modifiers of a damage roll under the shared bonus cap, and the attack and defense adjustments around it. The
/// rate and repeated-target parts are the real parts on the same scripted neighbours.
/// </summary>
[TestFixture]
public class CombatDamageModifiersServiceTests : CombatModifierTestBase
{
    // ------------------------------------------------------------------ critical roll

    [Test]
    public void ANormalRollIsDrawnBetweenTheDamageRangeAndKeepsTheCritical()
    {
        var (min, max) = CombatRules.CalculateDamageRange(100, 50, 20, 80, 10, 2);
        Random.SetSeed(7);
        var expected = (int)Random.NextFloat(min, max);
        Random.SetSeed(7);

        _service.CalculateDamageWithCriticalMitigation(Defender, 100, 50, 20, 80, 10, 2).Should().Be((expected, 2, false));
        _triggers.Should().BeEmpty("the defender has no downgrade");
    }

    [Test]
    public void APendingCriticalDowngradeForcesTheMinimumNormalDamageOnce()
    {
        _modifiers.Replace(Defender, StatType.CurrentIncomingAttackMinimumDamage, 1, 6, StatType.CurrentIncomingAttackMinimumDamage);
        var (min, _) = CombatRules.CalculateDamageRange(100, 50, 20, 80, 10, 0);

        _service.CalculateDamageWithCriticalMitigation(Defender, 100, 50, 20, 80, 10, 2).Should().Be((min, 0, true));
        _triggers.Should().BeEmpty("the pending downgrade is used first");
        _modifiers.GetStatAdjustment(Defender, StatType.CurrentIncomingAttackMinimumDamage, StatType.CurrentIncomingAttackMinimumDamage)
            .Should().Be(0);
    }

    [Test]
    public void ADefendersCriticalDowngradeUsesItsTriggerCooldownAndForcesTheMinimum()
    {
        Stat(Defender, StatType.IncomingCriticalHitDowngradeToMinimumDamage, 1);
        Stat(Defender, StatType.IncomingCriticalHitDowngradeCooldownMilliseconds, 4500);
        var (min, _) = CombatRules.CalculateDamageRange(100, 50, 20, 80, 10, 0, 3);

        _service.CalculateDamageWithCriticalMitigation(Defender, 100, 50, 20, 80, 10, 2, 3).Should().Be((min, 0, true));
        _triggers.Should().Equal($"{Defender} {StatType.IncomingCriticalHitDowngradeToMinimumDamage} {TimeSpan.FromMilliseconds(4500)}");

        _triggerReady = false;
        _service.TryUseIncomingCriticalHitDowngrade(Defender, 2).Should().BeFalse("on cooldown");
        _service.TryUseIncomingCriticalHitDowngrade(Defender, 0).Should().BeFalse("not a critical");
        _invalid.Add(Gone);
        _service.TryUseIncomingCriticalHitDowngrade(Gone, 2).Should().BeFalse();
        _service.TryUseIncomingCriticalHitDowngrade(Other, 2).Should().BeFalse("no downgrade stat");
        _triggers.Should().HaveCount(2);
        _service.CalculateDamageWithCriticalMitigation(Gone, 100, 50, 20, 80, 10, 0).Damage.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void TheCriticalDamageModifierAddsEveryAdjustmentAndIsCapped()
    {
        _service.ApplyCriticalDamageModifier(Attacker, 100, 0).Should().Be(100, "not a critical");
        _service.ApplyCriticalDamageModifier(Attacker, 0, 2).Should().Be(0);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2).Should().Be(100, "no adjustment");

        Stat(Attacker, StatType.CriticalDamagePercentAdjustment, 10);
        Stat(Attacker, StatType.WeaponCriticalDamagePercentAdjustment, 20);
        Stat(Attacker, StatType.StaffCriticalDamagePercentAdjustment, 40);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, SkillType.Rifle).Should().Be(130);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, SkillType.Staff, criticalDamagePercentAdjustment: 5).Should().Be(175);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, SkillType.Force).Should().Be(110);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, criticalDamagePercentAdjustment: 500).Should().Be(300, "capped at +200%");
    }

    [Test]
    public void TheHighHPTargetCriticalDamageNeedsTheTargetAboveItsThresholdAndShowsAPlayerItsLabel()
    {
        Stat(Attacker, StatType.CriticalDamageHighHPTargetThresholdPercent, 75);
        Stat(Attacker, StatType.CriticalDamageHighHPTargetPercentAdjustment, 30);
        HP(Defender, 80, 100);

        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(130);
        _feedback.Should().BeEmpty("an NPC sees no label");
        _players.Add(Attacker);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(130);
        _feedback.Should().ContainSingle().Which.Should().Contain("High Noon +30% critical damage");

        HP(Defender, 70, 100);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(100, "the target is below 75%");
        HP(Defender, 0, 0);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(100, "no maximum HP");
        _invalid.Add(Gone);
        HP(Gone, 100, 100);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Gone).Should().Be(100);
        _service.ApplyCriticalDamageModifier(Gone, 100, 2, defender: Defender).Should().Be(100);
        Stat(Attacker, StatType.CriticalDamageHighHPTargetThresholdPercent, 0);
        HP(Defender, 100, 100);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(100, "no threshold");
    }

    [Test]
    public void TheTargetStatusCriticalDamageNeedsTheConfiguredCategoryOnTheTarget()
    {
        Stat(Attacker, StatType.CriticalDamageTargetStatusPercentAdjustment, 25);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(100, "no category");
        Stat(Attacker, StatType.CriticalDamageTargetStatusCategory, (int)StatusEffectCategory.Bleeding);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(100, "the target is not bleeding");
        _targetHasCategory = true;
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(125);
        Stat(Attacker, StatType.CriticalDamageTargetStatusPercentAdjustment, 0);
        _service.ApplyCriticalDamageModifier(Attacker, 100, 2, defender: Defender).Should().Be(100);
    }

    [Test]
    public void TheSkillCriticalRateAddsStaffWeaponAndLowHPRates()
    {
        Stat(Attacker, StatType.StaffCriticalRatePercentAdjustment, 4);
        Stat(Attacker, StatType.WeaponCriticalRatePercentAdjustment, 3);
        _service.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Staff).Should().Be(7);
        _service.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Rifle).Should().Be(3);
        _service.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Force).Should().Be(0);

        Stat(Attacker, StatType.LowHPAttackThresholdPercent, 50);
        Stat(Attacker, StatType.LowHPCriticalRatePercentAdjustment, 10);
        HP(Attacker, 60, 100);
        _service.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Force).Should().Be(0, "above half HP");
        HP(Attacker, 50, 100);
        _service.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Force).Should().Be(10);
        HP(Attacker, 0, 0);
        _service.GetSkillCriticalRatePercentAdjustment(Attacker, SkillType.Force).Should().Be(0, "no maximum HP");
    }

    // ------------------------------------------------------------------ attack and defense

    [Test]
    public void TheRangedDefenseIgnoreAndTheBleedingTargetAttackBonusUseTheirStats()
    {
        _rangedIgnore = 25;
        _service.ApplyRangedAttackDefenseIgnore(Attacker, 100, SkillType.Rifle).Should().Be(CombatRules.ApplyDefenseIgnore(100, 25));

        _service.ApplyTargetStatusAttackModifiers(Attacker, Defender, 0, SkillType.Vibroblade).Should().Be(0);
        Stat(Attacker, StatType.AttackToBleedingTargetPercentAdjustment, 15);
        _service.ApplyTargetStatusAttackModifiers(Attacker, Defender, 99, SkillType.Vibroblade).Should().Be(99, "not bleeding");
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        _service.ApplyTargetStatusAttackModifiers(Attacker, Defender, 99, SkillType.Vibroblade).Should().Be(99 + 15);
        _service.ApplyTargetStatusAttackModifiers(Attacker, Defender, 99, SkillType.Rifle).Should().Be(99, "vibroblades only");
        Stat(Attacker, StatType.AttackToBleedingTargetPercentAdjustment, -200);
        _service.ApplyTargetStatusAttackModifiers(Attacker, Defender, 99, SkillType.Vibroblade).Should().Be(0);
    }

    [Test]
    public void TheLowHPAndLowFPAttackBonusesNeedTheResourceAtOrBelowTheThreshold()
    {
        _service.GetLowHPAttackAdjustment(Attacker).Should().Be(0);
        _service.GetLowFPAttackAdjustment(Attacker).Should().Be(0);
        Stat(Attacker, StatType.LowHPAttackThresholdPercent, 40);
        Stat(Attacker, StatType.LowHPAttackPercentAdjustment, 12);
        Stat(Attacker, StatType.LowFPAttackThresholdPercent, 30);
        Stat(Attacker, StatType.LowFPAttackPercentAdjustment, 8);
        _service.GetLowHPAttackAdjustment(Attacker).Should().Be(0, "no maximum HP");
        _service.GetLowFPAttackAdjustment(Attacker).Should().Be(0, "no maximum FP");

        HP(Attacker, 41, 100);
        _maxFP = 100;
        _currentFP = 31;
        _service.GetLowHPAttackAdjustment(Attacker).Should().Be(0);
        _service.GetLowFPAttackAdjustment(Attacker).Should().Be(0);
        HP(Attacker, 40, 100);
        _currentFP = 30;
        _service.GetLowHPAttackAdjustment(Attacker).Should().Be(12);
        _service.GetLowFPAttackAdjustment(Attacker).Should().Be(8);
    }

    [Test]
    public void TheNearbyStatusTargetAttackCountsHostileTargetsWithTheCategoryUpToItsMaximum()
    {
        _service.GetNearbyStatusTargetAttackAdjustment(Attacker).Should().Be(0, "not configured");
        Stat(Attacker, StatType.NearbyStatusTargetAttackPercentPerTarget, 5);
        Stat(Attacker, StatType.NearbyStatusTargetAttackRadiusMeters, 10);
        Stat(Attacker, StatType.NearbyStatusTargetAttackStatusCategory, (int)StatusEffectCategory.Bleeding);
        const uint vanishing = 55;
        const uint friendly = 66;
        var vanishingChecks = 0;
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! switch
        {
            vanishing => ++vanishingChecks == 1,
            0 or NWScript.OBJECT_INVALID => false,
            var other => !_invalid.Contains(other)
        });
        _engine.SetReturnSequence("GetFirstObjectInShape", Attacker);
        _engine.SetReturnSequence("GetNextObjectInShape", vanishing, Defender, friendly, Other, 0u);
        _engine.SetReturnHandler("GetIsReactionTypeHostile", args => (uint)args[0]! != friendly);
        foreach (var creature in new[] { Attacker, Defender, friendly })
            _categories.Add((creature, StatusEffectCategory.Bleeding));

        _service.GetNearbyStatusTargetAttackAdjustment(Attacker).Should().Be(5, "the attacker, the vanished, the friendly and the unbleeding do not count");
        _engine.CallsTo("GetFirstObjectInShape").Single().Arguments[1].Should().Be(10f);

        _categories.Add((Other, StatusEffectCategory.Bleeding));
        Stat(Attacker, StatType.NearbyStatusTargetAttackPercentMaximum, 7);
        _engine.SetReturnSequence("GetFirstObjectInShape", Defender);
        _engine.SetReturnSequence("GetNextObjectInShape", Other, 0u);
        _service.GetNearbyStatusTargetAttackAdjustment(Attacker).Should().Be(7, "two targets, capped at 7");
    }

    [Test]
    public void TheStatusSourceDefenseAndAccuracyModifiersUseTheSourceTables()
    {
        _service.ApplyStatusSourceDefenseModifiers(Attacker, Defender, 0).Should().Be(0);
        _service.ApplyStatusSourceAccuracyModifiers(Attacker, Defender, 0).Should().Be(0);
        _service.ApplyStatusSourceDefenseModifiers(Attacker, Defender, 80).Should().Be(80);
        _service.ApplyStatusSourceAccuracyModifiers(Attacker, Defender, 80).Should().Be(80);

        _statusSource[StatType.DefenseAgainstStatusSourcePercentAdjustment] = 25;
        _statusSource[StatType.AccuracyToStatusSourcePercentAdjustment] = -200;
        _service.ApplyStatusSourceDefenseModifiers(Attacker, Defender, 80).Should().Be(100);
        _service.ApplyStatusSourceAccuracyModifiers(Attacker, Defender, 80).Should().Be(1, "accuracy never drops below 1");
        _statusSource[StatType.DefenseAgainstStatusSourcePercentAdjustment] = -200;
        _service.ApplyStatusSourceDefenseModifiers(Attacker, Defender, 80).Should().Be(0);
    }

    // ------------------------------------------------------------------ single stages

    [Test]
    public void TheOrdinaryAttackMultiplierAndTheDamageTypeModifierReadTheirStats()
    {
        Stat(Attacker, StatType.OrdinaryAttackDamagePercentAdjustment, 50);
        _service.ApplyOrdinaryAttackDamageMultiplier(Attacker, 100).Should().Be(CombatRules.ApplyOrdinaryAttackDamageMultiplier(100, 50)).And.Be(150);

        _service.ApplyDamageTypeDealtModifiers(Attacker, 0, CombatDamageType.Poison).Should().Be(0);
        _service.ApplyDamageTypeDealtModifiers(Attacker, 100, CombatDamageType.Poison).Should().Be(100);
        Stat(Attacker, StatType.PoisonDamageDealtPercentAdjustment, 30);
        _service.ApplyDamageTypeDealtModifiers(Attacker, 100, CombatDamageType.Poison).Should().Be(Pct(100, 30));
        _service.ApplyDamageTypeDealtModifiers(Attacker, 100, CombatDamageType.Fire).Should().Be(100);
    }

    [Test]
    public void TheAbilityShapeModifierPicksTheSingleTargetOrAreaAdjustment()
    {
        Stat(Attacker, StatType.SingleTargetDamagePercentAdjustment, 10);
        Stat(Attacker, StatType.AreaAbilityDamagePercentAdjustment, 20);

        _service.ApplyAbilityShapeDamageModifier(Attacker, 100, false, null!).Should().Be(110, "an auto-attack is single target");
        _service.ApplyAbilityShapeDamageModifier(Attacker, 100, true, Hostile()).Should().Be(100, "neither shape");
        _area = true;
        _service.ApplyAbilityShapeDamageModifier(Attacker, 100, true, Hostile()).Should().Be(120);
        _singleTarget = true;
        _service.ApplyAbilityShapeDamageModifier(Attacker, 100, true, Hostile()).Should().Be(110);
        _service.ApplyAbilityShapeDamageModifier(Attacker, 100, true, new AbilityDetail()).Should().Be(100, "not hostile");
    }

    [Test]
    public void TheThrowingSingleTargetModifierCoversThrowingAutoAttacksAndSingleTargetThrowingAbilities()
    {
        _service.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 100, false, null!).Should().Be(100, "no adjustment");
        Stat(Attacker, StatType.ThrowingSingleTargetDamagePercentAdjustment, 15);
        _service.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 100, false, null!).Should().Be(Pct(100, 15));
        _service.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Rifle, 100, false, null!).Should().Be(100);
        _service.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 0, false, null!).Should().Be(0);
        _service.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 100, true, Hostile(SkillType.Throwing)).Should().Be(100, "an area ability");
        _singleTarget = true;
        _service.ApplyThrowingSingleTargetDamageModifier(Attacker, SkillType.Throwing, 100, true, Hostile(SkillType.Throwing)).Should().Be(Pct(100, 15));
    }

    [Test]
    public void TheMimicryAndSkillAbilityModifiersApplyOnlyToTheirAbilities()
    {
        Stat(Attacker, StatType.MimicryPotencyPercent, 40);
        _service.ApplyMimicryAbilityDamageModifier(Attacker, 100, SkillType.Mimicry, true).Should().Be(Pct(100, 40));
        _service.ApplyMimicryAbilityDamageModifier(Attacker, 100, SkillType.Mimicry, false).Should().Be(100);
        _service.ApplyMimicryAbilityDamageModifier(Attacker, 100, SkillType.Rifle, true).Should().Be(100);
        _service.ApplyMimicryAbilityDamageModifier(Attacker, 0, SkillType.Mimicry, true).Should().Be(0);

        _service.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Rifle, true).Should().Be(100, "no skill selected");
        Stat(Attacker, StatType.SkillAbilityDamagePercentAdjustmentSkillType, (int)SkillType.Rifle);
        _service.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Rifle, true).Should().Be(100, "no adjustment");
        Stat(Attacker, StatType.SkillAbilityDamagePercentAdjustment, 30);
        _service.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Rifle, true).Should().Be(Pct(100, 30));
        _service.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Pistol, true).Should().Be(100);
        _service.ApplySkillAbilityDamageModifier(Attacker, 100, SkillType.Rifle, false).Should().Be(100);
        _service.ApplySkillAbilityDamageModifier(Attacker, 0, SkillType.Rifle, true).Should().Be(0);
    }

    [Test]
    public void TheAbilityShapeStagesOfTwinBladeThrowingPhysicalAndSkillAreaUseTheirStats()
    {
        _service.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 100, true, true).Should().Be(100);
        Stat(Attacker, StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment, 10);
        Stat(Attacker, StatType.TwinBladeAreaAbilityDamagePercentAdjustment, 20);
        _service.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 100, true, false).Should().Be(110);
        _service.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 100, false, true).Should().Be(120);
        _service.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 100, true, true).Should().Be(Pct(100, 30));
        _service.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.Rifle, 100, true, true).Should().Be(100);
        _service.ApplyTwinBladeAbilityShapeDamageModifier(Attacker, SkillType.TwinBlade, 0, true, true).Should().Be(0);

        _service.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Throwing, 100, true).Should().Be(100);
        Stat(Attacker, StatType.ThrowingAreaAbilityDamagePercentAdjustment, 25);
        _service.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Throwing, 100, true).Should().Be(125);
        _service.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Throwing, 100, false).Should().Be(100);
        _service.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Rifle, 100, true).Should().Be(100);
        _service.ApplyThrowingAbilityShapeDamageModifier(Attacker, SkillType.Throwing, 0, true).Should().Be(0);

        _service.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Physical, 100, true).Should().Be(100);
        Stat(Attacker, StatType.SingleTargetPhysicalAbilityDamagePercentAdjustment, 35);
        _service.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Physical, 100, true).Should().Be(Pct(100, 35));
        _service.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Fire, 100, true).Should().Be(100);
        _service.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Physical, 100, false).Should().Be(100);
        _service.ApplyPhysicalAbilityShapeDamageModifier(Attacker, CombatDamageType.Physical, 0, true).Should().Be(0);

        _service.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 100, true).Should().Be(100, "no skill selected");
        Stat(Attacker, StatType.SkillAreaAbilityDamagePercentAdjustmentSkillType, (int)SkillType.Rifle);
        _service.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 100, true).Should().Be(100, "no adjustment");
        Stat(Attacker, StatType.SkillAreaAbilityDamagePercentAdjustment, 40);
        _service.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 100, true).Should().Be(Pct(100, 40));
        _service.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Pistol, 100, true).Should().Be(100);
        _service.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 100, false).Should().Be(100);
        _service.ApplySkillAreaAbilityDamageModifier(Attacker, SkillType.Rifle, 0, true).Should().Be(0);
    }

    [Test]
    public void TheLongRangeAndAfterDeflectionStagesNeedTheirConditions()
    {
        _service.ApplyRangedAbilityLongRangeDamageModifier(Attacker, Defender, SkillType.Rifle, 100).Should().Be(100, "not long range");
        _longRange = true;
        _service.ApplyRangedAbilityLongRangeDamageModifier(Attacker, Defender, SkillType.Rifle, 100).Should().Be(100, "no adjustment");
        Stat(Attacker, StatType.RangedAbilityLongRangeDamagePercentAdjustment, 20);
        _service.ApplyRangedAbilityLongRangeDamageModifier(Attacker, Defender, SkillType.Rifle, 100).Should().Be(120);
        _service.ApplyRangedAbilityLongRangeDamageModifier(Attacker, Defender, SkillType.Rifle, 0).Should().Be(0);

        Stat(Attacker, StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment, 30);
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Saberstaff, 100, true).Should().Be(100, "no window");
        Stat(Attacker, StatType.AreaAbilityAfterDeflectionWindowSeconds, 4);
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Saberstaff, 100, true).Should().Be(100, "no deflection source");
        _deflectionSource = DeflectionSource.Melee;
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Saberstaff, 100, true).Should().Be(100, "no recent deflection");
        _service.Deflection = (_, _, _) => true;
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Saberstaff, 100, true).Should().Be(Pct(100, 30), "any skill when unset");
        _service.DeflectionCalls.Should().Equal($"{Attacker} {DeflectionSource.Melee} 4", $"{Attacker} {DeflectionSource.Melee} 4");
        Stat(Attacker, StatType.AreaAbilityAfterDeflectionDamagePercentAdjustmentSkillType, (int)SkillType.Lightsaber);
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Saberstaff, 100, true).Should().Be(100);
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Lightsaber, 100, true).Should().Be(Pct(100, 30));
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Lightsaber, 100, false).Should().Be(100);
        _service.ApplyAreaAbilityAfterDeflectionDamageModifier(Attacker, SkillType.Lightsaber, 0, true).Should().Be(0);
    }

    [Test]
    public void TheDeflectionRecordIsReadThroughCombatByDefault()
    {
        _service.BaseHasRecentDeflection(Attacker, DeflectionSource.Melee, 4f).Should().BeFalse("nothing deflected");
        _service.BaseHasRecentDeflection(Attacker, DeflectionSource.None, 4f).Should().BeFalse();
    }

    // ------------------------------------------------------------------ the whole roll

    [Test]
    public void ARollWithNoModifiersComesThroughUnchangedAndRejectedRollsAreZero()
    {
        Dealt(out var taken).Should().Be(100);
        taken.Should().Be(0);
        Dealt(out taken, damage: 0).Should().Be(0);
        Dealt(out _, damage: -5).Should().Be(-5);
        Dealt(out _, isAbility: true, ability: Hostile(), abilityPercent: -100).Should().Be(0, "an authored zero-damage ability stays zero");
        _immune = true;
        Dealt(out taken).Should().Be(0);
        taken.Should().Be(0);
    }

    [Test]
    public void TheShortOverloadUsesItsDefaultsAndDropsTheTargetStatusAdjustment()
    {
        Stat(Defender, StatType.PhysicalDamageTakenPercentAdjustment, 20);
        _service.ApplyDamageDealtModifiers(Attacker, Defender, 100).Should().Be(120);
        Stat(Defender, StatType.ForceDamageTakenPercentAdjustment, 50);
        _service.ApplyDamageDealtModifiers(Attacker, Defender, 100, damageType: CombatDamageType.Force).Should().Be(150);
        _service.ApplyDamageDealtModifiers(Attacker, Defender, 100, isLandedAttack: false).Should().Be(120);
    }

    [Test]
    public void EachOutgoingStageOfTheRollAddsItsPercent()
    {
        void Case(string name, Action arrange, int expected, SkillType skill = SkillType.Invalid, bool isAbility = false,
            bool landed = false, CombatDamageType type = CombatDamageType.Physical)
        {
            _stats.Clear();
            _sideAttack = _backAttack = _area = _singleTarget = _longRange = _weaponOrForce = false;
            _service.Deflection = (_, _, _) => false;
            _deflectionSource = DeflectionSource.None;
            arrange();
            Dealt(out _, skill: skill, isAbility: isAbility, landed: landed, ability: isAbility ? Hostile(skill) : null!, type: type)
                .Should().Be(expected, name);
        }

        Case("side attack", () => { _sideAttack = true; Stat(Attacker, StatType.SideAttackDamagePercentAdjustment, 20); }, 120);
        Case("back attack on a landed auto-attack", () => { _backAttack = true; Stat(Attacker, StatType.BackAttackDamagePercentAdjustment, 10); }, 110, landed: true);
        Case("no back attack bonus off a landed hit", () => { _backAttack = true; Stat(Attacker, StatType.BackAttackDamagePercentAdjustment, 10); }, 100);
        Case("no back attack bonus for an ability", () => { _backAttack = true; Stat(Attacker, StatType.BackAttackDamagePercentAdjustment, 10); }, 100, isAbility: true, landed: true);
        Case("twin blade single target ability", () => { _singleTarget = true; Stat(Attacker, StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment, 10); }, 110, SkillType.TwinBlade, true);
        Case("throwing area ability", () => { _area = true; Stat(Attacker, StatType.ThrowingAreaAbilityDamagePercentAdjustment, 10); }, 110, SkillType.Throwing, true);
        Case("skill area ability", () => { _area = true; Stat(Attacker, StatType.SkillAreaAbilityDamagePercentAdjustmentSkillType, (int)SkillType.Rifle); Stat(Attacker, StatType.SkillAreaAbilityDamagePercentAdjustment, 10); }, 110, SkillType.Rifle, true);
        Case("physical single target ability", () => { _singleTarget = true; Stat(Attacker, StatType.SingleTargetPhysicalAbilityDamagePercentAdjustment, 10); }, 110, SkillType.Rifle, true);
        Case("area ability after a deflection", () =>
        {
            _area = true;
            _deflectionSource = DeflectionSource.Melee;
            _service.Deflection = (_, _, _) => true;
            Stat(Attacker, StatType.AreaAbilityAfterDeflectionWindowSeconds, 4);
            Stat(Attacker, StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment, 10);
        }, 110, SkillType.Lightsaber, true);
        Case("long range ability", () => { _longRange = true; Stat(Attacker, StatType.RangedAbilityLongRangeDamagePercentAdjustment, 10); }, 110, SkillType.Rifle, true);
        Case("mimicry potency", () => Stat(Attacker, StatType.MimicryPotencyPercent, 10), 110, SkillType.Mimicry, true);
        Case("skill ability", () => { Stat(Attacker, StatType.SkillAbilityDamagePercentAdjustmentSkillType, (int)SkillType.Rifle); Stat(Attacker, StatType.SkillAbilityDamagePercentAdjustment, 10); }, 110, SkillType.Rifle, true);
        Case("outgoing damage", () => Stat(Attacker, StatType.DamageDealtPercentAdjustment, 10), 110);
        Case("poison damage", () => Stat(Attacker, StatType.PoisonDamageDealtPercentAdjustment, 10), 110, type: CombatDamageType.Poison);
        Case("weapon or Force damage", () => { _weaponOrForce = true; Stat(Attacker, StatType.WeaponAndForceDamageDealtPercentAdjustment, 10); }, 110);
        Case("weapon or Force damage needs a weapon or Force", () => Stat(Attacker, StatType.WeaponAndForceDamageDealtPercentAdjustment, 10), 100);
        Case("weapon or Force damage with no adjustment", () => _weaponOrForce = true, 100);
        Case("single target auto-attack", () => Stat(Attacker, StatType.SingleTargetDamagePercentAdjustment, 10), 110);
        Case("throwing auto-attack", () => Stat(Attacker, StatType.ThrowingSingleTargetDamagePercentAdjustment, 10), 110, SkillType.Throwing);
        Case("low HP target", () =>
        {
            HP(Defender, 20, 100);
            Stat(Attacker, StatType.TargetLowHPDamageThresholdPercent, 25);
            Stat(Attacker, StatType.TargetLowHPDamagePercentAdjustment, 10);
        }, 110);
        Case("low HP target with a status", () =>
        {
            HP(Defender, 20, 100);
            _targetHasCategory = true;
            Stat(Attacker, StatType.TargetLowHPStatusDamageThresholdPercent, 25);
            Stat(Attacker, StatType.TargetLowHPStatusDamagePercentAdjustment, 10);
            Stat(Attacker, StatType.TargetLowHPStatusDamageStatusCategory, (int)StatusEffectCategory.Bleeding);
        }, 110);
        Case("low HP target with a status above the threshold", () =>
        {
            HP(Defender, 30, 100);
            Stat(Attacker, StatType.TargetLowHPStatusDamageThresholdPercent, 25);
            Stat(Attacker, StatType.TargetLowHPStatusDamagePercentAdjustment, 10);
            Stat(Attacker, StatType.TargetLowHPStatusDamageStatusCategory, (int)StatusEffectCategory.Bleeding);
        }, 100);
        Case("low HP target with a status and no maximum HP", () =>
        {
            HP(Defender, 0, 0);
            Stat(Attacker, StatType.TargetLowHPStatusDamageThresholdPercent, 25);
            Stat(Attacker, StatType.TargetLowHPStatusDamagePercentAdjustment, 10);
            Stat(Attacker, StatType.TargetLowHPStatusDamageStatusCategory, (int)StatusEffectCategory.Bleeding);
        }, 100);
    }

    [Test]
    public void EachTargetStatusOfTheRollAddsItsPercentOrFlatBonus()
    {
        void Case(string name, Action arrange, int expected, SkillType skill = SkillType.Invalid, bool isAbility = false,
            bool canRandom = true)
        {
            _stats.Clear();
            _statuses.Clear();
            _categories.Clear();
            _statusSource.Clear();
            _sourceApplied = _abilitySourceApplied = _abilitySourceAppliedBonus = _skillSelected = _highResource = 0;
            _nearby = false;
            arrange();
            Dealt(out var taken, skill: skill, isAbility: isAbility, ability: isAbility ? Hostile(skill) : null!, canRandom: canRandom)
                .Should().Be(expected, name);
            taken.Should().Be(0, name);
        }

        Case("status source", () => _statusSource[StatType.DamageToStatusSourcePercentAdjustment] = 10, 110);
        Case("sundered target", () => { _statuses.Add((Defender, typeof(SunderStatusEffect))); Stat(Attacker, StatType.DamageToSunderedTargetPercentAdjustment, 10); }, 110);
        Case("bleeding target", () => { _categories.Add((Defender, StatusEffectCategory.Bleeding)); Stat(Attacker, StatType.DamageToBleedingTargetPercentAdjustment, 10); }, 110);
        Case("bleeding target hit by an ability", () => { _categories.Add((Defender, StatusEffectCategory.Bleeding)); _skillSelected = 7; }, 107, SkillType.Rifle, true);
        Case("debuffed target", () => { _categories.Add((Defender, StatusEffectCategory.Debuff)); Stat(Attacker, StatType.DamageToDebuffedTargetPercentAdjustment, 10); }, 110);
        Case("target of the attacker's status", () => _sourceApplied = 10, 110);
        Case("target of the attacker's status hit by an ability", () => { _abilitySourceApplied = 10; _abilitySourceAppliedBonus = 3; }, 113, SkillType.Rifle, true);
        Case("katar ability on a poisoned target", () => { _statuses.Add((Defender, typeof(DisorientedStatusEffect))); Stat(Attacker, StatType.DamageToPoisonedOrDisorientedTargetPercentAdjustment, 10); }, 110, SkillType.Katar, true);
        Case("katar poison flat bonus", () =>
        {
            _statuses.Add((Defender, typeof(PoisonStatusEffect)));
            Stat(Attacker, StatType.DamageToPoisonedTargetFlatBonusChance, 100);
            Stat(Attacker, StatType.DamageToPoisonedTargetFlatBonus, 9);
        }, 109, SkillType.Katar);
        Case("katar poison flat bonus not rolled", () =>
        {
            _statuses.Add((Defender, typeof(PoisonStatusEffect)));
            Stat(Attacker, StatType.DamageToPoisonedTargetFlatBonusChance, 100);
            Stat(Attacker, StatType.DamageToPoisonedTargetFlatBonus, 9);
        }, 100, SkillType.Katar, canRandom: false);
        Case("weakened target", () => { _statuses.Add((Defender, typeof(HamstringStatusEffect))); Stat(Attacker, StatType.DamageToWeakenedOrHamstringTargetPercentAdjustment, 10); }, 110);
        Case("controlled target", () => { _categories.Add((Defender, StatusEffectCategory.Control)); Stat(Attacker, StatType.DamageToControlTargetPercentAdjustment, 10); }, 110);
        Case("high resource ability", () => _highResource = 10, 110, SkillType.Rifle, true);
        Case("rifle on a dazed target", () => { _statuses.Add((Defender, typeof(DazedStatusEffect))); Stat(Attacker, StatType.DamageToDisorientedDazedTargetPercentAdjustment, 10); }, 110, SkillType.Rifle);
        Case("ranged weapon on a nearby target", () => { _nearby = true; Stat(Attacker, StatType.RangedDamageToNearbyTargetPercentAdjustment, 10); }, 110, SkillType.Rifle);
        Case("pistol on a knocked-down target", () => { _statuses.Add((Defender, typeof(KnockdownStatusEffect))); Stat(Attacker, StatType.PistolDamageToDisorientedKnockdownOrTranquilizedTargetBonus, 4); }, 104, SkillType.Pistol);
        Case("staff ability on a blinded target", () => { _statuses.Add((Defender, typeof(BlindStatusEffect))); Stat(Attacker, StatType.AbilityDamageToKnockdownOrBlindTargetPercentAdjustment, 10); }, 110, SkillType.Staff, true);
        Case("katar ability on an unpoisoned target", () => Stat(Attacker, StatType.DamageToPoisonedOrDisorientedTargetPercentAdjustment, 10), 100, SkillType.Katar, true);
    }

    [Test]
    public void TheDefendersDamageTakenAdjustmentsComeBackSeparatelyAndAreAppliedLastWithinTheCombinedBudget()
    {
        void Case(string name, Action arrange, int expectedTaken, SkillType skill = SkillType.Invalid, bool isAbility = false,
            CombatDamageType type = CombatDamageType.Physical)
        {
            _stats.Clear();
            _statusSource.Clear();
            _partySource = 0;
            arrange();
            var dealt = Dealt(out var taken, skill: skill, isAbility: isAbility, ability: isAbility ? Hostile(skill) : null!, type: type);
            taken.Should().Be(expectedTaken, name);
            dealt.Should().Be(CombatRules.ApplyPercentDamageAdjustment(100, expectedTaken), name);
        }

        Case("taken from the status source", () => _statusSource[StatType.DamageTakenFromStatusSourcePercentAdjustment] = 10, 10);
        Case("taken from the source's party", () => _partySource = 15, 15);
        Case("physical taken", () => Stat(Defender, StatType.PhysicalDamageTakenPercentAdjustment, -20), -20);
        Case("physical ability taken", () => Stat(Defender, StatType.PhysicalAbilityDamageTakenPercentAdjustment, 12), 12, SkillType.Rifle, true);
        Case("Force taken", () => Stat(Defender, StatType.ForceDamageTakenPercentAdjustment, 25), 25, type: CombatDamageType.Force);
        Case("ranged physical taken", () => Stat(Defender, StatType.RangedPhysicalDamageTakenPercentAdjustment, 8), 8, SkillType.Rifle);
        Case("throwing taken", () => Stat(Defender, StatType.ThrowingDamageTakenPercentAdjustment, 6), 6, SkillType.Throwing);
        Case("capped at the combined reduction", () => Stat(Defender, StatType.PhysicalDamageTakenPercentAdjustment, -99), -85);
    }

    [Test]
    public void ThePercentStagesAreCappedTogetherWhileFlatBonusesStackOnTop()
    {
        Stat(Attacker, StatType.DamageDealtPercentAdjustment, 150);
        Dealt(out _).Should().Be(200, "the bonus is capped at +100%");
        Dealt(out _, abilityPercent: 20, isAbility: true, ability: Hostile(), skill: SkillType.Rifle).Should().Be(200);

        _stats.Clear();
        Dealt(out _, abilityPercent: 20, lowHPPercent: 10, isAbility: true, ability: Hostile(), skill: SkillType.Rifle)
            .Should().Be(CombatRules.ApplyPercentDamageAdjustment(CombatRules.ApplyPercentDamageAdjustment(100, 20), 10));

        Stat(Attacker, StatType.OrdinaryAttackDamagePercentAdjustment, -50);
        Dealt(out _, ordinary: true).Should().Be(50);
        Dealt(out _, ordinary: true, isAbility: true, ability: Hostile(), skill: SkillType.Rifle).Should().Be(100, "abilities are not ordinary attacks");
        Dealt(out _).Should().Be(100);
    }

}
