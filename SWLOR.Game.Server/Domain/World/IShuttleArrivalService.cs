namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Arrival: delivering a landed flight's passengers, tearing down its shuttle instance and the console announcements
    /// of the time left.
    /// </summary>
    public interface IShuttleArrivalService
    {
        /// <summary>Processes every flight whose arrival time has passed.</summary>
        void ProcessArrivals(DateTime now);

        /// <summary>Has each in-flight shuttle console announce the remaining time to its passengers.</summary>
        void ProcessTransitBroadcasts(DateTime now);
    }
}
