namespace SWLOR.Game.Server.Service.ShuttleService
{
    /// <summary>A directional shuttle route between two stops.</summary>
    public readonly record struct ShuttleRoute(ShuttleStop Origin, ShuttleStop Destination)
    {
        /// <summary>One end is a player station.</summary>
        public bool IsStationRoute => Origin.IsStation || Destination.IsStation;

        /// <summary>One end is a planet city's Starport.</summary>
        public bool IsCityRoute => Origin.IsCityStarport || Destination.IsCityStarport;

        /// <summary>The station end of a station route, or the default stop for a planet route.</summary>
        public ShuttleStop StationStop => Origin.IsStation ? Origin : Destination.IsStation ? Destination : default;

        /// <summary>
        /// A Home Port flight: from a planet's terminal to a city's own Starport, from a station's terminal to a city's Starport on
        /// the station's home planet, or to a player station from a terminal on another planet than its home port. A station's
        /// ordinary shuttle from its home port is not one.
        /// </summary>
        public bool IsHomePort
        {
            get
            {
                if (Origin.IsCityStarport)
                    return false;

                if (Destination.IsCityStarport)
                    return true;

                return !Origin.IsStation && Destination.IsStation && Origin.Planet != Destination.Planet;
            }
        }
    }
}
