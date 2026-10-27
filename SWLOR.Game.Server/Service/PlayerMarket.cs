using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using MarketCategoryType = SWLOR.Game.Server.Service.PlayerMarketService.MarketCategoryType;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the player market. The logic and state live in <see cref="IPlayerMarketService"/>; every member
    /// here forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change.
    /// </summary>
    public static class PlayerMarket
    {
        public const int MaxListingsPerMarket = 25;

        private static IPlayerMarketService Instance => FacadeAccessor.Resolve<IPlayerMarketService>();

        /// <summary>
        /// When the module caches, cache all static player market data for quick retrieval.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// Marks items as unlisted if they have been sitting on the market for longer than two weeks.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RemoveOldListings()
        {
            Instance.RemoveOldListings();
        }

        /// <summary>
        /// When a player enters the server, if they have credits in their market till, send them a message stating so.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void CheckMarketTill()
        {
            Instance.CheckMarketTill();
        }

        /// <summary>
        /// Retrieves all active market categories.
        /// </summary>
        /// <returns>A dictionary of active market categories.</returns>
        public static Dictionary<MarketCategoryType, MarketCategoryAttribute> GetActiveCategories()
        {
            return Instance.GetActiveCategories();
        }

        /// <summary>
        /// Retrieves the market region detail given a specific type.
        /// </summary>
        /// <param name="regionType">The type of market region</param>
        /// <returns>A market region detail</returns>
        public static MarketRegionAttribute GetMarketRegion(MarketRegionType regionType)
        {
            return Instance.GetMarketRegion(regionType);
        }

        /// <summary>
        /// Returns the market category decided by the base item alone (weapons and wearables), or null
        /// when the item's data decides it. Needs no live item, so offline migrations can use it.
        /// </summary>
        public static MarketCategoryType? GetBaseItemMarketCategory(BaseItem baseItemType)
        {
            return Instance.GetBaseItemMarketCategory(baseItemType);
        }

        /// <summary>
        /// Determines which market category an item should be placed in.
        /// If category cannot be determined, MarketCategoryType.Miscellaneous will be returned.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>A market category type to place the item in.</returns>
        public static MarketCategoryType GetItemMarketCategory(uint item)
        {
            return Instance.GetItemMarketCategory(item);
        }
    }
}
