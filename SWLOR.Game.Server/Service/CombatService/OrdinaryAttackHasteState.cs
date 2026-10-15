using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatService;

/// <summary>Same-target ordinary-hit Haste independent of weapon family and equipment changes.</summary>
public sealed class OrdinaryAttackHasteState
{
    private readonly HashSet<long> _hits = new();
    private readonly Queue<long> _hitOrder = new();
    private uint _target;
    private DateTime _grantReady;
    public int Stacks { get; private set; }
    public DateTime Expires { get; private set; }

    public void Reconcile(DateTime now, OrdinaryAttackHasteConfiguration configuration)
    {
        if (now >= Expires || configuration.MaximumStacks <= 0 || configuration.HastePerStack <= 0)
            Stacks = 0;
        else Stacks = Math.Min(Stacks, configuration.MaximumStacks);
    }

    public void RecordTargetChange(uint target)
    {
        if (_target == target) return;
        _target = target;
        Clear();
    }

    public bool RecordHit(DateTime now, OrdinaryAttackHasteConfiguration configuration, uint target, long rollId)
    {
        Reconcile(now, configuration);
        if (configuration.MaximumStacks <= 0 || configuration.HastePerStack <= 0 || configuration.DurationSeconds <= 0 ||
            !_hits.Add(rollId)) return false;
        RecordTargetChange(target);
        _hitOrder.Enqueue(rollId);
        if (_hitOrder.Count > 256) _hits.Remove(_hitOrder.Dequeue());
        Expires = now.AddSeconds(configuration.DurationSeconds);
        if (now < _grantReady) return true;
        Stacks = Math.Min(Stacks + 1, configuration.MaximumStacks);
        _grantReady = now.AddSeconds(configuration.GrantCooldownSeconds);
        return true;
    }

    public void Clear()
    {
        Stacks = 0;
        Expires = default;
        // Equipment, combat and stance transitions never reset the grant cooldown.
    }

    public void Reset()
    {
        Clear();
        _target = default;
        _grantReady = default;
        _hits.Clear();
        _hitOrder.Clear();
    }
}
