using System.Numerics;
using SWLOR.Component.World.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.AI.Contracts;
using SWLOR.Shared.Domain.AI.Enums;

namespace SWLOR.Component.World.Service
{
    /// <summary>
    /// Service responsible for creating and configuring spawn objects.
    /// </summary>
    public class SpawnCreationService : ISpawnCreationService
    {
        private readonly ILogger _logger;
        private readonly IWalkmeshService _walkmesh;
        private readonly IRandomService _random;
        private readonly IAIService _ai;
        private readonly ISpawnCacheService _spawnCache;

        public SpawnCreationService(
            ILogger logger,
            IWalkmeshService walkmesh,
            IRandomService random,
            IAIService ai,
            ISpawnCacheService spawnCache)
        {
            _logger = logger;
            _walkmesh = walkmesh;
            _random = random;
            _ai = ai;
            _spawnCache = spawnCache;
        }

        public uint CreateSpawnObject(
            string spawnId,
            string serializedObject,
            string spawnTableId,
            uint area,
            Vector3 position,
            float facing,
            bool useRandomSpawnLocation)
        {
            // Hand-placed spawns are stored as a serialized string.
            // Deserialize and add it to the area.
            if (!string.IsNullOrWhiteSpace(serializedObject))
            {
                var deserialized = ObjectPlugin.Deserialize(serializedObject);
                var finalPosition = useRandomSpawnLocation ?
                    GetPositionFromLocation(_walkmesh.GetRandomLocation(area)) :
                    position;
                ObjectPlugin.AddToArea(deserialized, area, finalPosition);

                var finalFacing = useRandomSpawnLocation ? _random.Next(360) : facing;
                AssignCommand(deserialized, () => SetFacing(finalFacing));
                SetLocalString(deserialized, "SPAWN_ID", spawnId);
                _ai.SetAIFlag(deserialized, AIFlagType.ReturnHome);
                AdjustScripts(deserialized);
                AdjustStats(deserialized);

                return deserialized;
            }
            // Spawn tables have their own logic which must be run to determine the spawn to use.
            // Create the object at the stored location.
            else if (!string.IsNullOrWhiteSpace(spawnTableId))
            {
                var spawnTable = _spawnCache.GetSpawnTable(spawnTableId);
                if (spawnTable == null)
                {
                    _logger.Write<ErrorLogGroup>($"Spawn table {spawnTableId} not found in cache.");
                    return OBJECT_INVALID;
                }

                var spawnObject = spawnTable.GetNextSpawn(_random);

                // It's possible that the rules of the spawn table don't have a spawn ready to be created.
                // In this case, exit early.
                if (string.IsNullOrWhiteSpace(spawnObject.Resref))
                {
                    return OBJECT_INVALID;
                }

                var finalPosition = useRandomSpawnLocation ?
                    GetPositionFromLocation(_walkmesh.GetRandomLocation(area)) :
                    position;

                var finalFacing = useRandomSpawnLocation ? _random.Next(360) : facing;
                var location = Location(area, finalPosition, finalFacing);

                var spawn = CreateObject(spawnObject.Type, spawnObject.Resref, location);
                SetLocalString(spawn, "SPAWN_ID", spawnId);

                _ai.SetAIFlag(spawn, spawnObject.AIFlags);
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

                return spawn;
            }

            return OBJECT_INVALID;
        }

        public void AdjustScripts(uint spawn)
        {
            if (GetIsPC(spawn) || GetIsDM(spawn) || GetIsDMPossessed(spawn))
                return;

            var type = GetObjectType(spawn);

            if (type == ObjectType.Creature)
            {
                var originalSpawnScript = GetEventScript(spawn, EventScriptType.Creature_OnSpawnIn);

                SetEventScript(spawn, EventScriptType.Creature_OnBlockedByDoor, "x2_def_onblocked");
                SetEventScript(spawn, EventScriptType.Creature_OnEndCombatRound, "x2_def_endcombat");
                //SetEventScript(creature, EventScript.Creature_OnDialogue, "x2_def_onconv");
                SetEventScript(spawn, EventScriptType.Creature_OnDamaged, "x2_def_ondamage");
                SetEventScript(spawn, EventScriptType.Creature_OnDeath, "x2_def_ondeath");
                SetEventScript(spawn, EventScriptType.Creature_OnDisturbed, "x2_def_ondisturb");
                SetEventScript(spawn, EventScriptType.Creature_OnHeartbeat, "x2_def_heartbeat");
                SetEventScript(spawn, EventScriptType.Creature_OnNotice, "x2_def_percept");
                SetEventScript(spawn, EventScriptType.Creature_OnMeleeAttacked, "x2_def_attacked");
                SetEventScript(spawn, EventScriptType.Creature_OnRested, "x2_def_rested");
                SetEventScript(spawn, EventScriptType.Creature_OnSpawnIn, "x2_def_spawn");
                SetEventScript(spawn, EventScriptType.Creature_OnSpellCastAt, "x2_def_spellcast");
                SetEventScript(spawn, EventScriptType.Creature_OnUserDefined, "x2_def_userdef");

                // The spawn script will not fire because it has already executed. In the event there wasn't a script
                // already on the creature, we need to run the normal spawn script to ensure it gets created appropriately.
                if (string.IsNullOrWhiteSpace(originalSpawnScript))
                {
                    ExecuteNWScript("x2_def_spawn", spawn);
                }
            }
            else if (type == ObjectType.Placeable)
            {
                if (string.IsNullOrWhiteSpace(GetEventScript(spawn, EventScriptType.Placeable_OnDeath)))
                {
                    SetEventScript(spawn, EventScriptType.Placeable_OnDeath, "plc_death");
                }
            }
        }

        public void AdjustStats(uint spawn)
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

            if (!GetIsObjectValid(GetItemInSlot(InventorySlotType.CreatureRight, spawn)))
            {
                var claw = CreateItemOnObject("npc_claw", spawn);
                AssignCommand(spawn, () =>
                {
                    ActionEquipItem(claw, InventorySlotType.CreatureRight);
                });
            }
            if (!GetIsObjectValid(GetItemInSlot(InventorySlotType.CreatureLeft, spawn)))
            {
                var claw = CreateItemOnObject("npc_claw", spawn);
                AssignCommand(spawn, () =>
                {
                    ActionEquipItem(claw, InventorySlotType.CreatureLeft);
                });
            }
        }

        public void DMSpawnCreature()
        {
            var objectType = (InternalObjectType)Convert.ToInt32(EventsPlugin.GetEventData("OBJECT_TYPE"));

            if (objectType != InternalObjectType.Creature)
                return;

            var objectData = EventsPlugin.GetEventData("OBJECT");
            var spawn = Convert.ToUInt32(objectData, 16); // Not sure why this is in hex.
            AdjustScripts(spawn);
            AdjustStats(spawn);
        }
    }
}
