using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class TemporaryStatModifier
    {
        private const string DefaultGroup = "";

        private static readonly Dictionary<uint, List<TemporaryModifier>> _modifiers = new();

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearExitingObject()
        {
            Clear(GetExitingObject());
        }

        [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
        public static void PurgeAllExpired()
        {
            foreach (var creature in _modifiers.Keys.ToList())
            {
                if (GetIsObjectValid(creature))
                {
                    PurgeExpired(creature);
                }
                else
                {
                    Clear(creature);
                }
            }
        }

        public static void Add(uint creature, StatType statType, int amount, float durationSeconds, string group = null)
        {
            if (creature == OBJECT_INVALID || amount == 0 || durationSeconds <= 0f)
                return;

            PurgeExpired(creature);

            if (!_modifiers.ContainsKey(creature))
            {
                _modifiers[creature] = new List<TemporaryModifier>();
            }

            _modifiers[creature].Add(new TemporaryModifier(
                statType,
                amount,
                DateTime.UtcNow.AddSeconds(durationSeconds),
                group ?? DefaultGroup));
        }

        public static void Add(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType)
        {
            Add(creature, statType, amount, durationSeconds, GetGroup(groupStatType));
        }

        public static void Replace(uint creature, StatType statType, int amount, float durationSeconds, string group = null)
        {
            Consume(creature, statType, group);
            Add(creature, statType, amount, durationSeconds, group);
        }

        public static void Replace(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType)
        {
            Replace(creature, statType, amount, durationSeconds, GetGroup(groupStatType));
        }

        public static int AddCapped(
            uint creature,
            StatType statType,
            int amount,
            float durationSeconds,
            int maxTotal,
            string group,
            int requestedStacks)
        {
            if (amount <= 0 || durationSeconds <= 0f || maxTotal <= 0 || requestedStacks <= 0)
                return 0;

            var current = GetStatAdjustment(creature, statType, group);
            var remaining = maxTotal - current;
            if (remaining <= 0)
                return 0;

            var stacks = Math.Min(requestedStacks, remaining / amount);
            for (var index = 0; index < stacks; index++)
            {
                Add(creature, statType, amount, durationSeconds, group);
            }

            return stacks;
        }

        public static int AddCapped(
            uint creature,
            StatType statType,
            int amount,
            float durationSeconds,
            int maxTotal,
            StatType groupStatType,
            int requestedStacks)
        {
            return AddCapped(
                creature,
                statType,
                amount,
                durationSeconds,
                maxTotal,
                GetGroup(groupStatType),
                requestedStacks);
        }

        public static int Consume(uint creature, StatType statType, string group = null)
        {
            PurgeExpired(creature);

            if (!_modifiers.TryGetValue(creature, out var modifiers))
                return 0;

            var matching = modifiers
                .Where(x => x.StatType == statType && MatchesGroup(x, group))
                .ToList();
            var total = matching.Sum(x => x.Amount);

            foreach (var modifier in matching)
            {
                modifiers.Remove(modifier);
            }

            if (modifiers.Count <= 0)
            {
                _modifiers.Remove(creature);
            }

            return total;
        }

        public static int Consume(uint creature, StatType statType, StatType groupStatType)
        {
            return Consume(creature, statType, GetGroup(groupStatType));
        }

        public static int GetStatAdjustment(uint creature, StatType statType, string group = null)
        {
            PurgeExpired(creature);

            return _modifiers.TryGetValue(creature, out var modifiers)
                ? modifiers
                    .Where(x => x.StatType == statType && MatchesGroup(x, group))
                    .Sum(x => x.Amount)
                : 0;
        }

        public static int GetStatAdjustment(uint creature, StatType statType, StatType groupStatType)
        {
            return GetStatAdjustment(creature, statType, GetGroup(groupStatType));
        }

        public static void Refresh(uint creature, StatType statType, float durationSeconds, string group = null)
        {
            if (durationSeconds <= 0f)
                return;

            PurgeExpired(creature);

            if (!_modifiers.TryGetValue(creature, out var modifiers))
                return;

            var expiration = DateTime.UtcNow.AddSeconds(durationSeconds);
            foreach (var modifier in modifiers.Where(x => x.StatType == statType && MatchesGroup(x, group)))
            {
                modifier.Refresh(expiration);
            }
        }

        public static void Clear(uint creature)
        {
            _modifiers.Remove(creature);
        }

        private static string GetGroup(StatType groupStatType)
        {
            return groupStatType.ToString();
        }

        private static bool MatchesGroup(TemporaryModifier modifier, string group)
        {
            return group == null || modifier.Group == group;
        }

        private static void PurgeExpired(uint creature)
        {
            if (!_modifiers.TryGetValue(creature, out var modifiers))
                return;

            var now = DateTime.UtcNow;
            modifiers.RemoveAll(x => x.Expiration <= now);

            if (modifiers.Count <= 0)
            {
                _modifiers.Remove(creature);
            }
        }

        private sealed class TemporaryModifier
        {
            public StatType StatType { get; }
            public int Amount { get; }
            public DateTime Expiration { get; private set; }
            public string Group { get; }

            public TemporaryModifier(StatType statType, int amount, DateTime expiration, string group)
            {
                StatType = statType;
                Amount = amount;
                Expiration = expiration;
                Group = group;
            }

            public void Refresh(DateTime expiration)
            {
                Expiration = expiration;
            }
        }
    }
}
