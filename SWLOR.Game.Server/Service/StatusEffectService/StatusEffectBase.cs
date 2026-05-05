using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public abstract class StatusEffectBase: IStatusEffect
    {
        private bool _isPermanent;
        private int _durationTicks;
        private DateTime _lastRun;

        public string Id { get; }
        public uint Source { get; private set; }
        public virtual StatusEffectActivationType ActivationType => StatusEffectActivationType.Tick;
        public virtual StatusEffectSourceType SourceType => StatusEffectSourceType.Normal;
        public abstract string Name { get; }
        public abstract EffectIconType Icon { get; }
        public abstract StatusEffectStackType StackingType { get; }
        public bool IsFlaggedForRemoval { get; protected set; }
        public virtual bool SendsApplicationMessage => true;
        public virtual bool SendsWornOffMessage => true;
        public abstract float Frequency { get; }
        public virtual bool IsRemovedOnJobChange => true;
        public StatGroup StatGroup { get; }
        public virtual List<Type> MorePowerfulEffectTypes { get; }
        public virtual List<Type> LessPowerfulEffectTypes { get; }

        protected StatusEffectBase()
        {
            Id = Guid.NewGuid().ToString();
            StatGroup = new StatGroup();
            MorePowerfulEffectTypes = new List<Type>();
            LessPowerfulEffectTypes = new List<Type>();
        }

        public virtual string CanApply(uint creature) { return string.Empty; }

        protected virtual void Apply(uint creature, int durationTicks) { }
        public void ApplyEffect(uint source, uint creature, int durationTicks)
        {
            if (durationTicks < 0)
                _isPermanent = true;

            _lastRun = DateTime.UtcNow;
            _durationTicks = durationTicks;
            Source = source;
            Apply(creature, durationTicks);
        }

        protected virtual void Remove(uint creature) { }
        public void RemoveEffect(uint creature)
        {
            Remove(creature);
        }

        protected virtual void Tick(uint creature) { }
        public void TickEffect(uint creature)
        {
            var currentTime = DateTime.UtcNow;
            if ((currentTime - _lastRun).TotalSeconds < Frequency)
            {
                return;
            }

            _lastRun = currentTime;

            // Reduce duration ticks and flag for removal if expired
            if (!_isPermanent && --_durationTicks <= 0)
            {
                IsFlaggedForRemoval = true;
            }

            Tick(creature);
        }

        protected virtual void OnHit(uint creature, uint target, int damage) { }
        public void OnHitEffect(uint creature, uint target, int damage)
        {
            OnHit(creature, target, damage);
        }
    }
}
