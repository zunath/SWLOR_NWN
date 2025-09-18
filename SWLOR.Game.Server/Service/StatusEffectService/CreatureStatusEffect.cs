using System;
using System.Collections.Generic;
using System.Linq;

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
            foreach (var (type, value) in statusEffect.StatGroup.Stats)
            {
                StatGroup.Stats[type] += value;
            }

            foreach (var (type, value) in statusEffect.StatGroup.Resists)
            {
                StatGroup.Resists[type] += value;
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
                StatGroup.Stats[type] -= value;
            }

            foreach (var (type, value) in statusEffect.StatGroup.Resists)
            {
                StatGroup.Resists[type] -= value;
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

        public CreatureStatusEffect()
        {
            StatGroup = new StatGroup();
        }
    }
}
