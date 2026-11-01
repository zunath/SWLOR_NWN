namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>Why an agent mission or a token store purchase is refused.</summary>
    public enum AgentRefusal
    {
        None = 0,
        OfferGone = 1,
        AlreadyTaken = 2,
        TooManyMissions = 3,
        Hostile = 4,
        StandingTooLow = 5,
        NoIntel = 6,
        NoShipDocked = 7,
        NotEnoughRoom = 8,
        NotEnoughTokens = 9,
    }
}
