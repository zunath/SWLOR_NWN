using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The minutes a performer's Flourish banks for the watchers who follow them: the logic that was
    /// <see cref="PerformanceAudienceService"/>'s own, moved here unchanged.
    /// </summary>
    public class PerformanceFlourishService : IPerformanceFlourishService
    {
        private readonly IPerformanceWatchingService _watching;
        private readonly IPerformanceRegistryService _sessions;
        private readonly IPerformanceInspiredService _inspired;
        private readonly IPerkService _perk;

        public PerformanceFlourishService(
            IPerformanceWatchingService watching,
            IPerformanceRegistryService sessions,
            IPerformanceInspiredService inspired,
            IPerkService perk)
        {
            _watching = watching;
            _sessions = sessions;
            _inspired = inspired;
            _perk = perk;
        }

        /// <summary>
        /// A Flourish banks minutes for every watcher past their warm-up who follows the performer, at most once per two
        /// minutes each. Two performers who Flourish within two seconds of each other double it for both audiences.
        /// </summary>
        public void OnFlourish(PerformanceSession session)
        {
            var now = session.LastFlourishUtc ?? UtcNow();
            var performer = session.Performer;
            var range = _watching.GetPerformerRange(performer);
            var baseMinutes = GetFlourishBaseMinutes(session);

            var partner = FindSynchronizedFlourish(session, now, range, out var partnerBaseMinutes, out var partnerRange);
            var synchronized = partner != null;

            foreach (var state in _watching.States)
            {
                // The same rules as the tick's watcher pass: a Flourish pays now, not after the next check.
                if (!state.IsWarmedUp || !_watching.CanWatchNow(state.Creature))
                    continue;

                var watcherBase = 0;
                if (state.Follows(performer) && _watching.IsInRange(state.Creature, performer, range))
                    watcherBase = baseMinutes;
                if (synchronized && state.Follows(partner.Performer) && _watching.IsInRange(state.Creature, partner.Performer, partnerRange))
                    watcherBase = Math.Max(watcherBase, partnerBaseMinutes);

                if (watcherBase > 0)
                    BankFlourish(state, watcherBase, synchronized, now);
            }
        }

        private int GetFlourishBaseMinutes(PerformanceSession session)
        {
            var capstone = session.Kind == PerformanceKind.Dance ? PerkType.Showstopper : PerkType.Virtuoso;
            return PerformanceAudienceRules.FlourishBaseMinutes(GetPerkLevel(session.Performer, capstone) > 0);
        }

        // The other performer in this show, in range of both, who Flourished within the two seconds before this one.
        private PerformanceSession FindSynchronizedFlourish(
            PerformanceSession session,
            DateTime now,
            float range,
            out int partnerBaseMinutes,
            out float partnerRange)
        {
            partnerBaseMinutes = 0;
            partnerRange = 0f;

            var performerPosition = GetPosition(session.Performer);
            foreach (var other in GetSessions())
            {
                if (other == session ||
                    !other.LastFlourishUtc.HasValue ||
                    other.Area != session.Area ||
                    !PerformanceAudienceRules.AreFlourishesSynchronized(now, other.LastFlourishUtc.Value) ||
                    !GetIsObjectValid(other.Performer))
                    continue;

                var otherRange = _watching.GetPerformerRange(other.Performer);
                if (!PerformanceAudienceRules.IsWithinRange(
                        performerPosition, GetPosition(other.Performer), Math.Min(range, otherRange)))
                    continue;

                partnerBaseMinutes = GetFlourishBaseMinutes(other);
                partnerRange = otherRange;
                return other;
            }

            return null;
        }

        private void BankFlourish(PerformanceAudienceState state, int baseMinutes, bool synchronized, DateTime now)
        {
            var minutes = PerformanceAudienceRules.FlourishMinutesToBank(
                baseMinutes, synchronized, state.LastFlourishBankUtc, state.LastFlourishBankedMinutes, now);
            if (minutes <= 0)
                return;

            // A watcher with no Inspired yet has banked nothing to add to; the next tick pays them.
            var watcher = state.Creature;
            if (!GetIsObjectValid(watcher) || !IsInspired(watcher))
                return;

            var frozenSeconds = PerformanceAudienceRules.FrozenRemainingSeconds(
                GetInspiredRemainingSeconds(watcher, now),
                state.HasFrozenRemaining,
                state.FrozenRemainingSeconds);
            var remainingSeconds = PerformanceAudienceRules.BankedRemainingSeconds(frozenSeconds, minutes, state.CapMinutes);
            SetInspiredRemaining(watcher, remainingSeconds, bankedAgain: remainingSeconds > frozenSeconds);

            state.HasFrozenRemaining = true;
            state.FrozenRemainingSeconds = remainingSeconds;
            state.LastFlourishBankedMinutes = PerformanceAudienceRules.FlourishBankedTotal(
                state.LastFlourishBankUtc, state.LastFlourishBankedMinutes, minutes, now);
            state.LastFlourishBankUtc = now;

            var addedMinutes = (remainingSeconds - frozenSeconds) / 60.0;
            if (addedMinutes > 0.0)
                SendMessageToPC(watcher, $"The flourish thrills you. (+{addedMinutes:0.#} min of Inspired)");
        }

        // The calls into the Perk and Performance services and the clock, one line each so a test can answer them.

        /// <summary>The time now.</summary>
        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        protected virtual IReadOnlyList<PerformanceSession> GetSessions() => _sessions.GetSessions();

        protected virtual bool IsInspired(uint creature) => _inspired.IsInspired(creature);

        protected virtual int GetPerkLevel(uint creature, PerkType perk) => _perk.GetPerkLevel(creature, perk);

        protected virtual double GetInspiredRemainingSeconds(uint creature, DateTime nowUtc) =>
            _inspired.GetRemainingSeconds(creature, nowUtc);

        protected virtual void SetInspiredRemaining(uint creature, double remainingSeconds, bool bankedAgain = false) =>
            _inspired.SetRemaining(creature, remainingSeconds, bankedAgain);
    }
}
