using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class AreaService : IAreaService
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IDataContext _bakeDB;

        public AreaService(
            INWScript script,
            IDataContext db,
            IDataContext bakeDB)
        {
            _ = script;
            _db = db;
            _bakeDB = bakeDB;

            // Disable stuff that slows down bulk inserts.
            _bakeDB.Configuration.AutoDetectChangesEnabled = false;
            _bakeDB.Configuration.ValidateOnSaveEnabled = false;
        }

        public void OnModuleLoad()
        {
            var areas = NWModule.Get().Areas;
            var dbAreas = _db.Areas.Where(x => x.IsActive).ToList();
            dbAreas.ForEach(x => x.IsActive = false);

            foreach (var area in areas)
            {
                var dbArea = dbAreas.SingleOrDefault(x => x.Resref == area.Resref);

                if (dbArea == null)
                {
                    dbArea = new Area
                    {
                        AreaID = Guid.NewGuid().ToString("N"),
                        Resref = area.Resref
                    };
                }

                int width = _.GetAreaSize(AREA_WIDTH, area.Object);
                int height = _.GetAreaSize(AREA_HEIGHT, area.Object);

                dbArea.Name = area.Name;
                dbArea.Tag = area.Tag;
                dbArea.ResourceSpawnTableID = area.GetLocalInt("RESOURCE_SPAWN_TABLE_ID");
                dbArea.Width = width;
                dbArea.Height = height;
                dbArea.PurchasePrice = area.GetLocalInt("PURCHASE_PRICE");
                dbArea.DailyUpkeep = area.GetLocalInt("DAILY_UPKEEP");
                dbArea.IsBuildable = 
                    (area.GetLocalInt("IS_BUILDABLE") == TRUE && 
                    dbArea.Width == 32 && 
                    dbArea.Height == 32 &&
                    dbArea.PurchasePrice > 0 &&
                    dbArea.DailyUpkeep > 0) || 
                    (area.GetLocalInt("IS_BUILDING") == TRUE);
                dbArea.IsActive = true;
                dbArea.AutoSpawnResources = area.GetLocalInt("AUTO_SPAWN_RESOURCES") == TRUE;
                dbArea.ResourceQuality = area.GetLocalInt("RESOURCE_QUALITY");

                _db.Areas.AddOrUpdate(dbArea);
            }
            _db.SaveChanges();

            BakeAreas();
        }

        // Area baking process
        // Check if walkmesh matches what's in the database.
        // If it doesn't, run through and look for valid locations for later use by the spawn system.
        // Each tile is 10x10 meters. The "step" value in the config table determines how many meters we progress before checking for a valid location.
        // If you adjust this to get finer precision your database may explode with a ton of records. I chose a value that got me the 
        // accuracy I wanted, without too much overhead. Your mileage may vary.
        private void BakeAreas()
        {
            var config = _db.ServerConfigurations.Single();
            int Step = config.AreaBakeStep;
            const float MinDistance = 6.0f;

            foreach (var area in NWModule.Get().Areas)
            {
                var dbArea = _bakeDB.Areas.Single(x => x.Resref == area.Resref);

                int arraySizeX = dbArea.Width * (10 / Step);
                int arraySizeY = dbArea.Height * (10 / Step);
                Tuple<bool, float>[,] locations = new Tuple<bool,float>[arraySizeX, arraySizeY];
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
                    ((DbContext) _bakeDB).Entry(dbArea).State = EntityState.Modified; // Manually mark as changed since AutoDetectChanges is disabled for this context
                    _bakeDB.SaveChanges();

                    Console.WriteLine("Baking area because its walkmesh has changed since last run: " + area.Name);

                    _bakeDB.StoredProcedure("DeleteAreaWalkmeshes",
                        new SqlParameter("AreaID", dbArea.AreaID));

                    Console.WriteLine("Cleared old walkmesh. Adding new one now.");

                    const int BatchSize = 5000;
                    int batchRecords = 0;
                    string sql = string.Empty;

                    for (int x = 0; x < arraySizeX; x++)
                    {
                        for (int y = 0; y < arraySizeY; y++)
                        {
                            // Ignore any points in the area that aren't walkable.
                            bool isWalkable = locations[x, y].Item1;
                            if (!isWalkable) continue;

                            float z = locations[x, y].Item2;
                            
                            // Raw SQL inserts are quicker than using EF.
                            sql += $@"INSERT INTO dbo.AreaWalkmesh(AreaID, LocationX, LocationY, LocationZ) VALUES('{dbArea.AreaID}', {x * Step}, {y * Step}, {z});";
                            
                            batchRecords++;
                            if (batchRecords >= BatchSize)
                            {
                                Console.WriteLine("Saving " + BatchSize + " records...");
                                _bakeDB.Database.ExecuteSqlCommand(sql);
                                sql = string.Empty;
                                batchRecords = 0;
                            }

                        }
                    }

                    Console.WriteLine("Saving " + batchRecords + " records...");
                    _bakeDB.Database.ExecuteSqlCommand(sql);
                    Console.WriteLine("Finished baking area: " + area.Name);
                }

            }
        }


        public NWArea CreateAreaInstance(string areaResref, string areaName)
        {
            string tag = Guid.NewGuid().ToString("N");
            NWArea area = (_.CreateArea(areaResref, tag, areaName));

            area.SetLocalInt("IS_AREA_INSTANCE", TRUE);
            area.Data["BASE_SERVICE_STRUCTURES"] = new List<AreaStructure>();
            
            return area;
        }
    }
}
