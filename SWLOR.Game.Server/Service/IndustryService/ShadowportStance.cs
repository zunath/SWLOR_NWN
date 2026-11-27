namespace SWLOR.Game.Server.Service.IndustryService
{
    /// <summary>How a city's Starport exchange treats the goods its planet's authority bans.</summary>
    public enum ShadowportStance
    {
        /// <summary>The exchange is an open port, or not a city's: it keeps the shared market's own rules.</summary>
        OpenPort = 0,

        /// <summary>A lawful port without the Shadowport Edict: contraband sells low, with a high chance of a customs bust.</summary>
        Lawful = 1,

        /// <summary>A lawful port with the Shadowport Edict: contraband sells near black-market prices, with a small chance of a bust.</summary>
        Shadowport = 2,
    }
}
