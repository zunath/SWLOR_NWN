using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Model;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Domain.World.Enums;

namespace SWLOR.Component.World.Service
{
    /// <summary>
    /// Service for managing weather climate data and planet-specific weather configurations.
    /// </summary>
    public class WeatherClimateService : IWeatherClimateService
    {
        private Dictionary<PlanetType, WeatherClimate> _planetClimates;
        private readonly Dictionary<string, PlanetType> _planetsByName = new();

        /// <summary>
        /// When the module loads, cache planet climates and other pertinent data.
        /// </summary>
        public void LoadData()
        {
            _planetClimates = WeatherPlanetDefinitions.GetPlanetClimates();

            foreach (var @enum in Enum.GetValues(typeof(PlanetType)))
            {
                var type = (PlanetType)@enum;
                _planetsByName[type.GetDescriptionAttribute()] = type;
            }
        }

        /// <summary>
        /// Gets the climate data for a specific planet type.
        /// </summary>
        /// <param name="planetType">The planet type to get climate for.</param>
        /// <returns>The climate data for the planet.</returns>
        public WeatherClimate GetClimateByPlanetType(PlanetType planetType)
        {
            return _planetClimates?.GetValueOrDefault(planetType) ?? new WeatherClimate();
        }

        /// <summary>
        /// Retrieves a planet's climate by its name. If the planet is not registered a default climate will be returned.
        /// </summary>
        /// <param name="planetName">The name of the planet to look for.</param>
        /// <returns>A weather climate for the specified planet.</returns>
        public WeatherClimate GetClimateByPlanetName(string planetName)
        {
            if (!_planetsByName.ContainsKey(planetName))
            {
                return new WeatherClimate();
            }

            var planetType = _planetsByName[planetName];
            return GetClimateByPlanetType(planetType);
        }

        /// <summary>
        /// Gets the climate data for an area based on its name.
        /// </summary>
        /// <param name="area">The area to get climate for.</param>
        /// <returns>The climate data for the area.</returns>
        public WeatherClimate GetAreaClimate(uint area)
        {
            var index = GetName(area).IndexOf("-", StringComparison.Ordinal);
            if (index <= 0) return new WeatherClimate();
            var planetName = GetName(area).Substring(0, index);

            return GetClimateByPlanetName(planetName);
        }
    }
}
