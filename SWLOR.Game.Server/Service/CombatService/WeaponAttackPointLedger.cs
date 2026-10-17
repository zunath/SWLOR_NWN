using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CombatService;

/// <summary>Bounds native hit callbacks already credited using their launch-time weapon identity.</summary>
public sealed class WeaponAttackPointLedger
{
    private sealed record Entry(uint Actor, uint Target, uint Weapon, uint Ammunition, long RollId, DateTime Expires);
    private readonly List<Entry> _pending = new();
    private readonly Dictionary<(uint Actor, long RollId), DateTime> _creditedRolls = new();
    private const int MaximumEntriesPerActor = 128;
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(10);

    public bool Capture(uint actor, uint target, uint weapon, uint ammunition, long rollId, DateTime now)
    {
        Prune(now);
        if (rollId != 0 && !_creditedRolls.TryAdd((actor, rollId), now + Lifetime)) return false;
        while (_creditedRolls.Count(pair => pair.Key.Actor == actor) > MaximumEntriesPerActor)
            _creditedRolls.Remove(_creditedRolls.First(pair => pair.Key.Actor == actor).Key);
        while (_pending.Count(entry => entry.Actor == actor) >= MaximumEntriesPerActor)
            _pending.RemoveAt(_pending.FindIndex(entry => entry.Actor == actor));
        _pending.Add(new Entry(actor, target, weapon, ammunition, rollId, now + Lifetime));
        return true;
    }

    public bool Consume(uint actor, uint target, uint callbackItem, DateTime now)
    {
        Prune(now);
        var index = _pending.FindIndex(entry => entry.Actor == actor && entry.Target == target &&
            (entry.Weapon == callbackItem || entry.Ammunition == callbackItem));
        if (index < 0) return false;
        _pending.RemoveAt(index);
        return true;
    }

    public void Clear(uint actor)
    {
        _pending.RemoveAll(entry => entry.Actor == actor);
        foreach (var key in _creditedRolls.Keys.Where(key => key.Actor == actor).ToArray())
            _creditedRolls.Remove(key);
    }

    private void Prune(DateTime now)
    {
        _pending.RemoveAll(entry => entry.Expires <= now);
        foreach (var key in _creditedRolls.Where(pair => pair.Value <= now).Select(pair => pair.Key).ToArray())
            _creditedRolls.Remove(key);
    }
}
