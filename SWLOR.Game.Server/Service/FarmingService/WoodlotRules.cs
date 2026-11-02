using System;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.FarmingService
{
    /// <summary>
    /// The woodlot rules of the Player City Revamp ("Farming for other crafts"), with no database or engine calls. A sapling is
    /// planted in a garden plot like a seed, grows for 3 days and yields 4 logs of its tier: wood, fine wood, ancient wood, aracia
    /// wood or hyphae wood, matching the five harvesting tree tiers. Everything else about a woodlot is the farming rule it shares
    /// with crops (Crop Management gates the tier, the planter's CD key holds at most <see cref="FarmingRules.MaxCropsPerCdKey"/>
    /// crops and saplings together, a crop that ripens withers 72 hours later), except where this class says otherwise.
    /// </summary>
    public static class WoodlotRules
    {
        /// <summary>Hours from planting to ripe, for every tier.</summary>
        public const int GrowHours = 72;

        /// <summary>
        /// The logs a harvest yields. It is flat: tending, fertilizer, Bountiful Harvest, a Greenhouse, Home Ground and pests do not
        /// change it, since the passive budget (10 saplings, 4 logs every 3 days) counts exactly this many.
        /// </summary>
        public const int LogsPerHarvest = 4;

        /// <summary>
        /// The weight a sapling holds in its tree's harvesting loot table, against 50 for the tier's own wood: about 1 roll in 60,
        /// the weight of the emerald in the hyphae table.
        /// </summary>
        public const int SaplingDropWeight = 1;

        /// <summary>The days a sapling takes to grow, for the passive output figure.</summary>
        public const int GrowDays = GrowHours / 24;

        private static readonly (string Sapling, string Wood, string TreeTable)[] Tiers =
        {
            ("sapling_wood", "wood", "HARVESTING_TREE"),
            ("sapling_fine", "fine_wood", "HARVESTING_OAK_TREE"),
            ("sapling_ancient", "ancient_wood", "HARVESTING_ANCIENT_TREE"),
            ("sapling_aracia", "aracia_wood", "HARVESTING_ARACIA_TREE"),
            ("sapling_hyphae", "hyphae_wood", "HARVESTING_HYPHAE_TREE"),
        };

        /// <summary>The crop types of the five saplings, lowest tier first.</summary>
        public static readonly CropType[] SaplingTypes =
        {
            CropType.WoodSapling,
            CropType.FineWoodSapling,
            CropType.AncientWoodSapling,
            CropType.AraciaWoodSapling,
            CropType.HyphaeWoodSapling,
        };

        public static bool IsSapling(CropType type) => Array.IndexOf(SaplingTypes, type) >= 0;

        /// <summary>The tier (1 to 5) of a sapling, or 0 for anything else.</summary>
        public static int GetTier(CropType type) => Array.IndexOf(SaplingTypes, type) + 1;

        /// <summary>The sapling item of a tier: the seed of that tier's woodlot.</summary>
        public static string GetSaplingResref(int tier) => GetTierRow(tier).Sapling;

        /// <summary>The logs a tier's sapling yields: the item its harvesting tree gives.</summary>
        public static string GetWoodResref(int tier) => GetTierRow(tier).Wood;

        /// <summary>The harvesting loot table of the tier's tree, which drops the tier's sapling.</summary>
        public static string GetTreeTable(int tier) => GetTierRow(tier).TreeTable;

        public static bool IsSaplingResref(string resref)
        {
            if (string.IsNullOrWhiteSpace(resref))
                return false;

            foreach (var row in Tiers)
            {
                if (string.Equals(row.Sapling, resref, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The logs of one harvest. Always <see cref="LogsPerHarvest"/>: the inputs are the ones a crop's yield takes, and none of
        /// them applies to a sapling.
        /// </summary>
        public static int GetYield(bool tended, bool fertilized, int bountifulHarvestRank, bool greenhouse, bool homeGround, bool pests) =>
            LogsPerHarvest;

        /// <summary>
        /// Why the sapling cannot go in this plot, or <see cref="FarmingRefusal.None"/>. A woodlot needs open ground: an outdoor plot
        /// of a city, never one inside a house, and never on a station deck.
        /// </summary>
        /// <param name="isSapling">Whether the seed being planted is a sapling. Anything else is allowed anywhere a plot is.</param>
        /// <param name="plotParent">What the plot stands in: a city, or a house.</param>
        /// <param name="parentIsStation">Whether the city the plot stands in is a station.</param>
        public static FarmingRefusal GetSiteRefusal(bool isSapling, PropertyType plotParent, bool parentIsStation)
        {
            if (!isSapling)
                return FarmingRefusal.None;
            if (plotParent != PropertyType.City)
                return FarmingRefusal.OutdoorPlotOnly;
            if (parentIsStation)
                return FarmingRefusal.NoOpenGroundOnStation;
            return FarmingRefusal.None;
        }

        /// <summary>
        /// Why fertilizer offered with a seed is refused, or <see cref="FarmingRefusal.None"/>. A sapling's harvest is flat, so
        /// fertilizer cannot add to it: planting one with fertilizer is refused before anything is used up, rather than taking the
        /// fertilizer for nothing. A crop takes fertilizer, so it is never refused here.
        /// </summary>
        public static FarmingRefusal GetFertilizerRefusal(bool isSapling, bool fertilizerOffered) =>
            isSapling && fertilizerOffered ? FarmingRefusal.NoFertilizerOnSaplings : FarmingRefusal.None;

        /// <summary>The logs a CD key's woodlot gets each day when <paramref name="saplings"/> saplings are replanted as soon as they ripen.</summary>
        public static double GetLogsPerDay(int saplings) =>
            Math.Clamp(saplings, 0, FarmingRules.MaxCropsPerCdKey) * (double)LogsPerHarvest / GrowDays;

        private static (string Sapling, string Wood, string TreeTable) GetTierRow(int tier)
        {
            if (tier < 1 || tier > Tiers.Length)
                throw new ArgumentOutOfRangeException(nameof(tier), tier, "A woodlot tier is 1 to 5.");
            return Tiers[tier - 1];
        }
    }
}
