using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class SpawnService : ISpawnService
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IObjectProcessingService _processor;
        private readonly AppState _state;
        private readonly IRandomService _random;

        public SpawnService(
            INWScript script,
            IDataContext db,
            IRandomService random,
            IObjectProcessingService processor,
            AppState state)
        {
            _ = script;
            _db = db;
            _random = random;
            _processor = processor;
            _state = state;
        }

        public void OnModuleLoad()
        {
            InitializeSpawns();
            _processor.RegisterProcessingEvent(Process);
        }

        private void InitializeSpawns()
        {
            foreach (var area in NWModule.Get().Areas)
            {
                var areaSpawn = new AreaSpawn();

                // Check for manually placed spawns
                NWObject obj = NWObject.Wrap(_.GetFirstObjectInArea(area.Object));
                while (obj.IsValid)
                {
                    bool isSpawn = obj.ObjectType == OBJECT_TYPE_WAYPOINT && obj.GetLocalInt("IS_SPAWN") == TRUE;

                    if (isSpawn)
                    {
                        int spawnType = obj.GetLocalInt("SPAWN_TYPE");
                        int objectType = spawnType == 0 || spawnType == OBJECT_TYPE_CREATURE ? OBJECT_TYPE_CREATURE : spawnType;
                        int spawnTableID = obj.GetLocalInt("SPAWN_TABLE_ID");
                        string spawnResref = obj.GetLocalString("SPAWN_RESREF");
                        float respawnTime = obj.GetLocalFloat("SPAWN_RESPAWN_SECONDS");
                        bool useResref = true;

                        // No resref specified but a table was, look in the database for a random record.
                        if (string.IsNullOrWhiteSpace(spawnResref) && spawnTableID > 0)
                        {
                            // Pick a random record.
                            var dbSpawn = _db.SpawnObjects.Where(x => x.SpawnID == spawnTableID)
                                .OrderBy(o => Guid.NewGuid()).First();
                            if (dbSpawn != null)
                            {
                                spawnResref = dbSpawn.Resref;
                                useResref = false;
                            }
                        }

                        // If we found a resref, spawn the object and add it to the cache.
                        if (!string.IsNullOrWhiteSpace(spawnResref))
                        {
                            // Delay the creation so that the iteration through the area doesn't get thrown off by new entries.
                            Location location = obj.Location;
                            _.DelayCommand(0.5f, () =>
                            {
                                var creature = NWCreature.Wrap(_.CreateObject(objectType, spawnResref, location));
                                var newSpawn = new ObjectSpawn(creature, true, respawnTime);
                                if (useResref)
                                {
                                    newSpawn.Resref = spawnResref;
                                }
                                else
                                {
                                    newSpawn.SpawnTableID = spawnTableID;
                                }

                                if (objectType == OBJECT_TYPE_CREATURE)
                                {
                                    areaSpawn.Creatures.Add(newSpawn);
                                }
                                else if (objectType == OBJECT_TYPE_PLACEABLE)
                                {
                                    areaSpawn.Placeables.Add(newSpawn);
                                }
                            });
                            
                        }
                         
                        // Destroy the waypoint to conserve resources.
                        obj.Destroy();
                    }

                    obj = NWObject.Wrap(_.GetNextObjectInArea(area.Object));
                }

                _state.AreaSpawns.Add(area.Resref, areaSpawn);
            }
        }

        private void Process()
        {
            var spawns = _state.AreaSpawns;
            
            foreach (var spawn in spawns)
            {
                string areaResref = spawn.Key;
                AreaSpawn areaSpawn = spawn.Value;

                foreach (var plc in areaSpawn.Placeables)
                {
                    ProcessSpawn(plc, OBJECT_TYPE_PLACEABLE, areaResref);
                }

                foreach (var creature in areaSpawn.Creatures)
                {
                    ProcessSpawn(creature, OBJECT_TYPE_CREATURE, areaResref);
                }
            }
        }

        private void ProcessSpawn(ObjectSpawn spawn, int objectType, string areaResref)
        {
            // Don't process anything that's valid.
            if (spawn.Spawn.IsValid) return;
            spawn.Timer += _processor.ProcessingTickInterval;

            // Time to respawn!
            if (spawn.Timer >= spawn.RespawnTime)
            {
                string resref = spawn.Resref;
                Location location = spawn.IsStaticSpawnPoint ? spawn.SpawnLocation : null;

                if (string.IsNullOrWhiteSpace(resref))
                {
                    var dbSpawn = _db.SpawnObjects.Where(x => x.SpawnID == spawn.SpawnTableID)
                        .OrderBy(o => Guid.NewGuid()).First();
                    resref = dbSpawn.Resref;
                }

                if (location == null)
                {
                    location = GetRandomSpawnPoint(areaResref);
                }

                _.CreateObject(objectType, resref, location);

                spawn.Timer = 0.0f;
            }
        }

        private Location GetRandomSpawnPoint(string areaResref)
        {
            var area = NWModule.Get().Areas.Single(x => x.Resref == areaResref);
            var spawnPoint = _db.AreaWalkmeshes
                .Where(x => x.Area.Resref == areaResref)
                .OrderBy(o => Guid.NewGuid()).First();

            return _.Location(area.Object,
                _.Vector((float) spawnPoint.LocationX, (float) spawnPoint.LocationY, (float) spawnPoint.LocationZ),
                _random.RandomFloat() * 100);
        }
    }
}
