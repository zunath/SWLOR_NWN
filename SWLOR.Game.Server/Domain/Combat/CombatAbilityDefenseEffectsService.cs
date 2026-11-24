using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The defense riders of an ability activation: the evasion, the attack and ranged deflection, the nearby ally defense and
    /// the perk category guard that an activation grants. Every step is a public member that
    /// <see cref="CombatAbilityActivationService"/> calls in its fixed order; the part holds no state.
    /// </summary>
    public class CombatAbilityDefenseEffectsService : ICombatAbilityDefenseEffectsService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatDamageTakenEffectsService _damageTakenEffects;
        private readonly IStatService _stat;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IPerkService _perk;

        public CombatAbilityDefenseEffectsService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatDamageTakenEffectsService damageTakenEffects,
            IStatService stat,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier,
            IPerkService perk)
        {
            _queries = queries;
            _resources = resources;
            _damageTakenEffects = damageTakenEffects;
            _stat = stat;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
            _perk = perk;
        }

        public void ApplyAreaAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType,
            bool isAreaAbility)
        {
            if (ability?.IsHostileAbility != true || !isAreaAbility)
                return;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AreaAbilityUsedEvasionPercentAdjustmentSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return;

            var evasion = _stat.GetStatAdjustment(activator, StatType.AreaAbilityUsedEvasionPercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AreaAbilityUsedEvasionDurationSeconds);
            if (evasion == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                activator,
                StatType.EvasionPercentAdjustment,
                evasion,
                duration,
                StatType.AreaAbilityUsedEvasionPercentAdjustment);
        }

        public void ApplyHostileAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityUsedEvasionPercentAdjustmentSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return;

            ApplyAbilityUsedEvasion(
                activator,
                StatType.HostileAbilityUsedEvasionPercentAdjustment,
                StatType.HostileAbilityUsedEvasionDurationSeconds);
        }

        public void ApplyCostlyAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true ||
                !_resources.TryGetAbilityStaminaCostState(activator, ability, out var costState))
            {
                return;
            }

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityUsedEvasionPercentAdjustmentSkillType));
            var minimumCost = _stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityUsedEvasionMinimumStaminaCost);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                minimumCost <= 0 ||
                costState.Cost < minimumCost)
            {
                return;
            }

            ApplyAbilityUsedEvasion(
                activator,
                StatType.CostlyAbilityUsedEvasionPercentAdjustment,
                StatType.CostlyAbilityUsedEvasionDurationSeconds);
        }

        public void ApplyAbilityUsedNearbyAllyDefense(uint activator)
        {
            var defense = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNearbyAllyDefensePercentAdjustment);
            var forceDefense = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNearbyAllyForceDefensePercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNearbyAllyDefenseDurationSeconds);
            if (duration <= 0 || defense == 0 && forceDefense == 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f, false))
            {
                if (defense != 0)
                {
                    _temporaryStatModifier.Replace(
                        friendly,
                        StatType.PhysicalDefensePercentAdjustment,
                        defense,
                        duration,
                        StatType.AbilityUsedNearbyAllyDefensePercentAdjustment);
                }

                if (forceDefense != 0)
                {
                    _temporaryStatModifier.Replace(
                        friendly,
                        StatType.ForceDefensePercentAdjustment,
                        forceDefense,
                        duration,
                        StatType.AbilityUsedNearbyAllyDefensePercentAdjustment);
                }
            }
        }

        public void ApplyAbilityUsedPerkCategorySelfDefense(
            uint activator,
            AbilityDetail ability)
        {
            if (!_queries.AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategorySelfDefenseCategoryId))
            {
                return;
            }

            var evasion = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfEvasionPercentAdjustment);
            var defense = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment);
            var forceDefense = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfForceDefensePercentAdjustment);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfDefenseDurationSeconds);
            if (duration <= 0 || evasion == 0 && defense == 0 && forceDefense == 0)
                return;

            var cooldown = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfDefenseCooldownSeconds);
            if (!_resources.TryUseStatTrigger(
                    activator,
                    StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment,
                    cooldown))
            {
                return;
            }

            _statusEffectApplication.ApplyStatusEffect(
                activator,
                activator,
                new GuardingStepStatusEffect(evasion, defense, forceDefense),
                duration);
        }

        public void ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(
            uint activator,
            AbilityDetail ability)
        {
            if (!_queries.AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId))
            {
                return;
            }

            var attackDeflection = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection);
            var deflectionStatType = _stat.GetGrantedDeflectionStatType(
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds);
            var selfEnmity = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionSelfEnmityPercentAdjustment);
            if (deflectionStatType == StatType.Invalid || attackDeflection <= 0 || duration <= 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f))
            {
                _statusEffectApplication.ApplyStatusEffect(
                    activator,
                    friendly,
                    new SentinelGuardStatusEffect(attackDeflection, selfEnmity, deflectionStatType),
                    duration);
            }
        }

        public void ApplyAbilityUsedSkillEvasion(uint activator, AbilityDetail ability)
        {
            ApplyAbilityUsedSkillEvasionChannel(
                activator,
                ability,
                StatType.AbilityUsedEvasionPercentAdjustmentSkillType,
                StatType.AbilityUsedEvasionPercentAdjustment,
                StatType.AbilityUsedEvasionDurationSeconds);
            ApplyAbilityUsedSkillEvasionChannel(
                activator,
                ability,
                StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType,
                StatType.SecondaryAbilityUsedEvasionPercentAdjustment,
                StatType.SecondaryAbilityUsedEvasionDurationSeconds,
                StatType.SecondaryAbilityUsedEvasionRangedOnly);
        }

        private void ApplyAbilityUsedSkillEvasionChannel(
            uint activator,
            AbilityDetail ability,
            StatType skillTypeStat,
            StatType evasionStat,
            StatType durationStat,
            StatType? rangedOnlyStat = null)
        {
            if (!_queries.AbilityUsedTriggerSkillMatches(
                    activator,
                    ability,
                    _queries.GetAbilitySkillType(activator, ability),
                    skillTypeStat,
                    rangedOnlyStat))
            {
                return;
            }

            var evasionPercent = _stat.GetStatAdjustment(
                activator,
                evasionStat);
            var duration = _stat.GetStatAdjustment(
                activator,
                durationStat);
            if (evasionPercent == 0 || duration <= 0)
                return;

            _statusEffectApplication.ApplyStatusEffect(
                activator,
                activator,
                new EvasiveFootworkStatusEffect(evasionPercent),
                duration);
        }

        public void ApplyAbilityUsedSkillRangedDeflection(uint activator, AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            if (!_queries.AbilityUsedTriggerSkillMatches(
                    activator,
                    ability,
                    _queries.GetAbilitySkillType(activator, ability),
                    StatType.AbilityUsedRangedDeflectionSkillType,
                    StatType.AbilityUsedRangedDeflectionRangedOnly))
            {
                return;
            }

            var deflection = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflection);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflectionDurationSeconds);
            var statusEffectIcon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflectionStatusEffectIcon));
            if (deflection <= 0 || duration <= 0 || statusEffectIcon == EffectIconType.Invalid)
                return;

            if (_statusEffectApplication.ApplyStatusEffect(
                    activator,
                    activator,
                    new RangedDeflectionStatusEffect(deflection, statusEffectIcon),
                    duration))
            {
                var source = _stat.GetStatTypeDeflectionSource(StatType.AbilityUsedRangedDeflection);
                _damageTakenEffects.ApplyAbilityGrantedAttackDeflectionEffects(activator, source);
            }
        }

        public void ApplySingleTargetAbilityUsedAttackDeflection(
            uint activator,
            AbilityDetail ability,
            bool isSingleTargetAbility)
        {
            if (!isSingleTargetAbility)
                return;

            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.SingleTargetAbilityAttackDeflectionSkillType));
            var abilitySkillType = _queries.GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            _damageTakenEffects.ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.SingleTargetAbilityAttackDeflection,
                StatType.SingleTargetAbilityAttackDeflectionDurationSeconds);
        }

        public void ApplyAbilityUsedSkillAttackDeflection(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedAttackDeflectionSkillType));
            var abilitySkillType = _queries.GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            _damageTakenEffects.ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.AbilityUsedAttackDeflection,
                StatType.AbilityUsedAttackDeflectionDurationSeconds,
                StatType.AbilityUsedAttackDeflectionFPRestore);
        }

        public void ApplyAbilityUsedPerkCategoryAttackDeflection(uint activator, AbilityDetail ability)
        {
            var categoryValue = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryAttackDeflectionCategoryType);
            if (!_perk.IsPerkInCategory(ability?.EffectiveLevelPerkType ?? PerkType.Invalid, categoryValue))
            {
                return;
            }

            _damageTakenEffects.ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.AbilityUsedPerkCategoryAttackDeflection,
                StatType.AbilityUsedPerkCategoryAttackDeflectionDurationSeconds,
                StatType.AbilityUsedPerkCategoryAttackDeflectionFPRestore);
        }

        private void ApplyAbilityUsedEvasion(
            uint activator,
            StatType evasionStatType,
            StatType durationStatType,
            StatType targetStatType = StatType.EvasionPercentAdjustment)
        {
            var evasionPercent = _stat.GetStatAdjustment(activator, evasionStatType);
            var duration = _stat.GetStatAdjustment(activator, durationStatType);
            if (evasionPercent == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                activator,
                targetStatType,
                evasionPercent,
                duration,
                evasionStatType);
        }
    }
}
