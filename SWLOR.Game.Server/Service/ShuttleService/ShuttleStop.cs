using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.ShuttleService
{
    /// <summary>
    /// One end of a shuttle flight: a planet's NPC starport, a player station (plan section 25), or a planet city's Starport
    /// (Player City Revamp, "Home Port"). A station stop carries its home planet, the NPC dock its shuttle runs to, and whether
    /// it sits in Wild Space. A city Starport stop carries the planet the city stands on and the city's property id; it is only
    /// ever a destination, and it is kept apart from <see cref="IsStation"/> so it is never read as a station or as the planet's
    /// NPC port.
    /// </summary>
    public readonly record struct ShuttleStop(PlanetType Planet, string StationId, bool IsWildSpace, string CityId = "")
    {
        public bool IsStation => !string.IsNullOrEmpty(StationId);

        /// <summary>The stop is a planet city's own Starport, which only a Home Port flight lands at.</summary>
        public bool IsCityStarport => !string.IsNullOrEmpty(CityId);

        /// <summary>The property id of the city this stop belongs to: the station for a station stop, the city for a city Starport.</summary>
        public string HomeCityId => IsStation ? StationId : IsCityStarport ? CityId : string.Empty;

        public static ShuttleStop ForPlanet(PlanetType planet) => new(planet, string.Empty, false, string.Empty);

        public static ShuttleStop ForStation(string stationId, PlanetType homePlanet, bool isWildSpace) => new(homePlanet, stationId ?? string.Empty, isWildSpace, string.Empty);

        public static ShuttleStop ForCityStarport(PlanetType planet, string cityId) => new(planet, string.Empty, false, cityId ?? string.Empty);
    }
}
