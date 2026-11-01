namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A group of hazard waypoints in a space area: minefields or wreck sites of one tier.</summary>
    public class SpaceHazardPlacement
    {
        public string AreaResref { get; init; }
        public string WaypointTag { get; init; }
        public int Count { get; init; }
        public int Tier { get; init; }
    }
}
