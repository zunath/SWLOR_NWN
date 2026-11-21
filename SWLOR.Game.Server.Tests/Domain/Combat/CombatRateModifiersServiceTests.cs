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
/// The critical rate and positional rate modifiers of <c>Combat</c>: the auto-attack and target-status critical rate,
/// the auto-attack cycle tracker, and the side and back attack adjustments.
/// </summary>
[TestFixture]
public class CombatRateModifiersServiceTests : CombatModifierTestBase
{
    [Test]
    public void TheAutoAttackCriticalRateAddsTheTargetStatusRateToTheCycleRate()
    {
        _rate.GetAutoAttackCriticalRateAdjustment(Attacker, Defender, 5).Should().Be(5);
        Stat(Attacker, StatType.TargetStatusCriticalRatePercentAdjustment, 8);
        Stat(Attacker, StatType.TargetStatusCriticalRateStatusCategory, (int)StatusEffectCategory.Control);
        _rate.GetAutoAttackCriticalRateAdjustment(Attacker, Defender, 5).Should().Be(5, "the target is not controlled");
        _targetHasCategory = true;
        _rate.GetAutoAttackCriticalRateAdjustment(Attacker, Defender, 5).Should().Be(13);
        _rate.GetTargetStatusCriticalRateAdjustment(Attacker, Defender).Should().Be(8);
        _invalid.Add(Gone);
        _rate.GetTargetStatusCriticalRateAdjustment(Gone, Defender).Should().Be(0);
        _rate.GetTargetStatusCriticalRateAdjustment(Attacker, Gone).Should().Be(0);
        Stat(Attacker, StatType.TargetStatusCriticalRatePercentAdjustment, 0);
        _rate.GetTargetStatusCriticalRateAdjustment(Attacker, Defender).Should().Be(0);
    }

