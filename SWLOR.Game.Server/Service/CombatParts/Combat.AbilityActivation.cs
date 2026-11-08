using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    // The ability activation part lives in Domain/Combat/CombatAbilityActivationService.cs. These forwarders keep every member
    // on Combat for its callers.
    public static partial class Combat
    {
        private static ICombatAbilityActivationService AbilityActivation => FacadeAccessor.Resolve<ICombatAbilityActivationService>();

        public static void ApplyAbilityActivatedEffects(
            uint activator,
            uint target,
            FeatType feat,
            AbilityDetail ability,
            AbilityImpactSummary summary) =>
            AbilityActivation.ApplyAbilityActivatedEffects(activator, target, feat, ability, summary);

        public static void TrackQueuedWeaponAbilityUse(uint activator, AbilityDetail ability) =>
            AbilityActivation.TrackQueuedWeaponAbilityUse(activator, ability);

        /// <inheritdoc cref="ICombatAbilityActivationService.ApplyHitPointSpendAbilityEffects"/>
        public static void ApplyHitPointSpendAbilityEffects(uint activator, int hitPointsSpent, int hitPointCost = 0) =>
            AbilityActivation.ApplyHitPointSpendAbilityEffects(activator, hitPointsSpent, hitPointCost);

        private static void ForgetActivationState(uint creature) => AbilityActivation.ForgetActivationState(creature);
    }
}
