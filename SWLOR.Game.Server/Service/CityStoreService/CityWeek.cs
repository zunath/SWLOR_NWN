using System;

namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>
    /// The weekly boundary for city stock: store allowances reset at it, and the weekly traveler rotation moves at it.
    /// The week runs from Monday 00:00 UTC. It is defined here and nowhere else.
    /// </summary>
    public static class CityWeek
    {
        public const DayOfWeek ResetDay = DayOfWeek.Monday;
        public const int ResetHourUtc = 0;

        /// <summary>The start of the week this moment falls in: the latest boundary at or before it.</summary>
        public static DateTime GetWeekStart(DateTime nowUtc)
        {
            var utc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            var daysSinceReset = ((int)utc.DayOfWeek - (int)ResetDay + 7) % 7;
            var start = utc.Date.AddDays(-daysSinceReset).AddHours(ResetHourUtc);

            return start > utc ? start.AddDays(-7) : start;
        }

        /// <summary>The next boundary after this moment.</summary>
        public static DateTime GetNextWeekStart(DateTime nowUtc)
        {
            return GetWeekStart(nowUtc).AddDays(7);
        }
    }
}
