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
/// The ability activation part of <c>Combat</c>: the riders that run when a creature activates an ability, in their fixed
/// order (the four effect parts are real, built from the same scripted leaves) and the queued weapon ability use.
/// </summary>
[TestFixture]
public class CombatAbilityActivationServiceTests : CombatAbilityActivationTestBase
{
    [Test]
    public void AnInvalidActivatorOrAMissingAbilityOrSummaryRunsNothing()
    {
        Stat(Activator, StatType.HostileAbilityFPRestore, 5);
        _restoredFP = 5;
        _invalid.Add(Activator);

        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.Invalid, Hostile(), Summary());
        _invalid.Clear();
        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.Invalid, null!, Summary());
        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.Invalid, Hostile(), null!);

        _log.Should().BeEmpty();
        _asked.Should().BeEmpty();
        Sequences().Count.Should().Be(0);
    }

    [Test]
    public void AnActivationWithNothingConfiguredOnlyTracksTheAbilityAndRecordsTheSequence()
    {
        var ability = Hostile();

        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.DoubleShot1, ability, Summary());

        _log.Should().Equal($"Track {Activator} Strike");
        Sequences().Contains(Activator).Should().BeTrue("the sequence remembers the last hostile ability even with no bleed configured");
    }

    [Test]
    public void AWeaponActivationIsNotTrackedHereButAQueuedWeaponUseIs()
    {
        var weapon = Hostile("Weapon Strike");
        weapon.ActivationType = AbilityActivationType.Weapon;

        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.DoubleShot1, weapon, Summary());
        _log.Should().BeEmpty("the queued weapon attack tracks the use when it lands");

        _service.TrackQueuedWeaponAbilityUse(Activator, weapon);
        _log.Should().Equal($"Track {Activator} Weapon Strike");
    }

    // Every rider configured to fire: the effects come out in the order ApplyAbilityActivatedEffects calls the riders.
    [Test]
    public void EveryRiderRunsInItsFixedOrder()
    {
        const int masterCategory = 7;
        var ability = Hostile("Saber Strike", fp: 10);
        ability.RecastGroup = RecastGroup.Earthshatter;
        ability.EffectiveLevelPerkType = PerkType.DoubleShot;
        _abilitySkill = SkillType.Lightsaber;
        _masters[Activator] = Master;
        _forcePower = true;
        _areaImpact = true;
        _staminaCost = 30;
        _restoredFP = 4;
        _inShape.AddRange(new[] { Activator, Ally });
        _party.Add(Ally);
        _perkCategories.Add(masterCategory);
        _matchingTriggerSkills.Add((StatType.AbilityUsedEvasionPercentAdjustmentSkillType, null));
        _matchingTriggerSkills.Add((StatType.AbilityUsedRangedDeflectionSkillType, StatType.AbilityUsedRangedDeflectionRangedOnly));
        _matchingTriggerSkills.Add((StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType, StatType.AbilityUsedMovementSpeedRangedOnly));
        _matchingPerkCategories.Add(StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId);
        _matchingPerkCategories.Add(StatType.AbilityUsedPerkCategorySelfDefenseCategoryId);
        _matchingPerkCategories.Add(StatType.WardAbilityDefenseCategoryId);
        _grantedDeflections[StatType.AreaAbilityUsedAttackDeflection] = StatType.MeleeDeflection;
        _grantedDeflections[StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection] = StatType.MeleeDeflection;
        _grantedDeflections[StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection] = StatType.MeleeDeflection;
        _deflectionSources[StatType.AreaAbilityUsedAttackDeflection] = DeflectionSource.Melee;
        _deflectionSources[StatType.AbilityUsedRangedDeflection] = DeflectionSource.Ranged;
        _temporaryTotal = 6;
        var skill = (int)SkillType.Lightsaber;

        // 1. recast reduction
        Stat(Activator, StatType.AbilityUsedRecastReductionTriggerGroup, (int)RecastGroup.Earthshatter);
        Stat(Activator, StatType.AbilityUsedRecastReductionTargetGroup, (int)RecastGroup.Flash);
        Stat(Activator, StatType.AbilityUsedRecastReductionSeconds, 4);
        // 2. next skill auto-attack damage and 3. next skill FP cost
        Stat(Activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonusTriggerSkillType, skill);
        Stat(Activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonusSkillType, skill);
        Stat(Activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonus, 3);
        Stat(Activator, StatType.AbilityUsedNextSkillAutoAttackDamageWindowSeconds, 9);
        Stat(Activator, StatType.AbilityUsedNextSkillFPCostAdjustmentTriggerSkillType, skill);
        Stat(Activator, StatType.AbilityUsedNextSkillFPCostAdjustmentSkillType, skill);
        Stat(Activator, StatType.AbilityUsedNextSkillFPCostAdjustment, -2);
        Stat(Activator, StatType.AbilityUsedNextSkillFPCostAdjustmentWindowSeconds, 8);
        // 4. master hit chance
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment, 5);
        Stat(Activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds, 6);
        // 5. Force FP cost riders
        Stat(Activator, StatType.ForcePrecognition, 1);
        Stat(Activator, StatType.ForceConvergence, 1);
        // 6. skill evasion
        Stat(Activator, StatType.AbilityUsedEvasionPercentAdjustment, 10);
        Stat(Activator, StatType.AbilityUsedEvasionDurationSeconds, 5);
        // 7. hostile evasion and 8. costly evasion
        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustment, 11);
        Stat(Activator, StatType.HostileAbilityUsedEvasionDurationSeconds, 5);
        Stat(Activator, StatType.CostlyAbilityUsedEvasionMinimumStaminaCost, 20);
        Stat(Activator, StatType.CostlyAbilityUsedEvasionPercentAdjustment, 12);
        Stat(Activator, StatType.CostlyAbilityUsedEvasionDurationSeconds, 5);
        // 9. ranged deflection
        Stat(Activator, StatType.AbilityUsedRangedDeflection, 15);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionDurationSeconds, 6);
        Stat(Activator, StatType.AbilityUsedRangedDeflectionStatusEffectIcon, (int)EffectIconType.DeflectingAuraStatusEffect);
        // 10. movement speed
        Stat(Activator, StatType.AbilityUsedMovementSpeedPercentAdjustment, 20);
        Stat(Activator, StatType.AbilityUsedMovementSpeedDurationSeconds, 4);
        // 11. skill attack deflection, 12. perk category attack deflection, 13. single target attack deflection
        Stat(Activator, StatType.AbilityUsedAttackDeflectionSkillType, skill);
        Stat(Activator, StatType.AbilityUsedPerkCategoryAttackDeflectionCategoryType, masterCategory);
        Stat(Activator, StatType.SingleTargetAbilityAttackDeflectionSkillType, skill);
        // 14. area evasion and 15. area rewards
        Stat(Activator, StatType.AreaAbilityUsedEvasionPercentAdjustment, 13);
        Stat(Activator, StatType.AreaAbilityUsedEvasionDurationSeconds, 5);
        Sources(Activator, StatType.AreaAbilityUsedAttackDeflection, new Dictionary<StatType, int>
        {
            [StatType.AreaAbilityUsedAttackDeflection] = 8,
            [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 6,
        });
        Stat(Activator, StatType.AreaAbilityUsedFPRestore, 2);
        // 16. Force Attack and 17. FP spend Force Attack
        Stat(Activator, StatType.HostileAbilityForceAttackPercentPerStack, 5);
        Stat(Activator, StatType.HostileAbilityForceAttackDurationSeconds, 10);
        Stat(Activator, StatType.HostileAbilityForceAttackPercentMax, 20);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackPercent, 4);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackDurationSeconds, 10);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackMaxPercent, 12);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackMinFPCost, 5);
        // 18. nearby ally defense, 19. nearby ally attack deflection, 20. self defense
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefensePercentAdjustment, 6);
        Stat(Activator, StatType.AbilityUsedNearbyAllyDefenseDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection, 9);
        Stat(Activator, StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds, 7);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfEvasionPercentAdjustment, 3);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefenseDurationSeconds, 5);
        Stat(Activator, StatType.AbilityUsedPerkCategorySelfDefenseCooldownSeconds, 20);
        // 21. attack adjustment
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustment, 2);
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustmentDurationSeconds, 8);
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustmentMaximum, 10);
        // 22. the Lightsaber riders: Centering, Guardian's Influence and the Ward
        Stat(Activator, StatType.LightsaberOffenseCenteringAccuracyPercent, 7);
        Stat(Activator, StatType.LightsaberOffenseCenteringDurationSeconds, 6);
        Stat(Activator, StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection, 5);
        Stat(Activator, StatType.WardAbilityDefensePercentAdjustment, 4);
        Stat(Activator, StatType.WardAbilityDefenseDurationSeconds, 6);
        // 23. the sequence and 24. the resource restores
        Stat(Activator, StatType.HostileAbilitySequenceWindowSeconds, 6);
        Stat(Activator, StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds, 3);
        Stat(Activator, StatType.HostileAbilityFPRestore, 4);

        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.DoubleShot1, ability, Summary(singleTarget: true));

        _log.Should().Equal(
            $"Recast {Activator} {RecastGroup.Flash} 4",
            $"NextAutoAttack {Activator} {SkillType.Lightsaber} 3 9",
            $"NextFPCost {Activator} {SkillType.Lightsaber} -2 8",
            $"Replace {Master} {StatType.AbilityHitChancePercentAdjustment} 5 6 {StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment}",
            $"Trigger {Activator} {StatType.ForcePrecognition} 12",
            $"Apply {Activator}>{Activator} DangerSenseStatusEffect 30",
            $"Trigger {Activator} {StatType.ForceConvergence} 45",
            $"Apply {Activator}>{Activator} ForceConvergenceStatusEffect 30",
            $"Apply {Activator}>{Activator} EvasiveFootworkStatusEffect(10) 5",
            $"Replace {Activator} {StatType.EvasionPercentAdjustment} 11 5 {StatType.HostileAbilityUsedEvasionPercentAdjustment}",
            $"Replace {Activator} {StatType.EvasionPercentAdjustment} 12 5 {StatType.CostlyAbilityUsedEvasionPercentAdjustment}",
            $"Apply {Activator}>{Activator} RangedDeflectionStatusEffect(15,{EffectIconType.DeflectingAuraStatusEffect}) 6",
            $"GrantedDeflection {Activator} {DeflectionSource.Ranged}",
            $"Replace {Activator} {StatType.MovementSpeedPercentAdjustment} 20 4 {StatType.AbilityUsedMovementSpeedPercentAdjustment}",
            $"UsedDeflection {Activator} {StatType.AbilityUsedAttackDeflection} {StatType.AbilityUsedAttackDeflectionDurationSeconds} {StatType.AbilityUsedAttackDeflectionFPRestore}",
            $"UsedDeflection {Activator} {StatType.AbilityUsedPerkCategoryAttackDeflection} {StatType.AbilityUsedPerkCategoryAttackDeflectionDurationSeconds} {StatType.AbilityUsedPerkCategoryAttackDeflectionFPRestore}",
            $"UsedDeflection {Activator} {StatType.SingleTargetAbilityAttackDeflection} {StatType.SingleTargetAbilityAttackDeflectionDurationSeconds} {StatType.Invalid}",
            $"Replace {Activator} {StatType.EvasionPercentAdjustment} 13 5 {StatType.AreaAbilityUsedEvasionPercentAdjustment}",
            $"Replace {Activator} {StatType.MeleeDeflection} 8 6 source0:{(int)StatType.AreaAbilityUsedAttackDeflection}",
            $"GrantedDeflection {Activator} {DeflectionSource.Melee}",
            $"RestoreFP {Activator} {StatType.AreaAbilityUsedFPRestore} 2",
            $"RestoredFP {Activator}",
            $"Apply {Activator}>{Activator} HostileAbilityForceAttackStatusEffect(5) 10",
            $"AddCapped {Activator} {StatType.ForceAttackPercentAdjustment} 4 10 12 {StatType.HostileAbilityFPSpendForceAttackPercent} 1 True",
            $"Replace {Ally} {StatType.PhysicalDefensePercentAdjustment} 6 5 {StatType.AbilityUsedNearbyAllyDefensePercentAdjustment}",
            $"Apply {Activator}>{Activator} SentinelGuardStatusEffect(9,0,{StatType.MeleeDeflection}) 7",
            $"Apply {Activator}>{Ally} SentinelGuardStatusEffect(9,0,{StatType.MeleeDeflection}) 7",
            $"Trigger {Activator} {StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment} 20",
            $"Apply {Activator}>{Activator} GuardingStepStatusEffect(3,0,0) 5",
            $"AddCapped {Activator} {StatType.AttackPercentAdjustment} 2 8 10 {StatType.HostileAbilityUsedAttackPercentAdjustment} 1 True",
            $"Apply {Activator}>{Activator} ButchersTempoStatusEffect(6) 8",
            $"Trigger {Activator} {StatType.LightsaberOffenseCenteringAccuracyPercent} 0",
            $"Apply {Activator}>{Activator} CenteringStatusEffect(7) 6",
            $"Replace {Ally} {StatType.MeleeDeflection} 5 12 {StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection}",
            $"Replace {Activator} {StatType.PhysicalDefensePercentAdjustment} 4 6 {StatType.WardAbilityDefensePercentAdjustment}",
            $"Replace {Activator} {StatType.WardTargetPhysicalDefensePercentAdjustment} 4 6 {StatType.WardAbilityDefensePercentAdjustment}",
            $"RestoreFP {Activator} {StatType.HostileAbilityFPRestore} 4",
            $"RestoredFP {Activator}",
            $"Track {Activator} Saber Strike");
        Sequences().Contains(Activator).Should().BeTrue();
    }

    [Test]
    public void TheSkillTypeTheRidersUseIsTheSummarysOrElseTheAbilitysAndTheSingleTargetFlagFallsBackToTheAbility()
    {
        var ability = Hostile();
        _abilitySkill = SkillType.Vibroknife;
        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustmentSkillType, (int)SkillType.Rifle);
        Stat(Activator, StatType.HostileAbilityUsedEvasionPercentAdjustment, 10);
        Stat(Activator, StatType.HostileAbilityUsedEvasionDurationSeconds, 5);
        Stat(Activator, StatType.SingleTargetAbilityAttackDeflectionSkillType, (int)SkillType.Vibroknife);

        // The summary names the Rifle, so the hostile evasion keyed to the Rifle fires; a summary with a skill type keeps the
        // single target flag it has, so the single target deflection does not fire.
        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.Invalid, ability, Summary(SkillType.Rifle));
        Lines("Replace").Should().ContainSingle();
        Lines("UsedDeflection").Should().BeEmpty();

        // With no skill type on the summary the ability's own skill type (the Vibroknife) is used, and a hostile single target
        // ability counts as single target.
        _log.Clear();
        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.Invalid, ability, Summary());
        Lines("Replace").Should().BeEmpty();
        Lines("UsedDeflection").Should().Equal(
            $"UsedDeflection {Activator} {StatType.SingleTargetAbilityAttackDeflection} {StatType.SingleTargetAbilityAttackDeflectionDurationSeconds} {StatType.Invalid}");

        // A non-hostile ability is not single target by fallback.
        _log.Clear();
        ability.IsHostileAbility = false;
        _service.ApplyAbilityActivatedEffects(Activator, Target, FeatType.Invalid, ability, Summary());
        Lines("UsedDeflection").Should().BeEmpty();
    }

    // ------------------------------------------------------------------ construction

    [Test]
    public void TheConstructorOnlyAssignsAndTheContainerBuildsOneInstanceWithTheConvertedParts()
    {
        _log.Should().BeEmpty();
        _asked.Should().BeEmpty();
        _engine.Calls.Should().BeEmpty("the constructor makes no engine call");

        var part = _facades.Get<ICombatAbilityActivationService>();
        part.Should().BeOfType<CombatAbilityActivationService>();
        _facades.Get<ICombatAbilityActivationService>().Should().BeSameAs(part);
        const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(CombatAbilityActivationService).GetField("_queries", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatQueriesService>());
        typeof(CombatAbilityActivationService).GetField("_combatActivity", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatActivityService>());
        typeof(CombatAbilityActivationService).GetField("_costEffects", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatAbilityCostEffectsService>());
        typeof(CombatAbilityActivationService).GetField("_defenseEffects", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatAbilityDefenseEffectsService>());
        typeof(CombatAbilityActivationService).GetField("_offenseEffects", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatAbilityOffenseEffectsService>());
        typeof(CombatAbilityActivationService).GetField("_weaponActivation", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatAbilityWeaponActivationService>());
        typeof(CombatAbilityActivationService).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Should().BeEmpty("the part holds no static state");
        typeof(CombatAbilityActivationService).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Should().OnlyContain(field => field.IsInitOnly, "the part holds no state of its own");
    }
}
