using System;
using System.Collections.Generic;
using System.Linq;

using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Entity;

using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Service
{
    public class Area
    {
        private static Dictionary<string, uint> AreasByResref { get; } = new();
        private static Dictionary<uint, List<uint>> PlayersByArea { get; } = new();

        [ScriptHandler(ScriptName.OnModuleCacheBefore)]
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

        /// <summary>
        /// Remove instance templates from the area cache on module load.
        /// This ensures player locations are not updated in places they shouldn't be.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleLoad)]
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
        /// When a player or DM enters an area, add them to the cache.
        /// </summary>
        [ScriptHandler(ScriptName.OnAreaEnter)]
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
                var prefix = GetName(area) + ": ";
                var message = string.Empty;
                foreach (var note in notes)
                {
                    message += note.PublicText;
                }

                if (!string.IsNullOrWhiteSpace(message.Trim()))
                {
                    SendMessageToPC(player, ColorToken.Purple(prefix + message));
                }
            }
        }

        /// <summary>
        /// When a player or DM leaves an area, remove them from the cache.
        /// </summary>
        [ScriptHandler(ScriptName.OnAreaExit)]
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
