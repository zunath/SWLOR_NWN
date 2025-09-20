using System;
using System.Collections.Generic;
using System.Linq;

using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Entity;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// This class is responsible for loading and retrieving NWN data which lives for the lifespan of the server.
    /// Nothing in here will be permanently stored, it's simply here to make queries quicker.
    /// If you need persistent storage, refer to the DB class.
    /// </summary>
    public static class Cache
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private static bool _cachedThisRun;
        private static Dictionary<string, string> ItemNamesByResref { get; set; } = new();
        private static Dictionary<int, int> PortraitIdsByInternalId { get; } = new();
        private static Dictionary<int, int> PortraitInternalIdsByPortraitId { get; } = new();
        private static Dictionary<int, string> PortraitResrefByInternalId { get; } = new();
        private static Dictionary<string, int> PortraitInternalIdsByPortraitResref { get; } = new();
        private static Dictionary<int, string> SoundSets { get; set; } = new();
        
        [ScriptHandler(ScriptName.OnModuleContentChange)]
        public static void CacheItemNamesByResref()
        {
            var resref = UtilPlugin.GetFirstResRef(ResRefType.Item);

            while (!string.IsNullOrWhiteSpace(resref))
            {
                CacheItemNameByResref(resref);
                resref = UtilPlugin.GetNextResRef();
            }

            var dbModuleCache = _db.Get<ModuleCache>("SWLOR_CACHE");
            dbModuleCache.ItemNamesByResref = ItemNamesByResref;
            _db.Set(dbModuleCache);

            _cachedThisRun = true;
        }

        /// <summary>
        /// Handles caching data into server memory for quicker lookup later.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            LoadItemCache();
            CachePortraitsById();
            CacheSoundSets();

            Console.WriteLine($"Loaded {ItemNamesByResref.Count} item names by resref.");
            Console.WriteLine($"Loaded {PortraitIdsByInternalId.Count} portraits by Id.");
            Console.WriteLine($"Loaded {SoundSets.Count} soundsets.");
        }

        private static void LoadItemCache()
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

        /// <summary>
        /// Retrieves the number of portraits registered in the system.
        /// </summary>
        public static int PortraitCount => PortraitIdsByInternalId.Count;

        private static void CachePortraitsById()
        {
            const string Portraits2DA = "portraits";
            var twoDACount = Get2DARowCount(Portraits2DA);
            var internalId = 1;

            for (var row = 0; row < twoDACount; row++)
            {
                var baseResref = Get2DAString(Portraits2DA, "BaseResRef", row);
                var race = Get2DAString(Portraits2DA, "Race", row);

                if (!string.IsNullOrWhiteSpace(baseResref) &&
                    !string.IsNullOrWhiteSpace(race))
                {
                    PortraitIdsByInternalId[internalId] = row;
                    PortraitInternalIdsByPortraitId[row] = internalId;
                    PortraitResrefByInternalId[internalId] = "po_" + baseResref;
                    PortraitInternalIdsByPortraitResref["po_" + baseResref] = internalId;
                    internalId++;
                }
            }
        }

        private static void CacheSoundSets()
        {
            const string SoundSets2DA = "soundset";
            var soundSetCount = Get2DARowCount(SoundSets2DA);
            for (var row = 0; row < soundSetCount; row++)
            {
                var strRef = Get2DAString(SoundSets2DA, "STRREF", row);
                var resref = Get2DAString(SoundSets2DA, "RESREF", row);

                if (!string.IsNullOrWhiteSpace(strRef) &&
                    !string.IsNullOrWhiteSpace(resref))
                {
                    SoundSets.Add(row, GetStringByStrRef(Convert.ToInt32(strRef)));
                }
            }

            SoundSets = SoundSets.OrderBy(o => o.Value).ToDictionary(x => x.Key, y => y.Value);
        }
        
        /// <summary>
        /// Retrieves the portrait 2DA Id from the internal Id of the portrait.
        /// The value returned by this method can be used with NWScript.SetPortrait
        /// </summary>
        /// <param name="portraitInternalId">The internal portrait Id to retrieve.</param>
        /// <returns>The 2DA Id of the portrait.</returns>
        public static int GetPortraitByInternalId(int portraitInternalId)
        {
            return PortraitIdsByInternalId[portraitInternalId];
        }

        /// <summary>
        /// Retrieves the internal Id of a portrait by its NWN 2DA Id.
        /// </summary>
        /// <param name="portraitId">The NWN portrait 2DA Id.</param>
        /// <returns>The internal Id of the portrait.</returns>
        public static int GetPortraitInternalId(int portraitId)
        {
            return PortraitInternalIdsByPortraitId[portraitId];
        }

        /// <summary>
        /// Retrieves the resref of the portrait by the internal portrait Id.
        /// The size of the portrait needs to be appended to the end of this result.
        /// </summary>
        /// <param name="portraitInternalId">The internal portrait Id</param>
        /// <returns>The resref of the portrait, excluding the size.</returns>
        public static string GetPortraitResrefByInternalId(int portraitInternalId)
        {
            return PortraitResrefByInternalId[portraitInternalId];
        }

        public static int GetPortraitInternalIdByResref(string resref)
        {
            if (!PortraitInternalIdsByPortraitResref.ContainsKey(resref))
                return -1;

            return PortraitInternalIdsByPortraitResref[resref];
        }

        public static Dictionary<int, string> GetSoundSets()
        {
            return SoundSets.ToDictionary(x => x.Key, y => y.Value);
        }
    }
}
