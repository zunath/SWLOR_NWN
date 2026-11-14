namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Why a ship cannot cross a rift.</summary>
    public enum RiftPassageRefusal
    {
        None = 0,
        Collapsed = 1,
        HullTooLarge = 2,
        MassSpent = 3,
    }
}
