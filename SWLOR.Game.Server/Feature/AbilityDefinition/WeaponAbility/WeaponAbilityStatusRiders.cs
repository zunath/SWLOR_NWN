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
    /// The statuses a landed hit adds to its target: conditional statuses, the spread of bleed, hemorrhage and sunder to nearby enemies, and suppression.
    /// </summary>
    public class WeaponAbilityStatusRiders
    {
        private ICombatActivityService CombatActivity { get; }
        private ICombatDefenseMemoryService CombatDefenseMemory { get; }
        private ICombatQueriesService CombatQueries { get; }
        private ICombatSuppressionService CombatSuppression { get; }
        private IStatusEffectService StatusEffect { get; }
        private IStatusEffectApplicationService StatusEffectApplication { get; }
        private IStatusEffectCatalogService StatusEffectCatalog { get; }

        public WeaponAbilityStatusRiders(ICombatActivityService combatActivity, ICombatDefenseMemoryService combatDefenseMemory, ICombatQueriesService combatQueries, ICombatSuppressionService combatSuppression, IStatusEffectService statusEffect, IStatusEffectApplicationService statusEffectApplication, IStatusEffectCatalogService statusEffectCatalog)
        {
            CombatActivity = combatActivity;
            CombatDefenseMemory = combatDefenseMemory;
            CombatQueries = combatQueries;
            CombatSuppression = combatSuppression;
            StatusEffect = statusEffect;
            StatusEffectApplication = statusEffectApplication;
            StatusEffectCatalog = statusEffectCatalog;
        }

        public bool IsBleeding(uint target)
        {
            return StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Bleeding);
        }

        public (bool Bleeding, bool Sundered) ReadSpreadSource(uint target)
        {
            return (IsBleeding(target), StatusEffect.HasStatusEffect<SunderStatusEffect>(target));
        }

        public StatusEffectCategory? GetAppliedCategories(bool applied, Type statusEffectType) =>
            applied ? StatusEffectCatalog.CreateStatusEffect(statusEffectType).Categories : null;

        public StatusEffectCategory? ApplyConditionalStatus(WeaponAbilityProfile profile, uint activator, uint target, CombatDamageType damageType)
        {
            if (profile.ConditionalStatusEffect == null ||
                profile.ConditionalStatusDurationSeconds <= 0 ||
                profile.ConditionalStatusAfterDeflectionWindowSeconds <= 0 ||
                !CombatDefenseMemory.HasRecentDeflection(activator, DeflectionSource.Ranged, profile.ConditionalStatusAfterDeflectionWindowSeconds))
            {
                return null;
            }

            return GetAppliedCategories(
                StatusEffectApplication.ApplyStatusEffect(
                    activator,
                    target,
                    profile.ConditionalStatusEffect,
                    profile.ConditionalStatusDurationSeconds,
                    damageType),
                profile.ConditionalStatusEffect);
        }

        public StatusEffectCategory? ApplyTargetConditionalStatus(WeaponAbilityProfile profile, uint activator, uint target, CombatDamageType damageType)
        {
            if (profile.ConditionalTargetStatusEffect == null || profile.ConditionalTargetStatusDurationSeconds <= 0)
                return null;

            if (profile.RequireBehindForConditionalStatus && !CombatQueries.IsTargetNotFacingAttacker(activator, target))
                return null;

            if (profile.RequiredTargetStatusEffectForConditionalStatus != null &&
                !StatusEffect.HasStatusEffect(target, profile.RequiredTargetStatusEffectForConditionalStatus))
            {
                return null;
            }

            if (profile.RequiredTargetStatusCategoryForConditionalStatus != 0 &&
                !StatusEffect.HasStatusEffectCategory(target, profile.RequiredTargetStatusCategoryForConditionalStatus))
            {
                return null;
            }

            var window = profile.ProtectedTargetHitWindowSeconds;
            if (profile.RequireRecentGuardedAllyHitForConditionalStatus &&
                !GuardedStatusEffect.HasRecentGuardedAllyHit(activator, target, window))
            {
                return null;
            }

            if (profile.RequireRecentWardHitForConditionalStatus &&
                !WardBondStatusEffect.HasRecentWardHit(activator, target, window))
            {
                return null;
            }

            if (profile.RequireTargetRecentlyDamagedActivatorForConditionalStatus &&
                !CombatActivity.HasRecentDamageTarget(target, activator, window))
            {
                return null;
            }

            return GetAppliedCategories(
                StatusEffectApplication.ApplyStatusEffect(
                    activator,
                    target,
                    profile.ConditionalTargetStatusEffect,
                    profile.ConditionalTargetStatusDurationSeconds,
                    damageType,
                    profile.ConvertsRequiredTargetStatusEffect ? profile.RequiredTargetStatusEffectForConditionalStatus : null),
                profile.ConditionalTargetStatusEffect);
        }

        public void ApplyStatusSpread(WeaponAbilityProfile profile, uint activator, uint target, CombatDamageType damageType, WeaponAbilityProfile.StatusSpreadSnapshot spreadSnapshot)
        {
            if (!profile.SpreadBleedFromTarget && !profile.SpreadHemorrhageFromTarget && !profile.SpreadSunderFromTarget)
                return;

            var sourceStatuses = spreadSnapshot.Capture(target);
            if (profile.SpreadBleedFromTarget && sourceStatuses.Bleeding)
            {
                SpreadStatusToNearbyHostile(
                    activator,
                    target,
                    typeof(BleedStatusEffect),
                    profile.SpreadBleedDurationSeconds > 0 ? profile.SpreadBleedDurationSeconds : 30f,
                    damageType,
                    spreadSnapshot);
            }

            if (profile.SpreadHemorrhageFromTarget && sourceStatuses.Bleeding)
            {
                SpreadStatusToNearbyHostile(
                    activator,
                    target,
                    typeof(HemorrhageStatusEffect),
                    profile.SpreadHemorrhageDurationSeconds > 0 ? profile.SpreadHemorrhageDurationSeconds : 30f,
                    damageType,
                    spreadSnapshot);
            }

            if (profile.SpreadSunderFromTarget && sourceStatuses.Sundered)
            {
                // Spread the source's own Sunder strength rather than the default one.
                var sourceSunder = StatusEffect.GetStatusEffect(target, typeof(SunderStatusEffect));
                SpreadStatusToNearbyHostile(
                    activator,
                    target,
                    typeof(SunderStatusEffect),
                    profile.SpreadSunderDurationSeconds > 0 ? profile.SpreadSunderDurationSeconds : 30f,
                    damageType,
                    spreadSnapshot,
                    profile.MaximumStatusSpreadsPerSource,
                    sourceSunder == null ? null : sourceSunder.Clone);
            }
        }

        private void SpreadStatusToNearbyHostile(
            uint activator,
            uint sourceTarget,
            Type statusEffect,
            float duration,
            CombatDamageType damageType,
            WeaponAbilityProfile.StatusSpreadSnapshot spreadSnapshot,
            int maximumRecipients = 1,
            Func<IStatusEffect> createStatus = null)
        {
            if (duration <= 0f || !GetIsObjectValid(sourceTarget))
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(
                         activator,
                         GetLocation(sourceTarget),
                         5f,
                         Math.Max(1, maximumRecipients),
                         predicate: nearby => nearby != sourceTarget && !StatusEffect.HasStatusEffect(nearby, statusEffect)))
            {
                spreadSnapshot.TrySpread(() =>
                {
                    // Remember the recipient's prerequisites before spreading, so an enemy
                    // hit later in this cast cannot chain a newly received status onward.
                    spreadSnapshot.Capture(nearby);
                    return createStatus == null
                        ? StatusEffectApplication.ApplyStatusEffect(activator, nearby, statusEffect, duration, damageType)
                        : StatusEffectApplication.ApplyStatusEffect(activator, nearby, createStatus(), duration, damageType);
                });
            }
        }

        public void ApplySuppressionEffects(WeaponAbilityProfile profile, uint activator, uint target, CombatDamageType damageType)
        {
            if (profile.ApplySuppressionStackOnHit && profile.SuppressionStackDurationSeconds > 0)
            {
                CombatSuppression.ApplySuppressionStack(
                    activator,
                    target,
                    profile.SuppressionStackEvasionPenaltyPercent,
                    profile.SuppressionStackDurationSeconds,
                    damageType);
            }

            if (profile.SuppressionDisorientedRequiredStacks <= 0 ||
                profile.SuppressionDisorientedDurationSeconds <= 0 ||
                CombatSuppression.GetSuppressionStackCount(target, activator) < profile.SuppressionDisorientedRequiredStacks)
            {
                return;
            }

            StatusEffectApplication.ApplyStatusEffect(
                activator,
                target,
                typeof(DisorientedStatusEffect),
                profile.SuppressionDisorientedDurationSeconds,
                ResistanceType.Mind);
        }
    }
}
