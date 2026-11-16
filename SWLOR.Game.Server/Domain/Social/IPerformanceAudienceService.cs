using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The audience side of Performance: one module-wide tick a minute turns watching into banked Inspired, cleared XP
    /// debt, band music and Performance XP for the performers. What each watcher is doing between ticks is
    /// <see cref="IPerformanceWatchingService"/>, the Flourish bank is <see cref="IPerformanceFlourishService"/> and the
    /// Watching window's actions are <see cref="IPerformanceWatchingWindowService"/>. The event handler is on
    /// <see cref="PerformanceAudienceService"/>.
    /// </summary>
    public interface IPerformanceAudienceService
    {
        /// <summary>Starts the module-wide audience tick when the module loads.</summary>
        void StartAudienceTick();
    }
}
