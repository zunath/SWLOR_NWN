using System.Collections.Generic;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Finds areas and objects for the DM chooser. Static callers reach the
    /// container's instance of this interface through <c>GameServices.DMObjectSearch</c>.
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
        /// Lists where a DM can land when jumping to an area, best first: its waypoints, then its center and the
        /// rest of its floor. Placeables and creatures are left out, because they come and go, and a decoration
        /// outside the walkable floor would send the jump nowhere.
        /// </summary>
        /// <param name="area">The area to jump to.</param>
        IEnumerable<Location> GetAreaJumpLocations(uint area);
    }
}
