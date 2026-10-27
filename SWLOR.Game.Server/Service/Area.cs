using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the area cache. The logic and state live in <see cref="IAreaService"/>; every member here
    /// forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change.
    /// </summary>
    public static class Area
    {
        private static IAreaService Instance => FacadeAccessor.Resolve<IAreaService>();

        /// <summary>
        /// Creates an area instance with the same event handlers as the startup areas.
        /// CreateArea loads the original resource, which does not include runtime script overrides.
        /// </summary>
        public static uint CreateInstance(string sourceResref, string newTag = "", string newName = "")
        {
            return Instance.CreateInstance(sourceResref, newTag, newName);
        }

        public static void RegisterEvents(uint area)
        {
            Instance.RegisterEvents(area);
        }

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// Remove instance templates from the area cache on module load.
        /// This ensures player locations are not updated in places they shouldn't be.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RemoveInstancesFromCache()
        {
            Instance.RemoveInstancesFromCache();
        }

        /// <summary>
        /// Retrieves an area by its resref. If the area does not exist, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="resref">The resref to use for the search.</param>
        /// <returns>The area ID or OBJECT_INVALID if area does not exist.</returns>
        public static uint GetAreaByResref(string resref)
        {
            return Instance.GetAreaByResref(resref);
        }

        /// <summary>
        /// Retrieves list of all areas.
        /// </summary>
        /// <returns>AreasByResref cache.</returns>
        public static Dictionary<string, uint> GetAreas()
        {
            return Instance.GetAreas();
        }

        /// <summary>
        /// Retrieves all of the players currently in the specified area.
        /// If no players are in the area, an empty list will returned.
        /// </summary>
        /// <param name="area">The area to search by.</param>
        /// <returns>A list of player objects</returns>
        public static List<uint> GetPlayersInArea(uint area)
        {
            return Instance.GetPlayersInArea(area);
        }

        /// <summary>
        /// When a player or DM enters an area, add them to the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void EnterArea()
        {
            Instance.EnterArea();
        }

        /// <summary>
        /// When a player or DM leaves an area, remove them from the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void ExitArea()
        {
            Instance.ExitArea();
        }
    }
}
