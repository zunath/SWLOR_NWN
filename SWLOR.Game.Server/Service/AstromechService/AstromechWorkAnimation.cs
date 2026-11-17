using System;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

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

        /// <summary>
        /// Plays the motion now, as <see cref="NamedAnimation.Play"/> does. Returns the token that
        /// <see cref="StopIfCurrent"/> takes to end it early.
        /// </summary>
        public static string Play(uint creature, AnimationClip clip, float duration)
        {
            var token = Map(creature, clip, duration);
            AssignCommand(creature, () => PlayAnimation(AstromechAnimations.Work, 1f, duration));
            return token;
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

        /// <summary>
        /// Ends the motion this token started, unless a later one has replaced it: the rod is stowed or the tool arm
        /// drawn in at once instead of when its time is up (a cancelled cast, a craft that ends). Returns true when it
        /// ended a motion. The rod needs no check of its item: the engine holds an unequip until the clip is over.
        /// </summary>
        public static bool StopIfCurrent(uint creature, string token)
        {
            if (!IsCurrent(creature, token))
                return false;

            Release(creature, token);
            AssignCommand(creature, () =>
            {
                // Something else may have started before this runs; only an idle astromech is put back at rest.
                if (GetCurrentHitPoints(creature) <= 0 || GetIsInCombat(creature) ||
                    GetCurrentAction(creature) != ActionType.Invalid ||
                    !string.IsNullOrEmpty(GetLocalString(creature, MappingVariable)))
                    return;
                PlayAnimation(Animation.LoopingPause, 1f, .1f);
            });
            return true;
        }

        private static string Map(uint creature, AnimationClip clip, float duration)
        {
            if (AstromechAnimations.TryGetRodClips(clip, out var rod))
            {
                ReplaceObjectAnimation(creature, EntrySource, rod.Entry);
                ReplaceObjectAnimation(creature, LoopSource, rod.Loop);
            }
            else
            {
                ReplaceObjectAnimation(creature, EntrySource);
                ReplaceObjectAnimation(creature, LoopSource);
            }

            // The rod must not outlive the action: later work at a bench would bring it out.
            var token = Guid.NewGuid().ToString();
            SetLocalString(creature, MappingVariable, token);
            AssignCommand(GetModule(), () => DelayCommand(duration + ReleaseGraceSeconds, () => Release(creature, token)));
            return token;
        }

        private static bool IsCurrent(uint creature, string token) =>
            !string.IsNullOrEmpty(token) && GetIsObjectValid(creature) && GetLocalString(creature, MappingVariable) == token;

        private static void Release(uint creature, string token)
        {
            if (!IsCurrent(creature, token))
                return;

            ReplaceObjectAnimation(creature, EntrySource);
            ReplaceObjectAnimation(creature, LoopSource);
            DeleteLocalString(creature, MappingVariable);
        }
    }
}
