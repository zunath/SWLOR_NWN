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

        public static float StatProgress(int stat)
        {
            var clamped = Math.Clamp(stat, ScalingBaselineStat, ScalingCapStat);
            return (clamped - ScalingBaselineStat) / (float)(ScalingCapStat - ScalingBaselineStat);
        }

        /// <summary>
        /// Scales by the healer's Willpower: in the game, +0 at 10 up to +25% at 26. A formula whose baseline multiplier
        /// is below 1 cuts heals from low Willpower instead.
        /// </summary>
        public static int ScaleByStat(int baseAmount, int stat, HealFormula? formula = null)
        {
            if (baseAmount <= 0)
                return baseAmount;

            var f = formula ?? HealFormula.Game;
            var multiplier = f.StatMultiplierAtBaseline + (f.StatMultiplierAtCap - f.StatMultiplierAtBaseline) * StatProgress(stat);
            return baseAmount + (int)Math.Ceiling(baseAmount * (multiplier - 1f));
        }

        public static float SocialScaledPercent(float minimum, float maximum, int social)
        {
            if (maximum <= minimum)
                return minimum;

            return minimum + (float)Math.Round((maximum - minimum) * StatProgress(social), MidpointRounding.AwayFromZero);
        }

        public static float AffinityMultiplier(int affinity, float cap) =>
            Math.Clamp(1f + 0.05f * affinity, 0.5f, cap);

        /// <summary>The game's CalculateOutgoingAbilityHealingAmount: only a bonus counts; zero or less changes nothing.</summary>
        public static int AddOutgoingPercent(int amount, float percent) =>
            amount <= 0 || percent <= 0f ? amount : amount + (int)Math.Ceiling(amount * (percent / 100f));

        /// <summary>
        /// The game's ApplyHealingReceivedAdjustment: a bonus or a cut of Ceiling(amount x percent), never below 1 HP.
        /// </summary>
        public static int AddReceivedPercent(int amount, float percent)
        {
            if (amount <= 0 || percent == 0f)
                return amount;

            var delta = (int)Math.Ceiling(amount * (Math.Abs(percent) / 100f));
            return Math.Max(1, percent > 0f ? amount + delta : amount - delta);
        }

        public static int AddPercent(int amount, float percent)
        {
            if (amount <= 0 || percent == 0f)
                return amount;

            return amount + (int)Math.Ceiling(amount * (percent / 100f));
        }

        public static int PercentOfMaxHp(int maxHp, float percent) =>
            Math.Max(1, (int)Math.Ceiling(maxHp * (percent / 100f)));

        /// <summary>
        /// A heal's percent read as HP. The game reads all of it from the target's max HP; a formula with a flat share
        /// reads that share from its reference max HP instead, the same for every target.
        /// </summary>
        public static int HealBase(int targetMaxHp, float percent, HealFormula? formula = null)
        {
            var f = formula ?? HealFormula.Game;
            if (f.PercentOfMaxHpShare >= 1f)
                return PercentOfMaxHp(targetMaxHp, percent);

            // Same float arithmetic as PercentOfMaxHp, so a target at the reference max HP heals exactly as in the game.
            var sizedFrom = f.ReferenceMaxHp + f.PercentOfMaxHpShare * (targetMaxHp - f.ReferenceMaxHp);
            return Math.Max(1, (int)Math.Ceiling(sizedFrom * (percent / 100f)));
        }

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
            float combatReadinessPercent,
            HealFormula? formula = null)
        {
            var percent = effect.Percent;
            if (!targetIsCaster)
                percent *= effect.AllyMultiplier;
            if (effect.LowHpThresholdPercent > 0 && targetHpPercent <= effect.LowHpThresholdPercent)
                percent *= effect.LowHpMultiplier;

            var amount = HealBase(targetMaxHp, percent, formula);
            return Finish(effect, amount, casterStat, casterAffinity, affinityCap, outgoingHealingPercent, combatReadinessPercent, formula);
        }

        /// <summary>
        /// One tick of a heal over time. Affinity is captured when cast and, as in the game, scales the effect's
        /// percent before each tick rounds it and adds Willpower; ticks never get Combat Readiness.
        /// </summary>
        public static int HealOverTimeTick(
            AbilityEffect effect,
            int targetMaxHp,
            int casterStat,
            float affinityMultiplierAtCast,
            float outgoingHealingPercent,
            HealFormula? formula = null)
        {
            var amount = HealBase(targetMaxHp, effect.Percent * affinityMultiplierAtCast / Math.Max(1, effect.Ticks), formula);
            amount = ScaleByStat(amount, casterStat, formula);
            return AddOutgoingPercent(amount, outgoingHealingPercent);
        }

        public static int PartyPulse(
            AbilityEffect effect,
            int targetMaxHp,
            int casterStat,
            int casterAffinity,
            float affinityCap,
            float outgoingHealingPercent,
            float combatReadinessPercent,
            HealFormula? formula = null)
        {
            var amount = HealBase(targetMaxHp, effect.Percent, formula);
            return Finish(effect, amount, casterStat, casterAffinity, affinityCap, outgoingHealingPercent, combatReadinessPercent, formula);
        }

        public static int TemporaryHp(
            AbilityEffect effect,
            int targetMaxHp,
            int casterWillpower,
            int casterSocial,
            int casterAffinity,
            float affinityCap,
            float combatReadinessPercent,
            HealFormula? formula = null)
        {
            var percent = effect.Scaling == ScalingStat.Social
                ? SocialScaledPercent(effect.Percent, effect.MaximumPercent, casterSocial)
                : effect.Percent;

            var amount = effect.Flat + HealBase(targetMaxHp, percent, formula);
            if (effect.Scaling == ScalingStat.Willpower)
                amount = ScaleByStat(amount, casterWillpower, formula);
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
            float combatReadinessPercent,
            HealFormula? formula)
        {
            if (effect.Scaling == ScalingStat.Willpower)
                amount = ScaleByStat(amount, casterStat, formula);
            if (effect.UsesForceAffinity)
                amount = (int)Math.Round(amount * AffinityMultiplier(casterAffinity, affinityCap), MidpointRounding.AwayFromZero);
            amount = AddOutgoingPercent(amount, outgoingHealingPercent);
            if (effect.UsesCombatReadiness)
                amount = AddPercent(amount, combatReadinessPercent);

            return amount;
        }
    }
}
