using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// What a watcher does in the Watching window: follow or unfollow a performer, redraw the window, close it, and switch
    /// the band's song on or off. A part of the Performance audience; the static facade
    /// <c>GameServices.PerformanceWatchingWindow</c> reaches the container's instance.
    /// </summary>
    public interface IPerformanceWatchingWindowService
    {
        /// <summary>
        /// Picks the performer a watcher follows for that performer's kind (dancer or musician); the Watching window calls this.
        /// The choice holds while the performer stays in range, and the strongest performer is followed otherwise.
        /// </summary>
        bool Follow(uint watcher, uint performer);

        /// <summary>Drops the watcher's pick of this performer, so they go back to following the strongest of that kind.</summary>
        bool Unfollow(uint watcher, uint performer);

        /// <summary>
        /// The Watching window's content from the last audience tick, or null when the watcher is not being served.
        /// A window that opens between ticks starts from it.
        /// </summary>
        PerformanceWatchView BuildWatchingView(uint watcher);

        /// <summary>
        /// Redraws the Watching window after a Follow click. The followed performers and the banking rate change at once;
        /// the Inspired and debt numbers stay as the last tick left them.
        /// </summary>
        void RefreshWatching(uint watcher);

        /// <summary>
        /// A watcher who closes the Watching window keeps it closed until they start watching a new show. A window closed
        /// because they changed area, or stopped watching, is not their choice and does not count.
        /// </summary>
        void OnWatchingWindowClosed(uint watcher);

        /// <summary>
        /// The Watching window's "Play the performer's song" box. Clearing it hands the watcher their own music back at once;
        /// checking it brings the band they follow back without waiting for the next tick.
        /// </summary>
        void SetPlayPerformerSong(uint watcher, bool play);
    }
}
