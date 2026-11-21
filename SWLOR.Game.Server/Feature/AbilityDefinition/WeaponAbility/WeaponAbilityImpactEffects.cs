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
    /// What happens after the last hit of a weapon ability: resource restores, healing, haste and critical-hit self effects.
    /// </summary>
    public class WeaponAbilityImpactEffects
    {
        private ICombatDefenseMemoryService CombatDefenseMemory { get; }
        private ICombatResourcesService CombatResources { get; }
        private IPlayerFeedbackService PlayerFeedback { get; }
        private IStatService Stat { get; }
        private IStatusEffectApplicationService StatusEffectApplication { get; }
        private ITemporaryStatModifierService TemporaryStatModifier { get; }
        private WeaponAbilityDamageModifiers WeaponAbilityDamageModifiers { get; }
        private WeaponAbilitySelfEffects WeaponAbilitySelfEffects { get; }

        public WeaponAbilityImpactEffects(ICombatDefenseMemoryService combatDefenseMemory, ICombatResourcesService combatResources, IPlayerFeedbackService playerFeedback, IStatService stat, IStatusEffectApplicationService statusEffectApplication, ITemporaryStatModifierService temporaryStatModifier, WeaponAbilityDamageModifiers weaponAbilityDamageModifiers, WeaponAbilitySelfEffects weaponAbilitySelfEffects)
        {
            CombatDefenseMemory = combatDefenseMemory;
            CombatResources = combatResources;
            PlayerFeedback = playerFeedback;
            Stat = stat;
            StatusEffectApplication = statusEffectApplication;
            TemporaryStatModifier = temporaryStatModifier;
            WeaponAbilityDamageModifiers = weaponAbilityDamageModifiers;
            WeaponAbilitySelfEffects = weaponAbilitySelfEffects;
        }

        public void RestoreResourcesForTargetCount(WeaponAbilityProfile profile, uint activator, AbilityImpactSummary summary)
        {
            if (profile.RestoreStaminaIfMinimumTargetsHit > 0 && profile.StaminaRestoreMinimumTargets > 0 &&
                (summary?.ImpactedTargetCount ?? 0) >= profile.StaminaRestoreMinimumTargets)
            {
                CombatResources.RestoreAbilityHitStamina(activator, summary.Ability, profile.RestoreStaminaIfMinimumTargetsHit);
            }
        }

        public void AfterImpact(
            WeaponAbilityProfile profile,
            uint activator,
            int totalDamage,
            int successfulHitCount = 0,
            AbilityImpactSummary summary = null,
            bool clearActivationIdleBonusSnapshots = true)
        {
            if (clearActivationIdleBonusSnapshots)
                WeaponAbilityDamageModifiers.ClearActivationIdleBonusSnapshots(activator);

            if (profile.SelfStatusEffectOnCriticalHitFactory != null &&
                (profile.SelfStatusEffectOnCriticalHitDurationSeconds > 0 ||
                 profile.SelfStatusEffectOnCriticalHitIsPermanent) &&
                (summary?.CriticalHitCount ?? 0) > 0)
            {
                var durationSeconds = profile.SelfStatusEffectOnCriticalHitIsPermanent
                    ? 0f
                    : profile.SelfStatusEffectOnCriticalHitDurationSeconds;
                StatusEffectApplication.ApplyStatusEffect(
                    activator,
                    activator,
                    profile.SelfStatusEffectOnCriticalHitFactory(summary),
                    durationSeconds);
            }

            // Keyed off landing the cone, not off dealing damage: a target that resists the
            // hit down to zero still counts as struck, and the authored restore does not
            // depend on damage. This has to run before the damage-only early return.
            if (profile.RestoreFPAfterRangedDeflection > 0 &&
                profile.RestoreFPAfterRangedDeflectionWindowSeconds > 0 &&
                (summary?.ImpactedTargetCount ?? 0) > 0 &&
                CombatDefenseMemory.HasRecentDeflection(
                    activator,
                    DeflectionSource.Ranged,
                    profile.RestoreFPAfterRangedDeflectionWindowSeconds))
            {
                if (Stat.RestoreFP(activator, profile.RestoreFPAfterRangedDeflection) > 0)
                    CombatResources.ApplyAbilityRestoredFPEffects(activator);
            }

            if (profile.RestoreStaminaIfAnyCriticalHit > 0 && (summary?.CriticalHitCount ?? 0) > 0)
            {
                var restored = CombatResources.RestoreAbilityHitStamina(activator, summary.Ability, profile.RestoreStaminaIfAnyCriticalHit,
                    sendFeedback: string.IsNullOrWhiteSpace(profile.RestoreStaminaIfAnyCriticalHitFeedbackLabel));
                if (restored > 0 &&
                    GetIsPC(activator) &&
                    !string.IsNullOrWhiteSpace(profile.RestoreStaminaIfAnyCriticalHitFeedbackLabel))
                {
                    PlayerFeedback.ShowDiagnosticFloatingText(
                        ColorToken.Combat($"{profile.RestoreStaminaIfAnyCriticalHitFeedbackLabel} restored {restored} STM"),
                        activator,
                        false);
                }
            }
            if (totalDamage <= 0)
            {
                if ((summary?.ImpactedTargetCount ?? 0) > 0)
                    WeaponAbilitySelfEffects.ApplyLandedImpactModifiers(profile, activator);
                return;
            }

            if (profile.HealPercentOfDamage > 0)
            {
                var amount = GameMath.PercentOf(totalDamage, profile.HealPercentOfDamage);
                if (profile.HealMaximum > 0)
                    amount = Math.Min(amount, profile.HealMaximum);
                ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), activator);
            }

            if (profile.RestoreStaminaAfterImpact > 0)
                CombatResources.RestoreAbilityHitStamina(activator, summary?.Ability, profile.RestoreStaminaAfterImpact);
            if (profile.RestoreFPAfterImpact > 0)
            {
                if (Stat.RestoreFP(activator, profile.RestoreFPAfterImpact) > 0)
                    CombatResources.ApplyAbilityRestoredFPEffects(activator);
            }
            if (profile.HitCount > 1 && successfulHitCount >= profile.HitCount)
            {
                if (profile.RestoreStaminaIfAllHitsLand > 0)
                    CombatResources.RestoreAbilityHitStamina(activator, summary?.Ability, profile.RestoreStaminaIfAllHitsLand);
                if (profile.RestoreFPIfAllHitsLand > 0)
                {
                    if (Stat.RestoreFP(activator, profile.RestoreFPIfAllHitsLand) > 0)
                        CombatResources.ApplyAbilityRestoredFPEffects(activator);
                }
                if (profile.SelfHastePercentIfAllHitsLand > 0 && profile.SelfHasteDurationSecondsIfAllHitsLand > 0)
                {
                    TemporaryStatModifier.AddCapped(
                        activator,
                        StatType.AttackDelayReductionPercent,
                        profile.SelfHastePercentIfAllHitsLand,
                        profile.SelfHasteDurationSecondsIfAllHitsLand,
                        profile.SelfHasteMaximumPercentIfAllHitsLand > 0 ? profile.SelfHasteMaximumPercentIfAllHitsLand : profile.SelfHastePercentIfAllHitsLand,
                        StatType.AttackDelayReductionPercent,
                        1);
                }
            }

            WeaponAbilitySelfEffects.ApplyLandedImpactModifiers(profile, activator);
        }
    }
}
