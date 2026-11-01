namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>The lawful power that runs a dock and keeps a contraband list (plan section 13). None is an open port.</summary>
    public enum TradeAuthority
    {
        None = 0,
        Republic = 1,
        Czerka = 2,
        Sith = 3,
    }
}
