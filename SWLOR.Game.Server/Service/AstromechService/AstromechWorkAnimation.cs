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
        private const string MappingVariable = "ASTROMECH_WORK_MAPPING";
        private static readonly AstromechWorkPlayback Playback = new(new NativeRuntime(), ReleaseIdlePose);

        /// <summary>
        /// Plays the motion now, as <see cref="NamedAnimation.Play"/> does. Returns the token that
        /// <see cref="StopIfCurrent"/> takes to end it early.
        /// </summary>
        public static string Play(uint creature, AnimationClip clip, float duration)
        {
            var token = Begin(creature, clip, duration);
            AssignCommand(creature, () =>
            {
                // The caller may have stopped it before this runs (a window closed at once).
                if (Playback.IsCurrent(creature, token))
                    PlayAnimation(AstromechAnimations.Work, 1f, duration);
            });
            return token;
        }

        /// <summary>Queues the motion behind the creature's current actions, as <see cref="NamedAnimation.Queue"/> does.</summary>
        public static void Queue(uint creature, AnimationClip clip, float duration)
        {
            AssignCommand(creature, () =>
            {
                // The clips are chosen when the motion starts: an earlier queued action may map its own.
                ActionDoCommand(() => Begin(creature, clip, duration));
                ActionPlayAnimation(AstromechAnimations.Work, 1f, duration);
            });
        }

        /// <summary>
        /// Ends the motion this token started, unless a later one has replaced it: the rod is stowed or the tool arm
        /// drawn in at once instead of when its time is up (a cancelled cast, a craft that ends). Returns true when it
        /// ended a motion, and false for a token that is not a work motion's (a native preview's).
        /// The rod needs no check of its item: the engine holds an unequip until the clip is over.
        /// </summary>
        public static bool StopIfCurrent(uint creature, string token) => Playback.StopIfCurrent(creature, token);

        /// <summary>Puts the working clips back before another animation takes the creature over. Nothing is played.</summary>
        public static void ReleaseForNativePlayback(uint creature) => Playback.ReleaseForNativePlayback(creature);

        private static string Begin(uint creature, AnimationClip clip, float duration)
        {
            // One animation owns the creature at a time: a native preview still held gives way to the work motion.
            NamedAnimation.ReleaseForNativePlayback(creature);
            return Playback.Begin(creature, clip, duration);
        }

        private static void ReleaseIdlePose(uint creature, Func<bool> stillAtRest)
        {
            AssignCommand(creature, () =>
            {
                // Something else may have started before this runs; only an idle astromech is put back at rest.
                if (!stillAtRest() || GetCurrentHitPoints(creature) <= 0 || GetIsInCombat(creature) ||
                    GetCurrentAction(creature) != ActionType.Invalid)
                    return;
                PlayAnimation(Animation.LoopingPause, 1f, .1f);
            });
        }

        private sealed class NativeRuntime : INamedAnimationRuntime
        {
            public bool IsValid(uint creature) => GetIsObjectValid(creature) && GetObjectType(creature) == ObjectType.Creature;
            public string GetToken(uint creature) => GetLocalString(creature, MappingVariable);
            public void SetToken(uint creature, string token)
            {
                if (string.IsNullOrEmpty(token)) DeleteLocalString(creature, MappingVariable);
                else SetLocalString(creature, MappingVariable, token);
            }
            public void Replace(uint creature, string source, string replacement) => ReplaceObjectAnimation(creature, source, replacement);
            public ActionType CurrentAction(uint creature) => GetCurrentAction(creature);
            public void ClearActions(uint creature) => ClearAllActions(oObject: creature);
            public void Schedule(float seconds, Action callback) => AssignCommand(GetModule(), () => DelayCommand(seconds, callback));
        }
    }
}
