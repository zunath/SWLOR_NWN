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

        public void Add(IStatusEffect statusEffect)
        {
            _allActiveEffects.Add(statusEffect);

            foreach (var (type, value) in statusEffect.StatGroup.Stats)
            {
                // Non-additive stats cannot be maintained as a running sum. Recompute them from
                // the active set so removing one effect reveals the next active value correctly.
                if (Stat.GetStatTypeAggregation(type) != StatTypeAggregation.Additive)
                {
                    RecomputeNonAdditiveStat(type);
                    continue;
                }

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
            _allActiveEffects.Remove(statusEffect);

            foreach (var (type, value) in statusEffect.StatGroup.Stats)
            {
                if (Stat.GetStatTypeAggregation(type) != StatTypeAggregation.Additive)
                {
                    RecomputeNonAdditiveStat(type);
                    continue;
                }

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

            if (_tickEffects.Contains(statusEffect))
                _tickEffects.Remove(statusEffect);
            if (_onHitEffects.Contains(statusEffect))
                _onHitEffects.Remove(statusEffect);

            if (_effectsBySourceType.ContainsKey(statusEffect.SourceType) &&
                _effectsBySourceType[statusEffect.SourceType].Contains(statusEffect))
                _effectsBySourceType[statusEffect.SourceType].Remove(statusEffect);
        }

        /// <summary>Consumes one stat payload while retaining each effect's other bonuses and lifetime.</summary>
        public void ConsumeStat(StatType type)
        {
            foreach (var effect in GetAllEffects())
            {
                if (!effect.StatGroup.Stats.TryGetValue(type, out var value) || value == 0)
                    continue;

                Remove(effect);
                effect.StatGroup.Stats[type] = 0;
                Add(effect);
            }
        }

        private void RecomputeNonAdditiveStat(StatType type)
        {
            var combined = 0;
            foreach (var effect in _allActiveEffects)
            {
                if (effect.StatGroup.Stats.TryGetValue(type, out var value))
                    combined = Stat.AggregateStatAdjustment(type, combined, value);
            }

            StatGroup.Stats[type] = combined;
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
