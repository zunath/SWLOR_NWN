using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class CombatState
    {
        private static readonly Dictionary<(uint, StatType), DateTime> _statTriggerCooldowns = new();
        private static readonly Dictionary<(uint, uint), DateTime> _recentDamageTargets = new();
        private static readonly Dictionary<uint, DateTime> _recentDamageTaken = new();
        private static readonly Dictionary<uint, DateTime> _recentGuardedHits = new();
        private static readonly Dictionary<uint, DateTime> _recentDeflections = new();
        private static readonly Dictionary<uint, DateTime> _lastCombatActivity = new();
        private static readonly Dictionary<uint, DateTime> _lastAttackActivity = new();
        private static readonly Dictionary<uint, DateTime> _lastCombatAbilityUse = new();
        private static readonly Dictionary<uint, HostileAbilitySequenceState> _hostileAbilitySequenceStates = new();
        private static readonly Dictionary<uint, CriticalHitSequenceState> _criticalHitSequenceStates = new();
        private static readonly Dictionary<(uint, uint), int> _sameTargetHostileAbilityHitCounts = new();
        private static readonly Dictionary<uint, int> _autoAttackCycleCounts = new();
        private static readonly Dictionary<uint, int> _autoAttackCycleCriticalCounts = new();
        private static readonly Dictionary<(uint, uint), TargetHitSequenceState> _areaAbilityTargetHitSequences = new();
        private static readonly Dictionary<uint, float> _attackSwingDebts = new();
        private static readonly Dictionary<uint, RepeatedTargetDamageState> _repeatedTargetDamageStates = new();
        private static readonly Dictionary<uint, AbilityStaminaCostState> _lastAbilityStaminaCosts = new();

        private static Func<DateTime> _utcNow = () => DateTime.UtcNow;

        internal static void SetClock(Func<DateTime> utcNow)
        {
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        internal static void ResetClock()
        {
            _utcNow = () => DateTime.UtcNow;
        }

        internal static void ClearAllForTests()
        {
            _statTriggerCooldowns.Clear();
            _recentDamageTargets.Clear();
            _recentDamageTaken.Clear();
            _recentGuardedHits.Clear();
            _recentDeflections.Clear();
            _lastCombatActivity.Clear();
            _lastAttackActivity.Clear();
            _lastCombatAbilityUse.Clear();
            _hostileAbilitySequenceStates.Clear();
            _criticalHitSequenceStates.Clear();
            _sameTargetHostileAbilityHitCounts.Clear();
            _autoAttackCycleCounts.Clear();
            _autoAttackCycleCriticalCounts.Clear();
            _areaAbilityTargetHitSequences.Clear();
            _attackSwingDebts.Clear();
            _repeatedTargetDamageStates.Clear();
            _lastAbilityStaminaCosts.Clear();
        }

        public static bool TryUseStatTrigger(uint creature, StatType statType, TimeSpan cooldown)
        {
            if (cooldown <= TimeSpan.Zero)
                return true;

            var key = (creature, statType);
            var now = Now;
            if (_statTriggerCooldowns.TryGetValue(key, out var nextAvailable) && nextAvailable > now)
                return false;

            _statTriggerCooldowns[key] = now.Add(cooldown);
            return true;
        }

        public static void TrackRecentDamageTarget(uint attacker, uint defender)
        {
            _recentDamageTargets[(attacker, defender)] = Now;
        }

        public static bool HasRecentDamageTarget(uint attacker, uint defender, float windowSeconds)
        {
            return HasRecent(_recentDamageTargets, (attacker, defender), windowSeconds);
        }

        public static IReadOnlyList<uint> GetRecentDamageSourcesForTarget(uint target, float windowSeconds)
        {
            var now = Now;
            var recentDamagers = _recentDamageTargets
                .Where(x => x.Key.Item2 == target)
                .ToList();
            var sources = new List<uint>();

            foreach (var ((source, recentTarget), lastDamaged) in recentDamagers)
            {
                if ((now - lastDamaged).TotalSeconds > windowSeconds)
                {
                    _recentDamageTargets.Remove((source, recentTarget));
                    continue;
                }

                sources.Add(source);
            }

            return sources;
        }

        public static void TrackRecentDamageTaken(uint creature)
        {
            _recentDamageTaken[creature] = Now;
        }

        public static bool HasRecentDamageTaken(uint creature, float windowSeconds)
        {
            return HasRecent(_recentDamageTaken, creature, windowSeconds);
        }

        public static void TrackCombatActivity(uint creature)
        {
            _lastCombatActivity[creature] = Now;
        }

        public static void TrackAttackActivity(uint creature)
        {
            _lastAttackActivity[creature] = Now;
            TrackCombatActivity(creature);
        }

        public static bool HasRecentAttackActivity(uint creature, float windowSeconds)
        {
            return HasRecent(_lastAttackActivity, creature, windowSeconds);
        }

        public static bool HasRecentCombatActivity(uint creature, float windowSeconds)
        {
            return HasRecent(_lastCombatActivity, creature, windowSeconds);
        }

        public static bool IncrementAutoAttackCycle(uint attacker, int requiredCount)
        {
            return IncrementCycle(_autoAttackCycleCounts, attacker, requiredCount);
        }

        public static bool IncrementAutoAttackCriticalCycle(uint attacker, int requiredCount)
        {
            return IncrementCycle(_autoAttackCycleCriticalCounts, attacker, requiredCount);
        }

        public static bool TrackCriticalHitSequence(uint attacker, int requiredCount, float windowSeconds)
        {
            if (requiredCount <= 0 || windowSeconds <= 0f)
                return false;

            var now = Now;
            var count = 1;
            if (_criticalHitSequenceStates.TryGetValue(attacker, out var state) &&
                (now - state.LastHit).TotalSeconds <= windowSeconds)
            {
                count = state.Count + 1;
            }

            if (count >= requiredCount)
            {
                _criticalHitSequenceStates.Remove(attacker);
                return true;
            }

            _criticalHitSequenceStates[attacker] = new CriticalHitSequenceState
            {
                Count = count,
                LastHit = now
            };
            return false;
        }

        public static void TrackGuardedHit(uint creature)
        {
            _recentGuardedHits[creature] = Now;
        }

        public static bool HasRecentGuardedHit(uint creature, float windowSeconds)
        {
            return HasRecent(_recentGuardedHits, creature, windowSeconds);
        }

        public static void TrackDeflection(uint creature)
        {
            _recentDeflections[creature] = Now;
        }

        public static bool HasRecentDeflection(uint creature, float windowSeconds)
        {
            return HasRecent(_recentDeflections, creature, windowSeconds);
        }

        public static void ClearRepeatedTargetDamage(uint attacker)
        {
            _repeatedTargetDamageStates.Remove(attacker);
        }

        public static int TrackRepeatedTargetDamageHit(
            uint attacker,
            uint defender,
            float durationSeconds,
            int maxStacks)
        {
            if (maxStacks <= 0)
                return 0;

            var now = Now;
            if (!_repeatedTargetDamageStates.TryGetValue(attacker, out var state) ||
                state.Target != defender ||
                durationSeconds > 0f && (now - state.LastHit).TotalSeconds > durationSeconds)
            {
                state = new RepeatedTargetDamageState(defender, now);
            }

            state.Stacks = Math.Min(state.Stacks + 1, maxStacks);
            state.LastHit = now;
            _repeatedTargetDamageStates[attacker] = state;

            return state.Stacks;
        }

        public static void TrackAbilityStaminaCost(uint creature, int staminaCost)
        {
            _lastAbilityStaminaCosts[creature] = new AbilityStaminaCostState
            {
                Cost = staminaCost,
                SpentAt = Now
            };
        }

        public static bool TryGetRecentAbilityStaminaCost(
            uint creature,
            float windowSeconds,
            out int staminaCost)
        {
            staminaCost = 0;
            if (!_lastAbilityStaminaCosts.TryGetValue(creature, out var costState))
                return false;

            if ((Now - costState.SpentAt).TotalSeconds > windowSeconds)
            {
                _lastAbilityStaminaCosts.Remove(creature);
                return false;
            }

            staminaCost = costState.Cost;
            return true;
        }

        public static void ClearAbilityStaminaCost(uint creature)
        {
            _lastAbilityStaminaCosts.Remove(creature);
        }

        public static bool TrackSameTargetHostileAbilityHit(uint activator, uint target, int requiredCount)
        {
            if (requiredCount <= 0)
                return false;

            var key = (activator, target);
            _sameTargetHostileAbilityHitCounts.TryGetValue(key, out var count);
            count++;

            if (count < requiredCount)
            {
                _sameTargetHostileAbilityHitCounts[key] = count;
                return false;
            }

            _sameTargetHostileAbilityHitCounts[key] = 0;
            return true;
        }

        public static bool TrackAreaAbilityTargetHitSequence(
            uint activator,
            uint target,
            int requiredCount,
            float windowSeconds)
        {
            if (requiredCount <= 0 || windowSeconds <= 0f)
                return false;

            var key = (activator, target);
            var now = Now;
            var count = 1;
            if (_areaAbilityTargetHitSequences.TryGetValue(key, out var state) &&
                (now - state.LastHit).TotalSeconds <= windowSeconds)
            {
                count = state.Count + 1;
            }

            if (count >= requiredCount)
            {
                _areaAbilityTargetHitSequences.Remove(key);
                return true;
            }

            _areaAbilityTargetHitSequences[key] = new TargetHitSequenceState
            {
                Count = count,
                LastHit = now
            };
            return false;
        }

        public static void TrackCombatAbilityUse(uint activator)
        {
            _lastCombatAbilityUse[activator] = Now;
        }

        public static bool TrackHostileAbilitySequence(uint activator, FeatType feat, float windowSeconds)
        {
            var now = Now;
            var isSequence =
                windowSeconds > 0f &&
                _hostileAbilitySequenceStates.TryGetValue(activator, out var state) &&
                state.LastFeat != feat &&
                (now - state.LastUse).TotalSeconds <= windowSeconds;

            _hostileAbilitySequenceStates[activator] = new HostileAbilitySequenceState
            {
                LastFeat = feat,
                LastUse = now
            };

            return isSequence;
        }

        public static bool HasRecentCombatAbilityUse(uint activator, float windowSeconds)
        {
            return HasRecent(_lastCombatAbilityUse, activator, windowSeconds);
        }

        public static float GetAttackSwingDebt(uint attacker)
        {
            return _attackSwingDebts.TryGetValue(attacker, out var attackDebt)
                ? attackDebt
                : 0f;
        }

        public static void UpdateAttackSwingDebt(uint attacker, float attackDebt)
        {
            if (attackDebt <= 0f)
            {
                _attackSwingDebts.Remove(attacker);
                return;
            }

            _attackSwingDebts[attacker] = attackDebt;
        }

        public static void ClearAttackSwingDebt(uint attacker)
        {
            _attackSwingDebts.Remove(attacker);
        }

        public static void ClearCreature(uint creature)
        {
            foreach (var key in _statTriggerCooldowns.Keys.Where(x => x.Item1 == creature).ToList())
            {
                _statTriggerCooldowns.Remove(key);
            }

            foreach (var key in _recentDamageTargets.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _recentDamageTargets.Remove(key);
            }

            _recentDamageTaken.Remove(creature);
            _recentGuardedHits.Remove(creature);
            _recentDeflections.Remove(creature);
            _lastCombatActivity.Remove(creature);
            _lastAttackActivity.Remove(creature);
            _lastCombatAbilityUse.Remove(creature);
            _hostileAbilitySequenceStates.Remove(creature);
            _criticalHitSequenceStates.Remove(creature);

            foreach (var key in _sameTargetHostileAbilityHitCounts.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _sameTargetHostileAbilityHitCounts.Remove(key);
            }

            _autoAttackCycleCounts.Remove(creature);
            _autoAttackCycleCriticalCounts.Remove(creature);
            _lastAbilityStaminaCosts.Remove(creature);

            foreach (var key in _areaAbilityTargetHitSequences.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _areaAbilityTargetHitSequences.Remove(key);
            }

            _attackSwingDebts.Remove(creature);
            _repeatedTargetDamageStates.Remove(creature);
            TemporaryStatModifier.Clear(creature);
        }

        private static DateTime Now => _utcNow();

        private static bool HasRecent<TKey>(
            IDictionary<TKey, DateTime> timestamps,
            TKey key,
            float windowSeconds)
        {
            if (windowSeconds <= 0f)
                return false;

            if (!timestamps.TryGetValue(key, out var timestamp))
                return false;

            var isRecent = (Now - timestamp).TotalSeconds <= windowSeconds;
            if (!isRecent)
                timestamps.Remove(key);

            return isRecent;
        }

        private static bool IncrementCycle<TKey>(
            IDictionary<TKey, int> counts,
            TKey key,
            int requiredCount)
        {
            if (requiredCount <= 0)
                return false;

            counts.TryGetValue(key, out var count);
            count++;
            if (count < requiredCount)
            {
                counts[key] = count;
                return false;
            }

            counts[key] = 0;
            return true;
        }

        private sealed class HostileAbilitySequenceState
        {
            public FeatType LastFeat { get; init; }
            public DateTime LastUse { get; init; }
        }

        private sealed class CriticalHitSequenceState
        {
            public int Count { get; init; }
            public DateTime LastHit { get; init; }
        }

        private sealed class TargetHitSequenceState
        {
            public int Count { get; init; }
            public DateTime LastHit { get; init; }
        }

        private sealed class AbilityStaminaCostState
        {
            public int Cost { get; init; }
            public DateTime SpentAt { get; init; }
        }

        private sealed class RepeatedTargetDamageState
        {
            public uint Target { get; }
            public int Stacks { get; set; }
            public DateTime LastHit { get; set; }

            public RepeatedTargetDamageState(uint target, DateTime now)
            {
                Target = target;
                LastHit = now;
            }
        }
    }
}
