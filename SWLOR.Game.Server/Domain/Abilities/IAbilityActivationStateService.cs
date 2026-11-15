using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The activation and queued-attack state <c>UsePerkFeat</c> runs on (part 20 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md). Static callers reach the container's instance through
    /// <c>GameServices.AbilityActivationState</c>; the reload hook is on <see cref="AbilityActivationStateService"/>.
    /// </summary>
    public interface IAbilityActivationStateService
    {
        /// <summary>The creature's current cast or channel, if one is active (the state <c>UsePerkFeat</c> runs on).</summary>
        bool TryGetActiveAbilityActivation(uint activator, out AbilityActiveActivation activation);

        /// <summary>Records the creature's current cast or channel, replacing any earlier one.</summary>
        void SetActiveAbilityActivation(uint activator, AbilityActiveActivation activation);

        /// <summary>Forgets the creature's current cast or channel.</summary>
        void RemoveActiveAbilityActivation(uint activator);

        /// <summary>The trait snapshot captured when the creature queued its weapon ability, if any.</summary>
        bool TryGetQueuedWeaponTraits(uint activator, out WeaponAbilityTraitActivation traits);

        /// <summary>Records the trait snapshot of a queued weapon ability, replacing any earlier one.</summary>
        void SetQueuedWeaponTraits(uint activator, WeaponAbilityTraitActivation traits);

        /// <summary>Forgets the trait snapshot of the creature's queued weapon ability.</summary>
        void RemoveQueuedWeaponTraits(uint activator);

        /// <summary>The attack roll reserved for the creature's queued weapon ability, if any.</summary>
        bool TryGetQueuedWeaponAttack(uint activator, out AbilityQueuedWeaponAttack reservation);

        /// <summary>Reserves an attack roll for a queued weapon ability, unless one is already reserved.</summary>
        void TryAddQueuedWeaponAttack(uint activator, AbilityQueuedWeaponAttack reservation);

        /// <summary>Forgets the attack roll reserved for the creature's queued weapon ability.</summary>
        void RemoveQueuedWeaponAttack(uint activator);
    }
}
