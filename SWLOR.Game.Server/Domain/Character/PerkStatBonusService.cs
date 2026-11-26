using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The stat bonuses perks give: the tables built from every perk's stat bonuses when the module loads, and the reads that
    /// <see cref="StatService"/> makes on every stat recalculation. They moved here from <see cref="PerkService"/> unchanged; the
    /// catalog and a creature's perk level stay there, and the force affinity reads are on <see cref="IPerkForceAffinityService"/>.
    /// </summary>
    public class PerkStatBonusService : IPerkStatBonusService
    {
        private readonly Dictionary<StatType, List<StatBonusGroup>> _statBonusGroupsByStat = new();
        private readonly Dictionary<StatType, List<TargetedStatBonusGroup>> _targetedStatBonusGroupsByAdjustmentStat = new();

        private class StatBonusGroup
        {
            public StatBonusGroup(PerkType perkType, PerkDetail perkDetail)
            {
                PerkType = perkType;
                PerkDetail = perkDetail;
            }

            public PerkType PerkType { get; }
            public PerkDetail PerkDetail { get; }
            public List<PerkStatBonus> PerkBonuses { get; } = new();
            public Dictionary<int, List<PerkStatBonus>> LevelBonuses { get; } = new();
        }

        private class TargetedStatBonusGroup
        {
            public TargetedStatBonusGroup(PerkType perkType)
            {
                PerkType = perkType;
            }

            public PerkType PerkType { get; }
            public Dictionary<int, Dictionary<StatType, List<PerkStatBonus>>> BonusesByLevel { get; } = new();
        }

        private readonly IPerkService _perk;
        private readonly ICompanionLookup _companions;

        public PerkStatBonusService(IPerkService perk, ICompanionLookup companions)
        {
            _perk = perk;
            _companions = companions;
        }

        /// <summary>
        /// When the module loads, builds the stat bonus tables from every perk the catalog cached.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CachePerkStatBonuses()
        {
            foreach (var (perkType, perkDetail) in _perk.EnumerateAllPerks())
            {
                CacheStatBonusPerk(perkType, perkDetail);
            }
        }

        private void CacheStatBonusPerk(PerkType perkType, PerkDetail perkDetail)
        {
            foreach (var statBonus in perkDetail.StatBonuses)
            {
                GetOrCreateStatBonusGroup(statBonus.Stat, perkType, perkDetail).PerkBonuses.Add(statBonus);
            }

            foreach (var (level, perkLevel) in perkDetail.PerkLevels)
            {
                if (perkLevel.StatBonuses.Count <= 0)
                    continue;

                var levelBonusesByStat = new Dictionary<StatType, List<PerkStatBonus>>();
                foreach (var statBonus in perkLevel.StatBonuses)
                {
                    var statBonusGroup = GetOrCreateStatBonusGroup(statBonus.Stat, perkType, perkDetail);
                    if (!statBonusGroup.LevelBonuses.TryGetValue(level, out var levelBonuses))
                    {
                        levelBonuses = new List<PerkStatBonus>();
                        statBonusGroup.LevelBonuses[level] = levelBonuses;
                    }

                    levelBonuses.Add(statBonus);

                    if (!levelBonusesByStat.TryGetValue(statBonus.Stat, out var bonusesByStat))
                    {
                        bonusesByStat = new List<PerkStatBonus>();
                        levelBonusesByStat[statBonus.Stat] = bonusesByStat;
                    }

                    bonusesByStat.Add(statBonus);
                }

                foreach (var adjustmentStat in levelBonusesByStat.Keys)
                {
                    var targetedStatBonusGroup = GetOrCreateTargetedStatBonusGroup(adjustmentStat, perkType);
                    targetedStatBonusGroup.BonusesByLevel[level] = levelBonusesByStat;
                }
            }
        }

        private StatBonusGroup GetOrCreateStatBonusGroup(
            StatType stat,
            PerkType perkType,
            PerkDetail perkDetail)
        {
            if (!_statBonusGroupsByStat.TryGetValue(stat, out var statBonusGroups))
            {
                statBonusGroups = new List<StatBonusGroup>();
                _statBonusGroupsByStat[stat] = statBonusGroups;
            }

            var statBonusGroup = statBonusGroups.SingleOrDefault(x => x.PerkType == perkType);
            if (statBonusGroup != null)
                return statBonusGroup;

            statBonusGroup = new StatBonusGroup(perkType, perkDetail);
            statBonusGroups.Add(statBonusGroup);
            return statBonusGroup;
        }

        private TargetedStatBonusGroup GetOrCreateTargetedStatBonusGroup(
            StatType adjustmentStat,
            PerkType perkType)
        {
            if (!_targetedStatBonusGroupsByAdjustmentStat.TryGetValue(adjustmentStat, out var targetedStatBonusGroups))
            {
                targetedStatBonusGroups = new List<TargetedStatBonusGroup>();
                _targetedStatBonusGroupsByAdjustmentStat[adjustmentStat] = targetedStatBonusGroups;
            }

            var targetedStatBonusGroup = targetedStatBonusGroups.SingleOrDefault(x => x.PerkType == perkType);
            if (targetedStatBonusGroup != null)
                return targetedStatBonusGroup;

            targetedStatBonusGroup = new TargetedStatBonusGroup(perkType);
            targetedStatBonusGroups.Add(targetedStatBonusGroup);
            return targetedStatBonusGroup;
        }

        public IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
        {
            if (!_statBonusGroupsByStat.TryGetValue(payloadStat, out var groups))
                return Array.Empty<StatAdjustmentSource>();

            List<StatAdjustmentSource> sources = null;
            foreach (var group in groups)
            {
                var level = GetStatBonusPerkLevel(creature, group.PerkType);
                if (level <= 0 || !group.PerkDetail.PerkLevels.TryGetValue(level, out var perkLevel))
                    continue;

                var stats = new Dictionary<StatType, int>();
                foreach (var bonus in group.PerkDetail.StatBonuses)
                {
                    stats.TryGetValue(bonus.Stat, out var current);
                    stats[bonus.Stat] = AggregateStatAdjustment(bonus.Stat, current, bonus.Calculate(creature));
                }
                foreach (var bonus in perkLevel.StatBonuses)
                {
                    stats.TryGetValue(bonus.Stat, out var current);
                    stats[bonus.Stat] = AggregateStatAdjustment(bonus.Stat, current, bonus.Calculate(creature));
                }

                if (stats.TryGetValue(payloadStat, out var value) && value != 0)
                {
                    sources ??= new List<StatAdjustmentSource>();
                    sources.Add(new StatAdjustmentSource($"perk:{(int)group.PerkType}", stats));
                }
            }
            return (IReadOnlyList<StatAdjustmentSource>)sources ?? Array.Empty<StatAdjustmentSource>();
        }

        public int GetStatBonus(uint creature, StatType stat)
        {
            var bonus = 0;
            if (!_statBonusGroupsByStat.TryGetValue(stat, out var statBonusGroups))
                return bonus;

            foreach (var statBonusGroup in statBonusGroups)
            {
                var level = GetStatBonusPerkLevel(creature, statBonusGroup.PerkType);
                if (level <= 0 || !statBonusGroup.PerkDetail.PerkLevels.ContainsKey(level))
                    continue;

                foreach (var statBonus in statBonusGroup.PerkBonuses)
                {
                    bonus = AggregateStatAdjustment(stat, bonus, statBonus.Calculate(creature));
                }

                if (!statBonusGroup.LevelBonuses.TryGetValue(level, out var levelBonuses))
                    continue;

                foreach (var statBonus in levelBonuses)
                {
                    bonus = AggregateStatAdjustment(stat, bonus, statBonus.Calculate(creature));
                }
            }

            return bonus;
        }

        public int GetTargetedStatBonus(
            uint creature,
            PerkType targetPerkType,
            StatType primaryPerkStatType,
            StatType secondaryPerkStatType,
            StatType adjustmentStatType)
        {
            if (targetPerkType == PerkType.Invalid)
                return 0;

            var bonus = 0;
            if (!_targetedStatBonusGroupsByAdjustmentStat.TryGetValue(adjustmentStatType, out var targetedStatBonusGroups))
                return bonus;

            foreach (var targetedStatBonusGroup in targetedStatBonusGroups)
            {
                var level = GetStatBonusPerkLevel(creature, targetedStatBonusGroup.PerkType);
                if (level <= 0 || !targetedStatBonusGroup.BonusesByLevel.TryGetValue(level, out var bonusesByStat))
                    continue;

                var primaryPerkValue = CalculateStatBonuses(creature, bonusesByStat, primaryPerkStatType);
                var secondaryPerkValue = CalculateStatBonuses(creature, bonusesByStat, secondaryPerkStatType);
                var adjustment = CalculateStatBonuses(creature, bonusesByStat, adjustmentStatType);

                if (adjustment != 0 && IsTargetedPerk(targetPerkType, primaryPerkValue, secondaryPerkValue))
                    bonus += adjustment;
            }

            return bonus;
        }

        private int CalculateStatBonuses(
            uint creature,
            IReadOnlyDictionary<StatType, List<PerkStatBonus>> bonusesByStat,
            StatType stat)
        {
            if (!bonusesByStat.TryGetValue(stat, out var statBonuses))
                return 0;

            var bonus = 0;
            foreach (var statBonus in statBonuses)
            {
                bonus += statBonus.Calculate(creature);
            }

            return bonus;
        }

        private int GetStatBonusPerkLevel(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            if (GetIsPC(creature) && !GetIsDMPossessed(creature) ||
                IsDroid(creature) ||
                _companions.IsPlayerBeast(creature))
            {
                return _perk.GetPerkLevel(creature, perkType);
            }

            return GetLocalInt(creature, $"PERK_LEVEL_{(int)perkType}");
        }

        private bool IsTargetedPerk(PerkType targetPerkType, int primaryPerkValue, int secondaryPerkValue)
        {
            return targetPerkType == GetPerkTypeFromStat(primaryPerkValue) ||
                   targetPerkType == GetPerkTypeFromStat(secondaryPerkValue);
        }

        private PerkType GetPerkTypeFromStat(int value)
        {
            return value > 0 && System.Enum.IsDefined(typeof(PerkType), value)
                ? (PerkType)value
                : PerkType.Invalid;
        }

        protected virtual int AggregateStatAdjustment(StatType stat, int current, int adjustment)
        {
            return StatRules.AggregateStatAdjustment(stat, current, adjustment);
        }

        protected virtual bool IsDroid(uint creature)
        {
            return _companions.IsDroid(creature);
        }
    }
}