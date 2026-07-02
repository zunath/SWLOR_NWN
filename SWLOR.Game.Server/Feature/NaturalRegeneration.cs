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
            ApplyLowResourceIntervalRestore(player);

            if (tick >= 5) // 6 seconds * 5 = 30 seconds
            {
                var vitality = Math.Max(0, GetAbilityScore(player, AbilityType.Vitality));
                var willpower = Math.Max(0, GetAbilityScore(player, AbilityType.Willpower));
                var might = Math.Max(0, GetAbilityScore(player, AbilityType.Might));

                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);
                var hpRegen = dbPlayer.HPRegen + vitality + Stat.GetStatAdjustment(player, StatType.HPRegen);
                var fpRegen = 1 + dbPlayer.FPRegen + willpower / 4 + Stat.GetStatAdjustment(player, StatType.FPRegen);
                var stmRegen = 1 + dbPlayer.STMRegen + might / 4 + Stat.GetStatAdjustment(player, StatType.StaminaRegen);

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

        private static void ApplyLowResourceIntervalRestore(uint player)
        {
            var threshold = Stat.GetStatAdjustment(player, StatType.LowFPAndStaminaIntervalThresholdPercent);
            if (threshold <= 0 || !Combat.IsCurrentFPAndStaminaAtOrBelowPercent(player, threshold))
                return;

            var fpRestore = Stat.GetStatAdjustment(player, StatType.LowFPAndStaminaIntervalFPRestore);
            var staminaRestore = Stat.GetStatAdjustment(player, StatType.LowFPAndStaminaIntervalStaminaRestore);
            if (fpRestore <= 0 && staminaRestore <= 0)
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));

            if (fpRestore > 0)
                Stat.RestoreFP(player, fpRestore, dbPlayer);

            if (staminaRestore > 0)
                Stat.RestoreStamina(player, staminaRestore, dbPlayer);
        }
    }
}
