using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Service;

/// <summary>Owns only melee swing keys, leaving ready, parry, throw, and custom emote mappings alone.</summary>
public sealed class QueuedAttackAnimationPlayback
{
    private readonly INamedAnimationRuntime runtime;
    private readonly Action<uint> restoreEquipmentAnimations;
    public QueuedAttackAnimationPlayback(INamedAnimationRuntime runtime, Action<uint> restoreEquipmentAnimations = null)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.restoreEquipmentAnimations = restoreEquipmentAnimations;
    }

    /// <summary>The swing keys this playback owns; built once per playback, so reads allocate nothing.</summary>
    public IReadOnlyList<string> SwingKeys { get; } = Array.AsReadOnly(
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
        restoreEquipmentAnimations?.Invoke(creature);
    }

    public void Stop(uint creature)
    {
        if (runtime.IsValid(creature)) Complete(creature, runtime.GetToken(creature));
    }
}
