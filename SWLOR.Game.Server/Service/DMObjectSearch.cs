using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API for finding areas and objects for the DM chooser. The logic lives in
    /// <see cref="IDMObjectSearchService"/>; every member here forwards to the container's instance.
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

        private static IDMObjectSearchService Instance => FacadeAccessor.Resolve<IDMObjectSearchService>();

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
            return Instance.StartSearch(dm, category, scope, search);
        }

        /// <summary>
        /// Lists where a DM can land when jumping to an area, best first: its waypoints, then its center and the
        /// rest of its floor. Placeables and creatures are left out, because they come and go, and a decoration
        /// outside the walkable floor would send the jump nowhere.
        /// </summary>
        /// <param name="area">The area to jump to.</param>
        public static IEnumerable<Location> GetAreaJumpLocations(uint area)
        {
            return Instance.GetAreaJumpLocations(area);
        }
    }
}
