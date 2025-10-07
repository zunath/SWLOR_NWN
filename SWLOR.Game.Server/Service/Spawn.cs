using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;

namespace SWLOR.Game.Server.Service
{
    public static class Spawn
    {
        public const int DespawnMinutes = 20;
        public const int DefaultRespawnMinutes = 5;

        private class SpawnDetail
        {
            public string SerializedObject { get; set; }
            public string SpawnTableId { get; set; }
            public uint Area { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public float Facing { get; set; }
            public int RespawnDelayMinutes { get; set; }
            public bool UseRandomSpawnLocation { get; set; }
        }

        private class ActiveSpawn
        {
            public Guid SpawnDetailId { get; set; }
            public uint SpawnObject { get; set; }
        }

        private class QueuedSpawn
        {
            public DateTime RespawnTime { get; set; }
            public Guid SpawnDetailId { get; set; }
            public int FailureCount { get; set; } = 0;
        }

        private class ResourceDespawn
        {
            public uint ResourceObject { get; set; }
            public DateTime DespawnTime { get; set; }
            public Guid SpawnDetailId { get; set; }
        }

        private static readonly Dictionary<Guid, SpawnDetail> _spawns = new();
        private static readonly List<QueuedSpawn> _queuedSpawns = new();
        private static readonly Dictionary<uint, List<QueuedSpawn>> _queuedSpawnsByArea = new();
        private static readonly Dictionary<uint, DateTime> _queuedAreaDespawns = new();
        private static readonly List<ResourceDespawn> _queuedResourceDespawns = new();
        private static readonly Dictionary<string, SpawnTable> _spawnTables = new();
        private static readonly Dictionary<uint, List<Guid>> _allSpawnsByArea = new();
        private static readonly Dictionary<uint, List<ActiveSpawn>> _activeSpawnsByArea = new();

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            LoadSpawnTables();
            StoreSpawns();
        }

        /// <summary>
        /// When the module loads, all spawn tables are loaded with reflection and stored into a dictionary cache.
        /// If any spawn tables with the same ID are found, an exception will be raised.
        /// </summary>
        private static void LoadSpawnTables()
        {
            // Get all implementations of spawn table definitions.
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(ISpawnListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (ISpawnListDefinition)Activator.CreateInstance(type);
                var builtTables = instance.BuildSpawnTables();

                foreach (var table in builtTables)
                {
                    if (string.IsNullOrWhiteSpace(table.Key))
                    {
                        Log.Write(LogGroup.Error, $"Spawn table {table.Key} has an invalid key. Values must be greater than zero.");
                        continue;
                    }

                    if (_spawnTables.ContainsKey(table.Key))
                    {
                        Log.Write(LogGroup.Error, $"Spawn table {table.Key} has already been registered. Please make sure all spawn tables use a unique ID.");
                        continue;
                    }

                    _spawnTables[table.Key] = table.Value;
                }
            }
        }


        /// <summary>
        /// When the module loads, spawns are located in all areas. Details about those spawns are stored into the cached data.
        /// Spawns can be hand placed creatures, waypoints, or marked as a local variable on the area.
        /// Resource spawn tables use 'RESOURCE_SPAWN_TABLE_ID' for the table name and 'RESOURCE_SPAWN_COUNT' for the number of spawns.
        /// Creature spawn tables use 'CREATURE_SPAWN_TABLE_ID' for the table name and 'CREATURE_SPAWN_COUNT' for the number of spawns.
        /// </summary>
        private static void StoreSpawns()
        {
            void RegisterAreaSpawnTable(uint area, string variableName, int spawnCount)
            {
                var spawnTableId = GetLocalString(area, variableName);
                if (!string.IsNullOrWhiteSpace(spawnTableId))
                {
                    if (!_spawnTables.ContainsKey(spawnTableId))
                    {
                        Log.Write(LogGroup.Error, $"Area has an invalid spawn table Id. ({spawnTableId}) is not defined. Do you have the right spawn table Id?");
                        return;
                    }
                    
                    var spawnTable = _spawnTables[spawnTableId];
                    if (spawnTable.Spawns == null || spawnTable.Spawns.Count == 0)
                    {
                        Log.Write(LogGroup.Error, $"Spawn table {spawnTableId} has no spawn objects defined. Skipping area spawn setup.");
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
                            RespawnDelayMinutes = 5
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
                                RespawnDelayMinutes = spawnTable.RespawnDelayMinutes
                            });

                            // Add this entry to the spawns by area cache.
                            if (!_allSpawnsByArea.ContainsKey(area))
                                _allSpawnsByArea[area] = new List<Guid>();

                            _allSpawnsByArea[area].Add(id);
                        }
                    }
                }

