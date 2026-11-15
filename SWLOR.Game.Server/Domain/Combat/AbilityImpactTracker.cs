using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// One ability impact while it resolves: its pending bonuses, its summary, its shape and targets and the damage it queues
    /// for its end. Moved from <c>AbilityService</c> (where it was the nested <c>TrackedAbilityImpact</c>) with
    /// <see cref="AbilityImpactStore"/>; the calls it made into the service now come from its callers (the stat service for
    /// the stat-source cache, the recorder for a queued effect), and consuming the creature's status-applied bonus and
    /// flushing the queued damage moved to the ability impact code that calls them.
    /// </summary>
    public sealed class AbilityImpactTracker
    {
        private readonly HashSet<uint> _impactedTargets = new();
        private readonly List<PendingDamageEffect> _pendingDamageEffects = new();
        private AbilityImpactSequence _sequence;
        private Dictionary<StatType, IReadOnlyList<StatAdjustmentSource>> _statSources;

        public AbilityDetail Ability { get; }
        public AbilityImpactVisualEffects VisualEffects { get; }
        public int? TriggeringWeaponDamage
        {
            get => Summary.TriggeringWeaponDamage;
            set => Summary.TriggeringWeaponDamage = value;
        }
        public CombatDamageType? TriggeringWeaponDamageType
        {
            get => Summary.TriggeringWeaponDamageType;
            set => Summary.TriggeringWeaponDamageType = value;
        }
        // Delayed shapes share the original tracker until a rider actually needs cast state.
        public AbilityImpactTracker SequenceOwner { get; set; }
        public AbilityImpactSequence Sequence => _sequence ??= SequenceOwner?.Sequence ?? new AbilityImpactSequence();
        public AbilityImpactSummary Summary { get; }
        public bool CountsAsAttackAttempt { get; }
        public IReadOnlyList<TelegraphGeometry> ActivationAreaTelegraphs { get; }
        public int NextAbilityDamageBonus { get; private set; }
        public int NextAbilityCriticalRatePercentAdjustment { get; private set; }
        public int NextAbilityCriticalDamagePercentAdjustment { get; private set; }
        public int NextAbilityDefenseIgnorePercentAdjustment { get; private set; }
        public int NextAttackEnmityBonus { get; private set; }
        public int StatusAppliedNextAttackDamageBonus { get; private set; }
        public Action ResolveDamageBonuses { get; set; }
        public bool DarkForceConversionApplied { get; set; }
        private bool _statusAppliedNextAttackDamageBonusConsumed;
        private bool _repeatedDamageBonusesConsumed;

        public void CopyRepeatedDamageBonusesFrom(AbilityImpactTracker source)
        {
            if (source._repeatedDamageBonusesConsumed)
                return;

            AddDamageBonuses(
                source.NextAbilityDamageBonus - source.StatusAppliedNextAttackDamageBonus,
                source.NextAbilityCriticalRatePercentAdjustment,
                source.NextAbilityDefenseIgnorePercentAdjustment,
                source.NextAttackEnmityBonus,
                0,
                source.NextAbilityCriticalDamagePercentAdjustment);
        }

        public void CompleteRepeatedDamageBonusImpact(bool impactedTarget)
        {
            _repeatedDamageBonusesConsumed |= impactedTarget;
        }

        public Action<AbilityImpactTracker> CapturePartitionedDamageBonuses()
        {
            var damage = NextAbilityDamageBonus - StatusAppliedNextAttackDamageBonus;
            var criticalRate = NextAbilityCriticalRatePercentAdjustment;
            var defenseIgnore = NextAbilityDefenseIgnorePercentAdjustment;
            var enmity = NextAttackEnmityBonus;
            var criticalDamage = NextAbilityCriticalDamagePercentAdjustment;
            return target => target.AddDamageBonuses(damage, criticalRate, defenseIgnore, enmity, 0, criticalDamage);
        }

        public void EnsureDamageBonuses()
        {
            var resolve = ResolveDamageBonuses;
            ResolveDamageBonuses = null;
            resolve?.Invoke();
        }

        public void AddDamageBonuses(
            int damage, int criticalRate, int defenseIgnore, int enmity,
            int statusAppliedDamage, int criticalDamage)
        {
            NextAbilityDamageBonus += damage;
            NextAbilityCriticalRatePercentAdjustment += criticalRate;
            NextAbilityDefenseIgnorePercentAdjustment += defenseIgnore;
            NextAttackEnmityBonus += enmity;
            StatusAppliedNextAttackDamageBonus += statusAppliedDamage;
            NextAbilityCriticalDamagePercentAdjustment += criticalDamage;
        }

        /// <summary>
        /// Initializes per-impact bonuses, summary classification, and the activation
        /// geometry that can suppress redundant impact markers.
        /// </summary>
        public AbilityImpactTracker(
            AbilityDetail ability,
            int nextAbilityDamageBonus,
            int nextAbilityCriticalRatePercentAdjustment,
            int nextAbilityDefenseIgnorePercentAdjustment,
            int nextAttackEnmityBonus,
            int statusAppliedNextAttackDamageBonus,
            bool countsAsAttackAttempt,
            int nextAbilityCriticalDamagePercentAdjustment,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs,
            AbilityImpactSequence sequence)
        {
            Ability = ability;
            VisualEffects = new AbilityImpactVisualEffects(ability.SuccessfulImpactVisualEffect);
            _sequence = sequence;
            NextAbilityDamageBonus = nextAbilityDamageBonus;
            NextAbilityCriticalRatePercentAdjustment = nextAbilityCriticalRatePercentAdjustment;
            NextAbilityCriticalDamagePercentAdjustment = nextAbilityCriticalDamagePercentAdjustment;
            NextAbilityDefenseIgnorePercentAdjustment = nextAbilityDefenseIgnorePercentAdjustment;
            NextAttackEnmityBonus = nextAttackEnmityBonus;
            StatusAppliedNextAttackDamageBonus = statusAppliedNextAttackDamageBonus;
            CountsAsAttackAttempt = countsAsAttackAttempt;
            ActivationAreaTelegraphs = activationAreaTelegraphs;
            Summary = new AbilityImpactSummary
            {
                Ability = ability,
                SkillType = ability.SkillType,
                IsAreaAbility = ability.IsAreaAbility,
                IsSingleTargetAbility = ability.IsSingleTargetAbility,
                ImpactedTargets = _impactedTargets
            };
        }

        public IReadOnlyList<StatAdjustmentSource> GetStatSources(uint activator, StatType payload, IStatService stat)
        {
            if (_statSources != null && _statSources.TryGetValue(payload, out var cached))
                return cached;

            var sources = stat.GetStatSources(activator, payload);
            if (sources.Count > 0)
            {
                _statSources ??= new Dictionary<StatType, IReadOnlyList<StatAdjustmentSource>>();
                _statSources.Add(payload, sources);
            }
            return sources;
        }

        /// <summary>Whether the status-applied bonus of this impact was consumed (once per impact).</summary>
        public bool StatusAppliedNextAttackDamageBonusConsumed => _statusAppliedNextAttackDamageBonusConsumed;

        /// <summary>The impact's half of consuming the status-applied bonus: the bonus leaves the next ability damage, once.</summary>
        public void CompleteStatusAppliedNextAttackDamageBonusConsumption()
        {
            NextAbilityDamageBonus -= StatusAppliedNextAttackDamageBonus;
            _statusAppliedNextAttackDamageBonusConsumed = true;
        }

        public void AddDefenseIgnorePercentAdjustment(int adjustment)
        {
            NextAbilityDefenseIgnorePercentAdjustment += adjustment;
        }

        public void RecordShape(SkillType skillType, bool isArea)
        {
            if (Summary.SkillType == SkillType.Invalid && skillType != SkillType.Invalid)
            {
                Summary.SkillType = skillType;
            }

            if (isArea)
            {
                Summary.IsAreaAbility = true;
                Summary.IsSingleTargetAbility = false;
            }
            else if (!Summary.IsAreaAbility)
            {
                Summary.IsSingleTargetAbility = true;
            }
        }

        public void RecordTarget(uint target)
        {
            _impactedTargets.Add(target);
            Summary.ImpactedTargetCount = _impactedTargets.Count;

            if (_impactedTargets.Count > 1)
            {
                Summary.IsAreaAbility = true;
                Summary.IsSingleTargetAbility = false;
            }
        }

        public void RecordCriticalHit()
        {
            Summary.CriticalHitCount++;
        }

        public void QueueDamageEffect(uint target, int damage, DamageType damageType, ICombatRecorderService recorder)
        {
            QueueDamageEffect(target, damage, damageType, CombatDamageType.Invalid, recorder);
        }

        public int GetPendingDamage(uint target)
        {
            return _pendingDamageEffects.Where(effect => effect.Target == target).Sum(effect => effect.Damage);
        }

        public void QueueDirectDamageEffect(
            uint target,
            int damage,
            DamageType damageType,
            CombatDamageType combatDamageType,
            ICombatRecorderService recorder)
        {
            QueueDamageEffect(target, damage, damageType, combatDamageType, recorder);
        }

        private void QueueDamageEffect(
            uint target,
            int damage,
            DamageType damageType,
            CombatDamageType combatDamageType,
            ICombatRecorderService recorder)
        {
            if (!GetIsObjectValid(target) || damage <= 0)
                return;

            Summary.AttributedDamage += damage;
            _pendingDamageEffects.Add(new PendingDamageEffect(
                target,
                damage,
                damageType,
                combatDamageType,
                recorder.CaptureScope()));
        }

        /// <summary>The damage effects queued for the end of the impact, in order.</summary>
        public List<PendingDamageEffect> PendingDamageEffects => _pendingDamageEffects;
    }
}
