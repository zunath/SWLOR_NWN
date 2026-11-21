using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The city upgrade read the death penalty makes (the Medical Center level). Combat owns the interface and
    /// <c>IPropertyCityGovernanceService</c> extends it (supply rule 2 of MaintainabilityRefactorDomainOrder.md), so the
    /// death service needs nothing from World.
    /// </summary>
    public interface ICityUpgradeLookup
    {
        /// <summary>
        /// Retrieves the effective upgrade level of a city, taking into account the city's overall level
        /// into the calculation.
        /// </summary>
        /// <param name="cityId">The property Id of the city.</param>
        /// <param name="upgradeType">The type of upgrade to check</param>
        /// <returns>A value ranging between 0 and 5.</returns>
        int GetEffectiveUpgradeLevel(string cityId, PropertyUpgradeType upgradeType);
    }
}
