using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Planet
    {
        private static readonly Dictionary<string, PlanetType> _prefixMappings = new()
        {
            {"Viscara - ", PlanetType.Viscara},
            {"CZ-220 - ", PlanetType.CZ220},
            {"Hutlar - ", PlanetType.Hutlar},
            {"Tatooine - ", PlanetType.Tatooine},
            {"Mon Cala -", PlanetType.MonCala}
        };

        /// <summary>
        /// When the module loads, assign a planet Id to every area that is considered to be a planet.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void RegisterAreaPlanetIds()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var areaName = GetName(area);

                foreach (var (prefix, planetType) in _prefixMappings)
                {
                    if (areaName.StartsWith(prefix))
                    {
                        SetLocalInt(area, "PLANET_TYPE_ID", (int)planetType);
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
    }
}
