namespace SWLOR.Game.Server.Service.HoldingService
{
    /// <summary>The events rolled every few days per holding (plan section 26).</summary>
    public enum HoldingEventType
    {
        None = 0,
        Boom = 1,
        Setback = 2,
        Raid = 3,
        Customs = 4,
    }
}
