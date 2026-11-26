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
    /// What an ability gives its caster: hit point cost, invisibility, immunity, party status, haste and the self modifiers.
    /// </summary>
    public class WeaponAbilitySelfEffects
    {
        private IAbilityImmunityService AbilityImmunity { get; }
        private ICombatAbilityCostEffectsService CombatAbilityCostEffects { get; }
        private ICombatDamageTakenEffectsService CombatDamageTakenEffects { get; }
        private ICombatRecorderService CombatRecorder { get; }
        private ICombatResourcesService CombatResources { get; }
        private IEnmityAttackService EnmityAttack { get; }
        private StatusAbilityEffects StatusAbilityEffects { get; }
        private IStatusEffectApplicationService StatusEffectApplication { get; }
        private ITemporaryStatModifierService TemporaryStatModifier { get; }
        private WeaponAbilityTemporaryModifiers WeaponAbilityTemporaryModifiers { get; }

        public WeaponAbilitySelfEffects(IAbilityImmunityService abilityImmunity, ICombatAbilityCostEffectsService combatAbilityCostEffects, ICombatDamageTakenEffectsService combatDamageTakenEffects, ICombatRecorderService combatRecorder, ICombatResourcesService combatResources, IEnmityAttackService enmityAttack, StatusAbilityEffects statusAbilityEffects, IStatusEffectApplicationService statusEffectApplication, ITemporaryStatModifierService temporaryStatModifier, WeaponAbilityTemporaryModifiers weaponAbilityTemporaryModifiers)
        {
            AbilityImmunity = abilityImmunity;
            CombatAbilityCostEffects = combatAbilityCostEffects;
            CombatDamageTakenEffects = combatDamageTakenEffects;
            CombatRecorder = combatRecorder;
            CombatResources = combatResources;
            EnmityAttack = enmityAttack;
            StatusAbilityEffects = statusAbilityEffects;
            StatusEffectApplication = statusEffectApplication;
            TemporaryStatModifier = temporaryStatModifier;
            WeaponAbilityTemporaryModifiers = weaponAbilityTemporaryModifiers;
        }

        public void SpendHitPoints(WeaponAbilityProfile profile, uint activator)
        {
            if (profile.HPCostPercent <= 0)
                return;

            var currentHP = GetCurrentHitPoints(activator);
            var hpCost = GameMath.PercentOf(currentHP, profile.HPCostPercent);
            hpCost = Math.Min(hpCost, Math.Max(0, currentHP - 1));
            if (hpCost <= 0)
                return;

            if (CombatRecorder.IsRecording)
                CombatRecorder.ExpectHitPointCost(activator, hpCost);
            AssignCommand(activator, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(hpCost), activator));
            CombatAbilityCostEffects.ApplyHitPointSpendAbilityEffects(activator, hpCost);
        }

        public void AfterActivation(WeaponAbilityProfile profile, uint activator, SkillType skillType)
        {
            ApplyNearbyPartyStatus(profile, activator);
            ApplySelfImmunity(profile, activator);
            ApplySelfModifiers(profile, activator);
            WeaponAbilityTemporaryModifiers.ApplyTemporaryDefeatedEnemyModifiers(profile, activator);
        }

        public void AfterHostileActivation(WeaponAbilityProfile profile, uint activator)
        {
            ApplySelfInvisibility(profile, activator);
            if (profile.ApplySelfModifiersOnHostileActivation)
                ApplySelfModifiers(profile, activator);
        }

        private void ApplySelfInvisibility(WeaponAbilityProfile profile, uint activator)
        {
            if (profile.SelfInvisibilityDurationSeconds <= 0)
                return;

            ApplyEffectToObject(
                DurationType.Temporary,
                EffectInvisibility(InvisibilityType.Normal),
                activator,
                profile.SelfInvisibilityDurationSeconds);
            EnmityAttack.ReevaluateEnemyAttackTargets(activator);
        }

        private void ApplyNearbyPartyStatus(WeaponAbilityProfile profile, uint activator)
        {
            if (profile.NearbyPartyStatusEffect == null || profile.NearbyPartyStatusDurationSeconds <= 0)
                return;

            StatusAbilityEffects.ApplyStatusToNearbyParty(
                activator,
                profile.NearbyPartyStatusEffect,
                profile.NearbyPartyStatusDurationSeconds,
                profile.NearbyPartyStatusIncludesSelf);
        }

        private void ApplySelfImmunity(WeaponAbilityProfile profile, uint activator)
        {
            if (profile.SelfKnockdownDazedImmunityDurationSeconds <= 0)
                return;

            AbilityImmunity.ApplyTemporaryImmunity(activator, profile.SelfKnockdownDazedImmunityDurationSeconds, ImmunityType.Knockdown);
            AbilityImmunity.ApplyTemporaryImmunity(activator, profile.SelfKnockdownDazedImmunityDurationSeconds, ImmunityType.Dazed);
        }

        private void ApplySelfModifiers(WeaponAbilityProfile profile, uint activator)
        {
            if (profile.SelfHastePercent > 0 && profile.SelfHasteDurationSeconds > 0)
            {
                var max = profile.SelfHasteMaximumPercent > 0 ? profile.SelfHasteMaximumPercent : profile.SelfHastePercent;
                TemporaryStatModifier.AddCapped(
                    activator,
                    StatType.AttackDelayReductionPercent,
                    profile.SelfHastePercent,
                    profile.SelfHasteDurationSeconds,
                    max,
                    StatType.AttackDelayReductionPercent,
                    1);
            }

            var duration = profile.SelfStatDurationSeconds;
            if (duration <= 0)
                return;

            if (profile.SelfStatResourceAboveThresholdPercent > 0 &&
                !CombatResources.IsCurrentFPAndStaminaAbovePercent(activator, profile.SelfStatResourceAboveThresholdPercent))
            {
                return;
            }

            if (profile.SelfStatusEffectFactory != null)
            {
                StatusEffectApplication.ApplyStatusEffect(
                    activator,
                    activator,
                    profile.SelfStatusEffectFactory(),
                    duration);
                return;
            }

            WeaponAbilityTemporaryModifiers.ReplaceTemporary(activator, StatType.AttackPercentAdjustment, profile.SelfAttackPercent, duration);
            WeaponAbilityTemporaryModifiers.ReplaceTemporary(activator, StatType.AccuracyPercentAdjustment, profile.SelfAccuracyPercent, duration);
            WeaponAbilityTemporaryModifiers.ReplaceTemporary(activator, StatType.EvasionPercentAdjustment, profile.SelfEvasionPercent, duration);
            WeaponAbilityTemporaryModifiers.ReplaceTemporary(activator, StatType.PhysicalDefensePercentAdjustment, profile.SelfDefensePercent, duration);
            WeaponAbilityTemporaryModifiers.ReplaceTemporary(activator, StatType.ForceDefensePercentAdjustment, profile.SelfForceDefensePercent, duration);
            WeaponAbilityTemporaryModifiers.ReplaceTemporary(activator, StatType.MeleeDeflection, profile.SelfMeleeDeflection, duration);
            if (profile.SelfMeleeDeflection != 0)
                CombatDamageTakenEffects.ApplyAbilityGrantedAttackDeflectionEffects(activator, DeflectionSource.Melee);
            WeaponAbilityTemporaryModifiers.ReplaceTemporary(activator, StatType.RangedDeflection, profile.SelfRangedDeflection, duration);
            if (profile.SelfRangedDeflection != 0)
                CombatDamageTakenEffects.ApplyAbilityGrantedAttackDeflectionEffects(activator, DeflectionSource.Ranged);
            WeaponAbilityTemporaryModifiers.ReplaceTemporary(activator, StatType.CriticalRatePercentAdjustment, profile.SelfCriticalRatePercent, duration);
        }

        public void ApplyLandedImpactModifiers(WeaponAbilityProfile profile, uint activator)
        {
            ApplyNearbyPartyStatus(profile, activator);
            ApplySelfImmunity(profile, activator);
            if (!profile.ApplySelfModifiersOnHostileActivation)
                ApplySelfModifiers(profile, activator);
            WeaponAbilityTemporaryModifiers.ApplyTemporaryDefeatedEnemyModifiers(profile, activator);
        }
    }
}
