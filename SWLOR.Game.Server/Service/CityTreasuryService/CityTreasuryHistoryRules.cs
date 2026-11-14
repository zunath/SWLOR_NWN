using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWLOR.Game.Server.Service.CityTreasuryService
{
    /// <summary>
    /// The rules of a city's treasury history: how long lines are kept, which kinds of income are kept as one total per day,
    /// and how a line reads.
    /// </summary>
    public static class CityTreasuryHistoryRules
    {
        /// <summary>Lines older than this are dropped.</summary>
        public const int KeepDays = 30;

        /// <summary>The most lines a city keeps, oldest dropped first, whatever their age.</summary>
        public const int MaxEntries = 500;

        /// <summary>Small income that arrives often, kept as one line per kind per day instead of one per payment.</summary>
        public static bool IsDailyTotal(CityTreasuryEntryType type) =>
            type == CityTreasuryEntryType.CitizenTaxes ||
            type == CityTreasuryEntryType.StoreSalesTax ||
            type == CityTreasuryEntryType.ExchangeTariffs ||
            type == CityTreasuryEntryType.TransportationTax ||
            type == CityTreasuryEntryType.ShipyardFees;

        /// <summary>
        /// Adds a line: a daily-total kind joins the same day's line of its kind, anything else is a line of its own. Then
        /// drops what is too old or over the limit.
        /// </summary>
        public static void Add(List<CityTreasuryHistoryEntry> entries, CityTreasuryHistoryEntry entry)
        {
            var total = IsDailyTotal(entry.Type)
                ? entries.LastOrDefault(x => x.Type == entry.Type && x.TimeUtc.Date == entry.TimeUtc.Date)
                : null;
            if (total == null)
            {
                entries.Add(entry);
            }
            else
            {
                total.Amount += entry.Amount;
                total.Restricted += entry.Restricted;
                total.Count += Math.Max(1, entry.Count);
                total.Balance = entry.Balance;
                total.TimeUtc = entry.TimeUtc;
            }

            Prune(entries, entry.TimeUtc);
        }

        /// <summary>Drops lines older than <see cref="KeepDays"/> days, then the oldest beyond <see cref="MaxEntries"/>.</summary>
        /// <returns>Whether anything was dropped.</returns>
        public static bool Prune(List<CityTreasuryHistoryEntry> entries, DateTime nowUtc)
        {
            var cutoff = nowUtc.AddDays(-KeepDays);
            var removed = entries.RemoveAll(x => x.TimeUtc < cutoff);
            if (entries.Count > MaxEntries)
            {
                removed += entries.Count - MaxEntries;
                entries.RemoveRange(0, entries.Count - MaxEntries);
            }

            return removed > 0;
        }

        /// <summary>A line's change as shown: "+1,200", "-1,200", or "0" for a line that moved nothing.</summary>
        public static string FormatAmount(long amount) =>
            amount > 0 ? "+" + FormatCredits(amount) : amount < 0 ? "-" + FormatCredits(-amount) : "0";

        public static string FormatCredits(long credits) => credits.ToString("N0", CultureInfo.InvariantCulture);

        /// <summary>When a line happened, as the list shows it.</summary>
        public static string FormatTime(DateTime timeUtc) => timeUtc.ToString("MMM d HH:mm", CultureInfo.InvariantCulture);

        /// <summary>What a line says. <paramref name="name"/> is the player's name as the reader knows it, or empty.</summary>
        public static string Describe(CityTreasuryHistoryEntry entry, string name)
        {
            var by = string.IsNullOrWhiteSpace(name) ? string.Empty : $" by {name}";
            var detail = string.IsNullOrWhiteSpace(entry.Detail) ? "unknown" : entry.Detail;
            var count = Math.Max(1, entry.Count);
            var text = entry.Type switch
            {
                CityTreasuryEntryType.Deposit => $"Deposit{by}",
                CityTreasuryEntryType.Withdrawal => $"Withdrawal{by}",
                CityTreasuryEntryType.CitizenTaxes => $"Citizen taxes ({Plural(count, "payment")})",
                CityTreasuryEntryType.StoreSalesTax => $"Store sales tax ({Plural(count, "sale")})",
                CityTreasuryEntryType.ExchangeTariffs => $"Exchange tariffs ({Plural(count, "trade")})",
                CityTreasuryEntryType.TransportationTax => $"Shuttle fares ({Plural(count, "trip")})",
                CityTreasuryEntryType.ShipyardFees => $"Shipyard fees ({Plural(count, "job")})",
                CityTreasuryEntryType.UpkeepPaid => $"Upkeep paid{by}",
                CityTreasuryEntryType.EdictEnacted => $"Edict enacted: {detail}{by}",
                CityTreasuryEntryType.SituationGain => $"Situation: {detail}",
                CityTreasuryEntryType.SituationCost => $"Situation: {detail}",
                CityTreasuryEntryType.BountyPosted => $"Bounty posted on {detail}{by}",
                CityTreasuryEntryType.BountyReturned => $"Bounty on {detail} ended unclaimed or withdrawn{by}",
                CityTreasuryEntryType.BountyCollected => $"Bounty of {FormatCredits(entry.EscrowCredits)} collected: {detail}",
                CityTreasuryEntryType.ProjectStarted => $"Project started: {detail}{by}",
                CityTreasuryEntryType.ProjectReturned => $"Project cancelled: {detail}",
                CityTreasuryEntryType.ProjectCompleted => $"Project completed: {detail} ({FormatCredits(entry.EscrowCredits)} spent)",
                CityTreasuryEntryType.TournamentPrizePosted => $"Tournament prize held: {detail}{by}",
                CityTreasuryEntryType.TournamentPrizeReturned => $"Tournament cancelled, prize returned: {detail}",
                CityTreasuryEntryType.TournamentPrizeAwarded => $"Tournament prize of {FormatCredits(entry.EscrowCredits)} awarded: {detail}",
                CityTreasuryEntryType.TournamentFees => $"Tournament entry fees kept: {detail}",
                _ => detail,
            };

            return entry.Restricted == 0
                ? text
                : $"{text} ({FormatCredits(Math.Abs(entry.Restricted))} restricted)";
        }

        private static string Plural(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
    }
}
