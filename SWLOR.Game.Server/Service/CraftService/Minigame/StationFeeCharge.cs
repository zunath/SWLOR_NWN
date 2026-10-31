namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>A station fee taken from a player for a craft (a Shipyard's, space revamp), to be credited to the station's treasury later.</summary>
public sealed class StationFeeCharge
{
    public StationFeeCharge(string propertyId, int fee)
    {
        PropertyId = propertyId;
        Fee = fee;
    }

    public string PropertyId { get; }

    public int Fee { get; }
}
