using SWLOR.Game.Server.Domain.World;
using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// What each watcher and performer is doing between the audience ticks: the watcher and performer states, and what
    /// interrupts watching (a new area, leaving range, combat, death, a performance starting, a band losing a member). A
    /// part of the Performance audience; its handlers are on <see cref="PerformanceWatchingService"/>.
    /// </summary>
    public interface IPerformanceWatchingService : IMusicReplacedListener
    {
        /// <summary>
        /// Gives a leaving watcher back the Inspired time frozen while they watched. Disconnect-before runs ahead of
        /// module exit, where StatusEffect stores the live countdown that is reconciled when the player returns.
        /// </summary>
        void RestoreFrozenInspiredOnDisconnect();

        /// <summary>Forgets the watching state of a player who leaves the module.</summary>
        void ForgetExitingPlayer();

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

        /// <summary>Death clears Inspired, so a watcher revived in range starts the warm-up over instead of resuming it.</summary>
        void StopWatchingOnDeath();

        /// <summary>
        /// Starting a performance interrupts watching at once, so starting and stopping between two ticks cannot keep
        /// the warm-up or the frozen Inspired.
        /// </summary>
        void OnPerformanceStarted(uint performer);

        /// <summary>
        /// A band that loses a member stops playing for the watchers who followed that member. Watchers following another
        /// band member hear the new leader's song at the next tick.
        /// </summary>
        void OnSessionEnded(PerformanceSession session, PerformanceEndReason reason);

        /// <summary>A musician picked another song in the Perform window; the band's audience hears it at the next tick.</summary>
        void SetPerformerSong(uint performer, int trackId);

        /// <summary>The number of the audience tick that is running, or ran last.</summary>
        int TickNumber { get; }

        /// <summary>Starts the next audience tick and returns its number.</summary>
        int NextTick();

        /// <summary>The creature's watcher state, created on first use.</summary>
        PerformanceAudienceState GetState(uint creature);

        /// <summary>The creature's watcher state, when it has one.</summary>
        bool TryGetState(uint creature, out PerformanceAudienceState state);

        /// <summary>Every watcher state.</summary>
        Dictionary<uint, PerformanceAudienceState>.ValueCollection States { get; }

        /// <summary>The performer's state, when they have one.</summary>
        bool TryGetPerformerState(uint performer, out PerformancePerformerState state);

        /// <summary>Keeps the performer's state until their session ends.</summary>
        void SetPerformerState(uint performer, PerformancePerformerState state);

        /// <summary>A player who left every show forgets their warm-up and gets their own music back; players who picked a performer keep that pick.</summary>
        void ReleaseUnseenStates();

        /// <summary>The warm-up starts over and the player's own music comes back. Picked performers are kept.</summary>
        void InterruptWatching(PerformanceAudienceState state);

        /// <summary>Hands the watcher their own music back.</summary>
        void RestoreMusic(PerformanceAudienceState state);

        /// <summary>Whether the player can be a watcher at all right now: valid, alive, out of combat and not performing.</summary>
        bool CanWatchNow(uint player);

        /// <summary>How far the performer's show carries.</summary>
        float GetPerformerRange(uint performer);

        /// <summary>Whether the watcher stands within the range of the performer, in the same area.</summary>
        bool IsInRange(uint watcher, uint performer, float range);

        /// <summary>The Watching window's content for the watcher's state, entry, report and plan.</summary>
        PerformanceWatchView BuildWatchView(
            PerformanceAudienceState state,
            PerformanceWatcherEntry entry,
            PerformanceWatcherReport report,
            PerformanceWatchPlan plan,
            InspiredEffectData composition);
    }
}
