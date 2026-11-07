namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Why a ship cannot dock with another (plan section 10, "Ship-to-ship docking").</summary>
    public enum ShipDockRefusal
    {
        None = 0,
        SameShip = 1,
        HostTooSmall = 2,
        VisitorTooLarge = 3,
        NoPermission = 4,
        OutOfRange = 5,
        HostInCombat = 6,
        VisitorInCombat = 7,
        HostFull = 8,
    }
}
