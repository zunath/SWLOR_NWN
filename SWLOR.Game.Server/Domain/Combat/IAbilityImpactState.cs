using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The impacts in progress and the last completed summary of each activator (part 2 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md). The ability impact lifecycle writes the store; Combat reads it, and
    /// Character reads the active summary through <see cref="IActiveAbilityImpact"/>. Static callers reach the container's
    /// instance through <c>GameServices.AbilityImpactState</c>. The reads run per hit and allocate nothing.
    /// </summary>
    public interface IAbilityImpactState : IActiveAbilityImpact
    {
        /// <summary>Starts tracking an impact for the activator, replacing the one it tracks.</summary>
        void Track(uint activator, AbilityImpactTracker impact);

        /// <summary>The impact the activator is resolving, if it is tracked.</summary>
        bool TryGetTrackedAbilityImpact(uint activator, out AbilityImpactTracker impact);

        /// <summary>Stops tracking the activator's impact.</summary>
        void Remove(uint activator);

        /// <summary>Records the summary of the impact the activator just completed.</summary>
        void SetLastCompletedAbilityImpactSummary(uint activator, AbilityImpactSummary summary);

        /// <summary>The impact the activator is resolving, or null.</summary>
        AbilityImpactTracker GetTrackedAbilityImpact(uint activator);

        AbilityImpactSequence GetAbilityImpactSequence(uint activator);

        int GetWeaponTraitAccuracy(uint activator);

        /// <summary>
        /// Native current HP includes temporary HP. Subtract queued damage so another hit
        /// cannot draw healing from health already claimed by an earlier impact.
        /// </summary>
        int GetRemainingDamageTargetHP(uint activator, uint target);

        /// <summary>
        /// Reuses a payload's contributing sources within one impact. Delayed phases and
        /// recurring pulses receive fresh trackers, while conditions can still be checked per target.
        /// The caller's stat service reads the sources the impact has not cached.
        /// </summary>
        IReadOnlyList<StatAdjustmentSource> GetAbilityImpactStatSources(uint activator, StatType payload, IStatService stat);

        bool TryTriggerAreaAbilityPulse(uint activator);

        /// <summary>
        /// The summary of the activator's most recently COMPLETED ability impact, or null if
        /// none completed since the last clear. Observability seam for the engine test
        /// harness: a queued weapon ability's damage rides the same landed hit as the weapon
        /// swing, so an HP drop alone cannot attribute damage to the ability - a completed
        /// summary with impacted targets can.
        /// </summary>
        AbilityImpactSummary GetLastCompletedAbilityImpactSummary(uint activator);

        /// <summary>
        /// Clears the activator's last completed impact summary so a subsequent
        /// <see cref="GetLastCompletedAbilityImpactSummary"/> observation cannot match an
        /// earlier ability's impact.
        /// </summary>
        void ClearLastCompletedAbilityImpactSummary(uint activator);

        /// <summary>Queues damage on the activator's impact for its end, under the recorder's current scope; false when none is tracked.</summary>
        bool TryQueueTrackedDamageEffect(uint activator, uint target, int damage, DamageType damageType, ICombatRecorderService recorder);

        void AddActiveAbilityDefenseIgnorePercentAdjustment(uint activator, int adjustment);

        /// <summary>Records the impact's skill and shape.</summary>
        void RecordAbilityImpactShape(uint activator, SkillType skillType, bool isArea);

        /// <summary>Records a target the impact reached, with the impact's skill and shape.</summary>
        void RecordAbilityImpactTarget(uint activator, uint target, SkillType skillType, bool isArea);

        /// <summary>Whether the activator's impact has become an area impact.</summary>
        bool IsTrackedAbilityArea(uint activator);

        /// <summary>Only a melee technique's cast can change shape (its conditional sweep); other abilities keep their authored flags.</summary>
        bool IsAreaImpact(uint activator, AbilityDetail ability);

        bool IsAreaImpact(AbilityImpactSummary summary, AbilityDetail ability);

        bool IsSingleTargetImpact(uint activator, AbilityDetail ability);

        /// <summary>Whether the activator's impact is a single-target impact.</summary>
        bool IsTrackedAbilitySingleTarget(uint activator);
    }
}
