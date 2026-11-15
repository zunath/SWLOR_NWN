using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The catalog of ability definitions (part 1 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md): the
    /// reads of <see cref="IAbilityCatalog"/>, which Character owns, and the module-cache handler that fills them. Static
    /// callers reach the container's instance through <c>GameServices.AbilityCatalog</c>; the handler is on
    /// <see cref="AbilityCatalogService"/>.
    /// </summary>
    public interface IAbilityCatalogService : IAbilityCatalog
    {
        /// <summary>
        /// When the module caches, abilities will be cached and events will be scheduled.
        /// </summary>
        void CacheData();
    }
}
