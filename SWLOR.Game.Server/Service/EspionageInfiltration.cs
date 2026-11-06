using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Tracks one infiltration attempt per player/hostile pair. The logic and the attempt state live in
    /// <see cref="IEspionageInfiltrationService"/>; the event handlers stay here and every member forwards to the
    /// container's instance.
    /// </summary>
    public static class EspionageInfiltration
    {
        public const float RequiredTravelDistanceMeters = Domain.Character.EspionageInfiltrationService.RequiredTravelDistanceMeters;
        public const float DetectionFailureXpPercent = Domain.Character.EspionageInfiltrationService.DetectionFailureXpPercent;
        public const int HostileFactionId = Domain.Character.EspionageInfiltrationService.HostileFactionId;

        private static IEspionageInfiltrationService Instance => FacadeAccessor.Resolve<IEspionageInfiltrationService>();

        /// <summary>
        /// Starts an attempt when a stealthed player enters an eligible hostile's real aggro range.
        /// The same method is also safe to call from Spot detection, which covers gaining line of
        /// sight after entering the aura behind an obstruction.
        /// </summary>
        public static bool TryBegin(uint player, uint npc)
        {
            return Instance.TryBegin(player, npc);
        }

        /// <summary>
        /// Records the engine's resolved Spot verdict. Detection pays the small failure award
        /// immediately; an undetected verdict qualifies the attempt for a later successful exit.
        /// </summary>
        public static void RecordDetection(uint observer, uint target, bool detected)
        {
            Instance.RecordDetection(observer, target, detected);
        }

        /// <summary>
        /// Completes an attempt when NWN reports that the player left the hostile's aggro aura.
        /// </summary>
        public static void Complete(uint player, uint npc)
        {
            Instance.Complete(player, npc);
        }

        /// <summary>
        /// Samples movement for active attempts. The dedicated one-second sampler catches quick
        /// traversals, while stealth ticks, Spot events, and aura exit provide additional samples.
        /// </summary>
        public static void UpdateMovement(uint player)
        {
            Instance.UpdateMovement(player);
        }

        public static bool MeetsSuccessRequirements(
            bool evadedDetection,
            float maximumTravelDistance,
            bool isStealthed,
            bool hasCombatEnmity)
        {
            return Instance.MeetsSuccessRequirements(evadedDetection, maximumTravelDistance, isStealthed, hasCombatEnmity);
        }

        public static int CalculateXp(int npcLevel, int espionageRank, bool wasDetected)
        {
            return Instance.CalculateXp(npcLevel, espionageRank, wasDetected);
        }

        public static bool ShouldRejectDetectionOutcome(
            bool detected,
            bool playerInitiatedCombat,
            bool hasPairCombatEnmity,
            bool hasUnrelatedCombatEnmity)
        {
            return Instance.ShouldRejectDetectionOutcome(
                detected, playerInitiatedCombat, hasPairCombatEnmity, hasUnrelatedCombatEnmity);
        }

        /// <summary>
        /// Marks every active attempt for a player before their attack can create pair enmity.
        /// The later Spot callback can then distinguish that hostile action from detection aggro.
        /// </summary>
        public static void RecordPlayerCombatInitiation(uint player)
        {
            Instance.RecordPlayerCombatInitiation(player);
        }

        public static void CancelPlayer(uint player)
        {
            Instance.CancelPlayer(player);
        }

        public static void ClearNpc(uint npc)
        {
            Instance.ClearNpc(npc);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void OnPlayerExit()
        {
            Instance.OnPlayerExit();
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnPlayerDeath()
        {
            Instance.OnPlayerDeath();
        }

        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        [NWNEventHandler(ScriptName.OnObjectDestroyed)]
        public static void OnNpcRemoved()
        {
            Instance.OnNpcRemoved();
        }

        /// <summary>
        /// Stealth granted by Shared Presence and enemies blinded by Cloud Mind award no bypass XP.
        /// </summary>
        public static bool IsBypassXpEligible(bool playerHasGrantedStealth, bool npcIsClouded)
        {
            return Instance.IsBypassXpEligible(playerHasGrantedStealth, npcIsClouded);
        }
    }
}
