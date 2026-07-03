using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class AbilityImpactEffectDispatcher
    {
        public static void ApplySuccessfulAbilityImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType,
            int damage,
            bool statusApplied,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target) || ability == null)
                return;

            AbilityImpactEffectDispatcher.ApplySameTargetHostileAbilityHitEffects(activator, target, ability);
            AbilityImpactEffectDispatcher.ApplyAbilityStatusRiders(
                activator,
                target,
                ability,
                skillType,
                damage,
                statusApplied,
                primaryStatusEffect,
                additionalStatusEffects);
            StatusAppliedEffects.ApplyStatusAppliedEffects(
                activator,
                target,
                statusApplied,
                primaryStatusEffect,
                additionalStatusEffects);
            StatusAppliedEffects.ApplyAbilityTargetStatusEffects(activator, target, ability);
            AbilityImpactEffectDispatcher.ApplyRangedAbilityHitNearTargetEffects(activator, target, ability, skillType);
            AbilityImpactEffectDispatcher.ApplyCostlyAbilityHitEffects(activator, target, ability, skillType);
            AbilityImpactEffectDispatcher.ApplyAbilityDamageRiders(activator, target, ability, skillType, damageType, damage);
            StatusAppliedEffects.ApplyAreaAbilityTargetHitSequenceEffects(activator, target, ability, skillType);
        }

        internal static void ApplyCostlyAbilityHitEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true ||
                !CombatState.TryGetRecentAbilityStaminaCost(activator, 10f, out var staminaCost))
            {
                return;
            }

            var staminaRestoreSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityHitStaminaRestoreSkillType));
            var statusSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityStatusSkillType));
            var minimumCost = Stat.GetStatAdjustment(activator, StatType.CostlyAbilityHitMinimumStaminaCost);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.CostlyAbilityHitStaminaRestore);
            var exposedDuration = Stat.GetStatAdjustment(activator, StatType.CostlyAbilityExposedDurationSeconds);
            if (minimumCost <= 0 || staminaCost < minimumCost)
            {
                return;
            }

            var applied = false;
            if (staminaRestore > 0 && AbilityImpactEffects.SkillTypeMatches(skillType, staminaRestoreSkillType))
            {
                Stat.RestoreStamina(activator, staminaRestore);
                applied = true;
            }

            if (exposedDuration > 0 && AbilityImpactEffects.SkillTypeMatches(skillType, statusSkillType))
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(ExposedStatusEffect),
                    exposedDuration,
                    CombatDamageType.Physical);
                applied = true;
            }

            if (applied)
                CombatState.ClearAbilityStaminaCost(activator);
        }

        internal static void ApplyRangedAbilityHitNearTargetEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null ||
                !ability.IsHostileAbility ||
                !CombatSkillType.IsRangedWeaponSkill(skillType) ||
                !GetIsObjectValid(target))
            {
                return;
            }

            var range = Stat.GetStatAdjustment(activator, StatType.RangedAbilityHitNearTargetRangeMeters);
            var damageDealt = Stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.RangedAbilityHitNearTargetDurationSeconds);
            if (range <= 0 || damageDealt == 0 || duration <= 0)
                return;

            if (GetDistanceBetween(activator, target) > range)
                return;

            TemporaryStatModifier.Replace(
                target,
                StatType.DamageDealtPercentAdjustment,
                damageDealt,
                duration,
                StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment);
        }

        internal static void ApplySameTargetHostileAbilityHitEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var requiredCount = Stat.GetStatAdjustment(activator, StatType.SameTargetHostileAbilityHitCountRequired);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.SameTargetHostileAbilityStaminaRestore);
            if (requiredCount <= 0 || staminaRestore <= 0)
                return;

            if (!CombatState.TrackSameTargetHostileAbilityHit(activator, target, requiredCount))
                return;

            Stat.RestoreStamina(activator, staminaRestore);
        }

        internal static void ApplyAbilityStatusRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            int damage,
            bool statusApplied,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            switch (skillType)
            {
                case SkillType.HeavyVibroblade:
                    WeaponAbilityImpactEffects.ApplyHeavyVibrobladeDefenseImpactRiders(activator, target, ability);
                    break;
                case SkillType.Force:
                    WeaponAbilityImpactEffects.ApplyForceDarkImpactRiders(activator, target, primaryStatusEffect, additionalStatusEffects);
                    break;
                case SkillType.Katar:
                    WeaponStatusImpactEffects.ApplyKatarVenomCurrentImpactRiders(activator, target);
                    break;
                case SkillType.Leadership:
                    WeaponStatusImpactEffects.ApplyLeadershipVanguardImpactRiders(activator, target);
                    break;
                case SkillType.Lightsaber:
                    WeaponAbilityImpactEffects.ApplyLightsaberOffenseImpactRiders(activator, target, ability);
                    break;
                case SkillType.Pistol:
                    WeaponStatusImpactEffects.ApplyPistolSkirmisherImpactRiders(activator, target, ability);
                    break;
                case SkillType.Rifle:
                    WeaponStatusImpactEffects.ApplyRiflePacificationImpactRiders(activator, target);
                    break;
                case SkillType.Saberstaff:
                    WeaponStatusImpactEffects.ApplySaberstaffConduitImpactRiders(activator, target, ability);
                    WeaponStatusImpactEffects.ApplySaberstaffTempestImpactRiders(activator, target, ability);
                    break;
                case SkillType.Spear:
                    WeaponStatusImpactEffects.ApplySpearDamageImpactRiders(activator, target, ability);
                    WeaponStatusImpactEffects.ApplySpearDisablerImpactRiders(activator, target, primaryStatusEffect, additionalStatusEffects);
                    break;
                case SkillType.Staff:
                    WeaponStatusImpactEffects.ApplyStaffCrusherImpactRiders(activator, target);
                    break;
                case SkillType.Throwing:
                    WeaponStatusImpactEffects.ApplyThrowingDeadeyeImpactRiders(activator, target, ability);
                    break;
                case SkillType.TwinBlade:
                    WeaponStatusImpactEffects.ApplyTwinBladeDuelistImpactRiders(activator, target, ability);
                    break;
                case SkillType.Vibroknife:
                    WeaponStatusImpactEffects.ApplyVibroknifeShadowImpactRiders(activator, target, ability);
                    WeaponStatusImpactEffects.ApplyVibroknifeSaboteurImpactRiders(activator, target, primaryStatusEffect, additionalStatusEffects);
                    break;
            }

            WeaponAbilityImpactEffects.ApplyAbilityUsedPerkCategoryTargetEnmityToSourceStatus(activator, target, ability);
            StatusAppliedEffects.ApplyGuardedHitNextSkillAbilityExposedStatus(activator, target, skillType);
        }

        internal static void ApplyAbilityDamageRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType,
            int damage)
        {
            if (damage <= 0)
                return;

            WeaponAbilityImpactEffects.ApplyRangedHitSuppressionStack(activator, target, skillType, damageType);
            AbilityImpactAreaDamageEffects.ApplyFoggyMindResourceDrain(activator, target, ability);
            WeaponAbilityImpactEffects.ApplyBleedingTargetAbilityBleedRefresh(activator, target, skillType);
            WeaponAbilityImpactEffects.ApplyBleedingTargetAbilityBleedSpread(activator, target, skillType, damageType);
            AbilityImpactAreaDamageEffects.ApplyAreaAbilityFragmentation(activator, target, ability, skillType, damageType);

            switch (skillType)
            {
                case SkillType.Katar when ability.IsSingleTargetAbility &&
                    Stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentSecondStrikeDamageBonus) > 0:
                    var bonus = Stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentSecondStrikeDamageBonus);
                    TriggeredCombatEffects.ApplyTriggeredDamage(activator, target, bonus, damageType);

                    if (StatusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect)))
                    {
                        StatusEffect.ApplyStatusEffect(activator, target, typeof(BleedStatusEffect), 30f, damageType);
                    }
                    break;
                case SkillType.Pistol when ability.IsSingleTargetAbility:
                    AbilityImpactAreaDamageEffects.ApplyRicochetDamage(
                        activator,
                        target,
                        damageType,
                        StatType.PistolSkirmisherRicochetDamageBonus,
                        StatType.PistolSkirmisherRicochetMaximumTargets,
                        StatType.PistolSkirmisherRicochetCooldownSeconds);
                    break;
                case SkillType.Throwing:
                    if (ability.IsSingleTargetAbility)
                    {
                        AbilityImpactAreaDamageEffects.ApplyRicochetDamage(
                            activator,
                            target,
                            damageType,
                            StatType.ThrowingDeadeyeRicochetDamageBonus,
                            StatType.ThrowingDeadeyeRicochetMaximumTargets);
                    }

                    if (ability.IsAreaAbility)
                    {
                        AbilityImpactAreaDamageEffects.ApplyClusterStormDamage(activator, target, damageType);
                        AbilityImpactAreaDamageEffects.ApplySaturationToss(activator, target);
                    }
                    break;
            }
        }

    }
}
