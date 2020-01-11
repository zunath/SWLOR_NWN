using NWN;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Area;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;
using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class AreaService
    {
        private static readonly Dictionary<NWArea, List<AreaWalkmesh>> _walkmeshesByArea = new Dictionary<NWArea, List<AreaWalkmesh>>();
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
                var dbArea = dbAreas.SingleOrDefault(x => x.Resref == area.Resref);
                var action = DatabaseActionType.Set;

                if (dbArea == null)
                {
                    dbArea = new Area
                    {
                        ID = Guid.NewGuid(),
                        Resref = area.Resref
                    };
                    action = DatabaseActionType.Set;
                }

                var width = _.GetAreaSize(AreaProperty.Width, area.Object);
                var height = _.GetAreaSize(AreaProperty.Height, area.Object);
                var northwestLootTableID = (LootTable)area.GetLocalInt("RESOURCE_NORTHWEST_LOOT_TABLE_ID");
                var northeastLootTableID = (LootTable)area.GetLocalInt("RESOURCE_NORTHEAST_LOOT_TABLE_ID");
                var southwestLootTableID = (LootTable)area.GetLocalInt("RESOURCE_SOUTHWEST_LOOT_TABLE_ID");
                var southeastLootTableID = (LootTable)area.GetLocalInt("RESOURCE_SOUTHEAST_LOOT_TABLE_ID");

                dbArea.Name = area.Name;
                dbArea.Tag = area.Tag;
                dbArea.ResourceSpawnTableID = (Spawn)area.GetLocalInt("RESOURCE_SPAWN_TABLE_ID");
                dbArea.Width = width;
                dbArea.Height = height;
                dbArea.PurchasePrice = area.GetLocalInt("PURCHASE_PRICE");
                dbArea.DailyUpkeep = area.GetLocalInt("DAILY_UPKEEP");
                dbArea.NorthwestLootTableID = northwestLootTableID;
                dbArea.NortheastLootTableID = northeastLootTableID;
                dbArea.SouthwestLootTableID = southwestLootTableID;
                dbArea.SoutheastLootTableID = southeastLootTableID;
                dbArea.IsBuildable =
                    (area.GetLocalBoolean("IS_BUILDABLE") == true &&
                    dbArea.Width == 32 &&
                    dbArea.Height == 32 &&
                    dbArea.PurchasePrice > 0 &&
                    dbArea.DailyUpkeep > 0 &&
                    dbArea.NorthwestLootTableID != null &&
                    dbArea.NortheastLootTableID != null &&
                    dbArea.SouthwestLootTableID != null &&
                    dbArea.SoutheastLootTableID != null) ||
                    (area.GetLocalBoolean("IS_BUILDING") == true);
                dbArea.IsActive = true;
                dbArea.AutoSpawnResources = area.GetLocalBoolean("AUTO_SPAWN_RESOURCES") == true;
                dbArea.ResourceQuality = area.GetLocalInt("RESOURCE_QUALITY");
                dbArea.MaxResourceQuality = area.GetLocalInt("RESOURCE_MAX_QUALITY");
                if (dbArea.MaxResourceQuality < dbArea.ResourceQuality)
                    dbArea.MaxResourceQuality = dbArea.ResourceQuality;

                DataService.SubmitDataChange(dbArea, action);
                
                BakeArea(area);
            }
            
        }

        // Area baking process
        // Run through and look for valid locations for later use by the spawn system.
        // Each tile is 10x10 meters. The "step" value in the config table determines how many meters we progress before checking for a valid location.
        private static void BakeArea(NWArea area)
        {
            _walkmeshesByArea[area] = new List<AreaWalkmesh>();

            const float MinDistance = 6.0f;
            var dbArea = DataService.Area.GetByResref(area.Resref);

            int arraySizeX = dbArea.Width * (10 / AreaBakeStep);
            int arraySizeY = dbArea.Height * (10 / AreaBakeStep);

            for (int x = 0; x < arraySizeX; x++)
            {
                for (int y = 0; y < arraySizeY; y++)
                {
                    Location checkLocation = _.Location(area.Object, _.Vector(x * AreaBakeStep, y * AreaBakeStep), 0.0f);
                    int material = _.GetSurfaceMaterial(checkLocation);
                    bool isWalkable = Convert.ToInt32(_.Get2DAString("surfacemat", "Walk", material)) == 1;

                    // Location is not walkable if another object exists nearby.
                    NWObject nearest = (_.GetNearestObjectToLocation(ObjectType.Creature | ObjectType.Door | ObjectType.Placeable | ObjectType.Trigger, checkLocation));
                    float distance = _.GetDistanceBetweenLocations(checkLocation, nearest.Location);
                    if (nearest.IsValid && distance <= MinDistance)
                    {
                        isWalkable = false;
                    }

                    if(isWalkable)
                    {
                        AreaWalkmesh mesh = new AreaWalkmesh()
                        {
                            AreaID = dbArea.ID,
                            LocationX = x * AreaBakeStep,
                            LocationY = y * AreaBakeStep,
                            LocationZ = _.GetGroundHeight(checkLocation)
                        };

                        _walkmeshesByArea[area].Add(mesh);
                    }
                }
            }

            Console.WriteLine("Area walkmesh up to date: " + area.Name);
        }
        
        public static NWArea CreateAreaInstance(NWPlayer owner, string areaResref, string areaName, string entranceWaypointTag)
        {
            string tag = Guid.NewGuid().ToString();
            NWArea instance = _.CreateArea(areaResref, tag, areaName);
            
            instance.SetLocalString("INSTANCE_OWNER", owner.GlobalID.ToString());
            instance.SetLocalString("ORIGINAL_RESREF", areaResref);
            instance.SetLocalBoolean("IS_AREA_INSTANCE", true);
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
                return new NWGameObject();
            }

            instance.SetLocalLocation("INSTANCE_ENTRANCE", entranceWP.Location);
            entranceWP.Destroy(); // Destroy it so we don't get dupes.

            MessageHub.Instance.Publish(new OnAreaInstanceCreated(instance));
            return instance;
        }

        public static void DestroyAreaInstance(NWArea area)
        {
            if (!area.IsInstance) return;

            MessageHub.Instance.Publish(new OnAreaInstanceDestroyed(area));
            _.DestroyArea(area);

        }

        private static void OnAreaEnter()
        {
            NWArea area = NWGameObject.OBJECT_SELF;
            int playerCount = NWNXArea.GetNumberOfPlayersInArea(area);
            if (playerCount > 0)
                _.SetEventScript(area, EventScriptArea.OnHeartbeat, "area_on_hb");
            else
                _.SetEventScript(area, EventScriptArea.OnHeartbeat, string.Empty);
        }

        private static void OnAreaExit()
        {
            NWArea area = NWGameObject.OBJECT_SELF;
            int playerCount = NWNXArea.GetNumberOfPlayersInArea(area);
            if (playerCount > 0)
                _.SetEventScript(area, EventScriptArea.OnHeartbeat, "area_on_hb");
            else
                _.SetEventScript(area, EventScriptArea.OnHeartbeat, string.Empty);
        }

        public static List<AreaWalkmesh> GetAreaWalkmeshes(NWArea area)
        {
            return _walkmeshesByArea[area].ToList();
        }
    }
}
