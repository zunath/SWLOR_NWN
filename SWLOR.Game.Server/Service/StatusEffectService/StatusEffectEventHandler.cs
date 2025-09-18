using System;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// Event handlers for the new StatusEffectService system
    /// </summary>
    public static class StatusEffectEventHandler
    {

        /// <summary>
        /// When a player enters the server, apply the NWN effect system
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnPlayerEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            StatusEffectService.OnPlayerEnter(GetModule());
        }

        /// <summary>
        /// When a player exits the server, clean up their status effects
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void OnPlayerExit()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            // Clean up any status effects for the player
            // This is handled by the service's internal cleanup
        }

        /// <summary>
        /// Process status effects on the SWLOR heartbeat
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public static void OnHeartbeat()
        {
            StatusEffectService.ProcessHeartbeat();
        }

        /// <summary>
        /// When a player dies, remove all status effects
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnPlayerDeath()
        {
            var player = GetLastPlayerDied();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            StatusEffectService.RemoveAllStatusEffects(player);
        }

        /// <summary>
        /// Handle damage events for OnHit status effects
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public static void OnCreatureDamaged()
        {
            var attacker = GetLastDamager();
            if (!GetIsObjectValid(attacker))
                return;

            StatusEffectService.OnCreatureDealtDamage(attacker);
        }

        /// <summary>
        /// Handle when a creature attacks for OnHit effects
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAttackAfter)]
        public static void OnCreatureAttack()
        {
            var attacker = GetLastAttacker();
            if (!GetIsObjectValid(attacker))
                return;

            StatusEffectService.OnCreatureDealtDamage(attacker);
        }

        /// <summary>
        /// Handle when a status effect is applied (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnApplyStatusEffect)]
        public static void OnApplyStatusEffect()
        {
            var player = GetEnteringObject();
            StatusEffectService.OnApplyNWNStatusEffect(player);
        }

        /// <summary>
        /// Handle when a status effect is removed (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnRemoveStatusEffect)]
        public static void OnRemoveStatusEffect()
        {
            var player = GetEnteringObject();
            StatusEffectService.OnRemoveNWNStatusEffect(player);
        }

        /// <summary>
        /// Handle status effect interval processing (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnStatusEffectInterval)]
        public static void OnStatusEffectInterval()
        {
            var creature = GetEnteringObject();
            StatusEffectService.OnNWNStatusEffectInterval(creature);
        }
    }
}
