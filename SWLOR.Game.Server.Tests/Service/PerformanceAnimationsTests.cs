using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceAnimationsTests
{
    [Test]
    public void EveryRoutineHasItsOwnLoop()
    {
        var loops = Enum.GetValues<PerformanceRoutineType>()
            .Select(routine => PerformanceAnimations.GetLoopClip(
                PerformanceKind.Dance, PerformanceInstrumentFamily.None, routine))
            .ToList();

        loops.Should().OnlyHaveUniqueItems().And.NotContainNulls();
        PerformanceAnimations.GetLoopClip(PerformanceKind.Dance, PerformanceInstrumentFamily.None, PerformanceRoutineType.CantinaShuffle)
            .Should().BeSameAs(AuthoredAnimation.CantinaShuffle);
    }

    [TestCase(PerformanceInstrumentFamily.Strings, "sw_str_loop")]
    [TestCase(PerformanceInstrumentFamily.Horns, "sw_hrn_loop")]
    [TestCase(PerformanceInstrumentFamily.Vocals, "sw_voc_loop")]
    [TestCase(PerformanceInstrumentFamily.Pipes, "sw_pip_loop")]
    [TestCase(PerformanceInstrumentFamily.BowedStrings, "sw_vio_loop")]
    [TestCase(PerformanceInstrumentFamily.Percussion, "sw_tam_loop")]
    public void MusicLoopsFollowTheInstrumentFamily(PerformanceInstrumentFamily family, string expected)
    {
        PerformanceAnimations.GetLoopClip(PerformanceKind.Music, family, PerformanceRoutineType.VigorReel)
            .Name.Should().Be(expected);
    }

    [TestCase(PerformanceCue.Left, "sw_dn_stepl")]
    [TestCase(PerformanceCue.Up, "sw_dn_stepu")]
    [TestCase(PerformanceCue.Down, "sw_dn_stepd")]
    [TestCase(PerformanceCue.Right, "sw_dn_stepr")]
    public void DanceStepsMatchTheirCue(PerformanceCue cue, string expected)
    {
        PerformanceAnimations.GetCueClip(PerformanceKind.Dance, PerformanceInstrumentFamily.None, cue)
            .Name.Should().Be(expected);
    }

    [TestCase(PerformanceInstrumentFamily.Strings, PerformanceCue.Low, "sw_str_low")]
    [TestCase(PerformanceInstrumentFamily.Strings, PerformanceCue.Mid, "sw_str_mid")]
    [TestCase(PerformanceInstrumentFamily.Strings, PerformanceCue.High, "sw_str_high")]
    [TestCase(PerformanceInstrumentFamily.Horns, PerformanceCue.Low, "sw_hrn_low")]
    [TestCase(PerformanceInstrumentFamily.Horns, PerformanceCue.Mid, "sw_hrn_mid")]
    [TestCase(PerformanceInstrumentFamily.Horns, PerformanceCue.High, "sw_hrn_high")]
    [TestCase(PerformanceInstrumentFamily.Vocals, PerformanceCue.Low, "sw_voc_low")]
    [TestCase(PerformanceInstrumentFamily.Vocals, PerformanceCue.Mid, "sw_voc_mid")]
    [TestCase(PerformanceInstrumentFamily.Vocals, PerformanceCue.High, "sw_voc_high")]
    [TestCase(PerformanceInstrumentFamily.Pipes, PerformanceCue.Low, "sw_pip_low")]
    [TestCase(PerformanceInstrumentFamily.Pipes, PerformanceCue.Mid, "sw_pip_mid")]
    [TestCase(PerformanceInstrumentFamily.Pipes, PerformanceCue.High, "sw_pip_high")]
    [TestCase(PerformanceInstrumentFamily.BowedStrings, PerformanceCue.Low, "sw_vio_low")]
    [TestCase(PerformanceInstrumentFamily.BowedStrings, PerformanceCue.Mid, "sw_vio_mid")]
    [TestCase(PerformanceInstrumentFamily.BowedStrings, PerformanceCue.High, "sw_vio_high")]
    [TestCase(PerformanceInstrumentFamily.Percussion, PerformanceCue.Low, "sw_tam_low")]
    [TestCase(PerformanceInstrumentFamily.Percussion, PerformanceCue.Mid, "sw_tam_mid")]
    [TestCase(PerformanceInstrumentFamily.Percussion, PerformanceCue.High, "sw_tam_high")]
    public void NotesMatchTheirCueAndFamily(PerformanceInstrumentFamily family, PerformanceCue cue, string expected)
    {
        PerformanceAnimations.GetCueClip(PerformanceKind.Music, family, cue).Name.Should().Be(expected);
    }

    [Test]
    public void CuesFromTheOtherKindPlayNothing()
    {
        PerformanceAnimations.GetCueClip(PerformanceKind.Dance, PerformanceInstrumentFamily.None, PerformanceCue.Mid).Should().BeNull();
        PerformanceAnimations.GetCueClip(PerformanceKind.Music, PerformanceInstrumentFamily.Strings, PerformanceCue.Left).Should().BeNull();
    }

    [Test]
    public void DanceFlourishesAlternateSpinAndDip()
    {
        PerformanceAnimations.GetFlourishClip(PerformanceKind.Dance, PerformanceInstrumentFamily.None, 0).Should().BeSameAs(AuthoredAnimation.DanceSpin);
        PerformanceAnimations.GetFlourishClip(PerformanceKind.Dance, PerformanceInstrumentFamily.None, 1).Should().BeSameAs(AuthoredAnimation.DanceDip);
        PerformanceAnimations.GetFlourishClip(PerformanceKind.Dance, PerformanceInstrumentFamily.None, 2).Should().BeSameAs(AuthoredAnimation.DanceSpin);
    }

    [TestCase(PerformanceInstrumentFamily.Strings, "sw_str_flour")]
    [TestCase(PerformanceInstrumentFamily.Horns, "sw_hrn_flour")]
    [TestCase(PerformanceInstrumentFamily.Vocals, "sw_voc_flour")]
    [TestCase(PerformanceInstrumentFamily.Pipes, "sw_pip_flour")]
    [TestCase(PerformanceInstrumentFamily.BowedStrings, "sw_vio_flour")]
    [TestCase(PerformanceInstrumentFamily.Percussion, "sw_tam_flour")]
    public void MusicFlourishesFollowTheInstrumentFamily(PerformanceInstrumentFamily family, string expected)
    {
        PerformanceAnimations.GetFlourishClip(PerformanceKind.Music, family, 1).Name.Should().Be(expected);
    }

    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Strings, NamedAnimationHold.Keep)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Horns, NamedAnimationHold.LiftInPlace)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Vocals, NamedAnimationHold.LiftTorchInPlace)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Pipes, NamedAnimationHold.LiftInPlace)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.BowedStrings, NamedAnimationHold.LiftInPlace)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Percussion, NamedAnimationHold.LiftInPlace)]
    [TestCase(PerformanceKind.Dance, PerformanceInstrumentFamily.None, NamedAnimationHold.Keep)]
    public void PerformanceClipsNeverUseTheOneShotCarrier(
        PerformanceKind kind, PerformanceInstrumentFamily family, NamedAnimationHold expected)
    {
        PerformanceAnimations.GetHoldMode(kind, family).Should().Be(expected);
    }

    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Horns, "kloo_horn", NamedAnimationHold.Auto)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Vocals, "night_microplain", NamedAnimationHold.ClearTorch)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Vocals, "night_microbrand", NamedAnimationHold.ClearTorch)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Horns, "KLOO_HORN", NamedAnimationHold.Auto)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Horns, "slitherhorn", NamedAnimationHold.LiftInPlace)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Strings, "guitar", NamedAnimationHold.Keep)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Pipes, "panpipes", NamedAnimationHold.Auto)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.Percussion, "tambourine", NamedAnimationHold.Auto)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.BowedStrings, "violin_f", NamedAnimationHold.Auto)]
    [TestCase(PerformanceKind.Music, PerformanceInstrumentFamily.BowedStrings, "violin_m", NamedAnimationHold.Auto)]
    [TestCase(PerformanceKind.Dance, PerformanceInstrumentFamily.None, "kloo_horn", NamedAnimationHold.Keep)]
    [TestCase(PerformanceKind.Dance, PerformanceInstrumentFamily.None, "", NamedAnimationHold.Keep)]
    public void InstrumentsAuthoredWithoutTheCarryLayerClearItAtOpening(
        PerformanceKind kind, PerformanceInstrumentFamily family, string instrument, NamedAnimationHold expected)
    {
        PerformanceAnimations.GetOpeningHoldMode(kind, family, instrument).Should().Be(expected);
    }

    [Test]
    public void OpeningClipsAreTheToggleAbilityClips()
    {
        PerformanceAnimations.GetOpeningClip(PerformanceKind.Dance).Should().BeSameAs(AuthoredAnimation.Dance);
        PerformanceAnimations.GetOpeningClip(PerformanceKind.Music).Should().BeSameAs(AuthoredAnimation.PlayInstrument);
    }

    [TestCase(PerformanceInstrumentFamily.Vocals, "sw_voc_loop")]
    [TestCase(PerformanceInstrumentFamily.Pipes, "sw_pip_loop")]
    [TestCase(PerformanceInstrumentFamily.BowedStrings, "sw_vio_loop")]
    [TestCase(PerformanceInstrumentFamily.Percussion, "sw_tam_loop")]
    public void AdditionalInstrumentsOpenInTheirOwnPlayingPose(PerformanceInstrumentFamily family, string expected)
    {
        PerformanceAnimations.GetOpeningClip(PerformanceKind.Music, family).Name.Should().Be(expected);
    }

    [Test]
    public void UgnaughtsSingWithTheMicrophoneLoweredToTheirMouth()
    {
        var sung = new[]
        {
            (PerformanceAnimations.GetOpeningClip(PerformanceKind.Music, PerformanceInstrumentFamily.Vocals), "sw_vlm_loop"),
            (PerformanceAnimations.GetLoopClip(PerformanceKind.Music, PerformanceInstrumentFamily.Vocals, PerformanceRoutineType.CantinaShuffle), "sw_vlm_loop"),
            (PerformanceAnimations.GetCueClip(PerformanceKind.Music, PerformanceInstrumentFamily.Vocals, PerformanceCue.Low), "sw_vlm_low"),
            (PerformanceAnimations.GetCueClip(PerformanceKind.Music, PerformanceInstrumentFamily.Vocals, PerformanceCue.Mid), "sw_vlm_mid"),
            (PerformanceAnimations.GetCueClip(PerformanceKind.Music, PerformanceInstrumentFamily.Vocals, PerformanceCue.High), "sw_vlm_high"),
            (PerformanceAnimations.GetFlourishClip(PerformanceKind.Music, PerformanceInstrumentFamily.Vocals, 0), "sw_vlm_flour"),
        };
        foreach (var (clip, lowered) in sung)
        {
            var fitted = PerformanceAnimations.FitToBody(clip, AppearanceType.Ugnaught);
            fitted.Name.Should().Be(lowered);
            fitted.Duration.Should().Be(clip.Duration, "the lowered copy keeps the clip's timing");
        }
    }

    [TestCase(AppearanceType.Human)]
    [TestCase(AppearanceType.Ewok)]
    [TestCase(AppearanceType.Cathar)]
    [TestCase(AppearanceType.Droid)]
    public void BodiesWhoseMouthMeetsTheMicrophoneKeepTheOrdinarySingingClips(AppearanceType appearance)
    {
        foreach (var clip in new[] { AuthoredAnimation.VocalsLoop, AuthoredAnimation.VocalsLow, AuthoredAnimation.VocalsMid,
                     AuthoredAnimation.VocalsHigh, AuthoredAnimation.VocalsFlourish })
            PerformanceAnimations.FitToBody(clip, appearance).Should().BeSameAs(clip);
    }

    [Test]
    public void OnlySingingIsRefittedForUgnaughts()
    {
        foreach (var clip in new[] { AuthoredAnimation.PipesLoop, AuthoredAnimation.TambourineFlourish, AuthoredAnimation.HornLoop,
                     AuthoredAnimation.CantinaShuffle, AuthoredAnimation.PerformanceBow })
            PerformanceAnimations.FitToBody(clip, AppearanceType.Ugnaught).Should().BeSameAs(clip);
        PerformanceAnimations.FitToBody(null, AppearanceType.Ugnaught).Should().BeNull();
    }
}
