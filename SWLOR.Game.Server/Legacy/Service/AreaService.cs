using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Area;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.Area;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.Event.SWLOR;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class AreaService
    {
        private static readonly Dictionary<uint, List<AreaWalkmesh>> _walkmeshesByArea = new Dictionary<uint, List<AreaWalkmesh>>();
        private const int AreaBakeStep = 5;

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
            MessageHub.Instance.Subscribe<OnAreaEnter>(message => OnAreaEnter());
            MessageHub.Instance.Subscribe<OnAreaExit>(message => OnAreaExit());

            MessageHub.Instance.Subscribe<OnRequestCacheStats>(message =>
            {
                message.Player.SendMessage("Walkmesh Areas: " + _walkmeshesByArea.Count);
                message.Player.SendMessage("Walkmeshes: " + _walkmeshesByArea.Values.Count);
            });
        }

        private static void OnModuleLoad()
        {
            var areas = NWModule.Get().Areas;
            var dbAreas = DataService.Area.GetAll().Where(x => x.IsActive).ToList();
            dbAreas.ForEach(x => x.IsActive = false);

            foreach (var area in areas)
            {
                var areaResref = GetResRef(area);
                var dbArea = dbAreas.SingleOrDefault(x => x.Resref == areaResref);
                var action = DatabaseActionType.Update;

                if (dbArea == null)
                {
                    dbArea = new Area
                    {
                        ID = Guid.NewGuid(),
                        Resref = areaResref
                    };
                    action = DatabaseActionType.Insert;
                }

                var width = GetAreaSize(Dimension.Width, area);
                var height = GetAreaSize(Dimension.Height, area);
                var northwestLootTableID = GetLocalInt(area, "RESOURCE_NORTHWEST_LOOT_TABLE_ID");
                var northeastLootTableID = GetLocalInt(area, "RESOURCE_NORTHEAST_LOOT_TABLE_ID");
                var southwestLootTableID = GetLocalInt(area, "RESOURCE_SOUTHWEST_LOOT_TABLE_ID");
                var southeastLootTableID = GetLocalInt(area, "RESOURCE_SOUTHEAST_LOOT_TABLE_ID");

                // If the loot tables don't exist, don't assign them to this DB entry or we'll get foreign key reference errors.
                if (DataService.LootTable.GetByIDOrDefault(northwestLootTableID) == null)
                    northwestLootTableID = 0;
                if (DataService.LootTable.GetByIDOrDefault(northeastLootTableID) == null)
                    northeastLootTableID = 0;
                if (DataService.LootTable.GetByIDOrDefault(southwestLootTableID) == null)
                    southwestLootTableID = 0;
                if (DataService.LootTable.GetByIDOrDefault(southeastLootTableID) == null)
                    southeastLootTableID = 0;

                dbArea.Name = GetName(area);
                dbArea.Tag = GetTag(area);
                dbArea.ResourceSpawnTableID = GetLocalInt(area, "RESOURCE_SPAWN_TABLE_ID");
                dbArea.Width = width;
                dbArea.Height = height;
                dbArea.PurchasePrice = GetLocalInt(area, "PURCHASE_PRICE");
                dbArea.DailyUpkeep = GetLocalInt(area, "DAILY_UPKEEP");
                dbArea.NorthwestLootTableID = northwestLootTableID > 0 ? northwestLootTableID : new int?();
                dbArea.NortheastLootTableID = northeastLootTableID > 0 ? northeastLootTableID : new int?();
                dbArea.SouthwestLootTableID = southwestLootTableID > 0 ? southwestLootTableID : new int?();
                dbArea.SoutheastLootTableID = southeastLootTableID > 0 ? southeastLootTableID : new int?();
                dbArea.IsBuildable =
                    (GetLocalBool(area, "IS_BUILDABLE") == true &&
                    dbArea.Width == 32 &&
                    dbArea.Height == 32 &&
                    dbArea.PurchasePrice > 0 &&
                    dbArea.DailyUpkeep > 0 &&
                    dbArea.NorthwestLootTableID != null &&
                    dbArea.NortheastLootTableID != null &&
                    dbArea.SouthwestLootTableID != null &&
                    dbArea.SoutheastLootTableID != null) ||
                    GetLocalBool(area, "IS_BUILDING");
                dbArea.IsActive = true;
                dbArea.AutoSpawnResources = GetLocalBool(area, "AUTO_SPAWN_RESOURCES") == true;
                dbArea.ResourceQuality = GetLocalInt(area, "RESOURCE_QUALITY");
                dbArea.MaxResourceQuality = GetLocalInt(area, "RESOURCE_MAX_QUALITY");
                if (dbArea.MaxResourceQuality < dbArea.ResourceQuality)
                    dbArea.MaxResourceQuality = dbArea.ResourceQuality;

                DataService.SubmitDataChange(dbArea, action);
                
                BakeArea(area);
            }
            
        }

        // Area baking process
        // Run through and look for valid locations for later use by the spawn system.
        // Each tile is 10x10 meters. The "step" value in the config table determines how many meters we progress before checking for a valid location.
        private static void BakeArea(uint area)
        {
            _walkmeshesByArea[area] = new List<AreaWalkmesh>();

            const float MinDistance = 6.0f;
            var areaResref = GetResRef(area);
            var dbArea = DataService.Area.GetByResref(areaResref);

            var arraySizeX = dbArea.Width * (10 / AreaBakeStep);
            var arraySizeY = dbArea.Height * (10 / AreaBakeStep);

            for (var x = 0; x < arraySizeX; x++)
            {
                for (var y = 0; y < arraySizeY; y++)
                {
                    var checkLocation = Location(area, Vector3(x * AreaBakeStep, y * AreaBakeStep), 0.0f);
                    var material = GetSurfaceMaterial(checkLocation);
                    var isWalkable = Convert.ToInt32(Get2DAString("surfacemat", "Walk", material)) == 1;

                    // Location is not walkable if another object exists nearby.
                    NWObject nearest = (GetNearestObjectToLocation(checkLocation, ObjectType.Creature | ObjectType.Door | ObjectType.Placeable | ObjectType.Trigger));
                    var distance = GetDistanceBetweenLocations(checkLocation, nearest.Location);
                    if (nearest.IsValid && distance <= MinDistance)
                    {
                        isWalkable = false;
                    }

                    if(isWalkable)
                    {
                        var mesh = new AreaWalkmesh()
                        {
                            AreaID = dbArea.ID,
                            LocationX = x * AreaBakeStep,
                            LocationY = y * AreaBakeStep,
                            LocationZ = GetGroundHeight(checkLocation)
                        };

                        _walkmeshesByArea[area].Add(mesh);
                    }
                }
            }

            Console.WriteLine("Area walkmesh up to date: " + GetName(area));
        }
        
        public static uint CreateAreaInstance(NWPlayer owner, string areaResref, string areaName, string entranceWaypointTag)
        {
            var tag = Guid.NewGuid().ToString();
            var instance = CreateArea(areaResref, tag, areaName);
            
            SetLocalString(instance, "INSTANCE_OWNER", owner.GlobalID.ToString());
            SetLocalString(instance, "ORIGINAL_RESREF", areaResref);
            SetLocalBool(instance, "IS_AREA_INSTANCE", true);

            NWObject searchByObject = GetFirstObjectInArea(instance);
            NWObject entranceWP;

            if (searchByObject.Tag == entranceWaypointTag)
                entranceWP = searchByObject;
            else
                entranceWP = GetNearestObjectByTag(entranceWaypointTag, searchByObject);
            
            if (!entranceWP.IsValid)
            {
                owner.SendMessage("ERROR: Couldn't locate entrance waypoint with tag '" + entranceWaypointTag + "'. Notify an admin.");
                return OBJECT_INVALID;
            }

            SetLocalLocation(instance, "INSTANCE_ENTRANCE", entranceWP.Location);
            entranceWP.Destroy(); // Destroy it so we don't get dupes.

            MessageHub.Instance.Publish(new OnAreaInstanceCreated(instance));
            return instance;
        }

        public static void DestroyAreaInstance(uint area)
        {
            if (!IsAreaInstance(area)) return;

            MessageHub.Instance.Publish(new OnAreaInstanceDestroyed(area));
            DestroyArea(area);

        }

        private static void OnAreaEnter()
        {
            var area = OBJECT_SELF;
            var playerCount = Core.NWNX.Area.GetNumberOfPlayersInArea(area);
            if (playerCount > 0)
                SetEventScript(area, EventScript.Area_OnHeartbeat, "area_on_hb");
            else
                SetEventScript(area, EventScript.Area_OnHeartbeat, string.Empty);
        }

        private static void OnAreaExit()
        {
            var area = OBJECT_SELF;
            var playerCount = Core.NWNX.Area.GetNumberOfPlayersInArea(area);
            if (playerCount > 0)
                SetEventScript(area, EventScript.Area_OnHeartbeat, "area_on_hb");
            else
                SetEventScript(area, EventScript.Area_OnHeartbeat, string.Empty);
        }

        public static List<AreaWalkmesh> GetAreaWalkmeshes(uint area)
        {
            return _walkmeshesByArea[area].ToList();
        }

        /// <summary>
        /// Returns true if an area is an instance. Otherwise returns false.
        /// </summary>
        /// <param name="area">The area to check</param>
        /// <returns>true if instance, false otherwise</returns>
        public static bool IsAreaInstance(uint area)
        {
            return GetLocalBool(area, "IS_AREA_INSTANCE");
        }
    }
}
