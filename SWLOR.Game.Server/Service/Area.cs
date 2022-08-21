using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Service
{
    public class Area
    {
        private static Dictionary<string, uint> AreasByResref { get; } = new();
        private static Dictionary<string, uint> TemplateAreasByResref { get; } = new();
        private static Dictionary<uint, List<uint>> TemplateAreaCustomObjectsByArea { get; } = new();
        private static Dictionary<uint, List<uint>> PlayersByArea { get; } = new();

        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            CacheAreasByResref();

            Console.WriteLine($"Loaded {AreasByResref.Count} areas by resref.");
        }

        /// <summary>
        /// Caches all areas by their resref.
        /// </summary>
        private static void CacheAreasByResref()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var resref = GetResRef(area);
                AreasByResref[resref] = area;
            }
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
            foreach (var area in GetAreas())
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

                Area.CacheTemplateArea(GetResRef(targetArea), targetArea);

                // restore transition targets
                for (var x = GetFirstObjectInArea(area.Value); GetIsObjectValid(x); x = GetNextObjectInArea(area.Value))
                {
                    SetTransitionTarget(GetLocalObject(x, "ORIGINAL_TRANSITION_TARGET"), x);
                    DeleteLocalObject(x, "ORIGINAL_TRANSITION_TARGET");
                }

                // restore all save DM spawned objects
                var query = new DBQuery<AreaTemplate>()
                    .AddFieldSearch(nameof(AreaTemplate.AreaResref), GetResRef(targetArea), false)
                    .OrderBy(nameof(AreaTemplate.AreaResref));
                var areaTemplates = DB.Search(query)
                    .ToList();

                //Console.WriteLine("Area Template created: " + GetName(targetArea));
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
                    Console.WriteLine("Object Restored: " + GetName(newObject));
                }
            }
            Console.WriteLine("Created " + GetTemplateAreas().Count + " template areas.");
        }

        /// <summary>
        /// Remove instance templates from the area cache on module load.
        /// This ensures player locations are not updated in places they shouldn't be.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void RemoveInstancesFromCache()
        {
            var propertyLayouts = Property.GetAllLayoutsByPropertyType(PropertyType.Apartment);
            foreach (var type in propertyLayouts)
            {
                var layout = Property.GetLayoutByType(type);
                if (AreasByResref.ContainsKey(layout.AreaInstanceResref))
                    AreasByResref.Remove(layout.AreaInstanceResref);
            }
        }

        /// <summary>
        /// Retrieves an area by its resref. If the area does not exist, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="resref">The resref to use for the search.</param>
        /// <returns>The area ID or OBJECT_INVALID if area does not exist.</returns>
        public static uint GetAreaByResref(string resref)
        {
            if (!AreasByResref.ContainsKey(resref))
                return OBJECT_INVALID;

            return AreasByResref[resref];
        }

        /// <summary>
        /// Retrieves list of all areas.
        /// </summary>
        /// <param> </param>
        /// <returns>AreasByResref cache.</returns>
        public static Dictionary<string, uint> GetAreas()
        {
            return AreasByResref;
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
        /// Retrieves all of the players currently in the specified area.
        /// If no players are in the area, an empty list will returned.
        /// </summary>
        /// <param name="area">The area to search by.</param>
        /// <returns>A list of player objects</returns>
        public static List<uint> GetPlayersInArea(uint area)
        {
            if (!PlayersByArea.ContainsKey(area))
                return new List<uint>();

            return PlayersByArea[area].ToList();
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

        // *************  need to handle on object destroyed ************

        [NWNEventHandler("dm_spwnobj_bef")]
        public static void DmSpawnObjectBefore()
        {
            // nothing needed here?
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

                    var areaTemplate = new AreaTemplate
                    {
                        AreaResref = GetResRef(area),
                        ObjectName = GetName(spawn),
                        ObjectData = ObjectPlugin.Serialize(spawn),
                        LocationX = GetPosition(spawn).X,
                        LocationY = GetPosition(spawn).Y,
                        LocationZ = GetPosition(spawn).Z,
                        LocationOrientation = GetFacing(spawn)
                    };
                    
                    DB.Set<AreaTemplate>(areaTemplate);
                    Console.WriteLine("Object saved: " + GetName(spawn));
                };                   
            }
        }

        /// <summary>
        /// When a player or DM enters an area, add them to the cache.
        /// </summary>
        [NWNEventHandler("area_enter")]
        public static void EnterArea()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player))
                return;

            var area = OBJECT_SELF;
            if (!PlayersByArea.ContainsKey(area))
                PlayersByArea[area] = new List<uint>();

            if(!PlayersByArea[area].Contains(player))
                PlayersByArea[area].Add(player);

            // Handle DM created Area Notes
            var query = new DBQuery<AreaNote>()
                .AddFieldSearch(nameof(AreaNote.AreaResref), GetResRef(area), false)
                .OrderBy(nameof(AreaNote.AreaResref));
            var notes = DB.Search(query)
                .ToList();

            if (notes.Count > 0)
            {
                var message = GetName(area) + ": ";
                foreach (var note in notes)
                {
                    message += note.PublicText;
                }
                SendMessageToPC(player, ColorToken.Purple(message));
            }
        }

        /// <summary>
        /// When a player or DM leaves an area, remove them from the cache.
        /// </summary>
        [NWNEventHandler("area_exit")]
        public static void ExitArea()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player))
                return;

            var area = OBJECT_SELF;
            if (!PlayersByArea.ContainsKey(area))
                PlayersByArea[area] = new List<uint>();

            if (PlayersByArea[area].Contains(player))
                PlayersByArea[area].Remove(player);
        }

    }
}
