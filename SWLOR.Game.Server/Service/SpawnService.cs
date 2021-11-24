﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.AI;
using SWLOR.Game.Server.Event.Area;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.SpawnRule.Contracts;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class SpawnService
    {
        private static readonly Dictionary<string, ISpawnRule> _spawnRules;
        private static Dictionary<NWArea, AreaSpawn> AreaSpawns { get; } = new Dictionary<NWArea, AreaSpawn>();

        static SpawnService()
        {
            _spawnRules = new Dictionary<string, ISpawnRule>();
        }

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
            MessageHub.Instance.Subscribe<OnObjectProcessorRan>(message => ProcessSpawns());

            MessageHub.Instance.Subscribe<OnAreaEnter>(message => ToggleCreatureEvents(_.OBJECT_SELF));
            MessageHub.Instance.Subscribe<OnAreaExit>(message => ToggleCreatureEvents(_.OBJECT_SELF));

            MessageHub.Instance.Subscribe<OnAreaInstanceCreated>(message => OnAreaInstanceCreated(message.Instance));
            MessageHub.Instance.Subscribe<OnAreaInstanceDestroyed>(message => OnAreaInstanceDestroyed(message.Instance));

            MessageHub.Instance.Subscribe<OnRequestCacheStats>(message =>
            {
                message.Player.SendMessage("AreaSpawns: " + AreaSpawns.Count);
                message.Player.SendMessage("AreaSpawns Creatures: " + AreaSpawns.Values.SelectMany(x => x.Creatures).Count());
                message.Player.SendMessage("AreaSpawns Placeables: " + AreaSpawns.Values.SelectMany(x => x.Placeables).Count());
            });
        }

        private static void OnModuleLoad()
        {
            RegisterSpawnRules();
            InitializeSpawns();
        }

        private static void RegisterSpawnRules()
        {
            // Use reflection to get all of SpawnRule implementations.
            var classes = Assembly.GetCallingAssembly().GetTypes()
                .Where(p => typeof(ISpawnRule).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract).ToArray();
            foreach (var type in classes)
            {
                ISpawnRule instance = Activator.CreateInstance(type) as ISpawnRule;
                if (instance == null)
                {
                    throw new NullReferenceException("Unable to activate instance of type: " + type);
                }
                _spawnRules.Add(type.Name, instance);
            }
        }

        public static ISpawnRule GetSpawnRule(string key)
        {
            if (!_spawnRules.ContainsKey(key))
            {
                throw new KeyNotFoundException("Spawn rule '" + key + "' is not registered. Did you create a class for it?");
            }

            return _spawnRules[key];
        }

        private static void InitializeSpawns()
        {
            foreach (var area in NWModule.Get().Areas)
            {
                InitializeAreaSpawns(area);
            }
        }

        private static void InitializeAreaSpawns(NWArea area)
        {
            var areaSpawn = new AreaSpawn();

            // Check for manually placed spawns
            NWObject obj = GetFirstObjectInArea(area.Object);
            while (obj.IsValid)
            {
                bool isSpawn = obj.ObjectType == ObjectType.Waypoint && obj.GetLocalBool("IS_SPAWN") == true;

                if (isSpawn)
                {
                    var spawnType = (ObjectType)obj.GetLocalInt("SPAWN_TYPE");
                    var objectType = spawnType == 0 || spawnType == ObjectType.Creature ? ObjectType.Creature : spawnType;
                    int spawnTableID = obj.GetLocalInt("SPAWN_TABLE_ID");
                    int npcGroupID = obj.GetLocalInt("SPAWN_NPC_GROUP_ID");
                    string behaviourScript = obj.GetLocalString("SPAWN_BEHAVIOUR_SCRIPT");
                    if (string.IsNullOrWhiteSpace(behaviourScript))
                        behaviourScript = obj.GetLocalString("SPAWN_BEHAVIOUR");

                    string spawnResref = obj.GetLocalString("SPAWN_RESREF");
                    float respawnTime = obj.GetLocalFloat("SPAWN_RESPAWN_SECONDS");
                    string spawnRule = obj.GetLocalString("SPAWN_RULE");
                    int deathVFXID = obj.GetLocalInt("SPAWN_DEATH_VFX");
                    AIFlags aiFlags = (AIFlags)obj.GetLocalInt("SPAWN_AI_FLAGS");
                    bool useResref = true;

                    // No resref specified but a table was, look in the database for a random record.
                    if (string.IsNullOrWhiteSpace(spawnResref) && spawnTableID > 0)
                    {
                        // Pick a random record.   
                        var spawnObjects = DataService.SpawnObject.GetAllBySpawnTableID(spawnTableID).ToList();
                        int count = spawnObjects.Count;
                        int index = count <= 0 ? 0 : RandomService.Random(count);
                        var dbSpawn = spawnObjects[index];

                        if (dbSpawn != null)
                        {
                            spawnResref = dbSpawn.Resref;
                            useResref = false;

                            if (dbSpawn.NPCGroupID != null && dbSpawn.NPCGroupID > 0)
                                npcGroupID = Convert.ToInt32(dbSpawn.NPCGroupID);

                            if (!string.IsNullOrWhiteSpace(dbSpawn.BehaviourScript))
                                behaviourScript = dbSpawn.BehaviourScript;

                            if (!string.IsNullOrWhiteSpace(dbSpawn.SpawnRule))
                                spawnRule = dbSpawn.SpawnRule;

                            if (deathVFXID <= 0)
                                deathVFXID = dbSpawn.DeathVFXID;

                            if (aiFlags == AIFlags.None)
                                aiFlags = dbSpawn.AIFlags;
                        }
                    }

                    // If we found a resref, spawn the object and add it to the cache.
                    if (!string.IsNullOrWhiteSpace(spawnResref))
                    {
                        // Delay the creation so that the iteration through the area doesn't get thrown off by new entries.
                        Location location = obj.Location;
                        bool isInstance = area.IsInstance;
                        
                        ObjectSpawn newSpawn;
                        if (useResref)
                        {
                            newSpawn = new ObjectSpawn(location, true, spawnResref, respawnTime);
                        }
                        else
                        {
                            newSpawn = new ObjectSpawn(location, true, spawnTableID, respawnTime);
                        }

                        if (npcGroupID > 0)
                        {
                            newSpawn.NPCGroupID = npcGroupID;
                        }

                        if (deathVFXID > 0)
                        {
                            newSpawn.DeathVFXID = deathVFXID;
                        }

                        if (!string.IsNullOrWhiteSpace(behaviourScript))
                        {
                            newSpawn.BehaviourScript = behaviourScript;
                        }

                        if (!string.IsNullOrWhiteSpace(spawnRule))
                        {
                            newSpawn.SpawnRule = spawnRule;
                        }

                        if (aiFlags == AIFlags.None)
                        {
                            newSpawn.AIFlags = aiFlags;
                        }

                        // Instance spawns are one-shot.
                        if (isInstance)
                        {
                            newSpawn.Respawns = false;
                        }

                        if (objectType == ObjectType.Creature)
                        {
                            areaSpawn.Creatures.Add(newSpawn);
                        }
                        else if (objectType == ObjectType.Placeable)
                        {
                            areaSpawn.Placeables.Add(newSpawn);
                        }                      
                    }
                }

                obj = GetNextObjectInArea(area.Object);
            }

            AreaSpawns.Add(area, areaSpawn);

            DelayCommand(1.0f, () =>
            {
                SpawnResources(area, areaSpawn);
            });
        }

        private static void CopyAreaSpawns(string originalResref, NWArea copyArea)
        {
            NWArea originalArea = NWModule.Get().Areas.Single(x => x.Resref == originalResref && x.GetLocalBool("IS_AREA_INSTANCE") == false);
            AreaSpawn copyAreaSpawn = AreaSpawns[originalArea].Clone();
            AreaSpawns.Add(copyArea, copyAreaSpawn);
        }
        
        public static Location GetRandomSpawnPoint(NWArea area)
        {
            var walkmeshes = AreaService.GetAreaWalkmeshes(area);
            int count = walkmeshes.Count;
            var index = count <= 0 ? 0 : RandomService.Random(count);

            var spawnPoint = walkmeshes[index];
            
            return Location(area.Object,
                Vector3((float)spawnPoint.LocationX, (float)spawnPoint.LocationY, (float)spawnPoint.LocationZ),
                RandomService.RandomFloat(0, 360));

        }

        private static void SpawnResources(NWArea area, AreaSpawn areaSpawn)
        {
            var dbArea = DataService.Area.GetByResref(area.Resref);

            if (dbArea.ResourceSpawnTableID <= 0 ||
                !dbArea.AutoSpawnResources) return;
            var possibleSpawns = DataService.SpawnObject.GetAllBySpawnTableID(dbArea.ResourceSpawnTableID).ToList();

            // 1024 size = 32x32
            // 256  size = 16x16
            // 64   size = 8x8
            int size = area.Width * area.Height;

            int maxSpawns = 0;
            if (size <= 12)
            {
                maxSpawns = 2;
            }
            else if (size <= 32)
            {
                maxSpawns = 6;
            }
            else if (size <= 64)
            {
                maxSpawns = 10;
            }
            else if (size <= 256)
            {
                maxSpawns = 25;
            }
            else if (size <= 512)
            {
                maxSpawns = 40;
            }
            else if (size <= 1024)
            {
                maxSpawns = 50;
            }

            int[] weights = new int[possibleSpawns.Count()];
            for (int x = 0; x < possibleSpawns.Count(); x++)
            {
                weights[x] = possibleSpawns.ElementAt(x).Weight;
            }

            for (int x = 1; x <= maxSpawns; x++)
            {
                int index = RandomService.GetRandomWeightedIndex(weights);
                var dbSpawn = possibleSpawns.ElementAt(index);
                Location location = GetRandomSpawnPoint(area);
                NWPlaceable plc = (CreateObject(ObjectType.Placeable, dbSpawn.Resref, location));
                ObjectSpawn spawn = new ObjectSpawn(location, false, dbArea.ResourceSpawnTableID, 600.0f);
                spawn.Spawn = plc;

                ObjectVisibilityService.ApplyVisibilityForObject(plc);
                
                if (dbSpawn.NPCGroupID != null && dbSpawn.NPCGroupID > 0)
                {
                    plc.SetLocalInt("NPC_GROUP", Convert.ToInt32(dbSpawn.NPCGroupID));
                    spawn.NPCGroupID = Convert.ToInt32(dbSpawn.NPCGroupID);
                }

                if (!string.IsNullOrWhiteSpace(dbSpawn.BehaviourScript) &&
                    string.IsNullOrWhiteSpace(plc.GetLocalString("BEHAVIOUR")))
                {
                    plc.SetLocalString("BEHAVIOUR", dbSpawn.BehaviourScript);
                    spawn.BehaviourScript = dbSpawn.BehaviourScript;
                }

                if (!string.IsNullOrWhiteSpace(dbSpawn.SpawnRule))
                {
                    var rule = GetSpawnRule(dbSpawn.SpawnRule);
                    rule.Run(plc);
                }

                areaSpawn.Placeables.Add(spawn);
            }
        }


        public static void AssignScriptEvents(NWCreature creature)
        {
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnHeartbeat)))
            {
                SetEventScript(creature, EventScript.Creature_OnHeartbeat, "x2_def_heartbeat");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnNotice)))
            {
                SetEventScript(creature, EventScript.Creature_OnNotice, "x2_def_percept");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnSpellCastAt)))
            {
                SetEventScript(creature, EventScript.Creature_OnSpellCastAt, "x2_def_spellcast");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnMeleeAttacked)))
            {
                SetEventScript(creature, EventScript.Creature_OnMeleeAttacked, "x2_def_attacked");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnDamaged)))
            {
                SetEventScript(creature, EventScript.Creature_OnDamaged, "x2_def_ondamage");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnDisturbed)))
            {
                SetEventScript(creature, EventScript.Creature_OnDisturbed, "x2_def_ondisturb");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnEndCombatRound)))
            {
                SetEventScript(creature, EventScript.Creature_OnEndCombatRound, "x2_def_endcombat");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnDialogue)))
            {
                SetEventScript(creature, EventScript.Creature_OnDialogue, "x2_def_onconv");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnSpawnIn)))
            {
                SetEventScript(creature, EventScript.Creature_OnSpawnIn, "x2_def_spawn");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnRested)))
            {
                SetEventScript(creature, EventScript.Creature_OnRested, "x2_def_rested");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnDeath)))
            {
                SetEventScript(creature, EventScript.Creature_OnDeath, "x2_def_ondeath");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnUserDefined)))
            {
                SetEventScript(creature, EventScript.Creature_OnUserDefined, "x2_def_userdef");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EventScript.Creature_OnBlockedByDoor)))
            {
                SetEventScript(creature, EventScript.Creature_OnBlockedByDoor, "x2_def_onblocked");
            }
        }

        private static void ToggleCreatureEvents(NWArea area)
        {
            AreaSpawn areaSpawn = AreaSpawns[area];
            int playerCount = NWNXArea.GetNumberOfPlayersInArea(area);

            foreach (var creature in areaSpawn.Creatures)
            {
                if (creature.Spawn.IsValid)
                {
                    bool eventsDisabled = GetLocalBool(creature.Spawn, "SPAWN_EVENTS_DISABLED") == true;
                    bool isCreature = creature.Spawn.IsCreature;

                    if (isCreature)
                    {
                        // Currently disabled, but players are in area. Enable them.
                        if (eventsDisabled && playerCount > 0)
                        {
                            EnableCreatureEvents(creature.SpawnCreature);
                            creature.SpawnCreature.SetLocalBool("SPAWN_EVENTS_DISABLED", false);
                        }
                        // Currently enabled, but players are no longer in area. Disable them.
                        else if (!eventsDisabled && playerCount <= 0)
                        {
                            DisableCreatureEvents(creature.SpawnCreature);
                            creature.SpawnCreature.SetLocalBool("SPAWN_EVENTS_DISABLED", true);
                        }
                    }
                }
            }
        }


        private static void ProcessSpawns()
        {
            using (new Profiler(nameof(SpawnService) + "." + nameof(ProcessSpawns)))
            {
                // On module load, we want to populate all areas with NPCs and disable their AI.
                // The reason for this is because we don't want lag when players enter an area. 
                // This'll use more memory but the CPU usage will be very limited as none of the
                // creatures will have scripts assigned.
                bool hasRunOnce = GetLocalBool(GetModule(), "SPAWN_HAS_RUN_ONCE");

                foreach (var spawn in AreaSpawns)
                {
                    // Check for a valid area - otherwise it causes hangs sometimes when the server shuts down.
                    if (!spawn.Key.IsValid) continue;

                    // Ignore empty areas.
                    int playerCount = NWNXArea.GetNumberOfPlayersInArea(spawn.Key);
                    if (playerCount <= 0 && hasRunOnce) continue;

                    AreaSpawn areaSpawn = spawn.Value;
                    bool forceSpawn = !areaSpawn.HasSpawned;

                    foreach (var plc in areaSpawn.Placeables.Where(x => x.Respawns || !x.Respawns && !x.HasSpawnedOnce))
                    {
                        ProcessSpawn(plc, ObjectType.Placeable, spawn.Key, forceSpawn);
                    }

                    foreach (var creature in areaSpawn.Creatures.Where(x => x.Respawns || !x.Respawns && !x.HasSpawnedOnce))
                    {
                        ProcessSpawn(creature, ObjectType.Creature, spawn.Key, forceSpawn);
                    }

                    areaSpawn.SecondsEmpty = 0.0f;
                    areaSpawn.HasSpawned = true;

                    // Toggle creature AI now, if this is the first time we're running this process.
                    if (!hasRunOnce)
                    {
                        ToggleCreatureEvents(spawn.Key);
                    }
                }

                SetLocalBool(GetModule(), "SPAWN_HAS_RUN_ONCE", true);
            }
        }


        private static void ProcessSpawn(ObjectSpawn spawn, ObjectType objectType, NWArea area, bool forceSpawn)
        {
            // Spawn expiry.
            if (spawn.Expires < DateTime.UtcNow)
            {
                DestroyObject(spawn.Spawn);                
            }
            // Don't process anything that's valid.  Put this in an else branch as object destruction
            // happens after script completion. 
            else if (spawn.Spawn.IsValid) return;

            spawn.Timer += ObjectProcessingService.ProcessingTickInterval;

            // Time to respawn!
            if (spawn.Timer >= spawn.RespawnTime || forceSpawn)
            {
                string resref = spawn.Resref;
                int npcGroupID = spawn.NPCGroupID;
                int deathVFXID = spawn.DeathVFXID;
                string behaviour = spawn.BehaviourScript;
                NWLocation location = spawn.IsStaticSpawnPoint ? spawn.SpawnLocation : null;
                AIFlags aiFlags = spawn.AIFlags;

                spawn.HasSpawnedOnce = true;

                // Look for a spawn out of the database set. Update spawn data if one is found.
                if (string.IsNullOrWhiteSpace(resref))
                {
                    var dbSpawn = DataService.SpawnObject.GetAllBySpawnTableID(spawn.SpawnTableID)
                        .OrderBy(o => Guid.NewGuid()).First();

                    resref = dbSpawn.Resref;
                    npcGroupID = dbSpawn.NPCGroupID ?? 0;
                    deathVFXID = dbSpawn.DeathVFXID;
                    behaviour = dbSpawn.BehaviourScript;
                    aiFlags = dbSpawn.AIFlags;

                    if (!string.IsNullOrWhiteSpace(dbSpawn.SpawnRule))
                    {
                        spawn.SpawnRule = dbSpawn.SpawnRule;
                    }
                    else
                    {
                        // Clear the saved spawn rule since we now have a new resref etc.
                        spawn.SpawnRule = null;
                    }
                }

                if (location == null)
                {
                    location = GetRandomSpawnPoint(area);
                }

                spawn.Spawn = CreateObject(objectType, resref, location);

                if (!spawn.Spawn.IsValid)
                {
                    Console.WriteLine("SPAWN SERVICE ERROR: Cannot locate object with resref " + resref + ". Error occurred in area " + area.Name + " (" + area.Resref + ")");
                    return;
                }

                if (npcGroupID > 0)
                    spawn.Spawn.SetLocalInt("NPC_GROUP", npcGroupID);

                if (deathVFXID > 0)
                    spawn.Spawn.SetLocalInt("DEATH_VFX", deathVFXID);

                if (!string.IsNullOrWhiteSpace(behaviour) &&
                    string.IsNullOrWhiteSpace(spawn.Spawn.GetLocalString("BEHAVIOUR")))
                    spawn.Spawn.SetLocalString("BEHAVIOUR", behaviour);

                spawn.Spawn.SetLocalInt("AI_FLAGS", (int) aiFlags);

                // Set expiry time for spawn.  Set creatures to 26 hours (i.e. longer than the server reset
                // cycle, so never).  Set placeables to 1 hour, so blocking objects will move and resources
                // will reset.
                if (objectType == ObjectType.Creature)
                {
                    AssignScriptEvents(spawn.Spawn.Object);
                    spawn.Expires = DateTime.UtcNow.AddHours(26);
                }
                else
                {
                    spawn.Expires = DateTime.UtcNow.AddHours(1);
                }

                if (!string.IsNullOrWhiteSpace(spawn.SpawnRule))
                {
                    var rule = GetSpawnRule(spawn.SpawnRule);
                    rule.Run(spawn.Spawn);
                }

                ObjectVisibilityService.ApplyVisibilityForObject(spawn.Spawn);

                spawn.Timer = 0.0f;
            }
        }


        /// <summary>
        /// Sets event scripts base on local variables and deletes the local variables when done.
        /// </summary>
        /// <param name="creature">The creatures whose events we're enabling</param>
        private static void EnableCreatureEvents(NWCreature creature)
        {
            // NOTE: Don't disable the spawn-in event because it's necessary for AI to work.
            SetEventScript(creature, EventScript.Creature_OnHeartbeat, creature.GetLocalString("SPAWN_CREATURE_EVENT_ON_HEARTBEAT"));
            SetEventScript(creature, EventScript.Creature_OnNotice, creature.GetLocalString("SPAWN_CREATURE_EVENT_ON_NOTICE"));
            SetEventScript(creature, EventScript.Creature_OnSpellCastAt, creature.GetLocalString("SPAWN_CREATURE_EVENT_ON_SPELLCASTAT"));
            SetEventScript(creature, EventScript.Creature_OnMeleeAttacked, creature.GetLocalString("SPAWN_CREATURE_EVENT_ON_MELEE_ATTACKED"));
            SetEventScript(creature, EventScript.Creature_OnDamaged, creature.GetLocalString("SPAWN_CREATURE_EVENT_ON_DAMAGED"));
            SetEventScript(creature, EventScript.Creature_OnDisturbed, creature.GetLocalString("SPAWN_CREATURE_EVENT_ON_DISTURBED"));
            SetEventScript(creature, EventScript.Creature_OnEndCombatRound, creature.GetLocalString("SPAWN_CREATURE_EVENT_ON_END_COMBATROUND"));
            SetEventScript(creature, EventScript.Creature_OnDialogue, creature.GetLocalString("SPAWN_CREATURE_EVENT_ON_DIALOGUE"));
            SetEventScript(creature, EventScript.Creature_OnRested, creature.GetLocalString("SPAWN_CREATURE_EVENT_ON_RESTED"));
            SetEventScript(creature, EventScript.Creature_OnDeath, creature.GetLocalString("SPAWN_CREATURE_EVENT_ON_DEATH"));
            SetEventScript(creature, EventScript.Creature_OnUserDefined, creature.GetLocalString("SPAWN_CREATURE_EVENT_ON_USER_DEFINED_EVENT"));
            SetEventScript(creature, EventScript.Creature_OnBlockedByDoor, creature.GetLocalString("SPAWN_CREATURE_EVENT_ON_BLOCKED_BY_DOOR"));
            creature.DeleteLocalString("SPAWN_CREATURE_EVENT_ON_HEARTBEAT");
            creature.DeleteLocalString("SPAWN_CREATURE_EVENT_ON_NOTICE");
            creature.DeleteLocalString("SPAWN_CREATURE_EVENT_ON_SPELLCASTAT");
            creature.DeleteLocalString("SPAWN_CREATURE_EVENT_ON_MELEE_ATTACK");
            creature.DeleteLocalString("SPAWN_CREATURE_EVENT_ON_DAMAGED");
            creature.DeleteLocalString("SPAWN_CREATURE_EVENT_ON_DISTURBED");
            creature.DeleteLocalString("SPAWN_CREATURE_EVENT_ON_END_COMBATROUND");
            creature.DeleteLocalString("SPAWN_CREATURE_EVENT_ON_DIALOGUE");
            creature.DeleteLocalString("SPAWN_CREATURE_EVENT_ON_RESTED");
            creature.DeleteLocalString("SPAWN_CREATURE_EVENT_ON_DEATH");
            creature.DeleteLocalString("SPAWN_CREATURE_EVENT_ON_USER_DEFINED_EVENT");
            creature.DeleteLocalString("SPAWN_CREATURE_EVENT_ON_BLOCKED_BY_DOOR");
        }

        /// <summary>
        /// Sets the current script names as local variables on the creature and then disables all events.
        /// </summary>
        /// <param name="creature">The creatures whose events we're disabling.</param>
        private static void DisableCreatureEvents(NWCreature creature)
        {
            creature.SetLocalString("SPAWN_CREATURE_EVENT_ON_HEARTBEAT", GetEventScript(creature, EventScript.Creature_OnHeartbeat));
            creature.SetLocalString("SPAWN_CREATURE_EVENT_ON_NOTICE", GetEventScript(creature, EventScript.Creature_OnNotice));
            creature.SetLocalString("SPAWN_CREATURE_EVENT_ON_SPELLCASTAT", GetEventScript(creature, EventScript.Creature_OnSpellCastAt));
            creature.SetLocalString("SPAWN_CREATURE_EVENT_ON_MELEE_ATTACKED", GetEventScript(creature, EventScript.Creature_OnMeleeAttacked));
            creature.SetLocalString("SPAWN_CREATURE_EVENT_ON_DAMAGED", GetEventScript(creature, EventScript.Creature_OnDamaged));
            creature.SetLocalString("SPAWN_CREATURE_EVENT_ON_DISTURBED", GetEventScript(creature, EventScript.Creature_OnDisturbed));
            creature.SetLocalString("SPAWN_CREATURE_EVENT_ON_END_COMBATROUND", GetEventScript(creature, EventScript.Creature_OnEndCombatRound));
            creature.SetLocalString("SPAWN_CREATURE_EVENT_ON_DIALOGUE", GetEventScript(creature, EventScript.Creature_OnDialogue));
            creature.SetLocalString("SPAWN_CREATURE_EVENT_ON_RESTED", GetEventScript(creature, EventScript.Creature_OnRested));
            creature.SetLocalString("SPAWN_CREATURE_EVENT_ON_DEATH", GetEventScript(creature, EventScript.Creature_OnDeath));
            creature.SetLocalString("SPAWN_CREATURE_EVENT_ON_USER_DEFINED_EVENT", GetEventScript(creature, EventScript.Creature_OnUserDefined));
            creature.SetLocalString("SPAWN_CREATURE_EVENT_ON_BLOCKED_BY_DOOR", GetEventScript(creature, EventScript.Creature_OnBlockedByDoor));
            SetEventScript(creature, EventScript.Creature_OnHeartbeat, string.Empty);
            SetEventScript(creature, EventScript.Creature_OnNotice, string.Empty);
            SetEventScript(creature, EventScript.Creature_OnSpellCastAt, string.Empty);
            SetEventScript(creature, EventScript.Creature_OnMeleeAttacked, string.Empty);
            SetEventScript(creature, EventScript.Creature_OnDamaged, string.Empty);
            SetEventScript(creature, EventScript.Creature_OnDisturbed, string.Empty);
            SetEventScript(creature, EventScript.Creature_OnEndCombatRound, string.Empty);
            SetEventScript(creature, EventScript.Creature_OnDialogue, string.Empty);
            SetEventScript(creature, EventScript.Creature_OnRested, string.Empty);
            SetEventScript(creature, EventScript.Creature_OnDeath, string.Empty);
            SetEventScript(creature, EventScript.Creature_OnUserDefined, string.Empty);
            SetEventScript(creature, EventScript.Creature_OnBlockedByDoor, string.Empty);
        }

        private static void OnAreaInstanceCreated(NWArea instance)
        {
            CopyAreaSpawns(instance.Resref, instance);
        }

        private static void OnAreaInstanceDestroyed(NWArea instance)
        {
            if(AreaSpawns.ContainsKey(instance))
                AreaSpawns.Remove(instance);
        }
    }
}
