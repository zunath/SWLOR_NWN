using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Service
{
    public class AreaTemplate
    {
        private static Dictionary<string, uint> TemplateAreasByResref { get; } = new();
        private static Dictionary<uint, List<uint>> TemplateAreaCustomObjectsByArea { get; } = new();
        private static Dictionary<int, Dictionary<int, string>> PlaceableAppearanceByPage { get; } = new();
        private static Dictionary<int, Dictionary<int, string>> CreatureAppearanceByPage { get; } = new();

        public static int TotalCountAppearancePlaceables { get; set; }
        public static int TotalCountAppearanceCreatures { get; set; }

        public const int PAGESIZE = 50;

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
                    if (actualRowCount > 0 && actualRowCount % PAGESIZE == 0) pageNumber++;

                    if (!PlaceableAppearanceByPage.ContainsKey(pageNumber))
                        PlaceableAppearanceByPage[pageNumber] = new Dictionary<int, string>();

                    PlaceableAppearanceByPage[pageNumber].Add(row, entry);
                    actualRowCount++;                 
                }
            }
            TotalCountAppearancePlaceables = actualRowCount;
            Console.WriteLine($"Loaded {PlaceableAppearanceByPage.Count} placeable appearance pages ({AreaTemplate.PAGESIZE} per page)  into cache.");

            rowCount = UtilPlugin.Get2DARowCount("appearance");
            pageNumber = 0;
            actualRowCount = 0;

            for (int row = 0; row < rowCount; row++)
            {
                var entry = Get2DAString("appearance", "Label", row);

                if (entry != "****" && !string.IsNullOrWhiteSpace(entry))
                {
                    if (actualRowCount > 0 && actualRowCount % PAGESIZE == 0) pageNumber++;

                    if (!CreatureAppearanceByPage.ContainsKey(pageNumber))
                        CreatureAppearanceByPage[pageNumber] = new Dictionary<int, string>();

                    CreatureAppearanceByPage[pageNumber].Add(row, entry);
                    actualRowCount++;
                }
            }
            TotalCountAppearanceCreatures = actualRowCount;
            Console.WriteLine($"Loaded {CreatureAppearanceByPage.Count} creature appearance pages ({AreaTemplate.PAGESIZE} per page)  into cache.");
        }

        [NWNEventHandler("mod_exit")]
        public static void OnExit()
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

        public static Dictionary<int, string> GetPlaceableAppearances(int pageNumber)
        {
            return PlaceableAppearanceByPage[pageNumber];
        }
        public static Dictionary<int, string> GetCreatureAppearances(int pageNumber)
        {
            return CreatureAppearanceByPage[pageNumber];
        }
        public static void CacheTemplateArea(string resref, uint area)
        {
            TemplateAreasByResref[resref] = area;
        }
        public static bool GetIsTemplateArea(uint area)
        {
            return GetLocalBool(area, "IS_TEMPLATE_AREA");
        }

        [NWNEventHandler("mod_load")]
        public static void CacheTemplateAreas()
        {
            Console.WriteLine("Creating Area Templates, please wait...");
            foreach (var area in Area.GetAreas())
            {
                // store transition targets to restore after CreateArea is called.
                for (var x = GetFirstObjectInArea(area.Value); GetIsObjectValid(x); x = GetNextObjectInArea(area.Value))
                {
                    if (GetTransitionTarget(x) != OBJECT_INVALID)
                        SetLocalObject(x, "ORIGINAL_TRANSITION_TARGET", GetTransitionTarget(x));
                }

                var targetArea = CreateArea(GetResRef(area.Value), "template_" + GetTag(area.Value), "[Template] " + GetName(area.Value));
                SetLocalBool(targetArea, "IS_TEMPLATE_AREA", true);

                for (var x = GetFirstObjectInArea(targetArea); GetIsObjectValid(x); x = GetNextObjectInArea(targetArea))
                {
                    if (GetObjectType(x) == ObjectType.Creature ||
                        GetObjectType(x) == ObjectType.Waypoint ||
                        GetObjectType(x) == ObjectType.Trigger)
                    {
                        DestroyObject(x);
                    }

                    if (GetObjectType(x) == ObjectType.Door)
                    {
                        SetTransitionTarget(x, OBJECT_INVALID);
                    }
                }

                CacheTemplateArea(GetResRef(targetArea), targetArea);

                // restore transition targets
                for (var x = GetFirstObjectInArea(area.Value); GetIsObjectValid(x); x = GetNextObjectInArea(area.Value))
                {
                    var transitionTarget = GetLocalObject(x, "ORIGINAL_TRANSITION_TARGET");
                    if (transitionTarget != OBJECT_INVALID)
                    {
                        SetTransitionTarget(transitionTarget, x);
                        DeleteLocalObject(x, "ORIGINAL_TRANSITION_TARGET");
                    }
                }

                // restore all save DM spawned objects
                var query = new DBQuery<AreaTemplateObject>()
                    .AddFieldSearch(nameof(AreaTemplateObject.AreaResref), GetResRef(targetArea), false)
                    .OrderBy(nameof(AreaTemplateObject.ObjectName));
                var areaTemplates = DB.Search(query)
                    .ToList();

                foreach (var x in areaTemplates)
                {
                    var deserialized = ObjectPlugin.Deserialize(x.ObjectData);
                    var position = Vector3(x.LocationX, x.LocationY, x.LocationZ);
                    var location = Location(targetArea, position, x.LocationOrientation);
                    var newObject = CopyObject(deserialized, location);

                    if (!TemplateAreaCustomObjectsByArea.ContainsKey(targetArea))
                        TemplateAreaCustomObjectsByArea[targetArea] = new List<uint>();

                    if (!TemplateAreaCustomObjectsByArea[targetArea].Contains(newObject))
                    {
                        TemplateAreaCustomObjectsByArea[targetArea].Add(newObject);
                    }

                    SetLocalString(newObject, "DBID", x.Id);
                }
            }
            Console.WriteLine("Created " + GetTemplateAreas().Count + " template areas.");
        }

        /// <summary>
        /// Retrieves list of template areas only.
        /// </summary>
        /// <param> </param>
        /// <returns>TemplateAreasByResref cache.</returns>
        public static Dictionary<string, uint> GetTemplateAreas()
        {
            return TemplateAreasByResref;
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
                        ObjectName = GetName(spawn),
                        ObjectData = ObjectPlugin.Serialize(spawn),
                        LocationX = GetPosition(spawn).X,
                        LocationY = GetPosition(spawn).Y,
                        LocationZ = GetPosition(spawn).Z,
                        LocationOrientation = GetFacing(spawn)
                    };
                    
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
