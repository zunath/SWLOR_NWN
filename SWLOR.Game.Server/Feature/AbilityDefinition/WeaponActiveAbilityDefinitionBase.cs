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
                .ResolvesImpactStatusCategories(activator => GameServices.WeaponAbilityImpacts.ResolveStatusCategories(
                    profile, activator, isHostile, statusEffect, duration, additionalStatusEffects))
                .UsesImpactAnimation(impactAnimation)
                .HasActivationAction((activator, target, level, targetLocation) =>
                    GameServices.WeaponAbilityActivations.ApplyActivation(
                        profile, activator, skill, isHostile, isFriendlyTarget, statusEffect))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    if (!isHostile)
                        GameServices.WeaponAbilityFriendlyTargets.ApplyFriendlyImpact(
                            profile, activator, target, isFriendlyTarget, statusEffect, duration, temporaryHPEffectKey, skill);
                    else
                        GameServices.WeaponAbilityImpacts.ApplyHostileImpact(
                            new WeaponAbilityImpactRequest(
                                profile, skill, baseDamage, duration, statusEffect, additionalStatusEffects, isArea, targetingShape,
                                targetingSizeX, targetingSizeY, targetingFlags, areaVisualEffect, combatImpactDamageAbility, impactAnimation),
                            activator, target, targetLocation);
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
                    return GameServices.WeaponAbilityFriendlyTargets.ValidateHostileTarget(profile, activator, target);
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
                        return GameServices.WeaponAbilityFriendlyTargets.ValidateFriendlyTargetStatus(profile, activator, target);
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
