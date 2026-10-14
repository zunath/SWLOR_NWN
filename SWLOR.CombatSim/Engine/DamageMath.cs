namespace SWLOR.CombatSim.Engine
{
    /// <summary>
    /// The game's damage-taken stages on whole points of damage, transcribed from Combat.ApplyPercentDamageAdjustment
    /// and Combat.ApplyCombinedDamageTakenAdjustment. Update these when those change.
    /// </summary>
    public static class DamageMath
    {
        /// <summary>The game's Combat.MaximumNormalDamageReductionPercent.</summary>
        public const int MaximumNormalReductionPercent = 95;

        /// <summary>The game's Combat.MaximumCombinedDamageReductionPercent.</summary>
        public const int MaximumCombinedReductionPercent = 85;

        /// <summary>A Leadership or typed cut: the rounded-up share is subtracted, keeping at least 1.</summary>
        public static double ApplyPercentCut(double damage, double cutPercent)
        {
            if (damage <= 0 || cutPercent <= 0)
                return damage;

            var adjustment = -Math.Min(cutPercent, MaximumNormalReductionPercent);
            return Math.Max(1, damage + Math.Ceiling(damage * (adjustment / 100.0)));
        }

        /// <summary>The summed generic cut: the result rounds up, and no cut leaves less than 15% of the hit or 1 point.</summary>
        public static double ApplyGenericCut(double damage, double cutPercent)
        {
            if (damage <= 0)
                return damage;

            var multiplier = 1.0 - Math.Min(Math.Max(0, cutPercent), MaximumNormalReductionPercent) / 100.0;
            multiplier = Math.Max((100 - MaximumCombinedReductionPercent) / 100.0, multiplier);
            return Math.Max(1, Math.Ceiling(damage * multiplier - 1e-9));
        }
    }
}
