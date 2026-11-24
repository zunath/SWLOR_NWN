using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The cost effects part of <c>Combat</c>: the recast reduction, the next skill bonuses, the master hit chance, the Force FP
/// cost riders and the riders of an HP-cost ability.
/// </summary>
[TestFixture]
public class CombatAbilityCostEffectsServiceTests : CombatAbilityActivationTestBase
{
    [Test]
    public void TheRecastReductionNeedsATriggerGroupMatchAValidTargetAndSeconds()
    {
        var ability = Hostile();
        ability.RecastGroup = RecastGroup.Flash;
        Stat(Activator, StatType.AbilityUsedRecastReductionSecondaryTriggerGroup, (int)RecastGroup.Flash);
        Stat(Activator, StatType.AbilityUsedRecastReductionSeconds, 3);

        _costEffects.ApplyAbilityUsedRecastReduction(Activator, ability);
        _log.Should().BeEmpty("no target group");

        Stat(Activator, StatType.AbilityUsedRecastReductionTargetGroup, (int)RecastGroup.Earthshatter);
        _costEffects.ApplyAbilityUsedRecastReduction(Activator, ability);
        _log.Should().Equal(new[] { $"Recast {Activator} {RecastGroup.Earthshatter} 3" }, "the secondary trigger group matches too");

        _log.Clear();
        Stat(Activator, StatType.AbilityUsedRecastReductionSeconds, 0);
        _costEffects.ApplyAbilityUsedRecastReduction(Activator, ability);
        ability.RecastGroup = RecastGroup.Invalid;
        Stat(Activator, StatType.AbilityUsedRecastReductionSeconds, 3);
        _costEffects.ApplyAbilityUsedRecastReduction(Activator, ability);
        ability.RecastGroup = RecastGroup.Earthshatter;
        _costEffects.ApplyAbilityUsedRecastReduction(Activator, ability);
        _log.Should().BeEmpty("no seconds, an ability with no recast group, and a group that triggers nothing");
    }

    [Test]
    public void TheNextSkillGrantsNeedTheAbilitysSkillToMatchTheirTrigger()
    {
        var ability = Hostile();
        _abilitySkill = SkillType.Pistol;
        Stat(Activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonusTriggerSkillType, (int)SkillType.Rifle);
        Stat(Activator, StatType.AbilityUsedNextSkillFPCostAdjustmentTriggerSkillType, (int)SkillType.Rifle);

        _costEffects.ApplyAbilityUsedNextSkillAutoAttackDamageBonus(Activator, ability);
        _costEffects.ApplyAbilityUsedNextSkillFPCostAdjustment(Activator, ability);
        _log.Should().BeEmpty();

        // An unset target skill and amounts pass through as they are: the timing part decides what to grant.
        _abilitySkill = SkillType.Rifle;
        _costEffects.ApplyAbilityUsedNextSkillAutoAttackDamageBonus(Activator, ability);
        _costEffects.ApplyAbilityUsedNextSkillFPCostAdjustment(Activator, ability);
        _log.Should().Equal(
            $"NextAutoAttack {Activator} {SkillType.Invalid} 0 0",
            $"NextFPCost {Activator} {SkillType.Invalid} 0 0");
    }

