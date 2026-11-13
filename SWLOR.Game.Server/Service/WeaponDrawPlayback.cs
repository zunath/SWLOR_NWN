using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service;

/// <summary>Suppresses cosmetic re-draws while leaving real equip and other mapping owners intact.</summary>
public sealed class WeaponDrawPlayback
{
    private readonly IWeaponDrawRuntime _runtime;

    public WeaponDrawPlayback(IWeaponDrawRuntime runtime) =>
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

    public const string EmptyDraw = "sw_nohold";
    private const string Right = "drwright";
    private const string Left = "drwleft";
    private sealed record Lease(long Token, string PreviousRight, string PreviousLeft);
    private readonly Dictionary<uint, Lease> _leases = new();
    private long _token;

    public void Begin(uint creature)
    {
        if (!_runtime.IsValid(creature)) { _leases.Remove(creature); return; }
        var right = _runtime.Replacement(creature, Right) ?? "";
        var left = _runtime.Replacement(creature, Left) ?? "";
        if (_leases.TryGetValue(creature, out var previous) && (right != EmptyDraw || left != EmptyDraw))
        {
            Release(creature);
            right = _runtime.Replacement(creature, Right) ?? "";
            left = _runtime.Replacement(creature, Left) ?? "";
            previous = null;
        }
        var first = previous == null;
        if (first)
        {
            if (right.Length > 0 || left.Length > 0) return;
            previous = new Lease(0, right, left);
        }
        var token = ++_token;
        _leases[creature] = previous with { Token = token };
        try
        {
            if (first)
            {
                _runtime.Replace(creature, Right, EmptyDraw);
                _runtime.Replace(creature, Left, EmptyDraw);
                // Appearance is read before the ordinary mapping update, so send the guard first.
                _runtime.SendMappings(creature);
            }
            _runtime.Schedule(.7f, () =>
            {
                if (_leases.TryGetValue(creature, out var current) && current.Token == token) Release(creature);
            });
        }
        catch
        {
            Release(creature);
            throw;
        }
    }

    public void Release(uint creature)
    {
        if (!_leases.Remove(creature, out var previous) || !_runtime.IsValid(creature)) return;
        var changed = false;
        foreach (var (source, value) in new[] { (Right, previous.PreviousRight), (Left, previous.PreviousLeft) })
        {
            if (_runtime.Replacement(creature, source) != EmptyDraw) continue;
            _runtime.Replace(creature, source, value);
            changed = true;
        }
        if (changed) _runtime.SendMappings(creature);
    }

    public void Prune()
    {
        foreach (var creature in _leases.Keys.Where(creature => !_runtime.IsValid(creature)).ToArray())
            _leases.Remove(creature);
    }
}
