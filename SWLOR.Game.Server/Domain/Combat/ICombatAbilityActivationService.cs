using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability activation part of combat: the order of the riders that run when a creature activates an ability, and the
    /// queued weapon ability use. The riders are on the cost, defense, offense and weapon activation parts.
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
    }
}
