using System;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Service;

/// <summary>The native operations required for a temporary per-creature animation replacement.</summary>
public interface INamedAnimationRuntime
{
    bool IsValid(uint creature);
    string GetToken(uint creature);
    void SetToken(uint creature, string token);
    void Replace(uint creature, string source, string replacement);
    void Schedule(float seconds, Action callback);
}

/// <summary>Owns temporary replacements; callbacks from superseded playbacks cannot clear a newer one.</summary>
public sealed class NamedAnimationPlayback
{
    public const string StartSource = "custom1start";
    public const string LoopSource = "custom1lp";
    public const string EndSource = "custom1end";
    private readonly INamedAnimationRuntime _runtime;

    public NamedAnimationPlayback(INamedAnimationRuntime runtime) => _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

    public string Begin(uint creature, AnimationClip clip, float duration)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (!float.IsFinite(duration) || duration <= 0 || duration > 600) throw new ArgumentOutOfRangeException(nameof(duration));
        if (!_runtime.IsValid(creature)) throw new ArgumentException("Animation target must be a valid creature.", nameof(creature));
        var token = Guid.NewGuid().ToString("N");
        _runtime.SetToken(creature, token);
        try
        {
            _runtime.Replace(creature, StartSource, clip.StartName);
            _runtime.Replace(creature, LoopSource, clip.Name);
            _runtime.Replace(creature, EndSource, clip.EndName);
            // Native queue cleanup normally runs first. A module-owned timeout also restores the
            // map when combat, movement, death, or another script clears the queued cleanup action.
            _runtime.Schedule(duration + 1f, () => Complete(creature, token));
            return token;
        }
        catch
        {
            Complete(creature, token);
            throw;
        }
    }

    public void Complete(uint creature, string token)
    {
        if (string.IsNullOrEmpty(token) || !_runtime.IsValid(creature) || _runtime.GetToken(creature) != token) return;
        _runtime.Replace(creature, StartSource, "");
        _runtime.Replace(creature, LoopSource, "");
        _runtime.Replace(creature, EndSource, "");
        _runtime.SetToken(creature, "");
    }

    public void Stop(uint creature)
    {
        if (_runtime.IsValid(creature)) Complete(creature, _runtime.GetToken(creature));
    }
}
