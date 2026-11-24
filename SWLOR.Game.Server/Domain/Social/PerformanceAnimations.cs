using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Chooses the authored clips a performance plays. Dance clips start and end in one dance stance, and each
    /// instrument family's clips start and end in its playing hold, so any clip can follow any other.
    /// </summary>
    public static class PerformanceAnimations
    {
        // The same singing motion with the microphone hand lowered.
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _lowMouthVocals = new Dictionary<AnimationClip, AnimationClip>
        {
            [AuthoredAnimation.VocalsLoop] = AuthoredAnimation.VocalsLowMouthLoop,
            [AuthoredAnimation.VocalsLow] = AuthoredAnimation.VocalsLowMouthLow,
            [AuthoredAnimation.VocalsMid] = AuthoredAnimation.VocalsLowMouthMid,
            [AuthoredAnimation.VocalsHigh] = AuthoredAnimation.VocalsLowMouthHigh,
            [AuthoredAnimation.VocalsFlourish] = AuthoredAnimation.VocalsLowMouthFlourish,
        }.ToFrozenDictionary();

        // The same singing motion with the microphone hand lowered half as far. The Wookiee sets have no middle accent
        // of their own and play the low one for it.
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _midMouthVocals = new Dictionary<AnimationClip, AnimationClip>
        {
            [AuthoredAnimation.VocalsLoop] = AuthoredAnimation.VocalsMidMouthLoop,
            [AuthoredAnimation.VocalsLow] = AuthoredAnimation.VocalsMidMouthLow,
            [AuthoredAnimation.VocalsMid] = AuthoredAnimation.VocalsMidMouthLow,
            [AuthoredAnimation.VocalsHigh] = AuthoredAnimation.VocalsMidMouthHigh,
            [AuthoredAnimation.VocalsFlourish] = AuthoredAnimation.VocalsMidMouthFlourish,
        }.ToFrozenDictionary();

        // The same pipes motion with the hands set apart.
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _widePipes = new Dictionary<AnimationClip, AnimationClip>
        {
            [AuthoredAnimation.PipesLoop] = AuthoredAnimation.PipesWideLoop,
            [AuthoredAnimation.PipesLow] = AuthoredAnimation.PipesWideLow,
            [AuthoredAnimation.PipesMid] = AuthoredAnimation.PipesWideLow,
            [AuthoredAnimation.PipesHigh] = AuthoredAnimation.PipesWideHigh,
            [AuthoredAnimation.PipesFlourish] = AuthoredAnimation.PipesWideFlourish,
        }.ToFrozenDictionary();

        // The same tambourine motion with the holding hand and the striking palm set apart.
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _wideTambourine = new Dictionary<AnimationClip, AnimationClip>
        {
            [AuthoredAnimation.TambourineLoop] = AuthoredAnimation.TambourineWideLoop,
            [AuthoredAnimation.TambourineLow] = AuthoredAnimation.TambourineWideLow,
            [AuthoredAnimation.TambourineMid] = AuthoredAnimation.TambourineWideLow,
            [AuthoredAnimation.TambourineHigh] = AuthoredAnimation.TambourineWideHigh,
            [AuthoredAnimation.TambourineFlourish] = AuthoredAnimation.TambourineWideFlourish,
        }.ToFrozenDictionary();

        // The horn motion with the hands spread along the Flanged Jessoon's long body (tools/DeriveJessoonPerformanceMotion.py),
        // one set per body: each holds the instrument at that body's mouth with its hands where the male Human's are.
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonHuman = JessoonSet(
            AuthoredAnimation.JessoonHumanLoop, AuthoredAnimation.JessoonHumanLow, AuthoredAnimation.JessoonHumanMid,
            AuthoredAnimation.JessoonHumanHigh, AuthoredAnimation.JessoonHumanFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonHumanFemale = JessoonSet(
            AuthoredAnimation.JessoonHumanFemaleLoop, AuthoredAnimation.JessoonHumanFemaleLow, AuthoredAnimation.JessoonHumanFemaleMid,
            AuthoredAnimation.JessoonHumanFemaleHigh, AuthoredAnimation.JessoonHumanFemaleFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonTrandoshan = JessoonSet(
            AuthoredAnimation.JessoonTrandoshanLoop, AuthoredAnimation.JessoonTrandoshanLow, AuthoredAnimation.JessoonTrandoshanMid,
            AuthoredAnimation.JessoonTrandoshanHigh, AuthoredAnimation.JessoonTrandoshanFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonTrandoshanFemale = JessoonSet(
            AuthoredAnimation.JessoonTrandoshanFemaleLoop, AuthoredAnimation.JessoonTrandoshanFemaleLow, AuthoredAnimation.JessoonTrandoshanFemaleMid,
            AuthoredAnimation.JessoonTrandoshanFemaleHigh, AuthoredAnimation.JessoonTrandoshanFemaleFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonMonCalamari = JessoonSet(
            AuthoredAnimation.JessoonMonCalamariLoop, AuthoredAnimation.JessoonMonCalamariLow, AuthoredAnimation.JessoonMonCalamariMid,
            AuthoredAnimation.JessoonMonCalamariHigh, AuthoredAnimation.JessoonMonCalamariFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonMonCalamariFemale = JessoonSet(
            AuthoredAnimation.JessoonMonCalamariFemaleLoop, AuthoredAnimation.JessoonMonCalamariFemaleLow, AuthoredAnimation.JessoonMonCalamariFemaleMid,
            AuthoredAnimation.JessoonMonCalamariFemaleHigh, AuthoredAnimation.JessoonMonCalamariFemaleFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonBothan = JessoonSet(
            AuthoredAnimation.JessoonBothanLoop, AuthoredAnimation.JessoonBothanLow, AuthoredAnimation.JessoonBothanMid,
            AuthoredAnimation.JessoonBothanHigh, AuthoredAnimation.JessoonBothanFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonBothanFemale = JessoonSet(
            AuthoredAnimation.JessoonBothanFemaleLoop, AuthoredAnimation.JessoonBothanFemaleLow, AuthoredAnimation.JessoonBothanFemaleMid,
            AuthoredAnimation.JessoonBothanFemaleHigh, AuthoredAnimation.JessoonBothanFemaleFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonRodian = JessoonSet(
            AuthoredAnimation.JessoonRodianLoop, AuthoredAnimation.JessoonRodianLow, AuthoredAnimation.JessoonRodianMid,
            AuthoredAnimation.JessoonRodianHigh, AuthoredAnimation.JessoonRodianFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonWookiee = JessoonSet(
            AuthoredAnimation.JessoonWookieeLoop, AuthoredAnimation.JessoonWookieeLow, AuthoredAnimation.JessoonWookieeMid,
            AuthoredAnimation.JessoonWookieeHigh, AuthoredAnimation.JessoonWookieeFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonWookieeFemale = JessoonSet(
            AuthoredAnimation.JessoonWookieeFemaleLoop, AuthoredAnimation.JessoonWookieeFemaleLow, AuthoredAnimation.JessoonWookieeFemaleMid,
            AuthoredAnimation.JessoonWookieeFemaleHigh, AuthoredAnimation.JessoonWookieeFemaleFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonCathar = JessoonSet(
            AuthoredAnimation.JessoonCatharLoop, AuthoredAnimation.JessoonCatharLow, AuthoredAnimation.JessoonCatharMid,
            AuthoredAnimation.JessoonCatharHigh, AuthoredAnimation.JessoonCatharFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonCatharFemale = JessoonSet(
            AuthoredAnimation.JessoonCatharFemaleLoop, AuthoredAnimation.JessoonCatharFemaleLow, AuthoredAnimation.JessoonCatharFemaleMid,
            AuthoredAnimation.JessoonCatharFemaleHigh, AuthoredAnimation.JessoonCatharFemaleFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonUgnaught = JessoonSet(
            AuthoredAnimation.JessoonUgnaughtLoop, AuthoredAnimation.JessoonUgnaughtLow, AuthoredAnimation.JessoonUgnaughtMid,
            AuthoredAnimation.JessoonUgnaughtHigh, AuthoredAnimation.JessoonUgnaughtFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonUgnaughtFemale = JessoonSet(
            AuthoredAnimation.JessoonUgnaughtFemaleLoop, AuthoredAnimation.JessoonUgnaughtFemaleLow, AuthoredAnimation.JessoonUgnaughtFemaleMid,
            AuthoredAnimation.JessoonUgnaughtFemaleHigh, AuthoredAnimation.JessoonUgnaughtFemaleFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonEwok = JessoonSet(
            AuthoredAnimation.JessoonEwokLoop, AuthoredAnimation.JessoonEwokLow, AuthoredAnimation.JessoonEwokMid,
            AuthoredAnimation.JessoonEwokHigh, AuthoredAnimation.JessoonEwokFlourish);
        private static readonly FrozenDictionary<AnimationClip, AnimationClip> _jessoonEwokFemale = JessoonSet(
            AuthoredAnimation.JessoonEwokFemaleLoop, AuthoredAnimation.JessoonEwokFemaleLow, AuthoredAnimation.JessoonEwokFemaleMid,
            AuthoredAnimation.JessoonEwokFemaleHigh, AuthoredAnimation.JessoonEwokFemaleFlourish);

        private static FrozenDictionary<AnimationClip, AnimationClip> JessoonSet(
            AnimationClip loop, AnimationClip low, AnimationClip mid, AnimationClip high, AnimationClip flourish) =>
            new Dictionary<AnimationClip, AnimationClip>
            {
                [AuthoredAnimation.HornLoop] = loop,
                [AuthoredAnimation.HornLow] = low,
                [AuthoredAnimation.HornMid] = mid,
                [AuthoredAnimation.HornHigh] = high,
                [AuthoredAnimation.HornFlourish] = flourish,
            }.ToFrozenDictionary();

        /// <summary>
        /// The horn clips are posed for the kloo horn, whose hands sit close together near the top of a short body. The
        /// Flanged Jessoon is a long saxophone, so it plays copies with the right hand low on the body and the left hand
        /// closed on the tube under the flange. Every body holds the same instrument, so its hands belong at the same
        /// places on it, and the bodies' arms, heads and hands differ too much for one copy: each body plays the copy solved
        /// on it. KelDor and the droids hold it well in the Human copies, and the female Rodian in the female Bothan's.
        /// </summary>
        public static AnimationClip FitToInstrument(
            AnimationClip clip, string instrumentResref, AppearanceType appearance, Gender gender)
        {
            if (clip == null || !string.Equals(instrumentResref, "jessoon", StringComparison.OrdinalIgnoreCase))
                return clip;
            var copies = (appearance, female: gender == Gender.Female) switch
            {
                (AppearanceType.Droid, _) => _jessoonHuman,
                (AppearanceType.Trandoshan, false) => _jessoonTrandoshan,
                (AppearanceType.Trandoshan, true) => _jessoonTrandoshanFemale,
                (AppearanceType.MonCalamari, false) => _jessoonMonCalamari,
                (AppearanceType.MonCalamari, true) => _jessoonMonCalamariFemale,
                (AppearanceType.Bothan, false) => _jessoonBothan,
                (AppearanceType.Bothan or AppearanceType.Rodian, true) => _jessoonBothanFemale,
                (AppearanceType.Rodian, false) => _jessoonRodian,
                (AppearanceType.Wookiee, false) => _jessoonWookiee,
                (AppearanceType.Wookiee, true) => _jessoonWookieeFemale,
                (AppearanceType.Cathar, false) => _jessoonCathar,
                (AppearanceType.Cathar, true) => _jessoonCatharFemale,
                (AppearanceType.Ugnaught, false) => _jessoonUgnaught,
                (AppearanceType.Ugnaught, true) => _jessoonUgnaughtFemale,
                (AppearanceType.Ewok, false) => _jessoonEwok,
                (AppearanceType.Ewok, true) => _jessoonEwokFemale,
                (_, true) => _jessoonHumanFemale,
                _ => _jessoonHuman
            };
            return copies.TryGetValue(clip, out var copy) ? copy : clip;
        }

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
