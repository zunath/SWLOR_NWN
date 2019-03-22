using System;
using System.Diagnostics;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Processor
{
    public class SpawnProcessor: IEventProcessor
    {
        private readonly IObjectProcessingService _processor;
        private readonly IDataService _data;
        private readonly ISpawnService _spawn;
        private readonly INWScript _;

        public SpawnProcessor(
            IObjectProcessingService processor,
            IDataService data,
            ISpawnService spawn,
            INWScript script)
        {
            _processor = processor;
            _data = data;
            _spawn = spawn;
            _ = script;
        }

        public void Run(object[] args)
        {
            var spawns = AppCache.AreaSpawns;

            foreach (var spawn in spawns)
            {
                // Check for a valid area - otherwise it causes hangs sometimes when the server shuts down.
                if (!spawn.Key.IsValid) continue;

                AreaSpawn areaSpawn = spawn.Value;
                int pcsInArea = NWModule.Get().Players.Count(x => x.Area.Equals(spawn.Key));

                // No players in area. Process the despawner.
                if(pcsInArea <= 0 && areaSpawn.HasSpawned)
                {
                    areaSpawn.SecondsEmpty += _processor.ProcessingTickInterval;

                    if(areaSpawn.SecondsEmpty >= 1200) // 20 minutes have passed with no players in the area.
                    {
                        areaSpawn.SecondsEmpty = 0.0f;
                        areaSpawn.HasSpawned = false;

                        foreach(var plc in areaSpawn.Placeables)
                        {
                            NWPlaceable prop = plc.Spawn.GetLocalObject("RESOURCE_PROP_OBJ");
                            if (prop.IsValid)
                            {
                                prop.Destroy();
                            }
                            
                            if(plc.Spawn.IsValid)
                            {
                                plc.Spawn.Destroy();
                            }
                        }

                        foreach(var creature in areaSpawn.Creatures)
                        {
                            if(creature.Spawn.IsValid)
                            {
                                creature.Spawn.Destroy();
                            }
                        }
                    }
                }
                // Players in the area
                else if(pcsInArea > 0)
                {
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


        private void ProcessSpawn(ObjectSpawn spawn, int objectType, NWArea area, bool forceSpawn)
        {
            // Don't process anything that's valid.
            if (spawn.Spawn.IsValid) return;

            spawn.Timer += _processor.ProcessingTickInterval;

            // Time to respawn!
            if (spawn.Timer >= spawn.RespawnTime || forceSpawn)
            {
                string resref = spawn.Resref;
                int npcGroupID = spawn.NPCGroupID;
                int deathVFXID = spawn.DeathVFXID;
                string behaviour = spawn.BehaviourScript;
                NWLocation location = spawn.IsStaticSpawnPoint ? spawn.SpawnLocation : null;

                spawn.HasSpawnedOnce = true;

                // Look for a spawn out of the database set. Update spawn data if one is found.
                if (string.IsNullOrWhiteSpace(resref))
                {
                    var dbSpawn = _data.Where<SpawnObject>(x => x.SpawnID == spawn.SpawnTableID)
                        .OrderBy(o => Guid.NewGuid()).First();

                    resref = dbSpawn.Resref;
                    npcGroupID = dbSpawn.NPCGroupID ?? 0;
                    deathVFXID = dbSpawn.DeathVFXID;
                    behaviour = dbSpawn.BehaviourScript;

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
                    location = _spawn.GetRandomSpawnPoint(area);
                }

                spawn.Spawn = _.CreateObject(objectType, resref, location);

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

                if (objectType == OBJECT_TYPE_CREATURE)
                    _spawn.AssignScriptEvents(spawn.Spawn.Object);

                if (!string.IsNullOrWhiteSpace(spawn.SpawnRule))
                {
                    App.ResolveByInterface<ISpawnRule>("SpawnRule." + spawn.SpawnRule, rule =>
                    {
                        rule.Run(spawn.Spawn);
                    });
                }

                App.Resolve<IObjectVisibilityService>(ovs =>
                {
                    ovs.ApplyVisibilityForObject(spawn.Spawn);
                });

                spawn.Timer = 0.0f;
            }
        }
    }
}
