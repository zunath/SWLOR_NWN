using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public abstract class StatusEffectBase : IStatusEffect
    {
        private bool _isPermanent;
        private int _durationTicks;
        private DateTime _lastRun;
        private float _durationAdjustmentSeconds;
        private readonly HashSet<string> _nativeEffectTagSuffixes = new();

        public string Id { get; }
        public uint Source { get; private set; }
        /// <inheritdoc />
        public AbilityDetail OriginatingAbility { get; set; }
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
        public DateTime LastTickTime => _lastRun;
        public virtual bool PreservesTickScheduleOnRefresh => false;
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

        public virtual bool CanRefreshInPlace(IStatusEffect active) => false;

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

        public void ApplyEffect(uint source, uint creature, int durationTicks, float durationSeconds = 0f, DateTime? tickAnchor = null)
        {
            RestartDuration(durationTicks, durationSeconds, tickAnchor);
            Source = source;
            if (CombatRecorder.IsRecording)
                CombatRecorder.RecordStatusApplied(source, creature, this, GetRemainingDurationSeconds(Clock.UtcNow), _isPermanent, false);

            using var recorderAttribution = CombatRecorder.BeginStatusEffectAttribution(source, creature, this);
            Apply(creature, durationTicks);
        }

        /// <summary>
        /// Starts the duration and tick schedule over exactly as a fresh application does, without
        /// running Apply.
        /// </summary>
        public void RestartDuration(int durationTicks, float durationSeconds = 0f, DateTime? tickAnchor = null)
        {
            _isPermanent = durationTicks < 0;

            _lastRun = tickAnchor ?? Clock.UtcNow;
            _durationTicks = durationTicks;
            _durationAdjustmentSeconds = 0f;
            if (!_isPermanent && durationSeconds > 0f)
            {
                _durationAdjustmentSeconds = durationSeconds - durationTicks * Math.Max(1f, Frequency);
                if (tickAnchor.HasValue)
                    SetDurationSeconds(durationSeconds);
            }
        }

        /// <summary>Updates a reconnected creature's handle without changing the granting ability.</summary>
        public void ReassignSource(uint source)
        {
            Source = source;
        }

        public float GetRemainingDurationSeconds(DateTime currentTime)
        {
            if (_isPermanent)
                return -1f;
            return (float)Math.Max(0d, _durationTicks * Math.Max(1f, Frequency) + _durationAdjustmentSeconds -
                Math.Max(0d, (currentTime - _lastRun).TotalSeconds));
        }

        public void ExtendDurationTicks(int ticks)
        {
            if (_isPermanent || IsFlaggedForRemoval || ticks <= 0 ||
                (Categories & StatusEffectCategory.HardCrowdControl) != 0)
                return;

            _durationTicks += ticks;
        }

        public void SetDurationTicks(int ticks)
        {
            if (_isPermanent || IsFlaggedForRemoval || ticks <= 0)
                return;

            _durationTicks = (Categories & StatusEffectCategory.HardCrowdControl) != 0
                ? Math.Min(_durationTicks, ticks)
                : ticks;
            _durationAdjustmentSeconds = 0f;
        }

        public void SetDurationSeconds(float seconds)
        {
            if (_isPermanent || IsFlaggedForRemoval || seconds <= 0f)
                return;

            if ((Categories & StatusEffectCategory.HardCrowdControl) != 0)
                seconds = Math.Min(seconds, GetRemainingDurationSeconds(Clock.UtcNow));

            var durationFromTickAnchor = seconds + (float)Math.Max(0d, (Clock.UtcNow - _lastRun).TotalSeconds);
            var frequency = Math.Max(1f, Frequency);
            _durationTicks = Math.Max(1, (int)Math.Ceiling(durationFromTickAnchor / frequency));
            _durationAdjustmentSeconds = durationFromTickAnchor - _durationTicks * frequency;
        }

        protected virtual void Reapply(uint creature) { }
        public void ReapplyEffect(uint creature)
        {
            Reapply(creature);
        }

        protected virtual void Remove(uint creature) { }
        public void RemoveEffect(uint creature, bool isReplacement = false)
        {
            if (CombatRecorder.IsRecording)
                CombatRecorder.RecordStatusRemoved(creature, this, isReplacement);

            IsBeingReplaced = isReplacement;
            using var recorderAttribution = CombatRecorder.BeginStatusEffectAttribution(Source, creature, this);
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
            var currentTime = Clock.UtcNow;
            var frequency = Math.Max(1f, Frequency);
            var elapsedSeconds = (currentTime - _lastRun).TotalSeconds;
            var secondsUntilExpiration = _durationTicks * frequency + _durationAdjustmentSeconds;
            // A fractional final interval extends the debuff, but cannot grant a premature damage tick.
            if (!_isPermanent && secondsUntilExpiration < frequency && elapsedSeconds >= secondsUntilExpiration)
            {
                _durationTicks = 0;
                IsFlaggedForRemoval = true;
                WasNaturallyExpired = true;
                SecondsSinceNaturalExpiration = (float)Math.Max(0d, elapsedSeconds - secondsUntilExpiration);
                return;
            }
            if (elapsedSeconds < frequency)
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

            using var recorderAttribution = CombatRecorder.BeginStatusEffectAttribution(Source, creature, this);
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
            var secondsUntilExpiration = _durationTicks * frequency + _durationAdjustmentSeconds;

            if (elapsedSeconds >= secondsUntilExpiration)
            {
                SecondsSinceNaturalExpiration = (float)Math.Max(0d, elapsedSeconds - secondsUntilExpiration);
                _durationTicks = 0;
                _lastRun = currentTime;
                IsFlaggedForRemoval = true;
                WasNaturallyExpired = true;
                return;
            }

            if (elapsedTicks <= 0)
                return;

            _durationTicks -= elapsedTicks;
            _lastRun = _lastRun.AddSeconds(elapsedTicks * frequency);

        }

        protected virtual void OnHit(uint creature, uint target, int damage) { }
        public void OnHitEffect(uint creature, uint target, int damage)
        {
            using var recorderAttribution = CombatRecorder.BeginStatusEffectAttribution(Source, creature, this);
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
            using var recorderAttribution = CombatRecorder.BeginStatusEffectAttribution(Source, attacker, this);
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
            using var recorderAttribution = CombatRecorder.BeginStatusEffectAttribution(Source, defender, this);
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
                : Math.Max(0.1f, GetRemainingDurationSeconds(Clock.UtcNow));
        }

        protected Effect TagNativeEffect(Effect effect, string tagSuffix = null)
        {
            tagSuffix = string.IsNullOrWhiteSpace(tagSuffix)
                ? GetType().Name
                : tagSuffix;

            _nativeEffectTagSuffixes.Add(tagSuffix);
            return TagEffect(HideEffectIcon(effect), GetNativeEffectTag(tagSuffix));
        }

        private string GetNativeEffectTag(string tagSuffix)
        {
            return $"{Id}:Native:{tagSuffix}";
        }
    }
}
