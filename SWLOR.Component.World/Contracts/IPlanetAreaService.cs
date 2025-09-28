using SWLOR.Shared.Domain.World.Enums;

namespace SWLOR.Component.World.Contracts
{
    public interface IPlanetAreaService
    {
        /// <summary>
        /// When the module loads, assign a planet Id to every area that is considered to be a planet.
        /// </summary>
        void RegisterAreaPlanetIds();

        /// <summary>
        /// Retrieves the planet type of a given area.
        /// This is determined by the prefix of the area name.
        /// Only planets which are fully recognized will return a value.
        /// Additional planets can be registered in the Planet service.
        /// </summary>
        /// <param name="area">The area to check</param>
        /// <returns>A planet type. Returns PlanetType.Invalid on failure.</returns>
        PlanetType GetPlanetType(uint area);

        /// <summary>
        /// Checks if an area belongs to a specific planet type.
        /// </summary>
        /// <param name="area">The area to check</param>
        /// <param name="planetType">The planet type to check against</param>
        /// <returns>True if the area belongs to the planet type, false otherwise</returns>
        bool IsAreaOnPlanet(uint area, PlanetType planetType);

        /// <summary>
        /// Gets all areas that belong to a specific planet type.
        /// </summary>
        /// <param name="planetType">The planet type to find areas for</param>
        /// <returns>A list of area IDs that belong to the planet type</returns>
        List<uint> GetAreasForPlanet(PlanetType planetType);
    }
}
