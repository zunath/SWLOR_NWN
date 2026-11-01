using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.ShuttleService
{
    /// <summary>
    /// One end of a shuttle flight: a planet's NPC starport, or a player station (plan section 25). A station stop carries
    /// its home planet, the NPC dock its shuttle runs to, and whether it sits in Wild Space.
    /// </summary>
    public readonly record struct ShuttleStop(PlanetType Planet, string StationId, bool IsWildSpace)
    {
        public bool IsStation => !string.IsNullOrEmpty(StationId);

        public static ShuttleStop ForPlanet(PlanetType planet) => new(planet, string.Empty, false);

        public static ShuttleStop ForStation(string stationId, PlanetType homePlanet, bool isWildSpace) => new(homePlanet, stationId ?? string.Empty, isWildSpace);
    }
}
