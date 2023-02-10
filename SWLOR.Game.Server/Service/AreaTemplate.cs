using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Core.NWScript.Enum.Area;

namespace SWLOR.Game.Server.Service
{
    public class AreaTemplate
    {
        public static Dictionary<string, uint> TemplateAreaByResRef { get; } = new();
        private static Dictionary<uint, List<uint>> TemplateAreaCustomObjectsByArea { get; } = new();
        private static Dictionary<int, Dictionary<int, string>> PlaceableAppearanceByPage { get; } = new();
        private static Dictionary<int, Dictionary<int, string>> CreatureAppearanceByPage { get; } = new();

        public static int TotalCountAppearancePlaceables { get; set; }
        public static int TotalCountAppearanceCreatures { get; set; }

        public const int PageSize = 50;

        public static int GetPlaceableAppearancePageCount()
        {
            return PlaceableAppearanceByPage.Count;
        }

        public static int GetCreatureAppearancePageCount()
        {
            return CreatureAppearanceByPage.Count;
        }

        [NWNEventHandler("mod_cache")]
        public static void CacheAppearances()
        {
            TotalCountAppearancePlaceables = 0;
            TotalCountAppearanceCreatures = 0;            

            var rowCount = UtilPlugin.Get2DARowCount("placeables");
            var pageNumber = 0;
            var actualRowCount = 0;

            for (int row = 0; row < rowCount; row++)
            {
                var entry = Get2DAString("placeables", "Label", row);

                if (entry != "****" && !string.IsNullOrWhiteSpace(entry))
                {
                    if (actualRowCount > 0 && actualRowCount % PageSize == 0) pageNumber++;

                    if (!PlaceableAppearanceByPage.ContainsKey(pageNumber))
                        PlaceableAppearanceByPage[pageNumber] = new Dictionary<int, string>();

                    PlaceableAppearanceByPage[pageNumber].Add(row, entry);
                    actualRowCount++;                 
                }
            }
            TotalCountAppearancePlaceables = actualRowCount;
            Console.WriteLine($"Loaded {PlaceableAppearanceByPage.Count} placeable appearance pages ({AreaTemplate.PageSize} per page)  into cache.");

            rowCount = UtilPlugin.Get2DARowCount("appearance");
            pageNumber = 0;
            actualRowCount = 0;

            for (int row = 0; row < rowCount; row++)
            {
                var entry = Get2DAString("appearance", "Label", row);

                if (entry != "****" && !string.IsNullOrWhiteSpace(entry))
                {
                    if (actualRowCount > 0 && actualRowCount % PageSize == 0) pageNumber++;

                    if (!CreatureAppearanceByPage.ContainsKey(pageNumber))
                        CreatureAppearanceByPage[pageNumber] = new Dictionary<int, string>();

                    CreatureAppearanceByPage[pageNumber].Add(row, entry);
                    actualRowCount++;
                }
            }
            TotalCountAppearanceCreatures = actualRowCount;
            Console.WriteLine($"Loaded {CreatureAppearanceByPage.Count} creature appearance pages ({AreaTemplate.PageSize} per page)  into cache.");
        }

        [NWNEventHandler("mod_exit")]
        public static void OnModuleExit()
        {
            //return;

            // For some reason I can't determine, the below is not actually working.
            // I.E. The visual effect is not removed.
            // The intent is to just have the OnClose GUI binding for this window called
            // so that selected Object Highlight effect is cleaned up.
            var player = GetExitingObject();
            Console.WriteLine("Toggling Area Manager window before client disconnect. Name = " + GetName(player));
            Gui.TogglePlayerWindow(player, GuiWindowType.AreaManager);
            Gui.CloseWindow(player, GuiWindowType.AreaManager, player);
            Console.WriteLine("Area Manager window closed.");                     
        }

        [NWNEventHandler("mod_load")]
        public static void OnModuleLoad()
        {
            LoadTemplateAreas();
            LoadTemplateAreaObjects();
        }

