namespace SWLOR.Game.Server.Service.CivicExchangeService
{
    /// <summary>Whether a character may use a city's Civic Exchange: citizens in good standing, of that city.</summary>
    public enum CivicExchangeAccess
    {
        /// <summary>A citizen of this city in good standing.</summary>
        Allowed = 0,

        /// <summary>Not a citizen of this city (a visitor, or a citizen of another city or its ally).</summary>
        NotACitizen = 1,

        /// <summary>A citizen who owes taxes.</summary>
        OwesTaxes = 2,

        /// <summary>A citizen for less than 7 days.</summary>
        TooNew = 3
    }
}
