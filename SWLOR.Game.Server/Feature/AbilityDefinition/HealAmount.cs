using System;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    /// <summary>
    /// The size of a player heal or shield before stat scaling: a flat amount plus a percent of a creature's max HP.
    /// Most of each heal is flat so that a large HP pool does not also multiply the healing it receives.
    /// </summary>
    public readonly record struct HealAmount(int Flat, float PercentOfMaxHp)
    {
        /// <summary>The max HP a flat amount made from a percent is sized for.</summary>
        public const int ReferenceMaxHitPoints = 600;

        /// <summary>Share of a former percent heal that became flat; the rest stays a percent of max HP.</summary>
        public const float FlatShare = 0.75f;

        /// <summary>A heal sized only from max HP, as revives are.</summary>
        public static HealAmount Percent(float percentOfMaxHp) => new(0, percentOfMaxHp);

        /// <summary>
        /// The size of a perk heal or shield whose stat still holds a percent: three quarters of that percent of
        /// <see cref="ReferenceMaxHitPoints"/> as flat HP, plus the remaining quarter as a percent of max HP. A rate of
        /// 12 gives 54 HP plus 3%.
        /// </summary>
        public static HealAmount FromRate(float ratePercent)
        {
            if (ratePercent <= 0f)
                return default;

            var flat = (int)Math.Floor(ratePercent * (double)ReferenceMaxHitPoints * FlatShare / 100.0 + 0.5);
            return new HealAmount(flat, ratePercent * (1f - FlatShare));
        }

        public int ForMaxHitPoints(int maxHitPoints, float multiplier = 1f)
        {
            return Math.Max(1, (int)Math.Ceiling((Flat + Math.Max(0, maxHitPoints) * (PercentOfMaxHp / 100f)) * multiplier));
        }

        /// <summary>
        /// A size between <paramref name="minimum"/> and <paramref name="maximum"/>, such as a Leadership shield that
        /// SOC raises; <paramref name="progress"/> runs from 0 to 1.
        /// </summary>
        public static HealAmount Between(HealAmount minimum, HealAmount maximum, float progress)
        {
            if (progress <= 0f)
                return minimum;
            if (progress >= 1f)
                return maximum;

            var flat = minimum.Flat + (int)Math.Round((maximum.Flat - minimum.Flat) * progress, MidpointRounding.AwayFromZero);
            var percent = minimum.PercentOfMaxHp + (maximum.PercentOfMaxHp - minimum.PercentOfMaxHp) * progress;
            return new HealAmount(flat, percent);
        }
    }
}
