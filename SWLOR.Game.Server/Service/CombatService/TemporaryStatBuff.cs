using System;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>
    /// Applies a timed stat buff together with the visible status effect that represents it.
    ///
    /// The status effect is display-only: the bonus itself lives in <see cref="TemporaryStatModifier"/>,
    /// so the paired effect must leave its StatGroup empty or the buff would be counted twice.
    /// Use this instead of calling <see cref="TemporaryStatModifier"/> directly whenever the buff is
    /// something the player should be able to see on their status bar.
    /// </summary>
    public static class TemporaryStatBuff
    {
        public static void Replace(
            uint creature,
            StatType statType,
            int amount,
            float durationSeconds,
            StatType groupStatType,
            Func<int, IStatusEffect> statusEffectFactory)
        {
            if (amount == 0 || durationSeconds <= 0f)
                return;

            TemporaryStatModifier.Replace(creature, statType, amount, durationSeconds, groupStatType);
            ApplyStatus(creature, statusEffectFactory, amount, durationSeconds);
        }

        public static void Replace(
            uint creature,
            StatType statType,
            int amount,
            float durationSeconds,
            string group,
            Func<int, IStatusEffect> statusEffectFactory)
        {
            if (amount == 0 || durationSeconds <= 0f)
                return;

            TemporaryStatModifier.Replace(creature, statType, amount, durationSeconds, group);
            ApplyStatus(creature, statusEffectFactory, amount, durationSeconds);
        }

        public static void Add(
            uint creature,
            StatType statType,
            int amount,
            float durationSeconds,
            StatType groupStatType,
            Func<int, IStatusEffect> statusEffectFactory)
        {
            if (amount == 0 || durationSeconds <= 0f)
                return;

            TemporaryStatModifier.Add(creature, statType, amount, durationSeconds, groupStatType);
            ApplyStatus(
                creature,
                statusEffectFactory,
                TemporaryStatModifier.GetStatAdjustment(creature, statType, groupStatType),
                durationSeconds);
        }

        /// <summary>
        /// Stacking variant. The status effect is built from the running total rather than the
        /// increment, so a stacking buff shows its current strength rather than the last stack added.
        /// Returns the number of stacks actually applied.
        /// </summary>
        public static int AddCapped(
            uint creature,
            StatType statType,
            int amount,
            float durationSeconds,
            int maxTotal,
            StatType groupStatType,
            int requestedStacks,
            Func<int, IStatusEffect> statusEffectFactory,
            bool refreshExistingStacks = false)
        {
            var stacks = TemporaryStatModifier.AddCapped(
                creature,
                statType,
                amount,
                durationSeconds,
                maxTotal,
                groupStatType,
                requestedStacks,
                refreshExistingStacks);

            var total = TemporaryStatModifier.GetStatAdjustment(creature, statType, groupStatType);
            if (total > 0)
            {
                ApplyStatus(creature, statusEffectFactory, total, durationSeconds);
            }

            return stacks;
        }

        private static void ApplyStatus(
            uint creature,
            Func<int, IStatusEffect> statusEffectFactory,
            int magnitude,
            float durationSeconds)
        {
            var statusEffect = statusEffectFactory?.Invoke(magnitude);
            if (statusEffect == null)
                return;

            StatusEffect.ApplyStatusEffect(creature, creature, statusEffect, durationSeconds);
        }
    }
}
