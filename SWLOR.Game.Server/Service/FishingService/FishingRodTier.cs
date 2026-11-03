using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>
    /// The Agriculture rank each fishing rod asks for, which is also its tier (rank / 10, so 0-4). It mirrors the rank in each rod's
    /// Agriculture requirement property in the module, and a test keeps the two in step.
    /// </summary>
    public static class FishingRodTier
    {
        public const int RanksPerTier = 10;
        public const int HighestTier = 4;

        private static readonly IReadOnlyDictionary<FishingRodType, int> RequiredRanks = new Dictionary<FishingRodType, int>
        {
            { FishingRodType.Bamboo, 0 },
            { FishingRodType.Carbon, 0 },
            { FishingRodType.Clothespole, 0 },
            { FishingRodType.Composite, 10 },
            { FishingRodType.Ebisu, 10 },
            { FishingRodType.Fastwater, 10 },
            { FishingRodType.GlassFiber, 20 },
            { FishingRodType.Halcyon, 20 },
            { FishingRodType.Judge, 20 },
            { FishingRodType.MazeMonger, 30 },
            { FishingRodType.Willow, 30 },
            { FishingRodType.Yew, 30 },
            { FishingRodType.Butterfly, 40 },
            { FishingRodType.Tranquility, 40 },
            { FishingRodType.LuShang, 40 }
        };

        /// <summary>Every rod and the Agriculture rank it requires.</summary>
        public static IReadOnlyDictionary<FishingRodType, int> All => RequiredRanks;

        /// <summary>The Agriculture rank the rod requires: 0, 10, 20, 30 or 40.</summary>
        public static int RequiredRank(FishingRodType rod)
        {
            if (!RequiredRanks.TryGetValue(rod, out var rank))
                throw new ArgumentOutOfRangeException(nameof(rod), rod, "The rod has no tier.");

            return rank;
        }

        /// <summary>The rod's tier, 0 to 4.</summary>
        public static int Tier(FishingRodType rod) => RequiredRank(rod) / RanksPerTier;
    }
}
