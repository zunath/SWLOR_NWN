using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.World.Enums;

namespace SWLOR.Component.World.Service
{
    public class PlanetAreaService : IPlanetAreaService
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded service to break circular dependency
        private readonly Lazy<IPlanetCacheService> _planetCacheService;

        public PlanetAreaService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _planetCacheService = new Lazy<IPlanetCacheService>(() => _serviceProvider.GetRequiredService<IPlanetCacheService>());
        }
        
        // Lazy-loaded service to break circular dependency
        private IPlanetCacheService PlanetCacheService => _planetCacheService.Value;

        /// <summary>
        /// When the module loads, assign a planet Id to every area that is considered to be a planet.
        /// </summary>
        public void RegisterAreaPlanetIds()
        {
            var planets = PlanetCacheService.GetAllPlanets();

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var areaName = GetName(area);

                foreach (var (type, detail) in planets)
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
        /// Checks if an area belongs to a specific planet type.
        /// </summary>
        /// <param name="area">The area to check</param>
        /// <param name="planetType">The planet type to check against</param>
        /// <returns>True if the area belongs to the planet type, false otherwise</returns>
        public bool IsAreaOnPlanet(uint area, PlanetType planetType)
        {
            return GetPlanetType(area) == planetType;
        }

        /// <summary>
        /// Gets all areas that belong to a specific planet type.
        /// </summary>
        /// <param name="planetType">The planet type to find areas for</param>
        /// <returns>A list of area IDs that belong to the planet type</returns>
        public List<uint> GetAreasForPlanet(PlanetType planetType)
        {
            var areas = new List<uint>();
            var planets = PlanetCacheService.GetAllPlanets();
            
            if (!planets.TryGetValue(planetType, out var planetDetail))
                return areas;

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var areaName = GetName(area);
                if (areaName.StartsWith(planetDetail.Prefix))
                {
                    areas.Add(area);
                }
            }

            return areas;
        }
    }
}
