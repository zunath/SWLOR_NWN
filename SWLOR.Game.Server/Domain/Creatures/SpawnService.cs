using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
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
        private readonly IAIService _ai;
        private readonly ICreatureStatService _stat;
        private readonly IWalkmeshService _walkmesh;

        public SpawnService(ISpawnRegistryService registry, IAIService ai, ICreatureStatService stat, IWalkmeshService walkmesh)
        {
            _registry = registry;
            _ai = ai;
            _stat = stat;
            _walkmesh = walkmesh;
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
        /// Queues a resource for despawning after the configured amount of time.
        /// This is automatically called when resources are spawned from resource spawn tables.
        /// A random variance of ±25% is applied to prevent all resources from despawning simultaneously.
        /// </summary>
        /// <param name="resourceObject">The resource object to despawn</param>
        /// <param name="spawnDetailId">The spawn detail ID of the resource</param>
        /// <param name="despawnMinutes">The number of minutes before despawning</param>
        private void QueueResourceDespawn(uint resourceObject, Guid spawnDetailId, int despawnMinutes)
        {
            var now = UtcNow();

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
            _registry.QueuedResourceDespawns.Add(resourceDespawn);
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
                    var spawnResult = SpawnObject(queuedSpawn.SpawnDetailId, detail);

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

        private void AdjustScripts(uint spawn)
        {
            if (GetIsPC(spawn) || GetIsDM(spawn) || GetIsDMPossessed(spawn))
                return;

            var type = GetObjectType(spawn);

            if (type == ObjectType.Creature)
            {
                var originalSpawnScript = GetEventScript(spawn, EventScript.Creature_OnSpawnIn);
                ObjectPlugin.SetConversationPrivate(spawn, true);

                SetEventScript(spawn, EventScript.Creature_OnBlockedByDoor, "x2_def_onblocked");
                SetEventScript(spawn, EventScript.Creature_OnEndCombatRound, "x2_def_endcombat");
                //SetEventScript(creature, EventScript.Creature_OnDialogue, "x2_def_onconv");
                SetEventScript(spawn, EventScript.Creature_OnDamaged, "x2_def_ondamage");
                SetEventScript(spawn, EventScript.Creature_OnDeath, "x2_def_ondeath");
                SetEventScript(spawn, EventScript.Creature_OnDisturbed, "x2_def_ondisturb");
                // Static service NPCs may opt directly into the managed heartbeat,
                // avoiding the stock ambient-AI script while retaining SWLOR behavior.
                if (GetEventScript(spawn, EventScript.Creature_OnHeartbeat) != ScriptName.OnCreatureHeartbeatAfter)
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
        private void AdjustStats(uint spawn)
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

        private void RemoveActiveSpawn(SpawnDetail detail, uint spawnObject)
        {
            if (!_registry.ActiveSpawnsByArea.TryGetValue(detail.Area, out var activeSpawns))
                return;

            activeSpawns.RemoveAll(x => x.SpawnObject == spawnObject);
        }

        private List<Vector3> GetActivePlaceableSpawnPositions(uint area)
        {
            var positions = new List<Vector3>();
            if (!_registry.ActiveSpawnsByArea.TryGetValue(area, out var activeSpawns))
                return positions;

            foreach (var activeSpawn in activeSpawns)
            {
                if (!GetIsObjectValid(activeSpawn.SpawnObject) ||
                    GetObjectType(activeSpawn.SpawnObject) != ObjectType.Placeable)
                    continue;

                positions.Add(GetPosition(activeSpawn.SpawnObject));
            }

            return positions;
        }

        private bool HasActiveRareSpawn(uint area, string spawnTableId)
        {
            if (string.IsNullOrWhiteSpace(spawnTableId) ||
                !_registry.ActiveSpawnsByArea.TryGetValue(area, out var activeSpawns))
                return false;

            activeSpawns.RemoveAll(x => !GetIsObjectValid(x.SpawnObject));

            foreach (var activeSpawn in activeSpawns)
            {
                if (!activeSpawn.IsRare)
                    continue;

                var detail = _registry.Spawns[activeSpawn.SpawnDetailId];
                if (detail.SpawnTableId == spawnTableId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// When a DM spawns a creature, attach all required scripts to it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMSpawnObjectAfter)]
        public void DMSpawnCreature()
        {
            var objectType = (InternalObjectType)Convert.ToInt32(EventsPlugin.GetEventData("OBJECT_TYPE"));

            if (objectType != InternalObjectType.Creature)
                return;

            var objectData = EventsPlugin.GetEventData("OBJECT");
            var spawn = Convert.ToUInt32(objectData, 16); // Not sure why this is in hex.
            PrepareDMSpawnedCreature(spawn);
        }

        /// <summary>
        /// Attaches the scripts and stat adjustments every DM-spawned creature receives.
        /// Used by the native DM creator event and the NUI DM creator.
        /// </summary>
        /// <param name="creature">The creature a DM spawned.</param>
        public void PrepareDMSpawnedCreature(uint creature)
        {
            AdjustScripts(creature);
            AdjustStats(creature);
        }

        /// <summary>
        /// Creates a new spawn object into its spawn area.
        /// Hand-placed objects are deserialized and added to the area.
        /// Spawn tables run their own logic to determine which object to spawn.
        /// </summary>
        /// <param name="spawnId">The ID of the spawn</param>
        /// <param name="detail">The details of the spawn</param>
        private SpawnResult SpawnObject(Guid spawnId, SpawnDetail detail)
        {
            // Hand-placed spawns are stored as a serialized string.
            // Deserialize and add it to the area.
            if (!string.IsNullOrWhiteSpace(detail.SerializedObject))
            {
                var deserialized = ObjectPlugin.Deserialize(detail.SerializedObject);
                var position = detail.UseRandomSpawnLocation ?
                    GetPositionFromLocation(_walkmesh.GetRandomLocation(detail.Area)) :
                    new Vector3(detail.X, detail.Y, detail.Z);
                ObjectPlugin.AddToArea(deserialized, detail.Area, position);

                var facing = detail.UseRandomSpawnLocation ? Random.Next(360) : detail.Facing;
                AssignCommand(deserialized, () => SetFacing(facing));
                SetLocalString(deserialized, "SPAWN_ID", spawnId.ToString());
                _ai.SetAIFlag(deserialized, AIFlag.ReturnHome);
                AdjustScripts(deserialized);
                AdjustStats(deserialized);
                _stat.LoadNPCStats(deserialized);

                return new SpawnResult { SpawnObject = deserialized };
            }
            // Spawn tables have their own logic which must be run to determine the spawn to use.
            // Create the object at the stored location.
            else if (!string.IsNullOrWhiteSpace(detail.SpawnTableId))
            {
                var spawnTable = _registry.SpawnTables[detail.SpawnTableId];
                var spawnObject = spawnTable.GetNextSpawn(!HasActiveRareSpawn(detail.Area, detail.SpawnTableId));

                // It's possible that the rules of the spawn table don't have a spawn ready to be created.
                // In this case, exit early.
                if (string.IsNullOrWhiteSpace(spawnObject.Resref))
                {
                    return new SpawnResult { SpawnObject = OBJECT_INVALID };
                }

                Vector3 position;
                if (!detail.UseRandomSpawnLocation)
                {
                    position = new Vector3(detail.X, detail.Y, detail.Z);
                }
                // Randomly placed placeables (resource nodes, terminals) keep their distance from each other
                // so that several of them cannot block a narrow path together.
                else if (spawnObject.Type == ObjectType.Placeable)
                {
                    position = SpawnSpacing.PickSpacedPosition(
                        () => GetPositionFromLocation(_walkmesh.GetRandomLocation(detail.Area)),
                        GetActivePlaceableSpawnPositions(detail.Area));
                }
                else
                {
                    position = GetPositionFromLocation(_walkmesh.GetRandomLocation(detail.Area));
                }

                var facing = detail.UseRandomSpawnLocation ? Random.Next(360) : detail.Facing;
                var location = Location(detail.Area, position, facing);

                var spawn = CreateObject(spawnObject.Type, spawnObject.Resref, location);
                SetLocalString(spawn, "SPAWN_ID", spawnId.ToString());

                _ai.SetAIFlag(spawn, spawnObject.AIFlags);
                if (spawnObject.AIProfile != AIProfileType.Invalid)
                {
                    _ai.SetAIProfile(spawn, spawnObject.AIProfile);
                }
                AdjustScripts(spawn);
                AdjustStats(spawn);
                if (spawnObject.Type == ObjectType.Creature)
                {
                    _stat.LoadNPCStats(spawn);
                }

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

                return new SpawnResult
                {
                    SpawnObject = spawn,
                    IsRare = spawnObject.IsRare
                };
            }

            return new SpawnResult { SpawnObject = OBJECT_INVALID };
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
            QueueResourceDespawn(resourceObject, id, spawnTable.ResourceDespawnMinutes);

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
