namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// Companion control for a player's droid or beast: the orders, the owner events and the combat round. The registered
    /// companions, the mode position and the authorized target are in the store, positioning and target authority parts.
    /// </summary>
    public interface ICompanionControlService
    {
        void Initialize(uint companion, bool preserveActionQueue = false);

        void Clear(uint companion);

        bool HandleConversation(uint companion);

        void RegisterDefensiveThreat(uint companion, uint threat);

        void RegisterOwnerAttack(uint companion, uint target);

        void BeginExplicitOrder(uint companion, float durationSeconds = 30f);

        void CancelExplicitOrder(uint companion);

        void ProcessCombatRound(uint companion, bool bypassDecisionThrottle = false);

        void ProcessHeartbeat(uint companion);

        void ResumeModePosition(uint companion);

        bool TryProcessPendingDefensiveReaction(uint companion);

        void ResetOwnerCompanionToFollow(uint master);

        void RenameToggleAbilitiesCommand();

        void OnOwnerAttacked();

        void OnOwnerDamaged();

        void OnHostileSpellCastAtOwner();

        void OnOwnerCombatRound();

        void OnOwnerAreaExit();
    }
}
