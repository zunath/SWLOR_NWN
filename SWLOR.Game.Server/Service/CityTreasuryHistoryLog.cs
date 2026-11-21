using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Writes and reads a city's treasury history, shown in the Manage City window's Treasury tab to those who may withdraw
    /// from the treasury. Each change is written after the city itself is saved, so the history only ever follows the
    /// treasury; a restart in between loses that one line, never credits. Lines are kept
    /// <see cref="CityTreasuryHistoryRules.KeepDays"/> days (<see cref="CityTreasuryHistoryRules"/>).
    /// </summary>
    public static class CityTreasuryHistoryLog
    {
        /// <summary>
        /// Notes a change to the city's treasury, which <paramref name="city"/> already holds. <paramref name="amount"/> is
        /// what the treasuries gained (negative when they lost credits); <paramref name="restricted"/> is the restricted part.
        /// </summary>
        public static void Record(
            WorldProperty city,
            CityTreasuryEntryType type,
            long amount,
            string detail = "",
            string playerId = "",
            long restricted = 0,
            long escrowCredits = 0)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.Id) || type == CityTreasuryEntryType.Invalid)
                return;
            if (amount == 0 && escrowCredits == 0)
                return;

            try
            {
                var history = DB.Get<CityTreasuryHistory>(CityTreasuryHistory.KeyFor(city.Id)) ?? new CityTreasuryHistory(city.Id);
                CityTreasuryHistoryRules.Add(history.Entries, new CityTreasuryHistoryEntry
                {
                    TimeUtc = DateTime.UtcNow,
                    Type = type,
                    Amount = amount,
                    Restricted = restricted,
                    EscrowCredits = escrowCredits,
                    Balance = city.Treasury + city.RestrictedTreasury,
                    Detail = detail ?? string.Empty,
                    PlayerId = playerId ?? string.Empty,
                });
                DB.Set(history);
            }
            catch (Exception ex)
            {
                // The history is a record of what happened; a failure to write it must never stop the treasury change.
                Log.Write(LogGroup.Error, $"Failed to write the treasury history of '{city.Id}': {ex}");
            }
        }

        /// <summary>The city's lines from the last <see cref="CityTreasuryHistoryRules.KeepDays"/> days, newest first.</summary>
        public static List<CityTreasuryHistoryEntry> Get(string cityId)
        {
            var history = string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<CityTreasuryHistory>(CityTreasuryHistory.KeyFor(cityId));
            if (history == null)
                return new List<CityTreasuryHistoryEntry>();

            var entries = history.Entries.ToList();
            CityTreasuryHistoryRules.Prune(entries, DateTime.UtcNow);
            entries.Reverse();
            return entries;
        }

        /// <summary>
        /// Drops lines past their time from every history, so a quiet city's record shrinks too, and removes the history of a
        /// city that is gone or has nothing left in it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void PruneAll()
        {
            var query = new DBQuery<CityTreasuryHistory>();
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return;

            var now = DateTime.UtcNow;
            var removed = 0;
            foreach (var history in DB.Search(query.AddPaging(count, 0)).Where(x => x != null).ToList())
            {
                var changed = CityTreasuryHistoryRules.Prune(history.Entries, now);
                if (history.Entries.Count == 0 || DB.Get<WorldProperty>(history.CityId) == null)
                {
                    DB.Delete<CityTreasuryHistory>(history.Id);
                    removed++;
                }
                else if (changed)
                {
                    DB.Set(history);
                }
            }

            if (removed > 0)
                Log.Write(LogGroup.Property, $"Removed {removed} empty or orphaned city treasury histories.");
        }
    }
}
