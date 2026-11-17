using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// Creates the object of a spawn (hand-placed or from a spawn table), queues a spawned resource's natural despawn and
    /// prepares DM-spawned creatures, moved here unchanged from <see cref="SpawnService"/> (part SP-3 of
    /// Readmes/MaintainabilityRefactorCreaturesSplit.md). It keeps no state of its own: the spawn tables, the active spawns and the
    /// despawn queue are <see cref="ISpawnRegistryService"/>.
    /// </summary>
    public class SpawnObjectService : ISpawnObjectService
    {
        private readonly ISpawnRegistryService _registry;
        private readonly IAIService _ai;
        private readonly ICreatureStatService _stat;
        private readonly IWalkmeshService _walkmesh;

        public SpawnObjectService(ISpawnRegistryService registry, IAIService ai, ICreatureStatService stat, IWalkmeshService walkmesh)
        {
            _registry = registry;
            _ai = ai;
            _stat = stat;
            _walkmesh = walkmesh;
        }

        /// <summary>
        /// The current UTC time. <c>protected virtual</c> so a unit test can place a resource despawn at a time of its own;
        /// the body is the original <c>DateTime.UtcNow</c>.
        /// </summary>
        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        /// <summary>
        /// Queues a resource for despawning after the configured amount of time.
        /// This is automatically called when resources are spawned from resource spawn tables.
        /// A random variance of ±25% is applied to prevent all resources from despawning simultaneously.
        /// </summary>
        /// <param name="resourceObject">The resource object to despawn</param>
        /// <param name="spawnDetailId">The spawn detail ID of the resource</param>
        /// <param name="despawnMinutes">The number of minutes before despawning</param>
        public void QueueResourceDespawn(uint resourceObject, Guid spawnDetailId, int despawnMinutes)
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
        public SpawnResult SpawnObject(Guid spawnId, SpawnDetail detail)
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
    }
}
