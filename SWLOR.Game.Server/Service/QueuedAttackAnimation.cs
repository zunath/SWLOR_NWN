using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>Changes the engine's swing while a weapon ability is readied; never queues an extra attack.</summary>
public static class QueuedAttackAnimation
{
    private const string TokenVariable = "_QUEUED_ATTACK_ANIMATION";
    private static readonly QueuedAttackAnimationPlayback Playback = new(new NativeRuntime());

    public static void Begin(uint creature, AnimationClip clip)
    {
        Playback.Stop(creature);
        if (clip != null) Playback.Begin(creature, clip);
    }

    public static void Stop(uint creature) => Playback.Stop(creature);

    private sealed class NativeRuntime : INamedAnimationRuntime
    {
        public bool IsValid(uint creature) => GetIsObjectValid(creature) && GetObjectType(creature) == ObjectType.Creature;
        public string GetToken(uint creature) => GetLocalString(creature, TokenVariable);
        public void SetToken(uint creature, string token)
        {
            if (string.IsNullOrEmpty(token)) DeleteLocalString(creature, TokenVariable);
            else SetLocalString(creature, TokenVariable, token);
        }
        public void Replace(uint creature, string source, string replacement) => ReplaceObjectAnimation(creature, source, replacement);
        public ActionType CurrentAction(uint creature) => GetCurrentAction(creature);
        public void ClearActions(uint creature) => ClearAllActions(oObject: creature);
        public void Schedule(float seconds, Action callback) => AssignCommand(GetModule(), () => DelayCommand(seconds, callback));
    }
}

/// <summary>Owns only melee swing keys, leaving ready, parry, throw, and custom emote mappings alone.</summary>
public sealed class QueuedAttackAnimationPlayback
{
    private readonly INamedAnimationRuntime runtime;
    public QueuedAttackAnimationPlayback(INamedAnimationRuntime runtime) => this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

    public static IReadOnlyList<string> SwingKeys { get; } = Array.AsReadOnly(
        new[] { "1h", "2h", "2w", "pl" }.SelectMany(prefix =>
            new[] { "slashl", "slashr", "slasho", "stab", "closeh", "closel", "reach" }.Select(suffix => prefix + suffix)).ToArray());

    public string Begin(uint creature, AnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (!runtime.IsValid(creature)) throw new ArgumentException("Animation target must be a creature.", nameof(creature));
        var token = Guid.NewGuid().ToString("N");
        runtime.SetToken(creature, token);
        try
        {
            foreach (var key in SwingKeys) runtime.Replace(creature, key, clip.Name);
            // The queue normally clears on a landed hit, cancellation, rest, equipment change or login.
            // The module owns this fallback so destroying or clearing the actor's actions cannot strand it.
            runtime.Schedule(30.1f, () => Complete(creature, token));
            return token;
        }
        catch { Complete(creature, token); throw; }
    }

    public void Complete(uint creature, string token)
    {
        if (string.IsNullOrEmpty(token) || !runtime.IsValid(creature) || runtime.GetToken(creature) != token) return;
        foreach (var key in SwingKeys) runtime.Replace(creature, key, "");
        runtime.SetToken(creature, "");
    }

    public void Stop(uint creature)
    {
        if (runtime.IsValid(creature)) Complete(creature, runtime.GetToken(creature));
    }
}
