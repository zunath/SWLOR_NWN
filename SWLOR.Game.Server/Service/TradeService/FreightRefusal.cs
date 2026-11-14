namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>Why a freight contract could not be accepted or delivered.</summary>
    public enum FreightRefusal
    {
        None = 0,
        OfferGone = 1,
        AlreadyTaken = 2,
        TooManyContracts = 3,
        NoCargoBay = 4,
        NotEnoughRoom = 5,
        NotEnoughCredits = 6,
        WrongExchange = 7,
        LoadNotAboard = 8,
        ShipNotDocked = 9,
        StandingTooLow = 10,
    }
}
