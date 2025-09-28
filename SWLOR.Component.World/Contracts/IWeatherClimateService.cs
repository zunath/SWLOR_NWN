using SWLOR.Component.World.Model;
using SWLOR.Shared.Domain.World.Enums;

namespace SWLOR.Component.World.Contracts
{
    /// <summary>
    /// Service for managing weather climate data and planet-specific weather configurations.
    /// </summary>
    public interface IWeatherClimateService
    {
        /// <summary>
        /// Gets the climate data for a specific planet type.
        /// </summary>
        /// <param name="planetType">The planet type to get climate for.</param>
        /// <returns>The climate data for the planet.</returns>
        WeatherClimate GetClimateByPlanetType(PlanetType planetType);

        /// <summary>
        /// Gets the climate data for a planet by its name.
        /// </summary>
        /// <param name="planetName">The name of the planet to look for.</param>
        /// <returns>A weather climate for the specified planet.</returns>
        WeatherClimate GetClimateByPlanetName(string planetName);

        /// <summary>
        /// Gets the climate data for an area based on its name.
        /// </summary>
        /// <param name="area">The area to get climate for.</param>
        /// <returns>The climate data for the area.</returns>
        WeatherClimate GetAreaClimate(uint area);

        /// <summary>
        /// Loads climate data when the module caches.
        /// </summary>
        void LoadData();
    }
}
