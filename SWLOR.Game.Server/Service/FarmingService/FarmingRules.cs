using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.FarmingService
{
    /// <summary>
    /// The farming rules of the Player City Revamp ("Farming (Agriculture)"), with no database or engine calls: which tier a
    /// Crop Management rank opens, how long a crop takes, when it can be tended, ripens and withers, what a harvest yields, how
    /// often a seed comes back and what the work pays. The <c>Farming</c> service reads and writes the records and the
    /// inventory and asks these rules what is allowed.
    /// </summary>
    public static class FarmingRules
    {
        public const int Tiers = 5;

        /// <summary>The most crops one CD key can have growing at once, across all its characters and plots.</summary>
        public const int MaxCropsPerCdKey = 10;

        /// <summary>What a ripe crop yields before tending, fertilizer, perks and bonuses.</summary>
        public const int BaseYield = 3;

        /// <summary>A crop left ripe this long withers and its seed is lost.</summary>
        public const int WitherHours = 72;

        /// <summary>A crop can be tended once after one quarter of its grow time.</summary>
        public const int TendDivisor = 4;

        /// <summary>What tending pays, as a percent of the XP one harvested item pays the tender.</summary>
        public const int TendXpPercent = 50;

        public const int MaxBountifulHarvestRank = 3;

        /// <summary>The garden plots one house holds.</summary>
        public const int HousePlotLimit = 5;

        /// <summary>The garden plots a Greenhouse holds.</summary>
        public const int GreenhousePlotLimit = 10;

        /// <summary>No limit besides the city's own structure limit.</summary>
        public const int NoPlotLimit = int.MaxValue;

        private static readonly int[] TierLevels = { 5, 15, 25, 35, 45 };
        private static readonly int[] TierGrowHours = { 12, 16, 20, 24, 24 };
        private static readonly int[] SeedSaverPercents = { 0, 15, 30 };

        /// <summary>The crop level of a tier (5, 15, 25, 35, 45), the level of the harvesting veins of that tier.</summary>
        public static int GetTierLevel(int tier)
        {
            CheckTier(tier);
            return TierLevels[tier - 1];
        }

        /// <summary>The hours a tier's crops take to ripen: 12, 16, 20, 24 and 24.</summary>
        public static int GetTierGrowHours(int tier)
        {
            CheckTier(tier);
            return TierGrowHours[tier - 1];
        }

        /// <summary>The highest crop tier a Crop Management rank (0 to 5) opens.</summary>
        public static int GetHighestTier(int cropManagementRank) => Math.Clamp(cropManagementRank, 0, Tiers);

        public static bool CanPlantTier(int cropManagementRank, int tier) => tier >= 1 && tier <= GetHighestTier(cropManagementRank);

        /// <summary>When a crop planted at <paramref name="plantedUtc"/> can first be tended: a quarter of its grow time in.</summary>
        public static DateTime GetTendReadyUtc(DateTime plantedUtc, int growHours) =>
            plantedUtc.AddMinutes(growHours * 60.0 / TendDivisor);

        public static DateTime GetRipeUtc(DateTime plantedUtc, int growHours) => plantedUtc.AddHours(growHours);

        /// <summary>When a crop that ripened at <paramref name="ripeUtc"/> withers.</summary>
        public static DateTime GetWitherUtc(DateTime ripeUtc) => ripeUtc.AddHours(WitherHours);

        /// <summary>The garden plots one house holds, or <see cref="NoPlotLimit"/> where only the structure limit applies.</summary>
        public static int GetPlotLimit(PropertyType propertyType, bool greenhouse)
        {
            return propertyType switch
            {
                PropertyType.House => greenhouse ? GreenhousePlotLimit : HousePlotLimit,
                PropertyType.City => NoPlotLimit,
                _ => 0
            };
        }

        /// <summary>Where the plot's crop is at <paramref name="now"/>. A missing record is an empty plot.</summary>
        public static GardenPlotStage GetStage(GardenPlot plot, DateTime now)
        {
            if (plot == null || plot.Crop == CropType.Invalid)
                return GardenPlotStage.Empty;

            if (now >= plot.WitherUtc)
                return GardenPlotStage.Withered;

            if (now >= plot.RipeUtc)
                return GardenPlotStage.Ripe;

            return !plot.Tended && now >= plot.TendReadyUtc
                ? GardenPlotStage.ReadyToTend
                : GardenPlotStage.Growing;
        }

        /// <summary>The time until the crop's next milestone: ripe while it grows, wither once it is ripe. Zero for an empty or withered plot.</summary>
        public static TimeSpan GetTimeLeft(GardenPlot plot, DateTime now)
        {
            var stage = GetStage(plot, now);
            return stage switch
            {
                GardenPlotStage.Growing or GardenPlotStage.ReadyToTend => plot.RipeUtc - now,
                GardenPlotStage.Ripe => plot.WitherUtc - now,
                _ => TimeSpan.Zero
            };
        }

        /// <summary>The items a harvest loses while the Pests in the Gardens situation is ignored in the city.</summary>
        public const int PestYieldPenalty = 1;

        /// <summary>
        /// Items in one harvest: 3, plus 1 if tended, 1 if fertilized, one per Bountiful Harvest rank (up to 3), and 1 each from a
        /// Greenhouse and Home Ground. The most is 10. While pests plague the city's gardens a harvest yields 1 less, never below 1.
        /// </summary>
        public static int GetYield(bool tended, bool fertilized, int bountifulHarvestRank, bool greenhouse, bool homeGround, bool pests = false)
        {
            var yield = BaseYield +
                        (tended ? 1 : 0) +
                        (fertilized ? 1 : 0) +
                        Math.Clamp(bountifulHarvestRank, 0, MaxBountifulHarvestRank) +
                        (greenhouse ? 1 : 0) +
                        (homeGround ? 1 : 0);
            return pests ? Math.Max(1, yield - PestYieldPenalty) : yield;
        }

        /// <summary>The chance, in percent, that a harvest also returns its seed: 0, 15 or 30 for a Seed Saver rank of 0 to 2.</summary>
        public static int GetSeedSaverPercent(int seedSaverRank) =>
            SeedSaverPercents[Math.Clamp(seedSaverRank, 0, SeedSaverPercents.Length - 1)];

        /// <summary>Whether a roll of 1 to 100 returns the seed at <paramref name="percent"/>.</summary>
        public static bool ReturnsSeed(int percent, int roll) => roll >= 1 && roll <= percent;

        /// <summary>
        /// The Agriculture XP one harvested item pays: <c>Skill.GetDeltaXP(crop level - rank)</c>, the formula fishing and
        /// harvesting use, so a crop the farmer has outgrown pays nothing.
        /// </summary>
        public static int GetXpPerItem(int cropLevel, int agricultureRank) => Skill.GetDeltaXP(cropLevel - agricultureRank);

        /// <summary>The XP for a harvest of <paramref name="items"/>, one item at a time.</summary>
        public static int GetHarvestXp(int cropLevel, int agricultureRank, int items) =>
            Math.Max(0, items) * GetXpPerItem(cropLevel, agricultureRank);

        /// <summary>The XP for tending a crop: <see cref="TendXpPercent"/> percent of one harvested item's XP, at the tender's rank.</summary>
        public static int GetTendXp(int cropLevel, int tenderAgricultureRank) =>
            GetXpPerItem(cropLevel, tenderAgricultureRank) * TendXpPercent / 100;

        /// <summary>
        /// The crops a CD key has growing: every record of its characters whose crop has not withered. A record whose plot
        /// is gone does not count, which the caller decides by passing only the plots that still stand.
        /// </summary>
        public static int CountGrowingCrops(IEnumerable<GardenPlot> plots, DateTime now) =>
            plots.Count(plot => GetStage(plot, now) is not (GardenPlotStage.Empty or GardenPlotStage.Withered));

        public static bool HasRoomForCrop(int growingCrops) => growingCrops < MaxCropsPerCdKey;

        /// <summary>Why a character cannot plant a seed in a plot, or <see cref="FarmingRefusal.None"/>. Checked in this order.</summary>
        /// <param name="plot">The plot's record, or null when nothing is planted. A withered crop must be removed first.</param>
        /// <param name="cropManagementRank">The planter's Crop Management rank (0 to 5).</param>
        /// <param name="cropTier">The tier of the crop the seed grows.</param>
        /// <param name="growingCrops">The crops the planter's CD key already has growing.</param>
        /// <param name="plotAboveLimit">Whether the plot is one of a house's plots past the first 5 after the house lost its Greenhouse.</param>
        public static FarmingRefusal CheckPlant(
            bool isPlayer,
            bool plotExists,
            GardenPlot plot,
            DateTime now,
            bool hasPermission,
            int cropManagementRank,
            int cropTier,
            int growingCrops,
            bool hasSeed,
            bool plotAboveLimit = false)
        {
            if (!isPlayer)
                return FarmingRefusal.NotAPlayer;
            if (!plotExists)
                return FarmingRefusal.NoPlot;
            if (GetStage(plot, now) != GardenPlotStage.Empty)
                return FarmingRefusal.PlotOccupied;
            if (plotAboveLimit)
                return FarmingRefusal.PlotAboveLimit;
            if (!hasPermission)
                return FarmingRefusal.NoPermission;
            if (!CanPlantTier(cropManagementRank, cropTier))
                return FarmingRefusal.TierLocked;
            if (!HasRoomForCrop(growingCrops))
                return FarmingRefusal.CropCap;
            if (!hasSeed)
                return FarmingRefusal.NoSeed;
            return FarmingRefusal.None;
        }

        /// <summary>
        /// Why a character cannot tend the crop, or <see cref="FarmingRefusal.None"/>. The planter tends their own crop, and so can
        /// anyone with garden permission, once.
        /// </summary>
        public static FarmingRefusal CheckTend(bool isPlayer, GardenPlot plot, DateTime now, bool isPlanter, bool hasPermission)
        {
            if (!isPlayer)
                return FarmingRefusal.NotAPlayer;

            var stage = GetStage(plot, now);
            if (stage == GardenPlotStage.Empty)
                return FarmingRefusal.PlotEmpty;
            if (stage == GardenPlotStage.Withered)
                return FarmingRefusal.Withered;
            if (!isPlanter && !hasPermission)
                return FarmingRefusal.NoPermission;
            if (plot.Tended)
                return FarmingRefusal.AlreadyTended;
            if (now < plot.TendReadyUtc)
                return FarmingRefusal.TooYoungToTend;
            return FarmingRefusal.None;
        }

        /// <summary>Why a character cannot harvest the crop, or <see cref="FarmingRefusal.None"/>. Only the planter harvests, and only a ripe crop.</summary>
        public static FarmingRefusal CheckHarvest(bool isPlayer, GardenPlot plot, DateTime now, bool isPlanter)
        {
            if (!isPlayer)
                return FarmingRefusal.NotAPlayer;

            var stage = GetStage(plot, now);
            if (stage == GardenPlotStage.Empty)
                return FarmingRefusal.PlotEmpty;
            if (stage == GardenPlotStage.Withered)
                return FarmingRefusal.Withered;
            if (!isPlanter)
                return FarmingRefusal.NotThePlanter;
            if (stage != GardenPlotStage.Ripe)
                return FarmingRefusal.NotRipe;
            return FarmingRefusal.None;
        }

        /// <summary>Why a character cannot clear the crop, or <see cref="FarmingRefusal.None"/>. A planter can always clear their own crop, at any stage.</summary>
        public static FarmingRefusal CheckClear(bool isPlayer, GardenPlot plot, DateTime now, bool isPlanter)
        {
            if (!isPlayer)
                return FarmingRefusal.NotAPlayer;
            if (GetStage(plot, now) == GardenPlotStage.Empty)
                return FarmingRefusal.PlotEmpty;
            if (!isPlanter)
                return FarmingRefusal.NotThePlanter;
            return FarmingRefusal.None;
        }

        /// <summary>The message a player reads for a refusal.</summary>
        public static string Describe(FarmingRefusal refusal)
        {
            return refusal switch
            {
                FarmingRefusal.None => string.Empty,
                FarmingRefusal.NotAPlayer => "Only characters can farm.",
                FarmingRefusal.NoPlot => "This garden plot is no longer here.",
                FarmingRefusal.PlotOccupied => "Something is already growing here.",
                FarmingRefusal.PlotEmpty => "Nothing is growing here.",
                FarmingRefusal.NoPermission => "You do not have permission to tend gardens here.",
                FarmingRefusal.TierLocked => "Your Crop Management is too low to plant that crop.",
                FarmingRefusal.CropCap => $"You already have {MaxCropsPerCdKey} crops growing. Harvest or clear one first.",
                FarmingRefusal.NoSeed => "You have no seed for that crop.",
                FarmingRefusal.TooYoungToTend => "The crop is too young to tend.",
                FarmingRefusal.AlreadyTended => "The crop has already been tended.",
                FarmingRefusal.NotRipe => "The crop is not ripe yet.",
                FarmingRefusal.NotThePlanter => "Only the planter can do that.",
                FarmingRefusal.Withered => "The crop has withered and its seed is lost.",
                FarmingRefusal.UnknownCrop => "That is not a crop.",
                FarmingRefusal.PlotAboveLimit => $"This house is no longer a Greenhouse, so it plants only its first {HousePlotLimit} plots. This one can be harvested, not replanted.",
                _ => string.Empty
            };
        }

        public static string DescribePlanted(string cropName, int growHours) =>
            $"You planted {cropName}. It will be ripe in {FormatDuration(TimeSpan.FromHours(growHours))}.";

        public static string DescribeTended(string cropName) =>
            $"You tended the {cropName}. Its harvest will yield 1 more item.";

        public static string DescribeCleared(string cropName) => $"You cleared the {cropName}.";

        /// <summary>The message for a harvest: the items, any that did not fit and went to overflow storage, and the seed.</summary>
        public static string DescribeHarvest(string cropName, int items, int itemsInOverflow, bool seedReturned, bool seedInOverflow)
        {
            var text = $"You harvested the {cropName} for {items} {(items == 1 ? "item" : "items")}.";
            if (itemsInOverflow > 0)
                text += $" {itemsInOverflow} did not fit in your inventory and are in your overflow storage. Use /overflow to claim them.";
            if (seedReturned)
                text += seedInOverflow
                    ? " The seed came back, and is in your overflow storage."
                    : " The seed came back.";
            return text;
        }

        /// <summary>The stage as a player reads it.</summary>
        public static string DescribeStage(GardenPlotStage stage)
        {
            return stage switch
            {
                GardenPlotStage.Empty => "Empty",
                GardenPlotStage.Growing => "Growing",
                GardenPlotStage.ReadyToTend => "Ready to tend",
                GardenPlotStage.Ripe => "Ripe",
                GardenPlotStage.Withered => "Withered",
                _ => string.Empty
            };
        }

        /// <summary>A duration as "2d 3h", "5h 12m" or "7m". Anything under a minute reads "under a minute".</summary>
        public static string FormatDuration(TimeSpan duration)
        {
            var totalMinutes = (int)Math.Floor(Math.Max(0, duration.TotalMinutes));
            if (totalMinutes < 1)
                return "under a minute";

            var days = totalMinutes / (24 * 60);
            var hours = totalMinutes / 60 % 24;
            var minutes = totalMinutes % 60;
            if (days > 0)
                return $"{days}d {hours}h";
            return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
        }

        private static void CheckTier(int tier)
        {
            if (tier < 1 || tier > Tiers)
                throw new ArgumentOutOfRangeException(nameof(tier), tier, "A crop tier is 1 to 5.");
        }
    }
}
