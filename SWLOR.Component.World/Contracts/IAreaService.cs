namespace SWLOR.Shared.Core.Contracts
{
    public interface IAreaService
    {
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
        /// <param> </param>
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