                // Resource and creature spawn tables can be placed as a local variable on the area.
                // If one is found, it will be registered.
                RegisterAreaSpawnTable(area, "RESOURCE_SPAWN_TABLE_ID", CalculateResourceSpawnCount(area));
                RegisterAreaSpawnTable(area, "CREATURE_SPAWN_TABLE_ID", CalculateCreatureSpawnCount(area));
            }
        }

        /// <summary>
        /// Determines the number of spawns to use in an area.
        /// If an int local variable 'RESOURCE_SPAWN_COUNT' is found, use that number.
        /// Otherwise the size of the area will be used to determine the count.
        /// </summary>
        /// <param name="area">The area to determine spawn counts for</param>
        /// <returns>A positive integer indicating the number of resource spawns to use in a given area.</returns>
        private static int CalculateResourceSpawnCount(uint area)
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

        private static int CalculateCreatureSpawnCount(uint area)
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

        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void SpawnArea()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) && !GetIsDM(player)) return;

            var area = OBJECT_SELF;

            // Area isn't registered. Could be an instanced area? No need to spawn.
            if (!_allSpawnsByArea.ContainsKey(area)) return;

            if (!_activeSpawnsByArea.ContainsKey(area))
                _activeSpawnsByArea[area] = new List<ActiveSpawn>();

            if (!_queuedSpawnsByArea.ContainsKey(area))
                _queuedSpawnsByArea[area] = new List<QueuedSpawn>();

            var activeSpawns = _activeSpawnsByArea[area];
            var queuedSpawns = _queuedSpawnsByArea[area];

            // Spawns are currently active for this area. No need to spawn.
            if (activeSpawns.Count > 0 || queuedSpawns.Count > 0) return;

            var now = DateTime.UtcNow;
            // No spawns are active. Spawn the area.
            foreach (var spawn in _allSpawnsByArea[area])
            {
                CreateQueuedSpawn(spawn, now);
            }
        }

        /// <summary>
        /// When the last player in an area leaves, a despawn request is queued up.
        /// The heartbeat processor will despawn all objects when this happens
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void QueueDespawnArea()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) && !GetIsDM(player)) return;

            var area = OBJECT_SELF;
            var playerCount = AreaPlugin.GetNumberOfPlayersInArea(area);
            if (playerCount > 0) return;

            var now = DateTime.UtcNow;
            _queuedAreaDespawns[area] = now.AddMinutes(DespawnMinutes);
        }

        /// <summary>
        /// Queues a resource for despawning after the configured amount of time.
        /// This is automatically called when resources are spawned from resource spawn tables.
        /// A random variance of ±25% is applied to prevent all resources from despawning simultaneously.
        /// </summary>
        /// <param name="resourceObject">The resource object to despawn</param>
        /// <param name="spawnDetailId">The spawn detail ID of the resource</param>
        /// <param name="despawnMinutes">The number of minutes before despawning</param>
        private static void QueueResourceDespawn(uint resourceObject, Guid spawnDetailId, int despawnMinutes)
        {
            var now = DateTime.UtcNow;
            
            // Add random variance of ±25% to stagger despawn times
            var variancePercent = Random.Next(-25, 26); // -25% to +25%
            var variance = (int)(despawnMinutes * (variancePercent / 100.0f));
            var actualDespawnMinutes = despawnMinutes + variance;
            
            // Ensure minimum despawn time of 1 minute
            if (actualDespawnMinutes < 1)
                actualDespawnMinutes = 1;
            
            var resourceDespawn = new ResourceDespawn
            {
                ResourceObject = resourceObject,
                DespawnTime = now.AddMinutes(actualDespawnMinutes),
                SpawnDetailId = spawnDetailId
            };
            _queuedResourceDespawns.Add(resourceDespawn);
        }

        /// <summary>
        /// Creates a queued spawn record which is picked up by the processor.
        /// The spawn object will be created when the respawn time has passed.
        /// </summary>
        /// <param name="spawnDetailId">The ID of the spawn detail.</param>
        /// <param name="respawnTime">The time the spawn will be created.</param>
        private static void CreateQueuedSpawn(Guid spawnDetailId, DateTime respawnTime)
        {
            var queuedSpawn = new QueuedSpawn
            {
                RespawnTime = respawnTime,
                SpawnDetailId = spawnDetailId
            };
            _queuedSpawns.Add(queuedSpawn);

            var spawnDetail = _spawns[spawnDetailId];
            if (!_queuedSpawnsByArea.ContainsKey(spawnDetail.Area))
                _queuedSpawnsByArea[spawnDetail.Area] = new List<QueuedSpawn>();

            _queuedSpawnsByArea[spawnDetail.Area].Add(queuedSpawn);
        }

        /// <summary>
        /// Removes a queued spawn.
        /// </summary>
        /// <param name="queuedSpawn">The queued spawn to remove.</param>
        private static void RemoveQueuedSpawn(QueuedSpawn queuedSpawn)
        {
            var spawnDetail = _spawns[queuedSpawn.SpawnDetailId];
            _queuedSpawns.Remove(queuedSpawn);
            _queuedSpawnsByArea[spawnDetail.Area].Remove(queuedSpawn);
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
            uint creature = OBJECT_SELF;
            var spawnId = GetLocalString(creature, "SPAWN_ID");
            if (string.IsNullOrWhiteSpace(spawnId)) return;
            if (GetLocalInt(creature, "RESPAWN_QUEUED") == 1) return;

            var spawnGuid = new Guid(spawnId);
            var detail = _spawns[spawnGuid];
            var respawnTime = DateTime.UtcNow.AddMinutes(detail.RespawnDelayMinutes);

            CreateQueuedSpawn(spawnGuid, respawnTime);
            SetLocalInt(creature, "RESPAWN_QUEUED", 1);
        }

        /// <summary>
        /// On each module heartbeat, process queued spawns and
        /// process dequeue area event requests.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public static void ProcessSpawnSystem()
        {
            ProcessQueuedSpawns();
            ProcessDespawnAreas();
            ProcessResourceDespawns();
        }

        /// <summary>
        /// On each module heartbeat, iterate over the list of queued spawns.
        /// If enough time has elapsed and spawn table rules are met, spawn the object and remove it from the queue.
        /// </summary>
        public static void ProcessQueuedSpawns()
        {
            var now = DateTime.UtcNow;
            for (var index = _queuedSpawns.Count - 1; index >= 0; index--)
            {
                var queuedSpawn = _queuedSpawns.ElementAt(index);

                if (now > queuedSpawn.RespawnTime)
                {
                    var detail = _spawns[queuedSpawn.SpawnDetailId];
                    var spawnedObject = SpawnObject(queuedSpawn.SpawnDetailId, detail);

                    // A valid spawn wasn't found because the spawn table didn't provide a resref.
                    // Either the table is configured wrong or the requirements for that specific table weren't met.
                    if (spawnedObject == OBJECT_INVALID)
                    {
                        queuedSpawn.FailureCount++;
                        
                        // If we've failed too many times (10 attempts), remove this spawn to prevent infinite loops
                        if (queuedSpawn.FailureCount >= 10)
                        {
                            Log.Write(LogGroup.Error, $"Spawn {queuedSpawn.SpawnDetailId} failed 10 times consecutively. Removing from queue to prevent infinite spawning. Check spawn table configuration.");
                            RemoveQueuedSpawn(queuedSpawn);
                            continue;
                        }
                        
                        // Exponential backoff: delay gets longer with each failure
                        var backoffMinutes = detail.RespawnDelayMinutes * Math.Pow(2, Math.Min(queuedSpawn.FailureCount - 1, 4)); // Cap at 16x delay
                        queuedSpawn.RespawnTime = now.AddMinutes(backoffMinutes);
                        continue;
                    }

                    // Reset failure count on successful spawn
                    queuedSpawn.FailureCount = 0;

                    var activeSpawn = new ActiveSpawn
                    {
                        SpawnDetailId = queuedSpawn.SpawnDetailId,
                        SpawnObject = spawnedObject
                    };

                    _activeSpawnsByArea[detail.Area].Add(activeSpawn);
                    RemoveQueuedSpawn(queuedSpawn);
                }
            }
        }

        /// <summary>
        /// On each module heartbeat, iterate over the list of areas which are scheduled to
        /// be despawned. If players have since entered the area, remove it from the queue list.
        /// </summary>
        private static void ProcessDespawnAreas()
        {
            var now = DateTime.UtcNow;
            for (var index = _queuedAreaDespawns.Count - 1; index >= 0; index--)
            {
                var (area, despawnTime) = _queuedAreaDespawns.ElementAt(index);
                // Players have entered this area. Remove it and move to the next entry.
                if (AreaPlugin.GetNumberOfPlayersInArea(area) > 0)
                {
                    _queuedAreaDespawns.Remove(area);
                    continue;
                }

                // Queued respawns are pending. These must all spawn before a despawn can occur.
                // Leave the queued despawn in place to ensure it eventually gets processed.
                if (_queuedSpawnsByArea.ContainsKey(area) &&
                    _queuedSpawnsByArea[area].Count > 0)
                {
                    continue;
                }

                if (now > despawnTime)
                {
                    // Destroy active spawned objects from the module.
                    if (_activeSpawnsByArea.ContainsKey(area))
                    {
                        foreach (var activeSpawn in _activeSpawnsByArea[area])
                        {
                            ExecuteScript("spawn_despawn", activeSpawn.SpawnObject);
                            DestroyObject(activeSpawn.SpawnObject);
                        }
                    }

                    if (!_queuedSpawnsByArea.ContainsKey(area))
                        _queuedSpawnsByArea[area] = new List<QueuedSpawn>();

                    // Removing all spawn Ids from the master queue list.
                    var spawnIds = _queuedSpawnsByArea[area].Select(s => s.SpawnDetailId);
                    _queuedSpawns.RemoveAll(x => spawnIds.Contains(x.SpawnDetailId));

                    // Remove area-specific resource despawns as well
                    if (_activeSpawnsByArea.ContainsKey(area))
                    {
                        var areaSpawnObjects = _activeSpawnsByArea[area].Select(x => x.SpawnObject);
                        _queuedResourceDespawns.RemoveAll(x => areaSpawnObjects.Contains(x.ResourceObject));
                    }

                    // Remove area from the various cache collections.
                    _queuedSpawnsByArea.Remove(area);

                    if (_activeSpawnsByArea.ContainsKey(area))
                    {
                        _activeSpawnsByArea[area].Clear();
                    }

                    _queuedAreaDespawns.Remove(area);
                }
            }
        }

        /// <summary>
        /// On each module heartbeat, iterate over the list of resources which are scheduled to
        /// be despawned naturally. If enough time has passed, despawn the resource.
        /// </summary>
        private static void ProcessResourceDespawns()
        {
            var now = DateTime.UtcNow;
            for (var index = _queuedResourceDespawns.Count - 1; index >= 0; index--)
            {
                var resourceDespawn = _queuedResourceDespawns[index];
                
                // Resource object no longer exists, remove from queue
                if (!GetIsObjectValid(resourceDespawn.ResourceObject))
                {
                    _queuedResourceDespawns.RemoveAt(index);
                    continue;
                }

                if (now > resourceDespawn.DespawnTime)
                {
                    // Execute the despawn script for cleanup (props, etc.)
                    ExecuteScript("spawn_despawn", resourceDespawn.ResourceObject);

                    // Remove from active spawns
                    var spawnDetail = _spawns[resourceDespawn.SpawnDetailId];
                    if (_activeSpawnsByArea.ContainsKey(spawnDetail.Area))
                    {
                        _activeSpawnsByArea[spawnDetail.Area].RemoveAll(x => x.SpawnObject == resourceDespawn.ResourceObject);
                    }

                    // Destroy the resource directly (avoids triggering death events which would queue duplicate respawns)
                    DestroyObject(resourceDespawn.ResourceObject);

                    // Queue for respawn
                    var respawnTime = now.AddMinutes(spawnDetail.RespawnDelayMinutes);
                    CreateQueuedSpawn(resourceDespawn.SpawnDetailId, respawnTime);

                    _queuedResourceDespawns.RemoveAt(index);
                }
            }
        }

        private static void AdjustScripts(uint spawn)
        {
            if (GetIsPC(spawn) || GetIsDM(spawn) || GetIsDMPossessed(spawn))
                return;

            var type = GetObjectType(spawn);

            if (type == ObjectType.Creature)
            {
                var originalSpawnScript = GetEventScript(spawn, EventScript.Creature_OnSpawnIn);

                SetEventScript(spawn, EventScript.Creature_OnBlockedByDoor, "x2_def_onblocked");
                SetEventScript(spawn, EventScript.Creature_OnEndCombatRound, "x2_def_endcombat");
                //SetEventScript(creature, EventScript.Creature_OnDialogue, "x2_def_onconv");
                SetEventScript(spawn, EventScript.Creature_OnDamaged, "x2_def_ondamage");
                SetEventScript(spawn, EventScript.Creature_OnDeath, "x2_def_ondeath");
                SetEventScript(spawn, EventScript.Creature_OnDisturbed, "x2_def_ondisturb");
                SetEventScript(spawn, EventScript.Creature_OnHeartbeat, "x2_def_heartbeat");
                SetEventScript(spawn, EventScript.Creature_OnNotice, "x2_def_percept");
                SetEventScript(spawn, EventScript.Creature_OnMeleeAttacked, "x2_def_attacked");
                SetEventScript(spawn, EventScript.Creature_OnRested, "x2_def_rested");
                SetEventScript(spawn, EventScript.Creature_OnSpawnIn, "x2_def_spawn");
                SetEventScript(spawn, EventScript.Creature_OnSpellCastAt, "x2_def_spellcast");
                SetEventScript(spawn, EventScript.Creature_OnUserDefined, "x2_def_userdef");

                // The spawn script will not fire because it has already executed. In the event there wasn't a script
                // already on the creature, we need to run the normal spawn script to ensure it gets created appropriately.
                if (string.IsNullOrWhiteSpace(originalSpawnScript))
                {
                    ExecuteScript("x2_def_spawn", spawn);
                }
            }
            else if (type == ObjectType.Placeable)
            {
                if (string.IsNullOrWhiteSpace(GetEventScript(spawn, EventScript.Placeable_OnDeath)))
                {
                    SetEventScript(spawn, EventScript.Placeable_OnDeath, "plc_death");
                }
            }
        }

        /// <summary>
        /// Make plot/immortal NPCs incredibly strong to dissuade players from attacking them and messing with spawns.
        /// </summary>
        /// <param name="spawn"></param>
        private static void AdjustStats(uint spawn)
        {
            if (!GetIsObjectValid(spawn) || GetObjectType(spawn) != ObjectType.Creature)
                return;

            if (GetIsPC(spawn) || GetIsDM(spawn) || GetIsDMPossessed(spawn))
                return;

            if (!GetPlotFlag(spawn) && !GetImmortal(spawn))
                return;

            CreaturePlugin.SetBaseAC(spawn, 100);
            CreaturePlugin.SetRawAbilityScore(spawn, AbilityType.Might, 100);
            CreaturePlugin.SetRawAbilityScore(spawn, AbilityType.Perception, 100);
            CreaturePlugin.SetRawAbilityScore(spawn, AbilityType.Vitality, 100);
            CreaturePlugin.SetRawAbilityScore(spawn, AbilityType.Agility, 100);
            CreaturePlugin.SetRawAbilityScore(spawn, AbilityType.Willpower, 100);
            CreaturePlugin.SetRawAbilityScore(spawn, AbilityType.Social, 100);
            CreaturePlugin.SetBaseAttackBonus(spawn, 254);
            CreaturePlugin.AddFeatByLevel(spawn, FeatType.WeaponProficiencyCreature, 1);

            AssignCommand(spawn, () => ClearAllActions());

            if (!GetIsObjectValid(GetItemInSlot(InventorySlot.CreatureRight, spawn)))
            {
                var claw = CreateItemOnObject("npc_claw", spawn);
                AssignCommand(spawn, () =>
                {
                    ActionEquipItem(claw, InventorySlot.CreatureRight);
                });
            }
            if (!GetIsObjectValid(GetItemInSlot(InventorySlot.CreatureLeft, spawn)))
            {
                var claw = CreateItemOnObject("npc_claw", spawn);
                AssignCommand(spawn, () =>
                {
                    ActionEquipItem(claw, InventorySlot.CreatureLeft);
                });
            }
        }

        /// <summary>
        /// When a DM spawns a creature, attach all required scripts to it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMSpawnObjectAfter)]
        public static void DMSpawnCreature()
        {
            var objectType = (InternalObjectType)Convert.ToInt32(EventsPlugin.GetEventData("OBJECT_TYPE"));

            if (objectType != InternalObjectType.Creature)
                return;

            var objectData = EventsPlugin.GetEventData("OBJECT");
            var spawn = Convert.ToUInt32(objectData, 16); // Not sure why this is in hex.
            AdjustScripts(spawn);
            AdjustStats(spawn);
        }

        /// <summary>
        /// Creates a new spawn object into its spawn area.
        /// Hand-placed objects are deserialized and added to the area.
        /// Spawn tables run their own logic to determine which object to spawn.
        /// </summary>
        /// <param name="spawnId">The ID of the spawn</param>
        /// <param name="detail">The details of the spawn</param>
        private static uint SpawnObject(Guid spawnId, SpawnDetail detail)
        {
            // Hand-placed spawns are stored as a serialized string.
            // Deserialize and add it to the area.
            if (!string.IsNullOrWhiteSpace(detail.SerializedObject))
            {
                var deserialized = ObjectPlugin.Deserialize(detail.SerializedObject);
                var position = detail.UseRandomSpawnLocation ?
                    GetPositionFromLocation(Walkmesh.GetRandomLocation(detail.Area)) :
                    new Vector3(detail.X, detail.Y, detail.Z);
                ObjectPlugin.AddToArea(deserialized, detail.Area, position);

                var facing = detail.UseRandomSpawnLocation ? Random.Next(360) : detail.Facing;
                AssignCommand(deserialized, () => SetFacing(facing));
                SetLocalString(deserialized, "SPAWN_ID", spawnId.ToString());
                AI.SetAIFlag(deserialized, AIFlag.ReturnHome);
                AdjustScripts(deserialized);
                AdjustStats(deserialized);

                return deserialized;
            }
            // Spawn tables have their own logic which must be run to determine the spawn to use.
            // Create the object at the stored location.
            else if (!string.IsNullOrWhiteSpace(detail.SpawnTableId))
            {
                var spawnTable = _spawnTables[detail.SpawnTableId];
                var spawnObject = spawnTable.GetNextSpawn();

                // It's possible that the rules of the spawn table don't have a spawn ready to be created.
                // In this case, exit early.
                if (string.IsNullOrWhiteSpace(spawnObject.Resref))
                {
                    return OBJECT_INVALID;
                }

                var position = detail.UseRandomSpawnLocation ?
                    GetPositionFromLocation(Walkmesh.GetRandomLocation(detail.Area)) :
                    new Vector3(detail.X, detail.Y, detail.Z);

                var facing = detail.UseRandomSpawnLocation ? Random.Next(360) : detail.Facing;
                var location = Location(detail.Area, position, facing);

                var spawn = CreateObject(spawnObject.Type, spawnObject.Resref, location);
                SetLocalString(spawn, "SPAWN_ID", spawnId.ToString());

                AI.SetAIFlag(spawn, spawnObject.AIFlags);
                AdjustScripts(spawn);
                AdjustStats(spawn);

                foreach (var animator in spawnObject.Animators)
                {
                    animator.SetLocalVariables(spawn);
                }

                foreach (var action in spawnObject.OnSpawnActions)
                {
                    action(spawn);
                }

                // If this is a placeable (resource) from a spawn table, queue it for natural despawning
                if (spawnObject.Type == ObjectType.Placeable && spawnTable.ResourceDespawnMinutes > 0)
                {
                    QueueResourceDespawn(spawn, spawnId, spawnTable.ResourceDespawnMinutes);
                }

                return spawn;
            }

            return OBJECT_INVALID;
        }
    }
}
