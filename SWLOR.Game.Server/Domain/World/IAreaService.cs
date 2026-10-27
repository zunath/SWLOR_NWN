using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The area cache: areas by resref, the players in each area, and the DM-written area notes shown on entry. The
    /// static facade <c>Service.Area</c> forwards to the container's instance of this interface and keeps the event
    /// handlers.
    /// </summary>
    public interface IAreaService
    {
        /// <summary>
        /// Creates an area instance with the same event handlers as the startup areas.
        /// CreateArea loads the original resource, which does not include runtime script overrides.
        /// </summary>
        uint CreateInstance(string sourceResref, string newTag = "", string newName = "");

        /// <summary>
        /// Points an area's enter, exit and user-defined events at the shared handlers and turns its heartbeat off.
        /// </summary>
        void RegisterEvents(uint area);

        /// <summary>
        /// When the module's caches are built, cache every area by its resref.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Remove instance templates from the area cache on module load.
        /// This ensures player locations are not updated in places they shouldn't be.
        /// </summary>
        void RemoveInstancesFromCache();

        /// <summary>
        /// Retrieves an area by its resref. If the area does not exist, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="resref">The resref to use for the search.</param>
        /// <returns>The area ID or OBJECT_INVALID if area does not exist.</returns>
        uint GetAreaByResref(string resref);

        /// <summary>
        /// Retrieves list of all areas.
        /// </summary>
        /// <returns>AreasByResref cache.</returns>
        Dictionary<string, uint> GetAreas();

        /// <summary>
        /// Retrieves all of the players currently in the specified area.
        /// If no players are in the area, an empty list will returned.
        /// </summary>
        /// <param name="area">The area to search by.</param>
        /// <returns>A list of player objects</returns>
        List<uint> GetPlayersInArea(uint area);

        /// <summary>
        /// When a player or DM enters an area, add them to the cache.
        /// </summary>
        void EnterArea();

        /// <summary>
        /// When a player or DM leaves an area, remove them from the cache.
        /// </summary>
        void ExitArea();
    }
}
