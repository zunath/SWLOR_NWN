using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static partial class Combat
    {
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void ApplyDefeatedEnemyEffects()
        {
            var defeated = OBJECT_SELF;
            var killer = GetLastKiller();

            if (GetIsObjectValid(killer) && killer != defeated)
            {
                ApplyDefeatedEnemyEffects(killer, defeated);
            }

            ApplyRecentDamagerDefeatedEnemyEffects(defeated);
            RemoveStatTriggerCooldowns(defeated);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearStatTriggerState()
        {
            var creature = GetExitingObject();
            if (!GetIsObjectValid(creature))
                return;

            RemoveStatTriggerCooldowns(creature);
        }

        private static void RemoveStatTriggerCooldowns(uint creature)
        {
            ForgetResourceTriggerState(creature);
            ForgetActivityState(creature);
            ForgetDamageTakenState(creature);
            ForgetSuppressionState(creature);
            ForgetActivationState(creature);
            ForgetHitEffectState(creature);
            ForgetImpactState(creature);
            ForgetOpeningActivityState(creature);
            ForgetAbilityStaminaCostState(creature);
            ForgetAttackTimingState(creature);
            ForgetDamageModifierState(creature);
            TemporaryStatModifier.Clear(creature);
        }

        public static void RefreshStatDrivenTrackerEffects(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            ReconcileOverwatchStatus(creature);
            ScheduleIdleReadinessRefresh(creature);

            if (Stat.GetStatAdjustment(creature, StatType.OpeningAutoAttackIdleSeconds) <= 0)
                StatusEffect.RemoveStatusEffect(creature, typeof(OpeningAttackReadyStatusEffect), false);
            if (!HasConfiguredIdleSkillAbilityChannel(creature))
                StatusEffect.RemoveStatusEffect(creature, typeof(IdleSkillAbilityReadyStatusEffect), false);

            if (Stat.GetStatAdjustment(creature, StatType.RangedRepeatedTargetDamageBonusPerHit) <= 0 ||
                Stat.GetStatAdjustment(creature, StatType.RangedRepeatedTargetDamageBonusMax) <= 0 ||
                Stat.GetStatAdjustment(creature, StatType.RangedRepeatedTargetDamageDurationSeconds) <= 0)
            {
                ClearRangedRepeatedTargetDamageTracker(creature);
            }

            if (Stat.GetStatAdjustment(creature, StatType.RangedAutoAttackCycleCriticalRateRequiredCount) <= 0 ||
                Stat.GetStatAdjustment(creature, StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment) <= 0)
            {
                ClearAutoAttackCycleCriticalRateTracker(creature);
            }

            if (Stat.GetStatAdjustment(
                    creature,
                    StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment) <= 0)
            {
                TemporaryStatModifier.Consume(
                    creature,
                    StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment,
                    StatType.PersistentNextSkillAbilityCriticalRateSkillType);
                TemporaryStatModifier.Consume(
                    creature,
                    StatType.PersistentNextSkillAbilityCriticalRateSkillType,
                    StatType.PersistentNextSkillAbilityCriticalRateSkillType);
                TemporaryStatModifier.Consume(
                    creature,
                    StatType.PersistentNextSkillAbilityCriticalRateRangedOnly,
                    StatType.PersistentNextSkillAbilityCriticalRateSkillType);
                StatusEffect.RemoveStatusEffect(
                    creature,
                    typeof(CriticalRateStackTrackerStatusEffect),
                    false);
            }
        }

        /// <summary>
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
        /// </summary>
        [NWNEventHandler(ScriptName.OnIntervalPC6Seconds)]
        public static void ClearCombatState()
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
