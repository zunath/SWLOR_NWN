using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public abstract class WeaponActiveAbilityDefinitionBase
    {
        private const float ToggleActivationDelaySeconds = 2f;

        protected sealed class WeaponAbilityProfile
        {
            private readonly string _modifierSource = $"ability:{Guid.NewGuid()}";

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
                        statuses = (IsBleeding(target), StatusEffect.HasStatusEffect<SunderStatusEffect>(target));
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
            /// get the visual-only impact flash from <see cref="Ability.ApplyTelegraphedCombatImpact"/>.
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
            public bool ClearTargetActionsOnHit { get; init; }
            public Type ConditionalStatusEffect { get; init; }
            public int ConditionalStatusDurationSeconds { get; init; }
            public int ConditionalStatusAfterDeflectionWindowSeconds { get; init; }
            public Type RequiredTargetStatusEffectForConditionalStatus { get; init; }
            public bool ConvertsRequiredTargetStatusEffect { get; init; }
            public StatusEffectCategory RequiredTargetStatusCategoryForConditionalStatus { get; init; }
            public bool RequireRecentGuardedHitForConditionalStatus { get; init; }
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

            public int GetBaseDamageAdjustment(
                uint activator,
                uint target,
                ActivationIdleBonusSnapshot activationIdleBonusSnapshot = null)
            {
                var bonus = 0;
                if (ExtraDamageIfRecentTarget != 0 &&
                    RecentTargetWindowSeconds > 0f &&
                    Combat.HasRecentDamageTarget(activator, target, RecentTargetWindowSeconds))
                {
                    bonus += ExtraDamageIfRecentTarget;
                }

                if (ExtraDamageIfHighResources != 0 &&
                    HighResourceExtraDamageThresholdPercent > 0 &&
                    Combat.IsCurrentFPAndStaminaAbovePercent(activator, HighResourceExtraDamageThresholdPercent))
                {
                    bonus += ExtraDamageIfHighResources;
                }

                if (ExtraDamageIfBesideOrBehind != 0 &&
                    (Combat.IsAttackerBesideTarget(activator, target) ||
                     Combat.IsTargetNotFacingAttacker(activator, target)))
                {
                    bonus += ExtraDamageIfBesideOrBehind;
                    if (GetIsPC(activator))
                    {
                        FloatingTextStringOnCreature(
                            ColorToken.Combat($"Flanking +{ExtraDamageIfBesideOrBehind} DMG"),
                            activator,
                            false);
                    }
                }

                var idleSnapshot = activationIdleBonusSnapshot?.HasSnapshot ??
                                   Combat.HasWeaponAbilityActivationIdleSnapshot(activator);
                var idleDamageBonus = activationIdleBonusSnapshot?.DamageBonus ??
                                      Combat.GetWeaponAbilityActivationIdleDamageBonus(activator);
                if (ExtraDamageIfIdle != 0 &&
                    (idleSnapshot
                        ? idleDamageBonus > 0
                        : IsIdle(activator)))
                {
                    bonus += idleSnapshot
                        ? idleDamageBonus
                        : ExtraDamageIfIdle;
                    if (GetIsPC(activator) && !string.IsNullOrWhiteSpace(ExtraDamageIfIdleFeedbackLabel))
                    {
                        FloatingTextStringOnCreature(
                            ColorToken.Combat($"{ExtraDamageIfIdleFeedbackLabel} +{ExtraDamageIfIdle} DMG"),
                            activator,
                            false);
                    }
                }

                if (ExtraDamageIfTargetBleeding != 0 && IsBleeding(target))
                    bonus += ExtraDamageIfTargetBleeding;

                if (ExtraDamageIfTargetDebuffed != 0 && StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Debuff))
                    bonus += ExtraDamageIfTargetDebuffed;

                if (ExtraDamageIfTargetControlled != 0 && StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Control))
                    bonus += ExtraDamageIfTargetControlled;

                if (ExtraDamageIfTargetLowHP != 0 && IsTargetBelowThreshold(target, TargetLowHPThresholdPercent))
                    bonus += ExtraDamageIfTargetLowHP;

                if (ExtraDamageIfTargetStatusEffect != 0 &&
                    ExtraDamageTargetStatusEffect != null &&
                    StatusEffect.HasStatusEffect(target, ExtraDamageTargetStatusEffect))
                {
                    bonus += ExtraDamageIfTargetStatusEffect;
                }

                if (ExtraDamageIfBehind != 0 && Combat.IsTargetNotFacingAttacker(activator, target))
                {
                    bonus += ExtraDamageIfBehind;
                    if (GetIsPC(activator) && !string.IsNullOrWhiteSpace(ExtraDamageIfBehindFeedbackLabel))
                    {
                        FloatingTextStringOnCreature(
                            ColorToken.Combat($"{ExtraDamageIfBehindFeedbackLabel} +{ExtraDamageIfBehind} DMG"),
                            activator,
                            false);
                    }
                }

                if (ExtraDamageIfSourceStatusEffect != 0 &&
                    ExtraDamageSourceStatusEffect != null &&
                    StatusEffect.HasStatusEffect(target, ExtraDamageSourceStatusEffect, activator))
                {
                    bonus += ExtraDamageIfSourceStatusEffect;
                }

                if (ExtraDamagePerSourceStack != 0 &&
                    ExtraDamageSourceStackStatusEffect != null &&
                    StatusEffect.GetStatusEffect(target, ExtraDamageSourceStackStatusEffect, activator) is InfectionStatusEffect infection)
                {
                    bonus += ExtraDamagePerSourceStack * infection.Stacks;
                }

                if (ExtraDamageIfRecentGuardedHit != 0 &&
                    RecentGuardedHitWindowSeconds > 0f &&
                    Combat.HasRecentGuardedHit(activator, RecentGuardedHitWindowSeconds))
                {
                    bonus += ExtraDamageIfRecentGuardedHit;
                }

                return bonus;
            }

            public int GetDamagePercentAdjustment(uint activator, uint target)
            {
                var adjustment = HPCostPercent > 0
                    ? Stat.GetStatAdjustment(activator, StatType.HitPointSpendAbilityDamagePercentAdjustment)
                    : 0;
                if (DamagePercentIfTargetDebuffed != 0 && StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Debuff))
                    adjustment += DamagePercentIfTargetDebuffed;
                if (DamagePercentIfTargetControlled != 0 && StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Control))
                    adjustment += DamagePercentIfTargetControlled;
                if (DamagePercentIfTargetLowHP != 0 && IsTargetBelowThreshold(target, TargetLowHPThresholdPercent))
                    adjustment += DamagePercentIfTargetLowHP;

                return adjustment;
            }

            public int GetCriticalRateAdjustment(
                uint activator,
                uint target,
                ActivationIdleBonusSnapshot activationIdleBonusSnapshot = null)
            {
                var adjustment = CriticalRatePercentAdjustment;
                var idleSnapshot = activationIdleBonusSnapshot?.HasSnapshot ??
                                   Combat.HasWeaponAbilityActivationIdleSnapshot(activator);
                var idleCriticalRate = activationIdleBonusSnapshot?.CriticalRatePercentAdjustment ??
                                       Combat.GetWeaponAbilityActivationIdleCriticalRateBonus(activator);
                if (!IsQueuedWeaponAbility && CriticalRateIfIdle != 0 &&
                    (idleSnapshot
                        ? idleCriticalRate > 0
                        : IsIdle(activator)))
                {
                    adjustment += idleSnapshot
                        ? idleCriticalRate
                        : CriticalRateIfIdle;
                    if (GetIsPC(activator) && !string.IsNullOrWhiteSpace(CriticalRateIfIdleFeedbackLabel))
                    {
                        FloatingTextStringOnCreature(
                            ColorToken.Combat($"{CriticalRateIfIdleFeedbackLabel} +{CriticalRateIfIdle}% Critical Rate"),
                            activator,
                            false);
                    }
                }
                if (CriticalRateIfNotRecentTarget != 0 &&
                    NotRecentTargetWindowSeconds > 0f &&
                    !Combat.HasRecentDamageTarget(activator, target, NotRecentTargetWindowSeconds))
                {
                    adjustment += CriticalRateIfNotRecentTarget;
                    if (!string.IsNullOrWhiteSpace(CriticalRateIfNotRecentTargetFeedbackLabel))
                    {
                        FloatingTextStringOnCreature(
                            ColorToken.Combat($"{CriticalRateIfNotRecentTargetFeedbackLabel} +{CriticalRateIfNotRecentTarget}% Critical Rate"),
                            activator,
                            false);
                    }
                }
                if (CriticalRateIfTargetDebuffedOrControlled != 0 &&
                    (StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Debuff) ||
                     StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Control)))
                {
                    adjustment += CriticalRateIfTargetDebuffedOrControlled;
                }

                return adjustment;
            }

            public int GetDefenseIgnorePercent(
                uint activator,
                ActivationIdleBonusSnapshot activationIdleBonusSnapshot = null)
            {
                var adjustment = DefenseIgnorePercent;
                var idleSnapshot = activationIdleBonusSnapshot?.HasSnapshot ??
                                   Combat.HasWeaponAbilityActivationIdleSnapshot(activator);
                var idleDefenseIgnore = activationIdleBonusSnapshot?.DefenseIgnorePercent ??
                                        Combat.GetWeaponAbilityActivationIdleDefenseIgnorePercent(activator);
                if (DefenseIgnoreIfIdle != 0 &&
                    (idleSnapshot
                        ? idleDefenseIgnore > 0
                        : IsIdle(activator)))
                {
                    adjustment += idleSnapshot
                        ? idleDefenseIgnore
                        : DefenseIgnoreIfIdle;
                }

                return adjustment;
            }

            public void SpendHitPoints(uint activator)
            {
                if (HPCostPercent <= 0)
                    return;

                var currentHP = GetCurrentHitPoints(activator);
                var hpCost = GameMath.PercentOf(currentHP, HPCostPercent);
                hpCost = Math.Min(hpCost, Math.Max(0, currentHP - 1));
                if (hpCost <= 0)
                    return;

                AssignCommand(activator, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(hpCost), activator));
                Combat.ApplyHitPointSpendAbilityEffects(activator, hpCost);
            }

            public void AfterSuccessfulHit(uint activator, uint target, CombatDamageType damageType, StatusSpreadSnapshot spreadSnapshot)
            {
                if (ClearTargetActionsOnHit)
                {
                    AssignCommand(target, () => ClearAllActions());
                    UsePerkFeat.InterruptAbilityActivation(target);
                }

                if (RestoreStaminaOnHit > 0)
                    Stat.RestoreStamina(activator, RestoreStaminaOnHit);
                if (RestoreFPOnHit > 0)
                {
                    if (Stat.RestoreFP(activator, RestoreFPOnHit) > 0)
                        Combat.ApplyAbilityRestoredFPEffects(activator);
                }
                if (DrainStaminaOnHit > 0)
                    Stat.ReduceStamina(target, DrainStaminaOnHit);
                if (DrainFPOnHit > 0)
                    Stat.ReduceFP(target, DrainFPOnHit);

                ApplyResourceConditionalRestore(activator);
                ApplyResourceConditionalDrain(activator, target);
                ApplyTargetUsingAbilityEffects(activator, target, damageType);
                ApplyConditionalStatus(activator, target, damageType);
                ApplyTargetConditionalStatus(activator, target, damageType);
                ApplyTargetTemporaryModifiers(target);
                ApplyProtectedTargetHitSelfModifiers(activator, target);
                ApplyStatusSpread(activator, target, damageType, spreadSnapshot);
                ApplySuppressionEffects(activator, target, damageType);
                ApplyDefenseIgnoreHitEffects(activator, target);
                ConsumeSourceStatusEffects(activator, target);

                if (ConsumeBleedIntoHemorrhage &&
                    StatusEffect.HasStatusEffect(target, typeof(BleedStatusEffect)))
                {
                    StatusEffect.RemoveStatusEffect(target, typeof(BleedStatusEffect), false);
                    StatusEffect.ApplyStatusEffect(
                        activator,
                        target,
                        typeof(HemorrhageStatusEffect),
                        HemorrhageDurationSeconds > 0 ? HemorrhageDurationSeconds : 30f,
                        damageType);
                }
            }

            public void BeforeSuccessfulImpactRiders(uint activator, uint target)
            {
                if (SourceStatusExtensionSeconds <= 0 || SourceStatusEffectsToExtend == null)
                    return;

                var extendedCount = 0;
                foreach (var statusEffectType in SourceStatusEffectsToExtend)
                {
                    if (statusEffectType == null)
                        continue;

                    if (StatusEffect.ExtendStatusEffectDuration(
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

            public void ClearActivationIdleBonusSnapshots(uint activator)
            {
                Combat.ClearAbilityActivationIdleBonuses(activator);
                Combat.ClearWeaponAbilityActivationIdleBonuses(activator);
            }

            public ActivationIdleBonusSnapshot CaptureActivationIdleBonusSnapshot(uint activator)
            {
                return new ActivationIdleBonusSnapshot
                {
                    HasSnapshot = Combat.HasWeaponAbilityActivationIdleSnapshot(activator),
                    DamageBonus = Combat.GetWeaponAbilityActivationIdleDamageBonus(activator),
                    CriticalRatePercentAdjustment = Combat.GetWeaponAbilityActivationIdleCriticalRateBonus(activator),
                    DefenseIgnorePercent = Combat.GetWeaponAbilityActivationIdleDefenseIgnorePercent(activator)
                };
            }

            public void RestoreResourcesForTargetCount(uint activator, AbilityImpactSummary summary)
            {
                if (RestoreStaminaIfMinimumTargetsHit > 0 && StaminaRestoreMinimumTargets > 0 &&
                    (summary?.ImpactedTargetCount ?? 0) >= StaminaRestoreMinimumTargets)
                {
                    Stat.RestoreStamina(activator, RestoreStaminaIfMinimumTargetsHit);
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
                    StatusEffect.ApplyStatusEffect(
                        activator,
                        activator,
                        SelfStatusEffectOnCriticalHitFactory(summary),
                        durationSeconds);
                }

                if (totalDamage <= 0)
                {
                    if ((summary?.ImpactedTargetCount ?? 0) > 0)
                        ApplyLandedImpactModifiers(activator);
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
                    Stat.RestoreStamina(activator, RestoreStaminaAfterImpact);
                if (RestoreStaminaIfAnyCriticalHit > 0 && (summary?.CriticalHitCount ?? 0) > 0)
                {
                    var restored = Stat.RestoreStamina(activator, RestoreStaminaIfAnyCriticalHit);
                    if (restored > 0 &&
                        GetIsPC(activator) &&
                        !string.IsNullOrWhiteSpace(RestoreStaminaIfAnyCriticalHitFeedbackLabel))
                    {
                        FloatingTextStringOnCreature(
                            ColorToken.Combat($"{RestoreStaminaIfAnyCriticalHitFeedbackLabel} restored {restored} STM"),
                            activator,
                            false);
                    }
                }
                if (RestoreFPAfterImpact > 0)
                {
                    if (Stat.RestoreFP(activator, RestoreFPAfterImpact) > 0)
                        Combat.ApplyAbilityRestoredFPEffects(activator);
                }
                if (HitCount > 1 && successfulHitCount >= HitCount)
                {
                    if (RestoreStaminaIfAllHitsLand > 0)
                        Stat.RestoreStamina(activator, RestoreStaminaIfAllHitsLand);
                    if (RestoreFPIfAllHitsLand > 0)
                    {
                        if (Stat.RestoreFP(activator, RestoreFPIfAllHitsLand) > 0)
                            Combat.ApplyAbilityRestoredFPEffects(activator);
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

                ApplyLandedImpactModifiers(activator);
            }

            private void ApplyLandedImpactModifiers(uint activator)
            {
                ApplyNearbyPartyStatus(activator);
                ApplySelfImmunity(activator);
                if (!ApplySelfModifiersOnHostileActivation)
                    ApplySelfModifiers(activator);
                ApplyTemporaryDefeatedEnemyModifiers(activator);
            }

            public void PrepareQueuedActivation(uint activator, SkillType skillType)
            {
                if (!IsQueuedWeaponAbility)
                    return;

                Combat.StoreQueuedWeaponAbilityActivationIdleBonuses(activator, skillType);
                if (CriticalRateIfIdle > 0 && IsIdle(activator))
                {
                    Combat.StoreQueuedWeaponAbilityActivationCriticalRateBonus(
                        activator,
                        skillType,
                        CriticalRateIfIdle);
                    if (GetIsPC(activator) && !string.IsNullOrWhiteSpace(CriticalRateIfIdleFeedbackLabel))
                    {
                        FloatingTextStringOnCreature(
                            ColorToken.Combat($"{CriticalRateIfIdleFeedbackLabel} +{CriticalRateIfIdle}% Critical Rate"),
                            activator,
                            false);
                    }
                }
            }

            public void PrepareActivationIdleBonuses(uint activator)
            {
                Combat.ClearWeaponAbilityActivationIdleBonuses(activator);
                if (IsQueuedWeaponAbility || IdleWindowSeconds <= 0f ||
                    (ExtraDamageIfIdle == 0 && CriticalRateIfIdle == 0 && DefenseIgnoreIfIdle == 0))
                    return;

                Combat.StoreWeaponAbilityActivationIdleBonuses(
                    activator,
                    IsIdle(activator),
                    ExtraDamageIfIdle,
                    CriticalRateIfIdle,
                    DefenseIgnoreIfIdle);
            }

            public void AfterActivation(uint activator, SkillType skillType)
            {
                ApplyNearbyPartyStatus(activator);
                ApplySelfImmunity(activator);
                ApplySelfModifiers(activator);
                ApplyTemporaryDefeatedEnemyModifiers(activator);
            }

            public void AfterHostileActivation(uint activator)
            {
                ApplySelfInvisibility(activator);
                if (ApplySelfModifiersOnHostileActivation)
                    ApplySelfModifiers(activator);
            }

            private void ApplySelfInvisibility(uint activator)
            {
                if (SelfInvisibilityDurationSeconds <= 0)
                    return;

                ApplyEffectToObject(
                    DurationType.Temporary,
                    EffectInvisibility(InvisibilityType.Normal),
                    activator,
                    SelfInvisibilityDurationSeconds);
            }

            private void ApplyNearbyPartyStatus(uint activator)
            {
                if (NearbyPartyStatusEffect == null || NearbyPartyStatusDurationSeconds <= 0)
                    return;

                ApplyStatusToNearbyParty(
                    activator,
                    NearbyPartyStatusEffect,
                    NearbyPartyStatusDurationSeconds,
                    NearbyPartyStatusIncludesSelf);
            }

            private void ApplySelfImmunity(uint activator)
            {
                if (SelfKnockdownDazedImmunityDurationSeconds <= 0)
                    return;

                Ability.ApplyTemporaryImmunity(activator, SelfKnockdownDazedImmunityDurationSeconds, ImmunityType.Knockdown);
                Ability.ApplyTemporaryImmunity(activator, SelfKnockdownDazedImmunityDurationSeconds, ImmunityType.Dazed);
            }

            private void ApplySelfModifiers(uint activator)
            {
                if (SelfHastePercent > 0 && SelfHasteDurationSeconds > 0)
                {
                    var max = SelfHasteMaximumPercent > 0 ? SelfHasteMaximumPercent : SelfHastePercent;
                    TemporaryStatModifier.AddCapped(
                        activator,
                        StatType.AttackDelayReductionPercent,
                        SelfHastePercent,
                        SelfHasteDurationSeconds,
                        max,
                        StatType.AttackDelayReductionPercent,
                        1);
                }

                var duration = SelfStatDurationSeconds;
                if (duration <= 0)
                    return;

                if (SelfStatResourceAboveThresholdPercent > 0 &&
                    !Combat.IsCurrentFPAndStaminaAbovePercent(activator, SelfStatResourceAboveThresholdPercent))
                {
                    return;
                }

                if (SelfStatusEffectFactory != null)
                {
                    StatusEffect.ApplyStatusEffect(
                        activator,
                        activator,
                        SelfStatusEffectFactory(),
                        duration);
                    return;
                }

                ReplaceTemporary(activator, StatType.AttackPercentAdjustment, SelfAttackPercent, duration);
                ReplaceTemporary(activator, StatType.AccuracyPercentAdjustment, SelfAccuracyPercent, duration);
                ReplaceTemporary(activator, StatType.EvasionPercentAdjustment, SelfEvasionPercent, duration);
                ReplaceTemporary(activator, StatType.PhysicalDefensePercentAdjustment, SelfDefensePercent, duration);
                ReplaceTemporary(activator, StatType.ForceDefensePercentAdjustment, SelfForceDefensePercent, duration);
                ReplaceTemporary(activator, StatType.MeleeDeflection, SelfMeleeDeflection, duration);
                if (SelfMeleeDeflection != 0)
                    Combat.ApplyAbilityGrantedAttackDeflectionEffects(activator, DeflectionSource.Melee);
                ReplaceTemporary(activator, StatType.RangedDeflection, SelfRangedDeflection, duration);
                if (SelfRangedDeflection != 0)
                    Combat.ApplyAbilityGrantedAttackDeflectionEffects(activator, DeflectionSource.Ranged);
                ReplaceTemporary(activator, StatType.CriticalRatePercentAdjustment, SelfCriticalRatePercent, duration);
            }

            private void ReplaceTemporaryPayload(uint activator, int durationSeconds, params (StatType Stat, int Amount)[] stats)
            {
                if (durationSeconds <= 0 || !stats.Any(stat => stat.Amount != 0))
                    return;

                foreach (var (stat, amount) in stats)
                    TemporaryStatModifier.Replace(activator, stat, amount, durationSeconds, _modifierSource);
            }

            private static void ReplaceTemporary(uint activator, StatType statType, int amount, int durationSeconds)
            {
                if (amount == 0)
                    return;

                TemporaryStatModifier.Replace(activator, statType, amount, durationSeconds, $"GeneratedWeaponAbility:{statType}");
            }

            private static void ReplaceTemporaryTarget(uint target, StatType statType, int amount, int durationSeconds)
            {
                if (amount == 0 || durationSeconds <= 0)
                    return;

                TemporaryStatModifier.Replace(target, statType, amount, durationSeconds, $"GeneratedWeaponAbilityTarget:{statType}");
            }

            private void ApplyTemporaryDefeatedEnemyModifiers(uint activator)
            {
                var duration = TemporaryDefeatedEnemyEffectDurationSeconds;
                if (duration <= 0)
                    return;

                ReplaceTemporary(activator, StatType.DefeatedEnemyStaminaRestore, TemporaryDefeatedEnemyStaminaRestore, duration);
                ReplaceTemporary(activator, StatType.DefeatedEnemyAttackDelayReductionPercent, TemporaryDefeatedEnemyAttackDelayReductionPercent, duration);
                ReplaceTemporary(
                    activator,
                    StatType.DefeatedEnemyAttackDelayReductionDurationSeconds,
                    TemporaryDefeatedEnemyAttackDelayReductionDurationSeconds,
                    duration);
                ReplaceTemporary(activator, StatType.HostileAbilityFPRestore, TemporaryHostileAbilityFPRestore, duration);
                ReplaceTemporary(activator, StatType.HostileAbilityStaminaRestore, TemporaryHostileAbilityStaminaRestore, duration);
                ReplaceTemporaryPayload(activator, duration,
                    (StatType.HighFPAndStaminaAbilityDamageBonus, TemporaryHighFPAndStaminaAbilityDamageBonus),
                    (StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, TemporaryHighFPAndStaminaAbilityDamageBonusThresholdPercent));
                ReplaceTemporary(
                    activator,
                    StatType.FrenzySlashHasteRefreshDurationSeconds,
                    TemporaryFrenzySlashHasteRefreshDurationSeconds,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.AvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds,
                    TemporaryAvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.AvoidedAttackNextAutoAttackNoDelaySkillType,
                    TemporaryAvoidedAttackNextAutoAttackNoDelaySkillType,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.AvoidedAttackNextAutoAttackNoDelayDurationSeconds,
                    TemporaryAvoidedAttackNextAutoAttackNoDelayDurationSeconds,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.RangedHitSuppressionStackDurationSeconds,
                    TemporaryRangedHitSuppressionStackDurationSeconds,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.RangedHitSuppressionStackEvasionPenaltyPercent,
                    TemporaryRangedHitSuppressionStackEvasionPenaltyPercent,
                    duration);
                if (TemporaryAreaAbilityFragmentationDamage > 0)
                {
                    // Replacing with Invalid clears any previous skill restriction for a global buff.
                    TemporaryStatModifier.Replace(
                        activator,
                        StatType.AreaAbilityFragmentationSkillType,
                        (int)TemporaryAreaAbilityFragmentationSkillType,
                        duration,
                        $"GeneratedWeaponAbility:{StatType.AreaAbilityFragmentationSkillType}");
                }
                ReplaceTemporary(activator, StatType.AreaAbilityFragmentationDamage, TemporaryAreaAbilityFragmentationDamage, duration);
                ReplaceTemporaryPayload(activator, duration,
                    (StatType.AreaAbilityPulseDamage, TemporaryAreaAbilityPulseDamage),
                    (StatType.AreaAbilityPulseRadiusMeters, TemporaryAreaAbilityPulseRadiusMeters),
                    (StatType.AreaAbilityUsedFPRestore, TemporaryAreaAbilityUsedFPRestore),
                    (StatType.AreaAbilityUsedAttackDeflection, TemporaryAreaAbilityUsedAttackDeflection),
                    (StatType.AreaAbilityUsedAttackDeflectionDurationSeconds, TemporaryAreaAbilityUsedAttackDeflectionDurationSeconds));
                ReplaceTemporary(
                    activator,
                    StatType.AreaAbilityFragmentationDurationSeconds,
                    TemporaryAreaAbilityFragmentationDurationSeconds,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.AreaAbilityFragmentationPulseSeconds,
                    TemporaryAreaAbilityFragmentationPulseSeconds,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.AreaAbilityMinTargetsResourceRestoreThreshold,
                    TemporaryAreaAbilityMinTargetsResourceRestoreThreshold,
                    duration);
                ReplaceTemporary(activator, StatType.AreaAbilityFPRestore, TemporaryAreaAbilityFPRestore, duration);
                ReplaceTemporary(
                    activator,
                    StatType.AreaAbilityMinTargetsBuffThreshold,
                    TemporaryAreaAbilityMinTargetsBuffThreshold,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.AreaAbilityAttackDeflection,
                    TemporaryAreaAbilityAttackDeflection,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.AreaAbilityBuffDurationSeconds,
                    TemporaryAreaAbilityBuffDurationSeconds,
                    duration);
                ReplaceTemporary(activator, StatType.StatusAppliedRequiredCategory, TemporaryStatusAppliedRequiredCategory, duration);
                ReplaceTemporary(
                    activator,
                    StatType.StatusAppliedSelfAttackDeflection,
                    TemporaryStatusAppliedSelfAttackDeflection,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.StatusAppliedSelfDurationSeconds,
                    TemporaryStatusAppliedSelfDurationSeconds,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.StatusAppliedSelfStaminaRestore,
                    TemporaryStatusAppliedSelfStaminaRestore,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.CostlyAbilityStatusSkillType,
                    TemporaryCostlyAbilityStatusSkillType,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.CostlyAbilityStatusMinimumStaminaCost,
                    TemporaryCostlyAbilityStatusMinimumStaminaCost,
                    duration);
                ReplaceTemporary(
                    activator,
                    StatType.CostlyAbilityExposedDurationSeconds,
                    TemporaryCostlyAbilityExposedDurationSeconds,
                    duration);
            }

            private bool IsIdle(uint activator)
            {
                if (IdleWindowSeconds <= 0f)
                    return false;

                var lastActivity = Combat.GetLastCompletedOffensiveActivityAt(activator);
                return lastActivity == default ||
                       (DateTime.UtcNow - lastActivity).TotalSeconds >= IdleWindowSeconds;
            }

            private static bool IsTargetBelowThreshold(uint target, int thresholdPercent)
            {
                return thresholdPercent > 0 &&
                       GetMaxHitPoints(target) > 0 &&
                       GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * (thresholdPercent / 100f);
            }

            private static bool IsBleeding(uint target)
            {
                return StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Bleeding);
            }

            private void ApplyResourceConditionalRestore(uint activator)
            {
                if (RestoreBothResourcesBelowThresholdPercent <= 0 ||
                    !Combat.IsCurrentFPAndStaminaAtOrBelowPercent(activator, RestoreBothResourcesBelowThresholdPercent))
                {
                    return;
                }

                var restoredFP = RestoreFPIfResourcesBelow > 0
                    ? Stat.RestoreFP(activator, RestoreFPIfResourcesBelow)
                    : 0;
                var restoredStamina = RestoreStaminaIfResourcesBelow > 0
                    ? Stat.RestoreStamina(activator, RestoreStaminaIfResourcesBelow)
                    : 0;

                if (restoredFP > 0)
                    Combat.ApplyAbilityRestoredFPEffects(activator);

                if (restoredFP > 0 && restoredStamina > 0)
                    Combat.ApplyAbilityRestoredBothResourcesEffects(activator);
            }

            private void ApplyResourceConditionalDrain(uint activator, uint target)
            {
                if (DrainTargetResourceAboveThresholdPercent <= 0)
                    return;

                if (DrainTargetFPIfActivatorFPAboveThreshold > 0 && IsFPAtOrAbove(activator, DrainTargetResourceAboveThresholdPercent))
                    Stat.ReduceFP(target, DrainTargetFPIfActivatorFPAboveThreshold);
                if (DrainTargetStaminaIfActivatorStaminaAboveThreshold > 0 && IsStaminaAtOrAbove(activator, DrainTargetResourceAboveThresholdPercent))
                    Stat.ReduceStamina(target, DrainTargetStaminaIfActivatorStaminaAboveThreshold);
            }

            private static bool IsFPAtOrAbove(uint creature, int thresholdPercent)
            {
                var maxFP = Stat.GetMaxFP(creature);
                return maxFP > 0 && Stat.GetCurrentFP(creature) >= maxFP * (thresholdPercent / 100f);
            }

            private static bool IsStaminaAtOrAbove(uint creature, int thresholdPercent)
            {
                var maxStamina = Stat.GetMaxStamina(creature);
                return maxStamina > 0 && Stat.GetCurrentStamina(creature) >= maxStamina * (thresholdPercent / 100f);
            }

            private void ApplyConditionalStatus(uint activator, uint target, CombatDamageType damageType)
            {
                if (ConditionalStatusEffect == null ||
                    ConditionalStatusDurationSeconds <= 0 ||
                    ConditionalStatusAfterDeflectionWindowSeconds <= 0 ||
                    !Combat.HasRecentDeflection(activator, DeflectionSource.Ranged, ConditionalStatusAfterDeflectionWindowSeconds))
                {
                    return;
                }

                StatusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    ConditionalStatusEffect,
                    ConditionalStatusDurationSeconds,
                    damageType);
            }

            private void ApplyTargetConditionalStatus(uint activator, uint target, CombatDamageType damageType)
            {
                if (ConditionalTargetStatusEffect == null || ConditionalTargetStatusDurationSeconds <= 0)
                    return;

                if (RequireBehindForConditionalStatus && !Combat.IsTargetNotFacingAttacker(activator, target))
                    return;

                if (RequiredTargetStatusEffectForConditionalStatus != null &&
                    !StatusEffect.HasStatusEffect(target, RequiredTargetStatusEffectForConditionalStatus))
                {
                    return;
                }

                if (RequiredTargetStatusCategoryForConditionalStatus != 0 &&
                    !StatusEffect.HasStatusEffectCategory(target, RequiredTargetStatusCategoryForConditionalStatus))
                {
                    return;
                }

                var window = ProtectedTargetHitWindowSeconds;
                if (RequireRecentGuardedHitForConditionalStatus &&
                    !Combat.HasRecentGuardedHit(activator, window))
                {
                    return;
                }

                if (RequireRecentGuardedAllyHitForConditionalStatus &&
                    !GuardedStatusEffect.HasRecentGuardedAllyHit(activator, target, window))
                {
                    return;
                }

                if (RequireRecentWardHitForConditionalStatus &&
                    !WardBondStatusEffect.HasRecentWardHit(activator, target, window))
                {
                    return;
                }

                if (RequireTargetRecentlyDamagedActivatorForConditionalStatus &&
                    !Combat.HasRecentDamageTarget(target, activator, window))
                {
                    return;
                }

                StatusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    ConditionalTargetStatusEffect,
                    ConditionalTargetStatusDurationSeconds,
                    damageType,
                    ConvertsRequiredTargetStatusEffect ? RequiredTargetStatusEffectForConditionalStatus : null);
            }

            private void ApplyTargetTemporaryModifiers(uint target)
            {
                ReplaceTemporaryTarget(target, StatType.AttackPercentAdjustment, TargetAttackPercent, TargetAttackDurationSeconds);
                ReplaceTemporaryTarget(target, StatType.AbilityHitChancePercentAdjustment, TargetAbilityHitChancePercent, TargetAbilityHitChanceDurationSeconds);
            }

            private void ApplyProtectedTargetHitSelfModifiers(uint activator, uint target)
            {
                var window = ProtectedTargetHitWindowSeconds;
                if (SelfGuardPercentIfRecentGuardedAllyHit != 0 &&
                    GuardedStatusEffect.HasRecentGuardedAllyHit(activator, target, window))
                {
                    ReplaceTemporary(activator, StatType.Guard, SelfGuardPercentIfRecentGuardedAllyHit, SelfGuardDurationSecondsIfRecentGuardedAllyHit);
                }

                if (SelfEnmityPercentIfRecentWardHit != 0 &&
                    WardBondStatusEffect.HasRecentWardHit(activator, target, window))
                {
                    ReplaceTemporary(activator, StatType.EnmityPercentAdjustment, SelfEnmityPercentIfRecentWardHit, SelfEnmityDurationSecondsIfRecentWardHit);
                }

                if (SelfEnmityPercentIfTargetRecentlyDamagedActivator != 0 &&
                    Combat.HasRecentDamageTarget(target, activator, window))
                {
                    ReplaceTemporary(
                        activator,
                        StatType.EnmityPercentAdjustment,
                        SelfEnmityPercentIfTargetRecentlyDamagedActivator,
                        SelfEnmityDurationSecondsIfTargetRecentlyDamagedActivator);
                }
            }

            private void ApplyTargetUsingAbilityEffects(uint activator, uint target, CombatDamageType damageType)
            {
                if (!Combat.IsUsingAbility(target))
                    return;

                if (TargetUsingAbilityDrainStamina > 0)
                    Stat.ReduceStamina(target, TargetUsingAbilityDrainStamina);
                if (TargetUsingAbilityDrainFP > 0)
                    Stat.ReduceFP(target, TargetUsingAbilityDrainFP);

                if (TargetUsingAbilityStatusEffect == null || TargetUsingAbilityStatusDurationSeconds <= 0)
                    return;

                StatusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    TargetUsingAbilityStatusEffect,
                    TargetUsingAbilityStatusDurationSeconds,
                    damageType);
            }

            private void ApplyStatusSpread(uint activator, uint target, CombatDamageType damageType, StatusSpreadSnapshot spreadSnapshot)
            {
                if (!SpreadBleedFromTarget && !SpreadHemorrhageFromTarget && !SpreadSunderFromTarget)
                    return;

                var sourceStatuses = spreadSnapshot.Capture(target);
                if (SpreadBleedFromTarget && sourceStatuses.Bleeding)
                {
                    SpreadStatusToNearbyHostile(
                        activator,
                        target,
                        typeof(BleedStatusEffect),
                        SpreadBleedDurationSeconds > 0 ? SpreadBleedDurationSeconds : 30f,
                        damageType,
                        spreadSnapshot);
                }

                if (SpreadHemorrhageFromTarget && sourceStatuses.Bleeding)
                {
                    SpreadStatusToNearbyHostile(
                        activator,
                        target,
                        typeof(HemorrhageStatusEffect),
                        SpreadHemorrhageDurationSeconds > 0 ? SpreadHemorrhageDurationSeconds : 30f,
                        damageType,
                        spreadSnapshot);
                }

                if (SpreadSunderFromTarget && sourceStatuses.Sundered)
                {
                    SpreadStatusToNearbyHostile(
                        activator,
                        target,
                        typeof(SunderStatusEffect),
                        SpreadSunderDurationSeconds > 0 ? SpreadSunderDurationSeconds : 30f,
                        damageType,
                        spreadSnapshot);
                }
            }

            private void ApplySuppressionEffects(uint activator, uint target, CombatDamageType damageType)
            {
                if (ApplySuppressionStackOnHit && SuppressionStackDurationSeconds > 0)
                {
                    Combat.ApplySuppressionStack(
                        activator,
                        target,
                        SuppressionStackEvasionPenaltyPercent,
                        SuppressionStackDurationSeconds,
                        damageType);
                }

                if (SuppressionDisorientedRequiredStacks <= 0 ||
                    SuppressionDisorientedDurationSeconds <= 0 ||
                    Combat.GetSuppressionStackCount(target, activator) < SuppressionDisorientedRequiredStacks)
                {
                    return;
                }

                StatusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(DisorientedStatusEffect),
                    SuppressionDisorientedDurationSeconds,
                    ResistanceType.Mind);
            }

            private static void SpreadStatusToNearbyHostile(
                uint activator,
                uint sourceTarget,
                Type statusEffect,
                float duration,
                CombatDamageType damageType,
                StatusSpreadSnapshot spreadSnapshot)
            {
                if (duration <= 0f || !GetIsObjectValid(sourceTarget))
                    return;

                foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(
                             activator,
                             GetLocation(sourceTarget),
                             5f,
                             1,
                             predicate: nearby => nearby != sourceTarget && !StatusEffect.HasStatusEffect(nearby, statusEffect)))
                {
                    spreadSnapshot.TrySpread(() =>
                    {
                        // Remember the recipient's prerequisites before spreading, so an enemy
                        // hit later in this cast cannot chain a newly received status onward.
                        spreadSnapshot.Capture(nearby);
                        return StatusEffect.ApplyStatusEffect(activator, nearby, statusEffect, duration, damageType);
                    });
                    break;
                }
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

                return StatusEffect.ApplyStatusEffect(
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
                    var temporaryHP = Math.Max(
                        1,
                        GetMaxHitPoints(maximumHPSource) * FriendlyTargetTemporaryHPPercent / 100);
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
                StatusEffect.ApplyStatusEffect(activator, activator, selfStatus, duration > 0f ? duration : 0f);
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

                StatusEffect.ApplyStatusEffect(activator, guardedTarget, statusEffect, duration);
            }
        }

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
            WeaponAbilityProfile profile = null)
        {
            profile ??= WeaponAbilityProfile.Empty;
            var temporaryHPEffectKey = $"WEAPON_{ability.ActiveEffectiveLevelPerkType}";

            ApplyCombatImpactDamageAbility(ability, combatImpactDamageAbility);

            ability.HasActivationDelay(activationDelay)
                .SkillType(skill)
                .UsesImpactAnimation(impactAnimation)
                .HasActivationAction((activator, target, level, targetLocation) =>
                {
                    profile.PrepareActivationIdleBonuses(activator);
                    if (isHostile && !profile.IsQueuedWeaponAbility)
                        Combat.StoreAbilityActivationIdleBonuses(activator, skill);
                    profile.PrepareQueuedActivation(activator, skill);
                    if (profile.IsQueuedWeaponAbility)
                        Combat.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(activator, skill);
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
                                profile.AfterActivation(activator, skill);
                            }
                            return;
                        }

                        if (isFriendlyTarget && profile.HasFriendlyTargetEffects())
                        {
                            profile.ApplyFriendlyTargetEffects(activator, target, temporaryHPEffectKey);
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
                            StatusEffect.ApplyStatusEffect(activator, activator, statusEffect, duration > 0f ? duration : 0f);
                            profile.ApplySelfStatusToGuardedTarget(activator, statusEffect, duration);
                        }

                            profile.AfterActivation(activator, skill);
                        return;
                    }

                    profile.SpendHitPoints(activator);
                    profile.AfterHostileActivation(activator);
                    var spreadSnapshot = new WeaponAbilityProfile.StatusSpreadSnapshot(profile.MaximumStatusSpreadsPerCast);
                    Action<uint> captureSpreadPrerequisites = profile.SpreadBleedFromTarget || profile.SpreadHemorrhageFromTarget || profile.SpreadSunderFromTarget
                        ? impactedTarget => spreadSnapshot.Capture(impactedTarget)
                        : null;
                    var activationIdleBonusSnapshot = profile.CaptureActivationIdleBonusSnapshot(activator);
                    Ability.AddActiveAbilityDefenseIgnorePercentAdjustment(
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
                                duration,
                                statusEffect,
                                ToCombatImpactAreaShape(targetingShape),
                                profile.TelegraphDuration,
                                targetingSizeX > 0f ? targetingSizeX : 5.0f,
                                targetingSizeY,
                                additionalStatusEffects,
                                CenterAreaOnActivator(targetingFlags),
                                profile.StatusEffectFactory,
                                damageType: profile.DamageType,
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
                                    profile.AfterSuccessfulHit(activator, impactedTarget, profile.DamageType, spreadSnapshot);
                                    Combat.ApplyRangedAbilityTargetDefenseReduction(activator, impactedTarget, skill);
                                },
                                beforeSuccessfulImpactRiders: impactedTarget => profile.BeforeSuccessfulImpactRiders(activator, impactedTarget),
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
                            Ability.GetActiveAbilityImpactSummary(activator),
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
                            duration,
                            statusEffect,
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
                                profile.AfterSuccessfulHit(activator, impactedTarget, profile.DamageType, spreadSnapshot);
                                Combat.ApplyRangedAbilityTargetDefenseReduction(activator, impactedTarget, skill);
                            },
                            beforeSuccessfulImpactRiders: impactedTarget => profile.BeforeSuccessfulImpactRiders(activator, impactedTarget),
                            hitChancePercentAdjustment: profile.HitChancePercentAdjustment,
                            criticalRatePercentAdjustment: profile.GetCriticalRateAdjustment(
                                activator,
                                target,
                                activationIdleBonusSnapshot));
                        totalDamage += hitDamage;
                        if (hitDamage > 0)
                            successfulHitCount++;
                    }

                    profile.RestoreResourcesForTargetCount(activator, Ability.GetActiveAbilityImpactSummary(activator));
                    profile.AfterImpact(activator, totalDamage, successfulHitCount, Ability.GetActiveAbilityImpactSummary(activator));
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

        protected static void ConfigureCastedTarget(
            AbilityBuilder ability,
            SkillType skill,
            int baseDamage,
            int stamina,
            int duration = 0,
            Type statusEffect = null,
            int extraDamageWhenLowHp = 0,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid)
        {
            ApplyCombatImpactDamageAbility(ability, combatImpactDamageAbility);

            ability.HasActivationDelay(0f)
                .SkillType(skill)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var damage = baseDamage;
                    if (extraDamageWhenLowHp > 0 && GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * 0.3f)
                    {
                        damage += extraDamageWhenLowHp;
                    }

                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        skill,
                        damage,
                        duration,
                        statusEffect,
                        false,
                        combatImpactDamageAbility: combatImpactDamageAbility);
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigureMultiHit(
            AbilityBuilder ability,
            SkillType skill,
            int baseDamage,
            int hits,
            int stamina,
            int duration = 0,
            Type statusEffect = null,
            Type additionalStatusEffect = null,
            Type bonusStatus = null,
            int bonusDamage = 0,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid)
        {
            ApplyCombatImpactDamageAbility(ability, combatImpactDamageAbility);

            ability.HasActivationDelay(0f)
                .SkillType(skill)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var damage = baseDamage;
                    if (bonusStatus != null && StatusEffect.HasStatusEffect(target, bonusStatus))
                    {
                        damage += bonusDamage;
                    }

                    for (var i = 0; i < hits; i++)
                    {
                        Ability.ApplyCombatImpact(
                            activator,
                            target,
                            targetLocation,
                            skill,
                            damage,
                            duration,
                            statusEffect,
                            false,
                            additionalStatusEffects: Additional(additionalStatusEffect),
                            combatImpactDamageAbility: combatImpactDamageAbility);
                    }
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigureInterrupt(
            AbilityBuilder ability,
            SkillType skill,
            int baseDamage,
            int duration,
            Type statusEffect,
            int stamina,
            Func<IStatusEffect> statusEffectFactory = null,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid)
        {
            ApplyCombatImpactDamageAbility(ability, combatImpactDamageAbility);

            ability.HasActivationDelay(0f)
                .SkillType(skill)
                .IsSingleTargetAbility()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    AssignCommand(target, () => ClearAllActions());
                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        skill,
                        baseDamage,
                        duration,
                        statusEffect,
                        false,
                        statusEffectFactory: statusEffectFactory,
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
                {
                    StatusEffect.RemoveOtherStanceStatuses(activator, type);
                    StatusEffect.ApplyStatusEffect(activator, activator, statusEffectFactory(), 0f);
                })
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
                {
                    var statusEffect = statusEffectFactory();
                    StatusEffect.ApplyStatusEffect(activator, activator, statusEffect, duration);
                    additionalAction?.Invoke(activator);
                })
                .IsCastedAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigureTargetStatus(AbilityBuilder ability, Type type, float duration, int stamina)
        {
            ability.HasActivationDelay(0f)
                .RequiresTarget()
                .IsSingleTargetAbility()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    StatusEffect.ApplyStatusEffect(activator, target, type, duration, CombatDamageType.Physical);
                    Ability.ApplyHostileAbilityEnmity(activator, target);
                })
                .IsCastedAbility()
                .IsHostileAbility()
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
                .HasImpactAction((activator, target, level, targetLocation) => ApplyStatusToNearbyParty(activator, statusEffectFactory, duration, includeSelf))
                .IsCastedAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static void ConfigureAreaStatus(
            AbilityBuilder ability,
            Type type,
            float duration,
            int stamina,
            bool centerOnActivator,
            int fpDrainPercent = 0,
            int restoreStamina = 0,
            float activationDelay = 0f)
        {
            ability.HasActivationDelay(activationDelay)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ApplyStatusToNearbyEnemies(activator, target, targetLocation, type, duration, centerOnActivator, fpDrainPercent);
                    if (restoreStamina > 0)
                    {
                        Stat.RestoreStamina(activator, restoreStamina);
                    }
                })
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }

        protected static bool ToggleSelfStatus(uint activator, Type type)
        {
            if (!StatusEffect.HasStatusEffect(activator, type))
                return true;

            StatusEffect.RemoveStatusEffect(activator, type, false);
            SendMessageToPC(activator, $"{StatusEffect.GetStatusEffectName(type)} deactivated.");
            return false;
        }

        protected static IEnumerable<Type> Additional(Type additionalStatusEffect)
        {
            return additionalStatusEffect == null
                ? null
                : new[] { additionalStatusEffect };
        }

        protected static Func<IStatusEffect> FoggyMind(int activationDelaySeconds)
        {
            return () => new FoggyMindStatusEffect(activationDelaySeconds);
        }

        protected static void ApplyStatusToNearbyParty(uint activator, Type type, float duration, bool includeSelf)
        {
            ApplyStatusToNearbyParty(activator, () => (IStatusEffect)Activator.CreateInstance(type), duration, includeSelf);
        }

        protected static void ApplyStatusToNearbyParty(uint activator, Func<IStatusEffect> statusEffectFactory, float duration, bool includeSelf)
        {
            if (includeSelf)
            {
                StatusEffect.ApplyStatusEffect(activator, activator, statusEffectFactory(), duration);
            }

            var location = GetLocation(activator);
            var creature = GetFirstObjectInShape(Shape.Sphere, 5f, location, true);

            while (GetIsObjectValid(creature))
            {
                if (creature != activator && Party.IsInParty(activator, creature))
                {
                    StatusEffect.ApplyStatusEffect(activator, creature, statusEffectFactory(), duration, CombatDamageType.Physical);
                }

                creature = GetNextObjectInShape(Shape.Sphere, 5f, location, true);
            }
        }

        protected static void ApplyStatusToNearbyEnemies(
            uint activator,
            uint target,
            Location targetLocation,
            Type type,
            float duration,
            bool centerOnActivator,
            int fpDrainPercent,
            VisualEffect targetVisualEffect = VisualEffect.None,
            VisualEffect areaVisualEffect = VisualEffect.None)
        {
            var location = GetAreaStatusLocation(activator, target, targetLocation, centerOnActivator);

            if (areaVisualEffect != VisualEffect.None)
            {
                ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(areaVisualEffect), location);
            }

            var creature = GetFirstObjectInShape(Shape.Sphere, 5f, location, true);

            while (GetIsObjectValid(creature))
            {
                if (GetIsReactionTypeHostile(creature, activator))
                {
                    if (targetVisualEffect != VisualEffect.None)
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(targetVisualEffect), creature);
                    }

                    StatusEffect.ApplyStatusEffect(activator, creature, type, duration, CombatDamageType.Physical);
                    Ability.ApplyHostileAbilityEnmity(activator, creature);
                    if (fpDrainPercent > 0)
                    {
                        var fpDrain = GameMath.PercentOf(Stat.GetCurrentFP(creature), fpDrainPercent);
                        Stat.ReduceFP(creature, fpDrain);
                    }
                }

                creature = GetNextObjectInShape(Shape.Sphere, 5f, location, true);
            }
        }

        protected static Location GetAreaStatusLocation(uint activator, uint target, Location targetLocation, bool centerOnActivator)
        {
            if (centerOnActivator)
            {
                return GetLocation(activator);
            }

            if (GetIsObjectValid(target))
            {
                return GetLocation(target);
            }

            return GetIsObjectValid(GetAreaFromLocation(targetLocation))
                ? targetLocation
                : GetLocation(activator);
        }

        protected static void PurifyAndMirror(uint activator)
        {
            var debuff = StatusEffect.GetCreatureStatusEffects(activator)
                .GetAllEffects()
                .FirstOrDefault(effect => StatusEffect.HasCleanseType(effect, StatusEffectCleanseType.Purify));

            if (debuff == null)
                return;

            var debuffType = debuff.GetType();
            var mirroredDebuff = debuff.Clone();
            var mirroredDuration = GetRemainingDurationSeconds(debuff);
            StatusEffect.RemoveStatusEffect(activator, debuffType, false);

            var enemy = GetNearestHostile(activator, 5f);
            if (GetIsObjectValid(enemy))
            {
                StatusEffect.ApplyStatusEffect(activator, enemy, mirroredDebuff, mirroredDuration);
                Ability.ApplyHostileAbilityEnmity(activator, enemy);
            }
        }

        private static float GetRemainingDurationSeconds(IStatusEffect statusEffect)
        {
            if (statusEffect.DurationTicks < 0)
                return 0f;

            return Math.Max(0.1f, statusEffect.DurationTicks * Math.Max(1f, statusEffect.Frequency));
        }

        protected static uint GetNearestHostile(uint activator, float radius)
        {
            var nth = 1;
            var location = GetLocation(activator);
            var creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);

            while (GetIsObjectValid(creature) && GetDistanceBetweenLocations(location, GetLocation(creature)) <= radius)
            {
                if (GetIsReactionTypeHostile(creature, activator))
                    return creature;

                nth++;
                creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
            }

            return OBJECT_INVALID;
        }
    }
}
