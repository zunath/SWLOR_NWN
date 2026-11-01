namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A rift size class: how often it opens, the largest hull through it and its mass budget.</summary>
    public class RiftSizeDetail
    {
        public int Weight { get; init; }
        public ShipSize MaxHull { get; init; }
        public int MassBudget { get; init; }
    }
}
