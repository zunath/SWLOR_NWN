using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Models;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Component.World.Service
{
    /// <summary>
    /// Service responsible for managing spawn data and area-specific operations.
    /// </summary>
    public class SpawnManagementService : ISpawnManagementService
    {
        private readonly ILogger _logger;
        private readonly ISpawnCalculationService _spawnCalculation;
        private readonly ISpawnCacheService _spawnCache;
        private readonly ISpawnProcessingService _spawnProcessing;

        private readonly Dictionary<Guid, SpawnDetail> _spawns = new();
        private readonly Dictionary<uint, List<Guid>> _allSpawnsByArea = new();

        public SpawnManagementService(
            ILogger logger,
            ISpawnCalculationService spawnCalculation,
            ISpawnCacheService spawnCache,
            ISpawnProcessingService spawnProcessing)
        {
            _logger = logger;
            _spawnCalculation = spawnCalculation;
            _spawnCache = spawnCache;
            _spawnProcessing = spawnProcessing;
        }

        public void StoreSpawns()
        {
            void RegisterAreaSpawnTable(uint area, string variableName, int spawnCount)
            {
                var spawnTableId = GetLocalString(area, variableName);
                if (!string.IsNullOrWhiteSpace(spawnTableId))
                {
                    if (!_spawnCache.HasSpawnTable(spawnTableId))
                    {
                        _logger.Write<ErrorLogGroup>($"Area has an invalid spawn table Id. ({spawnTableId}) is not defined. Do you have the right spawn table Id?");
                        return;
                    }
                    
                    var spawnTable = _spawnCache.GetSpawnTable(spawnTableId);
                    if (spawnTable.Spawns == null || spawnTable.Spawns.Count == 0)
                    {
                        _logger.Write<ErrorLogGroup>($"Spawn table {spawnTableId} has no spawn objects defined. Skipping area spawn setup.");
                        return;
                    }

                    for (var count = 1; count <= spawnCount; count++)
                    {
                        var id = Guid.NewGuid();
                        var detail = new SpawnDetail
                        {
                            SpawnTableId = spawnTableId,
                            Area = area,
                            RespawnDelayMinutes = spawnTable.RespawnDelayMinutes,
                            UseRandomSpawnLocation = true
                        };
                        _spawns.Add(id, detail);
                        _spawnProcessing.AddSpawnDetail(id, detail);

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

                        var detail = new SpawnDetail
                        {
                            SerializedObject = ObjectPlugin.Serialize(obj),
                            X = position.X,
                            Y = position.Y,
                            Z = position.Z,
                            Facing = facing,
                            Area = area,
                            RespawnDelayMinutes = 5
                        };
                        _spawns.Add(id, detail);
                        _spawnProcessing.AddSpawnDetail(id, detail);

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
                        if (_spawnCache.HasSpawnTable(spawnTableId))
                        {
                            var spawnTable = _spawnCache.GetSpawnTable(spawnTableId);
                            var detail = new SpawnDetail
                            {
                                SpawnTableId = spawnTableId,
                                X = position.X,
                                Y = position.Y,
                                Z = position.Z,
                                Facing = facing,
                                Area = area,
                                RespawnDelayMinutes = spawnTable.RespawnDelayMinutes
                            };
                            _spawns.Add(id, detail);
                            _spawnProcessing.AddSpawnDetail(id, detail);

                            // Add this entry to the spawns by area cache.
                            if (!_allSpawnsByArea.ContainsKey(area))
                                _allSpawnsByArea[area] = new List<Guid>();

                            _allSpawnsByArea[area].Add(id);
                        }
                    }
                }

                // Resource and creature spawn tables can be placed as a local variable on the area.
                // If one is found, it will be registered.
                RegisterAreaSpawnTable(area, "RESOURCE_SPAWN_TABLE_ID", _spawnCalculation.CalculateResourceSpawnCount(area));
                RegisterAreaSpawnTable(area, "CREATURE_SPAWN_TABLE_ID", _spawnCalculation.CalculateCreatureSpawnCount(area));
            }
        }

        public void SpawnArea()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) && !GetIsDM(player)) return;

            var area = OBJECT_SELF;

            // Area isn't registered. Could be an instanced area? No need to spawn.
            if (!AreaHasSpawns(area)) return;

            // Spawns are currently active for this area. No need to spawn.
            if (AreaHasActiveOrQueuedSpawns(area)) return;

            var now = DateTime.UtcNow;
            // No spawns are active. Spawn the area.
            foreach (var spawn in GetAllSpawnsForArea(area))
            {
                _spawnProcessing.CreateQueuedSpawn(spawn, now);
            }
        }

        public void QueueDespawnArea()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) && !GetIsDM(player)) return;

            var area = OBJECT_SELF;
            var playerCount = AreaPlugin.GetNumberOfPlayersInArea(area);
            if (playerCount > 0) return;

            _spawnProcessing.QueueAreaDespawn(area);
        }

        public void QueueRespawn()
        {
            uint creature = OBJECT_SELF;
            var spawnId = GetLocalString(creature, "SPAWN_ID");
            if (string.IsNullOrWhiteSpace(spawnId)) return;
            if (GetLocalInt(creature, "RESPAWN_QUEUED") == 1) return;

            var spawnGuid = new Guid(spawnId);
            var detail = GetSpawnDetail(spawnGuid);
            if (detail == null) return;

            var respawnTime = DateTime.UtcNow.AddMinutes(detail.RespawnDelayMinutes);
            _spawnProcessing.CreateQueuedSpawn(spawnGuid, respawnTime);
            SetLocalInt(creature, "RESPAWN_QUEUED", 1);
        }

        public List<Guid> GetAllSpawnsForArea(uint area)
        {
            return _allSpawnsByArea.ContainsKey(area) ? _allSpawnsByArea[area] : new List<Guid>();
        }

        public void AddSpawnDetail(Guid id, SpawnDetail detail)
        {
            _spawns[id] = detail;
        }

        public SpawnDetail GetSpawnDetail(Guid id)
        {
            return _spawns.TryGetValue(id, out var detail) ? detail : null;
        }

        public bool AreaHasSpawns(uint area)
        {
            return _allSpawnsByArea.ContainsKey(area);
        }

        public bool AreaHasActiveOrQueuedSpawns(uint area)
        {
            var activeSpawns = _spawnProcessing.GetActiveSpawnsForArea(area);
            var queuedSpawns = _spawnProcessing.GetQueuedSpawnsForArea(area);
            return activeSpawns.Count > 0 || queuedSpawns.Count > 0;
        }
    }
}
