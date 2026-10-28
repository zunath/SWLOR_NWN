namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// How an authority's faction regards a pilot (plan section 13, "Factions"). Trusted and above cut customs scans and
    /// improve prices; Hostile makes the authority's customs patrol attack on sight.
    /// </summary>
    public enum TradeStandingTier
    {
        Hostile = -1,
        Neutral = 0,
        Trusted = 1,
        Honored = 2,
        Exalted = 3,
    }
}
