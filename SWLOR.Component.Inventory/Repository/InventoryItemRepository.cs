using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Inventory.Repository
{
    /// <summary>
    /// Repository implementation for InventoryItem entity operations.
    /// </summary>
    public class InventoryItemRepository : IInventoryItemRepository
    {
        private readonly IDatabaseService _db;

        public InventoryItemRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public InventoryItem GetById(string id)
        {
            return _db.Get<InventoryItem>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<InventoryItem> GetByStorageId(string storageId)
        {
            var query = new DBQuery<InventoryItem>()
                .AddFieldSearch(nameof(InventoryItem.StorageId), storageId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<InventoryItem> GetByStorageIdAndPlayerId(string storageId, string playerId)
        {
            var query = new DBQuery<InventoryItem>()
                .AddFieldSearch(nameof(InventoryItem.StorageId), storageId, false)
                .AddFieldSearch(nameof(InventoryItem.PlayerId), playerId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public long GetCountByStorageIdAndPlayerId(string storageId, string playerId)
        {
            var query = new DBQuery<InventoryItem>()
                .AddFieldSearch(nameof(InventoryItem.StorageId), storageId, false)
                .AddFieldSearch(nameof(InventoryItem.PlayerId), playerId, false);
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public IEnumerable<InventoryItem> GetByPlayerId(string playerId)
        {
            var query = new DBQuery<InventoryItem>()
                .AddFieldSearch(nameof(InventoryItem.PlayerId), playerId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<InventoryItem> GetByName(string name)
        {
            var query = new DBQuery<InventoryItem>()
                .AddFieldSearch(nameof(InventoryItem.Name), name, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<InventoryItem> GetByTag(string tag)
        {
            var query = new DBQuery<InventoryItem>()
                .AddFieldSearch(nameof(InventoryItem.Tag), tag, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<InventoryItem> GetByResref(string resref)
        {
            var query = new DBQuery<InventoryItem>()
                .AddFieldSearch(nameof(InventoryItem.Resref), resref, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public void Save(InventoryItem inventoryItem)
        {
            _db.Set(inventoryItem);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<InventoryItem>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<InventoryItem> GetAll()
        {
            var query = new DBQuery<InventoryItem>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public long GetCount()
        {
            var query = new DBQuery<InventoryItem>();
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<InventoryItem>(id);
        }
    }
}
