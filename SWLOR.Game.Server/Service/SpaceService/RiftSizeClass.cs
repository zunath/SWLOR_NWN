namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>How big a rift is: it caps the largest hull through it (plan section 23). Held in memory, never saved.</summary>
    public enum RiftSizeClass
    {
        Invalid = 0,
        Small = 1,
        Medium = 2,
        Large = 3,
    }
}
