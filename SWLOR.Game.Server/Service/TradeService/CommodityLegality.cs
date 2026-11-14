namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// Legal goods trade everywhere. Restricted goods are banned by some lawful authorities and contraband by all of
    /// them. A lawful exchange trades a banned good only when it makes or needs it.
    /// </summary>
    public enum CommodityLegality
    {
        Legal = 0,
        Restricted = 1,
        Contraband = 2,
    }
}
