using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// The area cache reads the domains below World make: an area by resref and the players in an area. Foundation owns the
    /// interface and <c>IAreaService</c> extends it (supply rule 2 of MaintainabilityRefactorDomainOrder.md; P5-15a), so
    /// death and the DM tools read the cache without naming World. The container's one <c>AreaService</c> answers both.
    /// </summary>
    public interface IAreaLookup
    {
        /// <summary>
        /// Retrieves an area by its resref. If the area does not exist, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="resref">The resref to use for the search.</param>
        /// <returns>The area ID or OBJECT_INVALID if area does not exist.</returns>
        uint GetAreaByResref(string resref);

        /// <summary>
        /// Retrieves all of the players currently in the specified area.
        /// If no players are in the area, an empty list will returned.
        /// </summary>
        /// <param name="area">The area to search by.</param>
        /// <returns>A list of player objects</returns>
        List<uint> GetPlayersInArea(uint area);
    }
}
