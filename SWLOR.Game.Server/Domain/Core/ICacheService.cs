using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// NWN data which lives for the lifespan of the server: item names, icons and searchability, portraits and sound
    /// sets. Nothing in here is permanently stored; it only makes queries quicker. If you need persistent storage, refer
    /// to the DB class. The static facade <c>Service.Cache</c> forwards to the container's instance of this interface and
    /// keeps the event handler.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Handles caching data into server memory for quicker lookup later.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Searches the cached item catalog for items whose name contains the given text. Matching is
        /// case-insensitive and runs against the in-memory name cache, so it is safe to call on demand.
        /// Results are limited to items eligible for player-facing economy surfaces: player-market
        /// tradeable, and not an NPC/creature/internal item (see <see cref="Items.IItemService.IsEconomyRestricted(uint)"/>).
        /// </summary>
        /// <param name="search">The partial name to search for. Whitespace/empty returns no results.</param>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <returns>Matching (resref, name) pairs ordered by name, capped at <paramref name="maxResults"/>.</returns>
        List<(string Resref, string Name)> SearchItemsByName(string search, int maxResults);

        /// <summary>
        /// Determines whether an item blueprint may appear in player-facing economy search surfaces:
        /// player-market tradeable, and not an NPC/creature/internal item. If the resref isn't cached
        /// (i.e. it wasn't part of the module's item palette at boot), the item is spawned once to evaluate it.
        /// </summary>
        /// <param name="resref">The item blueprint resref to check.</param>
        /// <returns>true if the item may be shown to players, false otherwise.</returns>
        bool IsItemSearchableByResref(string resref);

        /// <summary>
        /// Retrieves the name of an item by its resref. If resref cannot be found, an empty string will be returned.
        /// </summary>
        /// <param name="resref">The resref to search for.</param>
        /// <returns>The name of an item, or an empty string if it cannot be found.</returns>
        string GetItemNameByResref(string resref);

        /// <summary>
        /// Retrieves the inventory icon resref of an item by its blueprint resref, using the same icon
        /// rules as the player market (<see cref="Items.IItemService.GetIconResref"/>). If the resref isn't cached
        /// (i.e. it wasn't part of the module's item palette at boot), the item is spawned once to capture it.
        /// </summary>
        /// <param name="resref">The item blueprint resref to look up.</param>
        /// <returns>The icon resref for the item.</returns>
        string GetItemIconByResref(string resref);

        /// <summary>
        /// Retrieves the number of portraits registered in the system.
        /// </summary>
        int PortraitCount { get; }

        /// <summary>
        /// Retrieves the portrait 2DA Id from the internal Id of the portrait.
        /// The value returned by this method can be used with NWScript.SetPortrait
        /// </summary>
        /// <param name="portraitInternalId">The internal portrait Id to retrieve.</param>
        /// <returns>The 2DA Id of the portrait.</returns>
        int GetPortraitByInternalId(int portraitInternalId);

        /// <summary>
        /// Retrieves the internal Id of a portrait by its NWN 2DA Id.
        /// </summary>
        /// <param name="portraitId">The NWN portrait 2DA Id.</param>
        /// <returns>The internal Id of the portrait.</returns>
        int GetPortraitInternalId(int portraitId);

        /// <summary>
        /// Retrieves the resref of the portrait by the internal portrait Id.
        /// The size of the portrait needs to be appended to the end of this result.
        /// </summary>
        /// <param name="portraitInternalId">The internal portrait Id</param>
        /// <returns>The resref of the portrait, excluding the size.</returns>
        string GetPortraitResrefByInternalId(int portraitInternalId);

        /// <summary>The internal Id of the portrait with a resref, or -1 when there is none.</summary>
        int GetPortraitInternalIdByResref(string resref);

        /// <summary>A copy of the sound sets: 2DA row to display name, ordered by name.</summary>
        Dictionary<int, string> GetSoundSets();

        /// <summary>The resref of the sound that previews a sound set, or an empty string when it has none.</summary>
        string GetSoundSetPreviewSoundResref(int soundSetId);
    }
}
