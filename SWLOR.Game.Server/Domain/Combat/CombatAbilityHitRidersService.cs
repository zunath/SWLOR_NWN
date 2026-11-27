using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The effects an ability hit has at hit time: the costly ability rewards, the near-target status of a ranged hit, the same-target
    /// hit count reward, the next auto-attack with no delay, and the effects of a finished impact by the number of targets it hit (the
    /// area rewards and haste, and the throwing, saberstaff and spear area effects). The same-target hit counts are this instance's own table.
    /// </summary>
    public class CombatAbilityHitRidersService : ICombatAbilityHitRidersService
    {
        private readonly Dictionary<(uint, uint), int> _sameTargetHostileAbilityHitCounts = new();

        private readonly ICombatResourcesService _resources;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly ICombatDamageTakenEffectsService _damageTakenEffects;
        private readonly IStatService _stat;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;

        public CombatAbilityHitRidersService(
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            ICombatDamageTakenEffectsService damageTakenEffects,
            IStatService stat,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier)
        {
            _resources = resources;
            _attackTiming = attackTiming;
            _damageTakenEffects = damageTakenEffects;
            _stat = stat;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
        }

        public void ApplyCostlyAbilityHitEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true ||
                !_resources.TryGetAbilityStaminaCostState(activator, ability, out var costState))
            {
                return;
            }

            var staminaRestoreSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityHitStaminaRestoreSkillType));
            var statusSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityStatusSkillType));
            var staminaRestoreMinimumCost = _stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityHitStaminaRestoreMinimumStaminaCost);
            var statusMinimumCost = _stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityStatusMinimumStaminaCost);
            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.CostlyAbilityHitStaminaRestore);
            var exposedDuration = _stat.GetStatAdjustment(activator, StatType.CostlyAbilityExposedDurationSeconds);

            if (staminaRestore > 0 &&
                staminaRestoreMinimumCost > 0 &&
                costState.Cost >= staminaRestoreMinimumCost &&
                !costState.StaminaRestoreApplied &&
                SkillTypeMatchesOrGlobal(skillType, staminaRestoreSkillType))
            {
                _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.CostlyAbilityHitStaminaRestore, staminaRestore, ability);
                costState.StaminaRestoreApplied = true;
            }

            if (exposedDuration > 0 &&
                statusMinimumCost > 0 &&
                costState.Cost >= statusMinimumCost &&
                SkillTypeMatchesOrGlobal(skillType, statusSkillType))
            {
                _statusEffectApplication.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(ExposedStatusEffect),
                    exposedDuration,
                    CombatDamageType.Physical);
            }
        }

        public void ApplyRangedAbilityHitNearTargetEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null ||
                !ability.IsHostileAbility ||
                !IsRangedWeaponSkill(skillType) ||
                !GetIsObjectValid(target))
            {
                return;
            }

            var range = _stat.GetStatAdjustment(activator, StatType.RangedAbilityHitNearTargetRangeMeters);
            var damageDealt = _stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.RangedAbilityHitNearTargetDurationSeconds);
            var nameStrRef = _stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectNameStrRef);
            var icon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectIcon));
            var cleanseTypes = GetStatusEffectCleanseTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectCleanseTypes));
            var resistanceType = GetResistanceTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectResistanceType));
            if (range <= 0 ||
                damageDealt == 0 ||
                duration <= 0 ||
                nameStrRef <= 0 ||
                icon == EffectIconType.Invalid ||
                cleanseTypes == StatusEffectCleanseType.None ||
                resistanceType == ResistanceType.Invalid)
            {
                return;
            }

            if (GetDistanceBetween(activator, target) > range)
                return;

            _statusEffectApplication.ApplyStatusEffect(
                activator,
                target,
                new DamageDealtAdjustmentStatusEffect(
                    damageDealt,
                    nameStrRef,
                    icon,
                    cleanseTypes,
                    resistanceType),
                duration,
                CombatDamageType.Physical);
        }

        public void ApplySameTargetHostileAbilityHitEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var requiredCount = _stat.GetStatAdjustment(activator, StatType.SameTargetHostileAbilityHitCountRequired);
            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.SameTargetHostileAbilityStaminaRestore);
            if (requiredCount <= 0 || staminaRestore <= 0)
                return;

            var key = (activator, target);
            _sameTargetHostileAbilityHitCounts.TryGetValue(key, out var count);
            count++;

            if (count < requiredCount)
            {
                _sameTargetHostileAbilityHitCounts[key] = count;
                return;
            }

            _sameTargetHostileAbilityHitCounts[key] = 0;
            _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.SameTargetHostileAbilityStaminaRestore, staminaRestore, ability);
        }

        public void ApplyHostileAbilityHitNextAutoAttackNoDelay(
            uint activator,
            AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var appliesToAllSkills = _stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityHitNextAutoAttackNoDelayAllSkills) > 0;
            var autoAttackSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityHitNextAutoAttackNoDelaySkillType));
            if (!appliesToAllSkills && autoAttackSkillType == SkillType.Invalid)
                return;

            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityHitNextAutoAttackNoDelayDurationSeconds);
            if (duration <= 0)
                return;

            if (appliesToAllSkills)
                _attackTiming.GrantNextAutoAttackNoDelay(activator, duration);
            else
                _attackTiming.GrantNextAutoAttackNoDelay(activator, autoAttackSkillType, duration);
        }

        public void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!GetIsObjectValid(activator) || summary == null || summary.ImpactedTargetCount <= 0)
                return;

            ApplyAreaHitRewards(activator, summary);

            switch (summary.SkillType)
            {
                case SkillType.Throwing:
                    ApplyThrowingAreaAbilityImpactEffects(activator, summary);
                    break;
                case SkillType.Saberstaff:
                    ApplyAreaAbilityImpactEffects(activator, summary);
                    break;
                case SkillType.Spear:
                    ApplySpearAbilityImpactEffects(activator, summary);
                    break;
            }
        }

        private void ApplyThrowingAreaAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var staminaThreshold = _stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold);
            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestore);
            if (staminaThreshold > 0 && staminaRestore > 0 && summary.ImpactedTargetCount >= staminaThreshold)
            {
                _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestore, staminaRestore, summary.Ability);
            }

            var attackPerTarget = _stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackPercentPerTarget);
            var attackDuration = _stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackDurationSeconds);
            var attackMax = _stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackPercentMax);
            if (attackPerTarget > 0 && attackDuration > 0 && attackMax > 0)
            {
                ApplyStackingAttackBoost(activator, attackPerTarget, attackMax, attackDuration, summary.ImpactedTargetCount);
            }
        }

        private void ApplyAreaAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var restoreThreshold = _stat.GetStatAdjustment(activator, StatType.AreaAbilityMinTargetsResourceRestoreThreshold);
            var fpRestore = _stat.GetStatAdjustment(activator, StatType.AreaAbilityFPRestore);
            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.AreaAbilityStaminaRestore);
            var restoreCooldown = _stat.GetStatAdjustment(activator, StatType.AreaAbilityResourceRestoreCooldownSeconds);
            if (restoreThreshold > 0 &&
                summary.ImpactedTargetCount >= restoreThreshold &&
                _resources.TryUseStatTrigger(activator, StatType.AreaAbilityFPRestore, restoreCooldown))
            {
                var restoredFP = fpRestore > 0
                    ? _resources.RestoreFPFromStat(activator, StatType.AreaAbilityFPRestore, fpRestore)
                    : 0;
                if (restoredFP > 0)
                    _resources.ApplyAbilityRestoredFPEffects(activator);

                var restoredStamina = staminaRestore > 0
                    ? _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.AreaAbilityStaminaRestore, staminaRestore, summary.Ability)
                    : 0;

                if (restoredFP > 0 && restoredStamina > 0)
                    _resources.ApplyAbilityRestoredBothResourcesEffects(activator);
            }

            var buffThreshold = _stat.GetStatAdjustment(activator, StatType.AreaAbilityMinTargetsBuffThreshold);
            if (buffThreshold <= 0 || summary.ImpactedTargetCount < buffThreshold)
                return;

            var duration = _stat.GetStatAdjustment(activator, StatType.AreaAbilityBuffDurationSeconds);
            if (duration <= 0)
                return;

            var haste = _stat.GetStatAdjustment(activator, StatType.AreaAbilityHastePercentAdjustment);
            if (haste != 0)
            {
                _temporaryStatModifier.Replace(
                    activator,
                    StatType.AttackDelayReductionPercent,
                    haste,
                    duration,
                    StatType.AttackDelayReductionPercent);
            }

            var deflection = _stat.GetStatAdjustment(activator, StatType.AreaAbilityAttackDeflection);
            if (deflection != 0)
            {
                var source = _stat.GetStatTypeDeflectionSource(StatType.AreaAbilityAttackDeflection);
                var deflectionStatType = _stat.GetGrantedDeflectionStatType(StatType.AreaAbilityAttackDeflection);
                if (deflectionStatType == StatType.Invalid)
                    return;

                _temporaryStatModifier.Replace(
                    activator,
                    deflectionStatType,
                    deflection,
                    duration,
                    StatType.AreaAbilityAttackDeflection);
                _damageTakenEffects.ApplyAbilityGrantedAttackDeflectionEffects(activator, source);
            }
        }

        private void ApplySpearAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var minimumTargets = _stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefenseMinimumTargets);
            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefenseStaminaRestore);
            if (minimumTargets > 0 &&
                staminaRestore > 0 &&
                summary.ImpactedTargetCount >= minimumTargets)
            {
                _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.SpearDamageCripplingDefenseStaminaRestore, staminaRestore, summary.Ability);
            }
        }

        private void ApplyAreaHitRewards(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            ApplyAreaHitHaste(activator, summary.ImpactedTargetCount);
            foreach (var source in _stat.GetStatSources(activator, StatType.AreaHitStaminaRestorePerTarget))
            {
                var perTarget = source[StatType.AreaHitStaminaRestorePerTarget];
                var maximum = source[StatType.AreaHitStaminaRestoreMaximum];
                if (perTarget > 0 && maximum > 0)
                    _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.AreaHitStaminaRestorePerTarget,
                        Math.Min(maximum, perTarget * summary.ImpactedTargetCount), summary.Ability);
            }
        }

        public void ApplyAreaHitHaste(uint activator, int impactedTargetCount)
        {
            foreach (var source in _stat.GetStatSources(activator, StatType.AreaAbilityHastePerStack))
            {
                var threshold = source[StatType.AreaAbilityHasteStackMinimumTargets];
                if (threshold <= 0 || impactedTargetCount < threshold)
                    continue;

                var stacks = source[StatType.AreaAbilityHasteStacksPerAdditionalTarget] > 0
                    ? impactedTargetCount - threshold + 1
                    : 1;
                _temporaryStatModifier.AddCapped(activator, StatType.AttackDelayReductionPercent,
                    source[StatType.AreaAbilityHastePerStack],
                    source[StatType.AreaAbilityHasteStackDurationSeconds],
                    source[StatType.AreaAbilityHasteStackMaximumPercent],
                    source.GetModifierGroup(StatType.AreaAbilityHastePerStack), stacks);
            }
        }

        private void ApplyStackingAttackBoost(
            uint activator,
            int attackPercent,
            int maxAttackPercent,
            int durationSeconds,
            int requestedStacks)
        {
            _temporaryStatModifier.AddCapped(
                activator,
                StatType.AttackPercentAdjustment,
                attackPercent,
                durationSeconds,
                maxAttackPercent,
                StatType.ThrowingAreaAbilityAttackPercentPerTarget,
                requestedStacks);
        }

        public void ForgetSameTargetHitState(uint creature)
        {
            foreach (var key in _sameTargetHostileAbilityHitCounts.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _sameTargetHostileAbilityHitCounts.Remove(key);
            }
        }

        /// <inheritdoc/>
        public void ForgetCreatureState(uint creature, CombatForgetPhase phase)
        {
            switch (phase)
            {
                case CombatForgetPhase.AbilityImpact:
                    ForgetSameTargetHitState(creature);
                    break;
            }
        }
    }
}
