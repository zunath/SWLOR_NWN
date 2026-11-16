using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The combat log lines of an ability impact (part 15 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md):
    /// the result of an impact's hit roll against a target, and the notice that an impact found no target. Both go to the
    /// players near the creature they name.
    /// </summary>
    public interface IAbilityImpactMessageService
    {
        /// <summary>
        /// Records the hit roll for the combat recorder and tells the players near the target how the ability's roll against it
        /// went.
        /// </summary>
        void SendCombatImpactResultMessage(
            uint activator,
            uint target,
            AbilityDetail ability,
            int attackResultType,
            int hitRate);

        /// <summary>Tells the players near the activator that the ability found no target.</summary>
        void SendCombatImpactNoTargetsMessage(
            uint activator,
            AbilityDetail ability);
    }
}
