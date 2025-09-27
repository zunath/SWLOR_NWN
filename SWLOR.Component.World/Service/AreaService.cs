using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.World.Contracts;

namespace SWLOR.Component.World.Service
{
    public class AreaService : IAreaService
    {
        private readonly IAreaNoteService _areaNoteService;
        private readonly IAreaCacheService _areaCacheService;

        public AreaService(
            IAreaNoteService areaNoteService,
            IAreaCacheService areaCacheService)
        {
            _areaNoteService = areaNoteService;
            _areaCacheService = areaCacheService;
        }

        /// <summary>
        /// Remove instance templates from the area cache on module load.
        /// This ensures player locations are not updated in places they shouldn't be.
        /// </summary>
        public void RemoveInstancesFromCache()
        {
            _areaCacheService.RemoveInstancesFromCache();
        }

        /// <summary>
        /// Retrieves an area by its resref. If the area does not exist, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="resref">The resref to use for the search.</param>
        /// <returns>The area ID or OBJECT_INVALID if area does not exist.</returns>
        public uint GetAreaByResref(string resref)
        {
            return _areaCacheService.GetAreaByResref(resref);
        }

        /// <summary>
        /// Retrieves list of all areas.
        /// </summary>
        /// <param> </param>
        /// <returns>AreasByResref cache.</returns>
        public Dictionary<string, uint> GetAreas()
        {
            return _areaCacheService.GetAreas();
        }

        /// <summary>
        /// Retrieves all of the players currently in the specified area.
        /// If no players are in the area, an empty list will returned.
        /// </summary>
        /// <param name="area">The area to search by.</param>
        /// <returns>A list of player objects</returns>
        public List<uint> GetPlayersInArea(uint area)
        {
            return _areaCacheService.GetPlayersInArea(area);
        }

        /// <summary>
        /// When a player or DM enters an area, add them to the cache.
        /// </summary>
        public void EnterArea()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player))
                return;

            var area = OBJECT_SELF;
            _areaCacheService.EnterArea(player, area);

            // Handle DM created Area Notes
            _areaNoteService.DisplayAreaNotes(player, area);
        }

        /// <summary>
        /// When a player or DM leaves an area, remove them from the cache.
        /// </summary>
        public void ExitArea()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player))
                return;

            var area = OBJECT_SELF;
            _areaCacheService.ExitArea(player, area);
        }

    }
}
