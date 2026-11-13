using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service;

/// <summary>Keeps client mesh identities separate while preserving the server item's identity.</summary>
public sealed class WeaponAppearanceIdentityCache
{
    private const uint InvalidObject = 0x7f000000;
    private readonly Dictionary<uint, Dictionary<WeaponAppearanceKey, uint>> _appearances = new();
    private readonly ConcurrentDictionary<uint, uint> _outbound = new();
    private readonly ConcurrentDictionary<uint, uint> _inbound = new();

    public uint[] Items => _appearances.Keys.ToArray();

    public uint Select(uint item, WeaponAppearanceKey appearance, Func<uint> reserve, out bool changed)
    {
        if (item == InvalidObject) throw new ArgumentException("A real item is required.", nameof(item));
        _appearances.TryGetValue(item, out var identities);
        if (identities == null || !identities.TryGetValue(appearance, out var identity))
        {
            identity = reserve();
            if (identity == InvalidObject || identity == item || _inbound.ContainsKey(identity))
                throw new InvalidOperationException("A fresh reserved item identity is required.");
            identities ??= new Dictionary<WeaponAppearanceKey, uint>();
            identities.Add(appearance, identity);
            _appearances[item] = identities;
            _inbound[identity] = item;
        }
        changed = Write(item) != identity;
        _outbound[item] = identity;
        return identity;
    }

    public uint Write(uint item) => _outbound.GetValueOrDefault(item, item);
    public uint Read(uint identity) => _inbound.GetValueOrDefault(identity, identity);
    public uint[] Identities(uint item) => _appearances.TryGetValue(item, out var identities)
        ? identities.Values.ToArray() : Array.Empty<uint>();

    public void Forget(uint item)
    {
        if (!_appearances.Remove(item, out var identities)) return;
        _outbound.TryRemove(item, out _);
        foreach (var identity in identities.Values) _inbound.TryRemove(identity, out _);
    }
}
