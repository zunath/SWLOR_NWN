using SWLOR.Component.World.Models;

namespace SWLOR.Component.World.Contracts
{
    /// <summary>
    /// Service responsible for managing spawn data and area-specific operations.
    /// </summary>
    public interface ISpawnManagementService
    {
        /// <summary>
        /// Stores spawn details for all areas when the module loads.
        /// </summary>
        void StoreSpawns();

        /// <summary>
        /// Spawns all objects in an area when a player enters.
        /// </summary>
        void SpawnArea();

        /// <summary>
        /// Queues an area for despawning when the last player leaves.
        /// </summary>
        void QueueDespawnArea();

        /// <summary>
        /// Queues a creature for respawning when it dies.
        /// </summary>
        void QueueRespawn();

        /// <summary>
        /// Gets all spawn IDs for an area.
        /// </summary>
        /// <param name="area">The area to get spawn IDs for</param>
        /// <returns>List of spawn IDs for the area</returns>
        List<Guid> GetAllSpawnsForArea(uint area);

        /// <summary>
        /// Adds a spawn detail to the collection.
        /// </summary>
        /// <param name="id">The spawn ID</param>
        /// <param name="detail">The spawn detail</param>
        void AddSpawnDetail(Guid id, SpawnDetail detail);

        /// <summary>
        /// Gets a spawn detail by ID.
        /// </summary>
        /// <param name="id">The spawn ID</param>
        /// <returns>The spawn detail if found, null otherwise</returns>
        SpawnDetail GetSpawnDetail(Guid id);

        /// <summary>
        /// Checks if an area has any spawns.
        /// </summary>
        /// <param name="area">The area to check</param>
        /// <returns>True if the area has spawns, false otherwise</returns>
        bool AreaHasSpawns(uint area);

        /// <summary>
        /// Checks if an area has active or queued spawns.
        /// </summary>
        /// <param name="area">The area to check</param>
        /// <returns>True if the area has active or queued spawns, false otherwise</returns>
        bool AreaHasActiveOrQueuedSpawns(uint area);
    }
}
