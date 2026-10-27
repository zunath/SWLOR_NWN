using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Finds areas and objects for the DM chooser: the logic that used to be static in <c>Service.DMObjectSearch</c>,
    /// moved here unchanged. The facade forwards every call to the container's instance and keeps the two search
    /// limits, which the logic here does not use.
    /// </summary>
    public class DMObjectSearchService : IDMObjectSearchService
    {
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

        public Location GetAreaJumpLocation(uint area)
        {
            var waypoint = GetFirstObjectInArea(area, ObjectType.Waypoint);
            if (GetIsObjectValid(waypoint))
                return GetLocation(waypoint);

            const ObjectType anchorTypes = ObjectType.Door | ObjectType.Placeable | ObjectType.Creature;
            var anchor = GetFirstObjectInArea(area, anchorTypes);
            if (GetIsObjectValid(anchor))
                return GetLocation(anchor);

            var width = GetAreaSize(Dimension.Width, area) * 10f;
            var height = GetAreaSize(Dimension.Height, area) * 10f;
            return Location(area, new Vector3(width / 2f, height / 2f, 0f), 0f);
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
