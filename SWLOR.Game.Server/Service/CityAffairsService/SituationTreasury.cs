using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityTreasuryService;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// Where a situation's treasury change lands: gains go into the restricted treasury, which cannot be withdrawn, and costs
    /// are paid from restricted funds first.
    /// </summary>
    public static class SituationTreasury
    {
        /// <summary>
        /// The only place situations touch the treasury. It changes the city in memory and does not save it: the caller
        /// saves the situation first and then the city, so a restart never applies a change twice. The change was already
        /// limited to what the city can pay.
        /// </summary>
        public static void Apply(WorldProperty city, int change)
        {
            if (change > 0)
                CityTreasury.CreditRestricted(city, change, save: false);
            else if (change < 0)
                CityTreasury.Spend(city, -change, restrictedFirst: true, save: false);
        }

        /// <summary>The credits a situation can spend: the treasury and the restricted treasury together.</summary>
        public static long GetSpendable(WorldProperty city)
        {
            return CityTreasuryRules.GetSpendable(city);
        }
    }
}
