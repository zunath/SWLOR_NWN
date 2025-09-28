using System;
using System.Collections.Generic;
using SWLOR.Component.World.Models;

namespace SWLOR.Component.World.Contracts
{
    /// <summary>
    /// Service responsible for processing spawn queues and despawning.
    /// </summary>
    public interface ISpawnProcessingService
    {
        /// <summary>
        /// Gets the number of minutes before spawned objects are despawned.
        /// </summary>
        int DespawnMinutes { get; }

        /// <summary>
        /// Gets the number of minutes before resources despawn naturally.
        /// </summary>
        int ResourceDespawnMinutes { get; }

        /// <summary>
        /// Processes the spawn system on each module heartbeat.
        /// </summary>
        void ProcessSpawnSystem();

        /// <summary>
        /// Processes queued spawns on each module heartbeat.
        /// </summary>
        void ProcessQueuedSpawns();

        /// <summary>
        /// Queues a resource for despawning after the configured amount of time.
        /// </summary>
        /// <param name="resourceObject">The resource object to despawn</param>
        /// <param name="spawnDetailId">The spawn detail ID of the resource</param>
        /// <param name="despawnMinutes">The number of minutes before despawning</param>
        void QueueResourceDespawn(uint resourceObject, Guid spawnDetailId, int despawnMinutes);

        /// <summary>
        /// Creates a queued spawn record which is picked up by the processor.
        /// </summary>
        /// <param name="spawnDetailId">The ID of the spawn detail.</param>
        /// <param name="respawnTime">The time the spawn will be created.</param>
        void CreateQueuedSpawn(Guid spawnDetailId, DateTime respawnTime);

        /// <summary>
        /// Removes a queued spawn.
        /// </summary>
        /// <param name="queuedSpawn">The queued spawn to remove.</param>
        void RemoveQueuedSpawn(object queuedSpawn);

        /// <summary>
        /// Gets all queued spawns for an area.
        /// </summary>
        /// <param name="area">The area to get queued spawns for</param>
        /// <returns>List of queued spawns for the area</returns>
        List<object> GetQueuedSpawnsForArea(uint area);

        /// <summary>
        /// Gets all active spawns for an area.
        /// </summary>
        /// <param name="area">The area to get active spawns for</param>
        /// <returns>List of active spawns for the area</returns>
        List<object> GetActiveSpawnsForArea(uint area);

        /// <summary>
        /// Adds an active spawn to an area.
        /// </summary>
        /// <param name="area">The area to add the spawn to</param>
        /// <param name="spawnDetailId">The spawn detail ID</param>
        /// <param name="spawnObject">The spawned object</param>
        void AddActiveSpawn(uint area, Guid spawnDetailId, uint spawnObject);

        /// <summary>
        /// Removes an active spawn from an area.
        /// </summary>
        /// <param name="area">The area to remove the spawn from</param>
        /// <param name="spawnObject">The spawned object to remove</param>
        void RemoveActiveSpawn(uint area, uint spawnObject);

        /// <summary>
        /// Clears all active spawns for an area.
        /// </summary>
        /// <param name="area">The area to clear spawns for</param>
        void ClearActiveSpawnsForArea(uint area);

        /// <summary>
        /// Queues an area for despawning.
        /// </summary>
        /// <param name="area">The area to queue for despawning</param>
        void QueueAreaDespawn(uint area);

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
    }
}
