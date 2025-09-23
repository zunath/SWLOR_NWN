using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.World.Contracts
{
    public interface IPlanetService
    {
        /// <summary>
        /// When the module loads, cache relevant data needed by the Planet service.
        /// </summary>
        void CacheData();

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
        /// Retrieves a planet detail by its type.
        /// Throws an exception if type is not registered or invalid.
        /// </summary>
        /// <param name="type">The type of planet to retrieve.</param>
        /// <returns>A planet detail object.</returns>
        PlanetAttribute GetPlanetByType(PlanetType type);

        /// <summary>
        /// Retrieves all of the active planets available.
        /// </summary>
        /// <returns>A dictionary containing the active planets.</returns>
        Dictionary<PlanetType, PlanetAttribute> GetAllPlanets();
    }
}
