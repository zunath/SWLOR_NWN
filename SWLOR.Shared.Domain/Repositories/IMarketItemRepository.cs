using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for MarketItem entity operations.
    /// </summary>
    public interface IMarketItemRepository
    {
        /// <summary>
        /// Gets a market item by its unique identifier.
        /// </summary>
        /// <param name="id">The market item's unique identifier</param>
        /// <returns>The market item if found, null otherwise</returns>
        MarketItem GetById(string id);

        /// <summary>
        /// Gets market items by seller player ID.
        /// </summary>
        /// <param name="sellerPlayerId">The seller player ID to search for</param>
        /// <returns>Collection of market items for the specified seller</returns>
        IEnumerable<MarketItem> GetBySellerPlayerId(string sellerPlayerId);

        /// <summary>
        /// Gets market items by seller player ID and market ID.
        /// </summary>
        /// <param name="sellerPlayerId">The seller player ID to search for</param>
        /// <param name="marketId">The market ID to search for</param>
        /// <returns>Collection of market items for the specified seller and market</returns>
        IEnumerable<MarketItem> GetBySellerPlayerIdAndMarketId(string sellerPlayerId, string marketId);

        /// <summary>
        /// Gets market items by item resref.
        /// </summary>
        /// <param name="itemResref">The item resref to search for</param>
        /// <returns>Collection of market items with the specified resref</returns>
        IEnumerable<MarketItem> GetByItemResref(string itemResref);

        /// <summary>
        /// Saves a market item entity.
        /// </summary>
        /// <param name="marketItem">The market item to save</param>
        void Save(MarketItem marketItem);

        /// <summary>
        /// Deletes a market item by its unique identifier.
        /// </summary>
        /// <param name="id">The market item's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Gets all market items.
        /// </summary>
        /// <returns>Collection of all market items</returns>
        IEnumerable<MarketItem> GetAll();

        /// <summary>
        /// Gets all listed market items.
        /// </summary>
        /// <returns>Collection of listed market items</returns>
        IEnumerable<MarketItem> GetListedItems();

        /// <summary>
        /// Gets listed market items by market ID with optional search and category filters.
        /// </summary>
        /// <param name="marketId">The market ID to search for</param>
        /// <param name="searchText">Optional search text to filter by name</param>
        /// <param name="categoryIds">Optional category IDs to filter by</param>
        /// <param name="sortByPriceAscending">Whether to sort by price ascending</param>
        /// <returns>Collection of listed market items matching the criteria</returns>
        IEnumerable<MarketItem> GetListedItemsByMarketId(string marketId, string searchText = null, IEnumerable<int> categoryIds = null, bool sortByPriceAscending = true);

        /// <summary>
        /// Gets the count of all market items.
        /// </summary>
        /// <returns>The count of all market items</returns>
        long GetCount();

        /// <summary>
        /// Gets the count of listed market items.
        /// </summary>
        /// <returns>The count of listed market items</returns>
        long GetListedCount();

        /// <summary>
        /// Checks if a market item exists by its unique identifier.
        /// </summary>
        /// <param name="id">The market item's unique identifier</param>
        /// <returns>True if the market item exists, false otherwise</returns>
        bool Exists(string id);
    }
}
