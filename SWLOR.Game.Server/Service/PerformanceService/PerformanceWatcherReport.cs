using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>What one watcher's turn in the audience tick produced; the Watching window shows it.</summary>
    public sealed class PerformanceWatcherReport
    {
        public PerformanceWatchPlan Plan { get; set; }

        /// <summary>The Inspired the watcher holds after this tick, in seconds.</summary>
        public double RemainingSeconds { get; set; }

        /// <summary>XP debt the tick cleared for this watcher.</summary>
        public int DebtCleared { get; set; }

        /// <summary>The watcher's XP debt after this tick, or -1 when the tick did not read it.</summary>
        public int XpDebt { get; set; } = -1;

        /// <summary>The player record read this tick, so the tick never reads it twice. Dropped when the watcher is done.</summary>
        public Player DbPlayer { get; set; }
    }
}
