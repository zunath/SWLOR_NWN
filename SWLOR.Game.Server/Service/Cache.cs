using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// This class is responsible for loading and retrieving NWN data which lives for the lifespan of the server.
    /// Nothing in here will be permanently stored, it's simply here to make queries quicker.
    /// If you need persistent storage, refer to the DB class.
    /// </summary>
    public static class Cache
    {
        private static Dictionary<string, uint> AreasByResref { get; } = new Dictionary<string, uint>();
        private static Dictionary<string, string> ItemNamesByResref { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Handles caching data into server memory for quicker lookup later.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void OnModuleLoad()
        {
            Console.WriteLine("Caching areas by resref.");
            CacheAreasByResref();

            Console.WriteLine("Caching item names by resref.");
            CacheItemNamesByResref();
        }

        /// <summary>
        /// Caches all areas by their resref.
        /// </summary>
        private static void CacheAreasByResref()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var resref = GetResRef(area);
                AreasByResref[resref] = area;
            }
        }

        /// <summary>
        /// Retrieves an area by its resref. If the area does not exist, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="resref">The resref to use for the search.</param>
        /// <returns>The area ID or OBJECT_INVALID if area does not exist.</returns>
        public static uint GetAreaByResref(string resref)
        {
            if (!AreasByResref.ContainsKey(resref))
                return OBJECT_INVALID;

            return AreasByResref[resref];
        }

        /// <summary>
        /// Stores the names of every item in the module. This speeds up the look-ups later on.
        /// </summary>
        private static void CacheItemNamesByResref()
        {
            var resref = Util.GetFirstResRef(ResRefType.Item);

            while (!string.IsNullOrWhiteSpace(resref))
            {
                CacheItemNameByResref(resref);
                resref = Util.GetNextResRef();
            }
        }

        /// <summary>
        /// Stores the name of an individual item into the cache.
        /// </summary>
        /// <param name="resref">The resref of the item we want to cache.</param>
        private static void CacheItemNameByResref(string resref)
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
        public static string GetItemNameByResref(string resref)
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
