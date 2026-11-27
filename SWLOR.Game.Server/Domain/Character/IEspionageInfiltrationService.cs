namespace SWLOR.Game.Server.Domain.Character
{
    public interface IEspionageInfiltrationService
    {
        /// <summary>
        /// Starts an attempt when a stealthed player enters an eligible hostile's real aggro range.
        /// The same method is also safe to call from Spot detection, which covers gaining line of
        /// sight after entering the aura behind an obstruction.
        /// </summary>
        bool TryBegin(uint player, uint npc);

        /// <summary>
        /// Records the engine's resolved Spot verdict. Detection pays the small failure award
        /// immediately; an undetected verdict qualifies the attempt for a later successful exit.
        /// </summary>
        void RecordDetection(uint observer, uint target, bool detected);

        /// <summary>
        /// Completes an attempt when NWN reports that the player left the hostile's aggro aura.
        /// </summary>
        void Complete(uint player, uint npc);

        /// <summary>
        /// Samples movement for active attempts. The dedicated one-second sampler catches quick
        /// traversals, while stealth ticks, Spot events, and aura exit provide additional samples.
        /// </summary>
        void UpdateMovement(uint player);

        bool MeetsSuccessRequirements(
            bool evadedDetection,
            float maximumTravelDistance,
            bool isStealthed,
            bool hasCombatEnmity);

        int CalculateXp(int npcLevel, int espionageRank, bool wasDetected);

        bool ShouldRejectDetectionOutcome(
            bool detected,
            bool playerInitiatedCombat,
            bool hasPairCombatEnmity,
            bool hasUnrelatedCombatEnmity);

        /// <summary>
        /// Marks every active attempt for a player before their attack can create pair enmity.
        /// The later Spot callback can then distinguish that hostile action from detection aggro.
        /// </summary>
        void RecordPlayerCombatInitiation(uint player);

        void CancelPlayer(uint player);

        void ClearNpc(uint npc);

        void OnPlayerExit();

        void OnPlayerDeath();

        void OnNpcRemoved();

        /// <summary>
        /// Stealth granted by Shared Presence and enemies blinded by Cloud Mind award no bypass XP.
        /// </summary>
        bool IsBypassXpEligible(bool playerHasGrantedStealth, bool npcIsClouded);
    }
}
