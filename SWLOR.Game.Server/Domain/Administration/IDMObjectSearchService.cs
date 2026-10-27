using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Finds areas and objects for the DM chooser. The static facade <c>Service.DMObjectSearch</c> forwards to the
    /// container's instance of this interface.
    /// </summary>
    public interface IDMObjectSearchService
    {
        /// <summary>
        /// Starts a search for areas or objects whose name, tag, or resref contain every search word.
        /// Run it with <see cref="DMObjectScan.Continue"/>.
        /// </summary>
        /// <param name="dm">The DM searching. The area of whatever they control is used for the current-area scope.</param>
        /// <param name="category">The kind of object to find.</param>
        /// <param name="scope">Whether to search the DM's area or every area. Ignored for areas.</param>
        /// <param name="search">The search text. Empty matches everything.</param>
        DMObjectScan StartSearch(
            uint dm,
            DMObjectCategory category,
            DMObjectSearchScope scope,
            string search);

        /// <summary>
        /// Picks where a DM lands when jumping to an area: a waypoint, then any placed object, then the area's center.
        /// </summary>
        /// <param name="area">The area to jump to.</param>
        Location GetAreaJumpLocation(uint area);
    }
}
