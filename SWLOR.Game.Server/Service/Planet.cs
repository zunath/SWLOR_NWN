using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Extension;

namespace SWLOR.Game.Server.Service
{
    public static class Planet
    {
        private static readonly Dictionary<PlanetType, PlanetAttribute> _planets = new();

        /// <summary>
        /// When the module loads, cache relevant data needed by the Planet service.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            CachePlanets();
            RegisterAreaPlanetIds();
        }

        /// <summary>
        /// When the module loads, cache all the different planet types.
        /// </summary>
        private static void CachePlanets()
        {
            var planetTypes = Enum.GetValues(typeof(PlanetType)).Cast<PlanetType>();
            foreach (var planetType in planetTypes)
            {
                var planetDetail = planetType.GetAttribute<PlanetType, PlanetAttribute>();

                if (planetDetail.IsActive)
                {
                    _planets[planetType] = planetDetail;
                }
            }
        }

        /// <summary>
        /// When the module loads, assign a planet Id to every area that is considered to be a planet.
        /// </summary>
        private static void RegisterAreaPlanetIds()
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
        public static PlanetType GetPlanetType(uint area)
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
        public static PlanetAttribute GetPlanetByType(PlanetType type)
        {
            return _planets[type];
        }

        /// <summary>
        /// Retrieves all of the active planets available.
        /// </summary>
        /// <returns>A dictionary containing the active planets.</returns>
        public static Dictionary<PlanetType, PlanetAttribute> GetAllPlanets()
        {
            return _planets.ToDictionary(x => x.Key, y => y.Value);
        }
    }
}
