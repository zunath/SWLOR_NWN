using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.CityTreasuryService
{
    /// <summary>
    /// How a city's money moves between its two balances and escrow (Player City Revamp, "City finances"). The
    /// ordinary balance is <see cref="WorldProperty.Treasury"/>; <see cref="WorldProperty.RestrictedTreasury"/> holds
    /// income that cannot be withdrawn. Every method changes only the city record it is given, so a caller saves the
    /// record once and a restart cannot split a transition. No database or engine calls.
    /// </summary>
    public static class CityTreasuryRules
    {
        /// <summary>The most a spend can take: both balances when restricted funds go first, otherwise the treasury alone.</summary>
        public static long GetSpendable(WorldProperty city, bool restrictedFirst = true)
        {
            return restrictedFirst ? city.Treasury + city.RestrictedTreasury : city.Treasury;
        }

        public static bool CanSpend(WorldProperty city, long amount, bool restrictedFirst = true)
        {
            return amount >= 0 && amount <= GetSpendable(city, restrictedFirst);
        }

        /// <summary>
        /// Takes credits from the city. Upkeep, Edicts and projects spend restricted funds first; bounties and prizes take
        /// only the ordinary treasury. The result says how much came from each balance.
        /// </summary>
        public static TreasurySpend Spend(WorldProperty city, long amount, bool restrictedFirst = true)
        {
            if (!CanSpend(city, amount, restrictedFirst))
                return TreasurySpend.Failed;

            var restricted = restrictedFirst ? Math.Min(city.RestrictedTreasury, amount) : 0;
            var unrestricted = amount - restricted;

            city.RestrictedTreasury -= restricted;
            city.Treasury -= unrestricted;
            return new TreasurySpend(true, restricted, unrestricted);
        }

        /// <summary>Adds income that cannot be withdrawn. Returns false for an amount that is not positive.</summary>
        public static bool CreditRestricted(WorldProperty city, long amount)
        {
            if (amount <= 0)
                return false;

            city.RestrictedTreasury += amount;
            return true;
        }

        /// <summary>
        /// Moves credits from the city into escrow under an id, remembering which balance each portion came from. Refused
        /// when the id already holds an escrow entry, so repeating a post after a restart cannot hold the money twice.
        /// </summary>
        public static TreasurySpend PostEscrow(WorldProperty city, string escrowId, long amount, bool restrictedFirst)
        {
            if (string.IsNullOrWhiteSpace(escrowId) || city.Escrow.ContainsKey(escrowId))
                return TreasurySpend.Failed;

            var spend = Spend(city, amount, restrictedFirst);
            if (!spend.Succeeded)
                return spend;

            city.Escrow[escrowId] = new CityEscrowEntry
            {
                Restricted = spend.Restricted,
                Unrestricted = spend.Unrestricted
            };
            return spend;
        }

        /// <summary>
        /// Holds a player's entry fee in an existing escrow entry. It belongs to that player and never joins either
        /// balance. Returns false when the entry does not exist or the amount is not positive.
        /// </summary>
        public static bool AddEntryFee(WorldProperty city, string escrowId, string playerId, long amount)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(playerId) || !city.Escrow.TryGetValue(escrowId ?? string.Empty, out var entry))
                return false;

            entry.PlayerFees[playerId] = entry.PlayerFees.GetValueOrDefault(playerId) + amount;
            return true;
        }

        /// <summary>
        /// Takes an escrow entry out of the city and hands back what it held. The credits leave the city for whoever earned
        /// them (a bounty's claimant, a prize's winner, a completed project), so neither balance changes.
        /// </summary>
        public static EscrowRelease PayOutEscrow(WorldProperty city, string escrowId)
        {
            return Release(city, escrowId, returnToBalances: false);
        }

        /// <summary>
        /// Takes an escrow entry out of the city and returns each portion to the balance it came from, so cancelling a
        /// project cannot turn restricted income into credits a mayor can withdraw. Entry fees are handed back in the
        /// release for the caller to return to the players who paid them.
        /// </summary>
        public static EscrowRelease RefundEscrow(WorldProperty city, string escrowId)
        {
            return Release(city, escrowId, returnToBalances: true);
        }

        private static EscrowRelease Release(WorldProperty city, string escrowId, bool returnToBalances)
        {
            if (string.IsNullOrWhiteSpace(escrowId) || !city.Escrow.Remove(escrowId, out var entry))
                return EscrowRelease.NotFound;

            if (returnToBalances)
            {
                city.RestrictedTreasury += entry.Restricted;
                city.Treasury += entry.Unrestricted;
            }

            return new EscrowRelease(
                true,
                entry.Restricted,
                entry.Unrestricted,
                new Dictionary<string, long>(entry.PlayerFees));
        }

        /// <summary>
        /// Pays the whole upkeep bill, restricted funds first, and clears the disrepair deadline that went with it.
        /// Returns false, changing nothing, when no bill is owed or the city cannot pay it.
        /// </summary>
        public static bool PayUpkeep(WorldProperty city)
        {
            if (city.Upkeep <= 0 || !CanSpend(city, city.Upkeep))
                return false;

            Spend(city, city.Upkeep);
            city.Upkeep = 0;
            city.Dates.Remove(PropertyDateType.DisrepairDestruction);
            return true;
        }
    }
}
