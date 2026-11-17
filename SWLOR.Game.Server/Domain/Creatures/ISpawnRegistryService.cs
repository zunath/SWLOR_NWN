using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The spawn registry: the spawn tables, the registered spawns of every area, the live spawned objects, the queued
    /// respawns and the despawn queues, with the boot handlers that fill them and the hot reload carry that hands them to the
    /// next build. The spawn runtime (<see cref="ISpawnService"/>) reads and changes the collections.
    /// </summary>
    public interface ISpawnRegistryService
    {
        /// <summary>
        /// When the module loads, the spawn tables are loaded into the cache. Writes nothing else; a hot reload runs it again.
        /// </summary>
        void LoadSpawnTables();

        /// <summary>
        /// At boot, right after <see cref="LoadSpawnTables"/>: the spawns of every area are stored and the hand-placed
        /// creatures are destroyed so the spawn system places them.
        /// </summary>
        void StoreSpawns();

        /// <summary>
        /// When a hot reload quiesces this build: leaves the registry on the module for the next build (<see cref="AdoptSpawns"/>).
        /// It changes nothing.
        /// </summary>
        void CarrySpawns();

        /// <summary>
        /// On a hot reload, after the rerun <see cref="LoadSpawnTables"/>: takes the registry the old build carried, so the live
        /// spawned objects keep their spawn ids and no area spawns a second set.
        /// </summary>
        void AdoptSpawns();

        /// <summary>Every registered spawn, by spawn id.</summary>
        Dictionary<Guid, SpawnDetail> Spawns { get; }

        /// <summary>The spawns waiting for their respawn time.</summary>
        List<QueuedSpawn> QueuedSpawns { get; }

        /// <summary>The spawns waiting for their respawn time, by area.</summary>
        Dictionary<uint, List<QueuedSpawn>> QueuedSpawnsByArea { get; }

        /// <summary>The time each emptied area despawns, by area.</summary>
        Dictionary<uint, DateTime> QueuedAreaDespawns { get; }

        /// <summary>The spawned resources waiting to despawn.</summary>
        List<ResourceDespawn> QueuedResourceDespawns { get; }

        /// <summary>The spawn tables, by table id.</summary>
        Dictionary<string, SpawnTable> SpawnTables { get; }

        /// <summary>The spawn ids registered in each area.</summary>
        Dictionary<uint, List<Guid>> AllSpawnsByArea { get; }

        /// <summary>The spawned objects alive in each area.</summary>
        Dictionary<uint, List<ActiveSpawn>> ActiveSpawnsByArea { get; }
    }
}
