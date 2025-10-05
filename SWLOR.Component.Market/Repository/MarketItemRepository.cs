using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Market.Repository
{
    /// <summary>
    /// Repository implementation for MarketItem entity operations.
    /// </summary>
    public class MarketItemRepository : IMarketItemRepository
    {
        private readonly IDatabaseService _db;

        public MarketItemRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public MarketItem GetById(string id)
        {
            return _db.Get<MarketItem>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<MarketItem> GetBySellerPlayerId(string sellerPlayerId)
        {
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.PlayerId), sellerPlayerId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<MarketItem> GetBySellerPlayerIdAndMarketId(string sellerPlayerId, string marketId)
        {
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.PlayerId), sellerPlayerId, false)
                .AddFieldSearch(nameof(MarketItem.MarketId), marketId, false)
                .OrderBy(nameof(MarketItem.Name));
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<MarketItem> GetByItemResref(string itemResref)
        {
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.Resref), itemResref, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public void Save(MarketItem marketItem)
        {
            _db.Set(marketItem);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<MarketItem>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<MarketItem> GetAll()
        {
            var query = new DBQuery<MarketItem>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<MarketItem> GetListedItems()
        {
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.IsListed), true);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<MarketItem> GetListedItemsByMarketId(string marketId, string searchText = null, IEnumerable<int> categoryIds = null, bool sortByPriceAscending = true)
        {
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.IsListed), true)
                .AddFieldSearch(nameof(MarketItem.MarketId), marketId, false);

            if (!string.IsNullOrWhiteSpace(searchText))
                query.AddFieldSearch(nameof(MarketItem.Name), searchText, true);

            if (categoryIds != null && categoryIds.Any())
            {
                query.AddFieldSearch(nameof(MarketItem.Category), categoryIds);
            }

            // Add sorting by price
            query.OrderBy(nameof(MarketItem.Price), sortByPriceAscending);

            return _db.Search(query);
        }

        /// <inheritdoc/>
        public long GetCount()
        {
            var query = new DBQuery<MarketItem>();
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public long GetListedCount()
        {
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.IsListed), true);
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<MarketItem>(id);
        }
    }
}
