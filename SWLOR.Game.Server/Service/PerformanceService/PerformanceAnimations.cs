using System.Collections.Generic;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// Chooses the authored clips a performance plays. Dance clips start and end in one dance stance, and each
    /// instrument family's clips start and end in its playing hold, so any clip can follow any other.
    /// </summary>
    public static class PerformanceAnimations
    {
        // The same singing motion with the microphone hand lowered.
        private static readonly Dictionary<AnimationClip, AnimationClip> _lowMouthVocals = new()
        {
            [AuthoredAnimation.VocalsLoop] = AuthoredAnimation.VocalsLowMouthLoop,
            [AuthoredAnimation.VocalsLow] = AuthoredAnimation.VocalsLowMouthLow,
            [AuthoredAnimation.VocalsMid] = AuthoredAnimation.VocalsLowMouthMid,
            [AuthoredAnimation.VocalsHigh] = AuthoredAnimation.VocalsLowMouthHigh,
            [AuthoredAnimation.VocalsFlourish] = AuthoredAnimation.VocalsLowMouthFlourish,
        };

        // The same singing motion with the microphone hand lowered half as far. The Wookiee sets have no middle accent
        // of their own and play the low one for it.
        private static readonly Dictionary<AnimationClip, AnimationClip> _midMouthVocals = new()
        {
            [AuthoredAnimation.VocalsLoop] = AuthoredAnimation.VocalsMidMouthLoop,
            [AuthoredAnimation.VocalsLow] = AuthoredAnimation.VocalsMidMouthLow,
            [AuthoredAnimation.VocalsMid] = AuthoredAnimation.VocalsMidMouthLow,
            [AuthoredAnimation.VocalsHigh] = AuthoredAnimation.VocalsMidMouthHigh,
            [AuthoredAnimation.VocalsFlourish] = AuthoredAnimation.VocalsMidMouthFlourish,
        };

        // The same pipes motion with the hands set apart.
        private static readonly Dictionary<AnimationClip, AnimationClip> _widePipes = new()
        {
            [AuthoredAnimation.PipesLoop] = AuthoredAnimation.PipesWideLoop,
            [AuthoredAnimation.PipesLow] = AuthoredAnimation.PipesWideLow,
            [AuthoredAnimation.PipesMid] = AuthoredAnimation.PipesWideLow,
            [AuthoredAnimation.PipesHigh] = AuthoredAnimation.PipesWideHigh,
            [AuthoredAnimation.PipesFlourish] = AuthoredAnimation.PipesWideFlourish,
        };

        // The same tambourine motion with the holding hand and the striking palm set apart.
        private static readonly Dictionary<AnimationClip, AnimationClip> _wideTambourine = new()
        {
            [AuthoredAnimation.TambourineLoop] = AuthoredAnimation.TambourineWideLoop,
            [AuthoredAnimation.TambourineLow] = AuthoredAnimation.TambourineWideLow,
            [AuthoredAnimation.TambourineMid] = AuthoredAnimation.TambourineWideLow,
            [AuthoredAnimation.TambourineHigh] = AuthoredAnimation.TambourineWideHigh,
            [AuthoredAnimation.TambourineFlourish] = AuthoredAnimation.TambourineWideFlourish,
        };

        /// <summary>
        /// One clip serves every body, and the singing clips hold the microphone at a Human-height mouth. An Ugnaught's
        /// mouth sits lower on its head, where that hand lands over the eyes, so Ugnaughts play the lowered copies; a
        /// female Wookiee's mouth sits between the two, so she plays the half-lowered ones. A Wookiee's large hands
        /// overlap at the mouth and push the pipes aside, and their thick forearms cross over the tambourine's skin, so
        /// Wookiees play both with the hands apart. Held instruments are otherwise fitted per species by moving the item
        /// (<c>HornFit</c>); a hand in the wrong place needs its own clips.
        /// </summary>
        public static AnimationClip FitToBody(AnimationClip clip, AppearanceType appearance, Gender gender)
        {
            if (clip == null)
                return null;
            if (appearance == AppearanceType.Wookiee &&
                (_widePipes.TryGetValue(clip, out var wide) || _wideTambourine.TryGetValue(clip, out wide)))
                return wide;
            if (appearance == AppearanceType.Wookiee && gender == Gender.Female &&
                _midMouthVocals.TryGetValue(clip, out var halfLowered))
                return halfLowered;
            return appearance == AppearanceType.Ugnaught && _lowMouthVocals.TryGetValue(clip, out var lowered)
                ? lowered
                : clip;
        }

        /// <summary>
        /// Performance clips loop for minutes, so they stay on the looping carrier. The instruments' base item is wielded
        /// like a polearm: horns and the new held props pose their arms themselves, while the plucked strings were authored
        /// around the native carry layer. Singers lift the microphone's left torch layer. Dancers keep their weapon holds.
        /// </summary>
        public static NamedAnimationHold GetHoldMode(PerformanceKind kind, PerformanceInstrumentFamily family) =>
            kind == PerformanceKind.Music && family == PerformanceInstrumentFamily.Vocals
                ? NamedAnimationHold.LiftTorchInPlace :
            kind == PerformanceKind.Music &&
            family is PerformanceInstrumentFamily.Horns or PerformanceInstrumentFamily.Pipes or PerformanceInstrumentFamily.Percussion or PerformanceInstrumentFamily.BowedStrings
                ? NamedAnimationHold.LiftInPlace
                : NamedAnimationHold.Keep;

        /// <summary>
        /// The looping carrier only stops the polearm hold from refreshing; a hold already on the character stays until the
        /// one-shot carrier removes it. An instrument played without that hold opens on that carrier once, so the loop that
        /// follows starts with the arm free. Repeating it would drop the pose every cycle, so nothing after the opening uses it.
        /// </summary>
        public static NamedAnimationHold GetOpeningHoldMode(
            PerformanceKind kind, PerformanceInstrumentFamily family, string instrumentResref) =>
            kind == PerformanceKind.Music && family == PerformanceInstrumentFamily.Vocals
                ? NamedAnimationHold.ClearTorch :
            kind == PerformanceKind.Music && PerformanceInstrumentCatalog.PlaysWithoutCarryLayer(instrumentResref)
                ? NamedAnimationHold.Auto
                : GetHoldMode(kind, family);

        public static AnimationClip GetOpeningClip(PerformanceKind kind, PerformanceInstrumentFamily family = PerformanceInstrumentFamily.None)
        {
            if (kind == PerformanceKind.Music &&
                family is PerformanceInstrumentFamily.Vocals or PerformanceInstrumentFamily.Pipes or PerformanceInstrumentFamily.BowedStrings or PerformanceInstrumentFamily.Percussion)
                return GetLoopClip(kind, family, PerformanceRoutineType.CantinaShuffle);

            return kind == PerformanceKind.Dance ? AuthoredAnimation.Dance : AuthoredAnimation.PlayInstrument;
        }

        public static AnimationClip GetLoopClip(PerformanceKind kind, PerformanceInstrumentFamily family, PerformanceRoutineType routine)
        {
            if (kind == PerformanceKind.Music)
                return family switch
                {
                    PerformanceInstrumentFamily.Horns => AuthoredAnimation.HornLoop,
                    PerformanceInstrumentFamily.Vocals => AuthoredAnimation.VocalsLoop,
                    PerformanceInstrumentFamily.Pipes => AuthoredAnimation.PipesLoop,
                    PerformanceInstrumentFamily.BowedStrings => AuthoredAnimation.ViolinLoop,
                    PerformanceInstrumentFamily.Percussion => AuthoredAnimation.TambourineLoop,
                    _ => AuthoredAnimation.StringsLoop
                };

            return routine switch
            {
                PerformanceRoutineType.LightFeet => AuthoredAnimation.LightFeet,
                PerformanceRoutineType.PowerStep => AuthoredAnimation.PowerStep,
                PerformanceRoutineType.Quickstep => AuthoredAnimation.Quickstep,
                PerformanceRoutineType.VigorReel => AuthoredAnimation.VigorReel,
                _ => AuthoredAnimation.CantinaShuffle
            };
        }

        /// <summary>The accent for a matched cue, or null for a cue the performance kind does not use.</summary>
        public static AnimationClip GetCueClip(PerformanceKind kind, PerformanceInstrumentFamily family, PerformanceCue cue)
        {
            if (kind == PerformanceKind.Dance)
            {
                return cue switch
                {
                    PerformanceCue.Left => AuthoredAnimation.DanceStepLeft,
                    PerformanceCue.Up => AuthoredAnimation.DanceStepUp,
                    PerformanceCue.Down => AuthoredAnimation.DanceStepDown,
                    PerformanceCue.Right => AuthoredAnimation.DanceStepRight,
                    _ => null
                };
            }

            return (family, cue) switch
            {
                (PerformanceInstrumentFamily.Horns, PerformanceCue.Low) => AuthoredAnimation.HornLow,
                (PerformanceInstrumentFamily.Horns, PerformanceCue.Mid) => AuthoredAnimation.HornMid,
                (PerformanceInstrumentFamily.Horns, PerformanceCue.High) => AuthoredAnimation.HornHigh,
                (PerformanceInstrumentFamily.Vocals, PerformanceCue.Low) => AuthoredAnimation.VocalsLow,
                (PerformanceInstrumentFamily.Vocals, PerformanceCue.Mid) => AuthoredAnimation.VocalsMid,
                (PerformanceInstrumentFamily.Vocals, PerformanceCue.High) => AuthoredAnimation.VocalsHigh,
                (PerformanceInstrumentFamily.Pipes, PerformanceCue.Low) => AuthoredAnimation.PipesLow,
                (PerformanceInstrumentFamily.Pipes, PerformanceCue.Mid) => AuthoredAnimation.PipesMid,
                (PerformanceInstrumentFamily.Pipes, PerformanceCue.High) => AuthoredAnimation.PipesHigh,
                (PerformanceInstrumentFamily.BowedStrings, PerformanceCue.Low) => AuthoredAnimation.ViolinLow,
                (PerformanceInstrumentFamily.BowedStrings, PerformanceCue.Mid) => AuthoredAnimation.ViolinMid,
                (PerformanceInstrumentFamily.BowedStrings, PerformanceCue.High) => AuthoredAnimation.ViolinHigh,
                (PerformanceInstrumentFamily.Percussion, PerformanceCue.Low) => AuthoredAnimation.TambourineLow,
                (PerformanceInstrumentFamily.Percussion, PerformanceCue.Mid) => AuthoredAnimation.TambourineMid,
                (PerformanceInstrumentFamily.Percussion, PerformanceCue.High) => AuthoredAnimation.TambourineHigh,
                (_, PerformanceCue.Low) => AuthoredAnimation.StringsLow,
                (_, PerformanceCue.Mid) => AuthoredAnimation.StringsMid,
                (_, PerformanceCue.High) => AuthoredAnimation.StringsHigh,
                _ => null
            };
        }

        /// <summary>Dancers alternate a spin and a dip; musicians play their family's flourish.</summary>
        public static AnimationClip GetFlourishClip(PerformanceKind kind, PerformanceInstrumentFamily family, int flourishesPlayed)
        {
            if (kind == PerformanceKind.Music)
                return family switch
                {
                    PerformanceInstrumentFamily.Horns => AuthoredAnimation.HornFlourish,
                    PerformanceInstrumentFamily.Vocals => AuthoredAnimation.VocalsFlourish,
                    PerformanceInstrumentFamily.Pipes => AuthoredAnimation.PipesFlourish,
                    PerformanceInstrumentFamily.BowedStrings => AuthoredAnimation.ViolinFlourish,
                    PerformanceInstrumentFamily.Percussion => AuthoredAnimation.TambourineFlourish,
                    _ => AuthoredAnimation.StringsFlourish
                };

            return flourishesPlayed % 2 == 0 ? AuthoredAnimation.DanceSpin : AuthoredAnimation.DanceDip;
        }
    }
}
