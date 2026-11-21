using SWLOR.Game.Server.Service.CityStoreService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// What one traveler sold of one limited item in one week. It belongs to the traveler, not to the city that hosts it
    /// or the stock on its shelf, so moving to another city or a restart never refills it. A new type, so no migration.
    /// </summary>
    public class TravelerStockAllowance : EntityBase, IStockAllowanceRecord
    {
        public TravelerStockAllowance()
        {
            TravelerKey = string.Empty;
            ItemResref = string.Empty;
        }

        public TravelerStockAllowance(string travelerKey, string itemResref)
        {
            Id = BuildId(travelerKey, itemResref);
            TravelerKey = travelerKey;
            ItemResref = itemResref;
        }

        /// <summary>Which traveler: the name of its <c>TravelerType</c> member.</summary>
        [Indexed]
        public string TravelerKey { get; set; }

        public string ItemResref { get; set; }

        /// <inheritdoc />
        public DateTime WeekStartUtc { get; set; }

        /// <inheritdoc />
        public int Sold { get; set; }

        /// <summary>The record id for a traveler and item, so each has exactly one record.</summary>
        public static string BuildId(string travelerKey, string itemResref)
        {
            return $"{travelerKey}:{itemResref}";
        }
    }
}
