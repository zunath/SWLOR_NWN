using SWLOR.Game.Server.Core;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using static SWLOR.Game.Server.Composition.GameServices;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public abstract class WeaponActiveAbilityDefinitionBase
    {
        private const float ToggleActivationDelaySeconds = 2f;

        protected static void ConfigureWeaponAbility(
            AbilityBuilder ability,
            SkillType skill,
            int baseDamage,
            int duration,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            int stamina,
            int fp,
            float activationDelay,
            bool isArea,
            bool isHostile,
            bool isFriendlyTarget,
            Spell targetingSpell,
            AbilityTargetingShapeType targetingShape,
            float targetingSizeX,
            float targetingSizeY,
            AbilityTargetingFlags targetingFlags,
            Animation impactAnimation,
            float maxRange,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            WeaponAbilityProfile profile = null,
            VisualEffect areaVisualEffect = VisualEffect.None)
        {
            profile ??= WeaponAbilityProfile.Empty;
            // The guarded-hit status replaces the impact status, so the ability must not author another one.
            if (profile.RecentGuardedHitStatusEffect != null &&
                (statusEffect != null || profile.StatusEffectFactory != null || !isHostile))
            {
                throw new InvalidOperationException(
                    "A recent guarded-hit status needs a hostile weapon ability without its own impact status.");
            }

            var temporaryHPEffectKey = $"WEAPON_{ability.ActiveEffectiveLevelPerkType}";

            ApplyCombatImpactDamageAbility(ability, combatImpactDamageAbility);

            ability.HasActivationDelay(activationDelay)
                .SkillType(skill)
                .ResolvesImpactStatusCategories(activator => isHostile
                    ? AbilityStatusApplication.GetCombatImpactStatusCategories(
                        profile.ResolveImpactStatus(activator, statusEffect, duration).StatusEffect,
                        additionalStatusEffects,
                        profile.StatusEffectFactory)
                    : StatusEffectCategory.None)
                .UsesImpactAnimation(impactAnimation)
                .HasActivationAction((activator, target, level, targetLocation) =>
                {
                    profile.PrepareActivationIdleBonuses(activator);
                    if (isHostile && !profile.IsQueuedWeaponAbility)
                        GameServices.CombatActivity.StoreAbilityActivationIdleBonuses(activator, skill);
                    profile.PrepareQueuedActivation(activator, skill);
                    if (profile.IsQueuedWeaponAbility)
                        GameServices.CombatAbilityBonus.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(activator, skill);
                    return isHostile || isFriendlyTarget || statusEffect == null || ToggleSelfStatus(activator, statusEffect);
                })
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    if (!isHostile)
                    {
                        target = profile.ResolveFriendlyTarget(activator, target);
                        if (isFriendlyTarget && profile.HasFriendlyTargetStatus())
                        {
                            if (profile.ApplyFriendlyTargetStatus(activator, target, duration))
                            {
                                profile.ApplyFriendlyTargetEffects(activator, target, temporaryHPEffectKey);
                                Ability.PlaySuccessfulImpactVisualEffect(activator, target);
                                profile.AfterActivation(activator, skill);
                            }
                            return;
                        }

                        if (isFriendlyTarget && profile.HasFriendlyTargetEffects())
                        {
                            profile.ApplyFriendlyTargetEffects(activator, target, temporaryHPEffectKey);
                            Ability.PlaySuccessfulImpactVisualEffect(activator, target);
                            profile.AfterActivation(activator, skill);
                            return;
                        }

                        if (profile.HasImmediateSelfStatusEffect())
                        {
                            profile.ApplySelfStatusEffect(activator, duration);
                        }
                        else if (statusEffect != null)
                        {
                            StatusEffect.RemoveOtherStanceStatuses(activator, statusEffect);
                            if (StatusEffectApplication.ApplyStatusEffect(activator, activator, statusEffect, duration > 0f ? duration : 0f))
                                Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
                            profile.ApplySelfStatusToGuardedTarget(activator, statusEffect, duration);
                        }

                            profile.AfterActivation(activator, skill);
                        return;
                    }

                    profile.SpendHitPoints(activator);
                    profile.AfterHostileActivation(activator);
                    var (impactStatusEffect, impactDuration) = profile.ResolveImpactStatus(activator, statusEffect, duration);
                    var spreadSnapshot = new WeaponAbilityProfile.StatusSpreadSnapshot(profile.MaximumStatusSpreadsPerCast);
                    Action<uint> captureSpreadPrerequisites = profile.SpreadBleedFromTarget || profile.SpreadHemorrhageFromTarget || profile.SpreadSunderFromTarget
                        ? impactedTarget => spreadSnapshot.Capture(impactedTarget)
                        : null;
                    var activationIdleBonusSnapshot = profile.CaptureActivationIdleBonusSnapshot(activator);
                    AbilityImpactState.AddActiveAbilityDefenseIgnorePercentAdjustment(
                        activator,
                        profile.GetDefenseIgnorePercent(activator, activationIdleBonusSnapshot));

                    if (isArea && ShouldUseTelegraphedCombatImpact(targetingShape, profile))
                    {
                        var areaDamage = 0;
                        try
                        {
                            areaDamage = Ability.ApplyTelegraphedCombatImpact(
                                activator,
                                target,
                                targetLocation,
                                skill,
                                baseDamage,
                                impactDuration,
                                impactStatusEffect,
                                ToCombatImpactAreaShape(targetingShape),
                                profile.TelegraphDuration,
                                targetingSizeX > 0f ? targetingSizeX : 5.0f,
                                targetingSizeY,
                                additionalStatusEffects,
                                CenterAreaOnActivator(targetingFlags),
                                profile.StatusEffectFactory,
                                damageType: profile.DamageType,
                                areaVisualEffect: areaVisualEffect,
                                combatImpactDamageAbility: combatImpactDamageAbility,
                                baseDamageAdjustment: impactedTarget => profile.GetBaseDamageAdjustment(
                                    activator,
                                    impactedTarget,
                                    activationIdleBonusSnapshot),
                                damagePercentAdjustment: impactedTarget => profile.GetDamagePercentAdjustment(activator, impactedTarget),
                                afterImpactAction: summary =>
                                {
                                    profile.RestoreResourcesForTargetCount(activator, summary);
                                    // Instant impacts apply these modifiers in AfterImpact below.
                                    if (profile.TelegraphDuration > 0f && summary.ImpactedTargetCount > 0)
                                        profile.AfterActivation(activator, skill);
                                },
                                maxTargets: profile.MaximumAreaTargets,
                                enmityBonus: profile.EnmityBonus,
                                beforeImpact: captureSpreadPrerequisites,
                                afterSuccessfulHit: impactedTarget =>
                                {
                                    profile.AfterSuccessfulHit(activator, impactedTarget, AbilityDamageInput.GetWeaponAbilityDamageType(activator, skill, profile.DamageType), spreadSnapshot);
                                    GameServices.CombatAbilityHit.ApplyRangedAbilityTargetDefenseReduction(activator, impactedTarget, skill);
                                },
                                beforeSuccessfulImpactRiders: impactedTarget => profile.BeforeSuccessfulImpactRiders(
                                    activator,
                                    impactedTarget,
                                    AbilityDamageInput.GetWeaponAbilityDamageType(activator, skill, profile.DamageType)),
                                hitChancePercentAdjustment: profile.HitChancePercentAdjustment,
                                criticalRatePercentAdjustment: profile.GetCriticalRateAdjustment(
                                    activator,
                                    target,
                                    activationIdleBonusSnapshot),
                                impactAnimation: impactAnimation);
                        }
                        finally
                        {
                            profile.ClearActivationIdleBonusSnapshots(activator);
                        }

                        profile.AfterImpact(
                            activator,
                            areaDamage,
                            areaDamage > 0 ? 1 : 0,
                            AbilityImpactState.GetActiveAbilityImpactSummary(activator),
                            clearActivationIdleBonusSnapshots: false);
                        return;
                    }

                    var totalDamage = 0;
                    var successfulHitCount = 0;
                    var hitCount = Math.Max(1, profile.HitCount);
                    for (var hit = 0; hit < hitCount; hit++)
                    {
                        var hitDamage = Ability.ApplyCombatImpact(
                            activator,
                            target,
                            targetLocation,
                            skill,
                            baseDamage,
                            impactDuration,
                            impactStatusEffect,
                            isArea,
                            additionalStatusEffects,
                            profile.StatusEffectFactory,
                            damageType: profile.DamageType,
                            combatImpactDamageAbility: combatImpactDamageAbility,
                            baseDamageAdjustment: impactedTarget => profile.GetBaseDamageAdjustment(
                                activator,
                                impactedTarget,
                                activationIdleBonusSnapshot),
                            damagePercentAdjustment: impactedTarget => profile.GetDamagePercentAdjustment(activator, impactedTarget),
                            enmityBonus: profile.EnmityBonus,
                            beforeImpact: captureSpreadPrerequisites,
                            afterSuccessfulHit: impactedTarget =>
                            {
                                profile.AfterSuccessfulHit(activator, impactedTarget, AbilityDamageInput.GetWeaponAbilityDamageType(activator, skill, profile.DamageType), spreadSnapshot);
                                GameServices.CombatAbilityHit.ApplyRangedAbilityTargetDefenseReduction(activator, impactedTarget, skill);
                            },
                            beforeSuccessfulImpactRiders: impactedTarget => profile.BeforeSuccessfulImpactRiders(
                                activator,
                                impactedTarget,
                                AbilityDamageInput.GetWeaponAbilityDamageType(activator, skill, profile.DamageType)),
                            hitChancePercentAdjustment: profile.HitChancePercentAdjustment,
                            criticalRatePercentAdjustment: profile.GetCriticalRateAdjustment(
                                activator,
                                target,
                                activationIdleBonusSnapshot),
                            playImpactAnimation: hit == 0);
                        totalDamage += hitDamage;
                        if (hitDamage > 0)
                            successfulHitCount++;
                    }

                    profile.RestoreResourcesForTargetCount(activator, AbilityImpactState.GetActiveAbilityImpactSummary(activator));
                    profile.AfterImpact(activator, totalDamage, successfulHitCount, AbilityImpactState.GetActiveAbilityImpactSummary(activator));
                })
                .BreaksStealth();

            if (profile.IsQueuedWeaponAbility)
                ability.IsWeaponAbility();
            else
                ability.IsCastedAbility();

            if (isHostile)
            {
                ability.IsHostileAbility();
                if (profile.SuppressSourceStatusStackRiders)
                    ability.SuppressesSourceStatusStackRiders();

                if (isArea)
                {
                    ability.IsAreaAbility();
                    ApplyTargetingMetadata(ability, activationDelay, targetingSpell, targetingShape, targetingSizeX, targetingSizeY, targetingFlags);
                }
                else
                {
                    ability.IsSingleTargetAbility();

                    // Queued weapon abilities fire on the wearer's next landed auto-attack, so they must
                    // not force up-front target selection (the on-hit event supplies the target). Only
                    // cast-style single-target abilities require picking a target object.
                    if (!profile.IsQueuedWeaponAbility)
                        ability.RequiresTarget();

                    if (maxRange > 0f)
                        ability.HasMaxRange(maxRange);
                }

                ability.HasCustomValidation((activator, target, level, targetLocation) =>
                {
                    return profile.ValidateHostileTarget(activator, target);
                });
            }
            else if (statusEffect != null)
            {
                ability.RemoveStatusEffectOnPerkRefund(statusEffect);
            }
            else if (isFriendlyTarget)
            {
                ability.IsSingleTargetAbility();

                if (!profile.RequiresGuardedTarget)
                    ability.RequiresTarget();

                ability.HasCustomValidation((activator, target, level, targetLocation) =>
                    {
                        return profile.ValidateFriendlyTargetStatus(activator, target);
                    });
                if (maxRange > 0f)
                    ability.HasMaxRange(maxRange);
            }

            if (profile.SourceOwnedStatusEffectTypeRemovedOnPerkRefund != null)
            {
                ability.RemoveSourceOwnedStatusEffectOnPerkRefund(
                    profile.SourceOwnedStatusEffectTypeRemovedOnPerkRefund);
            }

            if (stamina > 0)
                ability.RequirementStamina(stamina);
            if (fp > 0)
                ability.RequirementFP(fp);
        }

        private static bool ShouldUseTelegraphedCombatImpact(
            AbilityTargetingShapeType targetingShape,
            WeaponAbilityProfile profile)
        {
            return targetingShape != AbilityTargetingShapeType.None || profile.MaximumAreaTargets > 0;
        }

        private static CombatImpactAreaShape ToCombatImpactAreaShape(AbilityTargetingShapeType targetingShape)
        {
            return targetingShape switch
            {
                AbilityTargetingShapeType.Cone => CombatImpactAreaShape.Cone,
                AbilityTargetingShapeType.Rect => CombatImpactAreaShape.Line,
                _ => CombatImpactAreaShape.Sphere
            };
        }

        private static bool CenterAreaOnActivator(AbilityTargetingFlags targetingFlags)
        {
            return (targetingFlags & AbilityTargetingFlags.OriginOnSelf) == AbilityTargetingFlags.OriginOnSelf;
        }

        private static void ApplyTargetingMetadata(
            AbilityBuilder ability,
            float activationDelay,
            Spell targetingSpell,
            AbilityTargetingShapeType targetingShape,
            float targetingSizeX,
            float targetingSizeY,
            AbilityTargetingFlags targetingFlags)
        {
            if (targetingSpell != Spell.Invalid && targetingShape != AbilityTargetingShapeType.None)
            {
                ApplyClientTargeting(ability, targetingSpell, targetingShape, targetingSizeX, targetingSizeY, targetingFlags);
                return;
            }

            if (activationDelay <= 0f)
                return;

            ability.HasActivationTargetingSphere(5.0f, targetingFlags);
        }

        private static void ApplyClientTargeting(
            AbilityBuilder ability,
            Spell targetingSpell,
            AbilityTargetingShapeType targetingShape,
            float targetingSizeX,
            float targetingSizeY,
            AbilityTargetingFlags targetingFlags)
        {
            switch (targetingShape)
            {
                case AbilityTargetingShapeType.Sphere:
                    ability.HasTargetingSphere(targetingSpell, targetingSizeX, targetingFlags);
                    break;
                case AbilityTargetingShapeType.Rect:
                    ability.HasTargetingLine(targetingSpell, targetingSizeX, targetingSizeY, targetingFlags);
                    break;
                case AbilityTargetingShapeType.Cone:
                    ability.HasTargetingCone(targetingSpell, targetingSizeX, targetingSizeY, targetingFlags);
                    break;
            }
        }

        protected static void ConfigureWeapon(
            AbilityBuilder ability,
            SkillType skill,
            int baseDamage,
            int duration,
            Type statusEffect,
            int stamina,
            Type additionalStatusEffect = null,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid)
        {
            ApplyCombatImpactDamageAbility(ability, combatImpactDamageAbility);

            ability.HasActivationDelay(0f)
                .SkillType(skill)
                .IsSingleTargetAbility()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        skill,
                        baseDamage,
                        duration,
                        statusEffect,
                        false,
                        Additional(additionalStatusEffect),
                        statusEffectFactory,
                        damageType,
                        combatImpactDamageAbility: combatImpactDamageAbility);
                })
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        private static void ApplyCombatImpactDamageAbility(
            AbilityBuilder ability,
            AbilityType combatImpactDamageAbility)
        {
            if (combatImpactDamageAbility != AbilityType.Invalid)
                ability.CombatImpactDamageAbility(combatImpactDamageAbility);
        }

        protected static void ConfigureToggle(AbilityBuilder ability, Type type)
        {
            ConfigureToggle(ability, type, () => (IStatusEffect)Activator.CreateInstance(type));
        }

        protected static void ConfigureToggle(AbilityBuilder ability, Type type, Func<IStatusEffect> statusEffectFactory)
        {
            ability.HasActivationDelay(ToggleActivationDelaySeconds)
                .HasActivationAction((activator, target, level, targetLocation) => ToggleSelfStatus(activator, type))
                .RemoveStatusEffectOnPerkRefund(type)
                .HasImpactAction((activator, target, level, targetLocation) =>
                    GameServices.StatusAbilityEffects.ApplyToggleStatus(activator, type, statusEffectFactory))
                .IsCastedAbility()
                .BreaksStealth();
        }

        protected static void ConfigureSelfStatus(AbilityBuilder ability, Type type, float duration, int stamina, Action<uint> additionalAction = null, float activationDelay = 0f)
        {
            ConfigureSelfStatus(ability, () => (IStatusEffect)Activator.CreateInstance(type), duration, stamina, additionalAction, activationDelay);
        }

        protected static void ConfigureSelfStatus(AbilityBuilder ability, Func<IStatusEffect> statusEffectFactory, float duration, int stamina, Action<uint> additionalAction = null, float activationDelay = 0f)
        {
            ability.HasActivationDelay(activationDelay)
                .HasImpactAction((activator, target, level, targetLocation) =>
                    GameServices.StatusAbilityEffects.ApplySelfStatus(activator, statusEffectFactory, duration, additionalAction))
                .IsCastedAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigurePartyStatus(AbilityBuilder ability, Type type, float duration, int stamina, bool includeSelf, float activationDelay = 0f)
        {
            ConfigurePartyStatus(ability, () => (IStatusEffect)Activator.CreateInstance(type), duration, stamina, includeSelf, activationDelay);
        }

        protected static void ConfigurePartyStatus(AbilityBuilder ability, Func<IStatusEffect> statusEffectFactory, float duration, int stamina, bool includeSelf, float activationDelay = 0f)
        {
            ability.HasActivationDelay(activationDelay)
                .HasImpactAction((activator, target, level, targetLocation) =>
                    GameServices.StatusAbilityEffects.ApplyStatusToNearbyParty(activator, statusEffectFactory, duration, includeSelf))
                .IsCastedAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static bool ToggleSelfStatus(uint activator, Type type)
        {
            return GameServices.StatusAbilityEffects.ToggleSelfStatus(activator, type);
        }

        protected static IEnumerable<Type> Additional(Type additionalStatusEffect)
        {
            return additionalStatusEffect == null
                ? null
                : new[] { additionalStatusEffect };
        }

        protected static void ApplyStatusToNearbyParty(uint activator, Type type, float duration, bool includeSelf)
        {
            ApplyStatusToNearbyParty(activator, () => (IStatusEffect)Activator.CreateInstance(type), duration, includeSelf);
        }

        protected static void ApplyStatusToNearbyParty(uint activator, Func<IStatusEffect> statusEffectFactory, float duration, bool includeSelf)
        {
            GameServices.StatusAbilityEffects.ApplyStatusToNearbyParty(activator, statusEffectFactory, duration, includeSelf);
        }
    }
}
