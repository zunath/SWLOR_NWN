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

        private sealed record SignalledSlot(int[] Damage, List<Action> Actions);

        private sealed record SignalledImpact(uint Target, ulong FirstEffectId, ulong LastEffectId, DateTime ExpiresAt,
            List<SignalledSlot> Slots);

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
        /// strictly between <paramref name="effectIdBefore"/> and <paramref name="effectIdAfter"/>.
        /// <paramref name="slotDamage"/> holds each signalled slot's rolled damage by native damage type, starting
        /// at <paramref name="firstAttack"/>; an effect is matched to the slot whose damage it carries. Work for
        /// those slots queued against another target is dropped.
        /// </summary>
        public static void Bind(uint attacker, uint target, int firstAttack, IReadOnlyList<int[]> slotDamage,
            ulong effectIdBefore, ulong effectIdAfter, DateTime now)
        {
            var attacks = slotDamage?.Count ?? 0;
            if (attacks <= 0 || !_queued.TryGetValue(attacker, out var impacts))
                return;

            var slots = new SignalledSlot[attacks];
            for (var offset = 0; offset < attacks; offset++)
                slots[offset] = new SignalledSlot(slotDamage[offset], new List<Action>());
            var hasWork = false;
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
                slots[(impact.AttackIndex - firstAttack) & 0xFF].Actions.Add(impact.Action);
                hasWork = true;
            }

            if (kept.Count == 0)
                _queued.Remove(attacker);
            else
                _queued[attacker] = kept;

            if (!hasWork || effectIdAfter <= effectIdBefore + 1)
                return;
            if (!_signalled.TryGetValue(attacker, out var signalled))
                _signalled[attacker] = signalled = new List<SignalledImpact>();
            signalled.RemoveAll(impact => impact.ExpiresAt <= now);
            // Slots without work stay as placeholders so their effects cannot release another slot's work.
            signalled.Add(new SignalledImpact(target, effectIdBefore + 1, effectIdAfter - 1, now + Lifetime, slots.ToList()));
        }

        /// <summary>
        /// Removes and returns, in queue order, the work of the slot an applied damage effect belongs to: the first
        /// remaining slot of that effect's signal whose rolled damage the effect carries, or the signal's first
        /// remaining slot when none does.
        /// </summary>
        public static IReadOnlyList<Action> TakeApplied(uint attacker, uint target, ulong effectId, int[] effectDamage,
            DateTime now)
        {
            if (!_signalled.TryGetValue(attacker, out var signalled))
                return Array.Empty<Action>();

            IReadOnlyList<Action> due = Array.Empty<Action>();
            signalled.RemoveAll(impact => impact.ExpiresAt <= now);
            var signal = signalled.FirstOrDefault(impact => impact.Target == target &&
                                                            effectId >= impact.FirstEffectId && effectId <= impact.LastEffectId);
            if (signal != null)
            {
                var slot = signal.Slots.FirstOrDefault(candidate => CarriesDamage(effectDamage, candidate.Damage)) ??
                           signal.Slots[0];
                signal.Slots.Remove(slot);
                due = slot.Actions;
                if (signal.Slots.All(remaining => remaining.Actions.Count == 0))
                    signalled.Remove(signal);
            }

            if (signalled.Count == 0)
                _signalled.Remove(attacker);
            return due;
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
