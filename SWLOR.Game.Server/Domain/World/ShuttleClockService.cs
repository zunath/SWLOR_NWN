using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The time and scheduler seam of the shuttle services, moved unchanged from <see cref="ShuttleService"/> (P5-9c). Each
    /// member is <c>virtual</c> so a unit test can set the time and run the tick and the delayed tasks itself
    /// (<c>ClockedShuttleClockService</c> in <c>Tests/Support/ShuttleBench.cs</c>); the bodies are the original calls.
    /// </summary>
    public class ShuttleClockService : IShuttleClockService
    {
        /// <summary>The current UTC time (the original <c>DateTime.UtcNow</c>).</summary>
        public virtual DateTime UtcNow() => DateTime.UtcNow;

        /// <summary>Registers the repeating flight tick (the original <c>Scheduler.ScheduleRepeating</c> call).</summary>
        public virtual IDisposable ScheduleRepeating(Action task, TimeSpan interval) => Scheduler.ScheduleRepeating(task, interval);

        /// <summary>Registers a delayed task (the original <c>Scheduler.Schedule</c> call).</summary>
        public virtual IDisposable Schedule(Action task, TimeSpan delay) => Scheduler.Schedule(task, delay);
    }
}
