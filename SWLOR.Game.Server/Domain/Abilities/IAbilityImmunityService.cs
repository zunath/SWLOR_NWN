using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Temporary immunities after a control effect (part 19 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md).
    /// Static callers reach the container's instance through <c>GameServices.AbilityImmunity</c>.
    /// </summary>
    public interface IAbilityImmunityService
    {
        /// <summary>
        /// Applies a temporary immunity effect to a particular target.
        /// This will add 20 seconds on top of whatever the ability duration length is.
        /// An existing immunity of the same type is replaced so its timer restarts from this
        /// application without shortening a longer remaining immunity.
        /// If the immunity is one of the hard-CC types, this also grants temporary immunity
        /// to every other hard-CC type for the same duration.
        /// </summary>
        /// <param name="target">The target receiving the immunity</param>
        /// <param name="abilityDuration">The length of the ability's duration. This will be added on top of the 20 seconds.</param>
        /// <param name="immunity">The type of immunity to apply.</param>
        void ApplyTemporaryImmunity(uint target, float abilityDuration, ImmunityType immunity);

        /// <summary>
        /// Applies the remaining post-control immunity after accounting for time that elapsed
        /// while the affected creature was logged out.
        /// </summary>
        void ApplyPostControlImmunity(
            uint target,
            float secondsSinceControlEnded,
            ImmunityType immunity);

        bool HasTemporaryImmunity(uint target, ImmunityType immunity);

        /// <summary>
        /// Checks whether the target is immune to a hard-CC type: it still has immunity to that
        /// specific type, it recently suffered a different hard-CC type and is still within the
        /// shared post-control immunity window, or a hard-CC status is active on it right now -
        /// controls do not stack, they queue behind the post-control window.
        /// </summary>
        bool HasHardCrowdControlImmunity(uint target, ImmunityType immunity);
    }
}
