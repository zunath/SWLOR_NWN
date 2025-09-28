using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Domain.World.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.World.Service
{
    public class PlanetService : IPlanetService
    {
        private readonly IPlanetCacheService _planetCacheService;
        private readonly IPlanetAreaService _planetAreaService;

        public PlanetService(
            IPlanetCacheService planetCacheService,
            IPlanetAreaService planetAreaService)
        {
            _planetCacheService = planetCacheService;
            _planetAreaService = planetAreaService;
        }

        /// <summary>
        /// When the module loads, cache relevant data needed by the Planet service.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            _planetCacheService.InitializeCache();
            _planetAreaService.RegisterAreaPlanetIds();
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
            return _planetAreaService.GetPlanetType(area);
        }

        /// <summary>
        /// Retrieves a planet detail by its type.
        /// Throws an exception if type is not registered or invalid.
        /// </summary>
        /// <param name="type">The type of planet to retrieve.</param>
        /// <returns>A planet detail object.</returns>
        public PlanetAttribute GetPlanetByType(PlanetType type)
        {
            return _planetCacheService.GetPlanetByType(type);
        }

        /// <summary>
        /// Retrieves all of the active planets available.
        /// </summary>
        /// <returns>A dictionary containing the active planets.</returns>
        public Dictionary<PlanetType, PlanetAttribute> GetAllPlanets()
        {
            return _planetCacheService.GetAllPlanets();
        }
    }
}
