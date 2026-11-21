using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The critical rate and positional rate modifiers of <c>Combat</c>: the auto-attack and target-status critical rate,
    /// the auto-attack cycle critical tracker (the per-attacker cycle count is this instance's own table), and the side
    /// and back attack adjustments to damage, hit chance, critical rate and evasion.
    /// </summary>
    public class CombatRateModifiersService : ICombatRateModifiersService
    {
        private readonly Dictionary<uint, int> _autoAttackCycleCriticalCounts = new();

        private readonly ICombatQueriesService _queries;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IPlayerFeedbackService _playerFeedback;

        public CombatRateModifiersService(
            ICombatQueriesService queries,
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier,
            IPlayerFeedbackService playerFeedback)
        {
            _queries = queries;
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
            _playerFeedback = playerFeedback;
        }

        public int GetAutoAttackCriticalRateAdjustment(
            uint attacker,
            uint defender,
            int cycleCriticalRateAdjustment)
        {
            return GetTargetStatusCriticalRateAdjustment(attacker, defender) +
                   cycleCriticalRateAdjustment;
        }

        public int ApplySideAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !_queries.IsMatchingSideAttack(attacker, defender, skillType))
                return damage;

            var adjustment = _stat.GetStatAdjustment(attacker, StatType.SideAttackDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        public int GetSideAttackHitChanceAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return _queries.IsMatchingSideAttack(attacker, defender, skillType)
                ? _stat.GetStatAdjustment(attacker, StatType.SideAttackHitChancePercentAdjustment)
                : 0;
        }

        public int GetSideAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return _queries.IsMatchingSideAttack(attacker, defender, skillType)
                ? _stat.GetStatAdjustment(attacker, StatType.SideAttackCriticalRatePercentAdjustment)
                : 0;
        }

        public int ApplyBackAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !_queries.IsMatchingBackAttack(attacker, defender, skillType))
                return damage;

            ApplyBackAttackExposed(attacker, defender);

            var adjustment = _stat.GetStatAdjustment(attacker, StatType.BackAttackDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        // A primed back attack (Ghost Protocol) inflicts Exposed on the landed hit. Both halves of
        // the primer are consumed together so the rider fires exactly once per priming.
        private void ApplyBackAttackExposed(uint attacker, uint defender)
        {
            var exposedPercent = _stat.GetStatAdjustment(attacker, StatType.BackAttackExposedPercent);
            var exposedDuration = _stat.GetStatAdjustment(attacker, StatType.BackAttackExposedDurationSeconds);
            if (exposedPercent <= 0 || exposedDuration <= 0)
                return;

            _temporaryStatModifier.Consume(attacker, StatType.BackAttackExposedPercent);
            _temporaryStatModifier.Consume(attacker, StatType.BackAttackExposedDurationSeconds);

            // BackAttackExposedPercent is a reduction magnitude (declared BeneficialWhenPositive and
            // guarded above as positive), but ExposedStatusEffect writes its argument straight into
            // DefensePercentAdjustment. Passing the magnitude unchanged granted the target +20%
            // Defense instead of taking it away, so it must be negated here.
            _statusEffectApplication.ApplyStatusEffect(
                attacker,
                defender,
                new ExposedStatusEffect(-exposedPercent),
                exposedDuration,
                CombatDamageType.Physical);
        }

        public int GetBackAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return _queries.IsMatchingBackAttack(attacker, defender, skillType)
                ? _stat.GetStatAdjustment(attacker, StatType.BackAttackCriticalRatePercentAdjustment)
                : 0;
        }

        public int ApplySideAttackEvasionIgnore(uint attacker, uint defender, SkillType skillType, int evasion)
        {
            if (evasion <= 0 || !_queries.IsMatchingSideAttack(attacker, defender, skillType))
                return evasion;

            var chance = _stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChance);
            if (chance <= 0)
                return evasion;

            var scalingAbility = GetAbilityTypeFromStatPlusOne(
                _stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChanceScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                chance += Math.Max(0, GetAbilityScore(attacker, scalingAbility));
            }

            var chanceMaximum = _stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChanceMaximum);
            if (chanceMaximum > 0)
            {
                chance = Math.Min(chance, chanceMaximum);
            }

            var ignorePercent = _stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnorePercent);
            if (ignorePercent <= 0 || Random.D100(1) > chance)
                return evasion;

            return Math.Max(0, evasion - (int)Math.Ceiling(evasion * (Math.Min(100, ignorePercent) / 100f)));
        }

        public int PrepareAutoAttackCycleCriticalRate(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || !IsWeaponSkillType(skillType))
                return 0;

            var requiredCount = _stat.GetStatAdjustment(attacker, StatType.RangedAutoAttackCycleCriticalRateRequiredCount);
            var criticalRate = _stat.GetStatAdjustment(attacker, StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment);
            if (requiredCount <= 0 || criticalRate <= 0)
            {
                ClearAutoAttackCycleCriticalRateTracker(attacker);
                return 0;
            }

            _autoAttackCycleCriticalCounts.TryGetValue(attacker, out var count);
            count++;
            var trackerIcon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.RangedAutoAttackCycleCriticalRateTrackerEffectIconType));
            _statusEffect.RemoveStatusEffect(attacker, typeof(AttackCycleTrackerStatusEffect), false);
            if (trackerIcon != EffectIconType.Invalid)
            {
                _statusEffectApplication.ApplyStatusEffect(
                    attacker,
                    attacker,
                    new AttackCycleTrackerStatusEffect(
                        $"Attack cycle: {Math.Min(count, requiredCount)}/{requiredCount}",
                        trackerIcon),
                    count >= requiredCount ? 3f : 0f);
            }
            if (count < requiredCount)
            {
                _autoAttackCycleCriticalCounts[attacker] = count;
                return 0;
            }

            _autoAttackCycleCriticalCounts[attacker] = 0;
            _playerFeedback.ShowDiagnosticFloatingText(
                ColorToken.Combat($"Attack +{criticalRate}% Critical Rate"),
                attacker,
                false);
            return criticalRate;
        }

        public void ClearAutoAttackCycleCriticalRateTracker(uint creature)
        {
            _autoAttackCycleCriticalCounts.Remove(creature);
            _statusEffect.RemoveStatusEffect(creature, typeof(AttackCycleTrackerStatusEffect), false);
        }

        public int GetTargetStatusCriticalRateAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var category = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.TargetStatusCriticalRateStatusCategory));
            var adjustment = _stat.GetStatAdjustment(attacker, StatType.TargetStatusCriticalRatePercentAdjustment);
            if (category == 0 || adjustment == 0 || !_queries.TargetHasAnyStatusEffectCategory(defender, category))
                return 0;

            return adjustment;
        }

        /// <summary>Forgets the creature's auto-attack cycle count, without touching the tracker status effect.</summary>
        public void ForgetAutoAttackCycleCriticalRateCount(uint creature)
        {
            _autoAttackCycleCriticalCounts.Remove(creature);
        }
    }
}
