namespace SWLOR.Game.Server.Service.IndustryService
{
    /// <summary>Why a holding cannot be moved to a dock by the Planetary Depot.</summary>
    public enum DepotRefusal
    {
        None = 0,
        NotYourHolding = 1,

        /// <summary>The dock sells and serves the holdings of another planet.</summary>
        WrongPlanet = 2,
        AlreadyHere = 3,
        StarportTooLow = 4,

        /// <summary>A city Starport's Depot serves only its own citizens.</summary>
        NotACitizen = 5,
        TooSoon = 6,

        /// <summary>A payment for the holding is still being settled.</summary>
        PaymentPending = 7,

        /// <summary>Both docks are NPC docks: the Depot moves holdings only to or from a city Starport.</summary>
        NotACityMove = 8,
    }
}
