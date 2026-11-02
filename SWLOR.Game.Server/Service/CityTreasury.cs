using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// A city's money in motion (Player City Revamp, "City finances"): upkeep and Edict charges that spend restricted
    /// funds first, restricted income, and escrow for bounties, tournaments and projects. The rules live in
    /// <see cref="CityTreasuryRules"/>. Each method changes the city record it is handed and saves that record once, so
    /// anything else the caller changed on it is saved in the same write and a restart cannot split a transition.
    /// </summary>
    public static class CityTreasury
    {
        /// <summary>
        /// Spends credits from the city and saves it. Nothing changes when the city cannot pay. A caller that saves the city
        /// itself in an order that matters passes <paramref name="save"/> as false.
        /// </summary>
        public static TreasurySpend Spend(WorldProperty city, long amount, bool restrictedFirst = true, bool save = true)
        {
            var spend = CityTreasuryRules.Spend(city, amount, restrictedFirst);
            if (spend.Succeeded && save)
                DB.Set(city);

            return spend;
        }

        /// <summary>Adds income that cannot be withdrawn and saves the city, unless <paramref name="save"/> is false.</summary>
        public static bool CreditRestricted(WorldProperty city, long amount, bool save = true)
        {
            if (!CityTreasuryRules.CreditRestricted(city, amount))
                return false;

            if (save)
                DB.Set(city);

            return true;
        }

        /// <summary>Moves credits from the city into escrow under an id and saves the city.</summary>
        public static TreasurySpend PostEscrow(WorldProperty city, string escrowId, long amount, bool restrictedFirst)
        {
            var spend = CityTreasuryRules.PostEscrow(city, escrowId, amount, restrictedFirst);
            if (spend.Succeeded)
                DB.Set(city);

            return spend;
        }

        /// <summary>Holds a player's entry fee in an escrow entry and saves the city.</summary>
        public static bool AddEntryFee(WorldProperty city, string escrowId, string playerId, long amount)
        {
            if (!CityTreasuryRules.AddEntryFee(city, escrowId, playerId, amount))
                return false;

            DB.Set(city);
            return true;
        }

        /// <summary>Takes an escrow entry out of the city for whoever earned it and saves the city.</summary>
        public static EscrowRelease PayOutEscrow(WorldProperty city, string escrowId)
        {
            var release = CityTreasuryRules.PayOutEscrow(city, escrowId);
            if (release.Found)
                DB.Set(city);

            return release;
        }

        /// <summary>Returns an escrow entry's portions to the balances they came from and saves the city.</summary>
        public static EscrowRelease RefundEscrow(WorldProperty city, string escrowId)
        {
            var release = CityTreasuryRules.RefundEscrow(city, escrowId);
            if (release.Found)
                DB.Set(city);

            return release;
        }

        /// <summary>
        /// Pays the whole upkeep bill from the treasury, restricted funds first, and saves the city. An Edict that the
        /// payment lifts out of disrepair adds its part-week to the bill the city owes next.
        /// </summary>
        /// <returns>False when no bill is owed or the city cannot pay it.</returns>
        public static bool PayUpkeep(WorldProperty city, DateTime nowUtc)
        {
            if (city.Upkeep <= 0 || !CityTreasuryRules.CanSpend(city, city.Upkeep))
                return false;

            var billedBefore = Edict.GetBilledTypes(city);
            CityTreasuryRules.PayUpkeep(city);
            Edict.AddResumedBilling(city, billedBefore, nowUtc);

            DB.Set(city);
            return true;
        }
    }
}
