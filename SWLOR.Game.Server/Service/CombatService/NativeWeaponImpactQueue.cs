using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>
    /// Holds work that must wait until the engine applies a native weapon roll's damage.
    /// GetDamageRoll resolves a roll when the swing or shot starts. SignalMeleeDamage or
    /// SignalRangedDamage then creates the roll's damage effect and schedules it for the moment
    /// the swing connects or the projectile arrives. Work is queued per attack slot at the roll,
    /// bound to the effects its signal creates, and released when one of those effects is applied.
    /// </summary>
    public static class NativeWeaponImpactQueue
    {
        /// <summary>Work whose roll or effect never lands is dropped after this long.</summary>
        public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(6);

        // Destroyed attackers never take their work, so sweep expired work once this many attackers wait.
        private const int SweepThreshold = 128;

        private sealed record QueuedImpact(long Roll, int AttackIndex, uint Target, DateTime ExpiresAt, Action Action);

        private sealed record SignalledImpact(uint Target, ulong FirstEffectId, ulong LastEffectId, DateTime ExpiresAt,
            List<Action> Actions);

        private static readonly Dictionary<uint, List<QueuedImpact>> _queued = new();
        private static readonly Dictionary<uint, List<SignalledImpact>> _signalled = new();
        private static long _nextRoll;

        public static bool IsEmpty => _queued.Count == 0 && _signalled.Count == 0;
        public static bool HasSignalled => _signalled.Count > 0;

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
            if (_queued.Count + _signalled.Count >= SweepThreshold)
                Sweep(now);

            if (!_queued.TryGetValue(attacker, out var impacts))
                _queued[attacker] = impacts = new List<QueuedImpact>();

            // A new roll in a slot means the slot's earlier roll was never signalled.
            impacts.RemoveAll(impact => impact.ExpiresAt <= now || impact.AttackIndex == attackIndex && impact.Roll != roll);
            impacts.Add(new QueuedImpact(roll, attackIndex, target, now + Lifetime, action));
        }

        /// <summary>
        /// Forgets the work an earlier roll queued in this attack slot. Called when any new roll starts in the
        /// slot, hit or miss, because that earlier roll was never signalled and the slot's next signal
        /// belongs to the new roll.
        /// </summary>
        public static void ForgetSlot(uint attacker, int attackIndex)
        {
            if (!_queued.TryGetValue(attacker, out var impacts))
                return;
            impacts.RemoveAll(impact => impact.AttackIndex == attackIndex);
            if (impacts.Count == 0)
                _queued.Remove(attacker);
        }

        /// <summary>
        /// Whether a damage signal for <paramref name="attacks"/> slots starting at <paramref name="firstAttack"/>
        /// (wrapping like the engine's byte attack cursor) has queued work to bind.
        /// </summary>
        public static bool HasQueued(uint attacker, int firstAttack, int attacks)
        {
            return attacks > 0 && _queued.TryGetValue(attacker, out var impacts) &&
                   impacts.Any(impact => IsSignalledSlot(impact.AttackIndex, firstAttack, attacks));
        }

        /// <summary>
        /// Binds the work queued in a signal's slots to the damage effects that signal created, whose IDs lie
        /// strictly between <paramref name="effectIdBefore"/> and <paramref name="effectIdAfter"/>. Work for
        /// those slots queued against another target is dropped.
        /// </summary>
        public static void Bind(uint attacker, uint target, int firstAttack, int attacks,
            ulong effectIdBefore, ulong effectIdAfter, DateTime now)
        {
            if (attacks <= 0 || !_queued.TryGetValue(attacker, out var impacts))
                return;

            List<Action> actions = null;
            var kept = new List<QueuedImpact>(impacts.Count);
            foreach (var impact in impacts)
            {
                if (impact.ExpiresAt <= now)
                    continue;
                if (!IsSignalledSlot(impact.AttackIndex, firstAttack, attacks))
                {
                    kept.Add(impact);
                    continue;
                }
                if (impact.Target == target)
                    (actions ??= new List<Action>()).Add(impact.Action);
            }

            if (kept.Count == 0)
                _queued.Remove(attacker);
            else
                _queued[attacker] = kept;

            if (actions == null || effectIdAfter <= effectIdBefore + 1)
                return;
            if (!_signalled.TryGetValue(attacker, out var signalled))
                _signalled[attacker] = signalled = new List<SignalledImpact>();
            signalled.RemoveAll(impact => impact.ExpiresAt <= now);
            signalled.Add(new SignalledImpact(target, effectIdBefore + 1, effectIdAfter - 1, now + Lifetime, actions));
        }

        /// <summary>
        /// Removes and returns, in queue order, the work bound to an applied damage effect. One signal's work
        /// runs once, with the first of its effects to land.
        /// </summary>
        public static IReadOnlyList<Action> TakeApplied(uint attacker, uint target, ulong effectId, DateTime now)
        {
            if (!_signalled.TryGetValue(attacker, out var signalled))
                return Array.Empty<Action>();

            List<Action> due = null;
            signalled.RemoveAll(impact =>
            {
                if (impact.ExpiresAt <= now)
                    return true;
                if (impact.Target != target || effectId < impact.FirstEffectId || effectId > impact.LastEffectId)
                    return false;
                (due ??= new List<Action>()).AddRange(impact.Actions);
                return true;
            });
            if (signalled.Count == 0)
                _signalled.Remove(attacker);
            return due ?? (IReadOnlyList<Action>)Array.Empty<Action>();
        }

        public static void Clear()
        {
            _queued.Clear();
            _signalled.Clear();
        }

        private static bool IsSignalledSlot(int attackIndex, int firstAttack, int attacks)
        {
            return ((attackIndex - firstAttack) & 0xFF) < attacks;
        }

        private static void Sweep(DateTime now)
        {
            foreach (var attacker in _queued.Keys.ToList())
            {
                var impacts = _queued[attacker];
                impacts.RemoveAll(impact => impact.ExpiresAt <= now);
                if (impacts.Count == 0)
                    _queued.Remove(attacker);
            }

            foreach (var attacker in _signalled.Keys.ToList())
            {
                var impacts = _signalled[attacker];
                impacts.RemoveAll(impact => impact.ExpiresAt <= now);
                if (impacts.Count == 0)
                    _signalled.Remove(attacker);
            }
        }
    }
}
