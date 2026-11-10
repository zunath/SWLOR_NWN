using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Service;

/// <summary>Owns rifle mappings without clearing actions or replacing temporary ability mappings.</summary>
public sealed class RifleSupportAnimationPlayback
{
    private readonly IRifleSupportAnimationRuntime _runtime;

    public RifleSupportAnimationPlayback(IRifleSupportAnimationRuntime runtime) =>
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

    private readonly Dictionary<uint, RifleSupportPoses> _desired = new();
    private readonly Dictionary<uint, RifleSupportPoses> _applied = new();
    private readonly Dictionary<uint, (long Token, string PreviousIdle)> _clearing = new();
    private readonly Dictionary<uint, string> _uncleared = new();
    private long _token;
    private static readonly string[] Sources = { "xbowr", "xbowrdy", "xbowshot" };

    public void Apply(uint creature, RifleSupportPoses poses)
    {
        if (poses == null) _desired.Remove(creature);
        else _desired[creature] = poses;
        Refresh(creature);
    }

    public void Refresh(uint creature)
    {
        if (!_runtime.IsValid(creature)) { Forget(creature); return; }
        if (_runtime.IsTemporaryPlaybackActive(creature) || _clearing.ContainsKey(creature)) return;
        _desired.TryGetValue(creature, out var next);
        _applied.TryGetValue(creature, out var previous);
        var oldValues = Values(previous);
        var nextValues = Values(next);
        var current = Sources.Select(source => _runtime.Replacement(creature, source) ?? "").ToArray();
        // A different mapping owner keeps control until it releases its keys.
        if (current.Where((value, index) => value.Length > 0 && value != oldValues[index] &&
                value != nextValues[index]).Any()) return;
        for (var index = 0; index < Sources.Length; index++)
            if (current[index] != nextValues[index]) _runtime.Replace(creature, Sources[index], nextValues[index]);
        if (next == null)
        {
            _applied.Remove(creature);
            _uncleared.Remove(creature);
            return;
        }
        _applied[creature] = next;
        if (previous != next) _uncleared.TryAdd(creature, previous?.Idle ?? "xbowr");
        if (!_uncleared.TryGetValue(creature, out var physicalIdle) ||
            !string.IsNullOrEmpty(_runtime.Replacement(creature, NamedAnimationPlayback.HoldCarrierSource)) ||
            !string.IsNullOrEmpty(_runtime.Replacement(creature, NamedAnimationPlayback.ShoulderHoldSource))) return;
        var token = ++_token;
        _clearing[creature] = (token, physicalIdle);
        try
        {
            // Replacement lookup is single-pass. Borrow the shoulder key to clear the physical
            // old rifle layer while dodges resolves to the existing empty animation.
            _runtime.Replace(creature, NamedAnimationPlayback.ShoulderHoldSource, physicalIdle);
            _runtime.Replace(creature, NamedAnimationPlayback.HoldCarrierSource, NamedAnimationPlayback.NoHoldName);
            _runtime.ClearHold(creature);
            // Leave the carrier maps through several network updates so the client sees the clear.
            _runtime.Schedule(.6f, () => FinishClear(creature, token));
        }
        catch
        {
            RestoreCarriers(creature, physicalIdle);
            _clearing.Remove(creature);
            throw;
        }
    }

    private void FinishClear(uint creature, long token)
    {
        if (!_clearing.TryGetValue(creature, out var pending) || pending.Token != token) return;
        RestoreCarriers(creature, pending.PreviousIdle);
        _clearing.Remove(creature);
        _uncleared.Remove(creature);
        Refresh(creature);
    }

    private void RestoreCarriers(uint creature, string physicalIdle)
    {
        if (!_runtime.IsValid(creature) || _runtime.IsTemporaryPlaybackActive(creature)) return;
        if (_runtime.Replacement(creature, NamedAnimationPlayback.HoldCarrierSource) == NamedAnimationPlayback.NoHoldName)
            _runtime.Replace(creature, NamedAnimationPlayback.HoldCarrierSource, "");
        if (_runtime.Replacement(creature, NamedAnimationPlayback.ShoulderHoldSource) == physicalIdle)
            _runtime.Replace(creature, NamedAnimationPlayback.ShoulderHoldSource, "");
    }

    public void Prune()
    {
        foreach (var creature in _desired.Keys.Concat(_applied.Keys).Concat(_clearing.Keys).Distinct().ToArray())
            if (!_runtime.IsValid(creature)) Forget(creature);
    }

    private void Forget(uint creature)
    {
        _desired.Remove(creature);
        _applied.Remove(creature);
        _clearing.Remove(creature);
        _uncleared.Remove(creature);
    }

    private static string[] Values(RifleSupportPoses poses) =>
        poses == null ? new[] { "", "", "" } : new[] { poses.Idle, poses.Ready, poses.Shot };
}
