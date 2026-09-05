using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
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
                    if (PurgeExpired(creature))
                    {
                        PublishRefresh(creature);
                    }
                }
                else
                {
                    Clear(creature);
                }
            }
        }

        public static void Add(uint creature, StatType statType, int amount, float durationSeconds, string group = null)
        {
            AddInternal(creature, statType, amount, durationSeconds, group, true);
        }

        private static void AddInternal(
            uint creature,
            StatType statType,
            int amount,
            float durationSeconds,
            string group,
            bool publishRefresh)
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

            if (publishRefresh)
            {
                PublishRefresh(creature);
            }
        }

        public static void Add(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType)
        {
            Add(creature, statType, amount, durationSeconds, GetGroup(groupStatType));
        }

        public static void Replace(uint creature, StatType statType, int amount, float durationSeconds, string group = null)
        {
            ConsumeInternal(creature, statType, group, false);
            AddInternal(creature, statType, amount, durationSeconds, group, false);
            PublishRefresh(creature);
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
            int requestedStacks,
            bool refreshExistingStacks = false)
        {
            if (amount <= 0 || durationSeconds <= 0f || maxTotal <= 0 || requestedStacks <= 0)
                return 0;

            var current = GetStatAdjustment(creature, statType, group);
            var remaining = maxTotal - current;

            // Stacking buffs that refresh keep every stack on a single shared clock, so the
            // visible duration and the real bonus expire together. Without this, older stacks
            // drop off one at a time while the status effect still shows a full timer.
            if (refreshExistingStacks)
            {
                Refresh(creature, statType, durationSeconds, group);
            }

            if (remaining <= 0)
                return 0;

            var stacks = Math.Min(requestedStacks, remaining / amount);
            for (var index = 0; index < stacks; index++)
            {
                AddInternal(creature, statType, amount, durationSeconds, group, false);
            }

            if (stacks > 0)
            {
                PublishRefresh(creature);
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
            int requestedStacks,
            bool refreshExistingStacks = false)
        {
            return AddCapped(
                creature,
                statType,
                amount,
                durationSeconds,
                maxTotal,
                GetGroup(groupStatType),
                requestedStacks,
                refreshExistingStacks);
        }

        public static int Consume(uint creature, StatType statType, string group = null)
        {
            return ConsumeInternal(creature, statType, group, true);
        }

        private static int ConsumeInternal(
            uint creature,
            StatType statType,
            string group,
            bool publishRefresh)
        {
            var expiredRemoved = PurgeExpired(creature);

            if (!_modifiers.TryGetValue(creature, out var modifiers))
            {
                if (expiredRemoved && publishRefresh)
                {
                    PublishRefresh(creature);
                }
                return 0;
            }

            var matching = modifiers
                .Where(x => x.StatType == statType && MatchesGroup(x, group))
                .ToList();
            var total = matching.Aggregate(
                0, (aggregated, x) => Stat.AggregateStatAdjustment(statType, aggregated, x.Amount));

            foreach (var modifier in matching)
            {
                modifiers.Remove(modifier);
            }

            if (modifiers.Count <= 0)
            {
                _modifiers.Remove(creature);
            }

            if ((expiredRemoved || matching.Count > 0) && publishRefresh)
            {
                PublishRefresh(creature);
            }

            return total;
        }

        public static int Consume(uint creature, StatType statType, StatType groupStatType)
        {
            return Consume(creature, statType, GetGroup(groupStatType));
        }

        public static IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
        {
            if (PurgeExpired(creature))
                PublishRefresh(creature);

            if (!_modifiers.TryGetValue(creature, out var modifiers))
                return Array.Empty<StatAdjustmentSource>();

            Dictionary<string, Dictionary<StatType, int>> groups = null;
            foreach (var modifier in modifiers)
            {
                if (modifier.StatType != payloadStat || modifier.Amount == 0)
                    continue;

                groups ??= new Dictionary<string, Dictionary<StatType, int>>();
                if (!groups.ContainsKey(modifier.Group))
                    groups.Add(modifier.Group, new Dictionary<StatType, int>());
            }

            if (groups == null)
                return Array.Empty<StatAdjustmentSource>();

            foreach (var modifier in modifiers)
            {
                if (groups.TryGetValue(modifier.Group, out var stats))
                {
                    stats.TryGetValue(modifier.StatType, out var current);
                    stats[modifier.StatType] = Stat.AggregateStatAdjustment(modifier.StatType, current, modifier.Amount);
                }
            }

            var sources = new StatAdjustmentSource[groups.Count];
            var index = 0;
            foreach (var (group, stats) in groups)
                sources[index++] = new StatAdjustmentSource($"temporary:{group}", stats);
            return sources;
        }

        public static int GetStatAdjustment(uint creature, StatType statType, string group = null)
        {
            if (PurgeExpired(creature))
            {
                PublishRefresh(creature);
            }

            return _modifiers.TryGetValue(creature, out var modifiers)
                ? modifiers
                    .Where(x => x.StatType == statType && MatchesGroup(x, group))
                    .Aggregate(0, (total, x) => Stat.AggregateStatAdjustment(statType, total, x.Amount))
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

            if (PurgeExpired(creature))
            {
                PublishRefresh(creature);
            }

            if (!_modifiers.TryGetValue(creature, out var modifiers))
                return;

            var expiration = DateTime.UtcNow.AddSeconds(durationSeconds);
            foreach (var modifier in modifiers.Where(x => x.StatType == statType && MatchesGroup(x, group)))
            {
                modifier.Refresh(expiration);
            }
        }

        public static void Refresh(uint creature, StatType statType, float durationSeconds, StatType groupStatType)
        {
            Refresh(creature, statType, durationSeconds, GetGroup(groupStatType));
        }

        public static void Clear(uint creature)
        {
            if (_modifiers.Remove(creature))
            {
                PublishRefresh(creature);
            }
        }

        private static string GetGroup(StatType groupStatType)
        {
            return groupStatType.ToString();
        }

        private static bool MatchesGroup(TemporaryModifier modifier, string group)
        {
            return group == null || modifier.Group == group;
        }

        private static bool PurgeExpired(uint creature)
        {
            if (!_modifiers.TryGetValue(creature, out var modifiers))
                return false;

            var now = DateTime.UtcNow;
            var retained = 0;
            for (var index = 0; index < modifiers.Count; index++)
            {
                var modifier = modifiers[index];
                if (modifier.Expiration > now)
                {
                    if (retained != index)
                        modifiers[retained] = modifier;
                    retained++;
                }
            }
            var removed = retained < modifiers.Count;
            if (removed)
                modifiers.RemoveRange(retained, modifiers.Count - retained);

            if (modifiers.Count <= 0)
            {
                _modifiers.Remove(creature);
            }

            return removed;
        }

        private static void PublishRefresh(uint creature)
        {
            if (GetIsPC(creature))
            {
                DelayCommand(0.0f, () => Gui.PublishRefreshEvent(creature, new StatAdjustmentRefreshEvent()));
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
