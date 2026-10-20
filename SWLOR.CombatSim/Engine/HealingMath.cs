using SWLOR.CombatSim.Effects;
using SWLOR.Game.Server.Feature.AbilityDefinition;

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
        /// Scales by the healer's Willpower: x0.625 at 10 up to x1.25 at 26 in the game; +0 to +25% before the tuning.
        /// </summary>
        public static int ScaleByStat(int baseAmount, int stat, HealFormula? formula = null)
        {
            if (baseAmount <= 0)
                return baseAmount;

            var f = formula ?? HealFormula.PercentHeals;
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
            var multiplier = 1f;
            if (!targetIsCaster)
                multiplier *= effect.AllyMultiplier;
            if (effect.LowHpThresholdPercent > 0 && targetHpPercent <= effect.LowHpThresholdPercent)
                multiplier *= effect.LowHpMultiplier;

            // Before the tuning the multipliers scaled the percent itself, which rounds differently.
            var amount = (formula ?? HealFormula.PercentHeals).FlatHeals
                ? effect.Heal.ForMaxHitPoints(targetMaxHp, multiplier)
                : PercentOfMaxHp(targetMaxHp, effect.Percent * multiplier);
            return Finish(effect, amount, casterStat, casterAffinity, affinityCap, outgoingHealingPercent, combatReadinessPercent, formula);
        }

        /// <summary>
        /// One tick of a heal over time. Affinity is captured when cast and, as in the game, scales each tick's size
        /// before it rounds and adds Willpower; ticks never get Combat Readiness.
        /// </summary>
        public static int HealOverTimeTick(
            AbilityEffect effect,
            int targetMaxHp,
            int casterStat,
            float affinityMultiplierAtCast,
            float outgoingHealingPercent,
            HealFormula? formula = null)
        {
            var amount = (formula ?? HealFormula.PercentHeals).FlatHeals
                ? effect.Heal.ForMaxHitPoints(targetMaxHp, affinityMultiplierAtCast)
                : PercentOfMaxHp(targetMaxHp, effect.Percent * affinityMultiplierAtCast / Math.Max(1, effect.Ticks));
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
            var amount = (formula ?? HealFormula.PercentHeals).FlatHeals
                ? effect.Heal.ForMaxHitPoints(targetMaxHp)
                : PercentOfMaxHp(targetMaxHp, effect.Percent);
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
            int amount;
            if ((formula ?? HealFormula.PercentHeals).FlatHeals)
            {
                var size = effect.Scaling == ScalingStat.Social
                    ? HealAmount.Between(effect.Heal, effect.HealAtSocialCap, StatProgress(casterSocial))
                    : effect.Heal;
                amount = size.ForMaxHitPoints(targetMaxHp);
            }
            else
            {
                var percent = effect.Scaling == ScalingStat.Social
                    ? SocialScaledPercent(effect.Percent, effect.MaximumPercent, casterSocial)
                    : effect.Percent;
                amount = effect.Flat + PercentOfMaxHp(targetMaxHp, percent);
            }

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
