using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Finds areas and objects for the DM chooser.
    /// </summary>
    public static class DMObjectSearch
    {
        /// <summary>
        /// Searches stop after this many matches.
        /// </summary>
        public const int MaxResults = 500;

        /// <summary>
        /// How many objects a search examines per server tick before it continues on the next one.
        /// </summary>
        public const int ObjectsPerSlice = 4000;

        /// <summary>
        /// Starts a search for areas or objects whose name, tag, or resref contain every search word.
        /// Run it with <see cref="DMObjectScan.Continue"/>.
        /// </summary>
        /// <param name="dm">The DM searching. The area of whatever they control is used for the current-area scope.</param>
        /// <param name="category">The kind of object to find.</param>
        /// <param name="scope">Whether to search the DM's area or every area. Ignored for areas.</param>
        /// <param name="search">The search text. Empty matches everything.</param>
        public static DMObjectScan StartSearch(
            uint dm,
            DMObjectCategory category,
            DMObjectSearchScope scope,
            string search)
        {
            if (category == DMObjectCategory.Areas)
                return new DMObjectScan(category, ObjectType.All, GetAllAreas(), search);

            var areas = scope == DMObjectSearchScope.AllAreas
                ? GetAllAreas()
                : new List<uint> { GetArea(DMPresence.GetControlledCreature(dm)) };
            return new DMObjectScan(category, GetObjectType(category), areas, search);
        }

        /// <summary>
        /// Lists where a DM can land when jumping to an area, best first: its waypoints, then its center and the
        /// rest of its floor. Placeables and creatures are left out, because they come and go, and a decoration
        /// outside the walkable floor would send the jump nowhere.
        /// </summary>
        /// <param name="area">The area to jump to.</param>
        public static IEnumerable<Location> GetAreaJumpLocations(uint area)
        {
            var locations = new List<Location>();
            for (var waypoint = GetFirstObjectInArea(area, ObjectType.Waypoint);
                 GetIsObjectValid(waypoint);
                 waypoint = GetNextObjectInArea(area, ObjectType.Waypoint))
            {
                locations.Add(GetLocation(waypoint));
            }

            foreach (var spot in DMJumpSpots.GetAreaSpots(GetAreaSize(Dimension.Width, area), GetAreaSize(Dimension.Height, area)))
            {
                locations.Add(Location(area, new Vector3(spot.X, spot.Y, 0f), 0f));
            }

            return locations;
        }

        private static List<uint> GetAllAreas()
        {
            var areas = new List<uint>();
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                areas.Add(area);
            }

            return areas;
        }

        private static ObjectType GetObjectType(DMObjectCategory category)
        {
            return category switch
            {
                DMObjectCategory.Creatures => ObjectType.Creature,
                DMObjectCategory.Placeables => ObjectType.Placeable,
                DMObjectCategory.Items => ObjectType.Item,
                DMObjectCategory.Doors => ObjectType.Door,
                DMObjectCategory.Triggers => ObjectType.Trigger,
                DMObjectCategory.Waypoints => ObjectType.Waypoint,
                DMObjectCategory.Stores => ObjectType.Store,
                DMObjectCategory.Encounters => ObjectType.Encounter,
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };
        }
    }
}
