using SWLOR.Game.Server.Service.CityStoreService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// How many Civic Exchange crates one CD key bought in one week. It belongs to the CD key, not to a character or a city, so
    /// switching characters or cities never refills it. The count starts over at the weekly boundary (<see cref="CityWeek"/>)
    /// with no job running. A new type, so no migration.
    /// </summary>
    public class CivicExchangeAllowance : EntityBase, IStockAllowanceRecord
    {
        public CivicExchangeAllowance()
        {
            Key = string.Empty;
        }

        public CivicExchangeAllowance(string key)
        {
            Id = KeyFor(key);
            Key = key;
        }

        /// <summary>
        /// The DB cache holds one entity per id for the frame, whatever its type, so the id is prefixed: the bare CD key
        /// is also the id of the account.
        /// </summary>
        public static string KeyFor(string key) => "civic-exchange:" + key;

        /// <summary>The CD key (or, for a character with none, <c>player:{id}</c>) the allowance belongs to. The record id is <see cref="KeyFor"/> of it.</summary>
        public string Key { get; set; }

        /// <inheritdoc />
        public DateTime WeekStartUtc { get; set; }

        /// <inheritdoc />
        public int Sold { get; set; }
    }
}
