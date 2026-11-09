using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.StatService;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What is left of <c>Combat</c> once its parts are split out: the death, exit and six-second handlers, the cleanup that
    /// forgets a creature's combat state in every part in a fixed order, the refresh of the stat-driven trackers, and the
    /// damage roll that draws from the shared <c>Random</c>.
    /// It holds no state; every table it clears belongs to a part.
    /// </summary>
    public class CombatService : ICombatService
    {
        private readonly ICombatResourcesService _resources;
        private readonly ICombatSuppressionService _suppression;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly ICombatDamageModifiersService _damageModifiers;
        private readonly ICombatHitEffectsService _hitEffects;
        private readonly ICombatDamageTakenService _damageTaken;
        private readonly ICombatAbilityActivationService _abilityActivation;
        private readonly ICombatAbilityImpactService _abilityImpact;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;

        public CombatService(
            ICombatResourcesService resources,
            ICombatSuppressionService suppression,
            ICombatActivityService combatActivity,
            ICombatAttackTimingService attackTiming,
            ICombatDamageModifiersService damageModifiers,
            ICombatHitEffectsService hitEffects,
            ICombatDamageTakenService damageTaken,
            ICombatAbilityActivationService abilityActivation,
            ICombatAbilityImpactService abilityImpact,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier)
        {
            _resources = resources;
            _suppression = suppression;
            _combatActivity = combatActivity;
            _attackTiming = attackTiming;
            _damageModifiers = damageModifiers;
            _hitEffects = hitEffects;
            _damageTaken = damageTaken;
            _abilityActivation = abilityActivation;
            _abilityImpact = abilityImpact;
            _stat = stat;
            _statusEffect = statusEffect;
            _temporaryStatModifier = temporaryStatModifier;
        }

        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public void ApplyDefeatedEnemyEffects()
        {
            var defeated = OBJECT_SELF;
            var killer = GetLastKiller();

            if (GetIsObjectValid(killer) && killer != defeated)
            {
                _hitEffects.ApplyDefeatedEnemyEffects(killer, defeated);
            }

            _hitEffects.ApplyRecentDamagerDefeatedEnemyEffects(defeated);
            RemoveStatTriggerCooldowns(defeated);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ClearStatTriggerState()
        {
            var creature = GetExitingObject();
            if (!GetIsObjectValid(creature))
                return;

            RemoveStatTriggerCooldowns(creature);
        }

        private void RemoveStatTriggerCooldowns(uint creature)
        {
            _resources.ForgetResourceTriggerState(creature);
            _combatActivity.ForgetActivityState(creature);
            _damageTaken.ForgetDamageTakenState(creature);
            _suppression.ForgetSuppressionState(creature);
            _abilityActivation.ForgetActivationState(creature);
            _hitEffects.ForgetHitEffectState(creature);
            _abilityImpact.ForgetImpactState(creature);
            _combatActivity.ForgetOpeningActivityState(creature);
            _resources.ForgetAbilityStaminaCostState(creature);
            _attackTiming.ForgetAttackTimingState(creature);
            _damageModifiers.ForgetDamageModifierState(creature);
            _temporaryStatModifier.Clear(creature);
        }

        public void RefreshStatDrivenTrackerEffects(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            _suppression.ReconcileOverwatchStatus(creature);
            _combatActivity.ScheduleIdleReadinessRefresh(creature);

            if (_stat.GetStatAdjustment(creature, StatType.OpeningAutoAttackIdleSeconds) <= 0)
                _statusEffect.RemoveStatusEffect(creature, typeof(OpeningAttackReadyStatusEffect), false);
            if (!_combatActivity.HasConfiguredIdleSkillAbilityChannel(creature))
                _statusEffect.RemoveStatusEffect(creature, typeof(IdleSkillAbilityReadyStatusEffect), false);

            if (_stat.GetStatAdjustment(creature, StatType.RangedRepeatedTargetDamageBonusPerHit) <= 0 ||
                _stat.GetStatAdjustment(creature, StatType.RangedRepeatedTargetDamageBonusMax) <= 0 ||
                _stat.GetStatAdjustment(creature, StatType.RangedRepeatedTargetDamageDurationSeconds) <= 0)
            {
                _damageModifiers.ClearRangedRepeatedTargetDamageTracker(creature);
            }

            if (_stat.GetStatAdjustment(creature, StatType.RangedAutoAttackCycleCriticalRateRequiredCount) <= 0 ||
                _stat.GetStatAdjustment(creature, StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment) <= 0)
            {
                _damageModifiers.ClearAutoAttackCycleCriticalRateTracker(creature);
            }

            if (_stat.GetStatAdjustment(
                    creature,
                    StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment) <= 0)
            {
                _temporaryStatModifier.Consume(
                    creature,
                    StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment,
                    StatType.PersistentNextSkillAbilityCriticalRateSkillType);
                _temporaryStatModifier.Consume(
                    creature,
                    StatType.PersistentNextSkillAbilityCriticalRateSkillType,
                    StatType.PersistentNextSkillAbilityCriticalRateSkillType);
                _temporaryStatModifier.Consume(
                    creature,
                    StatType.PersistentNextSkillAbilityCriticalRateRangedOnly,
                    StatType.PersistentNextSkillAbilityCriticalRateSkillType);
                _statusEffect.RemoveStatusEffect(
                    creature,
                    typeof(CriticalRateStackTrackerStatusEffect),
                    false);
            }
        }

        /// <summary>
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
        /// </summary>
        [NWNEventHandler(ScriptName.OnIntervalPC6Seconds)]
        public void ClearCombatState()
        {
            uint player = OBJECT_SELF;

            // Clear combat state.
            if (!GetIsInCombat(player))
            {
                DeleteLocalFloat(player, "ATTACK_ORIENTATION_X");
                DeleteLocalFloat(player, "ATTACK_ORIENTATION_Y");
            }
        }

        // Not a rule of CombatRules: it draws from the shared Random generator, so it is not pure.
        /// <summary>
        /// Calculates a random damage amount based on the provided stats of the attacker and defender.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's raw defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <param name="deltaCap">Value to cap the lower and upper bounds of stat delta to. For weapons, should be weapon rank.</param>
        /// <returns>A damage value to apply to the target.</returns>
        public int CalculateDamage(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            var (minDamage, maxDamage) = CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                critical,
                deltaCap);

            return (int)Random.NextFloat(minDamage, maxDamage);
        }
    }
}
