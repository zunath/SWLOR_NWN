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
/// order (the cost and defense steps are the real parts), the queued weapon ability use and the hostile ability sequence.
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

    [Test]
    public void TheMovementSpeedNeedsAHostileAbilityAMatchingTriggerAndItsStats()
    {
        var ability = Hostile();
        Stat(Activator, StatType.AbilityUsedMovementSpeedPercentAdjustment, 15);
        Stat(Activator, StatType.AbilityUsedMovementSpeedDurationSeconds, 4);
        Private(_service, "ApplyAbilityUsedMovementSpeed", Activator, ability, SkillType.Rifle);
        Private(_service, "ApplyAbilityUsedMovementSpeed", Activator, new AbilityDetail { Name = "Heal" }, SkillType.Rifle);
        _log.Should().BeEmpty("no matching trigger, then not hostile");
        _asked.Should().ContainSingle().Which.Should().Be(
            $"TriggerSkill {Activator} {SkillType.Rifle} {StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType} {StatType.AbilityUsedMovementSpeedRangedOnly}");

        _matchingTriggerSkills.Add((StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType, StatType.AbilityUsedMovementSpeedRangedOnly));
        Stat(Activator, StatType.AbilityUsedMovementSpeedDurationSeconds, 0);
        Private(_service, "ApplyAbilityUsedMovementSpeed", Activator, ability, SkillType.Rifle);
        Stat(Activator, StatType.AbilityUsedMovementSpeedDurationSeconds, 4);
        Stat(Activator, StatType.AbilityUsedMovementSpeedPercentAdjustment, 0);
        Private(_service, "ApplyAbilityUsedMovementSpeed", Activator, ability, SkillType.Rifle);
        _log.Should().BeEmpty("no duration, then no speed");

        Stat(Activator, StatType.AbilityUsedMovementSpeedPercentAdjustment, -10);
        Private(_service, "ApplyAbilityUsedMovementSpeed", Activator, ability, SkillType.Rifle);
        _log.Should().Equal($"Replace {Activator} {StatType.MovementSpeedPercentAdjustment} -10 4 {StatType.AbilityUsedMovementSpeedPercentAdjustment}");
    }

    [Test]
    public void TheAreaRewardsGrantEachConfiguredSourcesDeflectionThenTheRidersOnceThenTheFPRestore()
    {
        var ability = Hostile();
        _grantedDeflections[StatType.AreaAbilityUsedAttackDeflection] = StatType.ShieldDeflection;
        _deflectionSources[StatType.AreaAbilityUsedAttackDeflection] = DeflectionSource.Shield;
        Sources(Activator, StatType.AreaAbilityUsedAttackDeflection,
            new Dictionary<StatType, int> { [StatType.AreaAbilityUsedAttackDeflection] = 5, [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 4 },
            new Dictionary<StatType, int> { [StatType.AreaAbilityUsedAttackDeflection] = 0, [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 4 },
            new Dictionary<StatType, int> { [StatType.AreaAbilityUsedAttackDeflection] = 3, [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 0 },
            new Dictionary<StatType, int> { [StatType.AreaAbilityUsedAttackDeflection] = 7, [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 6 });
        Stat(Activator, StatType.AreaAbilityUsedFPRestore, 3);

        Private(_service, "ApplyAreaAbilityUsedRewards", Activator, ability, false);
        Private(_service, "ApplyAreaAbilityUsedRewards", Activator, new AbilityDetail { Name = "Heal" }, true);
        _log.Should().BeEmpty("not an area ability, then not hostile");

        Private(_service, "ApplyAreaAbilityUsedRewards", Activator, ability, true);
        _log.Should().Equal(
            new[]
            {
                $"Replace {Activator} {StatType.ShieldDeflection} 5 4 source0:{(int)StatType.AreaAbilityUsedAttackDeflection}",
                $"Replace {Activator} {StatType.ShieldDeflection} 7 6 source3:{(int)StatType.AreaAbilityUsedAttackDeflection}",
                $"GrantedDeflection {Activator} {DeflectionSource.Shield}",
                $"RestoreFP {Activator} {StatType.AreaAbilityUsedFPRestore} 3",
            },
            "nothing was restored, so the restored FP riders do not run");

        _log.Clear();
        _restoredFP = 3;
        _sources.Clear();
        Private(_service, "ApplyAreaAbilityUsedRewards", Activator, ability, true);
        _log.Should().Equal($"RestoreFP {Activator} {StatType.AreaAbilityUsedFPRestore} 3", $"RestoredFP {Activator}");

        _log.Clear();
        Stat(Activator, StatType.AreaAbilityUsedFPRestore, 0);
        Private(_service, "ApplyAreaAbilityUsedRewards", Activator, ability, true);
        _log.Should().BeEmpty("no restore and no deflection source");
    }

    [Test]
    public void TheHostileForceAttackStacksOnTheCurrentStatusUpToItsMaximum()
    {
        var ability = Hostile();
        Stat(Activator, StatType.HostileAbilityForceAttackPercentPerStack, 6);
        Stat(Activator, StatType.HostileAbilityForceAttackDurationSeconds, 10);
        Stat(Activator, StatType.HostileAbilityForceAttackPercentMax, 15);
        _currentForceAttack = new HostileAbilityForceAttackStatusEffect(12);

        Private(_service, "ApplyHostileAbilityForceAttack", Activator, ability);

        _asked.Should().Equal($"Status {Activator} {nameof(HostileAbilityForceAttackStatusEffect)}");
        _log.Should().Equal($"Apply {Activator}>{Activator} HostileAbilityForceAttackStatusEffect(15) 10");

        _log.Clear();
        _currentForceAttack = null;
        Private(_service, "ApplyHostileAbilityForceAttack", Activator, ability);
        _log.Should().Equal($"Apply {Activator}>{Activator} HostileAbilityForceAttackStatusEffect(6) 10");
    }

    [Test]
    public void TheHostileForceAttackNeedsAHostileAbilityAndAllThreeStats()
    {
        Stat(Activator, StatType.HostileAbilityForceAttackPercentPerStack, 6);
        Stat(Activator, StatType.HostileAbilityForceAttackDurationSeconds, 10);
        Stat(Activator, StatType.HostileAbilityForceAttackPercentMax, 15);
        Private(_service, "ApplyHostileAbilityForceAttack", Activator, new AbilityDetail { Name = "Heal" });
        foreach (var stat in new[] { StatType.HostileAbilityForceAttackPercentMax, StatType.HostileAbilityForceAttackDurationSeconds, StatType.HostileAbilityForceAttackPercentPerStack })
        {
            Stat(Activator, stat, 0);
            Private(_service, "ApplyHostileAbilityForceAttack", Activator, Hostile());
        }

        _log.Should().BeEmpty();
        _asked.Should().BeEmpty();
    }

    [Test]
    public void TheFPSpendForceAttackNeedsAHostileAbilityItsStatsAndAnFPCostAtTheMinimum()
    {
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackPercent, 3);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackDurationSeconds, 9);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackMaxPercent, 9);
        Stat(Activator, StatType.HostileAbilityFPSpendForceAttackMinFPCost, 8);

        var twoCosts = Hostile(fp: 4);
        twoCosts.Requirements.Add(new AbilityRequirementFP(4));
        Private(_service, "ApplyHostileAbilityFPSpendForceAttack", Activator, Hostile(fp: 7));
        Private(_service, "ApplyHostileAbilityFPSpendForceAttack", Activator, new AbilityDetail { Name = "Heal" });
        _log.Should().BeEmpty("below the minimum, then not hostile");

        Private(_service, "ApplyHostileAbilityFPSpendForceAttack", Activator, twoCosts);
        _log.Should().Equal(
            new[] { $"AddCapped {Activator} {StatType.ForceAttackPercentAdjustment} 3 9 9 {StatType.HostileAbilityFPSpendForceAttackPercent} 1 True" },
            "the FP requirements add up");

        _log.Clear();
        foreach (var stat in new[] { StatType.HostileAbilityFPSpendForceAttackMinFPCost, StatType.HostileAbilityFPSpendForceAttackMaxPercent, StatType.HostileAbilityFPSpendForceAttackDurationSeconds, StatType.HostileAbilityFPSpendForceAttackPercent })
        {
            Stat(Activator, stat, 0);
            Private(_service, "ApplyHostileAbilityFPSpendForceAttack", Activator, twoCosts);
        }

        _log.Should().BeEmpty();
    }

    [Test]
    public void TheAttackAdjustmentStacksAndShowsTheTempoOnlyWhileItsTotalIsPositive()
    {
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustment, 2);
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustmentDurationSeconds, 8);
        Stat(Activator, StatType.HostileAbilityUsedAttackPercentAdjustmentMaximum, 10);

        Private(_service, "ApplyHostileAbilityUsedAttackAdjustment", Activator, new AbilityDetail { Name = "Heal" });
        _log.Should().BeEmpty();

        Private(_service, "ApplyHostileAbilityUsedAttackAdjustment", Activator, Hostile());
        _asked.Should().Equal($"Temporary {Activator} {StatType.AttackPercentAdjustment} {StatType.HostileAbilityUsedAttackPercentAdjustment}");
        _log.Should().Equal(
            new[] { $"AddCapped {Activator} {StatType.AttackPercentAdjustment} 2 8 10 {StatType.HostileAbilityUsedAttackPercentAdjustment} 1 True" },
            "no stacks are held, so no tempo status");

        _log.Clear();
        foreach (var stat in new[] { StatType.HostileAbilityUsedAttackPercentAdjustmentMaximum, StatType.HostileAbilityUsedAttackPercentAdjustmentDurationSeconds, StatType.HostileAbilityUsedAttackPercentAdjustment })
        {
            Stat(Activator, stat, 0);
            Private(_service, "ApplyHostileAbilityUsedAttackAdjustment", Activator, Hostile());
        }

        _log.Should().BeEmpty();
    }

    [Test]
    public void TheActivatedRidersRunOnlyForTheirSkillAndNeedAnAbility()
    {
        Stat(Activator, StatType.VibroknifeShadowEvasiveCombatRank, 1);
        var ability = Hostile();

        Private(_service, "ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.Rifle);
        _log.Should().BeEmpty();
        typeof(CombatAbilityActivationService).GetMethod("ApplyAbilityActivatedRiders", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_service, new object?[] { Activator, Target, null, SkillType.Vibroknife });
        _log.Should().BeEmpty("no ability");

        Private(_service, "ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.Vibroknife);
        _log.Should().Equal($"Apply {Activator}>{Activator} EvasiveCombatStatusEffect(-15,10,-15) 8");
    }

    [Test]
    public void TheHeavyVibrobladeRidersGrantTheNextAutoAttackBonusAndGuardiansResolveForTheirPerks()
    {
        var ability = Hostile();
        Stat(Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus, 6);
        Stat(Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds, 9);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 10);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds, 8);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveCooldownSeconds, 30);

        Private(_service, "ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.HeavyVibroblade);
        _log.Should().BeEmpty("neither rider's perks match");
        _asked.Should().Equal(
            $"PerkTypes {Activator} {string.Join(",", StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSecondaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerTertiaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuaternaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuinaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSenaryPerkType)}",
            $"PerkTypes {Activator} {string.Join(",", StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType, StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerSecondaryPerkType, StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerTertiaryPerkType, StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerQuaternaryPerkType)}");

        _matchingPerkTypes.Add(StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType);
        _matchingPerkTypes.Add(StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType);
        _hitPoints[Activator] = (100, 100);
        Private(_service, "ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.HeavyVibroblade);

        _log.Take(3).Should().Equal(
            $"Replace {Activator} {StatType.NextAutoAttackDamageBonus} 6 9 {StatType.NextAutoAttackDamageBonus}",
            $"Trigger {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent} 30",
            $"Attribute {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent}");
        var shield = SWLOR.Game.Server.Feature.AbilityDefinition.TemporaryHitPointEffects.GetGrantedAmount(
            Activator, SWLOR.Game.Server.Domain.Combat.TemporaryHitPointEffectKey.GuardiansResolve);
        var generation = SWLOR.Game.Server.Feature.AbilityDefinition.TemporaryHitPointEffects.GetGrantGeneration(
            Activator, SWLOR.Game.Server.Domain.Combat.TemporaryHitPointEffectKey.GuardiansResolve);
        shield.Should().Be(10, "10% of 100 HP fits under every cap");
        generation.Should().BeGreaterThan(0);
        _log.Skip(3).Should().Equal($"Apply {Activator}>{Activator} GuardiansResolveStatusEffect({shield},{generation}) 8");
    }

    [Test]
    public void TheNextAutoAttackBonusAndGuardiansResolveNeedTheirStats()
    {
        Private(_service, "ApplyNextAutoAttackDamageBonus", Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
            StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
        Stat(Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus, -3);
        Private(_service, "ApplyNextAutoAttackDamageBonus", Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
            StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
        _log.Should().BeEmpty("no bonus, then no duration");

        Stat(Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds, 4);
        Private(_service, "ApplyNextAutoAttackDamageBonus", Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
            StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
        _log.Should().Equal($"Replace {Activator} {StatType.NextAutoAttackDamageBonus} -3 4 {StatType.NextAutoAttackDamageBonus}");

        _log.Clear();
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 10);
        Private(_service, "ApplyGuardiansResolve", Activator);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 0);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds, 8);
        Private(_service, "ApplyGuardiansResolve", Activator);
        _log.Should().BeEmpty("no duration, then no shield");

        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 10);
        _triggerReady = false;
        Private(_service, "ApplyGuardiansResolve", Activator);
        _log.Should().Equal($"Trigger {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent} 0");

        // A creature with no hit points gets no pool, so no status follows the grant.
        _log.Clear();
        _triggerReady = true;
        Private(_service, "ApplyGuardiansResolve", Activator);
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent} 0",
            $"Attribute {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent}");
    }

    [Test]
    public void TheBalancedRecoveryRestoresTheBeastAndItsMasterWhenTheCategoryMatchesAndTheTriggerIsReady()
    {
        var ability = Hostile();
        Stat(Activator, StatType.BeastBalancedAbilityStaminaRestore, 5);
        Stat(Activator, StatType.BeastBalancedAbilityStaminaRestoreCooldownSeconds, 12);

        Private(_service, "ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.BeastMastery);
        _log.Should().BeEmpty("the category does not match");
        _asked.Should().Equal($"PerkCategory {Activator} {StatType.BeastBalancedAbilityStaminaRestoreCategoryId}");

        _matchingPerkCategories.Add(StatType.BeastBalancedAbilityStaminaRestoreCategoryId);
        _triggerReady = false;
        Private(_service, "ApplyBeastBalancedAbilityRecovery", Activator, ability);
        _log.Should().Equal($"Trigger {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 12");

        _log.Clear();
        _triggerReady = true;
        Private(_service, "ApplyBeastBalancedAbilityRecovery", Activator, ability);
        _log.Should().Equal(
            new[]
            {
                $"Trigger {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 12",
                $"RestoreStamina {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 5",
            },
            "a beast with no master restores only itself");

        _log.Clear();
        _masters[Activator] = Master;
        Private(_service, "ApplyBeastBalancedAbilityRecovery", Activator, ability);
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 12",
            $"RestoreStamina {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 5",
            $"RestoreStamina {Master} {StatType.BeastBalancedAbilityStaminaRestore} 5");

        _log.Clear();
        Stat(Activator, StatType.BeastBalancedAbilityStaminaRestore, 0);
        Private(_service, "ApplyBeastBalancedAbilityRecovery", Activator, ability);
        _log.Should().BeEmpty("no restore");
    }

    [TestCase(1, 10, -15)]
    [TestCase(2, 20, -25)]
    public void EvasiveCombatScalesWithItsRankForAreaOrSingleTargetAbilities(int rank, int evasion, int enmity)
    {
        Stat(Activator, StatType.VibroknifeShadowEvasiveCombatRank, rank);

        Private(_service, "ApplyVibroknifeShadowActivatedEffects", Activator, new AbilityDetail { Name = "Buff" });
        _log.Should().BeEmpty("neither an area nor a single target ability");

        Private(_service, "ApplyVibroknifeShadowActivatedEffects", Activator, new AbilityDetail { Name = "Fan", IsAreaAbility = true });
        _log.Should().Equal($"Apply {Activator}>{Activator} EvasiveCombatStatusEffect(-15,{evasion},{enmity}) 8");

        _log.Clear();
        Stat(Activator, StatType.VibroknifeShadowEvasiveCombatRank, 0);
        Private(_service, "ApplyVibroknifeShadowActivatedEffects", Activator, Hostile());
        _log.Should().BeEmpty("no rank");
    }

    [Test]
    public void TheSkirmisherRidersGrantEvasionTheNextPistolAbilityAndAnEnmityDropOnAValidTarget()
    {
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityDurationSeconds, 6);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEvasionPercent, 10);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityNextAttackDamageBonus, 4);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEnmityReductionPercent, 20);

        Private(_service, "ApplyAbilityActivatedRiders", Activator, Target, new AbilityDetail { Name = "Buff" }, SkillType.Pistol);
        _log.Should().BeEmpty("neither an area nor a single target ability");

        Private(_service, "ApplyAbilityActivatedRiders", Activator, Target, Hostile(), SkillType.Pistol);
        _log.Should().Equal(
            $"Replace {Activator} {StatType.EvasionPercentAdjustment} 10 6 {StatType.PistolSkirmisherEvasiveAbilityEvasionPercent}",
            $"NextSkill {Activator} {SkillType.Pistol} 4 0 6 0 {StatusEffectCategory.None}",
            $"ReduceEnmity {Activator} {Target} 20");

        _log.Clear();
        _invalid.Add(Target);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEvasionPercent, 0);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityNextAttackDamageBonus, 0);
        Private(_service, "ApplyPistolSkirmisherActivatedEffects", Activator, Target, Hostile());
        _invalid.Clear();
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEnmityReductionPercent, 0);
        Private(_service, "ApplyPistolSkirmisherActivatedEffects", Activator, Target, Hostile());
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityDurationSeconds, 0);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEvasionPercent, 10);
        Private(_service, "ApplyPistolSkirmisherActivatedEffects", Activator, Target, Hostile());
        _log.Should().BeEmpty("an invalid target, no reduction, then no duration");
    }

    [Test]
    public void CenteringNeedsItsStatsAndAReadyTriggerAndDropsEnmityOnAValidTarget()
    {
        Stat(Activator, StatType.LightsaberOffenseCenteringAccuracyPercent, 8);
        Stat(Activator, StatType.LightsaberOffenseCenteringDurationSeconds, 6);
        Stat(Activator, StatType.LightsaberOffenseCenteringCooldownSeconds, 20);
        Stat(Activator, StatType.LightsaberOffenseCenteringEnmityReductionPercent, 15);

        Private(_service, "ApplyLightsaberOffenseCentering", Activator, Target);
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.LightsaberOffenseCenteringAccuracyPercent} 20",
            $"Apply {Activator}>{Activator} CenteringStatusEffect(8) 6",
            $"ReduceEnmity {Activator} {Target} 15");

        _log.Clear();
        _invalid.Add(Target);
        Private(_service, "ApplyLightsaberOffenseCentering", Activator, Target);
        _invalid.Clear();
        Stat(Activator, StatType.LightsaberOffenseCenteringEnmityReductionPercent, 0);
        Private(_service, "ApplyLightsaberOffenseCentering", Activator, Target);
        _log.Where(line => line.StartsWith("ReduceEnmity", StringComparison.Ordinal)).Should().BeEmpty("an invalid target, then no reduction");

        _log.Clear();
        _triggerReady = false;
        Private(_service, "ApplyLightsaberOffenseCentering", Activator, Target);
        _log.Should().Equal($"Trigger {Activator} {StatType.LightsaberOffenseCenteringAccuracyPercent} 20");

        _log.Clear();
        Stat(Activator, StatType.LightsaberOffenseCenteringDurationSeconds, 0);
        Private(_service, "ApplyLightsaberOffenseCentering", Activator, Target);
        Stat(Activator, StatType.LightsaberOffenseCenteringDurationSeconds, 6);
        Stat(Activator, StatType.LightsaberOffenseCenteringAccuracyPercent, 0);
        Private(_service, "ApplyLightsaberOffenseCentering", Activator, Target);
        _log.Should().BeEmpty("no duration, then no accuracy");
    }

    [Test]
    public void SecondWindRestoresAPercentOfMaximumStaminaScaledByItsAbilityAndCappedAtLowStamina()
    {
        Stat(Activator, StatType.LightsaberOffenseSecondWindThresholdPercent, 30);
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, 10);
        Stat(Activator, StatType.LightsaberOffenseSecondWindCooldownSeconds, 60);
        Stat(Activator, StatType.LightsaberOffenseSecondWindScalingAbility, (int)AbilityType.Agility + 1);
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreMaximumPercent, 14);
        _abilityScores[AbilityType.Agility] = 6;
        _maxStamina[Activator] = 200;
        _stamina[Activator] = 60;

        Private(_service, "ApplyLightsaberOffenseSecondWind", Activator);
        _log.Should().Equal(
            new[]
            {
                $"Trigger {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 60",
                $"RestoreStamina {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 28",
            },
            "10% plus the Agility score of 6 is capped at 14% of 200");

        _log.Clear();
        _abilityScores[AbilityType.Agility] = -3;
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreMaximumPercent, 0);
        Private(_service, "ApplyLightsaberOffenseSecondWind", Activator);
        _log.Last().Should().Be(
            $"RestoreStamina {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 20",
            "a negative score adds nothing and no maximum caps nothing");

        _log.Clear();
        Stat(Activator, StatType.LightsaberOffenseSecondWindScalingAbility, 0);
        _abilityScores[AbilityType.Agility] = 6;
        Private(_service, "ApplyLightsaberOffenseSecondWind", Activator);
        _log.Last().Should().Be(
            $"RestoreStamina {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 20",
            "no scaling ability");

        _log.Clear();
        _triggerReady = false;
        Private(_service, "ApplyLightsaberOffenseSecondWind", Activator);
        _log.Should().Equal($"Trigger {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 60");

        _log.Clear();
        _triggerReady = true;
        _stamina[Activator] = 61;
        Private(_service, "ApplyLightsaberOffenseSecondWind", Activator);
        _maxStamina[Activator] = 0;
        _stamina[Activator] = 0;
        Private(_service, "ApplyLightsaberOffenseSecondWind", Activator);
        _maxStamina[Activator] = 200;
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, 0);
        Private(_service, "ApplyLightsaberOffenseSecondWind", Activator);
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, 10);
        Stat(Activator, StatType.LightsaberOffenseSecondWindThresholdPercent, 0);
        Private(_service, "ApplyLightsaberOffenseSecondWind", Activator);
        _log.Should().BeEmpty("above the threshold, no maximum stamina, no base percent, then no threshold");
    }

    [Test]
    public void GuardiansInfluenceGrantsItsDeflectionToNearbyPartyMembersButNotTheActivator()
    {
        _inShape.AddRange(new[] { Activator, Ally, Bystander });
        _party.Add(Ally);
        _grantedDeflections[StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection] = StatType.MeleeDeflection;

        Private(_service, "ApplyLightsaberDefenseActivatedEffects", Activator);
        _log.Should().BeEmpty("no deflection");

        Stat(Activator, StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection, 5);
        Private(_service, "ApplyLightsaberDefenseActivatedEffects", Activator);
        _log.Should().Equal($"Replace {Ally} {StatType.MeleeDeflection} 5 12 {StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection}");

        _log.Clear();
        _grantedDeflections.Clear();
        Private(_service, "ApplyLightsaberDefenseActivatedEffects", Activator);
        _log.Should().BeEmpty("no granted deflection type");
    }

    [Test]
    public void TheWardGrantsEachConfiguredDefenseAndItsTargetCopyForTheCategory()
    {
        var ability = Hostile();
        Stat(Activator, StatType.WardAbilityForceDefensePercentAdjustment, 3);
        Stat(Activator, StatType.WardAbilityDefenseDurationSeconds, 6);

        Private(_service, "ApplyLightsaberWardActivatedEffects", Activator, ability);
        _log.Should().BeEmpty("the category does not match");

        _matchingPerkCategories.Add(StatType.WardAbilityDefenseCategoryId);
        Private(_service, "ApplyLightsaberWardActivatedEffects", Activator, ability);
        _log.Should().Equal(
            $"Replace {Activator} {StatType.ForceDefensePercentAdjustment} 3 6 {StatType.WardAbilityForceDefensePercentAdjustment}",
            $"Replace {Activator} {StatType.WardTargetForceDefensePercentAdjustment} 3 6 {StatType.WardAbilityForceDefensePercentAdjustment}");

        _log.Clear();
        Stat(Activator, StatType.WardAbilityForceDefensePercentAdjustment, 0);
        Private(_service, "ApplyLightsaberWardActivatedEffects", Activator, ability);
        Stat(Activator, StatType.WardAbilityDefensePercentAdjustment, 2);
        Stat(Activator, StatType.WardAbilityDefenseDurationSeconds, 0);
        Private(_service, "ApplyLightsaberWardActivatedEffects", Activator, ability);
        _log.Should().BeEmpty("no defense, then no duration");
    }

    [Test]
    public void ADifferentHostileAbilityWithinTheWindowArmsTheNextAttackBleed()
    {
        Stat(Activator, StatType.HostileAbilitySequenceWindowSeconds, 6);
        Stat(Activator, StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds, 3);

        Private(_service, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        _log.Should().BeEmpty("the first ability starts the sequence");

        _now = _now.AddSeconds(6);
        Private(_service, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        _log.Should().BeEmpty("the same ability again does not continue the sequence");

        _now = _now.AddSeconds(6);
        Private(_service, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.PowerAttack, Hostile());
        _log.Should().Equal(
            new[] { $"Replace {Activator} {StatType.NextDamageDealtBleedDurationSeconds} 3 6 {StatType.NextDamageDealtBleedDurationSeconds}" },
            "a different ability at the edge of the window continues it");

        _log.Clear();
        _now = _now.AddSeconds(6.5);
        Private(_service, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        _log.Should().BeEmpty("past the window");
    }

    [Test]
    public void TheSequenceRecordsTheLastHostileAbilityEvenWithoutABleedAndIgnoresAnythingElse()
    {
        Private(_service, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        Sequences().Contains(Activator).Should().BeTrue();

        // With the bleed configured now, the ability recorded without it continues the sequence.
        Stat(Activator, StatType.HostileAbilitySequenceWindowSeconds, 6);
        Stat(Activator, StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds, 3);
        _now = _now.AddSeconds(2);
        Private(_service, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.PowerAttack, Hostile());
        _log.Should().ContainSingle();

        // A window with no bleed records the use and arms nothing.
        _log.Clear();
        Stat(Activator, StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds, 0);
        Private(_service, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        _log.Should().BeEmpty();

        // A non-hostile ability, a missing ability and an invalid activator are not recorded.
        Sequences().Clear();
        Private(_service, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, new AbilityDetail { Name = "Heal" });
        typeof(CombatAbilityActivationService).GetMethod("ApplyHostileAbilitySequenceEffects", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_service, new object?[] { Activator, FeatType.DoubleShot1, null });
        _invalid.Add(Activator);
        Private(_service, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        Sequences().Count.Should().Be(0);
    }

    [Test]
    public void ForgettingACreatureDropsOnlyItsSequence()
    {
        Private(_service, "ApplyHostileAbilitySequenceEffects", Activator, FeatType.DoubleShot1, Hostile());
        Private(_service, "ApplyHostileAbilitySequenceEffects", Ally, FeatType.DoubleShot1, Hostile());

        _service.ForgetActivationState(Activator);
        _service.ForgetActivationState(Bystander);

        Sequences().Contains(Activator).Should().BeFalse();
        Sequences().Contains(Ally).Should().BeTrue();
    }

    [Test]
    public void TheHostileResourceRestoresRunTheirRidersOnlyForWhatWasRestored()
    {
        var ability = Hostile();
        Stat(Activator, StatType.HostileAbilityFPRestore, 3);
        Stat(Activator, StatType.HostileAbilityStaminaRestore, 4);

        Private(_service, "ApplyHostileAbilityResourceRestoreEffects", Activator, ability);
        _log.Should().Equal(
            new[]
            {
                $"RestoreFP {Activator} {StatType.HostileAbilityFPRestore} 3",
                $"RestoreStamina {Activator} {StatType.HostileAbilityStaminaRestore} 4",
            },
            "no FP was restored, so neither the FP nor the both-resources riders run");

        _log.Clear();
        _restoredFP = 3;
        Private(_service, "ApplyHostileAbilityResourceRestoreEffects", Activator, ability);
        _log.Should().Equal(
            $"RestoreFP {Activator} {StatType.HostileAbilityFPRestore} 3",
            $"RestoredFP {Activator}",
            $"RestoreStamina {Activator} {StatType.HostileAbilityStaminaRestore} 4",
            $"RestoredBoth {Activator}");

        _log.Clear();
        _restoredStamina = 0;
        Private(_service, "ApplyHostileAbilityResourceRestoreEffects", Activator, ability);
        _log.Should().Equal(
            $"RestoreFP {Activator} {StatType.HostileAbilityFPRestore} 3",
            $"RestoredFP {Activator}",
            $"RestoreStamina {Activator} {StatType.HostileAbilityStaminaRestore} 4");

        _log.Clear();
        Stat(Activator, StatType.HostileAbilityFPRestore, 0);
        Stat(Activator, StatType.HostileAbilityStaminaRestore, 0);
        Private(_service, "ApplyHostileAbilityResourceRestoreEffects", Activator, ability);
        Stat(Activator, StatType.HostileAbilityFPRestore, 3);
        Private(_service, "ApplyHostileAbilityResourceRestoreEffects", Activator, new AbilityDetail { Name = "Heal" });
        _log.Should().BeEmpty("nothing to restore, then not hostile");
    }

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
        typeof(CombatAbilityActivationService).GetField("_damageTakenEffects", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatDamageTakenEffectsService>());
        typeof(CombatAbilityActivationService).GetField("_combatActivity", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatActivityService>());
        typeof(CombatAbilityActivationService).GetField("_costEffects", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatAbilityCostEffectsService>());
        typeof(CombatAbilityActivationService).GetField("_defenseEffects", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatAbilityDefenseEffectsService>());
        typeof(CombatAbilityActivationService).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Should().BeEmpty("the part holds no static state");
    }
}
