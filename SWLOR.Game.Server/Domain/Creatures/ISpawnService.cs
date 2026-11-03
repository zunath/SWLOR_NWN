namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The spawn system: spawn tables, hand-placed and table spawns per area, queued respawns and despawns. The
    /// static <c>Service.Spawn</c> forwards to the container's instance of this interface.
    /// </summary>
    public interface ISpawnService
    {
        /// <summary>
        /// When the module loads, the spawn tables are loaded and the spawns of every area are stored.
        /// </summary>
        void CacheData();

        /// <summary>
        /// When a player or DM enters an area with no active or queued spawns, every spawn of the area is queued.
        /// </summary>
        void SpawnArea();

        /// <summary>
        /// When the last player in an area leaves, a despawn request is queued up.
        /// The heartbeat processor will despawn all objects when this happens
        /// </summary>
        void QueueDespawnArea();

        /// <summary>
        /// Moves a spawned resource's natural despawn forward so it despawns and frees its spawn slot
        /// no later than the given delay from now. Used for searched resources which still hold items.
        /// An earlier scheduled despawn is kept. Resources not created by the spawn system are left alone.
        /// </summary>
        /// <param name="resourceObject">The resource object to despawn</param>
        /// <param name="delay">The maximum amount of time before the resource despawns</param>
        /// <returns>true if a despawn is scheduled, false otherwise</returns>
        bool ScheduleResourceDespawn(uint resourceObject, TimeSpan delay);

        /// <summary>
        /// When a creature dies, its details need to be queued up for a respawn.
        /// NOTE: plc_death and crea_death_aft will not trigger if the object is 'killed'
        /// via DestroyObject.  Call this method directly if you need to use DestroyObject
        /// on a respawning object.
        /// </summary>
        void QueueRespawn();

        /// <summary>
        /// Removes an active spawned object and queues its replacement using its spawn
        /// table's respawn window. Used by interactable resources which are consumed
        /// without firing a placeable death event.
        /// </summary>
        bool DespawnAndQueueRespawn(uint spawnObject);

        /// <summary>
        /// On each module heartbeat, process queued spawns and
        /// process dequeue area event requests.
        /// </summary>
        void ProcessSpawnSystem();

        /// <summary>
        /// On each module heartbeat, iterate over the list of queued spawns.
        /// If enough time has elapsed and spawn table rules are met, spawn the object and remove it from the queue.
        /// </summary>
        void ProcessQueuedSpawns();

        /// <summary>
        /// When a DM spawns a creature, attach all required scripts to it.
        /// </summary>
        void DMSpawnCreature();

        /// <summary>
        /// Attaches the scripts and stat adjustments every DM-spawned creature receives.
        /// Used by the native DM creator event and the NUI DM creator.
        /// </summary>
        /// <param name="creature">The creature a DM spawned.</param>
        void PrepareDMSpawnedCreature(uint creature);

        /// <summary>
        /// Registers an existing placeable as an active resource spawn from the given spawn table,
        /// with that table's natural despawn queued. Engine tests only.
        /// </summary>
        Guid RegisterResourceSpawnForTesting(uint resourceObject, string spawnTableId);

        bool IsSpawnActiveForTesting(Guid spawnId);

        int GetQueuedSpawnCountForTesting(Guid spawnId);

        DateTime? GetResourceDespawnTimeForTesting(uint resourceObject);

        void ProcessResourceDespawnsForTesting(DateTime now);

        /// <summary>
        /// Removes every trace of a spawn registered by <see cref="RegisterResourceSpawnForTesting"/>.
        /// </summary>
        void RemoveSpawnForTesting(Guid spawnId);
    }
}