    [Test]
    public void TheMasterHitChanceNeedsAMasterAnAdjustmentAndADuration()
    {
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment, -5);
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds, 6);
        _costEffects.ApplyAbilityUsedMasterAbilityHitChance(Activator);
        _log.Should().BeEmpty("no master");

        _masters[Activator] = Master;
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds, 0);
        _costEffects.ApplyAbilityUsedMasterAbilityHitChance(Activator);
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds, 6);
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment, 0);
        _costEffects.ApplyAbilityUsedMasterAbilityHitChance(Activator);
        _log.Should().BeEmpty("no duration, then no adjustment");

        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment, -5);
        _costEffects.ApplyAbilityUsedMasterAbilityHitChance(Activator);
        _log.Should().Equal(
            $"Replace {Master} {StatType.AbilityHitChancePercentAdjustment} -5 6 {StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment}");
    }

    [Test]
    public void TheForceFPCostRidersNeedAForcePowerThatCostsFPAndAReadyTrigger()
    {
        Stat(Activator, StatType.ForcePrecognition, 1);
        Stat(Activator, StatType.ForceConvergence, 1);
        _costEffects.ApplyForceFPCostActivatedEffects(Activator, Hostile(fp: 5));
        _log.Should().BeEmpty("not a Force power");

        _forcePower = true;
        _costEffects.ApplyForceFPCostActivatedEffects(Activator, Hostile());
        _log.Should().BeEmpty("no FP cost");

        _triggerReady = false;
        _costEffects.ApplyForceFPCostActivatedEffects(Activator, Hostile(fp: 5));
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.ForcePrecognition} 12",
            $"Trigger {Activator} {StatType.ForceConvergence} 45");

        _log.Clear();
        _triggerReady = true;
        Stat(Activator, StatType.ForcePrecognition, 0);
        _costEffects.ApplyForceFPCostActivatedEffects(Activator, Hostile(fp: 5));
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.ForceConvergence} 45",
            $"Apply {Activator}>{Activator} ForceConvergenceStatusEffect 30");
    }

    [Test]
    public void AnHPCostAbilityThatPaidNothingTriggersNothing()
    {
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendSoulSacrifice, 1);

        _costEffects.ApplyHitPointSpendAbilityEffects(Activator, 0, 10);
        _costEffects.ApplyHitPointSpendAbilityEffects(Activator, -5);

        _log.Should().BeEmpty();
    }

    [Test]
    public void AnHPCostAbilityRunsSoulSacrificeSoulAscensionTheStaminaRestoreAndTheTemporaryHPInThatOrder()
    {
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds, 8);
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendSoulSacrifice, 1);
        Stat(Activator, StatType.HeavyVibrobladeOffenseSoulAscension, 1);
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent, 10);
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreCooldownSeconds, 6);
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility, (int)AbilityType.Might + 1);
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreMaximumPercent, 13);
        Stat(Activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP, 50);
        Stat(Activator, StatType.HitPointSpendTemporaryHPDurationSeconds, 12);
        _abilityScores[AbilityType.Might] = 5;
        _maxStamina[Activator] = 100;

        _costEffects.ApplyHitPointSpendAbilityEffects(Activator, 20, 30);

        var restore = CombatRules.CalculateHitPointSpendStaminaRestore(100, 13, 20, 30);
        _log.Should().Equal(
            $"Apply {Activator}>{Activator} SoulSacrificeStatusEffect 30",
            $"Replace {Activator} {StatType.HeavyVibrobladeOffenseSoulAscension} 1 8 {StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds}",
            $"Trigger {Activator} {StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent} 6",
            $"RestoreStamina {Activator} {StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent} {restore}",
            $"Attribute {Activator} {StatType.HitPointSpendTemporaryHPPercentOfSpentHP}");
    }

    [Test]
    public void TheHPCostRidersUseAOneSecondWindowAtLeastAndNeedTheirStats()
    {
        Stat(Activator, StatType.HeavyVibrobladeOffenseSoulAscension, 1);

        _costEffects.ApplyHitPointSpendAbilityEffects(Activator, 10);

        _log.Should().Equal(
            new[] { $"Replace {Activator} {StatType.HeavyVibrobladeOffenseSoulAscension} 1 1 {StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds}" },
            "no stamina restore and no temporary HP are configured");
    }

    [Test]
    public void TheHPCostStaminaRestoreNeedsAReadyTriggerAndScalesOnlyWithAPositiveScore()
    {
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent, 10);
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility, (int)AbilityType.Might + 1);
        _abilityScores[AbilityType.Might] = -4;
        _maxStamina[Activator] = 100;

        _triggerReady = false;
        Private(_costEffects, "ApplyHitPointSpendStaminaRestore", Activator, 20, 0);
        _log.Should().Equal($"Trigger {Activator} {StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent} 0");

        _log.Clear();
        _triggerReady = true;
        Private(_costEffects, "ApplyHitPointSpendStaminaRestore", Activator, 20, 0);
        _log.Last().Should().Be(
            $"RestoreStamina {Activator} {StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent} {CombatRules.CalculateHitPointSpendStaminaRestore(100, 10, 20, 0)}",
            "a negative score adds nothing and no maximum caps nothing");

        _log.Clear();
        Stat(Activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility, 0);
        Private(_costEffects, "ApplyHitPointSpendStaminaRestore", Activator, 20, 0);
        _log.Last().Should().EndWith($" {CombatRules.CalculateHitPointSpendStaminaRestore(100, 10, 20, 0)}", "no scaling ability");
    }

    [Test]
    public void TheHPCostTemporaryHPNeedsAPercentAndADuration()
    {
        Stat(Activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP, 50);
        Private(_costEffects, "ApplyHitPointSpendTemporaryHitPoints", Activator, 10);
        Stat(Activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP, 0);
        Stat(Activator, StatType.HitPointSpendTemporaryHPDurationSeconds, 12);
        Private(_costEffects, "ApplyHitPointSpendTemporaryHitPoints", Activator, 10);
        Stat(Activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP, 50);
        Private(_costEffects, "ApplyHitPointSpendTemporaryHitPoints", Activator, 0);
        _log.Should().BeEmpty("no duration, no percent, then nothing spent");

        Private(_costEffects, "ApplyHitPointSpendTemporaryHitPoints", Activator, 10);
        _log.Should().Equal($"Attribute {Activator} {StatType.HitPointSpendTemporaryHPPercentOfSpentHP}");
    }

    // ------------------------------------------------------------------ construction

    [Test]
    public void TheConstructorOnlyAssignsAndTheContainerBuildsOneInstanceWithTheConvertedParts()
    {
        _log.Should().BeEmpty();
        _asked.Should().BeEmpty();
        _engine.Calls.Should().BeEmpty("the constructor makes no engine call");

        var part = _facades.Get<ICombatAbilityCostEffectsService>();
        part.Should().BeOfType<CombatAbilityCostEffectsService>();
        _facades.Get<ICombatAbilityCostEffectsService>().Should().BeSameAs(part);
        const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(CombatAbilityCostEffectsService).GetField("_recast", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<IRecastService>());
        typeof(CombatAbilityCostEffectsService).GetField("_attackTiming", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatAttackTimingService>());
        typeof(CombatAbilityCostEffectsService).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Should().BeEmpty("the part holds no static state");
        typeof(CombatAbilityCostEffectsService).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Should().OnlyContain(field => field.IsInitOnly, "the part holds no state of its own");
    }
}
