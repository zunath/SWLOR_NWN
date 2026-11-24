using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>
    /// Holds work that must wait until the engine applies a native weapon roll's damage.
    /// GetDamageRoll resolves a roll when the swing or shot starts. SignalMeleeDamage or
    /// SignalRangedDamage then creates each landed slot's damage effect, in slot order, and
    /// schedules it for the moment the swing connects or the projectile arrives. Work is queued
    /// per attack slot at the roll, bound to its own slot's damage effect when the signal runs,
    /// and released when exactly that effect is applied.
    /// </summary>
    public static class NativeWeaponImpactQueue
    {
        /// <summary>Work whose roll or effect never lands is dropped after this long.</summary>
        public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(6);

        // Destroyed attackers never take their work, so sweep expired work once this much waits.
        private const int SweepThreshold = 128;

        private sealed record QueuedImpact(long Roll, int AttackIndex, uint Target, DateTime ExpiresAt, Action Action);

        private sealed record BoundImpact(uint Attacker, uint Target, DateTime ExpiresAt, List<Action> Actions);

        private static readonly Dictionary<uint, List<QueuedImpact>> _queued = new();
        private static readonly Dictionary<ulong, BoundImpact> _bound = new();
        private static long _nextRoll;

        public static bool IsEmpty => _queued.Count == 0 && _bound.Count == 0;
        public static bool HasBound => _bound.Count > 0;

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
            if (_queued.Count + _bound.Count >= SweepThreshold)
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
        /// Binds the work queued in a signal's slots to the damage effects that signal scheduled against
        /// <paramref name="target"/>. <paramref name="slotDamage"/> holds each signalled slot's rolled damage by
        /// native damage type, starting at <paramref name="firstAttack"/>; <paramref name="scheduledEffects"/> holds
        /// the signal's combat damage effects. The engine creates them in slot order, skipping slots that deal no
        /// damage, so effects are matched to slots in ID order, each to the next slot whose damage it carries.
        /// Work for those slots queued against another target is dropped. Returns work whose slot got no effect,
        /// which the caller runs at once rather than lose.
        /// </summary>
        public static IReadOnlyList<Action> Bind(uint attacker, uint target, int firstAttack, IReadOnlyList<int[]> slotDamage,
            IEnumerable<(ulong Id, int[] Damage)> scheduledEffects, DateTime now)
        {
            var attacks = slotDamage?.Count ?? 0;
            if (attacks <= 0 || !_queued.TryGetValue(attacker, out var impacts))
                return Array.Empty<Action>();

            var slotWork = new List<Action>[attacks];
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
                if (impact.Target != target)
                    continue;
                (slotWork[(impact.AttackIndex - firstAttack) & 0xFF] ??= new List<Action>()).Add(impact.Action);
            }

            if (kept.Count == 0)
                _queued.Remove(attacker);
            else
                _queued[attacker] = kept;

            var slotEffects = AssignEffectsToSlots(slotDamage, scheduledEffects);
            List<Action> unbound = null;
            for (var slot = 0; slot < attacks; slot++)
            {
                if (slotWork[slot] == null)
                    continue;
                if (slotEffects[slot] is { } effectId)
                    _bound[effectId] = new BoundImpact(attacker, target, now + Lifetime, slotWork[slot]);
                else
                    (unbound ??= new List<Action>()).AddRange(slotWork[slot]);
            }

            return unbound ?? (IReadOnlyList<Action>)Array.Empty<Action>();
        }

        /// <summary>
        /// Matches a signal's damage effects to its slots: in ID order, each effect goes to the next slot whose
        /// rolled damage it carries, or to the next slot when none of the remaining slots matches.
        /// </summary>
        public static ulong?[] AssignEffectsToSlots(IReadOnlyList<int[]> slotDamage,
            IEnumerable<(ulong Id, int[] Damage)> scheduledEffects)
        {
            var slots = new ulong?[slotDamage?.Count ?? 0];
            var next = 0;
            foreach (var effect in (scheduledEffects ?? Enumerable.Empty<(ulong Id, int[] Damage)>()).OrderBy(effect => effect.Id))
            {
                if (next >= slots.Length)
                    break;
                var slot = next;
                while (slot < slots.Length && !CarriesDamage(effect.Damage, slotDamage[slot]))
                    slot++;
                if (slot == slots.Length)
                    slot = next;
                slots[slot] = effect.Id;
                next = slot + 1;
            }

            return slots;
        }

        /// <summary>Removes and returns the work bound to exactly this applied damage effect.</summary>
        public static IReadOnlyList<Action> TakeApplied(uint attacker, uint target, ulong effectId, DateTime now)
        {
            if (!_bound.TryGetValue(effectId, out var impact) || impact.Attacker != attacker || impact.Target != target)
                return Array.Empty<Action>();

            _bound.Remove(effectId);
            return impact.ExpiresAt <= now ? Array.Empty<Action>() : impact.Actions;
        }

        public static void Clear()
        {
            _queued.Clear();
            _bound.Clear();
        }

        /// <summary>Whether an effect's damage by native type equals a slot's rolled damage (no damage is 0 or -1).</summary>
        private static bool CarriesDamage(int[] effectDamage, int[] slotDamage)
        {
            var length = Math.Max(effectDamage?.Length ?? 0, slotDamage?.Length ?? 0);
            for (var index = 0; index < length; index++)
            {
                var effectAmount = effectDamage != null && index < effectDamage.Length ? Math.Max(0, effectDamage[index]) : 0;
                var slotAmount = slotDamage != null && index < slotDamage.Length ? Math.Max(0, slotDamage[index]) : 0;
                if (effectAmount != slotAmount)
                    return false;
            }
            return true;
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

            foreach (var effectId in _bound.Where(pair => pair.Value.ExpiresAt <= now).Select(pair => pair.Key).ToList())
                _bound.Remove(effectId);
        }
    }
}
