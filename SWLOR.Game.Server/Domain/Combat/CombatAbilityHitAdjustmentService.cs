using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability hit and critical adjustments of <c>Combat</c>: an ability's critical rate and critical roll, the hit and
    /// critical adjustments the skill and the target bring, the sundered target reads, the ranged defense reduction rider
    /// and the evaded Force ability effects.
    /// </summary>
    public class CombatAbilityHitAdjustmentService : ICombatAbilityHitAdjustmentService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatSuppressionService _suppression;
        private readonly ICombatDamageModifiersService _damageModifiers;
        private readonly ICombatRateModifiersService _rateModifiers;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;

        public CombatAbilityHitAdjustmentService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatSuppressionService suppression,
            ICombatDamageModifiersService damageModifiers,
            ICombatRateModifiersService rateModifiers,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier)
        {
            _queries = queries;
            _resources = resources;
            _suppression = suppression;
            _damageModifiers = damageModifiers;
            _rateModifiers = rateModifiers;
            _stat = stat;
            _statusEffect = statusEffect;
            _temporaryStatModifier = temporaryStatModifier;
        }

        public int CalculateAbilityCriticalRating(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID)
        {
            var criticalRate = GetAbilityCriticalRate(
                attacker,
                skillType,
                isAreaAbility,
                criticalRateAdjustment,
                defender);

            return criticalRate > 0 && Random.D100(1) <= criticalRate
                ? StandardCriticalRating
                : 0;
        }

        public int GetAbilityCriticalRate(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var totalAdjustment = criticalRateAdjustment;
            totalAdjustment += _damageModifiers.GetSkillCriticalRatePercentAdjustment(attacker, skillType);
            totalAdjustment += GetAbilityHitOrCriticalAdjustment(
                attacker,
                skillType,
                PerkType.Invalid,
                StatType.AbilityCriticalRatePercentAdjustmentSkillType,
                StatType.AbilityCriticalRatePercentAdjustmentPerkType,
                StatType.AbilityCriticalRatePercentAdjustmentSecondaryPerkType,
                StatType.AbilityCriticalRatePercentAdjustment,
                false,
                defender);

            if (isAreaAbility && skillType == SkillType.TwinBlade)
            {
                totalAdjustment += _stat.GetStatAdjustment(attacker, StatType.TwinBladeAreaAbilityCriticalRatePercentAdjustment);
            }

            if (skillType == SkillType.Throwing &&
                GetIsObjectValid(defender) &&
                (_statusEffect.HasStatusEffect(defender, typeof(DisorientedStatusEffect)) ||
                 _statusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding)))
            {
                totalAdjustment += _stat.GetStatAdjustment(attacker, StatType.ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment);
            }

            if (GetIsObjectValid(defender) && _queries.IsTargetNotFacingAttacker(attacker, defender))
            {
                totalAdjustment += _stat.GetStatAdjustment(attacker, StatType.CriticalRateAgainstTargetNotFacingAttackerPercentAdjustment);
            }

            totalAdjustment += GetCriticalRateAgainstSunderedTargetAdjustment(attacker, defender);
            totalAdjustment += _rateModifiers.GetTargetStatusCriticalRateAdjustment(attacker, defender);
            totalAdjustment += _rateModifiers.GetSideAttackCriticalRateAdjustment(attacker, defender, skillType);
            totalAdjustment += _rateModifiers.GetBackAttackCriticalRateAdjustment(attacker, defender, skillType);

            return CalculateAbilityCriticalChance(totalAdjustment);
        }

        public int GetAbilityHitOrCriticalAdjustment(
            uint creature,
            SkillType skillType,
            PerkType perkType,
            StatType skillTypeStat,
            StatType primaryPerkStat,
            StatType secondaryPerkStat,
            StatType adjustmentStat,
            bool includePerkTargeting,
            uint defender = OBJECT_INVALID)
        {
            // Each source carries its own skill selector alongside its own magnitude. Reading the
            // aggregated selector would sum unrelated skill ids (Force 5 + Force 5 becomes
            // Fabrication 10, Pistol 45 + Devices 33 becomes nothing at all), which silently
            // disables every contributing bonus. Resolve per source instead, and honour the
            // selectorless sources that are authored to affect every ability.
            var adjustment = _queries.GetSkillSelectedStatAdjustment(
                creature,
                skillType,
                skillTypeStat,
                adjustmentStat,
                unselectedSourcesApplyToEverySkill: true);

            // Long-range bonuses are independent of the generic skill-selector stats. This
            // allows a perk to affect every ranged ability without installing a second selector.
            if (adjustmentStat == StatType.AbilityHitChancePercentAdjustment)
                adjustment += _suppression.GetRangedAbilityLongRangeHitChanceAdjustment(creature, defender, skillType);
            else if (adjustmentStat == StatType.AbilityCriticalRatePercentAdjustment)
                adjustment += _suppression.GetRangedAbilityLongRangeCriticalRateAdjustment(creature, defender, skillType);

            if (includePerkTargeting)
            {
                adjustment += _queries.GetTargetedAbilityAdjustment(
                    creature,
                    perkType,
                    primaryPerkStat,
                    secondaryPerkStat,
                    adjustmentStat);
            }

            return adjustment;
        }

        public void ApplyRangedAbilityTargetDefenseReduction(
            uint attacker,
            uint defender,
            SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                !IsRangedWeaponSkill(skillType))
            {
                return;
            }

            var reduction = _stat.GetStatAdjustment(attacker, StatType.RangedAbilityTargetDefenseReductionPercent);
            var duration = _stat.GetStatAdjustment(attacker, StatType.RangedAbilityTargetDefenseReductionDurationSeconds);
            if (reduction <= 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                defender,
                StatType.PhysicalDefensePercentAdjustment,
                -reduction,
                duration,
                StatType.RangedAbilityTargetDefenseReductionPercent);
        }


        public int GetHitChanceAgainstSunderedTargetAdjustment(uint attacker, uint defender)
        {
            return GetIsObjectValid(defender) && _statusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect))
                ? _stat.GetStatAdjustment(attacker, StatType.HitChanceAgainstSunderedTargetPercentAdjustment)
                : 0;
        }

        public int GetCriticalRateAgainstSunderedTargetAdjustment(uint attacker, uint defender)
        {
            return GetIsObjectValid(defender) && _statusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect))
                ? _stat.GetStatAdjustment(attacker, StatType.CriticalRateAgainstSunderedTargetPercentAdjustment)
                : 0;
        }

        public void ApplyForceAbilityEvadedEffects(uint defender)
        {
            var forceDefense = _stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment);
            var duration = _stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedDurationSeconds);
            var staminaRestore = _stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedStaminaRestore);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedCooldownSeconds);
            if (forceDefense == 0 && staminaRestore <= 0 ||
                duration <= 0 ||
                !_resources.TryUseStatTrigger(defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, cooldown))
                return;

            if (forceDefense != 0)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.ForceDefensePercentAdjustment,
                    forceDefense,
                    duration,
                    StatType.ForceAbilityEvadedForceDefensePercentAdjustment);
            }

            if (staminaRestore > 0)
            {
                _resources.RestoreStaminaFromStat(defender, StatType.ForceAbilityEvadedStaminaRestore, staminaRestore);
            }
        }
    }
}
