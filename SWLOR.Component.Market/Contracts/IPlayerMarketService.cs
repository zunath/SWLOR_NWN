using SWLOR.Component.Market.Enums;
using SWLOR.Shared.Domain.Market.Enums;

namespace SWLOR.Component.Market.Contracts
{
    public interface IPlayerMarketService
    {
        /// <summary>
        /// Loads market categories from the database.
        /// </summary>
        void LoadMarketCategories();

        /// <summary>
        /// Loads markets from the database.
        /// </summary>
        void LoadMarkets();

        /// <summary>
        /// Removes old listings that have been on the market for too long.
        /// </summary>
        void RemoveOldListings();

        /// <summary>
        /// Checks if a player has credits in their market till and notifies them.
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
