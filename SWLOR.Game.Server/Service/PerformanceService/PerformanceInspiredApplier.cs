using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// Puts an Inspired status effect on a creature. The effect is rebuilt only when its composition changed;
    /// otherwise only its remaining time is refreshed, which is all a tick usually needs.
    /// </summary>
    public static class PerformanceInspiredApplier
    {
        /// <param name="creature">The watcher or performer who holds the effect; they are also its source.</param>
        /// <param name="data">The composed bonuses.</param>
        /// <param name="remainingSeconds">The remaining time to leave on the effect. It is never shorter than what the effect has now.</param>
        /// <param name="bankedAgain">True when the holder banked time this tick, which gives them a fresh Requiem.</param>
        /// <returns>True when a new effect was applied, false when an existing one was only refreshed.</returns>
        public static bool Apply(uint creature, InspiredEffectData data, double remainingSeconds, bool bankedAgain)
        {
            var seconds = (float)Math.Max(1.0, remainingSeconds);
            var existing = StatusEffect.GetStatusEffect<InspiredStatusEffect>(creature);
            if (existing != null && existing.Data.Equals(data))
            {
                StatusEffect.RefreshStatusEffectDuration(creature, typeof(InspiredStatusEffect), creature, seconds);
                if (bankedAgain)
                    existing.ResetRequiem();

                return false;
            }

            var effect = new InspiredStatusEffect(data);
            if (existing != null)
                effect.CarryStateFrom(existing, bankedAgain);

            return StatusEffect.ApplyStatusEffect(creature, creature, effect, seconds);
        }

        /// <summary>The seconds of Inspired the creature has right now, or 0 when they have none.</summary>
        public static double GetRemainingSeconds(uint creature, DateTime nowUtc)
        {
            var existing = StatusEffect.GetStatusEffect<InspiredStatusEffect>(creature);
            return existing == null ? 0.0 : existing.GetRemainingDurationSeconds(nowUtc);
        }

        /// <summary>Extends the creature's existing Inspired to a longer remaining time without changing what it grants.</summary>
        public static void SetRemaining(uint creature, double remainingSeconds)
        {
            StatusEffect.RefreshStatusEffectDuration(
                creature, typeof(InspiredStatusEffect), creature, (float)Math.Max(1.0, remainingSeconds));
        }
    }
}
