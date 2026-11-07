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
            Id = key;
            Key = key;
        }

        /// <summary>The CD key (or, for a character with none, <c>player:{id}</c>) the allowance belongs to; also the record id.</summary>
        public string Key { get; set; }

        /// <inheritdoc />
        public DateTime WeekStartUtc { get; set; }

        /// <inheritdoc />
        public int Sold { get; set; }
    }
}
