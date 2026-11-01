namespace SWLOR.Game.Server.Service.ShuttleService
{
    /// <summary>A directional shuttle route between two stops.</summary>
    public readonly record struct ShuttleRoute(ShuttleStop Origin, ShuttleStop Destination)
    {
        /// <summary>One end is a player station.</summary>
        public bool IsStationRoute => Origin.IsStation || Destination.IsStation;

        /// <summary>The station end of a station route, or the default stop for a planet route.</summary>
        public ShuttleStop StationStop => Origin.IsStation ? Origin : Destination.IsStation ? Destination : default;
    }
}
