using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Test-server shortcut for incubation, so testers need not wait out 36-hour stages. The weekday overrides
    /// live in <c>BeastMasteryService</c>.
    /// </summary>
    public static class IncubationTestTools
    {
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
