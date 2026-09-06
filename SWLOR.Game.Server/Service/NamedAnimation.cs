using System;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>Plays installed named animations through a temporary, creature-local custom emote mapping.</summary>
public static class NamedAnimation
{
    private const string PlaybackTokenVariable = "_NAMED_ANIMATION_PLAYBACK";
    private static readonly NamedAnimationPlayback Playback = new(new NativeRuntime());

    /// <summary>
    /// Queues a clip without clearing existing actions. The original Point Forward emote mapping
    /// is restored at completion or by a module-owned timeout if the queue is interrupted.
    /// </summary>
    public static void Queue(uint creature, AnimationClip clip, float? durationSeconds = null)
    {
        var duration = Validate(clip, durationSeconds);
        string token = null;
        AssignCommand(creature, () =>
        {
            ActionDoCommand(() => token = Playback.Begin(creature, clip, duration));
            ActionPlayAnimation(Animation.PointForward, 1f, duration);
            ActionDoCommand(() => Playback.Complete(creature, token));
        });
    }

    /// <summary>Plays immediately using NWN's PlayAnimation semantics. Prefer Queue when existing actions must finish.</summary>
    public static void Play(uint creature, AnimationClip clip, float? durationSeconds = null)
    {
        var duration = Validate(clip, durationSeconds);
        AssignCommand(creature, () =>
        {
            var token = Playback.Begin(creature, clip, duration, completeAtDuration: true);
            try { PlayAnimation(Animation.PointForward, 1f, duration); }
            catch { Playback.Complete(creature, token); throw; }
        });
    }

    /// <summary>Restores this helper's emote mapping; does not clear unrelated creature actions.</summary>
    public static void Stop(uint creature) => Playback.Stop(creature);

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
        public void Schedule(float seconds, Action callback) => AssignCommand(GetModule(), () => DelayCommand(seconds, callback));
    }
}
