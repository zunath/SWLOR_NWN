using System;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Service.AstromechService
{
    /// <summary>
    /// Owns the clips an astromech's working animation plays. Each motion has a token; one that a later motion has
    /// replaced can no longer stop or release anything.
    /// </summary>
    public sealed class AstromechWorkPlayback
    {
        /// <summary>The chassis clip that starts <see cref="AstromechAnimations.Work"/>.</summary>
        public const string EntrySource = "getmid";

        /// <summary>The chassis clip <see cref="AstromechAnimations.Work"/> loops.</summary>
        public const string LoopSource = "getmidlp";

        /// <summary>How long after the motion's time is up its clips stay chosen, so its last frames keep them.</summary>
        public const float ReleaseGraceSeconds = .5f;

        private readonly INamedAnimationRuntime _runtime;
        private readonly Action<uint, Func<bool>> _releasePose;

        public AstromechWorkPlayback(INamedAnimationRuntime runtime, Action<uint, Func<bool>> releasePose = null)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _releasePose = releasePose;
        }

        /// <summary>
        /// Chooses the clips for a motion of this length: the rod clips for a fishing clip, the tool arm otherwise.
        /// Returns the motion's token.
        /// </summary>
        public string Begin(uint creature, AnimationClip clip, float duration)
        {
            if (!_runtime.IsValid(creature))
                throw new ArgumentException("Animation target must be a valid creature.", nameof(creature));

            if (AstromechAnimations.TryGetRodClips(clip, out var rod))
            {
                _runtime.Replace(creature, EntrySource, rod.Entry);
                _runtime.Replace(creature, LoopSource, rod.Loop);
            }
            else
            {
                _runtime.Replace(creature, EntrySource, "");
                _runtime.Replace(creature, LoopSource, "");
            }

            // The rod must not outlive the action: later work at a bench would bring it out.
            var token = Guid.NewGuid().ToString("N");
            _runtime.SetToken(creature, token);
            _runtime.Schedule(duration + ReleaseGraceSeconds, () =>
            {
                if (IsCurrent(creature, token))
                    Restore(creature);
            });
            return token;
        }

        public bool IsCurrent(uint creature, string token) =>
            !string.IsNullOrEmpty(token) && _runtime.IsValid(creature) && _runtime.GetToken(creature) == token;

        /// <summary>
        /// Ends the motion this token started and puts the astromech back at rest. Returns false, changing nothing,
        /// for a token this playback does not hold: a replaced motion's, or one another animation system gave out.
        /// </summary>
        public bool StopIfCurrent(uint creature, string token)
        {
            if (!IsCurrent(creature, token))
                return false;

            Restore(creature);
            _releasePose?.Invoke(creature, () => _runtime.IsValid(creature) && string.IsNullOrEmpty(_runtime.GetToken(creature)));
            return true;
        }

        /// <summary>Hands the creature to another animation: the clips are put back and nothing is played.</summary>
        public void ReleaseForNativePlayback(uint creature)
        {
            if (_runtime.IsValid(creature) && !string.IsNullOrEmpty(_runtime.GetToken(creature)))
                Restore(creature);
        }

        private void Restore(uint creature)
        {
            _runtime.Replace(creature, EntrySource, "");
            _runtime.Replace(creature, LoopSource, "");
            _runtime.SetToken(creature, "");
        }
    }
}
