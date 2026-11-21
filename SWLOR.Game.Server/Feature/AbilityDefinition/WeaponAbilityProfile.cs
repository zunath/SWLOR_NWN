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
    public sealed class WeaponAbilityProfile
    {
        private readonly string _modifierSource = $"ability:{Guid.NewGuid()}";

    /// <summary>The source key of the temporary modifiers this profile writes; internal so it is not one of the profile's settings.</summary>
    internal string ModifierSource => _modifierSource;

        public sealed class StatusSpreadSnapshot
        {
            private readonly Dictionary<uint, (bool Bleeding, bool Sundered)> _sourceStatuses = new();
            private readonly int _maximumSpreads;
            private int _successfulSpreads;

            public StatusSpreadSnapshot(int maximumSpreads = 0)
            {
                _maximumSpreads = maximumSpreads;
            }

            public bool TrySpread(Func<bool> applySpread)
            {
                if (_maximumSpreads > 0 && _successfulSpreads >= _maximumSpreads)
                    return false;

                if (!applySpread())
                    return false;

                _successfulSpreads++;
                return true;
            }

            public (bool Bleeding, bool Sundered) Capture(uint target)
            {
                if (!_sourceStatuses.TryGetValue(target, out var statuses))
                {
                    statuses = GameServices.WeaponAbilityStatusRiders.ReadSpreadSource(target);
                    _sourceStatuses.Add(target, statuses);
                }

                return statuses;
            }
        }

        public sealed class ActivationIdleBonusSnapshot
        {
            public bool HasSnapshot { get; init; }
            public int DamageBonus { get; init; }
            public int CriticalRatePercentAdjustment { get; init; }
            public int DefenseIgnorePercent { get; init; }
        }

        public static readonly WeaponAbilityProfile Empty = new();

        public int HitCount { get; init; } = 1;
        public bool IsQueuedWeaponAbility { get; init; }
        public CombatDamageType DamageType { get; init; } = CombatDamageType.Physical;
        public int MaximumAreaTargets { get; init; }

        /// <summary>
        /// How long the pre-cast telegraph is shown before an area impact lands, in seconds.
        /// Leave at 0 for abilities the Bible marks "Instant" — they gate no damage and instead
        /// get the visual-only impact flash from <see cref="Domain.Abilities.IAbilityService.ApplyTelegraphedCombatImpact"/>.
        /// Only set this to match a Bible-granted casting time.
        /// </summary>
        public float TelegraphDuration { get; init; }
        public int ExtraDamageIfRecentTarget { get; init; }
        public float RecentTargetWindowSeconds { get; init; }
        public int ExtraDamageIfHighResources { get; init; }
        public int HighResourceExtraDamageThresholdPercent { get; init; }
        public int ExtraDamageIfBesideOrBehind { get; init; }
        public int ExtraDamageIfIdle { get; init; }
        public string ExtraDamageIfIdleFeedbackLabel { get; init; }
        public int CriticalRateIfIdle { get; init; }
        public string CriticalRateIfIdleFeedbackLabel { get; init; }
        public int CriticalRateIfNotRecentTarget { get; init; }
        public string CriticalRateIfNotRecentTargetFeedbackLabel { get; init; }
        public int DefenseIgnoreIfIdle { get; init; }
        public float IdleWindowSeconds { get; init; }
        public float NotRecentTargetWindowSeconds { get; init; }
        public int ExtraDamageIfTargetBleeding { get; init; }
        public int ExtraDamageIfTargetDebuffed { get; init; }
        public int ExtraDamageIfTargetControlled { get; init; }
        public int ExtraDamageIfTargetLowHP { get; init; }
        public Type ExtraDamageTargetStatusEffect { get; init; }
        public int ExtraDamageIfTargetStatusEffect { get; init; }
        public int ExtraDamageIfBehind { get; init; }
        public string ExtraDamageIfBehindFeedbackLabel { get; init; }
        public Type ExtraDamageSourceStatusEffect { get; init; }
        public int ExtraDamageIfSourceStatusEffect { get; init; }
        public Type ExtraDamageSourceStackStatusEffect { get; init; }
        public int ExtraDamagePerSourceStack { get; init; }
        public int ExtraDamageIfRecentGuardedHit { get; init; }
        public float RecentGuardedHitWindowSeconds { get; init; }
        /// <summary>
        /// Status the impact inflicts as the ability's own status when you guarded a hit within
        /// <see cref="RecentGuardedHitWindowSeconds"/>. Status-category perks (Control riders,
        /// control damage and duration bonuses) then treat it like a status the ability always applies.
        /// </summary>
        public Type RecentGuardedHitStatusEffect { get; init; }
        public int RecentGuardedHitStatusDurationSeconds { get; init; }
        public int TargetLowHPThresholdPercent { get; init; }
        public int DamagePercentIfTargetDebuffed { get; init; }
        public int DamagePercentIfTargetControlled { get; init; }
        public int DamagePercentIfTargetLowHP { get; init; }
        public int CriticalRateIfTargetDebuffedOrControlled { get; init; }
        public int DefenseIgnorePercent { get; init; }
        public int HitChancePercentAdjustment { get; init; }
        public int CriticalRatePercentAdjustment { get; init; }
        public int EnmityBonus { get; init; }
        public int HPCostPercent { get; init; }
        public int HealPercentOfDamage { get; init; }
        public int HealMaximum { get; init; }
        public int RestoreStaminaOnHit { get; init; }
        public int RestoreFPOnHit { get; init; }
        public int RestoreStaminaAfterImpact { get; init; }
        public int RestoreStaminaIfMinimumTargetsHit { get; init; }
        public int StaminaRestoreMinimumTargets { get; init; }
        public int RestoreFPAfterImpact { get; init; }

        /// <summary>
        /// FP restored once per cast when the caster's Ranged Deflection negated a ranged
        /// weapon auto-attack within
        /// <see cref="RestoreFPAfterRangedDeflectionWindowSeconds"/>. This is deliberately a
        /// per-cast payload rather than a per-hit one: an authored "restore N FP" on an area
        /// ability must not pay out once per struck target.
        /// </summary>
        public int RestoreFPAfterRangedDeflection { get; init; }

        /// <summary>Deflection lookback window for <see cref="RestoreFPAfterRangedDeflection"/>.</summary>
        public int RestoreFPAfterRangedDeflectionWindowSeconds { get; init; }
        public int RestoreStaminaIfAllHitsLand { get; init; }
        public int RestoreFPIfAllHitsLand { get; init; }
        public int RestoreStaminaIfAnyCriticalHit { get; init; }
        public string RestoreStaminaIfAnyCriticalHitFeedbackLabel { get; init; }
        public int SelfHastePercentIfAllHitsLand { get; init; }
        public int SelfHasteMaximumPercentIfAllHitsLand { get; init; }
        public int SelfHasteDurationSecondsIfAllHitsLand { get; init; }
        public int DrainStaminaOnHit { get; init; }
        public int DrainFPOnHit { get; init; }
        public int RestoreBothResourcesBelowThresholdPercent { get; init; }
        public int RestoreFPIfResourcesBelow { get; init; }
        public int RestoreStaminaIfResourcesBelow { get; init; }
        public int DrainTargetResourceAboveThresholdPercent { get; init; }
        public int DrainTargetFPIfActivatorFPAboveThreshold { get; init; }
        public int DrainTargetStaminaIfActivatorStaminaAboveThreshold { get; init; }
        public int SelfHastePercent { get; init; }
        public int SelfHasteDurationSeconds { get; init; }
        public int SelfHasteMaximumPercent { get; init; }
        public int SelfAttackPercent { get; init; }
        public int SelfAccuracyPercent { get; init; }
        public int SelfEvasionPercent { get; init; }
        public int SelfDefensePercent { get; init; }
        public int SelfForceDefensePercent { get; init; }
        public int SelfMeleeDeflection { get; init; }
        public int SelfRangedDeflection { get; init; }
        public int SelfCriticalRatePercent { get; init; }
        public int SelfStatDurationSeconds { get; init; }
        public int SelfStatResourceAboveThresholdPercent { get; init; }
        public int SelfGuardPercentIfRecentGuardedAllyHit { get; init; }
        public int SelfGuardDurationSecondsIfRecentGuardedAllyHit { get; init; }
        public int SelfEnmityPercentIfRecentWardHit { get; init; }
        public int SelfEnmityDurationSecondsIfRecentWardHit { get; init; }
        public int ProtectedTargetHitWindowSeconds { get; init; }
        public bool RequiresRecentWardHitTarget { get; init; }
        public int TemporaryDefeatedEnemyStaminaRestore { get; init; }
        public int TemporaryDefeatedEnemyAttackDelayReductionPercent { get; init; }
        public int TemporaryDefeatedEnemyAttackDelayReductionDurationSeconds { get; init; }
        public int TemporaryDefeatedEnemyEffectDurationSeconds { get; init; }
        public int TemporaryHostileAbilityFPRestore { get; init; }
        public int TemporaryHostileAbilityStaminaRestore { get; init; }
        public int TemporaryHighFPAndStaminaAbilityDamageBonus { get; init; }
        public int TemporaryHighFPAndStaminaAbilityDamageBonusThresholdPercent { get; init; }
        public int TemporaryFrenzySlashHasteRefreshDurationSeconds { get; init; }
        public int TemporaryAvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds { get; init; }
        public int TemporaryAvoidedAttackNextAutoAttackNoDelaySkillType { get; init; }
        public int TemporaryAvoidedAttackNextAutoAttackNoDelayDurationSeconds { get; init; }
        public int TemporaryRangedHitSuppressionStackDurationSeconds { get; init; }
        public int TemporaryRangedHitSuppressionStackEvasionPenaltyPercent { get; init; }
        public int TemporaryAreaAbilityFragmentationDamage { get; init; }
        public SkillType TemporaryAreaAbilityFragmentationSkillType { get; init; }
        public int TemporaryAreaAbilityFragmentationDurationSeconds { get; init; }
        public int TemporaryAreaAbilityFragmentationPulseSeconds { get; init; }
        public int TemporaryAreaAbilityPulseDamage { get; init; }
        public int TemporaryAreaAbilityPulseRadiusMeters { get; init; }
        public int TemporaryAreaAbilityUsedFPRestore { get; init; }
        public int TemporaryAreaAbilityUsedAttackDeflection { get; init; }
        public int TemporaryAreaAbilityUsedAttackDeflectionDurationSeconds { get; init; }
        public int TemporaryAreaAbilityMinTargetsResourceRestoreThreshold { get; init; }
        public int TemporaryAreaAbilityFPRestore { get; init; }
        public int TemporaryAreaAbilityMinTargetsBuffThreshold { get; init; }
        public int TemporaryAreaAbilityAttackDeflection { get; init; }
        public int TemporaryAreaAbilityBuffDurationSeconds { get; init; }
        public int TemporaryStatusAppliedRequiredCategory { get; init; }
        public int TemporaryStatusAppliedSelfAttackDeflection { get; init; }
        public int TemporaryStatusAppliedSelfDurationSeconds { get; init; }
        public int TemporaryStatusAppliedSelfStaminaRestore { get; init; }
        public Type NearbyPartyStatusEffect { get; init; }
        public int NearbyPartyStatusDurationSeconds { get; init; }
        public bool NearbyPartyStatusIncludesSelf { get; init; }
        public int SelfKnockdownDazedImmunityDurationSeconds { get; init; }
        public int TemporaryCostlyAbilityStatusSkillType { get; init; }
        public int TemporaryCostlyAbilityStatusMinimumStaminaCost { get; init; }
        public int TemporaryCostlyAbilityExposedDurationSeconds { get; init; }
        public bool ApplySuppressionStackOnHit { get; init; }
        public int SuppressionStackEvasionPenaltyPercent { get; init; }
        public int SuppressionStackDurationSeconds { get; init; }
        public int SuppressionDisorientedRequiredStacks { get; init; }
        public int SuppressionDisorientedDurationSeconds { get; init; }
        public bool ConsumeBleedIntoHemorrhage { get; init; }
        public int HemorrhageDurationSeconds { get; init; }
        public bool SpreadBleedFromTarget { get; init; }
        public int SpreadBleedDurationSeconds { get; init; }
        public bool SpreadHemorrhageFromTarget { get; init; }
        public int SpreadHemorrhageDurationSeconds { get; init; }
        public bool SpreadSunderFromTarget { get; init; }
        public int SpreadSunderDurationSeconds { get; init; }
        public int MaximumStatusSpreadsPerCast { get; init; }
        // How many nearby enemies one affected source may spread to; 0 keeps the default of one.
        public int MaximumStatusSpreadsPerSource { get; init; }
        public bool ClearTargetActionsOnHit { get; init; }
        public Type ConditionalStatusEffect { get; init; }
        public int ConditionalStatusDurationSeconds { get; init; }
        public int ConditionalStatusAfterDeflectionWindowSeconds { get; init; }
        public Type RequiredTargetStatusEffectForConditionalStatus { get; init; }
        public bool ConvertsRequiredTargetStatusEffect { get; init; }
        public StatusEffectCategory RequiredTargetStatusCategoryForConditionalStatus { get; init; }
        public bool RequireRecentGuardedAllyHitForConditionalStatus { get; init; }
        public bool RequireRecentWardHitForConditionalStatus { get; init; }
        public Type ConditionalTargetStatusEffect { get; init; }
        public int ConditionalTargetStatusDurationSeconds { get; init; }
        public bool RequireBehindForConditionalStatus { get; init; }
        public Type TargetUsingAbilityStatusEffect { get; init; }
        public int TargetUsingAbilityStatusDurationSeconds { get; init; }
        public int TargetUsingAbilityDrainStamina { get; init; }
        public int TargetUsingAbilityDrainFP { get; init; }
        public int TargetAttackPercent { get; init; }
        public int TargetAttackDurationSeconds { get; init; }
        public int TargetAbilityHitChancePercent { get; init; }
        public int TargetAbilityHitChanceDurationSeconds { get; init; }
        public Func<IStatusEffect> StatusEffectFactory { get; init; }
        public Type SourceOwnedStatusEffectTypeRemovedOnPerkRefund { get; init; }
        public Func<IStatusEffect> SelfStatusEffectFactory { get; init; }
        public bool ApplySelfModifiersOnHostileActivation { get; init; }
        public Func<AbilityImpactSummary, IStatusEffect> SelfStatusEffectOnCriticalHitFactory { get; init; }
        public int SelfStatusEffectOnCriticalHitDurationSeconds { get; init; }
        public bool SelfStatusEffectOnCriticalHitIsPermanent { get; init; }
        public Type[] SelfStatusEffectsToReplace { get; init; }
        public int SelfEnmityPercentIfTargetRecentlyDamagedActivator { get; init; }
        public int SelfEnmityDurationSecondsIfTargetRecentlyDamagedActivator { get; init; }
        public bool RequireTargetRecentlyDamagedActivatorForConditionalStatus { get; init; }
        public Func<IStatusEffect> FriendlyTargetStatusEffectFactory { get; init; }
        public bool FriendlyTargetStatusPersistsUntilBroken { get; init; }
        public bool RequiresGuardedTarget { get; init; }
        /// <summary>Shield rate, sized by <see cref="HealAmount.FromRate"/> from the chosen creature's max HP.</summary>
        public int FriendlyTargetTemporaryHPPercent { get; init; }
        public int FriendlyTargetTemporaryHPDurationSeconds { get; init; }
        public bool FriendlyTargetTemporaryHPUsesActivatorMaximum { get; init; }
        public int SelfGuardPercent { get; init; }
        public int SelfGuardDurationSeconds { get; init; }
        public bool SelfStatusAlsoAppliesToGuardedTarget { get; init; }
        public Type[] SourceStatusEffectsToExtend { get; init; }
        public int SourceStatusExtensionSeconds { get; init; }
        public bool ConsumeSourceStatusEffectsOnHit { get; init; }
        public bool SuppressSourceStatusStackRiders { get; init; }
        public int SelfInvisibilityDurationSeconds { get; init; }

        public void AfterSuccessfulHit(uint activator, uint target, CombatDamageType damageType, StatusSpreadSnapshot spreadSnapshot)
        {
            if (ClearTargetActionsOnHit)
            {
                ActionInterruption.ClearActions(target);
                UsePerkFeat.InterruptAbilityActivationFromHit(target);
            }

            if (RestoreStaminaOnHit > 0)
                GameServices.CombatResources.RestoreAbilityHitStamina(activator, AbilityImpactState.GetActiveAbilityImpactSummary(activator)?.Ability, RestoreStaminaOnHit);
            if (RestoreFPOnHit > 0)
            {
                if (Stat.RestoreFP(activator, RestoreFPOnHit) > 0)
                    GameServices.CombatResources.ApplyAbilityRestoredFPEffects(activator);
            }
            if (DrainStaminaOnHit > 0)
                Stat.ReduceStamina(target, DrainStaminaOnHit);
            if (DrainFPOnHit > 0)
                Stat.ReduceFP(target, DrainFPOnHit);

            GameServices.WeaponAbilityHitEffects.ApplyResourceConditionalRestore(this, activator);
            GameServices.WeaponAbilityHitEffects.ApplyResourceConditionalDrain(this, activator, target);
            GameServices.WeaponAbilityHitEffects.ApplyTargetTemporaryModifiers(this, target);
            GameServices.WeaponAbilityHitEffects.ApplyProtectedTargetHitSelfModifiers(this, activator, target);
            GameServices.WeaponAbilityStatusRiders.ApplyStatusSpread(this, activator, target, damageType, spreadSnapshot);
            GameServices.WeaponAbilityStatusRiders.ApplySuppressionEffects(this, activator, target, damageType);
            ApplyDefenseIgnoreHitEffects(activator, target);
            ConsumeSourceStatusEffects(activator, target);

            if (ConsumeBleedIntoHemorrhage &&
                StatusEffect.HasStatusEffect(target, typeof(BleedStatusEffect)))
            {
                StatusEffect.RemoveStatusEffect(target, typeof(BleedStatusEffect), false);
                StatusEffectApplication.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(HemorrhageStatusEffect),
                    HemorrhageDurationSeconds > 0 ? HemorrhageDurationSeconds : 30f,
                    damageType);
            }
        }

        /// <summary>
        /// Runs on a successful hit before the shared impact riders. Returns the categories of the
        /// conditional statuses it applied (null when none landed), so they count like the impact's own status.
        /// </summary>
        public StatusEffectCategory? BeforeSuccessfulImpactRiders(uint activator, uint target, CombatDamageType damageType)
        {
            ExtendSourceStatusEffects(activator, target);

            StatusEffectCategory? appliedCategories = null;
            void Track(StatusEffectCategory? categories)
            {
                if (categories is { } applied)
                    appliedCategories = (appliedCategories ?? StatusEffectCategory.None) | applied;
            }

            Track(ApplyTargetUsingAbilityEffects(activator, target, damageType));
            Track(GameServices.WeaponAbilityStatusRiders.ApplyConditionalStatus(this, activator, target, damageType));
            Track(GameServices.WeaponAbilityStatusRiders.ApplyTargetConditionalStatus(this, activator, target, damageType));
            return appliedCategories;
        }

        private void ExtendSourceStatusEffects(uint activator, uint target)
        {
            if (SourceStatusExtensionSeconds <= 0 || SourceStatusEffectsToExtend == null)
                return;

            var extendedCount = 0;
            foreach (var statusEffectType in SourceStatusEffectsToExtend)
            {
                if (statusEffectType == null)
                    continue;

                if (StatusEffectDuration.ExtendStatusEffectDuration(
                    target,
                    statusEffectType,
                    activator,
                    SourceStatusExtensionSeconds))
                {
                    extendedCount++;
                }
            }

            if (extendedCount > 0 && GetIsPC(activator))
            {
                var statusLabel = extendedCount == 1 ? "status" : "statuses";
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Extended {extendedCount} {statusLabel} by {SourceStatusExtensionSeconds}s"),
                    activator,
                    false);
            }
        }

        private void ConsumeSourceStatusEffects(uint activator, uint target)
        {
            if (!ConsumeSourceStatusEffectsOnHit)
                return;

            var consumed = false;
            if (ExtraDamageSourceStatusEffect != null &&
                StatusEffect.HasStatusEffect(target, ExtraDamageSourceStatusEffect, activator))
            {
                StatusEffect.RemoveStatusEffect(target, ExtraDamageSourceStatusEffect, activator, false);
                consumed = true;
            }

            if (ExtraDamageSourceStackStatusEffect != null &&
                StatusEffect.HasStatusEffect(target, ExtraDamageSourceStackStatusEffect, activator))
            {
                StatusEffect.RemoveStatusEffect(target, ExtraDamageSourceStackStatusEffect, activator, false);
                consumed = true;
            }

            if (consumed)
            {
                SendMessageToPC(activator, "You consume your Venom and Infection setup.");
            }
        }

        private void ApplyDefenseIgnoreHitEffects(uint activator, uint target)
        {
            if (GetDefenseIgnorePercent(activator) <= 0)
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

        public void RestoreResourcesForTargetCount(uint activator, AbilityImpactSummary summary)
        {
            if (RestoreStaminaIfMinimumTargetsHit > 0 && StaminaRestoreMinimumTargets > 0 &&
                (summary?.ImpactedTargetCount ?? 0) >= StaminaRestoreMinimumTargets)
            {
                GameServices.CombatResources.RestoreAbilityHitStamina(activator, summary.Ability, RestoreStaminaIfMinimumTargetsHit);
            }
        }

        public void AfterImpact(
            uint activator,
            int totalDamage,
            int successfulHitCount = 0,
            AbilityImpactSummary summary = null,
            bool clearActivationIdleBonusSnapshots = true)
        {
            if (clearActivationIdleBonusSnapshots)
                ClearActivationIdleBonusSnapshots(activator);

            if (SelfStatusEffectOnCriticalHitFactory != null &&
                (SelfStatusEffectOnCriticalHitDurationSeconds > 0 ||
                 SelfStatusEffectOnCriticalHitIsPermanent) &&
                (summary?.CriticalHitCount ?? 0) > 0)
            {
                var durationSeconds = SelfStatusEffectOnCriticalHitIsPermanent
                    ? 0f
                    : SelfStatusEffectOnCriticalHitDurationSeconds;
                StatusEffectApplication.ApplyStatusEffect(
                    activator,
                    activator,
                    SelfStatusEffectOnCriticalHitFactory(summary),
                    durationSeconds);
            }

            // Keyed off landing the cone, not off dealing damage: a target that resists the
            // hit down to zero still counts as struck, and the authored restore does not
            // depend on damage. This has to run before the damage-only early return.
            if (RestoreFPAfterRangedDeflection > 0 &&
                RestoreFPAfterRangedDeflectionWindowSeconds > 0 &&
                (summary?.ImpactedTargetCount ?? 0) > 0 &&
                GameServices.CombatDefenseMemory.HasRecentDeflection(
                    activator,
                    DeflectionSource.Ranged,
                    RestoreFPAfterRangedDeflectionWindowSeconds))
            {
                if (Stat.RestoreFP(activator, RestoreFPAfterRangedDeflection) > 0)
                    GameServices.CombatResources.ApplyAbilityRestoredFPEffects(activator);
            }

            if (RestoreStaminaIfAnyCriticalHit > 0 && (summary?.CriticalHitCount ?? 0) > 0)
            {
                var restored = GameServices.CombatResources.RestoreAbilityHitStamina(activator, summary.Ability, RestoreStaminaIfAnyCriticalHit,
                    sendFeedback: string.IsNullOrWhiteSpace(RestoreStaminaIfAnyCriticalHitFeedbackLabel));
                if (restored > 0 &&
                    GetIsPC(activator) &&
                    !string.IsNullOrWhiteSpace(RestoreStaminaIfAnyCriticalHitFeedbackLabel))
                {
                    PlayerFeedback.ShowDiagnosticFloatingText(
                        ColorToken.Combat($"{RestoreStaminaIfAnyCriticalHitFeedbackLabel} restored {restored} STM"),
                        activator,
                        false);
                }
            }
            if (totalDamage <= 0)
            {
                if ((summary?.ImpactedTargetCount ?? 0) > 0)
                    GameServices.WeaponAbilitySelfEffects.ApplyLandedImpactModifiers(this, activator);
                return;
            }

            if (HealPercentOfDamage > 0)
            {
                var amount = GameMath.PercentOf(totalDamage, HealPercentOfDamage);
                if (HealMaximum > 0)
                    amount = Math.Min(amount, HealMaximum);
                ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), activator);
            }

            if (RestoreStaminaAfterImpact > 0)
                GameServices.CombatResources.RestoreAbilityHitStamina(activator, summary?.Ability, RestoreStaminaAfterImpact);
            if (RestoreFPAfterImpact > 0)
            {
                if (Stat.RestoreFP(activator, RestoreFPAfterImpact) > 0)
                    GameServices.CombatResources.ApplyAbilityRestoredFPEffects(activator);
            }
            if (HitCount > 1 && successfulHitCount >= HitCount)
            {
                if (RestoreStaminaIfAllHitsLand > 0)
                    GameServices.CombatResources.RestoreAbilityHitStamina(activator, summary?.Ability, RestoreStaminaIfAllHitsLand);
                if (RestoreFPIfAllHitsLand > 0)
                {
                    if (Stat.RestoreFP(activator, RestoreFPIfAllHitsLand) > 0)
                        GameServices.CombatResources.ApplyAbilityRestoredFPEffects(activator);
                }
                if (SelfHastePercentIfAllHitsLand > 0 && SelfHasteDurationSecondsIfAllHitsLand > 0)
                {
                    TemporaryStatModifier.AddCapped(
                        activator,
                        StatType.AttackDelayReductionPercent,
                        SelfHastePercentIfAllHitsLand,
                        SelfHasteDurationSecondsIfAllHitsLand,
                        SelfHasteMaximumPercentIfAllHitsLand > 0 ? SelfHasteMaximumPercentIfAllHitsLand : SelfHastePercentIfAllHitsLand,
                        StatType.AttackDelayReductionPercent,
                        1);
                }
            }

            GameServices.WeaponAbilitySelfEffects.ApplyLandedImpactModifiers(this, activator);
        }

        private StatusEffectCategory? ApplyTargetUsingAbilityEffects(uint activator, uint target, CombatDamageType damageType)
        {
            if (!GameServices.CombatActivity.IsUsingAbility(target))
                return null;

            if (TargetUsingAbilityDrainStamina > 0)
                Stat.ReduceStamina(target, TargetUsingAbilityDrainStamina);
            if (TargetUsingAbilityDrainFP > 0)
                Stat.ReduceFP(target, TargetUsingAbilityDrainFP);

            if (TargetUsingAbilityStatusEffect == null || TargetUsingAbilityStatusDurationSeconds <= 0)
                return null;

            return GameServices.WeaponAbilityStatusRiders.GetAppliedCategories(
                StatusEffectApplication.ApplyStatusEffect(
                    activator,
                    target,
                    TargetUsingAbilityStatusEffect,
                    TargetUsingAbilityStatusDurationSeconds,
                    damageType),
                TargetUsingAbilityStatusEffect);
        }

        public bool HasFriendlyTargetStatus()
        {
            return FriendlyTargetStatusEffectFactory != null;
        }

        public bool HasFriendlyTargetEffects()
        {
            return FriendlyTargetTemporaryHPPercent > 0 ||
                   SelfGuardPercent > 0;
        }

        public string ValidateHostileTarget(uint activator, uint target)
        {
            if (!RequiresRecentWardHitTarget)
                return string.Empty;

            return WardBondStatusEffect.HasRecentWardHit(activator, target, ProtectedTargetHitWindowSeconds)
                ? string.Empty
                : "Target has not recently attacked your ward.";
        }

        public string ValidateFriendlyTargetStatus(uint activator, uint target)
        {
            target = ResolveFriendlyTarget(activator, target);
            if (RequiresGuardedTarget && !GetIsObjectValid(target))
                return "You do not have an active Guarded target within range.";

            var validation = AbilityTargeting.ValidateFriendlyTarget(activator, target, false);
            if (!string.IsNullOrWhiteSpace(validation))
                return validation;

            if (RequiresGuardedTarget &&
                !GuardedStatusEffect.IsActiveGuardedBySource(target, activator))
            {
                return "Target is not guarded by you or is too far away.";
            }

            if (FriendlyTargetStatusEffectFactory == null)
                return string.Empty;

            var statusEffect = FriendlyTargetStatusEffectFactory();
            statusEffect.ReassignSource(activator);
            return statusEffect.CanApply(target);
        }

        public uint ResolveFriendlyTarget(uint activator, uint target)
        {
            if (!RequiresGuardedTarget)
                return target;

            return GuardedStatusEffect.GetActiveGuardedTarget(activator);
        }

        public bool ApplyFriendlyTargetStatus(uint activator, uint target, float duration)
        {
            if (FriendlyTargetStatusEffectFactory == null)
                return false;

            var statusEffect = FriendlyTargetStatusEffectFactory();
            statusEffect.ReassignSource(activator);
            var canApply = statusEffect.CanApply(target);
            if (!string.IsNullOrWhiteSpace(canApply))
            {
                SendMessageToPC(activator, $"Effect failed to apply: {canApply}");
                return false;
            }

            StatusEffect.RemoveStatusEffectFromAllTargetsBySource(statusEffect.GetType(), activator, false);
            var effectDuration = FriendlyTargetStatusPersistsUntilBroken
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

        public void ApplyFriendlyTargetEffects(uint activator, uint target, string temporaryHPEffectKey)
        {
            if (FriendlyTargetTemporaryHPPercent > 0 &&
                FriendlyTargetTemporaryHPDurationSeconds > 0)
            {
                var maximumHPSource = FriendlyTargetTemporaryHPUsesActivatorMaximum
                    ? activator
                    : target;
                var temporaryHP = HealAmount.FromRate(FriendlyTargetTemporaryHPPercent)
                    .ForMaxHitPoints(GetMaxHitPoints(maximumHPSource));
                TemporaryHitPointEffects.ApplyFlat(
                    target,
                    temporaryHPEffectKey,
                    temporaryHP,
                    FriendlyTargetTemporaryHPDurationSeconds);
            }

            if (SelfGuardPercent > 0 &&
                SelfGuardDurationSeconds > 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.Guard,
                    SelfGuardPercent,
                    SelfGuardDurationSeconds,
                    "GeneratedWeaponAbility:GuardingPlayerGuard");
                GuardedStatusEffect.RefreshGuardBenefitsFromSource(activator);
                Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
            }
        }

        public bool HasImmediateSelfStatusEffect()
        {
            return SelfStatusEffectFactory != null && SelfStatDurationSeconds <= 0;
        }

        public void ApplySelfStatusEffect(uint activator, int duration)
        {
            if (SelfStatusEffectFactory == null)
                return;

            if (SelfStatusEffectsToReplace != null)
            {
                foreach (var statusEffectType in SelfStatusEffectsToReplace)
                {
                    if (statusEffectType != null)
                        StatusEffect.RemoveStatusEffect(activator, statusEffectType, false);
                }
            }

            var selfStatus = SelfStatusEffectFactory();
            StatusEffect.RemoveOtherStanceStatuses(activator, selfStatus.GetType());
            if (StatusEffectApplication.ApplyStatusEffect(activator, activator, selfStatus, duration > 0f ? duration : 0f))
                Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
        }

        public void ApplySelfStatusToGuardedTarget(uint activator, Type statusEffect, int duration)
        {
            if (!SelfStatusAlsoAppliesToGuardedTarget ||
                statusEffect == null ||
                duration <= 0)
                return;

            var guardedTarget = GuardedStatusEffect.GetActiveGuardedTarget(activator);
            if (!GetIsObjectValid(guardedTarget))
                return;

            if (StatusEffectApplication.ApplyStatusEffect(activator, guardedTarget, statusEffect, duration))
                Ability.PlaySuccessfulImpactVisualEffect(activator, guardedTarget);
        }

        public int GetBaseDamageAdjustment(
            uint activator,
            uint target,
            ActivationIdleBonusSnapshot activationIdleBonusSnapshot = null) =>
            GameServices.WeaponAbilityDamageModifiers.GetBaseDamageAdjustment(this, activator, target, activationIdleBonusSnapshot);

        public int GetDamagePercentAdjustment(uint activator, uint target) =>
            GameServices.WeaponAbilityDamageModifiers.GetDamagePercentAdjustment(this, activator, target);

        public int GetCriticalRateAdjustment(
            uint activator,
            uint target,
            ActivationIdleBonusSnapshot activationIdleBonusSnapshot = null) =>
            GameServices.WeaponAbilityDamageModifiers.GetCriticalRateAdjustment(this, activator, target, activationIdleBonusSnapshot);

        public int GetDefenseIgnorePercent(
            uint activator,
            ActivationIdleBonusSnapshot activationIdleBonusSnapshot = null) =>
            GameServices.WeaponAbilityDamageModifiers.GetDefenseIgnorePercent(this, activator, activationIdleBonusSnapshot);

        public (Type StatusEffect, int DurationSeconds) ResolveImpactStatus(
            uint activator,
            Type statusEffect,
            int durationSeconds) =>
            GameServices.WeaponAbilityDamageModifiers.ResolveImpactStatus(this, activator, statusEffect, durationSeconds);

        public void ClearActivationIdleBonusSnapshots(uint activator) =>
            GameServices.WeaponAbilityDamageModifiers.ClearActivationIdleBonusSnapshots(activator);

        public ActivationIdleBonusSnapshot CaptureActivationIdleBonusSnapshot(uint activator) =>
            GameServices.WeaponAbilityDamageModifiers.CaptureActivationIdleBonusSnapshot(activator);

        public void PrepareQueuedActivation(uint activator, SkillType skillType) =>
            GameServices.WeaponAbilityDamageModifiers.PrepareQueuedActivation(this, activator, skillType);

        public void PrepareActivationIdleBonuses(uint activator) =>
            GameServices.WeaponAbilityDamageModifiers.PrepareActivationIdleBonuses(this, activator);

        public void SpendHitPoints(uint activator) =>
            GameServices.WeaponAbilitySelfEffects.SpendHitPoints(this, activator);

        public void AfterActivation(uint activator, SkillType skillType) =>
            GameServices.WeaponAbilitySelfEffects.AfterActivation(this, activator, skillType);

        public void AfterHostileActivation(uint activator) =>
            GameServices.WeaponAbilitySelfEffects.AfterHostileActivation(this, activator);
    }
}
