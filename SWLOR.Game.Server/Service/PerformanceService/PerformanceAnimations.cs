using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// Chooses the authored clips a performance plays. Dance clips start and end in one dance stance, and each
    /// instrument family's clips start and end in its playing hold, so any clip can follow any other.
    /// </summary>
    public static class PerformanceAnimations
    {
        /// <summary>
        /// Performance clips loop for minutes, so they stay on the looping carrier. The instruments' base item is wielded
        /// like a polearm: the horn clips pose the arm around the horn themselves, so that hold is lifted for them, while
        /// the strings clips were authored around it. Dancers keep whatever their weapon holds.
        /// </summary>
        public static NamedAnimationHold GetHoldMode(PerformanceKind kind, PerformanceInstrumentFamily family) =>
            kind == PerformanceKind.Music && family == PerformanceInstrumentFamily.Horns
                ? NamedAnimationHold.LiftInPlace
                : NamedAnimationHold.Keep;

        public static AnimationClip GetOpeningClip(PerformanceKind kind)
        {
            return kind == PerformanceKind.Dance ? AuthoredAnimation.Dance : AuthoredAnimation.PlayInstrument;
        }

        public static AnimationClip GetLoopClip(PerformanceKind kind, PerformanceInstrumentFamily family, PerformanceRoutineType routine)
        {
            if (kind == PerformanceKind.Music)
                return family == PerformanceInstrumentFamily.Horns ? AuthoredAnimation.HornLoop : AuthoredAnimation.StringsLoop;

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

            var horns = family == PerformanceInstrumentFamily.Horns;
            return cue switch
            {
                PerformanceCue.Low => horns ? AuthoredAnimation.HornLow : AuthoredAnimation.StringsLow,
                PerformanceCue.Mid => horns ? AuthoredAnimation.HornMid : AuthoredAnimation.StringsMid,
                PerformanceCue.High => horns ? AuthoredAnimation.HornHigh : AuthoredAnimation.StringsHigh,
                _ => null
            };
        }

        /// <summary>Dancers alternate a spin and a dip; musicians play their family's flourish.</summary>
        public static AnimationClip GetFlourishClip(PerformanceKind kind, PerformanceInstrumentFamily family, int flourishesPlayed)
        {
            if (kind == PerformanceKind.Music)
                return family == PerformanceInstrumentFamily.Horns ? AuthoredAnimation.HornFlourish : AuthoredAnimation.StringsFlourish;

            return flourishesPlayed % 2 == 0 ? AuthoredAnimation.DanceSpin : AuthoredAnimation.DanceDip;
        }
    }
}
