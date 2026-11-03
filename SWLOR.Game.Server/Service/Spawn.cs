using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the spawn system. The logic and state live in <see cref="ISpawnService"/>; every member here
    /// forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change.
    /// </summary>
    public static class Spawn
    {
        private static ISpawnService Instance => FacadeAccessor.Resolve<ISpawnService>();

        public const int DespawnMinutes = Domain.Creatures.SpawnService.DespawnMinutes;
        public const int DefaultRespawnMinutes = 5;
        public const int LeftoverResourceDespawnMinutes = 5;

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void SpawnArea()
        {
            Instance.SpawnArea();
        }

        /// <summary>
        /// When the last player in an area leaves, a despawn request is queued up.
        /// The heartbeat processor will despawn all objects when this happens
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void QueueDespawnArea()
        {
            Instance.QueueDespawnArea();
        }

        /// <summary>
        /// Moves a spawned resource's natural despawn forward so it despawns and frees its spawn slot
        /// no later than the given delay from now. Used for searched resources which still hold items.
        /// An earlier scheduled despawn is kept. Resources not created by the spawn system are left alone.
        /// </summary>
        /// <param name="resourceObject">The resource object to despawn</param>
        /// <param name="delay">The maximum amount of time before the resource despawns</param>
        /// <returns>true if a despawn is scheduled, false otherwise</returns>
        public static bool ScheduleResourceDespawn(uint resourceObject, TimeSpan delay)
        {
            return Instance.ScheduleResourceDespawn(resourceObject, delay);
        }

        /// <summary>
        /// When a creature dies, its details need to be queued up for a respawn.
        /// NOTE: plc_death and crea_death_aft will not trigger if the object is 'killed'
        /// via DestroyObject.  Call this method directly if you need to use DestroyObject
        /// on a respawning object.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        [NWNEventHandler(ScriptName.OnPlaceableDeath)]
        public static void QueueRespawn()
        {
            Instance.QueueRespawn();
        }

        /// <summary>
        /// Removes an active spawned object and queues its replacement using its spawn
        /// table's respawn window. Used by interactable resources which are consumed
        /// without firing a placeable death event.
        /// </summary>
        public static bool DespawnAndQueueRespawn(uint spawnObject)
        {
            return Instance.DespawnAndQueueRespawn(spawnObject);
        }

        /// <summary>
        /// On each module heartbeat, process queued spawns and
        /// process dequeue area event requests.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public static void ProcessSpawnSystem()
        {
            Instance.ProcessSpawnSystem();
        }

        /// <summary>
        /// On each module heartbeat, iterate over the list of queued spawns.
        /// If enough time has elapsed and spawn table rules are met, spawn the object and remove it from the queue.
        /// </summary>
        public static void ProcessQueuedSpawns()
        {
            Instance.ProcessQueuedSpawns();
        }

        /// <summary>
        /// When a DM spawns a creature, attach all required scripts to it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMSpawnObjectAfter)]
        public static void DMSpawnCreature()
        {
            Instance.DMSpawnCreature();
        }

        /// <summary>
        /// Attaches the scripts and stat adjustments every DM-spawned creature receives.
        /// Used by the native DM creator event and the NUI DM creator.
        /// </summary>
        /// <param name="creature">The creature a DM spawned.</param>
        public static void PrepareDMSpawnedCreature(uint creature)
        {
            Instance.PrepareDMSpawnedCreature(creature);
        }

        /// <summary>
        /// Registers an existing placeable as an active resource spawn from the given spawn table,
        /// with that table's natural despawn queued. Engine tests only.
        /// </summary>
        public static Guid RegisterResourceSpawnForTesting(uint resourceObject, string spawnTableId)
        {
            return Instance.RegisterResourceSpawnForTesting(resourceObject, spawnTableId);
        }

        public static bool IsSpawnActiveForTesting(Guid spawnId)
        {
            return Instance.IsSpawnActiveForTesting(spawnId);
        }

        public static int GetQueuedSpawnCountForTesting(Guid spawnId)
        {
            return Instance.GetQueuedSpawnCountForTesting(spawnId);
        }

        public static DateTime? GetResourceDespawnTimeForTesting(uint resourceObject)
        {
            return Instance.GetResourceDespawnTimeForTesting(resourceObject);
        }

        public static void ProcessResourceDespawnsForTesting(DateTime now)
        {
            Instance.ProcessResourceDespawnsForTesting(now);
        }

        /// <summary>
        /// Removes every trace of a spawn registered by <see cref="RegisterResourceSpawnForTesting"/>.
        /// </summary>
        public static void RemoveSpawnForTesting(Guid spawnId)
        {
            Instance.RemoveSpawnForTesting(spawnId);
        }
    }
}
