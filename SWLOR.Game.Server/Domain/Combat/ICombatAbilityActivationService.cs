using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability activation part of combat: the riders that run when a creature activates an ability, the queued weapon
    /// ability use, and the riders of an HP-cost ability.
    /// </summary>
    public interface ICombatAbilityActivationService
    {
        void ApplyAbilityActivatedEffects(
            uint activator,
            uint target,
            FeatType feat,
            AbilityDetail ability,
            AbilityImpactSummary summary);

        void TrackQueuedWeaponAbilityUse(uint activator, AbilityDetail ability);

        /// <param name="hitPointsSpent">HP actually paid. Nothing triggers when no HP was paid.</param>
        /// <param name="hitPointCost">Full HP cost before the payment was clipped at 1 HP; 0 when it was paid in full.</param>
        void ApplyHitPointSpendAbilityEffects(uint activator, int hitPointsSpent, int hitPointCost = 0);

        void ForgetActivationState(uint creature);
    }
}
