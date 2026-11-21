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
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility
{
    /// <summary>
    /// Friendly, self and party-facing weapon abilities: target validation and what they apply to the target and the caster.
    /// </summary>
    public class WeaponAbilityFriendlyTargets
    {
        private IAbilityService Ability { get; }
        private IStatusEffectService StatusEffect { get; }
        private IStatusEffectApplicationService StatusEffectApplication { get; }
        private ITemporaryStatModifierService TemporaryStatModifier { get; }
        private WeaponAbilitySelfEffects WeaponAbilitySelfEffects { get; }

        public WeaponAbilityFriendlyTargets(IAbilityService ability, IStatusEffectService statusEffect, IStatusEffectApplicationService statusEffectApplication, ITemporaryStatModifierService temporaryStatModifier, WeaponAbilitySelfEffects weaponAbilitySelfEffects)
        {
            Ability = ability;
            StatusEffect = statusEffect;
            StatusEffectApplication = statusEffectApplication;
            TemporaryStatModifier = temporaryStatModifier;
            WeaponAbilitySelfEffects = weaponAbilitySelfEffects;
        }

        public static bool HasFriendlyTargetStatus(WeaponAbilityProfile profile)
        {
            return profile.FriendlyTargetStatusEffectFactory != null;
        }

        public static bool HasFriendlyTargetEffects(WeaponAbilityProfile profile)
        {
            return profile.FriendlyTargetTemporaryHPPercent > 0 ||
                   profile.SelfGuardPercent > 0;
        }

        public string ValidateHostileTarget(WeaponAbilityProfile profile, uint activator, uint target)
        {
            if (!profile.RequiresRecentWardHitTarget)
                return string.Empty;

            return WardBondStatusEffect.HasRecentWardHit(activator, target, profile.ProtectedTargetHitWindowSeconds)
                ? string.Empty
                : "Target has not recently attacked your ward.";
        }

        public string ValidateFriendlyTargetStatus(WeaponAbilityProfile profile, uint activator, uint target)
        {
            target = ResolveFriendlyTarget(profile, activator, target);
            if (profile.RequiresGuardedTarget && !GetIsObjectValid(target))
                return "You do not have an active Guarded target within range.";

            var validation = AbilityTargeting.ValidateFriendlyTarget(activator, target, false);
            if (!string.IsNullOrWhiteSpace(validation))
                return validation;

            if (profile.RequiresGuardedTarget &&
                !GuardedStatusEffect.IsActiveGuardedBySource(target, activator))
            {
                return "Target is not guarded by you or is too far away.";
            }

            if (profile.FriendlyTargetStatusEffectFactory == null)
                return string.Empty;

            var statusEffect = profile.FriendlyTargetStatusEffectFactory();
            statusEffect.ReassignSource(activator);
            return statusEffect.CanApply(target);
        }

        public static uint ResolveFriendlyTarget(WeaponAbilityProfile profile, uint activator, uint target)
        {
            if (!profile.RequiresGuardedTarget)
                return target;

            return GuardedStatusEffect.GetActiveGuardedTarget(activator);
        }

        public bool ApplyFriendlyTargetStatus(WeaponAbilityProfile profile, uint activator, uint target, float duration)
        {
            if (profile.FriendlyTargetStatusEffectFactory == null)
                return false;

            var statusEffect = profile.FriendlyTargetStatusEffectFactory();
            statusEffect.ReassignSource(activator);
            var canApply = statusEffect.CanApply(target);
            if (!string.IsNullOrWhiteSpace(canApply))
            {
                SendMessageToPC(activator, $"Effect failed to apply: {canApply}");
                return false;
            }

            StatusEffect.RemoveStatusEffectFromAllTargetsBySource(statusEffect.GetType(), activator, false);
            var effectDuration = profile.FriendlyTargetStatusPersistsUntilBroken
                ? 0f
                : duration > 0f
                    ? duration
                    : 45f;

            return StatusEffectApplication.ApplyStatusEffect(
                activator,
                target,
                statusEffect,
                effectDuration,
                CombatDamageType.Physical);
        }

        public void ApplyFriendlyTargetEffects(WeaponAbilityProfile profile, uint activator, uint target, string temporaryHPEffectKey)
        {
            if (profile.FriendlyTargetTemporaryHPPercent > 0 &&
                profile.FriendlyTargetTemporaryHPDurationSeconds > 0)
            {
                var maximumHPSource = profile.FriendlyTargetTemporaryHPUsesActivatorMaximum
                    ? activator
                    : target;
                var temporaryHP = HealAmount.FromRate(profile.FriendlyTargetTemporaryHPPercent)
                    .ForMaxHitPoints(GetMaxHitPoints(maximumHPSource));
                TemporaryHitPointEffects.ApplyFlat(
                    target,
                    temporaryHPEffectKey,
                    temporaryHP,
                    profile.FriendlyTargetTemporaryHPDurationSeconds);
            }

            if (profile.SelfGuardPercent > 0 &&
                profile.SelfGuardDurationSeconds > 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.Guard,
                    profile.SelfGuardPercent,
                    profile.SelfGuardDurationSeconds,
                    "GeneratedWeaponAbility:GuardingPlayerGuard");
                GuardedStatusEffect.RefreshGuardBenefitsFromSource(activator);
                Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
            }
        }

        public static bool HasImmediateSelfStatusEffect(WeaponAbilityProfile profile)
        {
            return profile.SelfStatusEffectFactory != null && profile.SelfStatDurationSeconds <= 0;
        }

        public void ApplySelfStatusEffect(WeaponAbilityProfile profile, uint activator, int duration)
        {
            if (profile.SelfStatusEffectFactory == null)
                return;

            if (profile.SelfStatusEffectsToReplace != null)
            {
                foreach (var statusEffectType in profile.SelfStatusEffectsToReplace)
                {
                    if (statusEffectType != null)
                        StatusEffect.RemoveStatusEffect(activator, statusEffectType, false);
                }
            }

            var selfStatus = profile.SelfStatusEffectFactory();
            StatusEffect.RemoveOtherStanceStatuses(activator, selfStatus.GetType());
            if (StatusEffectApplication.ApplyStatusEffect(activator, activator, selfStatus, duration > 0f ? duration : 0f))
                Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
        }

        public void ApplySelfStatusToGuardedTarget(WeaponAbilityProfile profile, uint activator, Type statusEffect, int duration)
        {
            if (!profile.SelfStatusAlsoAppliesToGuardedTarget ||
                statusEffect == null ||
                duration <= 0)
                return;

            var guardedTarget = GuardedStatusEffect.GetActiveGuardedTarget(activator);
            if (!GetIsObjectValid(guardedTarget))
                return;

            if (StatusEffectApplication.ApplyStatusEffect(activator, guardedTarget, statusEffect, duration))
                Ability.PlaySuccessfulImpactVisualEffect(activator, guardedTarget);
        }

        public void ApplyFriendlyImpact(
            WeaponAbilityProfile profile,
            uint activator,
            uint target,
            bool isFriendlyTarget,
            Type statusEffect,
            int duration,
            string temporaryHPEffectKey,
            SkillType skill)
        {
            target = ResolveFriendlyTarget(profile, activator, target);
            if (isFriendlyTarget && HasFriendlyTargetStatus(profile))
            {
                if (ApplyFriendlyTargetStatus(profile, activator, target, duration))
                {
                    ApplyFriendlyTargetEffects(profile, activator, target, temporaryHPEffectKey);
                    Ability.PlaySuccessfulImpactVisualEffect(activator, target);
                    WeaponAbilitySelfEffects.AfterActivation(profile, activator, skill);
                }
                return;
            }

            if (isFriendlyTarget && HasFriendlyTargetEffects(profile))
            {
                ApplyFriendlyTargetEffects(profile, activator, target, temporaryHPEffectKey);
                Ability.PlaySuccessfulImpactVisualEffect(activator, target);
                WeaponAbilitySelfEffects.AfterActivation(profile, activator, skill);
                return;
            }

            if (HasImmediateSelfStatusEffect(profile))
            {
                ApplySelfStatusEffect(profile, activator, duration);
            }
            else if (statusEffect != null)
            {
                StatusEffect.RemoveOtherStanceStatuses(activator, statusEffect);
                if (StatusEffectApplication.ApplyStatusEffect(activator, activator, statusEffect, duration > 0f ? duration : 0f))
                    Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
                ApplySelfStatusToGuardedTarget(profile, activator, statusEffect, duration);
            }

            WeaponAbilitySelfEffects.AfterActivation(profile, activator, skill);
            return;
        }
    }
}
