using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.PerformanceService;

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
    public void MusicFlourishesFollowTheInstrumentFamily(PerformanceInstrumentFamily family, string expected)
    {
        PerformanceAnimations.GetFlourishClip(PerformanceKind.Music, family, 1).Name.Should().Be(expected);
    }

    [Test]
    public void OpeningClipsAreTheToggleAbilityClips()
    {
        PerformanceAnimations.GetOpeningClip(PerformanceKind.Dance).Should().BeSameAs(AuthoredAnimation.Dance);
        PerformanceAnimations.GetOpeningClip(PerformanceKind.Music).Should().BeSameAs(AuthoredAnimation.PlayInstrument);
    }
}
