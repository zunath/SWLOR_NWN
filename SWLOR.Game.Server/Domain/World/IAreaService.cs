using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The area cache: areas by resref, the players in each area, and the DM-written area notes shown on entry. Static
    /// callers reach the container's instance through <c>GameServices.Area</c>; the event handlers are on
    /// <see cref="AreaService"/>.
    /// </summary>
    public interface IAreaService : IAreaLookup
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
        /// Retrieves list of all areas.
        /// </summary>
        /// <returns>AreasByResref cache.</returns>
        Dictionary<string, uint> GetAreas();

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
