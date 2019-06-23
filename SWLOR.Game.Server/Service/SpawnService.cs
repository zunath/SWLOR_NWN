using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NWN;
using SWLOR.Game.Server.AI;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;
using SWLOR.Game.Server.SpawnRule.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class SpawnService
    {
        private static readonly Dictionary<string, ISpawnRule> _spawnRules;

        static SpawnService()
        {
            _spawnRules = new Dictionary<string, ISpawnRule>();
        }

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
            MessageHub.Instance.Subscribe<OnObjectProcessorRan>(message => ProcessSpawns());
        }

        private static void OnModuleLoad()
        {
            RegisterSpawnRules();
            InitializeSpawns();
        }

        private static void RegisterSpawnRules()
        {
            // Use reflection to get all of SpawnRule implementations.
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
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

        public static void InitializeAreaSpawns(NWArea area)
        {
            var areaSpawn = new AreaSpawn();

            // Check for manually placed spawns
            NWObject obj = GetFirstObjectInArea(area.Object);
            while (obj.IsValid)
            {
                bool isSpawn = obj.ObjectType == OBJECT_TYPE_WAYPOINT && obj.GetLocalInt("IS_SPAWN") == TRUE;

                if (isSpawn)
                {
                    int spawnType = obj.GetLocalInt("SPAWN_TYPE");
                    int objectType = spawnType == 0 || spawnType == OBJECT_TYPE_CREATURE ? OBJECT_TYPE_CREATURE : spawnType;
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
                        var spawnObjects = DataService.Where<SpawnObject>(x => x.SpawnID == spawnTableID).ToList();
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

                        if (objectType == OBJECT_TYPE_CREATURE)
                        {
                            areaSpawn.Creatures.Add(newSpawn);
                        }
                        else if (objectType == OBJECT_TYPE_PLACEABLE)
                        {
                            areaSpawn.Placeables.Add(newSpawn);
                        }                      
                    }
                }

                obj = GetNextObjectInArea(area.Object);
            }

            AppCache.AreaSpawns.Add(area, areaSpawn);

            DelayCommand(1.0f, () =>
            {
                SpawnResources(area, areaSpawn);
            });
        }
        
        public static Location GetRandomSpawnPoint(NWArea area)
        {
            Area dbArea = DataService.Single<Area>(x => x.Resref == area.Resref);
            var walkmeshes = DataService.Where<AreaWalkmesh>(x => x.AreaID == dbArea.ID).ToList();
            int count = walkmeshes.Count;
            var index = count <= 0 ? 0 : RandomService.Random(count);

            var spawnPoint = walkmeshes[index];
            
            return Location(area.Object,
                Vector((float)spawnPoint.LocationX, (float)spawnPoint.LocationY, (float)spawnPoint.LocationZ),
                RandomService.RandomFloat(0, 360));

        }

        private static void SpawnResources(NWArea area, AreaSpawn areaSpawn)
        {
            var dbArea = DataService.GetAll<Area>().Single(x => x.Resref == area.Resref);

            if (dbArea.ResourceSpawnTableID <= 0 ||
                !dbArea.AutoSpawnResources) return;
            var possibleSpawns = DataService.Where<SpawnObject>(x => x.SpawnID == dbArea.ResourceSpawnTableID).ToList();

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
                NWPlaceable plc = (CreateObject(OBJECT_TYPE_PLACEABLE, dbSpawn.Resref, location));
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

        public static IReadOnlyCollection<ObjectSpawn> GetAreaPlaceableSpawns(NWArea area)
        {
            var areaSpawn = AppCache.AreaSpawns[area];
            return new ReadOnlyCollection<ObjectSpawn>(areaSpawn.Placeables);
        }
        public static IReadOnlyCollection<ObjectSpawn> GetAreaCreatureSpawns(NWArea area)
        {
            var areaSpawn = AppCache.AreaSpawns[area];
            return new ReadOnlyCollection<ObjectSpawn>(areaSpawn.Creatures);
        }

        public static void AssignScriptEvents(NWCreature creature)
        {
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_HEARTBEAT)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_HEARTBEAT, "x2_def_heartbeat");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_NOTICE)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_NOTICE, "x2_def_percept");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_SPELLCASTAT)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_SPELLCASTAT, "x2_def_spellcast");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_MELEE_ATTACKED)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_MELEE_ATTACKED, "x2_def_attacked");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DAMAGED)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DAMAGED, "x2_def_ondamage");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DISTURBED)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DISTURBED, "x2_def_ondisturb");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_END_COMBATROUND)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_END_COMBATROUND, "x2_def_endcombat");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DIALOGUE)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DIALOGUE, "x2_def_onconv");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_SPAWN_IN)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_SPAWN_IN, "x2_def_spawn");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_RESTED)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_RESTED, "x2_def_rested");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DEATH)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DEATH, "x2_def_ondeath");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_USER_DEFINED_EVENT)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_USER_DEFINED_EVENT, "x2_def_userdef");
            }
            if (string.IsNullOrWhiteSpace(GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_BLOCKED_BY_DOOR)))
            {
                SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_BLOCKED_BY_DOOR, "x2_def_onblocked");
            }
        }

        private static void ProcessSpawns()
        {
            using (new Profiler(nameof(SpawnService) + "." + nameof(ProcessSpawns)))
            {
                foreach (var spawn in AppCache.AreaSpawns)
                {
                    // Check for a valid area - otherwise it causes hangs sometimes when the server shuts down.
                    if (!spawn.Key.IsValid) continue;

                    AreaSpawn areaSpawn = spawn.Value;
                    bool forceSpawn = !areaSpawn.HasSpawned;

                    foreach (var plc in areaSpawn.Placeables.Where(x => x.Respawns || !x.Respawns && !x.HasSpawnedOnce))
                    {
                        ProcessSpawn(plc, OBJECT_TYPE_PLACEABLE, spawn.Key, forceSpawn);
                    }

                    foreach (var creature in areaSpawn.Creatures.Where(x => x.Respawns || !x.Respawns && !x.HasSpawnedOnce))
                    {
                        ProcessSpawn(creature, OBJECT_TYPE_CREATURE, spawn.Key, forceSpawn);
                    }

                    areaSpawn.SecondsEmpty = 0.0f;
                    areaSpawn.HasSpawned = true;
                }
            }
        }


        private static void ProcessSpawn(ObjectSpawn spawn, int objectType, NWArea area, bool forceSpawn)
        {
            // Don't process anything that's valid.
            if (spawn.Spawn.IsValid) return;

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
                    var dbSpawn = DataService.Where<SpawnObject>(x => x.SpawnID == spawn.SpawnTableID)
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
                    Console.WriteLine("ERROR: Cannot locate object with resref " + resref + ". Error occurred in area " + area.Name + " (" + area.Resref + ")");
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

                if (objectType == OBJECT_TYPE_CREATURE)
                    AssignScriptEvents(spawn.Spawn.Object);

                if (!string.IsNullOrWhiteSpace(spawn.SpawnRule))
                {
                    var rule = GetSpawnRule(spawn.SpawnRule);
                    rule.Run(spawn.Spawn);
                }

                ObjectVisibilityService.ApplyVisibilityForObject(spawn.Spawn);

                spawn.Timer = 0.0f;
            }
        }
    }
}
