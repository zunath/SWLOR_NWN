using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The planets of the module and which planet each area belongs to. The static facade <c>Service.Planet</c>
    /// forwards to the container's instance of this interface and keeps the event handler.
    /// </summary>
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
        /// Retrieves a planet type from an area resref when an area object is not available.
        /// </summary>
        /// <param name="areaResref">The area resref to check.</param>
        /// <returns>A planet type. Returns PlanetType.Invalid on failure.</returns>
        PlanetType GetPlanetTypeByAreaResref(string areaResref);

        /// <summary>
        /// Retrieves a planet detail by its type.
        /// Throws an exception if type is not registered or invalid.
        /// </summary>
        /// <param name="type">The type of planet to retrieve.</param>
        /// <returns>A planet detail object.</returns>
        PlanetAttribute GetPlanetByType(PlanetType type);

        /// <summary>
        /// Gets the message shown to players who try to travel to a closed planet.
        /// </summary>
        /// <param name="type">The planet to check.</param>
        /// <param name="message">The closure message, or an empty string when the planet is open.</param>
        /// <returns>True when the planet is closed to players.</returns>
        bool TryGetClosedMessage(PlanetType type, out string message);

        /// <summary>
        /// Retrieves all of the active planets available.
        /// </summary>
        /// <returns>A dictionary containing the active planets.</returns>
        Dictionary<PlanetType, PlanetAttribute> GetAllPlanets();
    }
}
