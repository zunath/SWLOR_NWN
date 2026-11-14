namespace SWLOR.Game.Server.Service.CityTreasuryService
{
    /// <summary>One line of a city's treasury history: a change, or a day's total of one kind of small income.</summary>
    public class CityTreasuryHistoryEntry
    {
        /// <summary>When the change happened; for a day's total, when its last part did.</summary>
        public DateTime TimeUtc { get; set; }

        public CityTreasuryEntryType Type { get; set; }

        /// <summary>
        /// The credits the treasury gained (positive) or lost (negative), both balances together. Zero for a line that moved
        /// nothing, such as a bounty collected out of escrow posted earlier.
        /// </summary>
        public long Amount { get; set; }

        /// <summary>The credits in escrow a line concerns when <see cref="Amount"/> is zero (a bounty or prize paid out).</summary>
        public long EscrowCredits { get; set; }

        /// <summary>Of <see cref="Amount"/>, the part that came from or went to the restricted treasury.</summary>
        public long Restricted { get; set; }

        /// <summary>The treasury and the restricted treasury together, after the change.</summary>
        public long Balance { get; set; }

        /// <summary>What the line is about (an Edict's name, a bounty's target as strangers saw them), or empty.</summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>The player who made the change, or empty. Readers see the name through <c>PlayerName</c>.</summary>
        public string PlayerId { get; set; } = string.Empty;

        /// <summary>How many changes a day's total holds; 1 for a single change.</summary>
        public int Count { get; set; } = 1;
    }
}
