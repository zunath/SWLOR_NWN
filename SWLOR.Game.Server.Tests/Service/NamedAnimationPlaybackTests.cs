using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class NamedAnimationPlaybackTests
{
    [Test] public void CompletionAndInterruptionReleaseThePoseOnlyForTheOwnedPlayback()
    {
        var runtime = new Runtime(); var released = new List<uint>();
        var playback = new NamedAnimationPlayback(runtime, released.Add);
        var first = playback.Begin(1, new AnimationClip("sw_wave", 2), 2);
        var second = playback.Begin(1, new AnimationClip("sw_point", 3), 3);
        playback.Complete(1, first); runtime.Callbacks[0]();
        released.Should().BeEmpty("an old timeout must not reset a newer animation's pose");
        playback.Stop(1);
        released.Should().Equal(1u);
        runtime.Callbacks[1](); playback.Complete(1, second);
        released.Should().HaveCount(1, "cleanup must not repeatedly restart idle");
        var third = playback.Begin(1, new AnimationClip("sw_wave", 2), 2);
        playback.Complete(1, third);
        released.Should().Equal(1u, 1u);
    }
    [Test] public void PlaybackUsesTheEngineCustomOneCarrierIncludingItsEntryAndExit()
    {
        ((int)Animation.PointForward).Should().Be(global::NWN.Core.NWScript.ANIMATION_LOOPING_CUSTOM1);
        var runtime = new Runtime(); var playback = new NamedAnimationPlayback(runtime);
        var token = playback.Begin(1, new AnimationClip("sw_wave", 2), 2);
        runtime.Replacements["custom1start"].Should().Be("sw_wave_in");
        runtime.Replacements["custom1lp"].Should().Be("sw_wave");
        runtime.Replacements["custom1end"].Should().Be("sw_wave_out");
        playback.Complete(1, token);
        runtime.Replacements.Values.Should().OnlyContain(value => value == "");
        runtime.Token.Should().BeEmpty();
    }
    [Test] public void AnOlderCompletionOrTimeoutCannotClearANewerPlayback()
    {
        var runtime = new Runtime(); var playback = new NamedAnimationPlayback(runtime);
        var first = playback.Begin(1, new AnimationClip("sw_wave", 2), 2);
        var second = playback.Begin(1, new AnimationClip("sw_point", 3), 3);
        playback.Complete(1, first); runtime.Callbacks[0]();
        runtime.Replacements["custom1lp"].Should().Be("sw_point");
        runtime.Token.Should().Be(second);
        runtime.Callbacks[1](); runtime.Token.Should().BeEmpty();
    }
    [Test] public void AnInterruptedQueueStillRestoresAllMappingsViaTimeout()
    {
        var runtime = new Runtime(); var playback = new NamedAnimationPlayback(runtime);
        playback.Begin(1, new AnimationClip("sw_wave", 2), 2);
        runtime.Callbacks[0]();
        runtime.Replacements.Values.Should().OnlyContain(value => value == "");
        runtime.Token.Should().BeEmpty();
    }
    [Test] public void ImmediatePlaybackRestoresAtTheRequestedDurationWithoutClearingANewerClip()
    {
        var runtime = new Runtime(); var playback = new NamedAnimationPlayback(runtime);
        playback.Begin(1, new AnimationClip("sw_wave", 2), .5f, completeAtDuration: true);
        runtime.Delays.Should().Equal(1.5f, .5f);
        runtime.Callbacks[1]();
        runtime.Replacements.Values.Should().OnlyContain(value => value == "");
        runtime.Token.Should().BeEmpty();
        playback.Begin(1, new AnimationClip("sw_point", 3), 3, completeAtDuration: true);
        runtime.Callbacks[0](); runtime.Callbacks[1]();
        runtime.Replacements["custom1lp"].Should().Be("sw_point");
    }
    [Test] public void CleanupIgnoresADestroyedOrReusedObject()
    {
        var runtime = new Runtime(); var playback = new NamedAnimationPlayback(runtime);
        playback.Begin(1, new AnimationClip("sw_wave", 2), 2);
        runtime.Token = "new object token";
        runtime.Callbacks[0](); runtime.Token.Should().Be("new object token");
        runtime.Valid = false; playback.Stop(1); runtime.Token.Should().Be("new object token");
    }
    [Test] public void SchedulingFailureRestoresMappingsImmediately()
    {
        var runtime = new Runtime { FailSchedule = true }; var playback = new NamedAnimationPlayback(runtime);
        Action act = () => playback.Begin(1, new AnimationClip("sw_wave", 2), 2);
        act.Should().Throw<InvalidOperationException>();
        runtime.Replacements.Values.Should().OnlyContain(value => value == ""); runtime.Token.Should().BeEmpty();
    }
    [Test] public void InvalidDurationDoesNotChangeCreatureState()
    {
        var runtime = new Runtime(); var playback = new NamedAnimationPlayback(runtime);
        Action act = () => playback.Begin(1, new AnimationClip("sw_wave", 2), float.NaN);
        act.Should().Throw<ArgumentOutOfRangeException>(); runtime.Token.Should().BeEmpty(); runtime.Replacements.Should().BeEmpty();
    }
    [TestCase(12, true)] [TestCase(13, false)]
    public void ClipNamesLeaveRoomForAllNativeReplacementPhases(int length, bool valid)
    {
        var name = new string('a', length);
        if (valid) new AnimationClip(name, 1).EndName.Length.Should().BeLessThanOrEqualTo(16);
        else { Action create = () => new AnimationClip(name, 1); create.Should().Throw<ArgumentException>(); }
    }
    private sealed class Runtime : INamedAnimationRuntime
    {
        public string Token = "";
        public bool Valid = true, FailSchedule;
        public Dictionary<string, string> Replacements { get; } = new();
        public List<Action> Callbacks { get; } = new();
        public List<float> Delays { get; } = new();
        public bool IsValid(uint creature) => Valid;
        public string GetToken(uint creature) => Token;
        public void SetToken(uint creature, string token) => Token = token;
        public void Replace(uint creature, string source, string replacement) => Replacements[source] = replacement;
        public void Schedule(float seconds, Action callback)
        {
            if (FailSchedule) throw new InvalidOperationException("Schedule failed");
            Delays.Add(seconds);
            Callbacks.Add(callback);
        }
    }
}
