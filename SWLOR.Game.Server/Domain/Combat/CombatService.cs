using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What is left of <c>Combat</c> once its parts are split out: the death, exit and six-second handlers, the cleanup that
    /// walks the <see cref="CombatForgetPhase"/> steps over every <see cref="ICombatCreatureStateOwner"/> to forget a creature's
    /// combat state, and the refresh of the stat-driven trackers. It holds no state; every table it clears belongs to a part.
    /// </summary>
    public class CombatService : ICombatService
    {
        private readonly ICombatCreatureStateOwner[] _stateOwners;
        private readonly ICombatSuppressionService _suppression;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatRateModifiersService _rateModifiers;
        private readonly ICombatRepeatedTargetService _repeatedTarget;
        private readonly ICombatDefeatedEnemyEffectsService _defeatedEnemyEffects;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;

        public CombatService(
            IEnumerable<ICombatCreatureStateOwner> stateOwners,
            ICombatSuppressionService suppression,
            ICombatActivityService combatActivity,
            ICombatRateModifiersService rateModifiers,
            ICombatRepeatedTargetService repeatedTarget,
            ICombatDefeatedEnemyEffectsService defeatedEnemyEffects,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier)
        {
            _stateOwners = stateOwners.ToArray();
            if (_stateOwners.Length == 0)
                throw new InvalidOperationException("No combat creature state owner is registered with the combat service.");

            _suppression = suppression;
            _combatActivity = combatActivity;
            _rateModifiers = rateModifiers;
            _repeatedTarget = repeatedTarget;
            _defeatedEnemyEffects = defeatedEnemyEffects;
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
                _defeatedEnemyEffects.ApplyDefeatedEnemyEffects(killer, defeated);
            }

            _defeatedEnemyEffects.ApplyRecentDamagerDefeatedEnemyEffects(defeated);
            RemoveStatTriggerCooldowns(defeated);
        }

        [PlayerSession(PlayerReload.LogoutOnly,
            "Forgets the player's combat trackers and takes pressure effects off others; the trackers go with the old build.")]
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
            for (var phase = CombatForgetPhase.ResourceTriggers; phase <= CombatForgetPhase.DamageModifiers; phase++)
            {
                foreach (var owner in _stateOwners)
                {
                    owner.ForgetCreatureState(creature, phase);
                }
            }

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
                _repeatedTarget.ClearRangedRepeatedTargetDamageTracker(creature);
            }

            if (_stat.GetStatAdjustment(creature, StatType.RangedAutoAttackCycleCriticalRateRequiredCount) <= 0 ||
                _stat.GetStatAdjustment(creature, StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment) <= 0)
            {
                _rateModifiers.ClearAutoAttackCycleCriticalRateTracker(creature);
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
    }
}
