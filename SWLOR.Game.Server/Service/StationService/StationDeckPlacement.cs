namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>Where a structure goes on a station deck: its slot for a civic building, otherwise where it was placed.</summary>
    public readonly record struct StationDeckPlacement(StationDeckRefusal Refusal, StationDeckSlotType Slot, float X, float Y);
}
