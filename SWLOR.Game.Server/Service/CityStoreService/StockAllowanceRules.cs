using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>
    /// Weekly allowances for limited stock. A record counts what was sold in one week; a record from an earlier week
    /// counts as nothing sold, so the allowance resets at the weekly boundary without any job running. The records live
    /// outside the stores, so recreating a store, picking up a stall, changing its theme or a restart never refills one.
    /// </summary>
    public static class StockAllowanceRules
    {
        /// <summary>How many were sold this week. A missing record, or one from an earlier week, is nothing.</summary>
        public static int GetSold(IStockAllowanceRecord record, DateTime weekStartUtc)
        {
            if (record == null || record.WeekStartUtc < weekStartUtc)
                return 0;

            return Math.Max(0, record.Sold);
        }

        /// <summary>How many of an item with this weekly limit are still for sale this week.</summary>
        public static int GetRemaining(int weeklyLimit, IStockAllowanceRecord record, DateTime weekStartUtc)
        {
            return Math.Max(0, weeklyLimit - GetSold(record, weekStartUtc));
        }

        /// <summary>Adds a sale to the record, starting a new count when the record is from an earlier week.</summary>
        public static void RecordSale(IStockAllowanceRecord record, int quantity, DateTime weekStartUtc)
        {
            if (record.WeekStartUtc < weekStartUtc)
            {
                record.WeekStartUtc = weekStartUtc;
                record.Sold = 0;
            }

            record.Sold = Math.Max(0, record.Sold) + Math.Max(0, quantity);
        }

        /// <summary>Whether a purchase of this many stays inside what is left of the allowance.</summary>
        public static bool CanSell(int weeklyLimit, IStockAllowanceRecord record, DateTime weekStartUtc, int quantity)
        {
            return quantity > 0 && quantity <= GetRemaining(weeklyLimit, record, weekStartUtc);
        }

        /// <summary>
        /// What a store puts on its shelf from its limited items: each stocked item for what is left of its allowance.
        /// An item sold out for the week is left off.
        /// </summary>
        /// <param name="items">The store's limited items.</param>
        /// <param name="level">The store's level, which decides which limited items are stocked.</param>
        /// <param name="getRecord">The allowance record of an item resref, or null when none has sold.</param>
        /// <param name="weekStartUtc">The start of the current week.</param>
        public static IReadOnlyList<CityStoreShelfEntry> PlanShelf(
            IEnumerable<CityStoreLimitedItem> items,
            int level,
            Func<string, IStockAllowanceRecord> getRecord,
            DateTime weekStartUtc)
        {
            return (items ?? Enumerable.Empty<CityStoreLimitedItem>())
                .Where(item => item.MinLevel <= level)
                .Select(item => new CityStoreShelfEntry(
                    item.Resref,
                    GetRemaining(item.WeeklyLimit, getRecord(item.Resref), weekStartUtc)))
                .Where(entry => entry.Quantity > 0)
                .ToList();
        }
    }
}
