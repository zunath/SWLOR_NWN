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
    /// The resource and modifier effects of each landed hit on the caster and the target.
    /// </summary>
    public class WeaponAbilityHitEffects
    {
        private ICombatActivityService CombatActivity { get; }
        private ICombatResourcesService CombatResources { get; }
        private IStatService Stat { get; }
        private IStatusEffectApplicationService StatusEffectApplication { get; }
        private WeaponAbilityStatusRiders WeaponAbilityStatusRiders { get; }
        private WeaponAbilityTemporaryModifiers WeaponAbilityTemporaryModifiers { get; }

        public WeaponAbilityHitEffects(ICombatActivityService combatActivity, ICombatResourcesService combatResources, IStatService stat, IStatusEffectApplicationService statusEffectApplication, WeaponAbilityStatusRiders weaponAbilityStatusRiders, WeaponAbilityTemporaryModifiers weaponAbilityTemporaryModifiers)
        {
            CombatActivity = combatActivity;
            CombatResources = combatResources;
            Stat = stat;
            StatusEffectApplication = statusEffectApplication;
            WeaponAbilityStatusRiders = weaponAbilityStatusRiders;
            WeaponAbilityTemporaryModifiers = weaponAbilityTemporaryModifiers;
        }

        public void ApplyResourceConditionalRestore(WeaponAbilityProfile profile, uint activator)
        {
            if (profile.RestoreBothResourcesBelowThresholdPercent <= 0 ||
                !CombatResources.IsCurrentFPAndStaminaAtOrBelowPercent(activator, profile.RestoreBothResourcesBelowThresholdPercent))
            {
                return;
            }

            var restoredFP = profile.RestoreFPIfResourcesBelow > 0
                ? Stat.RestoreFP(activator, profile.RestoreFPIfResourcesBelow)
                : 0;
            var restoredStamina = profile.RestoreStaminaIfResourcesBelow > 0
                ? Stat.RestoreStamina(activator, profile.RestoreStaminaIfResourcesBelow)
                : 0;

            if (restoredFP > 0)
                CombatResources.ApplyAbilityRestoredFPEffects(activator);

            if (restoredFP > 0 && restoredStamina > 0)
                CombatResources.ApplyAbilityRestoredBothResourcesEffects(activator);
        }

        public void ApplyResourceConditionalDrain(WeaponAbilityProfile profile, uint activator, uint target)
        {
            if (profile.DrainTargetResourceAboveThresholdPercent <= 0)
                return;

            if (profile.DrainTargetFPIfActivatorFPAboveThreshold > 0 && IsFPAtOrAbove(activator, profile.DrainTargetResourceAboveThresholdPercent))
                Stat.ReduceFP(target, profile.DrainTargetFPIfActivatorFPAboveThreshold);
            if (profile.DrainTargetStaminaIfActivatorStaminaAboveThreshold > 0 && IsStaminaAtOrAbove(activator, profile.DrainTargetResourceAboveThresholdPercent))
                Stat.ReduceStamina(target, profile.DrainTargetStaminaIfActivatorStaminaAboveThreshold);
        }

        private bool IsFPAtOrAbove(uint creature, int thresholdPercent)
        {
            var maxFP = Stat.GetMaxFP(creature);
            return maxFP > 0 && Stat.GetCurrentFP(creature) >= maxFP * (thresholdPercent / 100f);
        }

        private bool IsStaminaAtOrAbove(uint creature, int thresholdPercent)
        {
            var maxStamina = Stat.GetMaxStamina(creature);
            return maxStamina > 0 && Stat.GetCurrentStamina(creature) >= maxStamina * (thresholdPercent / 100f);
        }

        public void ApplyTargetTemporaryModifiers(WeaponAbilityProfile profile, uint target)
        {
            WeaponAbilityTemporaryModifiers.ReplaceTemporaryTarget(target, StatType.AttackPercentAdjustment, profile.TargetAttackPercent, profile.TargetAttackDurationSeconds);
            WeaponAbilityTemporaryModifiers.ReplaceTemporaryTarget(target, StatType.AbilityHitChancePercentAdjustment, profile.TargetAbilityHitChancePercent, profile.TargetAbilityHitChanceDurationSeconds);
        }

        public void ApplyProtectedTargetHitSelfModifiers(WeaponAbilityProfile profile, uint activator, uint target)
        {
            var window = profile.ProtectedTargetHitWindowSeconds;
            if (profile.SelfGuardPercentIfRecentGuardedAllyHit != 0 &&
                GuardedStatusEffect.HasRecentGuardedAllyHit(activator, target, window))
            {
                WeaponAbilityTemporaryModifiers.ReplaceTemporary(activator, StatType.Guard, profile.SelfGuardPercentIfRecentGuardedAllyHit, profile.SelfGuardDurationSecondsIfRecentGuardedAllyHit);
            }

            if (profile.SelfEnmityPercentIfRecentWardHit != 0 &&
                WardBondStatusEffect.HasRecentWardHit(activator, target, window))
            {
                StatusEffectApplication.ApplyStatusEffect(activator, target,
                    new ChallengeEnmityStatusEffect(profile.SelfEnmityPercentIfRecentWardHit), profile.SelfEnmityDurationSecondsIfRecentWardHit);
            }

            if (profile.SelfEnmityPercentIfTargetRecentlyDamagedActivator != 0 &&
                CombatActivity.HasRecentDamageTarget(target, activator, window))
            {
                StatusEffectApplication.ApplyStatusEffect(activator, target,
                    new ChallengeEnmityStatusEffect(profile.SelfEnmityPercentIfTargetRecentlyDamagedActivator),
                    profile.SelfEnmityDurationSecondsIfTargetRecentlyDamagedActivator);
            }
        }

        public StatusEffectCategory? ApplyTargetUsingAbilityEffects(WeaponAbilityProfile profile, uint activator, uint target, CombatDamageType damageType)
        {
            if (!CombatActivity.IsUsingAbility(target))
                return null;

            if (profile.TargetUsingAbilityDrainStamina > 0)
                Stat.ReduceStamina(target, profile.TargetUsingAbilityDrainStamina);
            if (profile.TargetUsingAbilityDrainFP > 0)
                Stat.ReduceFP(target, profile.TargetUsingAbilityDrainFP);

            if (profile.TargetUsingAbilityStatusEffect == null || profile.TargetUsingAbilityStatusDurationSeconds <= 0)
                return null;

            return WeaponAbilityStatusRiders.GetAppliedCategories(
                StatusEffectApplication.ApplyStatusEffect(
                    activator,
                    target,
                    profile.TargetUsingAbilityStatusEffect,
                    profile.TargetUsingAbilityStatusDurationSeconds,
                    damageType),
                profile.TargetUsingAbilityStatusEffect);
        }
    }
}
