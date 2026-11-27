using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// What each watcher and performer is doing between the audience ticks: the logic that was
    /// <see cref="PerformanceAudienceService"/>'s own, moved here unchanged. The watcher and performer states and the tick
    /// number are this instance's own, and so are the handlers that interrupt watching.
    /// </summary>
    public class PerformanceWatchingService : IPerformanceWatchingService
    {
        private readonly IPerformanceWindowsService _windows;
        private readonly IMusicService _music;
        private readonly IPerformanceInspiredService _inspired;
        private readonly IPerformanceRegistryService _sessions;
        private readonly IPlayerNameService _playerName;
        private readonly IStatService _stat;
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, PerformanceAudienceState> _states = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, PerformancePerformerState> _performerStates = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private int _tickNumber;

        public PerformanceWatchingService(
            IPerformanceWindowsService windows,
            IMusicService music,
            IPerformanceInspiredService inspired,
            IPerformanceRegistryService sessions,
            IPlayerNameService playerName,
            IStatService stat)
        {
            _windows = windows;
            _music = music;
            _inspired = inspired;
            _sessions = sessions;
            _playerName = playerName;
            _stat = stat;
        }

        public int TickNumber => _tickNumber;

        public Dictionary<uint, PerformanceAudienceState>.ValueCollection States => _states.Values;

        public int NextTick()
        {
            return ++_tickNumber;
        }

        /// <summary>
        /// Gives a leaving watcher back the Inspired time frozen while they watched. Disconnect-before runs ahead of
        /// module exit, where StatusEffect stores the live countdown that is reconciled when the player returns.
        /// </summary>
        [NWNEventHandler(ScriptName.OnClientDisconnectBefore)]
        public void RestoreFrozenInspiredOnDisconnect()
        {
            if (_states.TryGetValue(OBJECT_SELF, out var state))
                StopWatching(state);
        }

        [PlayerSession(PlayerReload.LogoutOnly,
            "Forgets the watcher in memory, which goes with the old build.")]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ForgetExitingPlayer()
        {
            _states.Remove(GetExitingObject());
        }

        /// <summary>
        /// A band that loses a member stops playing for the watchers who followed that member. Watchers following another
        /// band member hear the new leader's song at the next tick.
        /// </summary>
        public void OnSessionEnded(PerformanceSession session, PerformanceEndReason reason)
        {
            var performer = session.Performer;
            _performerStates.Remove(performer);

            foreach (var state in _states.Values)
            {
                // The cached view must not offer the ended performer until the next tick rebuilds it, or a Follow click or
                // the song box could bring back their band.
                if (state.LastEntry is { } entry)
                {
                    entry.Candidates.RemoveAll(candidate => candidate.Performer == performer);
                    if (entry.Dancer?.Performer == performer)
                        entry.Dancer = null;
                    if (entry.Musician?.Performer == performer)
                        entry.Musician = null;
                }

                if (state.DancerChoice == performer)
                    state.DancerChoice = 0;
                if (state.MusicianChoice == performer)
                    state.MusicianChoice = 0;
                if (state.FollowedDancer == performer)
                    state.FollowedDancer = 0;

                if (state.FollowedMusician != performer)
                    continue;

                state.FollowedMusician = 0;
                if (state.BandTrackId.HasValue)
                    RestoreMusic(state);
            }
        }

        public PerformanceWatchView BuildWatchView(
            PerformanceAudienceState state,
            PerformanceWatcherEntry entry,
            PerformanceWatcherReport report,
            PerformanceWatchPlan plan,
            InspiredEffectData composition)
        {
            var watcher = entry.Player;
            return PerformanceWatchViewBuilder.Build(
                entry.Candidates,
                entry.Dancer,
                entry.Musician,
                plan,
                state.ContinuousMinutes,
                report.RemainingSeconds,
                report.XpDebt,
                report.DebtCleared,
                composition,
                performer => state.GetName(performer, _tickNumber, () => GetDisplayName(watcher, performer)),
                state.DancerChoice,
                state.MusicianChoice);
        }

        /// <summary>A musician picked another song in the Perform window; the band's audience hears it at the next tick.</summary>
        public void SetPerformerSong(uint performer, int trackId)
        {
            if (_performerStates.TryGetValue(performer, out var performerState))
                performerState.SongTrackId = trackId;
        }

        /// <summary>
        /// Changing area interrupts watching even when the next tick finds the player at another show, and it replaces
        /// the client's music, so the band song is sent again at the next tick.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public void StopWatchingOnAreaEnter()
        {
            var player = GetEnteringObject();
            OnPlayerMusicReplaced(player);
            if (_states.TryGetValue(player, out var state))
                StopWatching(state);
        }

        /// <summary>
        /// Stepping out of every performer's range, or into combat, interrupts watching. The minute tick would miss a gap
        /// shorter than a minute, so each watcher's heartbeat checks; anyone else returns after one lookup.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public void StopWatchingWhenOutOfRange()
        {
            var player = OBJECT_SELF;
            if (!_states.TryGetValue(player, out var state) || !state.WatchingSinceUtc.HasValue)
                return;

            if (!GetIsInCombat(player) && IsCoveredByAnyPerformer(player))
                return;

            InterruptWatching(state);
        }

        public bool CanWatchNow(uint player)
        {
            return GetIsObjectValid(player) &&
                   !GetIsDead(player) &&
                   !GetIsInCombat(player) &&
                   !TryGetSession(player, out _);
        }

        private bool IsCoveredByAnyPerformer(uint watcher)
        {
            foreach (var session in GetSessions())
            {
                var performer = session.Performer;
                if (performer != watcher && IsInRange(watcher, performer, GetPerformerRange(performer)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Starting a performance interrupts watching at once, so starting and stopping between two ticks cannot keep
        /// the warm-up or the frozen Inspired.
        /// </summary>
        public void OnPerformanceStarted(uint performer)
        {
            if (_states.TryGetValue(performer, out var state))
                InterruptWatching(state);
        }

        // The warm-up starts over and the player's own music comes back. Picked performers are kept.
        public void InterruptWatching(PerformanceAudienceState state)
        {
            if (!state.IsWatching)
                return;

            RestoreMusic(state);
            StopWatching(state);
            // After StopWatching, so the window closing is not taken for the watcher choosing to hide it.
            if (GetIsObjectValid(state.Creature))
                _windows.CloseWatching(state.Creature);
        }

        /// <summary>
        /// Something else replaced the player's music, such as their own music player, so the band song they follow is
        /// sent again at the next tick.
        /// </summary>
        public void OnPlayerMusicReplaced(uint player)
        {
            if (_states.TryGetValue(player, out var state))
            {
                state.BandTrackId = null;
                state.BandArea = 0;
            }
        }

        /// <summary>Death clears Inspired, so a watcher revived in range starts the warm-up over instead of resuming it.</summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public void StopWatchingOnDeath()
        {
            if (_states.TryGetValue(GetLastPlayerDied(), out var state))
                StopWatching(state);
        }

        /// <summary>
        /// Clears a player's watching state. While they watched, each tick held their Inspired at a stored remainder (with
        /// padding so it outlasts the gap to the next tick); that stored remainder is what they leave with.
        /// </summary>
        private void StopWatching(PerformanceAudienceState state)
        {
            if (state.HasFrozenRemaining && GetIsObjectValid(state.Creature) &&
                IsInspired(state.Creature))
            {
                SetInspiredRemaining(state.Creature, state.FrozenRemainingSeconds);
            }

            state.StopWatching();
        }

        public void RestoreMusic(PerformanceAudienceState state)
        {
            if (state.BandTrackId.HasValue && GetIsObjectValid(state.Creature))
                _music.RestoreBackgroundMusic(state.Creature);

            state.BandTrackId = null;
            state.BandArea = 0;
        }

        // A player who left every show forgets their warm-up and gets their own music back; players who picked a performer keep that pick.
        public void ReleaseUnseenStates()
        {
            if (_states.Count == 0)
                return;

            List<uint> forgotten = null;
            foreach (var state in _states.Values)
            {
                if (state.LastSeenTick == _tickNumber)
                    continue;

                RestoreMusic(state);
                StopWatching(state);

                // After StopWatching, so the window closing is not taken for the watcher choosing to hide it.
                if (GetIsObjectValid(state.Creature))
                    _windows.CloseWatching(state.Creature);

                if (!state.HasChoice || !GetIsObjectValid(state.Creature))
                {
                    forgotten ??= new List<uint>();
                    forgotten.Add(state.Creature);
                }
            }

            if (forgotten == null)
                return;

            foreach (var creature in forgotten)
                _states.Remove(creature);
        }

        public PerformanceAudienceState GetState(uint creature)
        {
            if (!_states.TryGetValue(creature, out var state))
            {
                state = new PerformanceAudienceState(creature);
                _states[creature] = state;
            }

            return state;
        }

        public bool TryGetState(uint creature, out PerformanceAudienceState state)
        {
            return _states.TryGetValue(creature, out state);
        }

        public bool TryGetPerformerState(uint performer, out PerformancePerformerState state)
        {
            return _performerStates.TryGetValue(performer, out state);
        }

        public void SetPerformerState(uint performer, PerformancePerformerState state)
        {
            _performerStates[performer] = state;
        }

        public float GetPerformerRange(uint performer)
        {
            return PerformanceAudienceRules.GetRange(GetStatAdjustment(performer, StatType.PerformanceRangeBonusMeters));
        }

        public bool IsInRange(uint watcher, uint performer, float range)
        {
            if (!GetIsObjectValid(watcher) || !GetIsObjectValid(performer) || GetArea(watcher) != GetArea(performer))
                return false;

            return PerformanceAudienceRules.IsWithinRange(GetPosition(watcher), GetPosition(performer), range);
        }

        // The calls into the Stat, PlayerName and Performance services, one line each so a test can answer them.

        protected virtual bool TryGetSession(uint performer, out PerformanceSession session) =>
            _sessions.TryGetSession(performer, out session);

        protected virtual IReadOnlyList<PerformanceSession> GetSessions() => _sessions.GetSessions();

        protected virtual bool IsInspired(uint creature) => _inspired.IsInspired(creature);

        protected virtual int GetStatAdjustment(uint creature, StatType stat) => _stat.GetStatAdjustment(creature, stat);

        protected virtual string GetDisplayName(uint observer, uint target) => _playerName.GetDisplayName(observer, target);

        protected virtual void SetInspiredRemaining(uint creature, double remainingSeconds, bool bankedAgain = false) =>
            _inspired.SetRemaining(creature, remainingSeconds, bankedAgain);
    }
}
