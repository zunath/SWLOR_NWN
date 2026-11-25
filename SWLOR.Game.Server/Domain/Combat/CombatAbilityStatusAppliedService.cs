using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The status-applied part of <c>Combat</c>: the listener of the status effects applied and refreshed notices (the target stamina drain
    /// and the hostile debuff party buff), the riders a status applied to the target sets off, the target status effects of a hostile
    /// ability hit, and the temporary stat replacement they use.
    /// </summary>
    public class CombatAbilityStatusAppliedService : ICombatAbilityStatusAppliedService, IStatusEffectAppliedListener
    {
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IAbilityImpactState _impactState;
        private readonly ICombatRecorderService _combatRecorder;

        public CombatAbilityStatusAppliedService(
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier,
            IAbilityImpactState impactState,
            ICombatRecorderService combatRecorder)
        {
            _resources = resources;
            _attackTiming = attackTiming;
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
            _impactState = impactState;
            _combatRecorder = combatRecorder;
        }

        /// <summary>
        /// Gives the status effects this service as a listener of their applied notices. A reload runs boot handlers that rerun
        /// before it adopts the carried status effects, so the list is full before the first notice.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void RegisterStatusEffectListener()
        {
            _statusEffect.RegisterAppliedListener(this);
        }

        /// <summary>A status effect was applied or refreshed: the target loses the stamina the source's stat drains.</summary>
        public void OnStatusApplied(uint source, uint creature, StatusEffectCategory categories) =>
            ApplyStatusAppliedTargetStaminaDrain(source, creature, categories);

        /// <summary>A hostile debuff was applied or refreshed: the source's party buff for a hostile ability applies.</summary>
        public void OnHostileDebuffApplied(uint source, AbilityDetail ability) =>
            ApplyHostileAbilityPartyBuff(source, ability, 0, true);

        /// <summary>Nothing to do when an application completes.</summary>
        public void OnApplicationCompleted(uint source, uint creature, Type statusEffectType, AbilityDetail ability)
        {
        }

        public void ApplyHostileAbilityPartyBuff(uint activator, AbilityDetail ability, int damage, bool statusApplied)
        {
            if (!IsSuccessfulHostileAbilityImpact(ability, damage, statusApplied))
                return;

            var baseDamage = _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffDamagePercent);
            var baseAccuracy = _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffAccuracyPercent);
            var baseDuration = _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffCooldownSeconds);
            var nameStrRef = _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffNameStrRef);
            var icon = (EffectIconType)_stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffIcon);
            if ((baseDamage <= 0 && baseAccuracy <= 0) || baseDuration <= 0 || cooldown <= 0 ||
                nameStrRef <= 0 || icon == EffectIconType.Invalid)
                return;

            var sequence = _impactState.GetAbilityImpactSequence(activator);
            if (sequence != null && !sequence.TryTriggerPartyBuff())
                return;
            if (!_resources.TryUseStatTrigger(activator, StatType.HostileAbilityPartyBuffDamagePercent, cooldown))
                return;

            var damagePercent = AbilityEffectScaling.ScaleValueBySourceSocial(activator, baseDamage,
                _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffMaximumDamagePercent));
            var accuracyPercent = AbilityEffectScaling.ScaleValueBySourceSocial(activator, baseAccuracy,
                _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffMaximumAccuracyPercent));
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = LeadershipAbilityEffects.ApplyLeadershipCommandDurationBonus(activator, baseDuration);

            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, activator, true, radius))
            {
                _statusEffectApplication.ApplyStatusEffect(activator, friendly,
                    new HostileAbilityPartyBuffStatusEffect(damagePercent, accuracyPercent, nameStrRef, icon), duration);
            }
        }

        public void ApplyStatusAppliedEffects(
            uint activator,
            uint target,
            StatusEffectCategory appliedStatusCategories,
            bool targetWasAttackingAlly)
        {
            var requiredCategory = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedRequiredCategory));
            if (requiredCategory == 0 ||
                (appliedStatusCategories & requiredCategory) == 0)
            {
                return;
            }

            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextSkillAbilitySkillType));
            var damageBonus = _stat.GetStatAdjustment(activator, StatType.StatusAppliedNextSkillAbilityDamageBonus);
            var criticalRate = _stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextSkillAbilityCriticalRatePercentAdjustment);
            var window = _stat.GetStatAdjustment(activator, StatType.StatusAppliedNextSkillAbilityWindowSeconds);
            _attackTiming.GrantNextSkillAbilityBonuses(activator, skillType, damageBonus, criticalRate, window);

            var nextAttackDamage = _stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextAttackDamageBonus);
            var nextAttackWindow = _stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextAttackWindowSeconds);
            if (nextAttackWindow > 0)
            {
                var wasArmed = _temporaryStatModifier.GetStatAdjustment(
                    activator,
                    StatType.NextAttackStatusAppliedDMGBonus,
                    StatType.NextAttackStatusAppliedDMGBonus) != 0;
                _attackTiming.ShowReadyCue(activator, wasArmed, nextAttackDamage, ReadyCue.Damage("Charged Blows", nextAttackDamage));
            }

            _attackTiming.GrantStatusAppliedNextAttackDamageBonus(activator, nextAttackDamage, nextAttackWindow);

            ApplyStatusAppliedSelfEffects(activator);
            ApplyStatusAppliedTargetEffects(activator, target);
            if (targetWasAttackingAlly)
                ApplyStatusAppliedTargetAttackingAllyEnmity(activator, target);
        }

        private void ApplyStatusAppliedTargetAttackingAllyEnmity(uint activator, uint target)
        {
            var enmity = _stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedTargetAttackingAllyEnmityDurationSeconds);
            if (enmity <= 0 || duration <= 0 || !GetIsObjectValid(target))
                return;

            // Challenge is the shared "more Enmity toward its source" status. Never replace a stronger one
            // the activator already put on this target, such as Guardian's Challenge.
            var existing = _statusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .Where(effect => effect is ChallengeEnmityStatusEffect &&
                                 effect.Source == activator &&
                                 !effect.IsFlaggedForRemoval)
                .Select(effect => effect.StatGroup.Stats.GetValueOrDefault(StatType.EnmityToStatusSourcePercentAdjustment))
                .DefaultIfEmpty(0)
                .Max();
            if (existing > enmity)
                return;

            _statusEffectApplication.ApplyStatusEffect(activator, target, new ChallengeEnmityStatusEffect(enmity), duration);
        }

        private void ApplyStatusAppliedSelfEffects(uint activator)
        {
            var duration = _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfDurationSeconds);
            if (duration <= 0)
                return;

            ReplaceTemporaryStat(
                activator,
                _stat.GetGrantedDeflectionStatType(StatType.StatusAppliedSelfAttackDeflection),
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfAttackDeflection),
                duration,
                StatType.StatusAppliedSelfAttackDeflection);
            ReplaceTemporaryStat(
                activator,
                StatType.PhysicalDefensePercentAdjustment,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfDefensePercentAdjustment),
                duration,
                StatType.StatusAppliedSelfDefensePercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.EvasionPercentAdjustment,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfEvasionPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfEvasionPercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.ForceAttackPercentAdjustment,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfForceAttackPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfForceAttackPercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.AttackDelayReductionPercent,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfHastePercentAdjustment),
                duration,
                StatType.StatusAppliedSelfHastePercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.EnmityPercentAdjustment,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfEnmityPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfEnmityPercentAdjustment);

            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfStaminaRestore);
            if (staminaRestore > 0)
                _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.StatusAppliedSelfStaminaRestore, staminaRestore);
        }

        private void ApplyStatusAppliedTargetEffects(uint activator, uint target)
        {
            if (!GetIsObjectValid(target))
                return;

            var duration = _stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetDurationSeconds);
            if (duration <= 0)
                return;

            ReplaceTemporaryStat(
                target,
                StatType.PhysicalDefensePercentAdjustment,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment),
                duration,
                StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment);
            ReplaceTemporaryStat(
                target,
                StatType.AccuracyPercentAdjustment,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetAccuracyPercentAdjustment),
                duration,
                StatType.StatusAppliedTargetAccuracyPercentAdjustment);
        }

        private void ReplaceTemporaryStat(
            uint target,
            StatType statType,
            int amount,
            int durationSeconds,
            StatType group)
        {
            if (statType == StatType.Invalid || amount == 0 || durationSeconds <= 0)
                return;

            _temporaryStatModifier.Replace(target, statType, amount, durationSeconds, group);
        }

        public void ApplyAbilityTargetStatusEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility || !GetIsObjectValid(target))
                return;

            var requiredCategory = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityTargetStatusRequiredCategory));
            if (requiredCategory == 0 || !_statusEffect.HasStatusEffectCategory(target, requiredCategory))
                return;

            var physicalDefense = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityTargetStatusDurationSeconds);
            ReplaceTemporaryStat(
                target,
                StatType.PhysicalDefensePercentAdjustment,
                physicalDefense,
                duration,
                StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment);
        }

        public void ApplyStatusAppliedTargetStaminaDrain(
            uint activator,
            uint target,
            StatusEffectCategory appliedCategories)
        {
            if (!GetIsObjectValid(activator) ||
                !GetIsObjectValid(target) ||
                activator == target)
                return;

            var requiredCategory = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedTargetStaminaDrainRequiredCategory));
            var staminaDrain = _stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetStaminaDrain);
            var cooldown = _stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetStaminaDrainCooldownSeconds);
            if (requiredCategory == 0 ||
                staminaDrain <= 0 ||
                cooldown <= 0 ||
                (appliedCategories & requiredCategory) == 0 ||
                !_resources.TryUseStatTrigger(activator, StatType.StatusAppliedTargetStaminaDrain, cooldown))
            {
                return;
            }

            var staminaBefore = _stat.GetCurrentStamina(target);
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(target, StatType.StatusAppliedTargetStaminaDrain);
            _stat.ReduceStamina(target, staminaDrain);
            var staminaDrained = Math.Max(0, staminaBefore - _stat.GetCurrentStamina(target));
            FloatingTextStringOnCreature(
                ColorToken.Combat($"-{staminaDrained} STM"),
                target,
                false);
        }
    }
}
