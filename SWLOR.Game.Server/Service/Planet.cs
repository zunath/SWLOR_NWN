using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;

namespace SWLOR.Game.Server.Service
{
    public static class Planet
    {
        private const string PlanetTypeIdVariable = "PLANET_TYPE_ID";
        private static readonly Dictionary<PlanetType, PlanetAttribute> _planets = new();
        private static readonly Dictionary<string, PlanetType> _planetAreaResrefs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["area"] = PlanetType.Viscara,
            ["canyon_001"] = PlanetType.Tatooine,
            ["veles_exterior"] = PlanetType.Viscara,
            ["viscarawildlands"] = PlanetType.Viscara
        };
        private static readonly HashSet<string> _spaceAreaResrefs = new(StringComparer.OrdinalIgnoreCase)
        {
            "dantooineorbit",
            "hutlar_orbit",
            "moncalaorbit",
            "prefab_space",
            "prefab_space003",
            "prefab_space004",
            "prefab_space2",
            "space_dathomir",
            "space_derelict_k",
            "space_korriban",
            "space_midrim1",
            "space_midrim2",
            "tatooineorbit",
            "viscaraorbit"
        };
        private static readonly Dictionary<string, PlanetType> _planetAreaResrefPrefixes = new()
        {
            ["anc_"] = PlanetType.Tatooine,
            ["anchor_"] = PlanetType.Tatooine,
            ["ar_scor_k"] = PlanetType.Korriban,
            ["coxxian_"] = PlanetType.Viscara,
            ["czs220_"] = PlanetType.CZ220,
            ["dan_"] = PlanetType.Dantooine,
            ["dath"] = PlanetType.Dathomir,
            ["druz_"] = PlanetType.Viscara,
            ["fosz"] = PlanetType.Viscara,
            ["hutlar_"] = PlanetType.Hutlar,
            ["jeditemp_"] = PlanetType.Viscara,
            ["korr"] = PlanetType.Korriban,
            ["manda_"] = PlanetType.Viscara,
            ["moncala"] = PlanetType.MonCala,
            ["moseis_"] = PlanetType.Tatooine,
            ["nanostation"] = PlanetType.CZ220,
            ["scor_k"] = PlanetType.Korriban,
            ["smesks_"] = PlanetType.Tatooine,
            ["sol_hutlar"] = PlanetType.Hutlar,
            ["sol_mandaloriani"] = PlanetType.Hutlar,
            ["sol_swamp"] = PlanetType.Viscara,
            ["tat_"] = PlanetType.Tatooine,
            ["tochee_"] = PlanetType.Tatooine,
            ["tosche_"] = PlanetType.Tatooine,
            ["v_"] = PlanetType.Viscara,
            ["valkorr"] = PlanetType.Korriban,
            ["veles"] = PlanetType.Viscara,
            ["viscara"] = PlanetType.Viscara,
            ["vleg"] = PlanetType.Viscara,
            ["ziyhut"] = PlanetType.Hutlar
        };

        /// <summary>
        /// When the module loads, cache relevant data needed by the Planet service.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
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
                var planetType = ResolvePlanetTypeByArea(area);
                if (planetType != PlanetType.Invalid)
                    SetLocalInt(area, PlanetTypeIdVariable, (int)planetType);
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
            if (!GetIsObjectValid(area))
                return PlanetType.Invalid;

            if (_planets.Count <= 0)
                CachePlanets();

            var planetTypeId = GetLocalInt(area, PlanetTypeIdVariable);
            var planetType = (PlanetType)planetTypeId;
            if (_planets.ContainsKey(planetType))
                return planetType;

            var resolvedPlanetType = ResolvePlanetTypeByArea(area);
            if (resolvedPlanetType != PlanetType.Invalid)
            {
                SetLocalInt(area, PlanetTypeIdVariable, (int)resolvedPlanetType);
                return resolvedPlanetType;
            }

            return PlanetType.Invalid;
        }

        /// <summary>
        /// Retrieves a planet type from an area resref when an area object is not available.
        /// </summary>
        /// <param name="areaResref">The area resref to check.</param>
        /// <returns>A planet type. Returns PlanetType.Invalid on failure.</returns>
        public static PlanetType GetPlanetTypeByAreaResref(string areaResref)
        {
            return ResolvePlanetTypeByAreaResref(areaResref);
        }

        private static PlanetType ResolvePlanetTypeByArea(uint area)
        {
            var areaName = GetName(area);
            var planetType = ResolvePlanetTypeByAreaName(areaName);
            if (planetType != PlanetType.Invalid)
                return planetType;

            if (GetLocalBool(area, "SPACE") || areaName.StartsWith("Space -"))
                return PlanetType.Invalid;

            return ResolvePlanetTypeByAreaResref(GetResRef(area));
        }

        private static PlanetType ResolvePlanetTypeByAreaName(string areaName)
        {
            foreach (var (type, detail) in _planets)
            {
                if (!areaName.StartsWith(detail.Prefix))
                    continue;

                return type;
            }

            return PlanetType.Invalid;
        }

        private static PlanetType ResolvePlanetTypeByAreaResref(string areaResref)
        {
            if (string.IsNullOrWhiteSpace(areaResref))
                return PlanetType.Invalid;

            if (_spaceAreaResrefs.Contains(areaResref))
                return PlanetType.Invalid;

            if (_planetAreaResrefs.TryGetValue(areaResref, out var exactPlanetType))
                return exactPlanetType;

            foreach (var (resrefPrefix, planetType) in _planetAreaResrefPrefixes)
            {
                if (areaResref.StartsWith(resrefPrefix, StringComparison.OrdinalIgnoreCase))
                    return planetType;
            }

            return PlanetType.Invalid;
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
