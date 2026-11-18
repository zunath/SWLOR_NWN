using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class AstromechWorkPlaybackTests
{
    private const uint Droid = 1;

    [Test]
    public void AFishingClip_BringsTheRodOut_AndAnyOtherClipKeepsTheToolArm()
    {
        var runtime = new Runtime();
        var playback = new AstromechWorkPlayback(runtime);

        var cast = playback.Begin(Droid, AuthoredAnimation.Fishing6, 8f);

        playback.IsCurrent(Droid, cast).Should().BeTrue();
        runtime.Replacements[AstromechWorkPlayback.EntrySource].Should().Be("cfish");
        runtime.Replacements[AstromechWorkPlayback.LoopSource].Should().Be("cfishlp");

        var craft = playback.Begin(Droid, new AnimationClip("sw_bench", 3), 3f);

        playback.IsCurrent(Droid, craft).Should().BeTrue();
        playback.IsCurrent(Droid, cast).Should().BeFalse();
        runtime.Replacements[AstromechWorkPlayback.EntrySource].Should().BeEmpty();
        runtime.Replacements[AstromechWorkPlayback.LoopSource].Should().BeEmpty();
    }

    [Test]
    public void TheRodIsStowed_WhenTheMotionsTimeIsUp()
    {
        var runtime = new Runtime();
        var releases = 0;
        var playback = new AstromechWorkPlayback(runtime, (_, _) => releases++);

        playback.Begin(Droid, AuthoredAnimation.Reel, 3f);
        runtime.Delays.Should().Equal(3f + AstromechWorkPlayback.ReleaseGraceSeconds);
        runtime.Callbacks[0]();

        runtime.Token.Should().BeEmpty();
        runtime.Replacements[AstromechWorkPlayback.EntrySource].Should().BeEmpty();
        runtime.Replacements[AstromechWorkPlayback.LoopSource].Should().BeEmpty();
        releases.Should().Be(0, "the motion ended by itself, so nothing is played over what follows");
    }

    [Test]
    public void StoppingAMotion_StowsTheRodAtOnce_AndPutsTheDroidBackAtRest()
    {
        var runtime = new Runtime();
        var atRest = new List<Func<bool>>();
        var playback = new AstromechWorkPlayback(runtime, (_, stillAtRest) => atRest.Add(stillAtRest));
        var cast = playback.Begin(Droid, AuthoredAnimation.Fishing7, 8f);

        playback.StopIfCurrent(Droid, cast).Should().BeTrue();

        runtime.Token.Should().BeEmpty();
        runtime.Replacements[AstromechWorkPlayback.EntrySource].Should().BeEmpty();
        runtime.Replacements[AstromechWorkPlayback.LoopSource].Should().BeEmpty();
        atRest.Should().ContainSingle();
        atRest[0]().Should().BeTrue();

        // A second stop, and the motion's own timeout, find nothing left to end.
        playback.StopIfCurrent(Droid, cast).Should().BeFalse();
        runtime.Callbacks[0]();
        atRest.Should().ContainSingle();

        // A motion that starts before the rest pose is played keeps the droid working.
        playback.Begin(Droid, new AnimationClip("sw_bench", 3), 3f);
        atRest[0]().Should().BeFalse();
    }

    [Test]
    public void AReplacedMotion_CanNeitherStopNorReleaseItsReplacement()
    {
        var runtime = new Runtime();
        var releases = 0;
        var playback = new AstromechWorkPlayback(runtime, (_, _) => releases++);
        var craft = playback.Begin(Droid, new AnimationClip("sw_bench", 3), 3f);
        var cast = playback.Begin(Droid, AuthoredAnimation.Fishing8, 8f);

        playback.StopIfCurrent(Droid, craft).Should().BeFalse();
        runtime.Callbacks[0]();

        playback.IsCurrent(Droid, cast).Should().BeTrue();
        runtime.Replacements[AstromechWorkPlayback.LoopSource].Should().Be("cfishlp");
        releases.Should().Be(0);
    }

    [Test]
    public void EachTokenIsStoppedByItsOwner_ANativePreviewOrAWorkMotion()
    {
        // The two owners keep their tokens in separate variables on the creature.
        var workRuntime = new Runtime();
        var namedRuntime = new Runtime();
        int workReleases = 0, namedReleases = 0;
        var work = new AstromechWorkPlayback(workRuntime, (_, _) => workReleases++);
        var named = new NamedAnimationPlayback(namedRuntime, (_, _) => namedReleases++);
        bool Stop(string token) => work.StopIfCurrent(Droid, token) || named.StopIfCurrent(Droid, token);

        // A native preview on an astromech: the work playback does not know the token, the named playback ends it.
        var preview = named.BeginNative(Droid);
        work.StopIfCurrent(Droid, preview).Should().BeFalse();
        workRuntime.Replacements.Should().BeEmpty();
        Stop(preview).Should().BeTrue();
        named.IsCurrent(Droid, preview).Should().BeFalse();
        (workReleases, namedReleases).Should().Be((0, 1));
        Stop(preview).Should().BeFalse();

        // An authored work clip on an astromech: only the work playback ends it.
        var cast = work.Begin(Droid, AuthoredAnimation.Fishing6, 8f);
        named.StopIfCurrent(Droid, cast).Should().BeFalse();
        Stop(cast).Should().BeTrue();
        work.IsCurrent(Droid, cast).Should().BeFalse();
        (workReleases, namedReleases).Should().Be((1, 1));
        Stop(cast).Should().BeFalse();
        Stop(null).Should().BeFalse();
        Stop("").Should().BeFalse();
    }

    [Test]
    public void AnotherAnimationTakingOver_PutsTheClipsBack_AndPlaysNothing()
    {
        var runtime = new Runtime();
        var releases = 0;
        var playback = new AstromechWorkPlayback(runtime, (_, _) => releases++);

        playback.ReleaseForNativePlayback(Droid);
        runtime.Replacements.Should().BeEmpty("nothing was held");

        var cast = playback.Begin(Droid, AuthoredAnimation.Fishing6, 8f);
        playback.ReleaseForNativePlayback(Droid);

        playback.IsCurrent(Droid, cast).Should().BeFalse();
        playback.StopIfCurrent(Droid, cast).Should().BeFalse();
        runtime.Replacements[AstromechWorkPlayback.EntrySource].Should().BeEmpty();
        runtime.Replacements[AstromechWorkPlayback.LoopSource].Should().BeEmpty();
        releases.Should().Be(0);
    }

    [Test]
    public void AnInvalidCreature_HoldsNoMotion()
    {
        var runtime = new Runtime { Valid = false };
        var playback = new AstromechWorkPlayback(runtime);

        Action begin = () => playback.Begin(Droid, AuthoredAnimation.Fishing6, 8f);
        begin.Should().Throw<ArgumentException>();
        runtime.Replacements.Should().BeEmpty();

        runtime.Valid = true;
        var cast = playback.Begin(Droid, AuthoredAnimation.Fishing6, 8f);
        runtime.Valid = false;
        playback.IsCurrent(Droid, cast).Should().BeFalse();
        playback.StopIfCurrent(Droid, cast).Should().BeFalse();
    }

    private sealed class Runtime : INamedAnimationRuntime
    {
        public string Token = "";
        public bool Valid = true;
        public Dictionary<string, string> Replacements { get; } = new();
        public List<Action> Callbacks { get; } = new();
        public List<float> Delays { get; } = new();
        public bool IsValid(uint creature) => Valid;
        public string GetToken(uint creature) => Token;
        public void SetToken(uint creature, string token) => Token = token;
        public void Replace(uint creature, string source, string replacement) => Replacements[source] = replacement;
        public ActionType CurrentAction(uint creature) => ActionType.Invalid;
        public void ClearActions(uint creature) { }
        public void Schedule(float seconds, Action callback)
        {
            Delays.Add(seconds);
            Callbacks.Add(callback);
        }
    }
}
