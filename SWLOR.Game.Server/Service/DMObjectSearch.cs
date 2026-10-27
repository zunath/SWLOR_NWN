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
        /// Picks where a DM lands when jumping to an area: a waypoint, then any placed object, then the area's center.
        /// </summary>
        /// <param name="area">The area to jump to.</param>
        public static Location GetAreaJumpLocation(uint area)
        {
            return Instance.GetAreaJumpLocation(area);
        }
    }
}
