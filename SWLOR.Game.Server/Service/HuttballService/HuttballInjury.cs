namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// The result of a roll on the injury table.
    /// </summary>
    public enum HuttballInjury
    {
        /// <summary>1-10: knocked down.</summary>
        KnockedDown = 1,
        /// <summary>11-15: out until halftime.</summary>
        Injured = 2,
        /// <summary>16-19: out for the match.</summary>
        SevereInjury = 3,
        /// <summary>20: out for the match. Announced only; nothing stays on the character.</summary>
        Maimed = 4,
    }
}
