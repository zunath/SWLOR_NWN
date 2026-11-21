namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// The property id lookup the craft device terminals make. Foundation owns the interface and <c>IPropertyService</c> extends
    /// it (supply rule 2 of MaintainabilityRefactorDomainOrder.md), so the terminals need nothing from World.
    /// </summary>
    public interface IPropertyIdLookup
    {
        /// <summary>
        /// Retrieves the assigned property Id assigned to a specific object.
        /// Returns an empty string if not found.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>The property Id or an empty string if not found.</returns>
        string GetPropertyId(uint obj);
    }
}
