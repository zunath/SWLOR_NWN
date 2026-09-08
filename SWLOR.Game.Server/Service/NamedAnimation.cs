using System;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>Plays installed named animations through a temporary, creature-local custom emote mapping.</summary>
public static class NamedAnimation
{
    private const string PlaybackTokenVariable = "_NAMED_ANIMATION_PLAYBACK";
    private static readonly NamedAnimationPlayback Playback = new(new NativeRuntime(), ReleaseIdlePose);

    /// <summary>
    /// Queues a clip without clearing existing actions. The original Point Forward emote mapping
    /// is restored at completion or by a module-owned timeout if the queue is interrupted.
    /// </summary>
    public static void Queue(uint creature, AnimationClip clip, float? durationSeconds = null)
    {
        var duration = Validate(clip, durationSeconds);
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
            throw new ArgumentException("Animation target must be a valid creature.", nameof(creature));
        string token = null;
        AssignCommand(creature, () =>
        {
            ActionDoCommand(() => token = Playback.Begin(creature, clip, duration));
            ActionPlayAnimation(Animation.PointForward, 1f, duration);
            ActionDoCommand(() => Playback.Complete(creature, token));
        });
    }

    /// <summary>Plays immediately using NWN's PlayAnimation semantics. Prefer Queue when existing actions must finish.</summary>
    public static string Play(uint creature, AnimationClip clip, float? durationSeconds = null)
    {
        var duration = Validate(clip, durationSeconds);
        // AssignCommand is a deferred closure in SWLOR. Reserve ownership now so
        // the caller can stop it even before that closure runs (for example on close).
        var token = Playback.Begin(creature, clip, duration, completeAtDuration: true);
        try
        {
            AssignCommand(creature, () =>
            {
                if (!Playback.IsCurrent(creature, token)) return;
                try { PlayAnimation(Animation.PointForward, 1f, duration); }
                catch { Playback.Complete(creature, token); throw; }
            });
        }
        catch { Playback.Complete(creature, token); throw; }
        return token;
    }

    /// <summary>Stops only this caller's playback, preserving a newer animation started by an ability.</summary>
    public static bool StopIfCurrent(uint creature, string token) => Playback.StopIfCurrent(creature, token);

    public static void ClearOnDeath(uint creature) => Playback.ClearOnDeath(creature);

    public static void ReleaseForNativePlayback(uint creature) => Playback.ReleaseForNativePlayback(creature);

    /// <summary>
    /// Releases a started authored pose; optionally cancels its current scripted animation action.
    /// Clips still waiting for their queued begin have not claimed playback ownership.
    /// </summary>
    public static void Stop(uint creature, bool cancelQueuedAnimation = false) =>
        Playback.Stop(creature, cancelQueuedAnimation);

    private static void ReleaseIdlePose(uint creature, Func<bool> stillOwnsExit)
    {
        // Leave the emote while its authored exit remains mapped. Re-check ownership and
        // activity inside AssignCommand: another ability or movement may start before it runs.
        AssignCommand(creature, () =>
        {
            if (!stillOwnsExit() || GetCurrentHitPoints(creature) <= 0 || GetIsInCombat(creature) ||
                GetCurrentAction(creature) != ActionType.Invalid) return;
            PlayAnimation(Animation.LoopingPause, 1f, .1f);
        });
    }

    private static float Validate(AnimationClip clip, float? duration)
    {
        ArgumentNullException.ThrowIfNull(clip);
        var value = duration ?? clip.Duration;
        if (!float.IsFinite(value) || value <= 0 || value > 600) throw new ArgumentOutOfRangeException(nameof(duration));
        return value;
    }

    private sealed class NativeRuntime : INamedAnimationRuntime
    {
        public bool IsValid(uint creature) => GetIsObjectValid(creature) && GetObjectType(creature) == ObjectType.Creature;
        public string GetToken(uint creature) => GetLocalString(creature, PlaybackTokenVariable);
        public void SetToken(uint creature, string token)
        {
            if (string.IsNullOrEmpty(token)) DeleteLocalString(creature, PlaybackTokenVariable);
            else SetLocalString(creature, PlaybackTokenVariable, token);
        }
        public void Replace(uint creature, string source, string replacement) => ReplaceObjectAnimation(creature, source, replacement);
        public ActionType CurrentAction(uint creature) => GetCurrentAction(creature);
        public void ClearActions(uint creature) => ClearAllActions(oObject: creature);
        public void Schedule(float seconds, Action callback) => AssignCommand(GetModule(), () => DelayCommand(seconds, callback));
    }
}
