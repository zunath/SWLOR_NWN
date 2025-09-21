using System.Collections.Generic;

namespace SWLOR.Game.Server.Service;

public interface ICacheService
{
    void CacheItemNamesByResref();

    /// <summary>
    /// Handles caching data into server memory for quicker lookup later.
    /// </summary>
    void CacheData();

    /// <summary>
    /// Retrieves the name of an item by its resref. If resref cannot be found, an empty string will be returned.
    /// </summary>
    /// <param name="resref">The resref to search for.</param>
    /// <returns>The name of an item, or an empty string if it cannot be found.</returns>
    string GetItemNameByResref(string resref);

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

    int GetPortraitInternalIdByResref(string resref);
    Dictionary<int, string> GetSoundSets();
}