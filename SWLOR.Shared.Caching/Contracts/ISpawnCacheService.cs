using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.World.ValueObjects;

namespace SWLOR.Shared.Caching.Contracts
{
    /// <summary>
    /// Service responsible for caching spawn table data.
    /// </summary>
    public interface ISpawnCacheService
    {
        /// <summary>
        /// Gets the spawn table cache.
        /// </summary>
        IInterfaceCache<string, SpawnTable> SpawnTableCache { get; }

        /// <summary>
        /// Loads and caches all spawn tables.
        /// </summary>
        void LoadSpawnTables();

        /// <summary>
        /// Gets a spawn table by ID.
        /// </summary>
        /// <param name="spawnTableId">The spawn table ID</param>
        /// <returns>The spawn table if found, null otherwise</returns>
        SpawnTable GetSpawnTable(string spawnTableId);

        /// <summary>
        /// Checks if a spawn table exists.
        /// </summary>
        /// <param name="spawnTableId">The spawn table ID</param>
        /// <returns>True if the spawn table exists, false otherwise</returns>
        bool HasSpawnTable(string spawnTableId);
    }
}
