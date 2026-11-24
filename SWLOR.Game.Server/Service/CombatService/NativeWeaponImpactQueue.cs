using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>
    /// Holds work that must wait until the engine applies a native weapon roll's damage.
    /// GetDamageRoll resolves a roll when the swing or shot starts, but the engine applies its
    /// damage later, in SignalMeleeDamage or SignalRangedDamage, for the attack slots that signal names.
    /// </summary>
    public static class NativeWeaponImpactQueue
    {
        /// <summary>A signal that never arrives (an interrupted round) must not fire a later roll's work.</summary>
        public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(6);

        // Destroyed attackers never take their work, so sweep expired work once this many attackers wait.
        private const int SweepThreshold = 128;

        private sealed record PendingImpact(long Roll, int AttackIndex, uint Target, DateTime ExpiresAt, Action Action);

        private static readonly Dictionary<uint, List<PendingImpact>> _pending = new();
        private static long _nextRoll;

        public static bool IsEmpty => _pending.Count == 0;

        /// <summary>Returns a scheduler that queues work for one native roll's damage application.</summary>
        public static Action<Action> CreateScheduler(uint attacker, int attackIndex, uint target)
        {
            var roll = ++_nextRoll;
            return action => Add(attacker, roll, attackIndex, target, action, DateTime.UtcNow);
        }

        public static void Add(uint attacker, long roll, int attackIndex, uint target, Action action, DateTime now)
        {
            if (action == null)
                return;
            if (_pending.Count >= SweepThreshold)
                Sweep(now);

            if (!_pending.TryGetValue(attacker, out var impacts))
                _pending[attacker] = impacts = new List<PendingImpact>();

            // A new roll in a slot means the slot's earlier roll was never signalled.
            impacts.RemoveAll(impact => impact.ExpiresAt <= now || impact.AttackIndex == attackIndex && impact.Roll != roll);
            impacts.Add(new PendingImpact(roll, attackIndex, target, now + Lifetime, action));
        }

        /// <summary>
        /// Removes and returns, in queue order, the work for the attack slots a damage signal applies:
        /// <paramref name="attacks"/> slots starting at <paramref name="firstAttack"/>, wrapping like the
        /// engine's byte attack cursor. Work for those slots queued against another target is dropped.
        /// </summary>
        public static IReadOnlyList<Action> Take(uint attacker, uint target, int firstAttack, int attacks, DateTime now)
        {
            if (attacks <= 0 || !_pending.TryGetValue(attacker, out var impacts))
                return Array.Empty<Action>();

            List<Action> due = null;
            var kept = new List<PendingImpact>(impacts.Count);
            foreach (var impact in impacts)
            {
                if (impact.ExpiresAt <= now)
                    continue;
                if (((impact.AttackIndex - firstAttack) & 0xFF) >= attacks)
                {
                    kept.Add(impact);
                    continue;
                }
                if (impact.Target == target)
                    (due ??= new List<Action>()).Add(impact.Action);
            }

            if (kept.Count == 0)
                _pending.Remove(attacker);
            else
                _pending[attacker] = kept;
            return due ?? (IReadOnlyList<Action>)Array.Empty<Action>();
        }

        public static void Clear()
        {
            _pending.Clear();
        }

        private static void Sweep(DateTime now)
        {
            foreach (var attacker in _pending.Keys.ToList())
            {
                var impacts = _pending[attacker];
                impacts.RemoveAll(impact => impact.ExpiresAt <= now);
                if (impacts.Count == 0)
                    _pending.Remove(attacker);
            }
        }
    }
}
