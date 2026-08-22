using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public abstract class StatusEffectBase : IStatusEffect
    {
        private bool _isPermanent;
        private int _durationTicks;
        private DateTime _lastRun;
        private readonly HashSet<string> _nativeEffectTagSuffixes = new();

        public string Id { get; }
        public uint Source { get; private set; }
        public virtual StatusEffectActivationType ActivationType => StatusEffectActivationType.Tick;
        public virtual StatusEffectSourceType SourceType => StatusEffectSourceType.Normal;
        public abstract string Name { get; }
        public abstract EffectIconType Icon { get; }
        public virtual StatusEffectCategory Categories => StatusEffectCategory.None;
        public virtual StatusEffectStackType StackingType => StatusEffectStackType.Disabled;
        public bool IsFlaggedForRemoval { get; protected set; }
        public bool WasNaturallyExpired { get; private set; }
        public float SecondsSinceNaturalExpiration { get; private set; }
        public virtual bool SendsApplicationMessage => true;
        public virtual bool SendsWornOffMessage => true;
        public virtual StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.None;
        public virtual ResistanceType ResistanceType => ResistanceType.Invalid;
        public ResistanceType AppliedResistanceType { get; private set; }
        public virtual float Frequency => 1f;
        public int DurationTicks => _durationTicks;
        public virtual bool PersistsOnLogout => true;
        public virtual bool IsRemovedOnJobChange => true;
        protected bool IsBeingReplaced { get; private set; }
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

        public void ExtendDurationTicks(int ticks)
        {
            if (_isPermanent || IsFlaggedForRemoval || ticks <= 0)
                return;

            _durationTicks += ticks;
        }

        public void SetDurationTicks(int ticks)
        {
            if (_isPermanent || IsFlaggedForRemoval || ticks <= 0)
                return;

            _durationTicks = ticks;
        }

        protected virtual void Reapply(uint creature) { }
        public void ReapplyEffect(uint creature)
        {
            Reapply(creature);
        }

        protected virtual void Remove(uint creature) { }
        public void RemoveEffect(uint creature, bool isReplacement = false)
        {
            IsBeingReplaced = isReplacement;
            try
            {
                Remove(creature);
                RemoveNativeEffects(creature);
            }
            finally
            {
                IsBeingReplaced = false;
            }
        }

        public void RemoveNativeEffects(uint creature)
        {
            foreach (var tagSuffix in _nativeEffectTagSuffixes)
            {
                RemoveEffectByTag(creature, GetNativeEffectTag(tagSuffix));
            }
        }

        protected virtual void Tick(uint creature) { }
        public void TickEffect(uint creature)
        {
            var currentTime = DateTime.UtcNow;
            if ((currentTime - _lastRun).TotalSeconds < Frequency)
            {
                return;
            }

            // Preserve the logical cadence instead of anchoring the next tick to a slightly late
            // engine callback. Resetting to currentTime accumulates scheduler jitter and can turn a
            // 3-second effect into a 4-second cadence, dropping the final tick of a fixed-duration HoT.
            _lastRun = _lastRun.AddSeconds(Math.Max(1f, Frequency));

            // Reduce duration ticks and flag for removal if expired
            if (!_isPermanent && --_durationTicks <= 0)
            {
                IsFlaggedForRemoval = true;
                WasNaturallyExpired = true;
                SecondsSinceNaturalExpiration = 0f;
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
            var secondsUntilExpiration = _durationTicks * frequency;

            if (elapsedTicks <= 0)
                return;

            _durationTicks -= elapsedTicks;
            _lastRun = _lastRun.AddSeconds(elapsedTicks * frequency);

            if (_durationTicks > 0)
                return;

            SecondsSinceNaturalExpiration = (float)Math.Max(0d, elapsedSeconds - secondsUntilExpiration);
            _durationTicks = 0;
            _lastRun = currentTime;
            IsFlaggedForRemoval = true;
            WasNaturallyExpired = true;
        }

        protected virtual void OnHit(uint creature, uint target, int damage) { }
        public void OnHitEffect(uint creature, uint target, int damage)
        {
            OnHit(creature, target, damage);
        }

        protected virtual void OnDamageDealt(uint attacker, uint defender, int damage, CombatDamageType damageType) { }
        protected virtual void OnDamageDealt(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType != CombatDamageDeliveryType.Direct)
                return;

            OnDamageDealt(attacker, defender, damage, damageType);
        }
        public void OnDamageDealtEffect(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
        {
            OnDamageDealt(attacker, defender, damage, damageType, deliveryType);
        }

        protected virtual void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType) { }
        protected virtual void OnDamageTaken(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType != CombatDamageDeliveryType.Direct)
                return;

            OnDamageTaken(defender, attacker, damage, damageType);
        }
        public void OnDamageTakenEffect(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
        {
            OnDamageTaken(defender, attacker, damage, damageType, deliveryType);
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

        protected Effect TagNativeEffect(Effect effect, string tagSuffix = null)
        {
            tagSuffix = string.IsNullOrWhiteSpace(tagSuffix)
                ? GetType().Name
                : tagSuffix;

            _nativeEffectTagSuffixes.Add(tagSuffix);
            return TagEffect(effect, GetNativeEffectTag(tagSuffix));
        }

        private string GetNativeEffectTag(string tagSuffix)
        {
            return $"{Id}:Native:{tagSuffix}";
        }
    }
}
