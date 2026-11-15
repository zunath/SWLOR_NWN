using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The catalog of ability definitions by feat (part 1 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md),
    /// moved from <c>AbilityService</c> unchanged: the module-cache handler builds every definition once and hands the targeting
    /// overrides to <see cref="IAbilityTargetingCacheService"/>; the reads are <see cref="IAbilityCatalog"/>, which Character owns,
    /// so Character, Combat and the services above them read the catalog without reaching <c>IAbilityService</c>. The
    /// definition search sits behind a <c>protected virtual</c> method so a test can hand it its own definitions.
    /// </summary>
    public class AbilityCatalogService : IAbilityCatalogService
    {
        private readonly IAbilityTargetingCacheService _targetingCache;

        private readonly Dictionary<FeatType, AbilityDetail> _abilities = new();

        public AbilityCatalogService(IAbilityTargetingCacheService targetingCache)
        {
            _targetingCache = targetingCache;
        }

        /// <summary>
        /// When the module caches, abilities will be cached and events will be scheduled.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CacheData()
        {
            CacheAbilities();
        }

        private void CacheAbilities()
        {
            _abilities.Clear();
            var types = GetAbilityListDefinitionTypes();

            foreach (var type in types)
            {
                var instance = (IAbilityListDefinition)Activator.CreateInstance(type);
                var abilities = instance.BuildAbilities();

                foreach (var (feat, ability) in abilities)
                {
                    _abilities[feat] = ability;
                }
            }

            SWLOR.Game.Server.Domain.Abilities.AbilityAnimationBinding.Apply(_abilities, SWLOR.Game.Server.Domain.Foundation.ActiveAbilityAnimationCatalog.Entries);
            Console.WriteLine($"Loaded {_abilities.Count} abilities.");
            CacheAbilityTargeting(_abilities);
        }

        public IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails()
        {
            return _abilities;
        }

        /// <summary>
        /// Returns true if a feat is registered to an ability.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="featType">The type of feat to check.</param>
        /// <returns>true if feat is registered to an ability. false otherwise.</returns>
        public bool IsFeatRegistered(FeatType featType)
        {
            return _abilities.ContainsKey(featType);
        }

        /// <summary>
        /// Retrieves an ability's details by the specified feat type.
        /// If feat does not have an ability, an exception will be thrown.
        /// </summary>
        /// <param name="featType">The type of feat</param>
        /// <returns>The ability detail</returns>
        public AbilityDetail GetAbilityDetail(FeatType featType)
        {
            if (!_abilities.ContainsKey(featType))
                throw new KeyNotFoundException($"Feat '{featType}' is not registered to an ability.");

            return _abilities[featType];
        }

        /// <summary>The ability definitions of every loaded assembly. The search stays lazy: each definition is built when the loop reaches it.</summary>
        protected virtual IEnumerable<Type> GetAbilityListDefinitionTypes() =>
            GameAssemblies.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IAbilityListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

        protected virtual void CacheAbilityTargeting(IReadOnlyDictionary<FeatType, AbilityDetail> abilities) => _targetingCache.CacheData(abilities);
    }
}
