using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the data cached for the lifespan of the server. The logic and state live in
    /// <see cref="ICacheService"/>; every member here forwards to the container's instance, and the event handler stays
    /// here so its name in the handler order manifest does not change.
    /// </summary>
    public static class Cache
    {
        private static ICacheService Instance => FacadeAccessor.Resolve<ICacheService>();

        /// <summary>
        /// Handles caching data into server memory for quicker lookup later.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// Searches the cached item catalog for items whose name contains the given text. Matching is
        /// case-insensitive and runs against the in-memory name cache, so it is safe to call on demand.
        /// Results are limited to items eligible for player-facing economy surfaces: player-market
        /// tradeable, and not an NPC/creature/internal item (see <see cref="Item.IsEconomyRestricted"/>).
        /// </summary>
        /// <param name="search">The partial name to search for. Whitespace/empty returns no results.</param>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <returns>Matching (resref, name) pairs ordered by name, capped at <paramref name="maxResults"/>.</returns>
        public static List<(string Resref, string Name)> SearchItemsByName(string search, int maxResults)
        {
            return Instance.SearchItemsByName(search, maxResults);
        }

        /// <summary>
        /// Determines whether an item blueprint may appear in player-facing economy search surfaces:
        /// player-market tradeable, and not an NPC/creature/internal item. If the resref isn't cached
        /// (i.e. it wasn't part of the module's item palette at boot), the item is spawned once to evaluate it.
        /// </summary>
        /// <param name="resref">The item blueprint resref to check.</param>
        /// <returns>true if the item may be shown to players, false otherwise.</returns>
        public static bool IsItemSearchableByResref(string resref)
        {
            return Instance.IsItemSearchableByResref(resref);
        }

        /// <summary>
        /// Retrieves the name of an item by its resref. If resref cannot be found, an empty string will be returned.
        /// </summary>
        /// <param name="resref">The resref to search for.</param>
        /// <returns>The name of an item, or an empty string if it cannot be found.</returns>
        public static string GetItemNameByResref(string resref)
        {
            return Instance.GetItemNameByResref(resref);
        }

        /// <summary>
        /// Retrieves the inventory icon resref of an item by its blueprint resref, using the same icon
        /// rules as the player market (<see cref="Item.GetIconResref"/>). If the resref isn't cached
        /// (i.e. it wasn't part of the module's item palette at boot), the item is spawned once to capture it.
        /// </summary>
        /// <param name="resref">The item blueprint resref to look up.</param>
        /// <returns>The icon resref for the item.</returns>
        public static string GetItemIconByResref(string resref)
        {
            return Instance.GetItemIconByResref(resref);
        }

        /// <summary>
        /// Retrieves the number of portraits registered in the system.
        /// </summary>
        public static int PortraitCount => Instance.PortraitCount;

        /// <summary>
        /// Retrieves the portrait 2DA Id from the internal Id of the portrait.
        /// The value returned by this method can be used with NWScript.SetPortrait
        /// </summary>
        /// <param name="portraitInternalId">The internal portrait Id to retrieve.</param>
        /// <returns>The 2DA Id of the portrait.</returns>
        public static int GetPortraitByInternalId(int portraitInternalId)
        {
            return Instance.GetPortraitByInternalId(portraitInternalId);
        }

        /// <summary>
        /// Retrieves the internal Id of a portrait by its NWN 2DA Id.
        /// </summary>
        /// <param name="portraitId">The NWN portrait 2DA Id.</param>
        /// <returns>The internal Id of the portrait.</returns>
        public static int GetPortraitInternalId(int portraitId)
        {
            return Instance.GetPortraitInternalId(portraitId);
        }

        /// <summary>
        /// Retrieves the resref of the portrait by the internal portrait Id.
        /// The size of the portrait needs to be appended to the end of this result.
        /// </summary>
        /// <param name="portraitInternalId">The internal portrait Id</param>
        /// <returns>The resref of the portrait, excluding the size.</returns>
        public static string GetPortraitResrefByInternalId(int portraitInternalId)
        {
            return Instance.GetPortraitResrefByInternalId(portraitInternalId);
        }

        public static int GetPortraitInternalIdByResref(string resref)
        {
            return Instance.GetPortraitInternalIdByResref(resref);
        }

        public static Dictionary<int, string> GetSoundSets()
        {
            return Instance.GetSoundSets();
        }

        public static string GetSoundSetPreviewSoundResref(int soundSetId)
        {
            return Instance.GetSoundSetPreviewSoundResref(soundSetId);
        }
    }
}
