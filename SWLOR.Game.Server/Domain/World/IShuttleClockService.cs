namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The time and the scheduler the shuttle services use. A seam so a unit test can set the time, run the flight tick
    /// itself and run the delayed tasks on demand.
    /// </summary>
    public interface IShuttleClockService
    {
        /// <summary>The current UTC time.</summary>
        DateTime UtcNow();

        /// <summary>Registers the repeating flight tick.</summary>
        IDisposable ScheduleRepeating(Action task, TimeSpan interval);

        /// <summary>Registers a delayed task.</summary>
        IDisposable Schedule(Action task, TimeSpan delay);
    }
}
