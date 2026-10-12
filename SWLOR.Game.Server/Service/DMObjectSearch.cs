using System.Collections.Generic;
using System.Linq;
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
        /// Searches stop after this many matches so an all-areas search stays cheap.
        /// </summary>
        public const int MaxResults = 500;

        /// <summary>
        /// Finds areas or objects whose name, tag, or resref contain every search word.
        /// </summary>
        /// <param name="dm">The DM searching. Their area is used for the current-area scope.</param>
        /// <param name="category">The kind of object to find.</param>
        /// <param name="scope">Whether to search the DM's area or every area. Ignored for areas.</param>
        /// <param name="search">The search text. Empty matches everything.</param>
        /// <param name="isTruncated">Set when more matches existed than <see cref="MaxResults"/>.</param>
        public static List<DMObjectSearchResult> Search(
            uint dm,
            DMObjectCategory category,
            DMObjectSearchScope scope,
            string search,
            out bool isTruncated)
        {
            var terms = DMSearchText.GetTerms(search);
            var results = new List<DMObjectSearchResult>();
            isTruncated = false;

            if (category == DMObjectCategory.Areas)
            {
                foreach (var area in GetAllAreas())
                {
                    var name = GetName(area);
                    var tag = GetTag(area);
                    var resref = GetResRef(area);
                    if (!DMSearchText.Matches(terms, name, tag, resref))
                        continue;

                    if (results.Count >= MaxResults)
                    {
                        isTruncated = true;
                        break;
                    }

                    results.Add(new DMObjectSearchResult(area, category, name, tag, resref, name));
                }
            }
            else
            {
                var objectType = GetObjectType(category);
                var areas = scope == DMObjectSearchScope.AllAreas
                    ? GetAllAreas()
                    : new List<uint> { GetArea(DMPresence.GetControlledCreature(dm)) };

                foreach (var area in areas)
                {
                    if (!GetIsObjectValid(area))
                        continue;

                    var areaName = GetName(area);
                    for (var obj = GetFirstObjectInArea(area, objectType);
                         GetIsObjectValid(obj);
                         obj = GetNextObjectInArea(area, objectType))
                    {
                        // Player characters and DMs belong to the player search.
                        if (category == DMObjectCategory.Creatures &&
                            (GetIsDM(obj) || (GetIsPC(obj) && !GetIsDMPossessed(obj))))
                            continue;

                        var name = GetName(obj);
                        var tag = GetTag(obj);
                        var resref = GetResRef(obj);
                        if (!DMSearchText.Matches(terms, name, tag, resref))
                            continue;

                        if (results.Count >= MaxResults)
                        {
                            isTruncated = true;
                            break;
                        }

                        results.Add(new DMObjectSearchResult(obj, category, name, tag, resref, areaName));
                    }

                    if (isTruncated)
                        break;
                }
            }

            return results
                .OrderBy(result => result.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(result => result.AreaName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Picks where a DM lands when jumping to an area: a waypoint, then any placed object, then the area's center.
        /// </summary>
        /// <param name="area">The area to jump to.</param>
        public static Location GetAreaJumpLocation(uint area)
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
