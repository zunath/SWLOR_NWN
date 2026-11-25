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
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability hit riders part of <c>Combat</c>: the next auto-attack with no delay, the same-target hit count, the near-target status of a
/// ranged hit, the costly hit rewards and the effects of a finished impact by target count.
/// </summary>
[TestFixture]
public class CombatAbilityHitRidersServiceTests : CombatAbilityImpactTestBase
{
    protected override object PrivateTarget => _hitRiders;

    [Test]
    public void TheNextAutoAttackGrantUsesAllSkillsOrTheSelectedSkillAndNeedsADuration()
    {
        Private("ApplyHostileAbilityHitNextAutoAttackNoDelay", Attacker, Hostile());
        Private("ApplyHostileAbilityHitNextAutoAttackNoDelay", Attacker, null!);
        Stat(StatType.HostileAbilityHitNextAutoAttackNoDelaySkillType, (int)SkillType.Pistol);
        Private("ApplyHostileAbilityHitNextAutoAttackNoDelay", Attacker, Hostile());
        _log.Should().BeEmpty("no duration");

        Stat(StatType.HostileAbilityHitNextAutoAttackNoDelayDurationSeconds, 4);
        Private("ApplyHostileAbilityHitNextAutoAttackNoDelay", Attacker, Hostile());
        Private("ApplyHostileAbilityHitNextAutoAttackNoDelay", Attacker, new AbilityDetail());
        Stat(StatType.HostileAbilityHitNextAutoAttackNoDelayAllSkills, 1);
        Private("ApplyHostileAbilityHitNextAutoAttackNoDelay", Attacker, Hostile());
        _log.Should().Equal($"NoDelay {Attacker} {SkillType.Pistol} 4", $"NoDelay {Attacker} 4");
    }

    [Test]
    public void TheSameTargetHitsCountPerTargetAndRestoreStaminaAtTheRequiredCount()
    {
        var ability = Hostile();
        string Refund(uint target) => $"HitStamina {Attacker} {StatType.SameTargetHostileAbilityStaminaRestore} 4 Rifle";
        _hitRiders.ApplySameTargetHostileAbilityHitEffects(Attacker, Defender, new AbilityDetail());
        _hitRiders.ApplySameTargetHostileAbilityHitEffects(Attacker, Defender, ability);
        _log.Should().BeEmpty("nothing is configured");

        Stat(StatType.SameTargetHostileAbilityHitCountRequired, 3);
        Stat(StatType.SameTargetHostileAbilityStaminaRestore, 4);
        _hitRiders.ApplySameTargetHostileAbilityHitEffects(Attacker, Defender, ability);
        _hitRiders.ApplySameTargetHostileAbilityHitEffects(Attacker, Other, ability);
        _hitRiders.ApplySameTargetHostileAbilityHitEffects(Attacker, Defender, ability);
        _hitRiders.ApplySameTargetHostileAbilityHitEffects(Attacker, Other, ability);
        Lines("HitStamina").Should().BeEmpty("neither target has been hit three times");

        _hitRiders.ApplySameTargetHostileAbilityHitEffects(Attacker, Defender, ability);
        Lines("HitStamina").Should().Equal(Refund(Defender));
        _hitRiders.ApplySameTargetHostileAbilityHitEffects(Attacker, Defender, ability);
        _hitRiders.ApplySameTargetHostileAbilityHitEffects(Attacker, Defender, ability);
        Lines("HitStamina").Should().HaveCount(1, "the count started again after the refund");

        _hitRiders.ApplySameTargetHostileAbilityHitEffects(Attacker, Other, ability);
        Lines("HitStamina").Should().Equal(Refund(Defender), Refund(Other));

        _hitRiders.ApplySameTargetHostileAbilityHitEffects(Attacker, Defender, ability);
        Lines("HitStamina").Should().HaveCount(3, "the next cycle of the first target refunds at its third hit");
    }

