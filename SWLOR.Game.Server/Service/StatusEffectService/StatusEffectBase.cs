using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public abstract class StatusEffectBase : IStatusEffect
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
        public virtual StatusEffectCategory Categories => StatusEffectCategory.None;
        public virtual StatusEffectStackType StackingType => StatusEffectStackType.Disabled;
        public bool IsFlaggedForRemoval { get; protected set; }
        public virtual bool SendsApplicationMessage => true;
        public virtual bool SendsWornOffMessage => true;
        public virtual StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.None;
        public virtual ResistanceType ResistanceType => ResistanceType.Invalid;
        public ResistanceType AppliedResistanceType { get; private set; }
        public virtual float Frequency => 1f;
        public int DurationTicks => _durationTicks;
        public virtual bool PersistsOnLogout => true;
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
            AppliedResistanceType = ResistanceType.Invalid;
        }

        public virtual string CanApply(uint creature) { return string.Empty; }

        public bool HasCleanseType(StatusEffectCleanseType cleanseType)
        {
            return (CleanseTypes & cleanseType) == cleanseType;
        }

        public virtual IStatusEffect Clone()
        {
            return (IStatusEffect)Activator.CreateInstance(GetType());
        }

        protected virtual void Apply(uint creature, int durationTicks) { }
        public void AssignResistanceType(ResistanceType type)
        {
            AppliedResistanceType = type;
        }

        public void ApplyEffect(uint source, uint creature, int durationTicks)
        {
            if (durationTicks < 0)
                _isPermanent = true;

            _lastRun = DateTime.UtcNow;
            _durationTicks = durationTicks;
            Source = source;
            Apply(creature, durationTicks);
        }

        public void ReassignSource(uint source)
        {
            Source = source;
        }

        protected virtual void Reapply(uint creature) { }
        public void ReapplyEffect(uint creature)
        {
            Reapply(creature);
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

        public void ReconcileElapsedTime(DateTime currentTime)
        {
            if (_isPermanent || IsFlaggedForRemoval)
            {
                _lastRun = currentTime;
                return;
            }

            var frequency = Math.Max(1f, Frequency);
            var elapsedSeconds = (currentTime - _lastRun).TotalSeconds;
            var elapsedTicks = (int)Math.Floor(elapsedSeconds / frequency);

            if (elapsedTicks <= 0)
                return;

            _durationTicks -= elapsedTicks;
            _lastRun = _lastRun.AddSeconds(elapsedTicks * frequency);

            if (_durationTicks > 0)
                return;

            _durationTicks = 0;
            _lastRun = currentTime;
            IsFlaggedForRemoval = true;
        }

        protected virtual void OnHit(uint creature, uint target, int damage) { }
        public void OnHitEffect(uint creature, uint target, int damage)
        {
            OnHit(creature, target, damage);
        }

        protected virtual void OnDamageDealt(uint attacker, uint defender, int damage, CombatDamageType damageType) { }
        public void OnDamageDealtEffect(uint attacker, uint defender, int damage, CombatDamageType damageType)
        {
            OnDamageDealt(attacker, defender, damage, damageType);
        }

        protected virtual void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType) { }
        public void OnDamageTakenEffect(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            OnDamageTaken(defender, attacker, damage, damageType);
        }

        protected int PercentOfDamage(int damage, int percent)
        {
            return Math.Max(1, (int)Math.Ceiling(damage * (percent / 100f)));
        }

        protected int GetPositiveAbilityModifier(AbilityType abilityType, uint creature)
        {
            return Math.Max(0, GetAbilityModifier(abilityType, creature));
        }

        protected float GetDurationSeconds(int durationTicks)
        {
            return durationTicks < 0
                ? 0f
                : Math.Max(0.1f, durationTicks * Frequency);
        }
    }
}
