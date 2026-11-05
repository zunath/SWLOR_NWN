using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the planets and the planet each area belongs to. The logic and state live in
    /// <see cref="IPlanetService"/>; every member here forwards to the container's instance, and the event handler
    /// stays here so its name in the handler order manifest does not change.
    /// </summary>
    public static class Planet
    {
        private static IPlanetService Instance => FacadeAccessor.Resolve<IPlanetService>();

        /// <summary>
        /// When the module loads, cache relevant data needed by the Planet service.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// Retrieves the planet type of a given area.
        /// This is determined by the prefix of the area name.
        /// Only planets which are fully recognized will return a value.
        /// Additional planets can be registered in the Planet service.
        /// </summary>
        /// <param name="area">The area to check</param>
        /// <returns>A planet type. Returns PlanetType.Invalid on failure.</returns>
        public static PlanetType GetPlanetType(uint area)
        {
            return Instance.GetPlanetType(area);
        }

        /// <summary>
        /// Retrieves a planet type from an area resref when an area object is not available.
        /// </summary>
        /// <param name="areaResref">The area resref to check.</param>
        /// <returns>A planet type. Returns PlanetType.Invalid on failure.</returns>
        public static PlanetType GetPlanetTypeByAreaResref(string areaResref)
        {
            return Instance.GetPlanetTypeByAreaResref(areaResref);
        }

        /// <summary>
        /// Retrieves a planet detail by its type.
        /// Throws an exception if type is not registered or invalid.
        /// </summary>
        /// <param name="type">The type of planet to retrieve.</param>
        /// <returns>A planet detail object.</returns>
        public static PlanetAttribute GetPlanetByType(PlanetType type)
        {
            return Instance.GetPlanetByType(type);
        }

        /// <summary>
        /// Retrieves all of the active planets available.
        /// </summary>
        /// <returns>A dictionary containing the active planets.</returns>
        public static Dictionary<PlanetType, PlanetAttribute> GetAllPlanets()
        {
            return Instance.GetAllPlanets();
        }
    }
}
