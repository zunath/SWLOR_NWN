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

        /// <summary>The share of a hit a cut leaves, after the game's 95% limit on one cut.</summary>
        public static double CutMultiplier(double cutPercent) =>
            1.0 - Math.Min(Math.Max(0, cutPercent), MaximumNormalReductionPercent) / 100.0;

        /// <summary>
        /// The summed generic cut on damage that already carries <paramref name="statusMultiplier"/> from typed status
        /// cuts: the result rounds up, and the two stages together never leave less than 15% of the hit or 1 point.
        /// </summary>
        public static double ApplyGenericCut(double damage, double cutPercent, double statusMultiplier = 1.0)
        {
            if (damage <= 0)
                return damage;

            var combined = Math.Max((100 - MaximumCombinedReductionPercent) / 100.0, statusMultiplier * CutMultiplier(cutPercent));
            return Math.Max(1, Math.Ceiling(damage * combined / statusMultiplier - 1e-9));
        }
    }
}
