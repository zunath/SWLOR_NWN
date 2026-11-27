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
    }
}
