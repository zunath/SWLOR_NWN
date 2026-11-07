using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The stat type metadata and the rating formulas, with no engine, database or service behind them. The instance
    /// forwards to these, and code with no container calls them directly (<c>SWLOR.CombatSim</c>, <c>SWLOR.CraftSim</c>).
    /// </summary>
    public static class StatRules
    {
        private const float DefenseSkillMultiplier = 1.2f;

        /// <summary>
        /// Builds the stat type metadata now rather than on its first read.
        /// </summary>
        public static void LoadStatTypeMetadata() => _ = StatTypeMetadataTable.Entries.Count;

        public static StatTypeCategory GetStatTypeCategory(StatType statType)
        {
            return StatTypeMetadataTable.Entries.TryGetValue(statType, out var attribute)
                ? attribute.Category
                : StatTypeCategory.NonBeneficial;
        }

        public static StatTypeAggregation GetStatTypeAggregation(StatType statType)
        {
            return StatTypeMetadataTable.Entries.TryGetValue(statType, out var attribute)
                ? attribute.Aggregation
                : StatTypeAggregation.Additive;
        }

        public static DeflectionSource GetStatTypeDeflectionSource(StatType statType)
        {
            return StatTypeMetadataTable.Entries.TryGetValue(statType, out var attribute)
                ? attribute.DeflectionSource
                : DeflectionSource.None;
        }

        public static StatType GetGrantedDeflectionStatType(StatType sourceStatType)
        {
            return GetStatTypeDeflectionSource(sourceStatType) switch
            {
                DeflectionSource.Melee => StatType.MeleeDeflection,
                DeflectionSource.Ranged => StatType.RangedDeflection,
                DeflectionSource.Shield => StatType.ShieldDeflection,
                _ => StatType.Invalid
            };
        }

        public static int AggregateStatAdjustment(StatType statType, int current, int adjustment)
        {
            return GetStatTypeAggregation(statType) switch
            {
                StatTypeAggregation.BitwiseOr => current | adjustment,
                StatTypeAggregation.Maximum => Math.Max(current, adjustment),
                _ => current + adjustment
            };
        }

        public static bool IsBeneficialStatAdjustment(StatType statType, int value)
        {
            if (value == 0)
                return false;

            return GetStatTypeCategory(statType) switch
            {
                StatTypeCategory.BeneficialWhenPositive => value > 0,
                StatTypeCategory.BeneficialWhenNegative => value < 0,
                _ => false
            };
        }

        /// <summary>
        /// Retrieves the raw attack based on the level, stat, and any bonuses.
        /// </summary>
        /// <param name="level">The level (NPC or skill)</param>
        /// <param name="stat">The raw stat points</param>
        /// <param name="bonus">The amount of bonus attack or force attack</param>
        /// <returns></returns>
        public static int GetAttack(int level, int stat, int bonus)
        {
            return 8 + (2 * level) + stat + bonus;
        }

        public static int CalculateDefense(int defenderStat, int skillLevel, int defenseBonus)
        {
            return (int)(8 + (skillLevel * DefenseSkillMultiplier) + defenderStat + defenseBonus);
        }

        /// <summary>
        /// Gets the calculated accuracy for a given level, stat, and bonus.
        /// </summary>
        /// <param name="level">The level (skill/NPC)</param>
        /// <param name="stat">The raw accuracy stat amount</param>
        /// <param name="bonus">The amount of bonus accuracy.</param>
        /// <returns>The calculated accuracy result.</returns>
        public static int GetAccuracy(int level, int stat, int bonus)
        {
            return 8 + (2 * level) + stat + bonus;
        }

        /// <summary>
        /// Gets the evasion based on level, stat, and bonuses.
        /// </summary>
        /// <param name="level">The level (skill/NPC)</param>
        /// <param name="stat">The raw agility stat</param>
        /// <param name="bonus">The amount of bonus evasion</param>
        /// <returns></returns>
        public static int GetEvasion(int level, int stat, int bonus)
        {
            return 8 + (2 * level) + stat + bonus;
        }
    }
}
