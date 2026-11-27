namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>Why a structure cannot be placed on a station deck.</summary>
    public enum StationDeckRefusal
    {
        None = 0,
        SlotTaken = 1,
        TooCloseToSlot = 2,
    }
}
