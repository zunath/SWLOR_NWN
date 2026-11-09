using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Finds areas and objects for the DM chooser: the logic that used to be static in <c>DMObjectSearch</c>,
    /// moved here unchanged. Static callers reach the container's instance through <c>GameServices.DMObjectSearch</c>;
    /// the two search limits are constants of this class, which the logic here does not use.
    /// </summary>
    public class DMObjectSearchService : IDMObjectSearchService
    {
        /// <summary>
        /// Searches stop after this many matches.
        /// </summary>
        public const int MaxResults = 500;

        /// <summary>
        /// How many objects a search examines per server tick before it continues on the next one.
        /// </summary>
        public const int ObjectsPerSlice = 4000;

        private readonly IDMPresenceService _presence;

        public DMObjectSearchService(IDMPresenceService presence)
        {
            _presence = presence;
        }

        public DMObjectScan StartSearch(
            uint dm,
            DMObjectCategory category,
            DMObjectSearchScope scope,
            string search)
        {
            if (category == DMObjectCategory.Areas)
                return new DMObjectScan(category, ObjectType.All, GetAllAreas(), search);

            var areas = scope == DMObjectSearchScope.AllAreas
                ? GetAllAreas()
                : new List<uint> { GetArea(_presence.GetControlledCreature(dm)) };
            return new DMObjectScan(category, GetObjectType(category), areas, search);
        }

        public IEnumerable<Location> GetAreaJumpLocations(uint area)
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
        private List<uint> GetAllAreas()
        {
            var areas = new List<uint>();
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                areas.Add(area);
            }

            return areas;
        }

        private ObjectType GetObjectType(DMObjectCategory category)
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
