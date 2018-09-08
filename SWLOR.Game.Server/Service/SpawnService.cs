using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
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
        private readonly IResourceService _resource;

        public SpawnService(
            INWScript script,
            IDataContext db,
            IRandomService random,
            IObjectProcessingService processor,
            IResourceService resource,
            AppState state)
        {
            _ = script;
            _db = db;
            _random = random;
            _processor = processor;
            _state = state;
            _resource = resource;
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

                _.DelayCommand(1.0f, () =>
                {
                    SpawnResources(area, areaSpawn);
                });
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

                spawn.Spawn = NWObject.Wrap(_.CreateObject(objectType, resref, location));
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
                _random.RandomFloat(0, 360));
        }

        private void SpawnResources(NWArea area, AreaSpawn areaSpawn)
        {
            var dbArea = _db.Areas.Single(x => x.Resref == area.Resref);

            if (dbArea.ResourceQuality <= 0) return;

            // 1024 size = 32x32
            // 256  size = 16x16
            // 64   size = 8x8
            int size = area.Width * area.Height;
            int normalQualityChance = 20;
            int highQualityChance = 10;
            int veryHighQualityChance = 2;

            int maxSpawns = 0;
            if (size <= 12)
            {
                maxSpawns = 2;
                highQualityChance = 0;
                veryHighQualityChance = 0;
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

            for (int x = 1; x <= maxSpawns; x++)
            {
                int roll = _random.Random(0, 100);
                ResourceQuality quality = ResourceQuality.Low;
                ResourceType resourceType = ResourceType.Ore;
                int tier = dbArea.ResourceQuality;

                if (roll <= veryHighQualityChance)
                {
                    quality = ResourceQuality.VeryHigh;
                }
                else if (roll <= highQualityChance)
                {
                    quality = ResourceQuality.High;
                }
                else if (roll <= normalQualityChance)
                {
                    quality = ResourceQuality.Normal;
                }

                roll = _random.Random(0, 100);
                if (roll <= 30)
                {
                    resourceType = ResourceType.Organic;
                }

                roll = _random.Random(0, 100);
                if (roll <= 3)
                {
                    tier++;
                }

                if (tier > 10) tier = 10;

                string resref = _resource.GetResourcePlaceableResref(resourceType);
                Location location = GetRandomSpawnPoint(area.Resref);
                NWPlaceable plc = NWPlaceable.Wrap(_.CreateObject(OBJECT_TYPE_PLACEABLE, resref, location));
                plc.SetLocalInt("RESOURCE_QUALITY", (int)quality);
                plc.SetLocalInt("RESOURCE_TIER", tier);
                plc.SetLocalInt("RESOURCE_TYPE", (int)resourceType);
                plc.SetLocalInt("RESOURCE_COUNT", _random.Random(3, 10));
                
                ObjectSpawn spawn = new ObjectSpawn(plc, false, 600);
                areaSpawn.Placeables.Add(spawn);
            }
        }

        public IReadOnlyCollection<ObjectSpawn> GetAreaPlaceableSpawns(string areaResref)
        {
            var areaSpawn = _state.AreaSpawns[areaResref];
            return new ReadOnlyCollection<ObjectSpawn>(areaSpawn.Placeables);
        }
        public IReadOnlyCollection<ObjectSpawn> GetAreaCreatureSpawns(string areaResref)
        {
            var areaSpawn = _state.AreaSpawns[areaResref];
            return new ReadOnlyCollection<ObjectSpawn>(areaSpawn.Creatures);
        }
    }
}
