using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    /// <summary>
    /// The stats an enzyme rolls, with no engine calls. A hunted enzyme (<c>EnzymeLootTableDefinition</c>) rolls from every
    /// bracket: primary stats from 0.1% up to 10.0% and secondary stats up to 5.0%. A cultured one (<c>FermentationVats</c>)
    /// rolls with the same weights from the lowest brackets only. Both use <see cref="Roll"/>, so a cultured enzyme is a hunted
    /// enzyme with the best brackets cut off, never a different roll.
    /// </summary>
    public static class EnzymeRolls
    {
        /// <summary>The primary brackets: 0.1% to 1.0%, 1.1% to 2.0%, and so on up to 9.1% to 10.0%.</summary>
        public const int PrimaryBrackets = 10;

        /// <summary>The secondary brackets: 0.1% to 1.0% up to 4.1% to 5.0%.</summary>
        public const int SecondaryBrackets = 5;

        /// <summary>The highest primary bracket index (0 to 9) a hunted enzyme rolls.</summary>
        public const int HuntedMaxPrimaryBracket = PrimaryBrackets - 1;

        /// <summary>The highest secondary bracket index (0 to 4) a hunted enzyme rolls.</summary>
        public const int HuntedMaxSecondaryBracket = SecondaryBrackets - 1;

        private static readonly int[] _primaryWeights = { 70, 50, 20, 15, 10, 8, 6, 4, 2, 1 };
        private static readonly int[] _secondaryWeights = { 50, 20, 15, 10, 8 };

        private static readonly Dictionary<EnzymeType, IncubationStatType[]> _primaryStats = new()
        {
            [EnzymeType.Hydrolase] = new[]
            {
                IncubationStatType.LearningPurity,
                IncubationStatType.AttackPurity,
                IncubationStatType.TraumaResistancePurity,
                IncubationStatType.ElectricalResistancePurity,
                IncubationStatType.PhysicalDefensePurity
            },
            [EnzymeType.Isomerase] = new[]
            {
                IncubationStatType.XPPenalty,
                IncubationStatType.EvasionPurity,
                IncubationStatType.PoisonResistancePurity,
                IncubationStatType.FireResistancePurity,
                IncubationStatType.MindResistancePurity
            },
            [EnzymeType.Lyase] = new[]
            {
                IncubationStatType.MutationChance,
                IncubationStatType.AccuracyPurity,
                IncubationStatType.ForceDefensePurity,
                IncubationStatType.IceResistancePurity,
                IncubationStatType.MobilityResistancePurity
            },
        };

        private static readonly Dictionary<EnzymeType, IncubationStatType[]> _secondaryStats = new()
        {
            [EnzymeType.Hydrolase] = new[]
            {
                IncubationStatType.MutationChance,
                IncubationStatType.XPPenalty,
                IncubationStatType.EvasionPurity,
                IncubationStatType.PoisonResistancePurity,
                IncubationStatType.FireResistancePurity,
                IncubationStatType.MindResistancePurity,
                IncubationStatType.AccuracyPurity,
                IncubationStatType.ForceDefensePurity,
                IncubationStatType.IceResistancePurity,
                IncubationStatType.MobilityResistancePurity,
                IncubationStatType.DisruptionResistancePurity
            },
            [EnzymeType.Isomerase] = new[]
            {
                IncubationStatType.MutationChance,
                IncubationStatType.LearningPurity,
                IncubationStatType.AttackPurity,
                IncubationStatType.TraumaResistancePurity,
                IncubationStatType.ElectricalResistancePurity,
                IncubationStatType.PhysicalDefensePurity,
                IncubationStatType.AccuracyPurity,
                IncubationStatType.ForceDefensePurity,
                IncubationStatType.IceResistancePurity,
                IncubationStatType.MobilityResistancePurity,
                IncubationStatType.DisruptionResistancePurity
            },
            [EnzymeType.Lyase] = new[]
            {
                IncubationStatType.XPPenalty,
                IncubationStatType.LearningPurity,
                IncubationStatType.AttackPurity,
                IncubationStatType.TraumaResistancePurity,
                IncubationStatType.ElectricalResistancePurity,
                IncubationStatType.PhysicalDefensePurity,
                IncubationStatType.EvasionPurity,
                IncubationStatType.PoisonResistancePurity,
                IncubationStatType.FireResistancePurity,
                IncubationStatType.MindResistancePurity,
                IncubationStatType.DisruptionResistancePurity
            },
        };

        /// <summary>The weights of the primary brackets, lowest first.</summary>
        public static IReadOnlyList<int> PrimaryWeights => _primaryWeights;

        /// <summary>The weights of the secondary brackets, lowest first.</summary>
        public static IReadOnlyList<int> SecondaryWeights => _secondaryWeights;

        /// <summary>The stats an enzyme of the type rolls as primary stats (up to 2 of them).</summary>
        public static IReadOnlyList<IncubationStatType> GetPrimaryStats(EnzymeType type) => _primaryStats[type];

        /// <summary>The stats an enzyme of the type rolls as secondary stats (1 to 3 of them).</summary>
        public static IReadOnlyList<IncubationStatType> GetSecondaryStats(EnzymeType type) => _secondaryStats[type];

        /// <summary>The lowest amount, in tenths of a percent, a bracket rolls: 1, 11, 21 and so on.</summary>
        public static int GetBracketLow(int bracket) => bracket * 10 + 1;

        /// <summary>
        /// The highest amount a bracket rolls: 9, 19, 29 and so on. The upper end of each range is exclusive, as it always was
        /// for hunted enzymes, so the third primary bracket tops out at 2.9% and the second secondary one at 1.9%.
        /// </summary>
        public static int GetBracketHigh(int bracket) => bracket * 10 + 9;

        /// <summary>A random amount from the bracket.</summary>
        public static int RollBracketValue(int bracket) => Random.Next(GetBracketLow(bracket), GetBracketHigh(bracket) + 1);

        /// <summary>
        /// Rolls an enzyme of the type: one primary bracket and one secondary bracket by their weights (only the brackets up to the
        /// maximums), 0 to 2 primary stats and 1 to 3 secondary stats, each a different stat of the type's lists with an amount from
        /// the bracket. The primary stats come first in the result.
        /// </summary>
        /// <param name="maxPrimaryBracket">The highest primary bracket (0 to 9) that can come up.</param>
        /// <param name="maxSecondaryBracket">The highest secondary bracket (0 to 4) that can come up.</param>
        public static IReadOnlyList<EnzymeStatRoll> Roll(EnzymeType type, int maxPrimaryBracket, int maxSecondaryBracket)
        {
            if (!_primaryStats.ContainsKey(type))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Not an enzyme type.");

            var primaryBracket = Random.GetRandomWeightedIndex(GetWeights(_primaryWeights, maxPrimaryBracket));
            var secondaryBracket = Random.GetRandomWeightedIndex(GetWeights(_secondaryWeights, maxSecondaryBracket));
            var primaryCount = Random.Next(0, 3);
            var secondaryCount = 1 + Random.Next(0, 3);

            var rolls = new List<EnzymeStatRoll>();
            Add(rolls, _primaryStats[type].ToList(), primaryCount, primaryBracket);
            Add(rolls, _secondaryStats[type].ToList(), secondaryCount, secondaryBracket);
            return rolls;
        }

        private static int[] GetWeights(int[] weights, int maxBracket) =>
            weights.Take(Math.Clamp(maxBracket, 0, weights.Length - 1) + 1).ToArray();

        private static void Add(List<EnzymeStatRoll> rolls, List<IncubationStatType> stats, int count, int bracket)
        {
            for (var stat = 1; stat <= count; stat++)
            {
                if (stats.Count <= 0)
                    break;

                var amount = RollBracketValue(bracket);
                var index = Random.Next(stats.Count);
                rolls.Add(new EnzymeStatRoll(stats[index], amount));
                stats.RemoveAt(index);
            }
        }
    }
}
