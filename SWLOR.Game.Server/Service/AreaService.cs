using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.AreaInstance.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class AreaService : IAreaService
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly ISpawnService _spawn;
        
        public AreaService(
            INWScript script,
            IDataService data,
            ISpawnService spawn)
        {
            _ = script;
            _data = data;
            _spawn = spawn;
        }

        public void OnModuleLoad()
        {
            var areas = NWModule.Get().Areas;
            var dbAreas = _data.GetAll<Area>().Where(x => x.IsActive).ToList();
            dbAreas.ForEach(x => x.IsActive = false);

            foreach (var area in areas)
            {
                var dbArea = dbAreas.SingleOrDefault(x => x.Resref == area.Resref);
                var action = DatabaseActionType.Update;

                if (dbArea == null)
                {
                    dbArea = new Area
                    {
                        ID = Guid.NewGuid(),
                        Resref = area.Resref
                    };
                    action = DatabaseActionType.Insert;
                }

                int width = _.GetAreaSize(AREA_WIDTH, area.Object);
                int height = _.GetAreaSize(AREA_HEIGHT, area.Object);
                int northwestLootTableID = area.GetLocalInt("RESOURCE_NORTHWEST_LOOT_TABLE_ID");
                int northeastLootTableID = area.GetLocalInt("RESOURCE_NORTHEAST_LOOT_TABLE_ID");
                int southwestLootTableID = area.GetLocalInt("RESOURCE_SOUTHWEST_LOOT_TABLE_ID");
                int southeastLootTableID = area.GetLocalInt("RESOURCE_SOUTHEAST_LOOT_TABLE_ID");
                
                dbArea.Name = area.Name;
                dbArea.Tag = area.Tag;
                dbArea.ResourceSpawnTableID = area.GetLocalInt("RESOURCE_SPAWN_TABLE_ID");
                dbArea.Width = width;
                dbArea.Height = height;
                dbArea.PurchasePrice = area.GetLocalInt("PURCHASE_PRICE");
                dbArea.DailyUpkeep = area.GetLocalInt("DAILY_UPKEEP");
                dbArea.NorthwestLootTableID = northwestLootTableID > 0 ? northwestLootTableID : new int?();
                dbArea.NortheastLootTableID = northeastLootTableID > 0 ? northeastLootTableID : new int?();
                dbArea.SouthwestLootTableID = southwestLootTableID > 0 ? southwestLootTableID : new int?();
                dbArea.SoutheastLootTableID = southeastLootTableID > 0 ? southeastLootTableID : new int?();
                dbArea.IsBuildable =
                    (area.GetLocalInt("IS_BUILDABLE") == TRUE &&
                    dbArea.Width == 32 &&
                    dbArea.Height == 32 &&
                    dbArea.PurchasePrice > 0 &&
                    dbArea.DailyUpkeep > 0 &&
                    dbArea.NorthwestLootTableID != null &&
                    dbArea.NortheastLootTableID != null &&
                    dbArea.SouthwestLootTableID != null &&
                    dbArea.SoutheastLootTableID != null) ||
                    (area.GetLocalInt("IS_BUILDING") == TRUE);
                dbArea.IsActive = true;
                dbArea.AutoSpawnResources = area.GetLocalInt("AUTO_SPAWN_RESOURCES") == TRUE;
                dbArea.ResourceQuality = area.GetLocalInt("RESOURCE_QUALITY");
                dbArea.MaxResourceQuality = area.GetLocalInt("RESOURCE_MAX_QUALITY");
                if (dbArea.MaxResourceQuality < dbArea.ResourceQuality)
                    dbArea.MaxResourceQuality = dbArea.ResourceQuality;

                _data.SubmitDataChange(dbArea, action);
            }
            
            string arg = Environment.GetEnvironmentVariable("AREA_BAKING_ENABLED");
            bool bakingEnabled =  arg == null || Convert.ToBoolean(arg);
            
            if(bakingEnabled)
            {
                BakeAreas();
            }
            else
            {
                Console.WriteLine("WARNING: Area baking has been disabled. You may encounter errors during normal operations. This should only be disabled for debugging purposes. Please shut down the server and set the AREA_BAKING_ENABLED argument to true for all other scenarios.");
            }

            // Cache both areas and area walkmeshes now that they're built, if they aren't already.
            _data.GetAll<Area>();
            _data.GetAll<AreaWalkmesh>();
        }

        // Area baking process
        // Check if walkmesh matches what's in the database.
        // If it doesn't, run through and look for valid locations for later use by the spawn system.
        // Each tile is 10x10 meters. The "step" value in the config table determines how many meters we progress before checking for a valid location.
        // If you adjust this to get finer precision your database may explode with a ton of records. I chose a value that got me the 
        // accuracy I wanted, without too much overhead. Your mileage may vary.
        private void BakeAreas()
        {
            var config = _data.GetAll<ServerConfiguration>().First();
            int Step = config.AreaBakeStep;
            const float MinDistance = 6.0f;

            foreach (var area in NWModule.Get().Areas)
            {
                var dbArea = _data.Single<Area>(x => x.Resref == area.Resref);

                int arraySizeX = dbArea.Width * (10 / Step);
                int arraySizeY = dbArea.Height * (10 / Step);
                Tuple<bool, float>[,] locations = new Tuple<bool, float>[arraySizeX, arraySizeY];
                string walkmesh = string.Empty;

                for (int x = 0; x < arraySizeX; x++)
                {
                    for (int y = 0; y < arraySizeY; y++)
                    {
                        Location checkLocation = _.Location(area.Object, _.Vector(x * Step, y * Step), 0.0f);
                        int material = _.GetSurfaceMaterial(checkLocation);
                        bool isWalkable = Convert.ToInt32(_.Get2DAString("surfacemat", "Walk", material)) == 1;

                        // Location is not walkable if another object exists nearby.
                        NWObject nearest = (_.GetNearestObjectToLocation(OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR | OBJECT_TYPE_PLACEABLE | OBJECT_TYPE_TRIGGER, checkLocation));
                        float distance = _.GetDistanceBetweenLocations(checkLocation, nearest.Location);
                        if (nearest.IsValid && distance <= MinDistance)
                        {
                            isWalkable = false;
                        }


                        locations[x, y] = new Tuple<bool, float>(isWalkable, _.GetGroundHeight(checkLocation));

                        walkmesh += isWalkable ? "1" : "0";
                    }
                }

                if (dbArea.Walkmesh != walkmesh)
                {
                    dbArea.Walkmesh = walkmesh;
                    dbArea.DateLastBaked = DateTime.UtcNow;
                    _data.SubmitDataChange(dbArea, DatabaseActionType.Update);

                    Console.WriteLine("Baking area because its walkmesh has changed since last run: " + area.Name);

                    var walkmeshes = _data.Where<AreaWalkmesh>(x => x.AreaID == dbArea.ID).ToList();
                    for(int x = walkmeshes.Count-1; x >= 0; x--)
                    {
                        var mesh = walkmeshes.ElementAt(x);
                        _data.SubmitDataChange(mesh, DatabaseActionType.Delete);
                    }
                    
                    Console.WriteLine("Cleared old walkmesh. Adding new one now.");
                    
                    int records = 0;
                    for (int x = 0; x < arraySizeX; x++)
                    {
                        for (int y = 0; y < arraySizeY; y++)
                        {
                            // Ignore any points in the area that aren't walkable.
                            bool isWalkable = locations[x, y].Item1;
                            if (!isWalkable) continue;

                            float z = locations[x, y].Item2;
                            
                            AreaWalkmesh mesh = new AreaWalkmesh()
                            {
                                AreaID = dbArea.ID,
                                LocationX = x * Step,
                                LocationY = y * Step,
                                LocationZ = z
                            };

                            _data.SubmitDataChange(mesh, DatabaseActionType.Insert);

                            records++;
                        }
                    }

                    Console.WriteLine("Saved " + records + " records.");
                }
                Console.WriteLine("Area walkmesh up to date: " + area.Name);

            }
        }
        
        public NWArea CreateAreaInstance(NWPlayer owner, string areaResref, string areaName, string entranceWaypointTag)
        {
            string tag = Guid.NewGuid().ToString();
            NWArea instance = _.CreateArea(areaResref, tag, areaName);
            
            instance.SetLocalString("INSTANCE_OWNER", owner.GlobalID.ToString());
            instance.SetLocalString("ORIGINAL_RESREF", areaResref);
            instance.SetLocalInt("IS_AREA_INSTANCE", TRUE);
            instance.Data["BASE_SERVICE_STRUCTURES"] = new List<AreaStructure>();

            NWObject searchByObject = _.GetFirstObjectInArea(instance);
            NWObject entranceWP;

            if (searchByObject.Tag == entranceWaypointTag)
                entranceWP = searchByObject;
            else
                entranceWP = _.GetNearestObjectByTag(entranceWaypointTag, searchByObject);
            
            if (!entranceWP.IsValid)
            {
                owner.SendMessage("ERROR: Couldn't locate entrance waypoint with tag '" + entranceWaypointTag + "'. Notify an admin.");
                return new NWN.Object();
            }

            instance.SetLocalLocation("INSTANCE_ENTRANCE", entranceWP.Location);
            entranceWP.Destroy(); // Destroy it so we don't get dupes.

            _spawn.InitializeAreaSpawns(instance);
            
            string spawnScript = instance.GetLocalString("INSTANCE_ON_SPAWN");
            if (!string.IsNullOrWhiteSpace(spawnScript))
            {
                App.ResolveByInterface<IAreaInstance>("AreaInstance." + spawnScript, s =>
                {
                    s.Run(instance);
                });
            }

            return instance;
        }

        public void DestroyAreaInstance(NWArea area)
        {
            if (!area.IsInstance) return;

            if (AppCache.AreaSpawns.ContainsKey(area))
            {
                AppCache.AreaSpawns.Remove(area);
            }

            _.DestroyArea(area);

        }

    }
}
