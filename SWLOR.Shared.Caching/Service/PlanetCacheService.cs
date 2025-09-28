using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.World.Enums;

namespace SWLOR.Shared.Caching.Service
{
    public class PlanetCacheService : IPlanetCacheService
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded service to break circular dependency
        private readonly Lazy<IGenericCacheService> _cacheService;
        private IEnumCache<PlanetType, PlanetAttribute> _planetCache;
        private readonly Dictionary<PlanetType, PlanetAttribute> _allPlanets = new();

        public PlanetCacheService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _cacheService = new Lazy<IGenericCacheService>(() => _serviceProvider.GetRequiredService<IGenericCacheService>());
        }
        
        // Lazy-loaded service to break circular dependency
        private IGenericCacheService CacheService => _cacheService.Value;

        /// <summary>
        /// Initializes the planet cache with all planet data.
        /// </summary>
        public void InitializeCache()
        {
            _planetCache = CacheService.BuildEnumCache<PlanetType, PlanetAttribute>()
                .WithAllItems()
                .WithFilteredCache("Active", p => p.IsActive)
                .Build();

            // Populate the _allPlanets dictionary for fast retrieval
            foreach (var (planetType, planetAttribute) in _planetCache.AllItems)
            {
                _allPlanets[planetType] = planetAttribute;
            }
        }

        /// <summary>
        /// Retrieves a planet detail by its type.
        /// Throws an exception if type is not registered or invalid.
        /// </summary>
        /// <param name="type">The type of planet to retrieve.</param>
        /// <returns>A planet detail object.</returns>
        public PlanetAttribute GetPlanetByType(PlanetType type)
        {
            return _planetCache?.AllItems[type] ?? throw new KeyNotFoundException($"Planet {type} not found in cache");
        }

        /// <summary>
        /// Retrieves all planets (active and inactive).
        /// </summary>
        /// <returns>A dictionary containing all planets.</returns>
        public Dictionary<PlanetType, PlanetAttribute> GetAllPlanets()
        {
            return _allPlanets;
        }

        /// <summary>
        /// Retrieves only the active planets.
        /// </summary>
        /// <returns>A dictionary containing only active planets.</returns>
        public Dictionary<PlanetType, PlanetAttribute> GetActivePlanets()
        {
            return _planetCache?.GetFilteredCache("Active") ?? new Dictionary<PlanetType, PlanetAttribute>();
        }

        /// <summary>
        /// Checks if a planet type exists in the cache.
        /// </summary>
        /// <param name="type">The planet type to check.</param>
        /// <returns>True if the planet exists, false otherwise.</returns>
        public bool HasPlanet(PlanetType type)
        {
            return _planetCache?.AllItems.ContainsKey(type) ?? false;
        }
    }
}
