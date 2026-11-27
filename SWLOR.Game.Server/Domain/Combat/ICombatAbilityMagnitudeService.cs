namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// How Force affinity and combat readiness scale an ability's magnitude (part 4 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md). Static callers reach the container's instance through
    /// <c>GameServices.CombatAbilityMagnitude</c>. The reads run per heal and per impact and allocate nothing.
    /// </summary>
    public interface ICombatAbilityMagnitudeService
    {
        float GetActiveForceAffinityMagnitudeMultiplier(uint activator);

        float GetActiveForceAffinityRestorativeMagnitudeMultiplier(uint activator);

        int ApplyActiveForceAffinityRestorativeMagnitude(uint activator, int amount);

        int ApplyActiveForceAffinityMagnitude(uint activator, int amount);

        float ApplyActiveForceAffinityMagnitude(uint activator, float amount);

        int ApplyCombatReadinessToActivatedAbilityMagnitude(uint activator, int amount);

        int ApplyCombatReadinessMagnitude(uint creature, int amount);
    }
}
