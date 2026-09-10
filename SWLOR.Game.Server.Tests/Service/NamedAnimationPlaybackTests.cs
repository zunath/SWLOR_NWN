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
    [Test]
    public void RifleCarrierClearsTheExistingHoldAndSuppressesFutureHoldRefreshes()
    {
        ((int)Animation.FireForgetDodgeSide).Should().Be(global::NWN.Core.NWScript.ANIMATION_FIREFORGET_DODGE_SIDE);
        var runtime = new Runtime();
        var playback = new NamedAnimationPlayback(runtime);
        var token = playback.Begin(1, new AnimationClip("sw_aimedshot", 1.8f), 1.8f, suppressRifleHold: true);

        runtime.Replacements["dodges"].Should().Be("sw_aimedshot");
        runtime.Replacements["xbowr"].Should().Be(NamedAnimationPlayback.NoHoldName);
        runtime.Replacements["plpause1"].Should().Be("xbowr",
            "the second single-pass removal must target the existing physical hold, not the new no-op mapping");
        playback.Complete(1, token);
        runtime.Callbacks[^1]();
        runtime.Replacements.Values.Should().OnlyContain(value => value == "");
        runtime.Token.Should().BeEmpty();
    }

    [Test]
    public void ANewRifleClipKeepsItsMappingsWhenAnOlderExitAndTimeoutRun()
    {
        var runtime = new Runtime();
        var playback = new NamedAnimationPlayback(runtime);
        var first = playback.Begin(1, new AnimationClip("sw_aimedshot", 1.8f), 1.8f, suppressRifleHold: true);
        playback.Complete(1, first);
        var oldCallbacks = runtime.Callbacks.ToArray();
        var second = playback.Begin(1, new AnimationClip("sw_headshot", 2), 2, suppressRifleHold: true);

        foreach (var callback in oldCallbacks) callback();
        playback.StopIfCurrent(1, first).Should().BeFalse();
        playback.IsCurrent(1, second).Should().BeTrue();
        runtime.Replacements[NamedAnimationPlayback.RifleCarrierSource].Should().Be("sw_headshot");
        runtime.Replacements[NamedAnimationPlayback.RifleCarrySource].Should().Be(NamedAnimationPlayback.NoHoldName);
        runtime.Replacements[NamedAnimationPlayback.RifleCarryResetSource].Should().Be("xbowr");
    }

    [Test]
    public void SwitchingFromRifleToRegularPlaybackRemovesEveryRifleMapping()
    {
        var runtime = new Runtime();
        var playback = new NamedAnimationPlayback(runtime);
        playback.Begin(1, new AnimationClip("sw_aimedshot", 1.8f), 1.8f, suppressRifleHold: true);
        var oldCallbacks = runtime.Callbacks.ToArray();
        var regular = playback.Begin(1, new AnimationClip("sw_wave", 2), 2);

        runtime.Replacements[NamedAnimationPlayback.RifleCarrierSource].Should().BeEmpty();
        runtime.Replacements[NamedAnimationPlayback.RifleCarrySource].Should().BeEmpty();
        runtime.Replacements[NamedAnimationPlayback.RifleCarryResetSource].Should().BeEmpty();
        foreach (var callback in oldCallbacks) callback();
        playback.IsCurrent(1, regular).Should().BeTrue();
        runtime.Replacements[NamedAnimationPlayback.LoopSource].Should().Be("sw_wave");
    }

    [TestCase(ActionType.MoveToPoint)]
    [TestCase(ActionType.AttackObject)]
    public void StoppingARifleClipRestoresMappingsWithoutClearingMovementOrCombat(ActionType action)
    {
        var runtime = new Runtime { Action = action };
        var playback = new NamedAnimationPlayback(runtime);
        playback.Begin(1, new AnimationClip("sw_aimedshot", 1.8f), 1.8f, suppressRifleHold: true);
        var continued = false;
        runtime.Actions.Enqueue(() => continued = true);

        playback.Stop(1, cancelQueuedAnimation: true);
        runtime.Callbacks[^1]();
        runtime.RunActions();
        continued.Should().BeTrue();
        runtime.ClearedActions.Should().Be(0);
        runtime.Replacements.Values.Should().OnlyContain(value => value == "");
    }

    [Test]
    public void NativePreviewUsesNoAuthoredMappingsAndOldTimeoutCannotStopItsReplacement()
    {
        var runtime = new Runtime();
        var releases = 0;
        var playback = new NamedAnimationPlayback(runtime, (_, _) => releases++);
        playback.Begin(1, new AnimationClip("sw_old", 2), 2);
        var native = playback.BeginNative(1);
        runtime.Replacements.Values.Should().OnlyContain(value => value == "");
        playback.IsCurrent(1, native).Should().BeTrue();
        var callbacks = runtime.Callbacks.ToArray();
        var authored = playback.Begin(1, new AnimationClip("sw_new", 2), 2);
        foreach (var callback in callbacks) callback();
        playback.StopIfCurrent(1, native).Should().BeFalse();
        playback.IsCurrent(1, authored).Should().BeTrue();
        runtime.Replacements[NamedAnimationPlayback.LoopSource].Should().Be("sw_new");
        releases.Should().Be(0);
    }

    [Test]
    public void NativePreviewRepeatStopAndTimeoutPreserveNewerOwnershipAndNaturalNativeExit()
    {
        var runtime = new Runtime();
        var releases = 0;
        var playback = new NamedAnimationPlayback(runtime, (_, _) => releases++);
        var first = playback.BeginNative(1);
        var second = playback.BeginNative(1);
        playback.StopIfCurrent(1, first).Should().BeFalse();
        runtime.Callbacks[0]();
        playback.IsCurrent(1, second).Should().BeTrue();
        playback.StopIfCurrent(1, second).Should().BeTrue();
        releases.Should().Be(1);
        var third = playback.BeginNative(1);
        foreach (var callback in runtime.Callbacks.ToArray()) callback();
        playback.IsCurrent(1, third).Should().BeFalse();
        runtime.Token.Should().BeEmpty();
        releases.Should().Be(1, "native timeout must not force idle at a guessed model-specific duration");
        runtime.ClearedActions.Should().Be(0);
    }

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
    [TestCase(false)] [TestCase(true)]
    public void AnInterruptedQueueStillRestoresAllMappingsViaTimeout(bool rifle)
    {
        var runtime = new Runtime(); var playback = new NamedAnimationPlayback(runtime);
        playback.Begin(1, new AnimationClip("sw_wave", 2), 2, suppressRifleHold: rifle);
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
    [TestCase(false, false)] [TestCase(true, false)]
    [TestCase(false, true)] [TestCase(true, true)]
    public void NativeHandoffInvalidatesPreviewTimersAndDeferredRecoveryWithoutClearingActions(bool alreadyEnding, bool rifle)
    {
        var runtime = new Runtime();
        var exits = new List<Func<bool>>();
        var playback = new NamedAnimationPlayback(runtime, (_, ownsExit) => exits.Add(ownsExit));
        var token = playback.Begin(1, new AnimationClip("sw_preview", 2), 2, completeAtDuration: true, suppressRifleHold: rifle);
        if (alreadyEnding) playback.Complete(1, token);
        var exitsBeforeHandoff = exits.Count;
        runtime.Actions.Enqueue(() => { });
        playback.ReleaseForNativePlayback(1);
        foreach (var callback in runtime.Callbacks.ToArray()) callback();
        runtime.Token.Should().BeEmpty();
        runtime.Replacements.Values.Should().OnlyContain(value => value == "");
        exits.Should().HaveCount(exitsBeforeHandoff, "native handoff must not issue a recovery which interrupts the new native gesture");
        exits.Should().OnlyContain(ownsExit => !ownsExit());
        runtime.ClearedActions.Should().Be(0);
        runtime.Actions.Should().HaveCount(1);
    }

    [TestCase(false, false)] [TestCase(true, false)]
    [TestCase(false, true)] [TestCase(true, true)]
    public void DeathClearsActiveOrEndingMappingsWithoutARecoveryAfterRevival(bool alreadyEnding, bool rifle)
    {
        var runtime = new Runtime(); var exits = new List<Func<bool>>();
        var playback = new NamedAnimationPlayback(runtime, (_, ownsExit) => exits.Add(ownsExit));
        var token = playback.Begin(1, new AnimationClip("sw_wave", 2), 2, suppressRifleHold: rifle);
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
    [TestCase(false)] [TestCase(true)]
    public void SchedulingFailureRestoresMappingsImmediately(bool rifle)
    {
        var runtime = new Runtime { FailSchedule = true }; var playback = new NamedAnimationPlayback(runtime);
        Action act = () => playback.Begin(1, new AnimationClip("sw_wave", 2), 2, suppressRifleHold: rifle);
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
