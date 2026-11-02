using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// Where a situation's treasury change lands. Until the restricted-funds treasury exists, gains and costs both use the
    /// plain treasury.
    /// </summary>
    public static class SituationTreasury
    {
        /// <summary>
        /// The only place situations touch the treasury. Switch gains to <c>CityTreasury.CreditRestricted</c> when the
        /// restricted-funds treasury merges; costs keep coming out of the whole treasury.
        /// </summary>
        public static void Apply(WorldProperty city, int change)
        {
            city.Treasury += change;
        }
    }
}