        public static Dictionary<int, string> GetPlaceableAppearances(int pageNumber)
        {
            return PlaceableAppearanceByPage[pageNumber];
        }
        public static Dictionary<int, string> GetCreatureAppearances(int pageNumber)
        {
            return CreatureAppearanceByPage[pageNumber];
        }
        public static bool GetIsTemplateArea(uint area)
        {
            return GetLocalBool(area, "IS_TEMPLATE_AREA");
        }

        /// <summary>
        /// Retrieves an area by its resref. If the area does not exist, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="resref">The resref to use for the search.</param>
        /// <returns>The area ID or OBJECT_INVALID if area does not exist.</returns>
        public static uint GetTemplateAreaByResref(string resref)
        {
            if (!TemplateAreaByResRef.ContainsKey(resref))
                return OBJECT_INVALID;

            return TemplateAreaByResRef[resref];
        }

        public static void LoadTemplateAreas()
        {
            Console.WriteLine("Loading Area Templates, please wait...");

            var areaDataQuery = new DBQuery<TemplateArea>();
            var areaDataCount = DB.SearchCount(areaDataQuery);
            var areaDatas = DB.Search(areaDataQuery
                .AddPaging((int)areaDataCount, 0))
                .ToList();

            foreach (var templateArea in areaDatas)
            {
                Console.WriteLine($"Loaded Template Area Name: {templateArea.TemplateAreaName}.");
                Console.WriteLine($"Loaded Template Area Data: {templateArea.TemplateAreaData}.");
                Console.WriteLine($"Loaded Template Area ResRef: {templateArea.TemplateAreaResRef}.");
                Console.WriteLine($"Loaded Template Area Tag: {templateArea.TemplateAreaTag}.");

                var createdArea = CreateArea(templateArea.TemplateAreaResRef, templateArea.TemplateAreaTag, templateArea.TemplateAreaName);
                SetLocalBool(createdArea, "IS_TEMPLATE_AREA", true);

                // This deletes all of the creatures, waypoints, and transitions. Since many of our transitions are
                // placeables with the DESTINATION tag, the GetObjectType(x) == ObjectType.Placeable && GetLocalBool(x, "DESTINATION")) line
                // deletes placeables that serve as transitions, which would solve bugs like the placeable teleport
                // in Jim's Cantina surviving to the template area and teleporting people to weird places. 
                for (var x = GetFirstObjectInArea(createdArea); GetIsObjectValid(x); x = GetNextObjectInArea(createdArea))
                {
                    var isTeleportPlaceable = GetLocalString(x, "DESTINATION");

                    if (GetObjectType(x) == ObjectType.Creature ||
                        GetObjectType(x) == ObjectType.Waypoint ||
                        GetObjectType(x) == ObjectType.Trigger ||
                        (GetObjectType(x) == ObjectType.Placeable && (isTeleportPlaceable != "")))
                    {
                        DestroyObject(x);
                    }
                }
            }
            Console.WriteLine($"Loaded {areaDataCount} template areas.");
        }

