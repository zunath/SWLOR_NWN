using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The impacts in progress and the last completed summary of each activator (part 2 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md). Combat owns the store: the ability impact lifecycle writes it and Combat
    /// reads it (supply rule 1 of MaintainabilityRefactorDomainOrder.md), so no Combat service reaches up to Abilities for the
    /// state of an impact. The reads moved from <c>AbilityService</c> unchanged; the store takes no service, and the two reads
    /// that need one (the stat sources, the recorder scope of a queued damage effect) take it from their caller. Character reads
    /// the active summary through <see cref="IActiveAbilityImpact"/>.
    /// </summary>
    public class AbilityImpactStore : IAbilityImpactState
    {
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, AbilityImpactTracker> _trackedAbilityImpacts = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, AbilityImpactSummary> _lastCompletedImpactSummaries = new();

        /// <summary>Starts tracking an impact for the activator, replacing the one it tracks.</summary>
        public void Track(uint activator, AbilityImpactTracker impact)
        {
            _trackedAbilityImpacts[activator] = impact;
        }

        /// <summary>The impact the activator is resolving, if it is tracked.</summary>
        public bool TryGetTrackedAbilityImpact(uint activator, out AbilityImpactTracker impact)
        {
            return _trackedAbilityImpacts.TryGetValue(activator, out impact);
        }

        /// <summary>Stops tracking the activator's impact.</summary>
        public void Remove(uint activator)
        {
            _trackedAbilityImpacts.Remove(activator);
        }

        /// <summary>Records the summary of the impact the activator just completed.</summary>
        public void SetLastCompletedAbilityImpactSummary(uint activator, AbilityImpactSummary summary)
        {
            _lastCompletedImpactSummaries[activator] = summary;
        }

        public AbilityImpactSequence GetAbilityImpactSequence(uint activator)
        {
            return GetTrackedAbilityImpact(activator)?.Sequence;
        }

        public int GetWeaponTraitAccuracy(uint activator) =>
            GetTrackedAbilityImpact(activator)?.Sequence.WeaponTraits is { IsCommitted: true } traits
                ? traits.Snapshot.AccuracyAdjustment : 0;

        /// <summary>
        /// Native current HP includes temporary HP. Subtract queued damage so another hit
        /// cannot draw healing from health already claimed by an earlier impact.
        /// </summary>
        public int GetRemainingDamageTargetHP(uint activator, uint target)
        {
            if (!GetIsObjectValid(target) || GetPlotFlag(target))
                return 0;

            var pendingDamage = GetTrackedAbilityImpact(activator)?.GetPendingDamage(target) ?? 0;
            var currentHP = GetCurrentHitPoints(target);
            return CombatRules.CalculateDamageEligibleForHealing(currentHP, currentHP, pendingDamage);
        }

        /// <summary>
        /// Reuses a payload's contributing sources within one impact. Delayed phases and
        /// recurring pulses receive fresh trackers, while conditions can still be checked per target.
        /// </summary>
        public IReadOnlyList<StatAdjustmentSource> GetAbilityImpactStatSources(uint activator, StatType payload, IStatService stat)
        {
            var impact = GetTrackedAbilityImpact(activator);
            return impact == null ? stat.GetStatSources(activator, payload) : impact.GetStatSources(activator, payload, stat);
        }

        public bool TryTriggerAreaAbilityPulse(uint activator)
        {
            return GetAbilityImpactSequence(activator)?.TryTriggerAreaPulse() == true;
        }

        /// <summary>
        /// The summary of the activator's most recently COMPLETED ability impact, or null if
        /// none completed since the last clear. Observability seam for the engine test
        /// harness: a queued weapon ability's damage rides the same landed hit as the weapon
        /// swing, so an HP drop alone cannot attribute damage to the ability - a completed
        /// summary with impacted targets can.
        /// </summary>
        public AbilityImpactSummary GetLastCompletedAbilityImpactSummary(uint activator)
        {
            return _lastCompletedImpactSummaries.TryGetValue(activator, out var summary)
                ? summary
                : null;
        }

        /// <summary>
        /// Clears the activator's last completed impact summary so a subsequent
        /// <see cref="GetLastCompletedAbilityImpactSummary"/> observation cannot match an
        /// earlier ability's impact.
        /// </summary>
        public void ClearLastCompletedAbilityImpactSummary(uint activator)
        {
            _lastCompletedImpactSummaries.Remove(activator);
        }

        public bool TryQueueTrackedDamageEffect(uint activator, uint target, int damage, DamageType damageType, ICombatRecorderService recorder)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact == null)
                return false;

            trackedImpact.QueueDamageEffect(target, damage, damageType, recorder);
            return true;
        }

        public AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator)
        {
            return GetTrackedAbilityImpact(activator)?.Summary;
        }

        public void AddActiveAbilityDefenseIgnorePercentAdjustment(uint activator, int adjustment)
        {
            if (adjustment == 0)
                return;

            GetTrackedAbilityImpact(activator)?.AddDefenseIgnorePercentAdjustment(adjustment);
        }

        public AbilityImpactTracker GetTrackedAbilityImpact(uint activator)
        {
            return _trackedAbilityImpacts.TryGetValue(activator, out var impact)
                ? impact
                : null;
        }

        public void RecordAbilityImpactShape(uint activator, SkillType skillType, bool isArea)
        {
            var impact = GetTrackedAbilityImpact(activator);
            if (impact == null)
                return;

            impact.RecordShape(skillType, isArea);
        }

        public void RecordAbilityImpactTarget(uint activator, uint target, SkillType skillType, bool isArea)
        {
            var impact = GetTrackedAbilityImpact(activator);
            if (impact == null || !GetIsObjectValid(target))
                return;

            impact.RecordShape(skillType, isArea);
            impact.RecordTarget(target);
        }

        public bool IsTrackedAbilityArea(uint activator)
        {
            var impact = GetTrackedAbilityImpact(activator);
            return impact?.Summary.IsAreaAbility ?? false;
        }

        /// <summary>Only a melee technique's cast can change shape (its conditional sweep); other abilities keep their authored flags.</summary>
        public bool IsAreaImpact(uint activator, AbilityDetail ability) =>
            IsAreaImpact(GetTrackedAbilityImpact(activator)?.Summary, ability);

        public bool IsAreaImpact(AbilityImpactSummary summary, AbilityDetail ability) =>
            UsesTrackedImpactShape(summary, ability) ? summary.IsAreaAbility : ability?.IsAreaAbility == true;

        public bool IsSingleTargetImpact(uint activator, AbilityDetail ability)
        {
            var summary = GetTrackedAbilityImpact(activator)?.Summary;
            return UsesTrackedImpactShape(summary, ability)
                ? summary.IsSingleTargetAbility : ability?.IsSingleTargetAbility == true;
        }

        private bool UsesTrackedImpactShape(AbilityImpactSummary summary, AbilityDetail ability) =>
            ability?.MeleeTechnique != null && summary?.Ability == ability;

        public bool IsTrackedAbilitySingleTarget(uint activator)
        {
            var impact = GetTrackedAbilityImpact(activator);
            return impact?.Summary.IsSingleTargetAbility ?? false;
        }
    }
}
