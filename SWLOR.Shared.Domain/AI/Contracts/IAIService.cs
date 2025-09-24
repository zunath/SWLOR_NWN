
using SWLOR.Shared.Domain.AI.Enums;

namespace SWLOR.Shared.Domain.AI.Contracts
{
    public interface IAIService
    {
        void CacheAIData();

        /// <summary>
        /// Processes creature heartbeat logic.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureHeartbeat(uint creature);

        /// <summary>
        /// Processes creature perception logic.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreaturePerception(uint creature);

        /// <summary>
        /// Processes creature combat round end logic.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureCombatRoundEnd(uint creature);

        /// <summary>
        /// Processes creature conversation logic.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureConversation(uint creature);

        /// <summary>
        /// Processes creature physical attacked logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreaturePhysicalAttacked(uint creature);

        /// <summary>
        /// Processes creature damaged logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureDamaged(uint creature);

        /// <summary>
        /// Processes creature death logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureDeath(uint creature);

        /// <summary>
        /// Processes creature disturbed logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureDisturbed(uint creature);

        /// <summary>
        /// Processes creature spawn logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureSpawn(uint creature);

        /// <summary>
        /// Processes creature rested logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureRested(uint creature);

        /// <summary>
        /// Processes creature spell cast at logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureSpellCastAt(uint creature);

        /// <summary>
        /// Processes creature user defined logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureUserDefined(uint creature);

        /// <summary>
        /// Processes creature blocked logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureBlocked(uint creature);

        /// <summary>
        /// Processes when a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// Invisible creatures do not trigger this.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureAggroEnter(uint creature);

        /// <summary>
        /// Processes when a creature exits the aggro aura of another creature
        /// </summary>
        /// <param name="creature">The creature to process</param>
        void ProcessCreatureAggroExit(uint creature);

        /// <summary>
        /// Handles custom perk usage
        /// </summary>
        /// <param name="aiType">The AI definition type to use</param>
        /// <param name="creature">The creature to process</param>
        /// <param name="usesEnmity">Whether to use enmity targeting</param>
        void ProcessPerkAI(AIDefinitionType aiType, uint creature, bool usesEnmity);

        /// <summary>
        /// When the creature dies or is destroyed, remove it from all caches.
        /// </summary>
        /// <param name="creature">The creature to remove from caches</param>
        void RemoveFromAlliesCache(uint creature);

        /// <summary>
        /// Sets a set of AI flags onto a particular creature as a local variable.
        /// </summary>
        /// <param name="creature">The creature to set the flags onto.</param>
        /// <param name="flags">The flags to set.</param>
        void SetAIFlag(uint creature, AIFlag flags);

        /// <summary>
        /// Retrieves a set of AI flags from a particular creature. If <see cref="SetAIFlag"/> has not been called, this will return no flags.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>A set of AIFlags specified on a creature.</returns>
        AIFlag GetAIFlag(uint creature);
    }
}