using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// What city level a mayor and a citizen count can support, as the weekly upkeep check applies it, so the election
    /// window can warn a candidate whose City Management rank would lower the city. Pure. It mirrors the calculation in
    /// <c>Property.ProcessCityLevel</c>, which <c>LeadershipCivicPerkRegressionTests</c> pins: change them together.
    /// </summary>
    public static class CityLevelRules
    {
        public const int MaxLevel = 5;

        /// <summary>The highest city level a mayor's City Management rank supports: one more than the rank, or 1 with none.</summary>
        public static int GetMayorLevel(int? cityManagementRank)
        {
            return (cityManagementRank ?? 0) + 1;
        }

        /// <summary>
        /// The highest level the mayor's level and the citizen count both support, at least 1.
        /// </summary>
        /// <param name="mayorLevel">What <see cref="GetMayorLevel"/> returned.</param>
        /// <param name="citizenCount">The city's citizens.</param>
        /// <param name="citizensRequired">The citizens each level needs, by level.</param>
        public static int GetMaxSupportedLevel(int mayorLevel, long citizenCount, IReadOnlyDictionary<int, int> citizensRequired)
        {
            var maxLevel = 1;
            for (var level = 1; level <= MaxLevel; level++)
            {
                if (level > mayorLevel)
                    break;

                if (citizenCount >= citizensRequired[level])
                    maxLevel = level;
            }

            return maxLevel;
        }

        /// <summary>
        /// The level a city holds after the weekly check drops it to what its mayor and citizens support. A level never
        /// rises here; the check lifts it one level at a time when the last bill is paid.
        /// </summary>
        public static int GetLevelAfterDrop(int currentLevel, int mayorLevel, int maxSupportedLevel)
        {
            return Math.Min(currentLevel, Math.Min(mayorLevel, maxSupportedLevel));
        }
    }
}
