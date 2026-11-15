using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The catalog of ability definitions by feat, as the domains below Abilities read it. Character owns the interface and
    /// Abilities implements it (<c>AbilityCatalogService</c>, supply rule 2 of MaintainabilityRefactorDomainOrder.md): the
    /// implementation takes only the targeting cache, so the services that read it are built without a construction cycle.
    /// The reads run per activation and allocate nothing.
    /// </summary>
    public interface IAbilityCatalog
    {
        IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails();

        /// <summary>
        /// Returns true if a feat is registered to an ability.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="featType">The type of feat to check.</param>
        /// <returns>true if feat is registered to an ability. false otherwise.</returns>
        bool IsFeatRegistered(FeatType featType);

        /// <summary>
        /// Retrieves an ability's details by the specified feat type.
        /// If feat does not have an ability, an exception will be thrown.
        /// </summary>
        /// <param name="featType">The type of feat</param>
        /// <returns>The ability detail</returns>
        AbilityDetail GetAbilityDetail(FeatType featType);
    }
}
