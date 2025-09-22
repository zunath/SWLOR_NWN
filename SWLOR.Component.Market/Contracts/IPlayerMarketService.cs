using SWLOR.Component.Market.Enums;

namespace SWLOR.Component.Market.Contracts
{
    public interface IPlayerMarketService
    {
        /// <summary>
        /// When the module caches, cache all static player market data for quick retrieval.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Marks items as unlisted if they have been sitting on the market for longer than two weeks.
        /// </summary>
        void RemoveOldListings();

        /// <summary>
        /// When a player enters the server, if they have credits in their market till, send them a message stating so.
        /// </summary>
        void CheckMarketTill();

        /// <summary>
        /// Retrieves all active market categories.
        /// </summary>
        /// <returns>A dictionary of active market categories.</returns>
        Dictionary<MarketCategoryType, MarketCategoryAttribute> GetActiveCategories();

        /// <summary>
        /// Retrieves the market region detail given a specific type.
        /// </summary>
        /// <param name="regionType">The type of market region</param>
        /// <returns>A market region detail</returns>
        MarketRegionAttribute GetMarketRegion(MarketRegionType regionType);

        /// <summary>
        /// Determines which market category an item should be placed in.
        /// If category cannot be determined, MarketCategoryType.Miscellaneous will be returned.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>A market category type to place the item in.</returns>
        MarketCategoryType GetItemMarketCategory(uint item);
    }

}
