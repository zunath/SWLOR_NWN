using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>
    /// A fixed point in a space area where one player station can be founded (plan section 25). The area places a
    /// waypoint tagged <see cref="Tag"/> there; ships launching from the station appear at it.
    /// </summary>
    public class StationAnchor
    {
        public string Id { get; init; }
        public string Tag { get; init; }
        public string AreaResref { get; init; }
        public string SystemName { get; init; }
        public float X { get; init; }
        public float Y { get; init; }

        /// <summary>The NPC dock the station's shuttle runs to: its orbit's dock, or the nearest open port in Wild Space.</summary>
        public PlanetType HomePlanet { get; init; }

        /// <summary>The NPC exchange the station's Trade Exchange trades through.</summary>
        public TradeExchangeType Exchange { get; init; }

        public bool IsWildSpace { get; init; }
    }
}
