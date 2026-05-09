using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public static class NaturalRegeneration
    {
        /// <summary>
        /// On module heartbeat, process a player's HP/FP/STM regeneration.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public static void ProcessRegeneration()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var tick = GetLocalInt(player, "NATURAL_REGENERATION_TICK") + 1;
            if (tick >= 5) // 6 seconds * 5 = 30 seconds
            {
                var vitalityBonus = GetAbilityModifier(AbilityType.Vitality, player);
                if (vitalityBonus < 0)
                    vitalityBonus = 0;

                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);
                var hpRegen = dbPlayer.HPRegen + vitalityBonus * 4 + Stat.GetStatAdjustment(player, StatType.HPRegen);
                var fpRegen = 1 + dbPlayer.FPRegen + vitalityBonus / 2 + Stat.GetStatAdjustment(player, StatType.FPRegen);
                var stmRegen = 1 + dbPlayer.STMRegen + vitalityBonus / 2 + Stat.GetStatAdjustment(player, StatType.StaminaRegen);

                if (hpRegen > 0 && GetCurrentHitPoints(player) < GetMaxHitPoints(player))
                {
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(hpRegen), player);
                }

                if (fpRegen > 0)
                {
                    Stat.RestoreFP(player, fpRegen, dbPlayer);
                }

                if (stmRegen > 0)
                {
                    Stat.RestoreStamina(player, stmRegen, dbPlayer);
                }

                tick = 0;
            }

            SetLocalInt(player, "NATURAL_REGENERATION_TICK", tick);
        }
    }
}
