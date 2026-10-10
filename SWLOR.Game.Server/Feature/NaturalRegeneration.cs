using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public static class NaturalRegeneration
    {
        public const int StaminaRegenHeartbeats = 5;
        public const int BaseStaminaRegenPerThirtySeconds = 10;
        private const string StaminaRegenRemainderVariable = "NATURAL_STAMINA_REGEN_REMAINDER";

        /// <summary>
        /// Distributes the thirty-second budget over six-second heartbeats without
        /// multiplying equipment/food bonuses or losing fractional recovery.
        /// </summary>
        public static int GetStaminaRegenPerHeartbeat(int might, int persistedRegen, int bonus, ref int remainder)
        {
            var budget = Math.Max(0, BaseStaminaRegenPerThirtySeconds + Math.Max(0, might) / 4 + persistedRegen + bonus);
            var accumulated = budget + Math.Clamp(remainder, 0, StaminaRegenHeartbeats - 1);
            remainder = accumulated % StaminaRegenHeartbeats;
            return accumulated / StaminaRegenHeartbeats;
        }

        /// <summary>
        /// On module heartbeat, process a player's HP/FP/STM regeneration.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public static void ProcessRegeneration()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) ||
                GetLocalInt(player, Stat.SuppressNaturalRegenVariable) != 0) return;

            var tick = GetLocalInt(player, "NATURAL_REGENERATION_TICK") + 1;
            ApplyLowResourceIntervalRestore(player);

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null) return;

            var might = Math.Max(0, GetAbilityScore(player, AbilityType.Might));
            var remainder = GetLocalInt(player, StaminaRegenRemainderVariable);
            var stmRegen = GetStaminaRegenPerHeartbeat(might, dbPlayer.STMRegen,
                Stat.GetStatAdjustment(player, StatType.StaminaRegen), ref remainder);
            SetLocalInt(player, StaminaRegenRemainderVariable, remainder);
            if (stmRegen > 0)
            {
                if (dbPlayer.Stamina == Stat.GetMaxStamina(player, dbPlayer))
                    ExecuteScript(ScriptName.OnPlayerStaminaAdjusted, player);
                else
                    Stat.RestoreStamina(player, stmRegen, dbPlayer, sendFeedback: false);
            }

            if (tick >= 5) // 6 seconds * 5 = 30 seconds
            {
                var vitality = Math.Max(0, GetAbilityScore(player, AbilityType.Vitality));
                var willpower = Math.Max(0, GetAbilityScore(player, AbilityType.Willpower));
                var hpRegen = dbPlayer.HPRegen + vitality + Stat.GetStatAdjustment(player, StatType.HPRegen);
                var fpRegen = 1 + dbPlayer.FPRegen + willpower / 4 + Stat.GetStatAdjustment(player, StatType.FPRegen);

                if (hpRegen > 0 && GetCurrentHitPoints(player) < GetMaxHitPoints(player))
                {
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(hpRegen), player);
                }

                if (fpRegen > 0)
                {
                    Stat.RestoreFP(player, fpRegen, dbPlayer, sendFeedback: false);
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
                Stat.RestoreFP(player, fpRestore, dbPlayer, sendFeedback: false);

            if (staminaRestore > 0)
                Stat.RestoreStamina(player, staminaRestore, dbPlayer, sendFeedback: false);
        }
    }
}
