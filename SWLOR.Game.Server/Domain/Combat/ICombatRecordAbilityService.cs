using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability records part of the combat recorder: casts, refusals, interruptions, queued abilities, impacts, the cost of an
    /// activation and the note that a toggled stance was turned off. Static callers reach the container's instance through
    /// <c>GameServices.CombatRecordAbility</c>.
    /// </summary>
    public interface ICombatRecordAbilityService
    {
        void RecordAbilityCast(
            uint activator,
            uint target,
            Location targetLocation,
            AbilityDetail ability,
            FeatType feat,
            float activationDelay,
            bool isToggleOff = false);

        /// <summary>
        /// Records an attempted activation the ability's rules refused, with the refusal shown.
        /// </summary>
        /// <param name="atCompletion">True when the cast was refused as it completed.</param>
        void RecordAbilityDenied(
            uint activator,
            uint target,
            AbilityDetail ability,
            FeatType feat,
            string reason,
            bool atCompletion);

        /// <summary>
        /// Records a cast or channel stopped before it completed. A damage interruption names the
        /// creature that dealt the damage; an <see cref="CombatRecordInterruptCause.Other"/> stop made
        /// inside another creature's scope is that creature's crowd control.
        /// </summary>
        void RecordAbilityInterrupted(
            uint activator,
            AbilityDetail ability,
            CombatRecordInterruptCause cause,
            uint damagedBy = OBJECT_INVALID);

        void RecordAbilityQueued(uint activator, uint target, AbilityDetail ability, FeatType feat);

        void RecordAbilityImpact(
            uint activator,
            uint target,
            Location targetLocation,
            AbilityDetail ability,
            FeatType feat,
            AbilityImpactSummary summary);

        /// <summary>
        /// Notes that an activation turned a toggled stance off, which is a use of the ability
        /// even though its activation action reports no impact.
        /// </summary>
        void NoteToggleDeactivated(uint activator);

        /// <summary>
        /// Whether the activation that just ran turned <paramref name="activator"/>'s stance off.
        /// </summary>
        bool TakeToggleDeactivation(uint activator);

        /// <summary>
        /// Wraps an ability's activation requirements: measures the FP and STM they take and
        /// attributes resource changes inside the scope (including consumed items) to the
        /// ability's cost. Hit points an ability spends are self-damage during its impact and
        /// are recorded as damage attributed to the cost.
        /// </summary>
        CombatRecordAbilityCostScope BeginAbilityCost(uint activator, AbilityDetail ability, FeatType feat = FeatType.Invalid);

        void CompleteAbilityCost(in CombatRecordAbilityCostScope cost);
    }
}
