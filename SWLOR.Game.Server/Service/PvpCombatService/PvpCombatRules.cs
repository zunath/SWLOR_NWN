using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PvpCombatService
{
    /// <summary>
    /// The long-fight rules for player-vs-player combat. These values are the single source for
    /// the game and the combat simulator.
    /// </summary>
    public static class PvpCombatRules
    {
        /// <summary>
        /// Seconds without PvP damage dealt or taken before a player's PvP combat ends and its
        /// clock starts over.
        /// </summary>
        public const float CombatWindowSeconds = 30f;

        /// <summary>
        /// Hard crowd control a player or a player's pet applies to a player lasts this percent of
        /// its normal duration.
        /// </summary>
        public const int HardCrowdControlDurationPercent = 50;

        /// <summary>
        /// Healing a player receives is cut by this percent once they have been in PvP combat this
        /// long. It stops at the last step, so healers always keep part of their healing.
        /// </summary>
        public static IReadOnlyList<PvpCombatRampStep> HealingReductionSteps { get; } = new[]
        {
            new PvpCombatRampStep(120f, 10),
            new PvpCombatRampStep(150f, 20),
            new PvpCombatRampStep(180f, 30),
            new PvpCombatRampStep(210f, 45),
            new PvpCombatRampStep(240f, 60)
        };

        /// <summary>
        /// Damage players and their pets deal to a player rises by this percent once that player
        /// has been in PvP combat this long.
        /// </summary>
        public static IReadOnlyList<PvpCombatRampStep> DamageIncreaseSteps { get; } = new[]
        {
            new PvpCombatRampStep(180f, 20),
            new PvpCombatRampStep(210f, 40),
            new PvpCombatRampStep(240f, 60),
            new PvpCombatRampStep(270f, 80),
            new PvpCombatRampStep(300f, 100)
        };

        /// <summary>The index of the latest step reached after <paramref name="seconds"/>, or -1 before the first.</summary>
        public static int GetStepIndex(IReadOnlyList<PvpCombatRampStep> steps, double seconds)
        {
            var index = -1;
            for (var step = 0; step < steps.Count; step++)
            {
                if (seconds >= steps[step].AtSeconds)
                    index = step;
            }

            return index;
        }

        /// <summary>The percent of the latest step reached after <paramref name="seconds"/>, or 0 before the first.</summary>
        public static int GetStepPercent(IReadOnlyList<PvpCombatRampStep> steps, double seconds)
        {
            var index = GetStepIndex(steps, seconds);
            return index < 0 ? 0 : steps[index].Percent;
        }

        /// <summary>A heal cut by <paramref name="percent"/>; the cut rounds up, as healing received does.</summary>
        public static int ReduceHealing(int amount, int percent)
        {
            if (amount <= 0 || percent <= 0)
                return amount;

            return Math.Max(0, amount - (int)Math.Ceiling(amount * (percent / 100.0)));
        }

        /// <summary>Damage raised by <paramref name="percent"/>; the increase rounds up, as damage taken does.</summary>
        public static int IncreaseDamage(int damage, int percent)
        {
            if (damage <= 0 || percent <= 0)
                return damage;

            return damage + (int)Math.Ceiling(damage * (percent / 100.0));
        }

        /// <summary>A hard crowd-control duration against a player, at least one tick.</summary>
        public static int ShortenHardCrowdControlTicks(int ticks)
        {
            if (ticks <= 0)
                return ticks;

            return Math.Max(1, (int)Math.Ceiling(ticks * (HardCrowdControlDurationPercent / 100.0)));
        }
    }
}
