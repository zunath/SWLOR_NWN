using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The spawn system's runtime: spawning and despawning the areas, the queued respawns, placing a spawn and the DM spawns
    /// that used to be static in <c>Service.Spawn</c>, moved here unchanged. The spawn tables and the registry of spawns are
    /// <see cref="ISpawnRegistryService"/>. The event handlers run on the container's instance; static callers reach it
    /// through <c>GameServices.Spawn</c>.
    /// </summary>
    public class SpawnService : ISpawnService
    {
        private readonly ISpawnRegistryService _registry;
        private readonly ISpawnObjectService _objects;

        public SpawnService(ISpawnRegistryService registry, ISpawnObjectService objects)
        {
            _registry = registry;
            _objects = objects;
        }

        public const int DespawnMinutes = 20;
        public const int DefaultRespawnMinutes = 5;
        public const int LeftoverResourceDespawnMinutes = 5;

        /// <summary>
        /// The current UTC time. <c>protected virtual</c> so a unit test can place a respawn, a despawn or a
        /// heartbeat at a time of its own; the body is the original <c>DateTime.UtcNow</c>.
        /// </summary>
        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        /// <summary>
        /// Writes a configuration error to the error log. <c>protected virtual</c> so a unit test can read what the
        /// spawn tables and areas reported; the body is the original <c>Log.Write</c> call.
        /// </summary>
        protected virtual void WriteError(string message)
        {
            Log.Write(LogGroup.Error, message);
        }

        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public void SpawnArea()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) && !GetIsDM(player)) return;

            var area = OBJECT_SELF;

            // Area isn't registered. Could be an instanced area? No need to spawn.
            if (!_registry.AllSpawnsByArea.ContainsKey(area)) return;

            if (!_registry.ActiveSpawnsByArea.ContainsKey(area))
                _registry.ActiveSpawnsByArea[area] = new List<ActiveSpawn>();

            if (!_registry.QueuedSpawnsByArea.ContainsKey(area))
                _registry.QueuedSpawnsByArea[area] = new List<QueuedSpawn>();

            var activeSpawns = _registry.ActiveSpawnsByArea[area];
            var queuedSpawns = _registry.QueuedSpawnsByArea[area];

            // Spawns are currently active for this area. No need to spawn.
            if (activeSpawns.Count > 0 || queuedSpawns.Count > 0) return;

            var now = UtcNow();
            // No spawns are active. Spawn the area.
            foreach (var spawn in _registry.AllSpawnsByArea[area])
            {
                CreateQueuedSpawn(spawn, now);
            }
        }

        /// <summary>
        /// When the last player in an area leaves, a despawn request is queued up.
        /// The heartbeat processor will despawn all objects when this happens
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public void QueueDespawnArea()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) && !GetIsDM(player)) return;

            var area = OBJECT_SELF;
            var playerCount = AreaPlugin.GetNumberOfPlayersInArea(area);
            if (playerCount > 0) return;

            var now = UtcNow();
            _registry.QueuedAreaDespawns[area] = now.AddMinutes(DespawnMinutes);
        }

        /// <summary>
        /// Moves a spawned resource's natural despawn forward so it despawns and frees its spawn slot
        /// no later than the given delay from now. Used for searched resources which still hold items.
        /// An earlier scheduled despawn is kept. Resources not created by the spawn system are left alone.
        /// </summary>
        /// <param name="resourceObject">The resource object to despawn</param>
        /// <param name="delay">The maximum amount of time before the resource despawns</param>
        /// <returns>true if a despawn is scheduled, false otherwise</returns>
        public bool ScheduleResourceDespawn(uint resourceObject, TimeSpan delay)
        {
            if (!GetIsObjectValid(resourceObject))
                return false;

            if (GetLocalInt(resourceObject, "RESPAWN_QUEUED") == 1)
                return false;

            var spawnId = GetLocalString(resourceObject, "SPAWN_ID");
            if (!Guid.TryParse(spawnId, out var spawnGuid) || !_registry.Spawns.ContainsKey(spawnGuid))
                return false;

            var despawnTime = UtcNow().Add(delay);
            var existing = _registry.QueuedResourceDespawns.FirstOrDefault(x => x.ResourceObject == resourceObject);
            if (existing != null)
            {
                if (despawnTime < existing.DespawnTime)
                    existing.DespawnTime = despawnTime;
            }
            else
            {
                _registry.QueuedResourceDespawns.Add(new ResourceDespawn
                {
                    ResourceObject = resourceObject,
                    DespawnTime = despawnTime,
                    SpawnDetailId = spawnGuid
                });
            }

            return true;
        }

        /// <summary>
        /// Creates a queued spawn record which is picked up by the processor.
        /// The spawn object will be created when the respawn time has passed.
        /// </summary>
        /// <param name="spawnDetailId">The ID of the spawn detail.</param>
        /// <param name="respawnTime">The time the spawn will be created.</param>
        private void CreateQueuedSpawn(Guid spawnDetailId, DateTime respawnTime)
        {
            var queuedSpawn = new QueuedSpawn
            {
                RespawnTime = respawnTime,
                SpawnDetailId = spawnDetailId
            };
            _registry.QueuedSpawns.Add(queuedSpawn);

            var spawnDetail = _registry.Spawns[spawnDetailId];
            if (!_registry.QueuedSpawnsByArea.ContainsKey(spawnDetail.Area))
                _registry.QueuedSpawnsByArea[spawnDetail.Area] = new List<QueuedSpawn>();

            _registry.QueuedSpawnsByArea[spawnDetail.Area].Add(queuedSpawn);
        }

        /// <summary>
        /// Removes a queued spawn.
        /// </summary>
        /// <param name="queuedSpawn">The queued spawn to remove.</param>
        private void RemoveQueuedSpawn(QueuedSpawn queuedSpawn)
        {
            var spawnDetail = _registry.Spawns[queuedSpawn.SpawnDetailId];
            _registry.QueuedSpawns.Remove(queuedSpawn);
            _registry.QueuedSpawnsByArea[spawnDetail.Area].Remove(queuedSpawn);
        }

        /// <summary>
        /// When a creature dies, its details need to be queued up for a respawn.
        /// NOTE: plc_death and crea_death_aft will not trigger if the object is 'killed'
        /// via DestroyObject.  Call this method directly if you need to use DestroyObject
        /// on a respawning object.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        [NWNEventHandler(ScriptName.OnPlaceableDeath)]
        public void QueueRespawn()
        {
            uint creature = OBJECT_SELF;
            var spawnId = GetLocalString(creature, "SPAWN_ID");
            if (string.IsNullOrWhiteSpace(spawnId)) return;
            if (GetLocalInt(creature, "RESPAWN_QUEUED") == 1) return;

            var spawnGuid = new Guid(spawnId);
            var detail = _registry.Spawns[spawnGuid];
            var respawnTime = GetRespawnTime(detail);

            CreateQueuedSpawn(spawnGuid, respawnTime);
            RemoveActiveSpawn(detail, creature);
            SetLocalInt(creature, "RESPAWN_QUEUED", 1);
        }

        /// <summary>
        /// Removes an active spawned object and queues its replacement using its spawn
        /// table's respawn window. Used by interactable resources which are consumed
        /// without firing a placeable death event.
        /// </summary>
        public bool DespawnAndQueueRespawn(uint spawnObject)
        {
            if (!GetIsObjectValid(spawnObject))
                return false;

            var spawnId = GetLocalString(spawnObject, "SPAWN_ID");
            if (!Guid.TryParse(spawnId, out var spawnGuid) || !_registry.Spawns.TryGetValue(spawnGuid, out var detail))
            {
                DestroySpawnObject(spawnObject);
                return false;
            }

            if (GetLocalInt(spawnObject, "RESPAWN_QUEUED") == 1)
                return false;

            CreateQueuedSpawn(spawnGuid, GetRespawnTime(detail));
            RemoveActiveSpawn(detail, spawnObject);
            SetLocalInt(spawnObject, "RESPAWN_QUEUED", 1);
            DestroySpawnObject(spawnObject);
            return true;
        }

        /// <summary>
        /// Destroys a spawned object without leaving anything behind. The engine drops the
        /// inventory of a destroyed placeable into a "Remains" bag, so a resource node that
        /// still holds items has its items destroyed first.
        /// </summary>
        /// <param name="spawnObject">The spawned object to destroy.</param>
        private void DestroySpawnObject(uint spawnObject)
        {
            if (GetObjectType(spawnObject) == ObjectType.Placeable && GetHasInventory(spawnObject))
            {
                for (var item = GetFirstItemInInventory(spawnObject); GetIsObjectValid(item); item = GetNextItemInInventory(spawnObject))
                {
                    DestroyObject(item);
                }
            }

            DestroyObject(spawnObject);
        }

        private DateTime GetRespawnTime(SpawnDetail detail)
        {
            var maximum = Math.Max(detail.RespawnDelayMinutes, detail.RespawnDelayMaximumMinutes);
            var delay = maximum == detail.RespawnDelayMinutes
                ? detail.RespawnDelayMinutes
                : Random.Next(detail.RespawnDelayMinutes, maximum + 1);
            return UtcNow().AddMinutes(delay);
        }

        /// <summary>
        /// On each module heartbeat, process queued spawns and
        /// process dequeue area event requests.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public void ProcessSpawnSystem()
        {
            ProcessQueuedSpawns();
            ProcessDespawnAreas();
            ProcessResourceDespawns();
        }

        /// <summary>
        /// On each module heartbeat, iterate over the list of queued spawns.
        /// If enough time has elapsed and spawn table rules are met, spawn the object and remove it from the queue.
        /// </summary>
        public void ProcessQueuedSpawns()
        {
            var now = UtcNow();
            for (var index = _registry.QueuedSpawns.Count - 1; index >= 0; index--)
            {
                var queuedSpawn = _registry.QueuedSpawns.ElementAt(index);

                if (now > queuedSpawn.RespawnTime)
                {
                    var detail = _registry.Spawns[queuedSpawn.SpawnDetailId];
                    var spawnResult = _objects.SpawnObject(queuedSpawn.SpawnDetailId, detail);

                    // A valid spawn wasn't found because the spawn table didn't provide a resref.
                    // Either the table is configured wrong or the requirements for that specific table weren't met.
                    if (spawnResult.SpawnObject == OBJECT_INVALID)
                    {
                        queuedSpawn.FailureCount++;

                        // If we've failed too many times (10 attempts), remove this spawn to prevent infinite loops
                        if (queuedSpawn.FailureCount >= 10)
                        {
                            WriteError($"Spawn {queuedSpawn.SpawnDetailId} failed 10 times consecutively. Removing from queue to prevent infinite spawning. Check spawn table configuration.");
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
                        SpawnObject = spawnResult.SpawnObject,
                        IsRare = spawnResult.IsRare
                    };

                    _registry.ActiveSpawnsByArea[detail.Area].Add(activeSpawn);
                    RemoveQueuedSpawn(queuedSpawn);
                }
            }
        }

        /// <summary>
        /// On each module heartbeat, iterate over the list of areas which are scheduled to
        /// be despawned. If players have since entered the area, remove it from the queue list.
        /// </summary>
        private void ProcessDespawnAreas()
        {
            var now = UtcNow();
            for (var index = _registry.QueuedAreaDespawns.Count - 1; index >= 0; index--)
            {
                var (area, despawnTime) = _registry.QueuedAreaDespawns.ElementAt(index);
                // Players have entered this area. Remove it and move to the next entry.
                if (AreaPlugin.GetNumberOfPlayersInArea(area) > 0)
                {
                    _registry.QueuedAreaDespawns.Remove(area);
                    continue;
                }

                // Queued respawns are pending. These must all spawn before a despawn can occur.
                // Leave the queued despawn in place to ensure it eventually gets processed.
                if (_registry.QueuedSpawnsByArea.ContainsKey(area) &&
                    _registry.QueuedSpawnsByArea[area].Count > 0)
                {
                    continue;
                }

                if (now > despawnTime)
                {
                    // Destroy active spawned objects from the module.
                    if (_registry.ActiveSpawnsByArea.ContainsKey(area))
                    {
                        foreach (var activeSpawn in _registry.ActiveSpawnsByArea[area])
                        {
                            ExecuteScript("spawn_despawn", activeSpawn.SpawnObject);
                            DestroySpawnObject(activeSpawn.SpawnObject);
                        }
                    }

                    if (!_registry.QueuedSpawnsByArea.ContainsKey(area))
                        _registry.QueuedSpawnsByArea[area] = new List<QueuedSpawn>();

                    // Removing all spawn Ids from the master queue list.
                    var spawnIds = _registry.QueuedSpawnsByArea[area].Select(s => s.SpawnDetailId);
                    _registry.QueuedSpawns.RemoveAll(x => spawnIds.Contains(x.SpawnDetailId));

                    // Remove area-specific resource despawns as well
                    if (_registry.ActiveSpawnsByArea.ContainsKey(area))
                    {
                        var areaSpawnObjects = _registry.ActiveSpawnsByArea[area].Select(x => x.SpawnObject);
                        _registry.QueuedResourceDespawns.RemoveAll(x => areaSpawnObjects.Contains(x.ResourceObject));
                    }

                    // Remove area from the various cache collections.
                    _registry.QueuedSpawnsByArea.Remove(area);

                    if (_registry.ActiveSpawnsByArea.ContainsKey(area))
                    {
                        _registry.ActiveSpawnsByArea[area].Clear();
                    }

                    _registry.QueuedAreaDespawns.Remove(area);
                }
            }
        }

        /// <summary>
        /// On each module heartbeat, iterate over the list of resources which are scheduled to
        /// be despawned naturally. If enough time has passed, despawn the resource.
        /// </summary>
        private void ProcessResourceDespawns()
        {
            ProcessResourceDespawns(UtcNow());
        }

        private void ProcessResourceDespawns(DateTime now)
        {
            for (var index = _registry.QueuedResourceDespawns.Count - 1; index >= 0; index--)
            {
                var resourceDespawn = _registry.QueuedResourceDespawns[index];

                // Resource object no longer exists, remove from queue
                if (!GetIsObjectValid(resourceDespawn.ResourceObject))
                {
                    _registry.QueuedResourceDespawns.RemoveAt(index);
                    continue;
                }

                // The resource already queued its respawn (for example, it was emptied and died).
                if (GetLocalInt(resourceDespawn.ResourceObject, "RESPAWN_QUEUED") == 1)
                {
                    _registry.QueuedResourceDespawns.RemoveAt(index);
                    continue;
                }

                if (now > resourceDespawn.DespawnTime)
                {
                    // Execute the despawn script for cleanup (props, etc.)
                    ExecuteScript("spawn_despawn", resourceDespawn.ResourceObject);

                    // Remove from active spawns
                    var spawnDetail = _registry.Spawns[resourceDespawn.SpawnDetailId];
                    if (_registry.ActiveSpawnsByArea.ContainsKey(spawnDetail.Area))
                    {
                        _registry.ActiveSpawnsByArea[spawnDetail.Area].RemoveAll(x => x.SpawnObject == resourceDespawn.ResourceObject);
                    }

                    // Destroy the resource directly (avoids triggering death events which would queue duplicate respawns)
                    SetLocalInt(resourceDespawn.ResourceObject, "RESPAWN_QUEUED", 1);
                    DestroySpawnObject(resourceDespawn.ResourceObject);

                    // Queue for respawn
                    var respawnTime = GetRespawnTime(spawnDetail);
                    CreateQueuedSpawn(resourceDespawn.SpawnDetailId, respawnTime);

                    _registry.QueuedResourceDespawns.RemoveAt(index);
                }
            }
        }

        private void RemoveActiveSpawn(SpawnDetail detail, uint spawnObject)
        {
            if (!_registry.ActiveSpawnsByArea.TryGetValue(detail.Area, out var activeSpawns))
                return;

            activeSpawns.RemoveAll(x => x.SpawnObject == spawnObject);
        }

        /// <summary>
        /// Registers an existing placeable as an active resource spawn from the given spawn table,
        /// with that table's natural despawn queued. Engine tests only.
        /// </summary>
        public Guid RegisterResourceSpawnForTesting(uint resourceObject, string spawnTableId)
        {
            var spawnTable = _registry.SpawnTables[spawnTableId];
            var area = GetArea(resourceObject);
            var position = GetPosition(resourceObject);
            var id = Guid.NewGuid();
            _registry.Spawns.Add(id, new SpawnDetail
            {
                SpawnTableId = spawnTableId,
                Area = area,
                X = position.X,
                Y = position.Y,
                Z = position.Z,
                Facing = GetFacing(resourceObject),
                RespawnDelayMinutes = spawnTable.RespawnDelayMinutes,
                RespawnDelayMaximumMinutes = spawnTable.RespawnDelayMaximumMinutes
            });

            if (!_registry.ActiveSpawnsByArea.ContainsKey(area))
                _registry.ActiveSpawnsByArea[area] = new List<ActiveSpawn>();

            _registry.ActiveSpawnsByArea[area].Add(new ActiveSpawn { SpawnDetailId = id, SpawnObject = resourceObject });
            SetLocalString(resourceObject, "SPAWN_ID", id.ToString());
            _objects.QueueResourceDespawn(resourceObject, id, spawnTable.ResourceDespawnMinutes);

            return id;
        }

        public bool IsSpawnActiveForTesting(Guid spawnId)
        {
            return _registry.Spawns.TryGetValue(spawnId, out var detail) &&
                   _registry.ActiveSpawnsByArea.TryGetValue(detail.Area, out var activeSpawns) &&
                   activeSpawns.Any(x => x.SpawnDetailId == spawnId);
        }

        public int GetQueuedSpawnCountForTesting(Guid spawnId)
        {
            return _registry.QueuedSpawns.Count(x => x.SpawnDetailId == spawnId);
        }

        public DateTime? GetResourceDespawnTimeForTesting(uint resourceObject)
        {
            return _registry.QueuedResourceDespawns.FirstOrDefault(x => x.ResourceObject == resourceObject)?.DespawnTime;
        }

        public void ProcessResourceDespawnsForTesting(DateTime now)
        {
            ProcessResourceDespawns(now);
        }

        /// <summary>
        /// Removes every trace of a spawn registered by <see cref="RegisterResourceSpawnForTesting"/>.
        /// </summary>
        public void RemoveSpawnForTesting(Guid spawnId)
        {
            if (!_registry.Spawns.TryGetValue(spawnId, out var detail))
                return;

            foreach (var queuedSpawn in _registry.QueuedSpawns.Where(x => x.SpawnDetailId == spawnId).ToList())
            {
                RemoveQueuedSpawn(queuedSpawn);
            }

            if (_registry.ActiveSpawnsByArea.TryGetValue(detail.Area, out var activeSpawns))
            {
                var spawnObjects = activeSpawns.Where(x => x.SpawnDetailId == spawnId).Select(x => x.SpawnObject).ToList();
                activeSpawns.RemoveAll(x => x.SpawnDetailId == spawnId);
                _registry.QueuedResourceDespawns.RemoveAll(x => spawnObjects.Contains(x.ResourceObject));
            }

            _registry.QueuedResourceDespawns.RemoveAll(x => x.SpawnDetailId == spawnId);
            _registry.Spawns.Remove(spawnId);
        }
    }
}
