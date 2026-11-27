namespace SWLOR.Game.Server.Service.ShuttleService
{
    /// <summary>Whether a character may book a Home Port flight to a city's Starport (Player City Revamp, "Home Port").</summary>
    public enum HomePortAccess
    {
        /// <summary>The character may book the flight.</summary>
        Allowed = 0,

        /// <summary>The character does not hold the CZ-220 Shuttle Pass every flight needs.</summary>
        NoShuttlePass = 1,

        /// <summary>The city has an Embargo against the character's city.</summary>
        Embargoed = 2,

        /// <summary>The character owes taxes to their own city.</summary>
        OwesTaxes = 3,

        /// <summary>The character is neither a citizen of the city nor a citizen of its ally.</summary>
        NotEligible = 4
    }
}
