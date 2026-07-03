using System.Linq;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class AbilityUseEffects
    {
        public static float GetAbilityRecastDelayFlatAdjustment(uint creature, PerkType perkType)
        {
            return AbilityImpactEffects.GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityRecastDelayFlatAdjustmentPerkType,
                StatType.AbilityRecastDelayFlatAdjustmentPerkType,
                StatType.AbilityRecastDelayFlatAdjustment);
        }

        public static float ApplyAbilityRecastDelayModifiers(
            uint activator,
            AbilityDetail ability,
            float recastDelaySeconds)
        {
            if (!GetIsObjectValid(activator) || ability?.IsHostileAbility != true || recastDelaySeconds <= 0f)
                return recastDelaySeconds;

            var adjustment = Stat.GetStatAdjustment(activator, StatType.HostileAbilityRecastDelayPercentAdjustment);
            if (adjustment == 0)
                return recastDelaySeconds;

            return Math.Max(0f, recastDelaySeconds + recastDelaySeconds * (adjustment / 100f));
        }

        internal static void ApplyAbilityUsedRecastReduction(uint activator, AbilityDetail ability)
        {
            AbilityUseEffects.ApplyAbilityUsedRecastReduction(activator, ability?.RecastGroup ?? RecastGroup.Invalid);
        }

        internal static void ApplyAbilityUsedRecastReduction(uint activator, RecastGroup activatedRecastGroup)
        {
            var triggerGroup = AbilityImpactEffects.GetRecastGroupFromStat(Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionTriggerGroup));
            var secondaryTriggerGroup = AbilityImpactEffects.GetRecastGroupFromStat(Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionSecondaryTriggerGroup));
            if (activatedRecastGroup == RecastGroup.Invalid ||
                activatedRecastGroup != triggerGroup &&
                activatedRecastGroup != secondaryTriggerGroup)
                return;

            var targetGroup = AbilityImpactEffects.GetRecastGroupFromStat(Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionTargetGroup));
            var seconds = Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionSeconds);
            if (targetGroup == RecastGroup.Invalid || seconds <= 0)
                return;

            Recast.ReduceRecastDelay(activator, targetGroup, seconds);
        }

        internal static void ApplyAbilityUsedNextSkillAutoAttackDamageBonus(uint activator, AbilityDetail ability)
        {
            var triggerSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillAutoAttackDamageBonusTriggerSkillType));
            var abilitySkillType = QueuedCombatActions.GetAbilitySkillType(activator, ability);
            if (!AbilityImpactEffects.SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            var targetSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillAutoAttackDamageBonusSkillType));
            var damageBonus = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonus);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillAutoAttackDamageWindowSeconds);
            AbilityImpactEffects.GrantNextSkillAutoAttackDamageBonus(activator, targetSkillType, damageBonus, duration);
        }

        internal static void ApplyAbilityUsedNextSkillFPCostAdjustment(uint activator, AbilityDetail ability)
        {
            var triggerSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillFPCostAdjustmentTriggerSkillType));
            var abilitySkillType = QueuedCombatActions.GetAbilitySkillType(activator, ability);
            if (!AbilityImpactEffects.SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            var targetSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillFPCostAdjustmentSkillType));
            var adjustment = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillFPCostAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillFPCostAdjustmentWindowSeconds);
            AbilityImpactEffects.GrantNextAbilityFPCostAdjustment(activator, targetSkillType, adjustment, duration);
        }

        internal static void ApplyAbilityUsedSkillEvasion(uint activator, AbilityDetail ability)
        {
            var triggerSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedEvasionPercentAdjustmentSkillType));
            var abilitySkillType = QueuedCombatActions.GetAbilitySkillType(activator, ability);
            if (!AbilityImpactEffects.SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            AbilityRecoveryEffects.ApplyAbilityUsedEvasion(
                activator,
                StatType.AbilityUsedEvasionPercentAdjustment,
                StatType.AbilityUsedEvasionDurationSeconds);
        }

        internal static void ApplyAbilityUsedSkillRangedEvasion(uint activator, AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var triggerSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedEvasionPercentAdjustmentSkillType));
            var abilitySkillType = QueuedCombatActions.GetAbilitySkillType(activator, ability);
            if (!AbilityImpactEffects.SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            AbilityRecoveryEffects.ApplyAbilityUsedEvasion(
                activator,
                StatType.AbilityUsedRangedEvasionPercentAdjustment,
                StatType.AbilityUsedRangedEvasionDurationSeconds,
                StatType.RangedEvasionPercentAdjustment);
        }

        internal static void ApplySingleTargetAbilityUsedAttackDeflection(
            uint activator,
            AbilityDetail ability,
            bool isSingleTargetAbility)
        {
            if (!isSingleTargetAbility)
                return;

            var triggerSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.SingleTargetAbilityAttackDeflectionSkillType));
            var abilitySkillType = QueuedCombatActions.GetAbilitySkillType(activator, ability);
            if (!AbilityImpactEffects.SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            AbilityRecoveryEffects.ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.SingleTargetAbilityAttackDeflection,
                StatType.SingleTargetAbilityAttackDeflectionDurationSeconds);
        }

        internal static void ApplyAbilityUsedSkillAttackDeflection(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var triggerSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedAttackDeflectionSkillType));
            var abilitySkillType = QueuedCombatActions.GetAbilitySkillType(activator, ability);
            if (!AbilityImpactEffects.SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            AbilityRecoveryEffects.ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.AbilityUsedAttackDeflection,
                StatType.AbilityUsedAttackDeflectionDurationSeconds,
                StatType.AbilityUsedAttackDeflectionFPRestore);
        }

        internal static void ApplyAbilityUsedPerkCategoryAttackDeflection(uint activator, AbilityDetail ability)
        {
            var categoryValue = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryAttackDeflectionCategoryType);
            if (!Perk.IsPerkInCategory(ability?.EffectiveLevelPerkType ?? PerkType.Invalid, categoryValue))
            {
                return;
            }

            AbilityRecoveryEffects.ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.AbilityUsedPerkCategoryAttackDeflection,
                StatType.AbilityUsedPerkCategoryAttackDeflectionDurationSeconds,
                StatType.AbilityUsedPerkCategoryAttackDeflectionFPRestore);
        }

        internal static void TrackCombatAbilityUse(uint activator, AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            var skillType = QueuedCombatActions.GetAbilitySkillType(activator, ability);
            if (skillType == SkillType.Invalid)
                return;

            CombatState.TrackCombatAbilityUse(activator);
        }

        internal static void ApplyHostileAbilitySequenceEffects(
            uint activator,
            FeatType feat,
            AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null || !ability.IsHostileAbility)
                return;

            var windowSeconds = Stat.GetStatAdjustment(activator, StatType.HostileAbilitySequenceWindowSeconds);
            var bleedDuration = Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds);
            var completedSequence = CombatState.TrackHostileAbilitySequence(activator, feat, windowSeconds);
            if (windowSeconds <= 0 || bleedDuration <= 0)
                return;

            if (completedSequence)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.NextDamageDealtBleedDurationSeconds,
                    bleedDuration,
                    windowSeconds,
                    StatType.NextDamageDealtBleedDurationSeconds);
            }
        }

        internal static void ApplyHostileAbilityResourceRestoreEffects(uint activator, AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var fpRestore = Stat.GetStatAdjustment(activator, StatType.HostileAbilityFPRestore);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.HostileAbilityStaminaRestore);

            if (fpRestore > 0)
            {
                Stat.RestoreFP(activator, fpRestore);
                AbilityRecoveryEffects.ApplyAbilityRestoredFPEffects(activator);
            }
            if (staminaRestore > 0)
                Stat.RestoreStamina(activator, staminaRestore);

            if (fpRestore > 0 && staminaRestore > 0)
                AbilityRecoveryEffects.ApplyAbilityRestoredBothResourcesEffects(activator);
        }

    }
}
