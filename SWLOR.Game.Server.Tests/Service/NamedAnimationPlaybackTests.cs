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
    [TestCase(false)] [TestCase(true)]
    public void CancellationWithoutCurrentOwnershipPreservesUnrelatedQueuedWork(bool completed)
    {
        var runtime = new Runtime(); var playback = new NamedAnimationPlayback(runtime);
        if (completed)
        {
            var token = playback.Begin(1, new AnimationClip("sw_finished", 2), 2);
            playback.Complete(1, token);
            runtime.Callbacks[^1]();
        }
        var continued = false;
        runtime.Actions.Enqueue(() => continued = true);
        playback.Stop(1, cancelQueuedAnimation: true);
        runtime.RunActions();
        continued.Should().BeTrue();
        runtime.ClearedActions.Should().Be(0);
        runtime.Token.Should().BeEmpty();
    }

    [Test]
    public void RepeatedCancellationDuringExitDoesNotClearNewQueuedWork()
    {
        var runtime = new Runtime(); var playback = new NamedAnimationPlayback(runtime);
        playback.Begin(1, new AnimationClip("sw_finished", 2), 2);
        playback.Stop(1, cancelQueuedAnimation: true);
        var continued = false;
        runtime.Actions.Enqueue(() => continued = true);
        playback.Stop(1, cancelQueuedAnimation: true);
        runtime.RunActions();
        continued.Should().BeTrue();
        runtime.ClearedActions.Should().Be(1);
    }

    [TestCase(ActionType.MoveToPoint, true)]
    [TestCase(ActionType.AttackObject, true)]
    [TestCase(ActionType.Invalid, false)]
    public void StoppingPreservesMovementCombatAndQueuesWithoutAnExplicitCancellation(ActionType action, bool cancel)
    {
        var runtime = new Runtime { Action = action }; var playback = new NamedAnimationPlayback(runtime);
        var continued = false;
        runtime.Actions.Enqueue(() => continued = true);
        playback.Stop(1, cancel);
        runtime.RunActions();
        continued.Should().BeTrue();
        runtime.ClearedActions.Should().Be(0);
    }

    [Test] public void CancellationOfAnActiveClipClearsTheQueueAndStillReleasesItsOwnedPose()
    {
        var runtime = new Runtime(); var releases = new List<uint>();
        var playback = new NamedAnimationPlayback(runtime, (creature, _) => releases.Add(creature));
        playback.Begin(1, new AnimationClip("sw_active", 2), 2);
        runtime.Actions.Enqueue(() => throw new InvalidOperationException("Cancelled action executed."));
        playback.Stop(1, cancelQueuedAnimation: true);
        runtime.RunActions();
        runtime.ClearedActions.Should().Be(1);
        releases.Should().Equal(1u);
        runtime.Callbacks[^1]();
        runtime.Token.Should().BeEmpty();
        runtime.Replacements.Values.Should().OnlyContain(value => value == "");
    }

    [Test] public void PreviewStopDoesNotCancelANewerAbilityAndReleasesOnlyOnce()
    {
        var runtime = new Runtime(); var released = new List<uint>();
        var playback = new NamedAnimationPlayback(runtime, (creature, _) => released.Add(creature));
        var preview = playback.Begin(1, new AnimationClip("sw_preview", 2), 2);
        playback.IsCurrent(1, preview).Should().BeTrue();
        var ability = playback.Begin(1, new AnimationClip("sw_ability", 3), 3);
        playback.IsCurrent(1, preview).Should().BeFalse("a deferred preview callback must skip a superseded animation");
        playback.StopIfCurrent(1, preview).Should().BeFalse();
        runtime.Token.Should().Be(ability);
        released.Should().BeEmpty();
        playback.StopIfCurrent(1, ability).Should().BeTrue();
        playback.StopIfCurrent(1, ability).Should().BeFalse();
        playback.IsCurrent(1, ability).Should().BeFalse("closing before the deferred play must prevent it from starting");
        released.Should().Equal(1u);
    }
    [Test] public void CompletionAndInterruptionReleaseThePoseOnlyForTheOwnedPlayback()
    {
        var runtime = new Runtime(); var released = new List<uint>();
        var playback = new NamedAnimationPlayback(runtime, (creature, _) => released.Add(creature));
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
        runtime.Replacements["custom1end"].Should().Be("sw_wave_out", "the authored exit must remain mapped while leaving the emote");
        runtime.Delays[^1].Should().Be(NamedAnimationPlayback.ExitGraceSeconds);
        runtime.Callbacks[^1]();
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
        runtime.Callbacks[1](); runtime.Callbacks[^1](); runtime.Token.Should().BeEmpty();
    }
    [Test] public void AnInterruptedQueueStillRestoresAllMappingsViaTimeout()
    {
        var runtime = new Runtime(); var playback = new NamedAnimationPlayback(runtime);
        playback.Begin(1, new AnimationClip("sw_wave", 2), 2);
        runtime.Callbacks[0]();
        runtime.Callbacks[^1]();
        runtime.Replacements.Values.Should().OnlyContain(value => value == "");
        runtime.Token.Should().BeEmpty();
    }
    [Test] public void ImmediatePlaybackRestoresAtTheRequestedDurationWithoutClearingANewerClip()
    {
        var runtime = new Runtime(); var playback = new NamedAnimationPlayback(runtime);
        playback.Begin(1, new AnimationClip("sw_wave", 2), .5f, completeAtDuration: true);
        runtime.Delays.Should().Equal(1.5f, .5f);
        runtime.Callbacks[1]();
        runtime.Callbacks[^1]();
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
    [Test] public void DeferredExitKeepsAuthoredPhasesAndCannotResetANewerAnimation()
    {
        var runtime = new Runtime(); var queued = new List<Action>(); var exits = new List<string>();
        var playback = new NamedAnimationPlayback(runtime, (_, stillOwnsExit) => queued.Add(() =>
        {
            if (stillOwnsExit()) exits.Add(runtime.Replacements[NamedAnimationPlayback.EndSource]);
        }));
        var first = playback.Begin(1, new AnimationClip("sw_first", 1), 1);
        playback.Complete(1, first);
        var oldRestore = runtime.Callbacks[^1];
        playback.IsCurrent(1, first).Should().BeFalse();
        runtime.Replacements[NamedAnimationPlayback.LoopSource].Should().Be("sw_first");
        var second = playback.Begin(1, new AnimationClip("sw_second", 1), 1);
        queued[0](); oldRestore();
        exits.Should().BeEmpty("an old deferred idle command must not interrupt the new clip");
        runtime.Replacements[NamedAnimationPlayback.EndSource].Should().Be("sw_second_out");
        playback.StopIfCurrent(1, second).Should().BeTrue();
        playback.StopIfCurrent(1, second).Should().BeFalse("ending playback must release only once");
        queued[1]();
        exits.Should().Equal(new[] { "sw_second_out" }, "leaving the emote must use the authored exit, not the pointing gesture");
        runtime.Callbacks[^1]();
        queued[1]();
        exits.Should().HaveCount(1);
        runtime.Token.Should().BeEmpty();
        runtime.Replacements.Values.Should().OnlyContain(value => value == "");
    }
    [TestCase(false)] [TestCase(true)]
    public void DeathClearsActiveOrEndingMappingsWithoutARecoveryAfterRevival(bool alreadyEnding)
    {
        var runtime = new Runtime(); var exits = new List<Func<bool>>();
        var playback = new NamedAnimationPlayback(runtime, (_, ownsExit) => exits.Add(ownsExit));
        var token = playback.Begin(1, new AnimationClip("sw_wave", 2), 2);
        if (alreadyEnding) playback.Complete(1, token);
        playback.ClearOnDeath(1);
        runtime.Replacements.Values.Should().OnlyContain(value => value == "");
        runtime.Token.Should().BeEmpty();
        exits.Should().OnlyContain(ownsExit => !ownsExit());
        playback.Begin(1, new AnimationClip("sw_revived", 1), 1);
        runtime.Callbacks[0]();
        if (alreadyEnding) runtime.Callbacks[1]();
        runtime.Replacements[NamedAnimationPlayback.LoopSource].Should().Be("sw_revived");
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
        public ActionType Action = ActionType.Invalid;
        public int ClearedActions;
        public Queue<Action> Actions { get; } = new();
        public Dictionary<string, string> Replacements { get; } = new();
        public List<Action> Callbacks { get; } = new();
        public List<float> Delays { get; } = new();
        public bool IsValid(uint creature) => Valid;
        public string GetToken(uint creature) => Token;
        public void SetToken(uint creature, string token) => Token = token;
        public void Replace(uint creature, string source, string replacement) => Replacements[source] = replacement;
        public ActionType CurrentAction(uint creature) => Action;
        public void ClearActions(uint creature) { ClearedActions++; Actions.Clear(); }
        public void RunActions() { while (Actions.TryDequeue(out var action)) action(); }
        public void Schedule(float seconds, Action callback)
        {
            if (FailSchedule) throw new InvalidOperationException("Schedule failed");
            Delays.Add(seconds);
            Callbacks.Add(callback);
        }
    }
}
