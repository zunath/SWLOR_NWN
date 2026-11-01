namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// A dock's commodity exchange (plan section 13). Persisted by name in the TradeExchangeState id, so a rename needs
    /// a data migration.
    /// </summary>
    public enum TradeExchangeType
    {
        Invalid = 0,
        Viscara = 1,
        CZ220 = 2,
        MonCala = 3,
        Hutlar = 4,
        Tatooine = 5,
        SmugglersMoon = 6,
        Korriban = 7,
        Dathomir = 8,
        Dantooine = 9,
        Eshan = 10,
    }
}