    [TestCase(StatType.RangedAbilityHitNearTargetRangeMeters, 0)]
    [TestCase(StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment, 0)]
    [TestCase(StatType.RangedAbilityHitNearTargetDurationSeconds, 0)]
    [TestCase(StatType.RangedAbilityHitNearTargetStatusEffectNameStrRef, 0)]
    [TestCase(StatType.RangedAbilityHitNearTargetStatusEffectIcon, -1)]
    [TestCase(StatType.RangedAbilityHitNearTargetStatusEffectCleanseTypes, 0)]
    [TestCase(StatType.RangedAbilityHitNearTargetStatusEffectResistanceType, 0)]
    public void TheNearTargetEffectNeedsEveryPartOfItsConfiguration(StatType missing, int value)
    {
        NearTargetStats();
        Stat(missing, value);
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Defender, Hostile(SkillType.Pistol), SkillType.Pistol);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheNearTargetEffectAppliesOnlyFromAHostileRangedAbilityWithinRange()
    {
        NearTargetStats();
        _invalid.Add(Gone);
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Defender, Hostile(SkillType.Lightsaber), SkillType.Lightsaber);
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Defender, new AbilityDetail(), SkillType.Pistol);
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Defender, null!, SkillType.Pistol);
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Gone, Hostile(SkillType.Pistol), SkillType.Pistol);
        _distances[Defender] = 6f;
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Defender, Hostile(SkillType.Pistol), SkillType.Pistol);
        _log.Should().BeEmpty();

        _distances[Defender] = 5f;
        Private("ApplyRangedAbilityHitNearTargetEffects", Attacker, Defender, Hostile(SkillType.Pistol), SkillType.Pistol);
        Lines("Apply").Should().ContainSingle().Which.Should().Contain("DamageDealtAdjustmentStatusEffect").And.EndWith($" 6 {CombatDamageType.Physical}");
    }

    [Test]
    public void TheCostlyHitRestoresStaminaOncePerCostAndExposesForTheSelectedSkills()
    {
        var ability = Hostile();
        Stat(StatType.CostlyAbilityHitStaminaRestore, 5);
        Stat(StatType.CostlyAbilityHitStaminaRestoreMinimumStaminaCost, 10);
        Stat(StatType.CostlyAbilityStatusMinimumStaminaCost, 15);
        Stat(StatType.CostlyAbilityExposedDurationSeconds, 4);
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, ability, SkillType.Rifle);
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, new AbilityDetail(), SkillType.Rifle);
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, null!, SkillType.Rifle);
        _log.Should().BeEmpty("nothing was paid");

        _costState = new CombatAbilityStaminaCostState { Cost = 12 };
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, ability, SkillType.Rifle);
        _costState.StaminaRestoreApplied.Should().BeTrue();
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, ability, SkillType.Rifle);
        _log.Should().Equal($"HitStamina {Attacker} {StatType.CostlyAbilityHitStaminaRestore} 5 Rifle");

        _log.Clear();
        _costState = new CombatAbilityStaminaCostState { Cost = 20 };
        Stat(StatType.CostlyAbilityHitStaminaRestoreSkillType, (int)SkillType.Pistol);
        Stat(StatType.CostlyAbilityStatusSkillType, (int)SkillType.Rifle);
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, ability, SkillType.Rifle);
        _log.Should().Equal($"Apply {Attacker}>{Defender} ExposedStatusEffect 4 {CombatDamageType.Physical}");

        _log.Clear();
        _stats.Clear();
        Private("ApplyCostlyAbilityHitEffects", Attacker, Defender, ability, SkillType.Rifle);
        _log.Should().BeEmpty("nothing is configured");
    }

    [Test]
    public void AFinishedImpactNeedsAValidActivatorASummaryAndATarget()
    {
        Stat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold, 1);
        Stat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestore, 3);
        _invalid.Add(Gone);
        _hitRiders.ApplyAbilityImpactEffects(Gone, Summary(SkillType.Throwing, 3));
        _hitRiders.ApplyAbilityImpactEffects(Attacker, null!);
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Throwing, 0));
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Throwing, 3, area: false));
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 3, area: false));
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Spear, 3, area: false));
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Rifle, 3));
        _log.Should().BeEmpty();
    }

    [Test]
    public void AFinishedAreaImpactGrantsTheAreaHitRewardsBeforeTheThrowingEffects()
    {
        _sources[(Attacker, StatType.AreaAbilityHastePerStack)] = new[]
        {
            Source("haste", (StatType.AreaAbilityHastePerStack, 5), (StatType.AreaAbilityHasteStackMinimumTargets, 2),
                (StatType.AreaAbilityHasteStackDurationSeconds, 10), (StatType.AreaAbilityHasteStackMaximumPercent, 20),
                (StatType.AreaAbilityHasteStacksPerAdditionalTarget, 1))
        };
        _sources[(Attacker, StatType.AreaHitStaminaRestorePerTarget)] = new[]
        {
            Source("stamina", (StatType.AreaHitStaminaRestorePerTarget, 2), (StatType.AreaHitStaminaRestoreMaximum, 5)),
            Source("noMaximum", (StatType.AreaHitStaminaRestorePerTarget, 2))
        };
        Stat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold, 3);
        Stat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestore, 4);
        Stat(StatType.ThrowingAreaAbilityAttackPercentPerTarget, 5);
        Stat(StatType.ThrowingAreaAbilityAttackDurationSeconds, 10);
        Stat(StatType.ThrowingAreaAbilityAttackPercentMax, 25);

        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Throwing, 4));

        _log.Should().Equal(
            $"AddCapped {Attacker} {StatType.AttackDelayReductionPercent} 5 10 20 haste:{(int)StatType.AreaAbilityHastePerStack} 3 False",
            $"HitStamina {Attacker} {StatType.AreaHitStaminaRestorePerTarget} 5 Throwing",
            $"HitStamina {Attacker} {StatType.ThrowingAreaAbilityMinTargetsStaminaRestore} 4 Throwing",
            $"AddCapped {Attacker} {StatType.AttackPercentAdjustment} 5 10 25 {StatType.ThrowingAreaAbilityAttackPercentPerTarget} 4 False");
    }

    [Test]
    public void TheAreaHasteStacksOncePerSourceOrPerAdditionalTargetFromItsThreshold()
    {
        _sources[(Attacker, StatType.AreaAbilityHastePerStack)] = new[]
        {
            Source("flat", (StatType.AreaAbilityHastePerStack, 3), (StatType.AreaAbilityHasteStackMinimumTargets, 2),
                (StatType.AreaAbilityHasteStackDurationSeconds, 6), (StatType.AreaAbilityHasteStackMaximumPercent, 9)),
            Source("unset", (StatType.AreaAbilityHastePerStack, 3)),
            Source("high", (StatType.AreaAbilityHastePerStack, 3), (StatType.AreaAbilityHasteStackMinimumTargets, 5))
        };

        _hitRiders.ApplyAreaHitHaste(Attacker, 4);
        _hitRiders.ApplyAreaHitHaste(Attacker, 1);
        _log.Should().Equal($"AddCapped {Attacker} {StatType.AttackDelayReductionPercent} 3 6 9 flat:{(int)StatType.AreaAbilityHastePerStack} 1 False");
    }

    [Test]
    public void TheThrowingAreaEffectsNeedTheirThresholdsAndFullConfigurations()
    {
        Stat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold, 5);
        Stat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestore, 4);
        Stat(StatType.ThrowingAreaAbilityAttackPercentPerTarget, 5);
        Stat(StatType.ThrowingAreaAbilityAttackDurationSeconds, 10);
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Throwing, 4));
        _log.Should().BeEmpty("too few targets and no attack maximum");
    }

    [Test]
    public void ASaberstaffAreaImpactRestoresResourcesOffCooldownAndBuffsItsActivatorAboveItsThresholds()
    {
        Stat(StatType.AreaAbilityMinTargetsResourceRestoreThreshold, 2);
        Stat(StatType.AreaAbilityFPRestore, 3);
        Stat(StatType.AreaAbilityStaminaRestore, 4);
        Stat(StatType.AreaAbilityResourceRestoreCooldownSeconds, 15);
        Stat(StatType.AreaAbilityMinTargetsBuffThreshold, 3);
        Stat(StatType.AreaAbilityBuffDurationSeconds, 8);
        Stat(StatType.AreaAbilityHastePercentAdjustment, 10);
        Stat(StatType.AreaAbilityAttackDeflection, 1);

        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 3));

        var deflection = StatRules.GetGrantedDeflectionStatType(StatType.AreaAbilityAttackDeflection);
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.AreaAbilityFPRestore} 15",
            $"FP {Attacker} {StatType.AreaAbilityFPRestore} 3",
            $"RestoredFP {Attacker}",
            $"HitStamina {Attacker} {StatType.AreaAbilityStaminaRestore} 4 Saberstaff",
            $"RestoredBoth {Attacker}",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 10 8 {StatType.AttackDelayReductionPercent}",
            $"Replace {Attacker} {deflection} 1 8 {StatType.AreaAbilityAttackDeflection}",
            $"GrantedDeflection {Attacker} {StatRules.GetStatTypeDeflectionSource(StatType.AreaAbilityAttackDeflection)}");
    }

    [Test]
    public void ASaberstaffAreaImpactRestoresNothingItCannotAndSkipsTheBuffBelowItsThresholdOrWithoutADuration()
    {
        Stat(StatType.AreaAbilityMinTargetsResourceRestoreThreshold, 2);
        Stat(StatType.AreaAbilityStaminaRestore, 4);
        _restoredStamina = 0;
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 2));
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.AreaAbilityFPRestore} 0",
            $"HitStamina {Attacker} {StatType.AreaAbilityStaminaRestore} 4 Saberstaff");

        _log.Clear();
        _restoredStamina = -1;
        _restoredFP = 0;
        Stat(StatType.AreaAbilityStaminaRestore, 0);
        Stat(StatType.AreaAbilityFPRestore, 3);
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 2));
        _blockedTriggers.Add(StatType.AreaAbilityFPRestore);
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 2));
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 1));
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.AreaAbilityFPRestore} 0",
            $"FP {Attacker} {StatType.AreaAbilityFPRestore} 3",
            $"Trigger {Attacker} {StatType.AreaAbilityFPRestore} 0");

        _log.Clear();
        Stat(StatType.AreaAbilityMinTargetsResourceRestoreThreshold, 0);
        Stat(StatType.AreaAbilityMinTargetsBuffThreshold, 3);
        Stat(StatType.AreaAbilityHastePercentAdjustment, 10);
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 2));
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 3));
        Stat(StatType.AreaAbilityBuffDurationSeconds, 8);
        Stat(StatType.AreaAbilityHastePercentAdjustment, 0);
        Stat(StatType.AreaAbilityAttackDeflection, 0);
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 3));
        _log.Should().BeEmpty();
    }

    [Test]
    public void ASaberstaffAreaDeflectionWithNoGrantedStatGrantsNothingAfterTheHaste()
    {
        Stat(StatType.AreaAbilityMinTargetsBuffThreshold, 3);
        Stat(StatType.AreaAbilityBuffDurationSeconds, 8);
        Stat(StatType.AreaAbilityHastePercentAdjustment, 10);
        Stat(StatType.AreaAbilityAttackDeflection, 1);
        _noGrantedDeflection = true;

        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Saberstaff, 3));

        _log.Should().Equal($"Replace {Attacker} {StatType.AttackDelayReductionPercent} 10 8 {StatType.AttackDelayReductionPercent}");
    }

    [Test]
    public void ASpearAreaImpactRestoresStaminaAtItsMinimumTargets()
    {
        Stat(StatType.SpearDamageCripplingDefenseMinimumTargets, 3);
        Stat(StatType.SpearDamageCripplingDefenseStaminaRestore, 2);
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Spear, 2));
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Spear, 3));
        Stat(StatType.SpearDamageCripplingDefenseStaminaRestore, 0);
        _hitRiders.ApplyAbilityImpactEffects(Attacker, Summary(SkillType.Spear, 3));
        _log.Should().Equal($"HitStamina {Attacker} {StatType.SpearDamageCripplingDefenseStaminaRestore} 2 Spear");
    }
}
