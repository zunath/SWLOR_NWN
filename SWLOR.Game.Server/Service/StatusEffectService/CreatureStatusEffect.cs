using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public class CreatureStatusEffect
    {
        private readonly HashSet<IStatusEffect> _allActiveEffects = new();
        private readonly HashSet<IStatusEffect> _tickEffects = new();
        private readonly HashSet<IStatusEffect> _onHitEffects = new();
        private readonly Dictionary<StatusEffectSourceType, HashSet<IStatusEffect>> _effectsBySourceType = new();
        public StatGroup StatGroup { get; set; }

        /// <summary>
        /// Reads the creature's aggregate contribution to a stat.
        ///
        /// Identity stats cannot be kept as a running total, because Add/Remove maintain one
        /// incrementally and removing the highest contributor would leave a stale value behind.
        /// They are recomputed from the live effects instead.
        /// </summary>
        public int GetStatAdjustment(StatType stat)
        {
            if (!Stat.IsIdentityStat(stat))
            {
                return StatGroup.Stats[stat];
            }

            var identity = 0;
            foreach (var statusEffect in _allActiveEffects)
            {
                if (statusEffect.StatGroup.Stats.TryGetValue(stat, out var value) && value > identity)
                    identity = value;
            }

            return identity;
        }

        public void Add(IStatusEffect statusEffect)
        {
            foreach (var (type, value) in statusEffect.StatGroup.Stats)
            {
                StatGroup.Stats.TryGetValue(type, out var current);
                StatGroup.Stats[type] = current + value;
            }

            foreach (var (type, value) in statusEffect.StatGroup.Resists)
            {
                StatGroup.Resists.TryGetValue(type, out var current);
                StatGroup.Resists[type] = current + value;
            }

            foreach (var (type, value) in statusEffect.StatGroup.Abilities)
            {
                StatGroup.Abilities.TryGetValue(type, out var current);
                StatGroup.Abilities[type] = current + value;
            }

            foreach (var (bonusType, skills) in statusEffect.StatGroup.CraftSkillBonuses)
            {
                foreach (var (skill, value) in skills)
                {
                    StatGroup.CraftSkillBonuses[bonusType].TryGetValue(skill, out var current);
                    StatGroup.CraftSkillBonuses[bonusType][skill] = current + value;
                }
            }

            _allActiveEffects.Add(statusEffect);

            if (statusEffect.ActivationType == StatusEffectActivationType.Tick)
            {
                _tickEffects.Add(statusEffect);
            }
            else if (statusEffect.ActivationType == StatusEffectActivationType.OnHit)
            {
                _onHitEffects.Add(statusEffect);
            }

            if (!_effectsBySourceType.ContainsKey(statusEffect.SourceType))
                _effectsBySourceType[statusEffect.SourceType] = new HashSet<IStatusEffect>();
            _effectsBySourceType[statusEffect.SourceType].Add(statusEffect);
        }

        public void Remove(IStatusEffect statusEffect)
        {
            foreach (var (type, value) in statusEffect.StatGroup.Stats)
            {
                if (StatGroup.Stats.TryGetValue(type, out var current))
                    StatGroup.Stats[type] = current - value;
            }

            foreach (var (type, value) in statusEffect.StatGroup.Resists)
            {
                if (StatGroup.Resists.TryGetValue(type, out var current))
                    StatGroup.Resists[type] = current - value;
            }

            foreach (var (type, value) in statusEffect.StatGroup.Abilities)
            {
                if (StatGroup.Abilities.TryGetValue(type, out var current))
                    StatGroup.Abilities[type] = current - value;
            }

            foreach (var (bonusType, skills) in statusEffect.StatGroup.CraftSkillBonuses)
            {
                foreach (var (skill, value) in skills)
                {
                    if (StatGroup.CraftSkillBonuses[bonusType].TryGetValue(skill, out var current))
                        StatGroup.CraftSkillBonuses[bonusType][skill] = current - value;
                }
            }

            _allActiveEffects.Remove(statusEffect);
            if (_tickEffects.Contains(statusEffect))
                _tickEffects.Remove(statusEffect);
            if (_onHitEffects.Contains(statusEffect))
                _onHitEffects.Remove(statusEffect);

            if (_effectsBySourceType.ContainsKey(statusEffect.SourceType) &&
                _effectsBySourceType[statusEffect.SourceType].Contains(statusEffect))
                _effectsBySourceType[statusEffect.SourceType].Remove(statusEffect);
        }

        public HashSet<IStatusEffect> GetAllEffects()
        {
            return _allActiveEffects.ToHashSet();
        }

        public HashSet<IStatusEffect> GetAllTickEffects()
        {
            return _tickEffects.ToHashSet();
        }

        public HashSet<IStatusEffect> GetAllOnHitEffects()
        {
            return _onHitEffects.ToHashSet();
        }

        public HashSet<IStatusEffect> GetAllBySourceType(StatusEffectSourceType sourceType)
        {
            if (!_effectsBySourceType.ContainsKey(sourceType))
                return new HashSet<IStatusEffect>();

            return _effectsBySourceType[sourceType].ToHashSet();
        }

        public bool HasEffect(Type effectType)
        {
            return _allActiveEffects.Any(x => x.GetType() == effectType);
        }

        public IStatusEffect GetEffect(Type effectType)
        {
            return _allActiveEffects.FirstOrDefault(x => x.GetType() == effectType);
        }

        public T GetEffect<T>()
            where T : class, IStatusEffect
        {
            return _allActiveEffects.OfType<T>().FirstOrDefault();
        }

        public CreatureStatusEffect()
        {
            StatGroup = new StatGroup();
        }
    }
}
