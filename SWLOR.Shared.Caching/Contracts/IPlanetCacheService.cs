using SWLOR.Shared.Domain.World.Enums;

namespace SWLOR.Shared.Caching.Contracts
{
    public interface IPlanetCacheService
    {
        /// <summary>
        /// Initializes the planet cache with all planet data.
        /// </summary>
        void InitializeCache();

        /// <summary>
        /// Retrieves a planet detail by its type.
        /// Throws an exception if type is not registered or invalid.
        /// </summary>
        /// <param name="type">The type of planet to retrieve.</param>
        /// <returns>A planet detail object.</returns>
        PlanetAttribute GetPlanetByType(PlanetType type);

        /// <summary>
        /// Retrieves all planets (active and inactive).
        /// </summary>
        /// <returns>A dictionary containing all planets.</returns>
        Dictionary<PlanetType, PlanetAttribute> GetAllPlanets();

        /// <summary>
        /// Retrieves only the active planets.
        /// </summary>
        /// <returns>A dictionary containing only active planets.</returns>
        Dictionary<PlanetType, PlanetAttribute> GetActivePlanets();

        /// <summary>
        /// Checks if a planet type exists in the cache.
        /// </summary>
        /// <param name="type">The planet type to check.</param>
        /// <returns>True if the planet exists, false otherwise.</returns>
        bool HasPlanet(PlanetType type);
    }
}
