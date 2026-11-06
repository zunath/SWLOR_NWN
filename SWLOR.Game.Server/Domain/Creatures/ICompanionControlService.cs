using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CompanionControlService;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// Companion control for a player's droid or beast: mode, engagement and hostile ability authorization.
    /// </summary>
    public interface ICompanionControlService
    {
        bool IsRegisteredCompanion(uint creature);

        bool IsControlledCompanion(uint creature);

        void Initialize(uint companion, bool preserveActionQueue = false);

        void Clear(uint companion);

        CompanionMode GetMode(uint companion);

        bool AreAbilitiesEnabled(uint companion);

        bool HandleConversation(uint companion);

        void RegisterDefensiveThreat(uint companion, uint threat);

        void RegisterOwnerAttack(uint companion, uint target);

        void BeginExplicitOrder(uint companion, float durationSeconds = 30f);

        void CancelExplicitOrder(uint companion);

        void ProcessCombatRound(uint companion, bool bypassDecisionThrottle = false);

        void ProcessHeartbeat(uint companion);

        void ResumeModePosition(uint companion);

        bool TryProcessPendingDefensiveReaction(uint companion);

        uint PeekAuthorizedTarget(uint companion);

        uint ResolveHostileAbilityTarget(uint companion, AbilityDetail ability, uint authorizedTarget, uint selectedTarget);

        bool IsHostileAbilityTargetAuthorized(uint companion, AbilityDetail ability, uint selectedTarget);

        bool CanIssueAttackCommand(uint companion, uint target);

        void ResetOwnerCompanionToFollow(uint master);

        void RenameToggleAbilitiesCommand();

        void OnOwnerAttacked();

        void OnOwnerDamaged();

        void OnHostileSpellCastAtOwner();

        void OnOwnerCombatRound();

        void OnOwnerAreaExit();
    }
}
