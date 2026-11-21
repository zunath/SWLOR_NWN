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
    /// Writes the temporary stat modifiers an ability's profile asks for, on the caster and on the target.
    /// </summary>
    public class WeaponAbilityTemporaryModifiers
    {
        private ITemporaryStatModifierService TemporaryStatModifier { get; }

        public WeaponAbilityTemporaryModifiers(ITemporaryStatModifierService temporaryStatModifier)
        {
            TemporaryStatModifier = temporaryStatModifier;
        }

        public void ReplaceTemporary(uint activator, StatType statType, int amount, int durationSeconds)
        {
            if (amount == 0)
                return;

            TemporaryStatModifier.Replace(activator, statType, amount, durationSeconds, $"GeneratedWeaponAbility:{statType}");
        }

        public void ReplaceTemporaryTarget(uint target, StatType statType, int amount, int durationSeconds)
        {
            if (amount == 0 || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(target, statType, amount, durationSeconds, $"GeneratedWeaponAbilityTarget:{statType}");
        }

        private void ReplaceTemporaryPayload(WeaponAbilityProfile profile, uint activator, int durationSeconds, params (StatType Stat, int Amount)[] stats)
        {
            if (durationSeconds <= 0 || !stats.Any(stat => stat.Amount != 0))
                return;

            foreach (var (stat, amount) in stats)
                TemporaryStatModifier.Replace(activator, stat, amount, durationSeconds, profile.ModifierSource);
        }

        public void ApplyTemporaryDefeatedEnemyModifiers(WeaponAbilityProfile profile, uint activator)
        {
            var duration = profile.TemporaryDefeatedEnemyEffectDurationSeconds;
            if (duration <= 0)
                return;

            ReplaceTemporary(activator, StatType.DefeatedEnemyStaminaRestore, profile.TemporaryDefeatedEnemyStaminaRestore, duration);
            ReplaceTemporary(activator, StatType.DefeatedEnemyAttackDelayReductionPercent, profile.TemporaryDefeatedEnemyAttackDelayReductionPercent, duration);
            ReplaceTemporary(
                activator,
                StatType.DefeatedEnemyAttackDelayReductionDurationSeconds,
                profile.TemporaryDefeatedEnemyAttackDelayReductionDurationSeconds,
                duration);
            ReplaceTemporary(activator, StatType.HostileAbilityFPRestore, profile.TemporaryHostileAbilityFPRestore, duration);
            ReplaceTemporary(activator, StatType.HostileAbilityStaminaRestore, profile.TemporaryHostileAbilityStaminaRestore, duration);
            ReplaceTemporaryPayload(profile, activator, duration,
                (StatType.HighFPAndStaminaAbilityDamageBonus, profile.TemporaryHighFPAndStaminaAbilityDamageBonus),
                (StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, profile.TemporaryHighFPAndStaminaAbilityDamageBonusThresholdPercent));
            ReplaceTemporary(
                activator,
                StatType.FrenzySlashHasteRefreshDurationSeconds,
                profile.TemporaryFrenzySlashHasteRefreshDurationSeconds,
                duration);
            ReplaceTemporary(
                activator,
                StatType.AvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds,
                profile.TemporaryAvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds,
                duration);
            ReplaceTemporary(
                activator,
                StatType.AvoidedAttackNextAutoAttackNoDelaySkillType,
                profile.TemporaryAvoidedAttackNextAutoAttackNoDelaySkillType,
                duration);
            ReplaceTemporary(
                activator,
                StatType.AvoidedAttackNextAutoAttackNoDelayDurationSeconds,
                profile.TemporaryAvoidedAttackNextAutoAttackNoDelayDurationSeconds,
                duration);
            ReplaceTemporary(
                activator,
                StatType.RangedHitSuppressionStackDurationSeconds,
                profile.TemporaryRangedHitSuppressionStackDurationSeconds,
                duration);
            ReplaceTemporary(
                activator,
                StatType.RangedHitSuppressionStackEvasionPenaltyPercent,
                profile.TemporaryRangedHitSuppressionStackEvasionPenaltyPercent,
                duration);
            if (profile.TemporaryAreaAbilityFragmentationDamage > 0)
            {
                // Replacing with Invalid clears any previous skill restriction for a global buff.
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.AreaAbilityFragmentationSkillType,
                    (int)profile.TemporaryAreaAbilityFragmentationSkillType,
                    duration,
                    $"GeneratedWeaponAbility:{StatType.AreaAbilityFragmentationSkillType}");
            }
            ReplaceTemporary(activator, StatType.AreaAbilityFragmentationDamage, profile.TemporaryAreaAbilityFragmentationDamage, duration);
            ReplaceTemporaryPayload(profile, activator, duration,
                (StatType.AreaAbilityPulseDamage, profile.TemporaryAreaAbilityPulseDamage),
                (StatType.AreaAbilityPulseRadiusMeters, profile.TemporaryAreaAbilityPulseRadiusMeters),
                (StatType.AreaAbilityUsedFPRestore, profile.TemporaryAreaAbilityUsedFPRestore),
                (StatType.AreaAbilityUsedAttackDeflection, profile.TemporaryAreaAbilityUsedAttackDeflection),
                (StatType.AreaAbilityUsedAttackDeflectionDurationSeconds, profile.TemporaryAreaAbilityUsedAttackDeflectionDurationSeconds));
            ReplaceTemporary(
                activator,
                StatType.AreaAbilityFragmentationDurationSeconds,
                profile.TemporaryAreaAbilityFragmentationDurationSeconds,
                duration);
            ReplaceTemporary(
                activator,
                StatType.AreaAbilityFragmentationPulseSeconds,
                profile.TemporaryAreaAbilityFragmentationPulseSeconds,
                duration);
            ReplaceTemporary(
                activator,
                StatType.AreaAbilityMinTargetsResourceRestoreThreshold,
                profile.TemporaryAreaAbilityMinTargetsResourceRestoreThreshold,
                duration);
            ReplaceTemporary(activator, StatType.AreaAbilityFPRestore, profile.TemporaryAreaAbilityFPRestore, duration);
            ReplaceTemporary(
                activator,
                StatType.AreaAbilityMinTargetsBuffThreshold,
                profile.TemporaryAreaAbilityMinTargetsBuffThreshold,
                duration);
            ReplaceTemporary(
                activator,
                StatType.AreaAbilityAttackDeflection,
                profile.TemporaryAreaAbilityAttackDeflection,
                duration);
            ReplaceTemporary(
                activator,
                StatType.AreaAbilityBuffDurationSeconds,
                profile.TemporaryAreaAbilityBuffDurationSeconds,
                duration);
            ReplaceTemporary(activator, StatType.StatusAppliedRequiredCategory, profile.TemporaryStatusAppliedRequiredCategory, duration);
            ReplaceTemporary(
                activator,
                StatType.StatusAppliedSelfAttackDeflection,
                profile.TemporaryStatusAppliedSelfAttackDeflection,
                duration);
            ReplaceTemporary(
                activator,
                StatType.StatusAppliedSelfDurationSeconds,
                profile.TemporaryStatusAppliedSelfDurationSeconds,
                duration);
            ReplaceTemporary(
                activator,
                StatType.StatusAppliedSelfStaminaRestore,
                profile.TemporaryStatusAppliedSelfStaminaRestore,
                duration);
            ReplaceTemporary(
                activator,
                StatType.CostlyAbilityStatusSkillType,
                profile.TemporaryCostlyAbilityStatusSkillType,
                duration);
            ReplaceTemporary(
                activator,
                StatType.CostlyAbilityStatusMinimumStaminaCost,
                profile.TemporaryCostlyAbilityStatusMinimumStaminaCost,
                duration);
            ReplaceTemporary(
                activator,
                StatType.CostlyAbilityExposedDurationSeconds,
                profile.TemporaryCostlyAbilityExposedDurationSeconds,
                duration);
        }
    }
}
