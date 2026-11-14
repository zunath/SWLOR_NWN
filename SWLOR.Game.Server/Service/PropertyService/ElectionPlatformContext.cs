namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// What the election window's platform step knows about the city and the candidate: the numbers its cost preview
    /// and City Management warning are written from. Read once when the step opens.
    /// </summary>
    /// <param name="CityLevel">The city's level now.</param>
    /// <param name="IsWildSpaceStation">Whether the city is a Wild Space station, where planet-based Edicts are not offered.</param>
    /// <param name="CitizenCount">The city's citizens.</param>
    /// <param name="BaseUpkeep">The city's weekly base and facility upkeep.</param>
    /// <param name="CurrentEdictUpkeep">What the Edicts in force add to the weekly bill.</param>
    /// <param name="Treasury">The treasury balance the mayor can withdraw.</param>
    /// <param name="RestrictedTreasury">The balance that cannot be withdrawn, which upkeep and Edicts spend first.</param>
    /// <param name="CitizenshipTax">The citizenship tax each citizen owes a week.</param>
    /// <param name="CandidateMayorLevel">The city level the candidate's City Management rank supports.</param>
    /// <param name="CandidateSupportedLevel">The level the weekly check would drop the city to under the candidate, never above
    /// the current level.</param>
    /// <param name="BaselineSupportedLevel">The level the check would drop the city to whoever is mayor, from the citizen count alone.</param>
    /// <param name="IsOpenPort">Whether the city's Starport exchange is an open port, where Shadowport is not offered.</param>
    public sealed record ElectionPlatformContext(
        int CityLevel,
        bool IsWildSpaceStation,
        int CitizenCount,
        int BaseUpkeep,
        int CurrentEdictUpkeep,
        long Treasury,
        long RestrictedTreasury,
        int CitizenshipTax,
        int CandidateMayorLevel,
        int CandidateSupportedLevel,
        int BaselineSupportedLevel,
        bool IsOpenPort = false);
}
