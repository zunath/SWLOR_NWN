namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>A trade event that changes faction standing (plan section 13, "Factions").</summary>
    public enum TradeStandingEvent
    {
        Invalid = 0,
        CaughtSmuggling = 1,
        RanFromCustoms = 2,
        AttackedLawfulShip = 3,
        RaidedConvoy = 4,
        DeliveredFreight = 5,
    }
}
