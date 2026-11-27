namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Why a ship cannot board a derelict or send a salvage team to it (plan section 28).</summary>
    public enum DerelictRefusal
    {
        None = 0,
        NotADerelict = 1,
        NotBoardable = 2,
        Looted = 3,
        NotLocated = 4,
        VisitorTooLarge = 5,
        OutOfRange = 6,
        InCombat = 7,
        DefensesLeft = 8,
        OtherParty = 9,
        Full = 10,
        SalvageUnderway = 11,
        BoardingUnderway = 12,
        NoDroids = 13,
        TeamAlreadyOut = 14,
    }
}
