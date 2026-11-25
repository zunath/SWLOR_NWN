using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What a critical hit sets off for the attacker and the target (stamina and HP restores, the sequence reward, the next-ability
    /// grants, the target debuffs and the bleeding extension), and the riders of a non-critical ability. The critical sequence table
    /// is this instance's own field, cleared through <see cref="ICombatCriticalHitEffectsService.ForgetCriticalHitSequenceState"/>.
    /// </summary>
    public class CombatCriticalHitEffectsService : ICombatCriticalHitEffectsService
    {
        private readonly Dictionary<uint, CriticalHitSequenceState> _criticalHitSequenceStates = new();

        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly ICombatAbilityHitAdjustmentService _abilityHitAdjustment;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly IStatusEffectDurationService _statusEffectDuration;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IAbilityImpactState _impactState;

        public CombatCriticalHitEffectsService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            ICombatAbilityHitAdjustmentService abilityHitAdjustment,
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            IStatusEffectDurationService statusEffectDuration,
            ITemporaryStatModifierService temporaryStatModifier,
            IAbilityImpactState impactState)
        {
            _queries = queries;
            _resources = resources;
            _attackTiming = attackTiming;
            _abilityHitAdjustment = abilityHitAdjustment;
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _statusEffectDuration = statusEffectDuration;
            _temporaryStatModifier = temporaryStatModifier;
            _impactState = impactState;
        }

        private sealed class CriticalHitSequenceState
        {
            public int Count { get; init; }
            public DateTime LastHit { get; init; }
        }

        public void ApplyCriticalHitEffects(
            uint attacker,
            uint defender,
            int damage,
            int criticalRating,
            bool isSingleTargetImpact = false,
            SkillType skillType = SkillType.Invalid)
        {
            if (criticalRating <= 0)
                return;

            // Limited Haste is earned by the critical result itself, even when mitigation reduces
            // that critical's damage to zero. Damage-derived critical riders remain below.
            ApplyCriticalHitLimitedHaste(attacker, skillType);

            if (damage <= 0)
                return;

            if (_abilityHitAdjustment.GetCriticalRateAgainstSunderedTargetAdjustment(attacker, defender) > 0)
            {
                FloatingTextStringOnCreature(ColorToken.Combat("Weak Points"), attacker, false);
            }

            var staminaRestore = _stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestore);
            var staminaRestoreSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestoreSkillType));
            var staminaRestoreCooldown = _stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestoreCooldownSeconds);
            if (staminaRestore > 0 &&
                SkillTypeMatches(skillType, staminaRestoreSkillType) &&
                _resources.TryUseStatTrigger(attacker, StatType.CriticalStaminaRestore, staminaRestoreCooldown))
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalStaminaRestore, staminaRestore);
            }

            ApplyCriticalNextAbilityDamageBonus(attacker, skillType);
            ApplyCriticalNextSkillAbilityDefenseIgnore(attacker, skillType);
            ApplyCriticalNextAutoAttackNoDelay(attacker, skillType);
            ApplyCriticalSideAttackStaminaRestore(attacker, defender);
            ApplyCriticalBleedingStatusDurationExtension(attacker, defender);
            ApplyCriticalHitSequenceEffects(attacker);
            ApplyCriticalHitSelfEffects(attacker);

            var poisonedTargetStaminaRestore = _stat.GetStatAdjustment(attacker, StatType.CriticalPoisonedTargetStaminaRestore);
            if (poisonedTargetStaminaRestore > 0 && _statusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalPoisonedTargetStaminaRestore, poisonedTargetStaminaRestore);
            }

            var markedTargetStaminaRestore = _stat.GetStatAdjustment(attacker, StatType.CriticalMarkedTargetStaminaRestore);
            if (markedTargetStaminaRestore > 0 && _statusEffect.HasStatusEffect(defender, typeof(MarkingTossStatusEffect), attacker))
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalMarkedTargetStaminaRestore, markedTargetStaminaRestore);
            }

            var targetFPLossPercent = _stat.GetStatAdjustment(attacker, StatType.CriticalTargetFPLossPercentOfDamage);
            if (targetFPLossPercent > 0)
            {
                var fpLoss = GameMath.PercentOf(damage, targetFPLossPercent);
                _resources.ReduceFPFromStat(defender, StatType.CriticalTargetFPLossPercentOfDamage, fpLoss);
            }

            var targetStaminaLossPercent = _stat.GetStatAdjustment(attacker, StatType.CriticalTargetStaminaLossPercentOfDamage);
            if (targetStaminaLossPercent > 0)
            {
                var staminaLoss = GameMath.PercentOf(damage, targetStaminaLossPercent);
                _resources.ReduceStaminaFromStat(defender, StatType.CriticalTargetStaminaLossPercentOfDamage, staminaLoss);
            }

            var hpRestorePercent = _stat.GetStatAdjustment(attacker, StatType.CriticalHPPercentOfDamageRestore);
            var hpRestoreCooldown = _stat.GetStatAdjustment(attacker, StatType.CriticalHPPercentOfDamageRestoreCooldownSeconds);
            if (hpRestorePercent > 0 &&
                _resources.TryUseStatTrigger(attacker, StatType.CriticalHPPercentOfDamageRestore, hpRestoreCooldown))
            {
                _resources.ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent,
                    passiveHealingCooldownSeconds: hpRestoreCooldown);
            }

            var accuracyPercent = _stat.GetStatAdjustment(attacker, StatType.CriticalAccuracyPercentAdjustment);
            var accuracyDuration = _stat.GetStatAdjustment(attacker, StatType.CriticalAccuracyDurationSeconds);
            if (accuracyPercent != 0 && accuracyDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    attacker,
                    StatType.AccuracyPercentAdjustment,
                    accuracyPercent,
                    accuracyDuration,
                    StatType.CriticalAccuracyPercentAdjustment);
            }

            var targetEvasionPercent = _stat.GetStatAdjustment(attacker, StatType.CriticalTargetEvasionPercentAdjustment);
            var targetEvasionDuration = _stat.GetStatAdjustment(attacker, StatType.CriticalTargetEvasionDurationSeconds);
            if (targetEvasionPercent != 0 && targetEvasionDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.EvasionPercentAdjustment,
                    targetEvasionPercent,
                    targetEvasionDuration,
                    StatType.CriticalTargetEvasionPercentAdjustment);
            }

            var targetDefensePercent = _stat.GetStatAdjustment(attacker, StatType.CriticalTargetDefensePercentAdjustment);
            var targetDefenseDuration = _stat.GetStatAdjustment(attacker, StatType.CriticalTargetDefenseDurationSeconds);
            if (skillType == SkillType.Staff)
            {
                targetDefensePercent += _stat.GetStatAdjustment(attacker, StatType.StaffCriticalTargetDefensePercentAdjustment);
                targetDefenseDuration = Math.Max(
                    targetDefenseDuration,
                    _stat.GetStatAdjustment(attacker, StatType.StaffCriticalTargetDefenseDurationSeconds));
            }
            if (targetDefensePercent != 0 && targetDefenseDuration > 0)
            {
                _statusEffectApplication.ApplyStatusEffect(
                    attacker,
                    defender,
                    new ExposedStatusEffect(targetDefensePercent),
                    targetDefenseDuration,
                    CombatDamageType.Physical);
            }

            if (isSingleTargetImpact)
            {
                ApplySingleTargetCriticalTargetDefenseEffect(attacker, defender);
            }
        }

        private void ApplyCriticalHitSelfEffects(uint attacker)
        {
            var evasion = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfEvasionPercentAdjustment);
            var evasionDuration = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfEvasionDurationSeconds);
            if (evasion != 0 && evasionDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    attacker,
                    StatType.EvasionPercentAdjustment,
                    evasion,
                    evasionDuration,
                    StatType.CriticalHitSelfEvasionPercentAdjustment);
            }

            var haste = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfHastePercentAdjustment);
            var hasteDuration = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfHasteDurationSeconds);
            if (haste != 0 && hasteDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    haste,
                    hasteDuration,
                    StatType.CriticalHitSelfHastePercentAdjustment);
            }
        }

        public void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType)
        {
            if (!GetIsObjectValid(activator) || skillType == SkillType.Invalid)
                return;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateSkillType));
            var criticalRate = _stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment);
            var maximum = _stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateMax);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateWindowSeconds);
            var rangedOnly = _stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateRangedOnly) > 0;
            if (!MatchesNextSkillAbilityCriticalRateSkill(skillType, requiredSkillType, rangedOnly) ||
                criticalRate <= 0 ||
                maximum <= 0 ||
                duration <= 0)
            {
                return;
            }

            _temporaryStatModifier.Replace(
                activator,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType,
                (int)requiredSkillType,
                duration,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            _temporaryStatModifier.Replace(
                activator,
                StatType.PersistentNextSkillAbilityCriticalRateRangedOnly,
                rangedOnly ? 1 : 0,
                duration,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            _temporaryStatModifier.AddCapped(
                activator,
                StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment,
                criticalRate,
                duration,
                maximum,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType,
                1,
                refreshExistingStacks: true);

            var currentTotal = _temporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            if (currentTotal > 0)
            {
                var trackerIcon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                    activator,
                    StatType.NonCriticalAbilityNextSkillAbilityCriticalRateTrackerEffectIconType));
                _statusEffect.RemoveStatusEffect(
                    activator,
                    typeof(CriticalRateStackTrackerStatusEffect),
                    false);
                if (trackerIcon != EffectIconType.Invalid)
                {
                    _statusEffectApplication.ApplyStatusEffect(
                        activator,
                        activator,
                        new CriticalRateStackTrackerStatusEffect(
                            $"Next ranged ability: +{currentTotal}% Critical Rate",
                            trackerIcon),
                        duration);
                }
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Next ranged ability +{currentTotal}% Critical Rate"),
                    activator,
                    false);
            }
        }

        private void ApplyCriticalBleedingStatusDurationExtension(uint attacker, uint defender)
        {
            var extensionSeconds = _stat.GetStatAdjustment(attacker, StatType.CriticalBleedingStatusDurationExtensionSeconds);
            if (extensionSeconds <= 0 ||
                !GetIsObjectValid(defender) ||
                !_statusEffect.HasStatusEffect(defender, typeof(BleedStatusEffect), attacker) &&
                !_statusEffect.HasStatusEffect(defender, typeof(HemorrhageStatusEffect), attacker))
            {
                return;
            }

            var cooldownSeconds = _stat.GetStatAdjustment(
                attacker,
                StatType.CriticalBleedingStatusDurationExtensionCooldownSeconds);
            if (!_resources.TryUseStatTrigger(attacker, StatType.CriticalBleedingStatusDurationExtensionSeconds, cooldownSeconds))
                return;

            _statusEffectDuration.ExtendStatusEffectDuration(defender, typeof(BleedStatusEffect), attacker, extensionSeconds);
            _statusEffectDuration.ExtendStatusEffectDuration(defender, typeof(HemorrhageStatusEffect), attacker, extensionSeconds);
        }

        private void ApplyCriticalHitSequenceEffects(uint attacker)
        {
            var requiredCount = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceCountRequired);
            var windowSeconds = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceWindowSeconds);
            var staminaRestore = _stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceStaminaRestore);
            if (requiredCount <= 0 || windowSeconds <= 0 || staminaRestore <= 0)
                return;

            var now = Clock.UtcNow;
            var count = 1;
            if (_criticalHitSequenceStates.TryGetValue(attacker, out var state) &&
                (now - state.LastHit).TotalSeconds <= windowSeconds)
            {
                count = state.Count + 1;
            }

            if (count >= requiredCount)
            {
                _criticalHitSequenceStates.Remove(attacker);
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalHitSequenceStaminaRestore, staminaRestore);
                return;
            }

            _criticalHitSequenceStates[attacker] = new CriticalHitSequenceState
            {
                Count = count,
                LastHit = now
            };
        }

        private void ApplyCriticalNextSkillAbilityDefenseIgnore(uint attacker, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return;

            var defenseIgnore = _stat.GetStatAdjustment(attacker, StatType.CriticalNextSkillAbilityDefenseIgnorePercentAdjustment);
            var duration = _stat.GetStatAdjustment(attacker, StatType.CriticalNextSkillAbilityDefenseIgnoreDurationSeconds);
            if (defenseIgnore <= 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilitySkillType,
                (int)skillType,
                duration,
                StatType.NextSkillAbilitySkillType);

            _temporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                defenseIgnore,
                duration,
                StatType.NextSkillAbilitySkillType);

            // The newest grant decides which abilities may spend the shared next-ability bonuses.
            _temporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilityRequiredCategory,
                0,
                duration,
                StatType.NextSkillAbilitySkillType);
        }

        private void ApplyCriticalNextAbilityDamageBonus(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusTriggerSkillType));
            if (!SkillTypeMatches(skillType, triggerSkillType))
                return;

            var perkType = GetPerkTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusPerkType));
            var bonus = _stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonus);
            var duration = _stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusCooldownSeconds);
            if (perkType == PerkType.Invalid || bonus == 0 || duration <= 0)
                return;

            if (_resources.TryUseStatTrigger(attacker, StatType.CriticalNextAbilityDamageBonus, cooldown))
            {
                _attackTiming.GrantNextAbilityDamageBonus(attacker, perkType, bonus, duration);
            }
        }

        private void ApplyCriticalHitLimitedHaste(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHasteTriggerSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, triggerSkillType))
                return;

            var hastePercent = _stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHastePercentAdjustment);
            var duration = _stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHasteDurationSeconds);
            var attackCount = _stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHasteAttackCount);
            var statusEffectIcon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.CriticalHitLimitedHasteStatusEffectIcon));
            if (hastePercent <= 0 ||
                duration <= 0 ||
                attackCount <= 0 ||
                statusEffectIcon == EffectIconType.Invalid)
            {
                return;
            }

            _statusEffectApplication.ApplyStatusEffect(
                attacker,
                attacker,
                new LimitedHasteStatusEffect(
                    hastePercent,
                    attackCount,
                    SkillType.Invalid,
                    statusEffectIcon,
                    _impactState.GetActiveAbilityImpactSummary(attacker)),
                duration);
        }

        private void ApplyCriticalNextAutoAttackNoDelay(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayTriggerSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, triggerSkillType))
                return;

            var noDelaySkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelaySkillType));
            var duration = _stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayCooldownSeconds);
            if (duration <= 0)
                return;

            if (_resources.TryUseStatTrigger(attacker, StatType.CriticalNextAutoAttackNoDelaySkillType, cooldown))
            {
                if (noDelaySkillType == SkillType.Invalid)
                    _attackTiming.GrantNextAutoAttackNoDelay(attacker, duration);
                else
                    _attackTiming.GrantNextAutoAttackNoDelay(attacker, noDelaySkillType, duration);
            }
        }

        private void ApplyCriticalSideAttackStaminaRestore(uint attacker, uint defender)
        {
            var chance = _stat.GetStatAdjustment(attacker, StatType.CriticalSideAttackStaminaRestoreChance);
            var staminaRestore = _stat.GetStatAdjustment(attacker, StatType.CriticalSideAttackStaminaRestore);
            if (chance <= 0 || staminaRestore <= 0 || !_queries.IsAttackerBesideTarget(attacker, defender))
                return;

            if (Random.D100(1) <= chance)
            {
                _resources.RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalSideAttackStaminaRestore, staminaRestore);
            }
        }

        private void ApplySingleTargetCriticalTargetDefenseEffect(uint attacker, uint defender)
        {
            var targetDefensePercent = _stat.GetStatAdjustment(attacker, StatType.SingleTargetCriticalTargetDefensePercentAdjustment);
            var targetDefenseDuration = _stat.GetStatAdjustment(attacker, StatType.SingleTargetCriticalTargetDefenseDurationSeconds);
            if (targetDefensePercent == 0 || targetDefenseDuration <= 0)
                return;

            _statusEffectApplication.ApplyStatusEffect(
                attacker,
                defender,
                new ExposedStatusEffect(targetDefensePercent),
                targetDefenseDuration,
                CombatDamageType.Physical);
        }

        public void ForgetCriticalHitSequenceState(uint creature)
        {
            _criticalHitSequenceStates.Remove(creature);
        }
    }
}
