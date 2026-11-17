using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Property maintenance: the boot clean-up of expired leases and properties queued for deletion, the refresh of permissions,
    /// deleting a property with everything it owns, and returning a starship to a safe dock. Split from <c>PropertyService</c> (P5-9b);
    /// the members moved unchanged.
    /// </summary>
    public interface IPropertyMaintenanceService
    {
        /// <summary>
        /// When the module loads, update the permissions list on all properties to reflect any changes.
        /// </summary>
        void RefreshPermissions();

        /// <summary>
        /// When the module loads, remove all data marked for deletion and any properties with expired leases.
        /// </summary>
        void CleanUpData();

        /// <summary>
        /// Deletes a property and its owned children, clears associated data and permissions,
        /// and revokes its citizens' membership and unpaid citizenship taxes.
        /// </summary>
        /// <param name="property">The property to delete.</param>
        void DeleteProperty(WorldProperty property);

        /// <summary>
        /// Starships left in a ship-only location, at a player starport that no longer exists (probably destroyed
        /// by the previous cleanup) or is being retrieved, or without a dock return to the last regular starport
        /// they docked at and its orbit. The caller saves the property.
        /// </summary>
        /// <param name="property">The starship property to update.</param>
        /// <param name="retrievedStarportId">A player starport interior being retrieved. Ships docked there return even though it still exists.</param>
        /// <param name="fallbackAreaResref">An area whose planet's starport receives the ship if it has no usable safe dock, such as the retrieved starport's city.</param>
        /// <returns>What happened to the starship's dock.</returns>
        StarshipDockReturnResult ReturnStarshipToSafeDock(WorldProperty property, string retrievedStarportId = "", string fallbackAreaResref = "");
    }
}