        /// <summary>
        /// This loads objects for the Template Areas upon module load.
        /// </summary>
        public static void LoadTemplateAreaObjects()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                if (GetIsTemplateArea(area))
                {
                    var query = new DBQuery<AreaTemplateObject>()
                        .AddFieldSearch(nameof(AreaTemplateObject.AreaTag), GetTag(area), false)
                        .OrderBy(nameof(AreaTemplateObject.ObjectName));
                    var areaTemplates = DB.Search(query)
                        .ToList();

                    foreach (var x in areaTemplates)
                    {
                        var deserialized = ObjectPlugin.Deserialize(x.ObjectData);
                        var position = Vector3(x.LocationX, x.LocationY, x.LocationZ);
                        var location = Location(area, position, x.LocationOrientation);
                        var newObject = CopyObject(deserialized, location);

                        if (!TemplateAreaCustomObjectsByArea.ContainsKey(area))
                            TemplateAreaCustomObjectsByArea[area] = new List<uint>();

                        if (!TemplateAreaCustomObjectsByArea[area].Contains(newObject))
                        {
                            TemplateAreaCustomObjectsByArea[area].Add(newObject);
                        }

                        SetLocalString(newObject, "DBID", x.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves list of template areas only.
        /// </summary>
        /// <param> </param>
        /// <returns>TemplateAreasByResref cache.</returns>
        public static Dictionary<string, uint> GetTemplateAreas()
        {
            return TemplateAreaByResRef;
        }

        /// <summary>
        /// Retrieves all of the objects created by DM's in the currently in the specified template area.
        /// If no objects are in the area, an empty list will returned.
        /// </summary>
        /// <param name="area">The area to search by.</param>
        /// <returns>A list of player objects</returns>
        public static List<uint> GetTemplateAreaCustomObjectsByArea(uint area)
        {
            if (!TemplateAreaCustomObjectsByArea.ContainsKey(area))
                return new List<uint>();

            return TemplateAreaCustomObjectsByArea[area].ToList();
        }

        public static void RemoveTemplateAreaCustomObjectByArea(uint area, uint customObject)
        {
            if (!TemplateAreaCustomObjectsByArea.ContainsKey(area))
                    TemplateAreaCustomObjectsByArea[area] = new List<uint>();

            if (TemplateAreaCustomObjectsByArea[area].Contains(customObject))
                TemplateAreaCustomObjectsByArea[area].Remove(customObject);
        }

        /// <summary>
        /// When a DM spawns an object in the template area, add it to the cache.
        /// </summary>
        [NWNEventHandler("dm_spwnobj_aft")]
        public static void DmSpawnObjectAfter()
        {
            var area = StringToObject(EventsPlugin.GetEventData("AREA"));
            var spawn = StringToObject(EventsPlugin.GetEventData("OBJECT"));
            if (GetIsTemplateArea(area))
            {
                if (!TemplateAreaCustomObjectsByArea.ContainsKey(area))
                    TemplateAreaCustomObjectsByArea[area] = new List<uint>();

                if (!TemplateAreaCustomObjectsByArea[area].Contains(spawn))
                {
                    TemplateAreaCustomObjectsByArea[area].Add(spawn);

                    var areaTemplate = new AreaTemplateObject
                    {
                        AreaResref = GetResRef(area),
                        AreaTag = GetTag(area),
                        ObjectName = GetName(spawn),
                        ObjectData = ObjectPlugin.Serialize(spawn),
                        LocationX = GetPosition(spawn).X,
                        LocationY = GetPosition(spawn).Y,
                        LocationZ = GetPosition(spawn).Z,
                        LocationOrientation = GetFacing(spawn)
                    };
                    
                    var areaName = GetName(area);
                    var objectName = GetName(spawn);

                    DB.Set<AreaTemplateObject>(areaTemplate);
                    SetLocalString(spawn, "DBID", areaTemplate.Id);
                };                   
            }
        }

        // this is in case DM's adjust objects positions or other attributes and want to resave the changes.
        public static void ResaveAllAreaTemplateObjectsByArea(uint area)
        {
            if (!GetIsTemplateArea(area))
                return;

            foreach(var _customObject in TemplateAreaCustomObjectsByArea[area])
            {                
                var customObject = _customObject;
                var dbId = GetLocalString(customObject, "DBID");

                var dbObject = DB.Get<AreaTemplateObject>(dbId);

                dbObject.ObjectName = GetName(customObject);
                dbObject.ObjectData = ObjectPlugin.Serialize(customObject);
                dbObject.LocationX = GetPosition(customObject).X;
                dbObject.LocationY = GetPosition(customObject).Y;
                dbObject.LocationZ = GetPosition(customObject).Z;
                dbObject.LocationOrientation = GetFacing(customObject);

                DB.Set<AreaTemplateObject>(dbObject);
            }
        }
    }
}
