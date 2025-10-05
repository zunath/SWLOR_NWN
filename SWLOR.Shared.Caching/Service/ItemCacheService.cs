using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Caching.Entity;

namespace SWLOR.Shared.Caching.Service
{
    /// <summary>
    /// This class is responsible for loading and retrieving item names which live for the lifespan of the server.
    /// Nothing in here will be permanently stored, it's simply here to make queries quicker.
    /// If you need persistent storage, refer to the DB class.
    /// </summary>
    public class ItemCacheService : IItemCacheService
    {
        private readonly IDatabaseService _db;
        private readonly IUtilPluginService _utilPlugin;
        private bool _cachedThisRun;
        private Dictionary<string, string> ItemNamesByResref { get; set; } = new();

        public ItemCacheService(IDatabaseService db, IUtilPluginService utilPlugin)
        {
            _db = db;
            _utilPlugin = utilPlugin;
        }

        public void CacheItemNamesByResref()
        {
            var resref = _utilPlugin.GetFirstResRef(ResRefType.Item);

            while (!string.IsNullOrWhiteSpace(resref))
            {
                CacheItemNameByResref(resref);
                resref = _utilPlugin.GetNextResRef();
            }

            var dbModuleCache = _db.Get<ModuleCache>("SWLOR_CACHE");
            dbModuleCache.ItemNamesByResref = ItemNamesByResref;
            _db.Set(dbModuleCache);

            _cachedThisRun = true;
        }

        private void LoadItemCache()
        {
            // No need to load from the DB, it's already in memory.
            if (_cachedThisRun)
                return;

            var dbModuleCache = _db.Get<ModuleCache>("SWLOR_CACHE");
            ItemNamesByResref = dbModuleCache.ItemNamesByResref;
        }

        /// <summary>
        /// Stores the name of an individual item into the cache.
        /// </summary>
        /// <param name="resref">The resref of the item we want to cache.</param>
        private void CacheItemNameByResref(string resref)
        {
            var storageContainer = GetObjectByTag("TEMP_ITEM_STORAGE");
            var item = CreateItemOnObject(resref, storageContainer);
            ItemNamesByResref[resref] = GetName(item);
            DestroyObject(item);
        }

        /// <summary>
        /// Retrieves the name of an item by its resref. If resref cannot be found, an empty string will be returned.
        /// </summary>
        /// <param name="resref">The resref to search for.</param>
        /// <returns>The name of an item, or an empty string if it cannot be found.</returns>
        public string GetItemNameByResref(string resref)
        {
            // Item couldn't be found in the cache. Spawn it, get its details, put them in the cache, then destroy it.
            if (!ItemNamesByResref.ContainsKey(resref))
            {
                CacheItemNameByResref(resref);
            }

            return ItemNamesByResref[resref];
        }
    }
}
