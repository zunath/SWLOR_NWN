using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service
{
    public static partial class Skill
    {
        private static readonly Dictionary<int, int> _skillXPRequirements = new()
        {
            { 0, 550 },
            { 1, 825 },
            { 2, 1100 },
            { 3, 1375 },
            { 4, 1650 },
            { 5, 1925 },
            { 6, 2200 },
            { 7, 2420 },
            { 8, 2640 },
            { 9, 2860 },
            { 10, 3080 },
            { 11, 4200 },
            { 12, 4480 },
            { 13, 4760 },
            { 14, 5040 },
            { 15, 5320 },
            { 16, 5600 },
            { 17, 5880 },
            { 18, 6160 },
            { 19, 6440 },
            { 20, 6720 },
            { 21, 8500 },
            { 22, 8670 },
            { 23, 8840 },
            { 24, 9010 },
            { 25, 9180 },
            { 26, 9350 },
            { 27, 9520 },
            { 28, 9690 },
            { 29, 9860 },
            { 30, 10030 },
            { 31, 10200 },
            { 32, 10370 },
            { 33, 10540 },
            { 34, 10710 },
            { 35, 10880 },
            { 36, 11050 },
            { 37, 11220 },
            { 38, 11390 },
            { 39, 11560 },
            { 40, 11730 },
            { 41, 14000 },
            { 42, 14200 },
            { 43, 14400 },
            { 44, 14600 },
            { 45, 14800 },
            { 46, 15000 },
            { 47, 15200 },
            { 48, 15400 },
            { 49, 16000 },
            { 50, 18400 },
            { 51, 24960 },
            { 52, 27840 },
            { 53, 30720 },
            { 54, 33600 },
            { 55, 36480 },
            { 56, 39360 },
            { 57, 42240 },
            { 58, 45120 },
            { 59, 48000 },
            { 60, 51600 },
            { 61, 55200 },
            { 62, 58800 },
            { 63, 62400 },
            { 64, 66000 },
            { 65, 69600 },
            { 66, 73200 },
            { 67, 76800 },
            { 68, 81600 },
            { 69, 86400 },
            { 70, 91200 },
            { 71, 108000 },
            { 72, 113400 },
            { 73, 118800 },
            { 74, 120150 },
            { 75, 121500 },
            { 76, 122850 },
            { 77, 124200 },
            { 78, 125550 },
            { 79, 126900 },
            { 80, 128250 },
            { 81, 144000 },
            { 82, 145500 },
            { 83, 147000 },
            { 84, 148500 },
            { 85, 150000 },
            { 86, 151500 },
            { 87, 153000 },
            { 88, 154500 },
            { 89, 156000 },
            { 90, 159000 },
            { 91, 216000 },
            { 92, 220000 },
            { 93, 224000 },
            { 94, 228000 },
            { 95, 232000 },
            { 96, 236000 },
            { 97, 240000 },
            { 98, 260000 },
            { 99, 280000 },
            { 100, 400000 }
        };
        private static readonly Dictionary<int, int> _skillTotalXP = new();
        private static readonly Dictionary<int, int> _skillDeltaXP = new()
        {
            { 6, 1200 },
            { 5, 1050 },
            { 4, 976 },
            { 3, 900 },
            { 2, 750 },
            { 1, 676 },
            { 0, 600 },
            { -1, 450 },
            { -2, 300 },
            { -3, 150 },
            { -4, 76 }
        };

        private static int _highestDelta;

        /// <summary>
        /// When the module loads, cache all XP chart data used for quick access.
        /// </summary>
        [NWNEventHandler("mod_cache_bef")]
        public static void CacheXPChartData()
        {
            CalculateTotalXP();
            _highestDelta = _skillDeltaXP.Keys.Max();
        }

        /// <summary>
        /// Determines the total XP required for each level.
        /// </summary>
        private static void CalculateTotalXP()
        {
            var totalXP = 0;
            foreach (var (level, xp) in _skillXPRequirements)
            {
                totalXP += xp;
                _skillTotalXP[level] = totalXP;
            }
        }

        /// <summary>
        /// Gets the amount of XP required to reach the next level.
        /// </summary>
        /// <param name="level">The level to use for the search.</param>
        /// <returns>The amount of XP required to reach the next level.</returns>
        public static int GetRequiredXP(int level)
        {
            if(!_skillXPRequirements.ContainsKey(level))
                throw new Exception($"Level {level} not registered in the SkillXPRequirements dictionary.");

            return _skillXPRequirements[level];
        }

        /// <summary>
        /// Gets the total amount of XP attained at this level, excluding the XP needed to reach the next level.
        /// </summary>
        /// <param name="level">The level to retrieve.</param>
        /// <returns>The total amount of XP attained at this level</returns>
        public static int GetTotalRequiredXP(int level)
        {
            if (!_skillTotalXP.ContainsKey(level))
                throw new Exception($"Level {level} not registered in the SkillTotalXP dictionary.");

            return _skillTotalXP[level];
        }

        /// <summary>
        /// Gets the highest level by a total XP amount, returning the remainder XP as the second item.
        /// </summary>
        /// <param name="totalXP">The total XP gained</param>
        /// <returns>A tuple containing the level and a remainder amount of XP</returns>
        public static (int, int) GetLevelByTotalXP(int totalXP)
        {
            var (level, requiredXP) = _skillTotalXP
                .OrderBy(o => o.Value)
                .First(x => x.Value >= totalXP);

            var remainderXP = requiredXP - totalXP;
            remainderXP = _skillXPRequirements[level] - remainderXP;
            return (level, remainderXP);
        }

        /// <summary>
        /// Retrieves the base XP amount by the delta of a player's skill rank versus the target's level.
        /// If delta is above the highest delta, the highest delta will be used.
        /// If delta is lower than the lowest delta, zero will be returned.
        /// </summary>
        /// <param name="delta">The delta to compare.</param>
        /// <returns>The base XP amount based on the delta. Returns 0 if delta is below the lowest.</returns>
        public static int GetDeltaXP(int delta)
        {
            if (delta > _highestDelta)
                delta = _highestDelta;

            if (!_skillDeltaXP.ContainsKey(delta)) return 0;

            return _skillDeltaXP[delta];
        }
    }
}
