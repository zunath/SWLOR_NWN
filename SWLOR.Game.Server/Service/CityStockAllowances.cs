using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The saved weekly allowances for limited stock: a city store's (<see cref="CityStockAllowance"/>) and a traveler's
    /// (<see cref="TravelerStockAllowance"/>). The rules live in <see cref="StockAllowanceRules"/>; this reads and writes
    /// the records. A store rebuilds its shelf from them whenever it is created, and each sale is recorded, so nothing
    /// that recreates a store ever refills an allowance.
    /// </summary>
    public static class CityStockAllowances
    {
        // City stores

        /// <summary>How many of a limited item the city's store has left this week.</summary>
        public static int GetRemaining(string cityId, CityStoreKind kind, CityStoreLimitedItem item, DateTime nowUtc)
        {
            var record = DB.Get<CityStockAllowance>(CityStockAllowance.BuildId(cityId, kind, item.Resref));
            return StockAllowanceRules.GetRemaining(item.WeeklyLimit, record, CityWeek.GetWeekStart(nowUtc));
        }

        /// <summary>What a store of this kind and level puts on its shelf from its limited items right now.</summary>
        public static IReadOnlyList<CityStoreShelfEntry> PlanShelf(string cityId, CityStoreDetail detail, int level, DateTime nowUtc)
        {
            return StockAllowanceRules.PlanShelf(
                detail.LimitedItems,
                level,
                resref => DB.Get<CityStockAllowance>(CityStockAllowance.BuildId(cityId, detail.Kind, resref)),
                CityWeek.GetWeekStart(nowUtc));
        }

        /// <summary>Records a sale of a limited item against the city's allowance.</summary>
        public static void RecordSale(string cityId, CityStoreKind kind, string resref, int quantity, DateTime nowUtc)
        {
            var id = CityStockAllowance.BuildId(cityId, kind, resref);
            var record = DB.Get<CityStockAllowance>(id) ?? new CityStockAllowance(cityId, kind, resref);

            StockAllowanceRules.RecordSale(record, quantity, CityWeek.GetWeekStart(nowUtc));
            DB.Set(record);
        }

        /// <summary>Removes every allowance a removed city held.</summary>
        public static void DeleteAll(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return;

            var query = new DBQuery<CityStockAllowance>()
                .AddFieldSearch(nameof(CityStockAllowance.CityId), cityId, false);
            var count = checked((int)DB.SearchCount(query));
            if (count <= 0)
                return;

            foreach (var record in DB.Search(query.AddPaging(count, 0)).ToList())
                DB.Delete<CityStockAllowance>(record.Id);
        }

        // Travelers

        /// <summary>How many of a limited item the traveler has left this week.</summary>
        public static int GetTravelerRemaining(string travelerKey, string resref, int weeklyLimit, DateTime nowUtc)
        {
            var record = DB.Get<TravelerStockAllowance>(TravelerStockAllowance.BuildId(travelerKey, resref));
            return StockAllowanceRules.GetRemaining(weeklyLimit, record, CityWeek.GetWeekStart(nowUtc));
        }

        /// <summary>Records a sale of a limited item against the traveler's allowance.</summary>
        public static void RecordTravelerSale(string travelerKey, string resref, int quantity, DateTime nowUtc)
        {
            var id = TravelerStockAllowance.BuildId(travelerKey, resref);
            var record = DB.Get<TravelerStockAllowance>(id) ?? new TravelerStockAllowance(travelerKey, resref);

            StockAllowanceRules.RecordSale(record, quantity, CityWeek.GetWeekStart(nowUtc));
            DB.Set(record);
        }
    }
}
