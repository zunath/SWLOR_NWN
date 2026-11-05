using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The audience side of Performance: one module-wide tick a minute turns watching into banked Inspired, cleared XP
    /// debt, band music and Performance XP for the performers. The static facade
    /// <c>Service.PerformanceService.PerformanceAudience</c> forwards to the container's instance of this interface and
    /// keeps the event handlers.
    /// </summary>
    public interface IPerformanceAudienceService
    {
        /// <summary>Starts the module-wide audience tick when the module loads.</summary>
        void StartAudienceTick();

        /// <summary>
        /// Gives a leaving watcher back the Inspired time frozen while they watched. Disconnect-before runs ahead of
        /// module exit, where StatusEffect stores the live countdown that is reconciled when the player returns.
        /// </summary>
        void RestoreFrozenInspiredOnDisconnect();

        /// <summary>Forgets the watching state of a player who leaves the module.</summary>
        void ForgetExitingPlayer();

        /// <summary>
        /// Picks the performer a watcher follows for that performer's kind (dancer or musician); the Watching window calls this.
        /// The choice holds while the performer stays in range, and the strongest performer is followed otherwise.
        /// </summary>
        bool Follow(uint watcher, uint performer);

        /// <summary>Drops the watcher's pick of this performer, so they go back to following the strongest of that kind.</summary>
        bool Unfollow(uint watcher, uint performer);

        /// <summary>
        /// A Flourish banks minutes for every watcher past their warm-up who follows the performer, at most once per two
        /// minutes each. Two performers who Flourish within two seconds of each other double it for both audiences.
        /// </summary>
        void OnFlourish(PerformanceSession session);

        /// <summary>
        /// A band that loses a member stops playing for the watchers who followed that member. Watchers following another
        /// band member hear the new leader's song at the next tick.
        /// </summary>
        void OnSessionEnded(PerformanceSession session, PerformanceEndReason reason);

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

        /// <summary>A musician picked another song in the Perform window; the band's audience hears it at the next tick.</summary>
        void SetPerformerSong(uint performer, int trackId);

        /// <summary>
        /// Changing area interrupts watching even when the next tick finds the player at another show, and it replaces
        /// the client's music, so the band song is sent again at the next tick.
        /// </summary>
        void StopWatchingOnAreaEnter();

        /// <summary>
        /// Stepping out of every performer's range, or into combat, interrupts watching. The minute tick would miss a gap
        /// shorter than a minute, so each watcher's heartbeat checks; anyone else returns after one lookup.
        /// </summary>
        void StopWatchingWhenOutOfRange();

        /// <summary>
        /// Starting a performance interrupts watching at once, so starting and stopping between two ticks cannot keep
        /// the warm-up or the frozen Inspired.
        /// </summary>
        void OnPerformanceStarted(uint performer);

        /// <summary>
        /// Something else replaced the player's music, such as their own music player, so the band song they follow is
        /// sent again at the next tick.
        /// </summary>
        void ForgetBandSong(uint player);

        /// <summary>Death clears Inspired, so a watcher revived in range starts the warm-up over instead of resuming it.</summary>
        void StopWatchingOnDeath();
    }
}
