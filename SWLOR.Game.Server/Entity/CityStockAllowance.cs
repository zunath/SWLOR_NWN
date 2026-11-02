using SWLOR.Game.Server.Service.CityStoreService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// What one city store (a building's, or a market stall theme's) sold of one limited item in one week. It belongs to
    /// the city and the store kind, not to a store object, so picking up or replacing a stall, changing its theme, a
    /// facility level change or a restart never refills it. A new type, so no migration.
    /// </summary>
    public class CityStockAllowance : EntityBase, IStockAllowanceRecord
    {
        public CityStockAllowance()
        {
            CityId = string.Empty;
            ItemResref = string.Empty;
        }

        public CityStockAllowance(string cityId, CityStoreKind kind, string itemResref)
        {
            Id = BuildId(cityId, kind, itemResref);
            CityId = cityId;
            Kind = kind;
            ItemResref = itemResref;
        }

        /// <summary>The property id of the city (or station) the allowance belongs to.</summary>
        [Indexed]
        public string CityId { get; set; }

        /// <summary>The building store or stall theme the allowance belongs to.</summary>
        public CityStoreKind Kind { get; set; }

        public string ItemResref { get; set; }

        /// <inheritdoc />
        public DateTime WeekStartUtc { get; set; }

        /// <inheritdoc />
        public int Sold { get; set; }

        /// <summary>The record id for a city, store and item, so each has exactly one record.</summary>
        public static string BuildId(string cityId, CityStoreKind kind, string itemResref)
        {
            return $"{cityId}:{(int)kind}:{itemResref}";
        }
    }
}
