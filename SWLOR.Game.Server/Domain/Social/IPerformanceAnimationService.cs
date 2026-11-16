using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The clips a performer plays: the routine loop, one-shot accents that hand back to it, and the bow at the end.
    /// A part of the Performance service.
    /// </summary>
    public interface IPerformanceAnimationService
    {
        /// <summary>Every performance clip plays on the looping carrier, so the routine loop repeats in place for minutes at a time.</summary>
        void StartLoopingAnimation(PerformanceSession session);

        /// <summary>A one-shot clip hands back to the routine when it ends, unless a newer clip has replaced it.</summary>
        void PlayThenLoop(PerformanceSession session, AnimationClip clip);

        /// <summary>A one-shot clip played in the given hold mode, which hands back to the routine when it ends.</summary>
        void PlayThenLoop(PerformanceSession session, AnimationClip clip, NamedAnimationHold holdMode);

        /// <summary>Walking away or attacking has already replaced the clip; only a performer standing idle bows.</summary>
        void ReleaseAnimation(PerformanceSession session, PerformanceEndReason reason);

        /// <summary>Plays the accent clip of a cue the performer hit, when it has one.</summary>
        void PlayHitAnimation(PerformanceSession session, PerformanceCue cue);
    }
}
