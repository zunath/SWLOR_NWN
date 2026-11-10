using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>Everything the Watching window shows, built once per audience tick. Plain text and numbers.</summary>
    public sealed class PerformanceWatchView
    {
        public string HeaderText { get; init; } = string.Empty;
        public IReadOnlyList<PerformanceWatchRow> Rows { get; init; } = new List<PerformanceWatchRow>();

        /// <summary>Banked Inspired as a share of the cap, 0 to 1.</summary>
        public float BankedFraction { get; init; }

        /// <summary>The banked minutes against the cap, the banking rate and the minutes left to fill it.</summary>
        public string BankedText { get; init; } = string.Empty;

        public string DebtText { get; init; } = string.Empty;
        public string LeaveWithText { get; init; } = string.Empty;
    }
}
