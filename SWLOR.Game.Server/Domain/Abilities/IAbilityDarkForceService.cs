namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The Dark Force conversion and restoration of an ability impact (part 14 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md): the extra damage against a target at low HP, the HP an activator restores
    /// from the damage it deals, and the FP it gains for an HP cost once per impact. Each applies only while the impact's
    /// ability triggers Dark Force conversion. Static callers reach the container's instance through
    /// <c>GameServices.AbilityDarkForce</c>.
    /// </summary>
    public interface IAbilityDarkForceService
    {
        /// <summary>
        /// The damage percent adjustment the activator's Dark Force stats give against a target at or below their HP threshold,
        /// or 0.
        /// </summary>
        int GetDarkForceTargetLowHPDamageAdjustment(uint activator, uint target);

        /// <summary>The HP restoration and the cast conversion for damage an impact dealt to a target.</summary>
        void ApplyDarkForceConversion(uint activator, uint target, int damage);

        /// <summary>Restores the activator's HP by its Dark Force restore percent of the damage dealt.</summary>
        void ApplyDarkForceDamageRestoration(uint activator, int damage);

        /// <summary>The FP restore and HP cost of the impact's cast, once per impact.</summary>
        void ApplyDarkForceCastConversion(uint activator, uint target);
    }
}
