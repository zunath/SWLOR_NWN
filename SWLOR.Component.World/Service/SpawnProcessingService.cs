using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Models;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Events.Events.World;

namespace SWLOR.Component.World.Service
{
    /// <summary>
    /// Service responsible for processing spawn queues and despawning.
    /// </summary>
    public class SpawnProcessingService : ISpawnProcessingService
    {
        private readonly ILogger _logger;
        private readonly IRandomService _random;
        private readonly ISpawnCreationService _spawnCreation;
        private readonly ISpawnCacheService _spawnCache;
        private readonly IEventAggregator _eventAggregator;
        private readonly IAreaPluginService _areaPlugin;

        public int DespawnMinutes => 20;
        public int ResourceDespawnMinutes => 180;

        private readonly Dictionary<Guid, SpawnDetail> _spawns = new();
        private readonly List<QueuedSpawn> _queuedSpawns = new();
        private readonly Dictionary<uint, List<QueuedSpawn>> _queuedSpawnsByArea = new();
        private readonly Dictionary<uint, DateTime> _queuedAreaDespawns = new();
        private readonly List<ResourceDespawn> _queuedResourceDespawns = new();
        private readonly Dictionary<uint, List<ActiveSpawn>> _activeSpawnsByArea = new();

        public SpawnProcessingService(
            ILogger logger,
            IRandomService random,
            ISpawnCreationService spawnCreation,
            ISpawnCacheService spawnCache,
            IEventAggregator eventAggregator,
            IAreaPluginService areaPlugin)
        {
            _logger = logger;
            _random = random;
            _spawnCreation = spawnCreation;
            _spawnCache = spawnCache;
            _eventAggregator = eventAggregator;
            _areaPlugin = areaPlugin;
        }

        public void ProcessSpawnSystem()
        {
            ProcessQueuedSpawns();
            ProcessDespawnAreas();
            ProcessResourceDespawns();
        }

