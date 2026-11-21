namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>Why a purchase or sale at an exchange did not happen.</summary>
    public enum TradeRefusal
    {
        None = 0,
        InvalidAmount = 1,
        NotTradedHere = 2,
        NoCargoBay = 3,
        CargoBayFull = 4,
        NotEnoughCredits = 5,
        NoneAboard = 6,
        ShipNotDocked = 7,
        NoExchangeHere = 8,
        HoldingsOnly = 9,
    }
}
