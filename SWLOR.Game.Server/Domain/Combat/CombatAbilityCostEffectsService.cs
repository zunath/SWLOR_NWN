using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The cost riders of an ability activation: the recast reduction, the next skill bonuses, the master hit chance, the
    /// Force FP cost riders and the riders of an HP-cost ability. Every step is a public member that
    /// <see cref="CombatAbilityActivationService"/> calls in its fixed order; the part holds no state.
    /// </summary>
    public class CombatAbilityCostEffectsService : ICombatAbilityCostEffectsService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly IStatService _stat;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IRecastService _recast;
        private readonly ICombatRecorderService _combatRecorder;

        public CombatAbilityCostEffectsService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            IStatService stat,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier,
            IRecastService recast,
            ICombatRecorderService combatRecorder)
        {
            _queries = queries;
            _resources = resources;
            _attackTiming = attackTiming;
            _stat = stat;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
            _recast = recast;
            _combatRecorder = combatRecorder;
        }

        /// <param name="hitPointsSpent">HP actually paid. Nothing triggers when no HP was paid.</param>
        /// <param name="hitPointCost">Full HP cost before the payment was clipped at 1 HP; 0 when it was paid in full.</param>
        public void ApplyHitPointSpendAbilityEffects(uint activator, int hitPointsSpent, int hitPointCost = 0)
        {
            if (hitPointsSpent <= 0)
                return;

            var window = Math.Max(1, _stat.GetStatAdjustment(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds));

            if (_stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendSoulSacrifice) > 0)
            {
                _statusEffectApplication.ApplyStatusEffect(activator, activator, typeof(SoulSacrificeStatusEffect), 30f);
            }

            if (_stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseSoulAscension) > 0)
            {
                _temporaryStatModifier.Replace(
                    activator,
                    StatType.HeavyVibrobladeOffenseSoulAscension,
                    1,
                    window,
                    StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds);
            }

            ApplyHitPointSpendStaminaRestore(activator, hitPointsSpent, hitPointCost);
            ApplyHitPointSpendTemporaryHitPoints(activator, hitPointsSpent);
        }

        private void ApplyHitPointSpendTemporaryHitPoints(uint activator, int hitPointsSpent)
        {
            if (hitPointsSpent <= 0)
                return;

            var percent = _stat.GetStatAdjustment(activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP);
            var duration = _stat.GetStatAdjustment(activator, StatType.HitPointSpendTemporaryHPDurationSeconds);
            if (percent <= 0 || duration <= 0)
                return;

            var temporaryHP = GameMath.PercentOf(hitPointsSpent, percent);
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP);
            TemporaryHitPointEffects.ApplyFlat(activator, "HIT_POINT_SPEND", temporaryHP, duration);
        }

        private void ApplyHitPointSpendStaminaRestore(uint activator, int hitPointsSpent, int hitPointCost)
        {
            var basePercent = _stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent);
            if (basePercent <= 0)
                return;

            var cooldown = _stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreCooldownSeconds);
            if (!_resources.TryUseStatTrigger(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent, cooldown))
                return;

            var percent = basePercent;
            var scalingAbility = GetAbilityTypeFromStatPlusOne(_stat.GetStatAdjustment(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                percent += Math.Max(0, GetAbilityScore(activator, scalingAbility));
            }

            var maximum = _stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreMaximumPercent);
            if (maximum > 0)
            {
                percent = Math.Min(maximum, percent);
            }

            _resources.RestoreStaminaFromStat(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent,
                CalculateHitPointSpendStaminaRestore(_stat.GetMaxStamina(activator), percent, hitPointsSpent, hitPointCost));
        }

        public void ApplyForceFPCostActivatedEffects(uint activator, AbilityDetail ability)
        {
            if (!_queries.IsForcePower(activator, ability) ||
                !ability.Requirements.OfType<AbilityRequirementFP>().Any(x => x.RequiredFP > 0))
            {
                return;
            }

            if (_stat.GetStatAdjustment(activator, StatType.ForcePrecognition) > 0 &&
                _resources.TryUseStatTrigger(activator, StatType.ForcePrecognition, 12))
            {
                _statusEffectApplication.ApplyStatusEffect(activator, activator, typeof(DangerSenseStatusEffect), 30f);
            }

            if (_stat.GetStatAdjustment(activator, StatType.ForceConvergence) > 0 &&
                _resources.TryUseStatTrigger(activator, StatType.ForceConvergence, 45))
            {
                _statusEffectApplication.ApplyStatusEffect(activator, activator, typeof(ForceConvergenceStatusEffect), 30f);
            }
        }

        public void ApplyAbilityUsedMasterAbilityHitChance(uint activator)
        {
            var master = GetMaster(activator);
            if (!GetIsObjectValid(master))
                return;

            var adjustment = _stat.GetStatAdjustment(activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds);
            if (adjustment == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                master,
                StatType.AbilityHitChancePercentAdjustment,
                adjustment,
                duration,
                StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment);
        }

        public void ApplyAbilityUsedRecastReduction(uint activator, AbilityDetail ability)
        {
            ApplyAbilityUsedRecastReduction(activator, ability?.RecastGroup ?? RecastGroup.Invalid);
        }

        private void ApplyAbilityUsedRecastReduction(uint activator, RecastGroup activatedRecastGroup)
        {
            var triggerGroup = GetRecastGroupFromStat(_stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionTriggerGroup));
            var secondaryTriggerGroup = GetRecastGroupFromStat(_stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionSecondaryTriggerGroup));
            if (activatedRecastGroup == RecastGroup.Invalid ||
                activatedRecastGroup != triggerGroup &&
                activatedRecastGroup != secondaryTriggerGroup)
                return;

            var targetGroup = GetRecastGroupFromStat(_stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionTargetGroup));
            var seconds = _stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionSeconds);
            if (targetGroup == RecastGroup.Invalid || seconds <= 0)
                return;

            _recast.ReduceRecastDelay(activator, targetGroup, seconds);
        }

        public void ApplyAbilityUsedNextSkillAutoAttackDamageBonus(uint activator, AbilityDetail ability)
        {
            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillAutoAttackDamageBonusTriggerSkillType));
            var abilitySkillType = _queries.GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            var targetSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillAutoAttackDamageBonusSkillType));
            var damageBonus = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonus);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillAutoAttackDamageWindowSeconds);
            _attackTiming.GrantNextSkillAutoAttackDamageBonus(activator, targetSkillType, damageBonus, duration);
        }

        public void ApplyAbilityUsedNextSkillFPCostAdjustment(uint activator, AbilityDetail ability)
        {
            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillFPCostAdjustmentTriggerSkillType));
            var abilitySkillType = _queries.GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            var targetSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillFPCostAdjustmentSkillType));
            var adjustment = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillFPCostAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillFPCostAdjustmentWindowSeconds);
            _attackTiming.GrantNextAbilityFPCostAdjustment(activator, targetSkillType, adjustment, duration);
        }
    }
}