        public void ProcessQueuedSpawns()
        {
            var now = DateTime.UtcNow;
            for (var index = _queuedSpawns.Count - 1; index >= 0; index--)
            {
                var queuedSpawn = _queuedSpawns.ElementAt(index);

                if (now > queuedSpawn.RespawnTime)
                {
                    var detail = _spawns[queuedSpawn.SpawnDetailId];
                    var spawnedObject = _spawnCreation.CreateSpawnObject(
                        queuedSpawn.SpawnDetailId.ToString(),
                        detail.SerializedObject,
                        detail.SpawnTableId,
                        detail.Area,
                        new System.Numerics.Vector3(detail.X, detail.Y, detail.Z),
                        detail.Facing,
                        detail.UseRandomSpawnLocation);

                    // A valid spawn wasn't found because the spawn table didn't provide a resref.
                    // Either the table is configured wrong or the requirements for that specific table weren't met.
                    if (spawnedObject == OBJECT_INVALID)
                    {
                        queuedSpawn.FailureCount++;
                        
                        // If we've failed too many times (10 attempts), remove this spawn to prevent infinite loops
                        if (queuedSpawn.FailureCount >= 10)
                        {
                            _logger.Write<ErrorLogGroup>($"Spawn {queuedSpawn.SpawnDetailId} failed 10 times consecutively. Removing from queue to prevent infinite spawning. Check spawn table configuration.");
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

                    AddActiveSpawn(detail.Area, queuedSpawn.SpawnDetailId, spawnedObject);
                    RemoveQueuedSpawn(queuedSpawn);
                }
            }
        }

        public void QueueResourceDespawn(uint resourceObject, Guid spawnDetailId, int despawnMinutes)
        {
            var now = DateTime.UtcNow;
            
            // Add random variance of ±25% to stagger despawn times
            var variancePercent = _random.Next(-25, 26); // -25% to +25%
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

        public void CreateQueuedSpawn(Guid spawnDetailId, DateTime respawnTime)
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

        public void RemoveQueuedSpawn(object queuedSpawn)
        {
            if (queuedSpawn is QueuedSpawn qs)
            {
                var spawnDetail = _spawns[qs.SpawnDetailId];
                _queuedSpawns.Remove(qs);
                _queuedSpawnsByArea[spawnDetail.Area].Remove(qs);
            }
        }

        public List<object> GetQueuedSpawnsForArea(uint area)
        {
            return _queuedSpawnsByArea.ContainsKey(area) 
                ? _queuedSpawnsByArea[area].Cast<object>().ToList() 
                : new List<object>();
        }

        public List<object> GetActiveSpawnsForArea(uint area)
        {
            return _activeSpawnsByArea.ContainsKey(area) 
                ? _activeSpawnsByArea[area].Cast<object>().ToList() 
                : new List<object>();
        }

        public void AddActiveSpawn(uint area, Guid spawnDetailId, uint spawnObject)
        {
            if (!_activeSpawnsByArea.ContainsKey(area))
                _activeSpawnsByArea[area] = new List<ActiveSpawn>();

            var activeSpawn = new ActiveSpawn
            {
                SpawnDetailId = spawnDetailId,
                SpawnObject = spawnObject
            };

            _activeSpawnsByArea[area].Add(activeSpawn);
        }

        public void RemoveActiveSpawn(uint area, uint spawnObject)
        {
            if (_activeSpawnsByArea.ContainsKey(area))
            {
                _activeSpawnsByArea[area].RemoveAll(x => x.SpawnObject == spawnObject);
            }
        }

        public void ClearActiveSpawnsForArea(uint area)
        {
            if (_activeSpawnsByArea.ContainsKey(area))
            {
                _activeSpawnsByArea[area].Clear();
            }
        }

        private void ProcessDespawnAreas()
        {
            var now = DateTime.UtcNow;
            for (var index = _queuedAreaDespawns.Count - 1; index >= 0; index--)
            {
                var (area, despawnTime) = _queuedAreaDespawns.ElementAt(index);
                // Players have entered this area. Remove it and move to the next entry.
                if (_areaPlugin.GetNumberOfPlayersInArea(area) > 0)
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
                            _eventAggregator.Publish(new OnSpawnDespawn(), activeSpawn.SpawnObject);
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
                    ClearActiveSpawnsForArea(area);
                    _queuedAreaDespawns.Remove(area);
                }
            }
        }

        private void ProcessResourceDespawns()
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
                    // Execute the despawn script and remove the resource
                    _eventAggregator.Publish(new OnSpawnDespawn(), resourceDespawn.ResourceObject);
                    SetPlotFlag(resourceDespawn.ResourceObject, false);
                    ApplyEffectToObject(DurationType.Instant, EffectDeath(), resourceDespawn.ResourceObject);

                    // Remove from active spawns and queue a respawn
                    var spawnDetail = _spawns[resourceDespawn.SpawnDetailId];
                    RemoveActiveSpawn(spawnDetail.Area, resourceDespawn.ResourceObject);

                    // Queue for respawn
                    var respawnTime = now.AddMinutes(spawnDetail.RespawnDelayMinutes);
                    CreateQueuedSpawn(resourceDespawn.SpawnDetailId, respawnTime);

                    _queuedResourceDespawns.RemoveAt(index);
                }
            }
        }

        public void QueueAreaDespawn(uint area)
        {
            var now = DateTime.UtcNow;
            _queuedAreaDespawns[area] = now.AddMinutes(DespawnMinutes);
        }

        public void AddSpawnDetail(Guid id, SpawnDetail detail)
        {
            _spawns[id] = detail;
        }

        public SpawnDetail GetSpawnDetail(Guid id)
        {
            return _spawns.TryGetValue(id, out var detail) ? detail : null;
        }

        public void AddSpawnToArea(uint area, Guid spawnId)
        {
            if (!_queuedSpawnsByArea.ContainsKey(area))
                _queuedSpawnsByArea[area] = new List<QueuedSpawn>();
        }
    }
}
