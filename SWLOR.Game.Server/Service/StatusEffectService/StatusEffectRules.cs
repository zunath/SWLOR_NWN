using SWLOR.Game.Server.Service.PvpCombatService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// The pure rules of status durations. No state, no engine and no database, so code that has no container (the
    /// combat simulator) can call it directly. <c>StatusEffect</c> and the status effect service use it too.
    /// </summary>
    public static class StatusEffectRules
    {
        public const int MaximumHardCrowdControlDurationSeconds = 30;

        public static int GetOutgoingDurationPercentAdjustment(StatusEffectCategory categories, Func<StatType, int> getStat)
        {
            var adjustment = 0;
            if ((categories & StatusEffectCategory.Debuff) == StatusEffectCategory.Debuff)
            {
                adjustment += getStat(StatType.OutgoingDebuffDurationPercentAdjustment);
            }

            if ((categories & StatusEffectCategory.Control) == StatusEffectCategory.Control)
            {
                adjustment += getStat(StatType.OutgoingControlDurationPercentAdjustment);
            }

            if ((categories & StatusEffectCategory.ForceDisruption) == StatusEffectCategory.ForceDisruption)
            {
                adjustment += getStat(StatType.OutgoingForceDisruptionDurationPercentAdjustment);
            }

            if ((categories & StatusEffectCategory.AbilityDisruption) != 0)
                adjustment += getStat(StatType.OutgoingAbilityDisruptionDurationPercentAdjustment);

            return adjustment;
        }

        /// <summary>
        /// Apply the final control budget after outgoing duration bonuses and resistance.
        /// Self-applied system penalties, soft debuffs and permanent effects are unaffected.
        /// </summary>
        /// <param name="isPlayerAgainstPlayer">A player or a player's pet applied it to another player, so it lasts half as long.</param>
        public static int ClampHardCrowdControlDurationTicks(
            StatusEffectCategory categories, int ticks, float frequency, bool isSelfApplied = false, bool isPlayerAgainstPlayer = false)
        {
            if (isSelfApplied || ticks <= 0 || (categories & StatusEffectCategory.HardCrowdControl) == 0)
                return ticks;

            ticks = Math.Min(ticks, (int)Math.Floor(MaximumHardCrowdControlDurationSeconds / Math.Max(1f, frequency)));
            return isPlayerAgainstPlayer ? PvpCombatRules.ShortenHardCrowdControlTicks(ticks) : ticks;
        }

        public static float CalculateAdjustedStatusDurationSeconds(float seconds, int percentAdjustment)
        {
            return Math.Max(0.1f, seconds * (1f + percentAdjustment / 100f));
        }
    }
}
