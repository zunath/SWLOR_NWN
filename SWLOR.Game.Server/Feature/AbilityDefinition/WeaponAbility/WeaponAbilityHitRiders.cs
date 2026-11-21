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
    /// What each landed hit of a weapon ability triggers: resource and status riders, source status changes and defense ignore.
    /// </summary>
    public class WeaponAbilityHitRiders
    {
        private ICombatResourcesService CombatResources { get; }
        private IStatService Stat { get; }
        private IStatusEffectService StatusEffect { get; }
        private IStatusEffectApplicationService StatusEffectApplication { get; }
        private IStatusEffectDurationService StatusEffectDuration { get; }
        private ITemporaryStatModifierService TemporaryStatModifier { get; }
        private WeaponAbilityDamageModifiers WeaponAbilityDamageModifiers { get; }
        private WeaponAbilityHitEffects WeaponAbilityHitEffects { get; }
        private WeaponAbilityStatusRiders WeaponAbilityStatusRiders { get; }
        private IAbilityImpactState IAbilityImpactState { get; }

        public WeaponAbilityHitRiders(ICombatResourcesService combatResources, IStatService stat, IStatusEffectService statusEffect, IStatusEffectApplicationService statusEffectApplication, IStatusEffectDurationService statusEffectDuration, ITemporaryStatModifierService temporaryStatModifier, WeaponAbilityDamageModifiers weaponAbilityDamageModifiers, WeaponAbilityHitEffects weaponAbilityHitEffects, WeaponAbilityStatusRiders weaponAbilityStatusRiders, IAbilityImpactState abilityImpactState)
        {
            CombatResources = combatResources;
            Stat = stat;
            StatusEffect = statusEffect;
            StatusEffectApplication = statusEffectApplication;
            StatusEffectDuration = statusEffectDuration;
            TemporaryStatModifier = temporaryStatModifier;
            WeaponAbilityDamageModifiers = weaponAbilityDamageModifiers;
            WeaponAbilityHitEffects = weaponAbilityHitEffects;
            WeaponAbilityStatusRiders = weaponAbilityStatusRiders;
            IAbilityImpactState = abilityImpactState;
        }

        public void AfterSuccessfulHit(WeaponAbilityProfile profile, uint activator, uint target, CombatDamageType damageType, WeaponAbilityProfile.StatusSpreadSnapshot spreadSnapshot)
        {
            if (profile.ClearTargetActionsOnHit)
            {
                ActionInterruption.ClearActions(target);
                UsePerkFeat.InterruptAbilityActivationFromHit(target);
            }

            if (profile.RestoreStaminaOnHit > 0)
                CombatResources.RestoreAbilityHitStamina(activator, IAbilityImpactState.GetActiveAbilityImpactSummary(activator)?.Ability, profile.RestoreStaminaOnHit);
            if (profile.RestoreFPOnHit > 0)
            {
                if (Stat.RestoreFP(activator, profile.RestoreFPOnHit) > 0)
                    CombatResources.ApplyAbilityRestoredFPEffects(activator);
            }
            if (profile.DrainStaminaOnHit > 0)
                Stat.ReduceStamina(target, profile.DrainStaminaOnHit);
            if (profile.DrainFPOnHit > 0)
                Stat.ReduceFP(target, profile.DrainFPOnHit);

            WeaponAbilityHitEffects.ApplyResourceConditionalRestore(profile, activator);
            WeaponAbilityHitEffects.ApplyResourceConditionalDrain(profile, activator, target);
            WeaponAbilityHitEffects.ApplyTargetTemporaryModifiers(profile, target);
            WeaponAbilityHitEffects.ApplyProtectedTargetHitSelfModifiers(profile, activator, target);
            WeaponAbilityStatusRiders.ApplyStatusSpread(profile, activator, target, damageType, spreadSnapshot);
            WeaponAbilityStatusRiders.ApplySuppressionEffects(profile, activator, target, damageType);
            ApplyDefenseIgnoreHitEffects(profile, activator, target);
            ConsumeSourceStatusEffects(profile, activator, target);

            if (profile.ConsumeBleedIntoHemorrhage &&
                StatusEffect.HasStatusEffect(target, typeof(BleedStatusEffect)))
            {
                StatusEffect.RemoveStatusEffect(target, typeof(BleedStatusEffect), false);
                StatusEffectApplication.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(HemorrhageStatusEffect),
                    profile.HemorrhageDurationSeconds > 0 ? profile.HemorrhageDurationSeconds : 30f,
                    damageType);
            }
        }

        /// <summary>
        /// Runs on a successful hit before the shared impact riders. Returns the categories of the
        /// conditional statuses it applied (null when none landed), so they count like the impact's own status.
        /// </summary>
        public StatusEffectCategory? BeforeSuccessfulImpactRiders(WeaponAbilityProfile profile, uint activator, uint target, CombatDamageType damageType)
        {
            ExtendSourceStatusEffects(profile, activator, target);

            StatusEffectCategory? appliedCategories = null;
            void Track(StatusEffectCategory? categories)
            {
                if (categories is { } applied)
                    appliedCategories = (appliedCategories ?? StatusEffectCategory.None) | applied;
            }

            Track(WeaponAbilityHitEffects.ApplyTargetUsingAbilityEffects(profile, activator, target, damageType));
            Track(WeaponAbilityStatusRiders.ApplyConditionalStatus(profile, activator, target, damageType));
            Track(WeaponAbilityStatusRiders.ApplyTargetConditionalStatus(profile, activator, target, damageType));
            return appliedCategories;
        }

        private void ExtendSourceStatusEffects(WeaponAbilityProfile profile, uint activator, uint target)
        {
            if (profile.SourceStatusExtensionSeconds <= 0 || profile.SourceStatusEffectsToExtend == null)
                return;

            var extendedCount = 0;
            foreach (var statusEffectType in profile.SourceStatusEffectsToExtend)
            {
                if (statusEffectType == null)
                    continue;

                if (StatusEffectDuration.ExtendStatusEffectDuration(
                    target,
                    statusEffectType,
                    activator,
                    profile.SourceStatusExtensionSeconds))
                {
                    extendedCount++;
                }
            }

            if (extendedCount > 0 && GetIsPC(activator))
            {
                var statusLabel = extendedCount == 1 ? "status" : "statuses";
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Extended {extendedCount} {statusLabel} by {profile.SourceStatusExtensionSeconds}s"),
                    activator,
                    false);
            }
        }

        private void ConsumeSourceStatusEffects(WeaponAbilityProfile profile, uint activator, uint target)
        {
            if (!profile.ConsumeSourceStatusEffectsOnHit)
                return;

            var consumed = false;
            if (profile.ExtraDamageSourceStatusEffect != null &&
                StatusEffect.HasStatusEffect(target, profile.ExtraDamageSourceStatusEffect, activator))
            {
                StatusEffect.RemoveStatusEffect(target, profile.ExtraDamageSourceStatusEffect, activator, false);
                consumed = true;
            }

            if (profile.ExtraDamageSourceStackStatusEffect != null &&
                StatusEffect.HasStatusEffect(target, profile.ExtraDamageSourceStackStatusEffect, activator))
            {
                StatusEffect.RemoveStatusEffect(target, profile.ExtraDamageSourceStackStatusEffect, activator, false);
                consumed = true;
            }

            if (consumed)
            {
                SendMessageToPC(activator, "You consume your Venom and Infection setup.");
            }
        }

        private void ApplyDefenseIgnoreHitEffects(WeaponAbilityProfile profile, uint activator, uint target)
        {
            if (WeaponAbilityDamageModifiers.GetDefenseIgnorePercent(profile, activator) <= 0)
                return;

            var physicalDefense = Stat.GetStatAdjustment(
                activator,
                StatType.DefenseIgnoreHitPhysicalDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.DefenseIgnoreHitPhysicalDefenseDurationSeconds);
            if (physicalDefense == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                target,
                StatType.PhysicalDefensePercentAdjustment,
                physicalDefense,
                duration,
                StatType.DefenseIgnoreHitPhysicalDefensePercentAdjustment);
        }
    }
}
