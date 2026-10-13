using SWLOR.CombatSim.Effects;

namespace SWLOR.CombatSim.Engine
{
    /// <summary>
    /// Heal and shield size formulas, mirroring AbilityEffectScaling, Stat healing adjustments and
    /// Force affinity in SWLOR.Game.Server.
    /// </summary>
    public static class HealingMath
    {
        private const int ScalingBaselineStat = 10;
        private const int ScalingCapStat = 26;
        private const float MaximumStatBonus = 0.25f;

        public static float StatProgress(int stat)
        {
            var clamped = Math.Clamp(stat, ScalingBaselineStat, ScalingCapStat);
            return (clamped - ScalingBaselineStat) / (float)(ScalingCapStat - ScalingBaselineStat);
        }

        public static int ScaleByStat(int baseAmount, int stat)
        {
            if (baseAmount <= 0)
                return baseAmount;

            return baseAmount + (int)Math.Ceiling(baseAmount * MaximumStatBonus * StatProgress(stat));
        }

        public static float SocialScaledPercent(float minimum, float maximum, int social)
        {
            if (maximum <= minimum)
                return minimum;

            return minimum + (float)Math.Round((maximum - minimum) * StatProgress(social), MidpointRounding.AwayFromZero);
        }

        public static float AffinityMultiplier(int affinity, float cap) =>
            Math.Clamp(1f + 0.05f * affinity, 0.5f, cap);

        public static int AddPercent(int amount, float percent)
        {
            if (amount <= 0 || percent == 0f)
                return amount;

            return amount + (int)Math.Ceiling(amount * (percent / 100f));
        }

        public static int PercentOfMaxHp(int maxHp, float percent) =>
            Math.Max(1, (int)Math.Ceiling(maxHp * (percent / 100f)));

        /// <summary>
        /// Size of one direct heal before the recipient's healing-received bonus and PvP rules.
        /// </summary>
        public static int DirectHeal(
            AbilityEffect effect,
            int targetMaxHp,
            float targetHpPercent,
            bool targetIsCaster,
            int casterStat,
            int casterAffinity,
            float affinityCap,
            float outgoingHealingPercent,
            float combatReadinessPercent)
        {
            var percent = effect.Percent;
            if (!targetIsCaster)
                percent *= effect.AllyMultiplier;
            if (effect.LowHpThresholdPercent > 0 && targetHpPercent <= effect.LowHpThresholdPercent)
                percent *= effect.LowHpMultiplier;

            var amount = PercentOfMaxHp(targetMaxHp, percent);
            return Finish(effect, amount, casterStat, casterAffinity, affinityCap, outgoingHealingPercent, combatReadinessPercent);
        }

        /// <summary>
        /// One tick of a heal over time. Affinity is captured when cast; ticks never get Combat Readiness.
        /// </summary>
        public static int HealOverTimeTick(
            AbilityEffect effect,
            int targetMaxHp,
            int casterStat,
            float affinityMultiplierAtCast,
            float outgoingHealingPercent)
        {
            var amount = PercentOfMaxHp(targetMaxHp, effect.Percent / Math.Max(1, effect.Ticks));
            amount = ScaleByStat(amount, casterStat);
            amount = (int)Math.Round(amount * affinityMultiplierAtCast);
            return effect.UsesOutgoingHealing ? AddPercent(amount, outgoingHealingPercent) : amount;
        }

        public static int PartyPulse(
            AbilityEffect effect,
            int targetMaxHp,
            int casterStat,
            int casterAffinity,
            float affinityCap,
            float outgoingHealingPercent,
            float combatReadinessPercent)
        {
            var amount = PercentOfMaxHp(targetMaxHp, effect.Percent);
            return Finish(effect, amount, casterStat, casterAffinity, affinityCap, outgoingHealingPercent, combatReadinessPercent);
        }

        public static int TemporaryHp(
            AbilityEffect effect,
            int targetMaxHp,
            int casterWillpower,
            int casterSocial,
            int casterAffinity,
            float affinityCap,
            float combatReadinessPercent)
        {
            var percent = effect.Scaling == ScalingStat.Social
                ? SocialScaledPercent(effect.Percent, effect.MaximumPercent, casterSocial)
                : effect.Percent;

            var amount = effect.Flat + PercentOfMaxHp(targetMaxHp, percent);
            if (effect.Scaling == ScalingStat.Willpower)
                amount = ScaleByStat(amount, casterWillpower);
            if (effect.UsesForceAffinity)
                amount = (int)Math.Round(amount * AffinityMultiplier(casterAffinity, affinityCap), MidpointRounding.AwayFromZero);
            if (effect.UsesCombatReadiness)
                amount = AddPercent(amount, combatReadinessPercent);

            return amount;
        }

        private static int Finish(
            AbilityEffect effect,
            int amount,
            int casterStat,
            int casterAffinity,
            float affinityCap,
            float outgoingHealingPercent,
            float combatReadinessPercent)
        {
            if (effect.Scaling == ScalingStat.Willpower)
                amount = ScaleByStat(amount, casterStat);
            if (effect.UsesForceAffinity)
                amount = (int)Math.Round(amount * AffinityMultiplier(casterAffinity, affinityCap), MidpointRounding.AwayFromZero);
            if (effect.UsesOutgoingHealing)
                amount = AddPercent(amount, outgoingHealingPercent);
            if (effect.UsesCombatReadiness)
                amount = AddPercent(amount, combatReadinessPercent);

            return amount;
        }
    }
}
