using System;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Service.AstromechService
{
    /// <summary>
    /// Plays an astromech's own motion in place of an authored humanoid working clip. The chassis has one working
    /// animation (its tool arm out, probing). Fishing swaps that animation's clips for the rod clips for the length of
    /// the action, so the built-in rod comes out, casts, and works the line.
    /// </summary>
    public static class AstromechWorkAnimation
    {
        /// <summary>The chassis clip that starts <see cref="AstromechAnimations.Work"/>.</summary>
        public const string EntrySource = "cgetmid";

        /// <summary>The chassis clip <see cref="AstromechAnimations.Work"/> loops.</summary>
        public const string LoopSource = "cgetmidlp";

        private const string MappingVariable = "ASTROMECH_WORK_MAPPING";
        private const float ReleaseGraceSeconds = 0.5f;

        /// <summary>Plays the motion now, as <see cref="NamedAnimation.Play"/> does.</summary>
        public static void Play(uint creature, AnimationClip clip, float duration)
        {
            Map(creature, clip, duration);
            AssignCommand(creature, () => PlayAnimation(AstromechAnimations.Work, 1f, duration));
        }

        /// <summary>Queues the motion behind the creature's current actions, as <see cref="NamedAnimation.Queue"/> does.</summary>
        public static void Queue(uint creature, AnimationClip clip, float duration)
        {
            AssignCommand(creature, () =>
            {
                // The clips are chosen when the motion starts: an earlier queued action may map its own.
                ActionDoCommand(() => Map(creature, clip, duration));
                ActionPlayAnimation(AstromechAnimations.Work, 1f, duration);
            });
        }

        private static void Map(uint creature, AnimationClip clip, float duration)
        {
            var hasRod = AstromechAnimations.TryGetRodClips(clip, out var rod);
            if (!hasRod)
            {
                Release(creature, GetLocalString(creature, MappingVariable));
                return;
            }

            ReplaceObjectAnimation(creature, EntrySource, rod.Entry);
            ReplaceObjectAnimation(creature, LoopSource, rod.Loop);

            // The rod must not outlive the action: later work at a bench would bring it out.
            var token = Guid.NewGuid().ToString();
            SetLocalString(creature, MappingVariable, token);
            AssignCommand(GetModule(), () => DelayCommand(duration + ReleaseGraceSeconds, () => Release(creature, token)));
        }

        private static void Release(uint creature, string token)
        {
            if (string.IsNullOrEmpty(token) || !GetIsObjectValid(creature) || GetLocalString(creature, MappingVariable) != token)
                return;

            ReplaceObjectAnimation(creature, EntrySource);
            ReplaceObjectAnimation(creature, LoopSource);
            DeleteLocalString(creature, MappingVariable);
        }
    }
}
