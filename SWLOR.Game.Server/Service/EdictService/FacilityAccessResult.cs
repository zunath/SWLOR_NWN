namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>What a city's Bank or Starport does with a character (Player City Revamp, "Who may use the Bank and Starport").</summary>
    public enum FacilityAccessResult
    {
        /// <summary>The character may use the facility.</summary>
        Allowed = 0,

        /// <summary>The city has an Embargo against the character's city.</summary>
        Embargoed = 1,

        /// <summary>The character owes taxes (to this city, or to the home city of an allied citizen).</summary>
        OwesTaxes = 2,

        /// <summary>Not a citizen, not an ally, and Open Gates is not in effect.</summary>
        NotWelcome = 3
    }
}
