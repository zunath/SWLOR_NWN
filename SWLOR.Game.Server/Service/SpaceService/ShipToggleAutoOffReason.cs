namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Why a toggled module turned itself off.
    /// </summary>
    public enum ShipToggleAutoOffReason
    {
        None = 0,
        ShipLeftSpace = 1,
        TargetLost = 2,
        InvalidTarget = 3,
        OutOfRange = 4,
        ValidationFailed = 5,
        NoCapacitor = 6,
    }
}
