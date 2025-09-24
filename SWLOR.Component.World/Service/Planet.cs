using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.World.Service
{
    public class PlanetService : IPlanetService
    {
        private readonly IGenericCacheService _cacheService;
        private IEnumCache<PlanetType, PlanetAttribute>? _planetCache;
        
        // Additional cache for backward compatibility
        private readonly Dictionary<PlanetType, PlanetAttribute> _planets = new();
        
        // Pre-computed cache for fast retrieval
        private readonly Dictionary<PlanetType, PlanetAttribute> _allPlanets = new();

        public PlanetService(IGenericCacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// When the module loads, cache relevant data needed by the Planet service.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            CachePlanets();
            RegisterAreaPlanetIds();
        }

        /// <summary>
        /// When the module loads, cache all the different planet types.
        /// </summary>
        private void CachePlanets()
        {
            _planetCache = _cacheService.BuildEnumCache<PlanetType, PlanetAttribute>()
                .WithAllItems()
                .WithFilteredCache("Active", p => p.IsActive)
                .Build();

            // Populate the _planets dictionary for backward compatibility
            foreach (var (planetType, planetAttribute) in _planetCache.AllItems)
            {
                _planets[planetType] = planetAttribute;
                _allPlanets[planetType] = planetAttribute;
            }
        }

        /// <summary>
        /// When the module loads, assign a planet Id to every area that is considered to be a planet.
        /// </summary>
        private void RegisterAreaPlanetIds()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var areaName = GetName(area);

                foreach (var (type, detail) in _planets)
                {
                    if (areaName.StartsWith(detail.Prefix))
                    {
                        SetLocalInt(area, "PLANET_TYPE_ID", (int)type);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the planet type of a given area.
        /// This is determined by the prefix of the area name.
        /// Only planets which are fully recognized will return a value.
        /// Additional planets can be registered in the Planet service.
        /// </summary>
        /// <param name="area">The area to check</param>
        /// <returns>A planet type. Returns PlanetType.Invalid on failure.</returns>
        public PlanetType GetPlanetType(uint area)
        {
            var planetTypeId = GetLocalInt(area, "PLANET_TYPE_ID");

            return (PlanetType)planetTypeId;
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
        /// Retrieves all of the active planets available.
        /// </summary>
        /// <returns>A dictionary containing the active planets.</returns>
        public Dictionary<PlanetType, PlanetAttribute> GetAllPlanets()
        {
            return _allPlanets;
        }
    }
}
