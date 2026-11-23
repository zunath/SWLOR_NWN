using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The target a companion may engage and whether a hostile ability or an attack command may go to a target. The enmity
    /// tables read it through <see cref="ICompanionTargetQuery"/>.
    /// </summary>
    public interface ICompanionTargetAuthorityService : ICompanionTargetQuery
    {
        /// <summary>The target the companion engages this round, tracking its progress; it may complete or drop an engagement.</summary>
        uint AdvanceAuthorizedTarget(uint companion);

        uint ResolveHostileAbilityTarget(uint companion, AbilityDetail ability, uint authorizedTarget, uint selectedTarget);

        bool IsHostileAbilityTargetAuthorized(uint companion, AbilityDetail ability, uint selectedTarget);

        void StartTracking(CompanionControlState state, uint companion, uint target);

        bool IsValidHostileTarget(uint companion, uint target);
    }
}
