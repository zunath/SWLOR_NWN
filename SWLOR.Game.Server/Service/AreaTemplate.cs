using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Service
{
    public class AreaTemplate
    {
        private static Dictionary<string, uint> TemplateAreasByResref { get; } = new();
        private static Dictionary<uint, List<uint>> TemplateAreaCustomObjectsByArea { get; } = new();

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
                    SetTransitionTarget(GetLocalObject(x, "ORIGINAL_TRANSITION_TARGET"), x);
                    DeleteLocalObject(x, "ORIGINAL_TRANSITION_TARGET");
                }

                // restore all save DM spawned objects
                var query = new DBQuery<AreaTemplateObject>()
                    .AddFieldSearch(nameof(AreaTemplateObject.AreaResref), GetResRef(targetArea), false)
                    .OrderBy(nameof(AreaTemplateObject.AreaResref));
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
    }
}
