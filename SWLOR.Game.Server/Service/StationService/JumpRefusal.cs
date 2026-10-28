namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>Why a Jump Beacon cannot jump the ship, from <see cref="StationBuildingRules.GetJumpRefusal"/>.</summary>
    public enum JumpRefusal
    {
        None = 0,
        ShipNotDocked = 1,
        CannotPilot = 2,
        WildSpace = 3,
        SameStation = 4,
        OriginNotPermitted = 5,
        DestinationNotPermitted = 6,
        HullTooLarge = 7,
        InsufficientFuel = 8,
        UnknownSize = 9,
    }
}
