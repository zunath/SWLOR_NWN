using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service;

/// <summary>Tracks a client-only hold clear and its subsequent native-animation restore per observer.</summary>
public sealed class RifleSupportHoldProjection
{
    private sealed class Request
    {
        public long Started;
        public string PhysicalIdle;
        public bool Cancelled;
        public bool Attacking;
        public readonly Dictionary<uint, long> Cleared = new();
        public readonly HashSet<uint> Restored = new();
        public readonly HashSet<uint> PendingRestore = new();
    }
    private readonly Dictionary<uint, Request> _requests = new();

    public void Begin(uint creature, string physicalIdle, long now)
    {
        var next = new Request { Started = now, PhysicalIdle = physicalIdle };
        if (_requests.TryGetValue(creature, out var previous))
            next.PendingRestore.UnionWith(previous.Cleared.Keys.Concat(previous.PendingRestore)
                .Where(observer => !previous.Restored.Contains(observer)));
        _requests[creature] = next;
    }

    // Forward native attack 9 immediately; observers not yet cleared can wait for its return.
    public void ObserveAnimation(uint creature, int animation)
    {
        if (_requests.TryGetValue(creature, out var request)) request.Attacking = animation == 9;
    }

    public RifleSupportHoldStage Current(uint creature, uint observer, long now)
    {
        if (!_requests.TryGetValue(creature, out var request) || request.Restored.Contains(observer)) return RifleSupportHoldStage.None;
        if (request.Attacking)
            return request.Cleared.ContainsKey(observer) || request.PendingRestore.Contains(observer)
                ? RifleSupportHoldStage.Restore : RifleSupportHoldStage.None;
        if (!request.Cleared.TryGetValue(observer, out var sent))
            return !request.Cancelled && now - request.Started < 10000 ? RifleSupportHoldStage.Clear :
                request.PendingRestore.Contains(observer) ? RifleSupportHoldStage.Restore : RifleSupportHoldStage.None;
        // Keep the clear through the mapping owner's network grace, and ensure a late
        // observer receives distinct clear/restore updates rather than one collapsed packet.
        return !request.Cancelled && (now - request.Started < 600 || now - sent < 200) ? RifleSupportHoldStage.Hold : RifleSupportHoldStage.Restore;
    }

    public string PhysicalIdle(uint creature) => _requests[creature].PhysicalIdle;
    public void Cancel(uint creature)
    {
        if (_requests.TryGetValue(creature, out var request)) request.Cancelled = true;
    }
    public void Sent(uint creature, uint observer, RifleSupportHoldStage stage, long now)
    {
        if (!_requests.TryGetValue(creature, out var request)) return;
        if (stage == RifleSupportHoldStage.Clear) request.Cleared.TryAdd(observer, now);
        if (stage == RifleSupportHoldStage.Restore) request.Restored.Add(observer);
    }
    public void Prune(Func<uint, bool> valid, long now)
    {
        foreach (var (creature, request) in _requests.ToArray())
            if (!valid(creature) || now - request.Started > 30000) _requests.Remove(creature);
    }
}