    [Test]
    public void TheSideAndBackAttackBonusesNeedAMatchingPosition()
    {
        Stat(Attacker, StatType.SideAttackDamagePercentAdjustment, 15);
        Stat(Attacker, StatType.SideAttackHitChancePercentAdjustment, 6);
        Stat(Attacker, StatType.SideAttackCriticalRatePercentAdjustment, 7);
        Stat(Attacker, StatType.BackAttackDamagePercentAdjustment, 25);
        Stat(Attacker, StatType.BackAttackCriticalRatePercentAdjustment, 9);

        _rate.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 99).Should().Be(99);
        _rate.GetSideAttackHitChanceAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0);
        _rate.GetSideAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0);
        _rate.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 99).Should().Be(99);
        _rate.GetBackAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0);

        _sideAttack = _backAttack = true;
        _rate.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 99).Should().Be(99 + 15);
        _rate.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 0).Should().Be(0);
        _rate.GetSideAttackHitChanceAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(6);
        _rate.GetSideAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(7);
        _rate.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 99).Should().Be(99 + 25);
        _rate.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 0).Should().Be(0);
        _rate.GetBackAttackCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(9);

        Stat(Attacker, StatType.SideAttackDamagePercentAdjustment, 0);
        Stat(Attacker, StatType.BackAttackDamagePercentAdjustment, 0);
        _rate.ApplySideAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 99).Should().Be(99);
        _rate.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 99).Should().Be(99);
        _statusLog.Should().BeEmpty("no back attack was primed");
    }

    [Test]
    public void APrimedBackAttackExposesTheTargetOnceWithTheNegatedDefense()
    {
        _backAttack = true;
        Stat(Attacker, StatType.BackAttackExposedPercent, 20);
        _modifiers.Replace(Attacker, StatType.BackAttackExposedPercent, 20, 30);
        _modifiers.Replace(Attacker, StatType.BackAttackExposedDurationSeconds, 8, 30, "primer");

        _rate.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 50).Should().Be(50, "only the duration is missing");
        _statusLog.Should().BeEmpty();

        Stat(Attacker, StatType.BackAttackExposedDurationSeconds, 8);
        _rate.ApplyBackAttackDamageModifier(Attacker, Defender, SkillType.Rifle, 50).Should().Be(50);
        _statusLog.Should().Equal($"Apply {Attacker}>{Defender} {nameof(ExposedStatusEffect)} 8 {CombatDamageType.Physical}");
        ((ExposedStatusEffect)_applied.Single()).StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-20);
        _modifiers.GetStatAdjustment(Attacker, StatType.BackAttackExposedPercent).Should().Be(0, "both halves of the primer are consumed");
        _modifiers.GetStatAdjustment(Attacker, StatType.BackAttackExposedDurationSeconds).Should().Be(0);
    }

    [Test]
    public void ASideAttackIgnoresEvasionOnARollUnderItsScaledAndCappedChance()
    {
        _rate.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 80).Should().Be(80, "not a side attack");
        _sideAttack = true;
        _rate.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 0).Should().Be(0);
        _rate.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 80).Should().Be(80, "no chance");

        Stat(Attacker, StatType.SideAttackEvasionIgnoreChance, 40);
        _rate.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 80).Should().Be(80, "no ignore percent");

        Stat(Attacker, StatType.SideAttackEvasionIgnorePercent, 150);
        Stat(Attacker, StatType.SideAttackEvasionIgnoreChanceScalingAbility, (int)AbilityType.Agility + 1);
        Stat(Attacker, StatType.SideAttackEvasionIgnoreChanceMaximum, 100);
        _abilityScore = 60;
        Random.SetSeed(3);
        var roll = Random.D100(1);
        Random.SetSeed(3);
        _rate.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 80).Should().Be(0, $"a chance of 100 always passes (rolled {roll}) and the ignore is capped at 100%");
        _engine.CallsTo("GetAbilityScore").Single().Arguments[1].Should().Be(AbilityType.Agility);

        Stat(Attacker, StatType.SideAttackEvasionIgnorePercent, 25);
        Stat(Attacker, StatType.SideAttackEvasionIgnoreChanceMaximum, 1);
        for (var seed = 0; seed < 20; seed++)
        {
            Random.SetSeed(seed);
            var expected = Random.D100(1) > 1 ? 80 : 80 - 20;
            Random.SetSeed(seed);
            _rate.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 80).Should().Be(expected, $"seed {seed}");
        }

        Stat(Attacker, StatType.SideAttackEvasionIgnoreChanceScalingAbility, 0);
        Stat(Attacker, StatType.SideAttackEvasionIgnoreChanceMaximum, 0);
        _abilityScore = -50;
        Stat(Attacker, StatType.SideAttackEvasionIgnoreChance, 100);
        _rate.ApplySideAttackEvasionIgnore(Attacker, Defender, SkillType.Rifle, 80).Should().Be(60, "an unset scaling ability adds nothing");
    }

    // ------------------------------------------------------------------ auto-attack cycle

    [Test]
    public void TheAutoAttackCycleCountsWeaponAttacksAndGrantsItsCriticalRateOnTheLastOne()
    {
        _invalid.Add(Gone);
        _rate.PrepareAutoAttackCycleCriticalRate(Gone, SkillType.Rifle).Should().Be(0);
        _rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Force).Should().Be(0);
        _rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0, "not configured");
        _statusLog.Should().Equal($"Remove {Attacker} {nameof(AttackCycleTrackerStatusEffect)}");

        Stat(Attacker, StatType.RangedAutoAttackCycleCriticalRateRequiredCount, 3);
        Stat(Attacker, StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment, 25);
        Stat(Attacker, StatType.RangedAutoAttackCycleCriticalRateTrackerEffectIconType, (int)EffectIconType.Haste);
        _statusLog.Clear();
        _rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0);
        _rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0);
        _rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(25);
        _rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0, "the cycle starts over");
        _applied.OfType<AttackCycleTrackerStatusEffect>().Select(effect => effect.Name)
            .Should().Equal("Attack cycle: 1/3", "Attack cycle: 2/3", "Attack cycle: 3/3", "Attack cycle: 1/3");
        _statusLog.Where(line => line.StartsWith("Apply")).Select(line => line.Split(' ').Last())
            .Should().Equal(new[] { "0", "0", "3", "0" }, "the full cycle's tracker shows for 3 seconds");
        _feedback.Should().ContainSingle().Which.Should().Contain("Attack +25% Critical Rate");

        Stat(Attacker, StatType.RangedAutoAttackCycleCriticalRateTrackerEffectIconType, 0);
        _applied.Clear();
        _rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0);
        _rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(25, "counted without a tracker icon");
        _applied.Should().BeEmpty();

        _rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0);
        _repeated.ForgetDamageModifierState(Attacker);
        _rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0, "forgotten: 1 of 3 again");
        _rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(0);
        _rate.PrepareAutoAttackCycleCriticalRate(Attacker, SkillType.Rifle).Should().Be(25);
    }
}
