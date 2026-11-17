using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The spawn registry that was fields of <see cref="SpawnService"/>, moved here unchanged (P5-8c): the spawn tables, the
    /// spawns of every area, the live spawned objects, the queued respawns and the despawn queues, with the boot handlers that
    /// fill them and the hot reload carry that hands them to the next build. The spawn runtime reads and changes the
    /// collections through <see cref="ISpawnRegistryService"/>.
    /// </summary>
    public class SpawnRegistryService : ISpawnRegistryService
    {
        /// <summary>The key the spawn registry is carried under across a hot reload (<see cref="ReloadCarry"/>).</summary>
        private const string SpawnCarryKey = "spawns";

        // The spawn registry: filled by the boot-only StoreSpawns and the spawn system as it runs; a hot reload carries it to
        // the new build (CarrySpawns, AdoptSpawns), so the live spawns keep their ids and no area spawns a second set.
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<Guid, SpawnDetail> _spawns = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly List<QueuedSpawn> _queuedSpawns = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<uint, List<QueuedSpawn>> _queuedSpawnsByArea = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<uint, DateTime> _queuedAreaDespawns = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly List<ResourceDespawn> _queuedResourceDespawns = new();
        // Some tables roll their respawn delays at random as they load; the spawns keep the delays the boot copied into them.
        [ReloadState(ReloadStateKind.Rerolled)]
        private readonly Dictionary<string, SpawnTable> _spawnTables = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<uint, List<Guid>> _allSpawnsByArea = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<uint, List<ActiveSpawn>> _activeSpawnsByArea = new();
        // True once LoadSpawnTables ran to its end. StoreSpawns refuses to run otherwise: the single CacheData stopped at its
        // first failure, so the hand-placed creatures were never captured against a partly loaded table cache.
        private bool _spawnTablesLoaded;

        public Dictionary<Guid, SpawnDetail> Spawns => _spawns;

        public List<QueuedSpawn> QueuedSpawns => _queuedSpawns;

        public Dictionary<uint, List<QueuedSpawn>> QueuedSpawnsByArea => _queuedSpawnsByArea;

        public Dictionary<uint, DateTime> QueuedAreaDespawns => _queuedAreaDespawns;

        public List<ResourceDespawn> QueuedResourceDespawns => _queuedResourceDespawns;

        public Dictionary<string, SpawnTable> SpawnTables => _spawnTables;

        public Dictionary<uint, List<Guid>> AllSpawnsByArea => _allSpawnsByArea;

        public Dictionary<uint, List<ActiveSpawn>> ActiveSpawnsByArea => _activeSpawnsByArea;

        /// <summary>
        /// The spawn table definitions of every loaded assembly. <c>protected virtual</c> so a unit test registers the
        /// definitions it wants instead of the whole server's; the body is the original search, and it stays lazy: each
        /// definition is built when the loop reaches it.
        /// </summary>
        protected virtual IEnumerable<Type> GetSpawnListDefinitionTypes()
        {
            return GameAssemblies.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(ISpawnListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);
        }

        /// <summary>
        /// Writes a configuration error to the error log. <c>protected virtual</c> so a unit test can read what the
        /// spawn tables and areas reported; the body is the original <c>Log.Write</c> call.
        /// </summary>
        protected virtual void WriteError(string message)
        {
            Log.Write(LogGroup.Error, message);
        }

        /// <summary>
        /// When the module loads, all spawn tables are loaded with reflection and stored into a dictionary cache.
        /// If any spawn tables with the same ID are found, an exception will be raised. It only fills the cache, so a hot
        /// reload runs it again; <see cref="StoreSpawns"/> runs right after it at boot.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void LoadSpawnTables()
        {
            _spawnTablesLoaded = false;
            // Get all implementations of spawn table definitions.
            var types = GetSpawnListDefinitionTypes();

            foreach (var type in types)
            {
                var instance = (ISpawnListDefinition)Activator.CreateInstance(type);
                var builtTables = instance.BuildSpawnTables();

                foreach (var table in builtTables)
                {
                    if (string.IsNullOrWhiteSpace(table.Key))
                    {
                        WriteError($"Spawn table {table.Key} has an invalid key. Values must be greater than zero.");
                        continue;
                    }

                    if (_spawnTables.ContainsKey(table.Key))
                    {
                        WriteError($"Spawn table {table.Key} has already been registered. Please make sure all spawn tables use a unique ID.");
                        continue;
                    }

                    _spawnTables[table.Key] = table.Value;
                }
            }

            _spawnTablesLoaded = true;
        }


        /// <summary>
        /// When the module loads, spawns are located in all areas. Details about those spawns are stored into the cached data.
        /// Spawns can be hand placed creatures, waypoints, or marked as a local variable on the area.
        /// Resource spawn tables use 'RESOURCE_SPAWN_TABLE_ID' for the table name and 'RESOURCE_SPAWN_COUNT' for the number of spawns.
        /// Creature spawn tables use 'CREATURE_SPAWN_TABLE_ID' for the table name and 'CREATURE_SPAWN_COUNT' for the number of spawns.
        /// Runs after <see cref="LoadSpawnTables"/>. Boot only: it destroys the hand-placed creatures and gives every spawn a
        /// new id, so a second run would capture and destroy the creatures the spawn system placed. A hot reload carries the
        /// registry instead (<see cref="CarrySpawns"/>, <see cref="AdoptSpawns"/>).
        /// </summary>
        [BootPhase(BootReload.BootOnly, AdoptedBy = nameof(AdoptSpawns), CarriedBy = nameof(CarrySpawns))]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void StoreSpawns()
        {
            if (!_spawnTablesLoaded)
                throw new InvalidOperationException("The spawns are not stored: LoadSpawnTables did not finish.");

            void RegisterAreaSpawnTable(uint area, string variableName, int spawnCount)
            {
                var spawnTableId = GetLocalString(area, variableName);
                if (!string.IsNullOrWhiteSpace(spawnTableId))
                {
                    if (!_spawnTables.ContainsKey(spawnTableId))
                    {
                        WriteError($"Area has an invalid spawn table Id. ({spawnTableId}) is not defined. Do you have the right spawn table Id?");
                        return;
                    }

                    var spawnTable = _spawnTables[spawnTableId];
                    if (spawnTable.Spawns == null || spawnTable.Spawns.Count == 0)
                    {
                        WriteError($"Spawn table {spawnTableId} has no spawn objects defined. Skipping area spawn setup.");
                        return;
                    }

                    for (var count = 1; count <= spawnCount; count++)
                    {
                        var id = Guid.NewGuid();
                        _spawns.Add(id, new SpawnDetail
                        {
                            SpawnTableId = spawnTableId,
                            Area = area,
                            RespawnDelayMinutes = spawnTable.RespawnDelayMinutes,
                            RespawnDelayMaximumMinutes = spawnTable.RespawnDelayMaximumMinutes,
                            UseRandomSpawnLocation = true
                        });

                        if (!_allSpawnsByArea.ContainsKey(area))
                            _allSpawnsByArea[area] = new List<Guid>();

                        _allSpawnsByArea[area].Add(id);
                    }
                }
            }

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                // Process spawns within the area.
                for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
                {
                    var type = GetObjectType(obj);
                    var position = GetPosition(obj);
                    var facing = GetFacing(obj);
                    var id = Guid.NewGuid();

                    // Hand-placed creature information is stored and the actual NPC is destroyed so it can be spawned by the system.
                    if (type == ObjectType.Creature)
                    {
                        // Some plot creatures use the Object Visibility service.  This relies on object references so we
                        // should not spawn new instances of those creatures.  Just leave them as they are.
                        if (!String.IsNullOrEmpty(GetLocalString(obj, "VISIBILITY_OBJECT_ID")))
                        {
                            continue;
                        }

                        _spawns.Add(id, new SpawnDetail
                        {
                            SerializedObject = ObjectPlugin.Serialize(obj),
                            X = position.X,
                            Y = position.Y,
                            Z = position.Z,
                            Facing = facing,
                            Area = area,
                            RespawnDelayMinutes = 5,
                            RespawnDelayMaximumMinutes = 5
                        });

                        // Add this entry to the spawns by area cache.
                        if (!_allSpawnsByArea.ContainsKey(area))
                            _allSpawnsByArea[area] = new List<Guid>();

                        _allSpawnsByArea[area].Add(id);

                        DestroyObject(obj);
                    }
                    // Waypoints with a spawn table ID in the tag
                    else if (type == ObjectType.Waypoint)
                    {
                        var spawnTableId = GetTag(obj);
                        if (_spawnTables.ContainsKey(spawnTableId))
                        {
                            var spawnTable = _spawnTables[spawnTableId];
                            _spawns.Add(id, new SpawnDetail
                            {
                                SpawnTableId = spawnTableId,
                                X = position.X,
                                Y = position.Y,
                                Z = position.Z,
                                Facing = facing,
                                Area = area,
                                RespawnDelayMinutes = spawnTable.RespawnDelayMinutes,
                                RespawnDelayMaximumMinutes = spawnTable.RespawnDelayMaximumMinutes
                            });

                            // Add this entry to the spawns by area cache.
                            if (!_allSpawnsByArea.ContainsKey(area))
                                _allSpawnsByArea[area] = new List<Guid>();

                            _allSpawnsByArea[area].Add(id);
                        }
                    }
                }

                // Resource, creature, and slicing-terminal spawn tables can be placed as a local variable on the area.
                // If one is found, it will be registered.
                RegisterAreaSpawnTable(area, "RESOURCE_SPAWN_TABLE_ID", CalculateResourceSpawnCount(area));
                RegisterAreaSpawnTable(area, "CREATURE_SPAWN_TABLE_ID", CalculateCreatureSpawnCount(area));
                var slicingTerminalCount = Math.Max(1, GetLocalInt(area, "SLICING_TERMINAL_SPAWN_COUNT"));
                RegisterAreaSpawnTable(area, "SLICING_TERMINAL_SPAWN_TABLE_ID", slicingTerminalCount);
            }
        }

        /// <summary>
        /// When a hot reload quiesces this build: leaves the spawn registry on the module for the next build
        /// (<see cref="AdoptSpawns"/>): every spawn with its id and detail (the hand-placed creatures that boot destroyed live
        /// only here), the spawns of each area, the live spawned objects, the queued respawns, area despawns and resource
        /// despawns. It changes nothing.
        /// </summary>
        public void CarrySpawns()
        {
            var queuedIndex = new Dictionary<QueuedSpawn, int>(ReferenceEqualityComparer.Instance);
            for (var index = 0; index < _queuedSpawns.Count; index++)
                queuedIndex[_queuedSpawns[index]] = index;

            var carry = new JObject
            {
                ["spawns"] = new JArray(_spawns.Select(pair => new JObject { ["id"] = pair.Key, ["detail"] = JObject.FromObject(pair.Value) })),
                ["byArea"] = new JArray(_allSpawnsByArea.Select(pair => new JObject { ["area"] = pair.Key, ["ids"] = JArray.FromObject(pair.Value) })),
                ["active"] = new JArray(_activeSpawnsByArea.Select(pair => new JObject { ["area"] = pair.Key, ["spawns"] = JArray.FromObject(pair.Value) })),
                ["queued"] = JArray.FromObject(_queuedSpawns),
                ["queuedByArea"] = new JArray(_queuedSpawnsByArea.Select(pair => new JObject
                {
                    ["area"] = pair.Key,
                    ["indexes"] = JArray.FromObject(pair.Value.Select(queued => queuedIndex.TryGetValue(queued, out var index) ? index : -1))
                })),
                ["areaDespawns"] = new JArray(_queuedAreaDespawns.Select(pair => new JObject { ["area"] = pair.Key, ["time"] = pair.Value })),
                ["resourceDespawns"] = JArray.FromObject(_queuedResourceDespawns),
            };

            ReloadCarry.Put(SpawnCarryKey, carry.ToString(Formatting.None));
        }

        /// <summary>
        /// On a hot reload, after the rerun <see cref="LoadSpawnTables"/>: stands in for <see cref="StoreSpawns"/> by taking the
        /// registry the old build carried (<see cref="CarrySpawns"/>), so the live spawned objects keep their spawn ids, an
        /// occupied area does not spawn a second set and an empty one spawns its hand-placed creatures as before. It creates and
        /// destroys nothing. Without a carried registry it reports that no area spawns until a restart.
        /// </summary>
        public void AdoptSpawns()
        {
            var text = ReloadCarry.Take(SpawnCarryKey);
            if (text == null)
            {
                WriteError("Hot reload: the build this one replaced carried no spawn registry, so no area spawns until the server restarts.");
                return;
            }

            var carry = JObject.Parse(text);
            _spawns.Clear();
            _allSpawnsByArea.Clear();
            _activeSpawnsByArea.Clear();
            _queuedSpawns.Clear();
            _queuedSpawnsByArea.Clear();
            _queuedAreaDespawns.Clear();
            _queuedResourceDespawns.Clear();

            foreach (var entry in carry["spawns"])
                _spawns[entry["id"].ToObject<Guid>()] = entry["detail"].ToObject<SpawnDetail>();
            foreach (var entry in carry["byArea"])
                _allSpawnsByArea[entry["area"].ToObject<uint>()] = entry["ids"].ToObject<List<Guid>>();
            foreach (var entry in carry["active"])
                _activeSpawnsByArea[entry["area"].ToObject<uint>()] = entry["spawns"].ToObject<List<ActiveSpawn>>();

            _queuedSpawns.AddRange(carry["queued"].ToObject<List<QueuedSpawn>>());
            foreach (var entry in carry["queuedByArea"])
            {
                _queuedSpawnsByArea[entry["area"].ToObject<uint>()] = entry["indexes"].ToObject<List<int>>()
                    .Where(index => index >= 0 && index < _queuedSpawns.Count)
                    .Select(index => _queuedSpawns[index])
                    .ToList();
            }

            foreach (var entry in carry["areaDespawns"])
                _queuedAreaDespawns[entry["area"].ToObject<uint>()] = entry["time"].ToObject<DateTime>();
            _queuedResourceDespawns.AddRange(carry["resourceDespawns"].ToObject<List<ResourceDespawn>>());

            Log.Write(LogGroup.Server, $"Hot reload: adopted {_spawns.Count} spawns in {_allSpawnsByArea.Count} areas, " +
                                       $"{_activeSpawnsByArea.Sum(area => area.Value.Count)} live and {_queuedSpawns.Count} queued.", true);
        }

        /// <summary>
        /// Determines the number of spawns to use in an area.
        /// If an int local variable 'RESOURCE_SPAWN_COUNT' is found, use that number.
        /// Otherwise the size of the area will be used to determine the count.
        /// </summary>
        /// <param name="area">The area to determine spawn counts for</param>
        /// <returns>A positive integer indicating the number of resource spawns to use in a given area.</returns>
        private int CalculateResourceSpawnCount(uint area)
        {
            var count = GetLocalInt(area, "RESOURCE_SPAWN_COUNT");

            // Found the local variable. Use that count.
            if (count > 0) return count;

            // Local variable wasn't found or was zero.
            // Determine the count by the size of the area.
            var width = GetAreaSize(Dimension.Width, area);
            var height = GetAreaSize(Dimension.Height, area);
            var size = width * height;

            if (size <= 12)
                count = 2;
            else if (size <= 32)
                count = 6;
            else if (size <= 64)
                count = 10;
            else if (size <= 256)
                count = 25;
            else if (size <= 512)
                count = 40;
            else if (size <= 1024)
                count = 50;

            return count;
        }

        private int CalculateCreatureSpawnCount(uint area)
        {
            var count = GetLocalInt(area, "CREATURE_SPAWN_COUNT");

            // Found the local variable. Use that count.
            if (count > 0) return count;

            // Local variable wasn't found or was zero.
            // Determine the count by the size of the area.
            var width = GetAreaSize(Dimension.Width, area);
            var height = GetAreaSize(Dimension.Height, area);
            var size = width * height;

            if (size <= 12)
                count = 3;
            else if (size <= 32)
                count = 6;
            else if (size <= 64)
                count = 14;
            else if (size <= 256)
                count = 20;
            else if (size <= 512)
                count = 35;
            else if (size <= 1024)
                count = 45;

            return count;
        }
    }
}
