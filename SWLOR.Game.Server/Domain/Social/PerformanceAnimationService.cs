using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The clips a performer plays: the logic that was <see cref="PerformanceService"/>'s own, moved here unchanged.
    /// </summary>
    public class PerformanceAnimationService : IPerformanceAnimationService
    {
        // One named playback lasts at most ten minutes; the beat loop restarts the routine a minute early.
        private const float LoopPlaybackSeconds = 600f;
        private const float LoopRenewSeconds = 540f;
        // If an accent's hand-back is lost, the beat loop resumes the routine this long after the accent ends.
        private const float AccentRecoverySeconds = 1f;

        private readonly INamedAnimationService _namedAnimation;
        private readonly IPerformanceRegistryService _sessions;

        public PerformanceAnimationService(INamedAnimationService namedAnimation, IPerformanceRegistryService sessions)
        {
            _namedAnimation = namedAnimation;
            _sessions = sessions;
        }

        // Every performance clip plays on the looping carrier, so the routine loop repeats in place for minutes at a time.
        public void StartLoopingAnimation(PerformanceSession session)
        {
            var clip = PerformanceAnimations.FitToBody(
                PerformanceAnimations.GetLoopClip(session.Kind, session.InstrumentFamily, session.Routine),
                GetAppearanceType(session.Performer), GetGender(session.Performer));
            session.AnimationToken = _namedAnimation.Play(session.Performer, clip, LoopPlaybackSeconds, holdMode: HoldMode(session));
            session.AnimationRenewUtc = UtcNow().AddSeconds(LoopRenewSeconds);
        }

        private NamedAnimationHold HoldMode(PerformanceSession session) =>
            PerformanceAnimations.GetHoldMode(session.Kind, session.InstrumentFamily);

        // A one-shot clip hands back to the routine when it ends, unless a newer clip has replaced it.
        public void PlayThenLoop(PerformanceSession session, AnimationClip clip) =>
            PlayThenLoop(session, clip, HoldMode(session));

        public void PlayThenLoop(PerformanceSession session, AnimationClip clip, NamedAnimationHold holdMode)
        {
            var player = session.Performer;
            clip = PerformanceAnimations.FitToBody(clip, GetAppearanceType(player), GetGender(player));
            var token = _namedAnimation.Play(player, clip, holdMode: holdMode);
            session.AnimationToken = token;
            session.AnimationRenewUtc = UtcNow().AddSeconds(clip.Duration + AccentRecoverySeconds);
            AssignCommand(GetModule(), () => DelayCommand(clip.Duration, () =>
            {
                if (_sessions.TryGetSession(player, out var live) && live == session && session.AnimationToken == token)
                    StartLoopingAnimation(session);
            }));
        }

        // Walking away or attacking has already replaced the clip; only a performer standing idle bows.
        public void ReleaseAnimation(PerformanceSession session, PerformanceEndReason reason)
        {
            var player = session.Performer;
            if (reason == PerformanceEndReason.Stopped && GetCurrentAction(player) == ActionType.Invalid)
                _namedAnimation.Play(player, AuthoredAnimation.PerformanceBow, holdMode: HoldMode(session));
            else
                _namedAnimation.StopIfCurrent(player, session.AnimationToken);
        }

        public void PlayHitAnimation(PerformanceSession session, PerformanceCue cue)
        {
            var clip = PerformanceAnimations.GetCueClip(session.Kind, session.InstrumentFamily, cue);
            if (clip != null)
                PlayThenLoop(session, clip);
        }

        // The clock, one line so a test can answer it.

        /// <summary>The time now.</summary>
        protected virtual DateTime UtcNow() => DateTime.UtcNow;
    }
}
