using System.Collections.Concurrent;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    /// <summary>
    /// Test-server shortcuts for incubation, so testers need not wait out 36-hour stages or the weekday a
    /// mutation needs. The chat commands that set these refuse to run on production. Overrides live in
    /// memory only and clear on reboot.
    /// </summary>
    public static class IncubationTestTools
    {
        private static readonly ConcurrentDictionary<string, DayOfWeek> _dayOverrides = new();

        /// <summary>
        /// The weekday incubation checks for this player: the tester's chosen day when set, otherwise the
        /// server's day (UTC).
        /// </summary>
        public static DayOfWeek GetServerDay(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId) && _dayOverrides.TryGetValue(playerId, out var day)
                ? day
                : DateTime.UtcNow.DayOfWeek;
        }

        public static bool HasDayOverride(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId) && _dayOverrides.ContainsKey(playerId);
        }

        public static void SetDayOverride(string playerId, DayOfWeek day)
        {
            _dayOverrides[playerId] = day;
        }

        public static void ClearDayOverride(string playerId)
        {
            _dayOverrides.TryRemove(playerId, out _);
        }

        /// <summary>
        /// Ends the running stage of each job now, keeping its stage count, and returns the jobs it changed.
        /// Jobs whose stage already finished are left alone.
        /// </summary>
        public static List<IncubationJob> FinishRunningStages(IEnumerable<IncubationJob> jobs, DateTime now)
        {
            var finished = new List<IncubationJob>();
            foreach (var job in jobs)
            {
                if (job.DateCompleted <= now)
                    continue;

                job.DateCompleted = now;
                if (job.DateStarted > now)
                    job.DateStarted = now;

                finished.Add(job);
            }

            return finished;
        }
    }
}
