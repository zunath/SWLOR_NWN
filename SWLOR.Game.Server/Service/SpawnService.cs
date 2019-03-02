﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class SpawnService : ISpawnService
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IObjectProcessingService _processor;
        private readonly AppCache _cache;
        private readonly IRandomService _random;

        public SpawnService(
            INWScript script,
            IDataService data,
            IRandomService random,
            IObjectProcessingService processor,
            AppCache cache)
        {
            _ = script;
            _data = data;
            _random = random;
            _processor = processor;
            _cache = cache;
        }

        public void OnModuleLoad()
        {
            InitializeSpawns();
            _processor.RegisterProcessingEvent<SpawnProcessor>();
        }

        private void InitializeSpawns()
        {
            foreach (var area in NWModule.Get().Areas)
            {
                InitializeAreaSpawns(area);
            }
        }

        public void InitializeAreaSpawns(NWArea area)
        {
            var areaSpawn = new AreaSpawn();

            // Check for manually placed spawns
            NWObject obj = _.GetFirstObjectInArea(area.Object);
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
                    bool useResref = true;

                    // No resref specified but a table was, look in the database for a random record.
                    if (string.IsNullOrWhiteSpace(spawnResref) && spawnTableID > 0)
                    {
                        // Pick a random record.   
                        var spawnObjects = _data.Where<SpawnObject>(x => x.SpawnID == spawnTableID).ToList();
                        int count = spawnObjects.Count;
                        int index = count <= 0 ? 0 : _random.Random(count);
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

                obj = _.GetNextObjectInArea(area.Object);
            }

            _cache.AreaSpawns.Add(area, areaSpawn);

            _.DelayCommand(1.0f, () =>
            {
                SpawnResources(area, areaSpawn);
            });
        }
        
        public Location GetRandomSpawnPoint(NWArea area)
        {
            Area dbArea = _data.Single<Area>(x => x.Resref == area.Resref);
            var walkmeshes = _data.Where<AreaWalkmesh>(x => x.AreaID == dbArea.ID).ToList();
            int count = walkmeshes.Count;
            var index = count <= 0 ? 0 : _random.Random(count);

            var spawnPoint = walkmeshes[index];
            
            return _.Location(area.Object,
                _.Vector((float)spawnPoint.LocationX, (float)spawnPoint.LocationY, (float)spawnPoint.LocationZ),
                _random.RandomFloat(0, 360));

        }

        private void SpawnResources(NWArea area, AreaSpawn areaSpawn)
        {
            var dbArea = _data.GetAll<Area>().Single(x => x.Resref == area.Resref);

            if (dbArea.ResourceSpawnTableID <= 0 ||
                !dbArea.AutoSpawnResources) return;
            var possibleSpawns = _data.Where<SpawnObject>(x => x.SpawnID == dbArea.ResourceSpawnTableID).ToList();

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
                int index = _random.GetRandomWeightedIndex(weights);
                var dbSpawn = possibleSpawns.ElementAt(index);
                Location location = GetRandomSpawnPoint(area);
                NWPlaceable plc = (_.CreateObject(OBJECT_TYPE_PLACEABLE, dbSpawn.Resref, location));
                ObjectSpawn spawn = new ObjectSpawn(location, false, dbArea.ResourceSpawnTableID, 600.0f);
                spawn.Spawn = plc;

                App.Resolve<IObjectVisibilityService>(ovs =>
                {
                    ovs.ApplyVisibilityForObject(plc);
                });

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
                    App.ResolveByInterface<ISpawnRule>("SpawnRule." + dbSpawn.SpawnRule, rule =>
                    {
                        rule.Run(plc);
                    });
                }

                areaSpawn.Placeables.Add(spawn);
            }
        }

        public IReadOnlyCollection<ObjectSpawn> GetAreaPlaceableSpawns(NWArea area)
        {
            var areaSpawn = _cache.AreaSpawns[area];
            return new ReadOnlyCollection<ObjectSpawn>(areaSpawn.Placeables);
        }
        public IReadOnlyCollection<ObjectSpawn> GetAreaCreatureSpawns(NWArea area)
        {
            var areaSpawn = _cache.AreaSpawns[area];
            return new ReadOnlyCollection<ObjectSpawn>(areaSpawn.Creatures);
        }

        public void AssignScriptEvents(NWCreature creature)
        {
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_HEARTBEAT)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_HEARTBEAT, "x2_def_heartbeat");
            }
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_NOTICE)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_NOTICE, "x2_def_percept");
            }
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_SPELLCASTAT)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_SPELLCASTAT, "x2_def_spellcast");
            }
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_MELEE_ATTACKED)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_MELEE_ATTACKED, "x2_def_attacked");
            }
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DAMAGED)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DAMAGED, "x2_def_ondamage");
            }
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DISTURBED)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DISTURBED, "x2_def_ondisturb");
            }
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_END_COMBATROUND)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_END_COMBATROUND, "x2_def_endcombat");
            }
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DIALOGUE)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DIALOGUE, "x2_def_onconv");
            }
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_SPAWN_IN)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_SPAWN_IN, "x2_def_spawn");
            }
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_RESTED)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_RESTED, "x2_def_rested");
            }
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DEATH)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_DEATH, "x2_def_ondeath");
            }
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_USER_DEFINED_EVENT)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_USER_DEFINED_EVENT, "x2_def_userdef");
            }
            if (string.IsNullOrWhiteSpace(_.GetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_BLOCKED_BY_DOOR)))
            {
                _.SetEventScript(creature, EVENT_SCRIPT_CREATURE_ON_BLOCKED_BY_DOOR, "x2_def_onblocked");
            }
        }
    }
}
