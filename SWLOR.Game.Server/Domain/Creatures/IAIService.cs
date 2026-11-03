using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// Creature AI: the creature event entry points, the combat leash, the aggro aura, the ally cache and the profile
    /// decision engine (profiles, per-creature state, action selection). The static <c>Service.AI</c> forwards to the
    /// container's instance of this interface.
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// When the module cache is built, the AI profiles of every profile definition are loaded and validated.
        /// </summary>
        void CacheAIData();

        /// <summary>
        /// After a DM toggles the AI of a creature off, its AI state is cleared.
        /// </summary>
        void DMToggleAIAfter();

        /// <summary>
        /// Entry point for creature heartbeat logic.
        /// </summary>
        void CreatureHeartbeat();

        /// <summary>
        /// Entry point for creature perception logic.
        /// </summary>
        void CreaturePerception();

        /// <summary>
        /// Entry point for creature combat round end logic.
        /// </summary>
        void CreatureCombatRoundEnd();

        /// <summary>
        /// Entry point for creature conversation logic.
        /// </summary>
        void CreatureConversation();

        /// <summary>
        /// Entry point for creature physical attacked logic
        /// </summary>
        void CreaturePhysicalAttacked();

        /// <summary>
        /// Entry point for creature damaged logic
        /// </summary>
        void CreatureDamaged();

        /// <summary>
        /// Entry point for creature death logic
        /// </summary>
        void CreatureDeath();

        /// <summary>
        /// Entry point for creature disturbed logic
        /// </summary>
        void CreatureDisturbed();

        /// <summary>
        /// Entry point for creature spawn logic
        /// </summary>
        void CreatureSpawn();

        /// <summary>
        /// Entry point for creature rested logic
        /// </summary>
        void CreatureRested();

        /// <summary>
        /// Entry point for creature spell cast at logic
        /// </summary>
        void CreatureSpellCastAt();

        /// <summary>
        /// Entry point for creature user defined logic
        /// </summary>
        void CreatureUserDefined();

        /// <summary>
        /// Entry point for creature blocked logic
        /// </summary>
        void CreatureBlocked();

        /// <summary>
        /// When a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// Invisible creatures do not trigger this.
        /// </summary>
        void CreatureAggroEnter();

        /// <summary>
        /// When a creature exits the aggro aura of another creature,
        /// </summary>
        void CreatureAggroExit();

        bool ProcessTrigger(
            uint creature,
            AITriggerType trigger,
            uint eventTarget = OBJECT_INVALID,
            bool bypassDecisionThrottle = false);

        void SetAIProfile(uint creature, AIProfileType profile);

        AIProfileType GetAIProfile(uint creature);

        /// <summary>
        /// When the creature dies or is destroyed, remove it from all caches.
        /// </summary>
        void RemoveFromAlliesCache();

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

        bool IsLeashEvading(uint creature);

        bool TryStartCombatLeashEvade(uint creature, uint target);

        bool IsInAggroRange(uint creature, uint target);

        /// <summary>
        /// Re-enters the normal proximity-aggro path when a later native Spot check reveals a
        /// stealthed player who is still inside the observer's aggro aura. Non-AI observers and
        /// targets that no longer satisfy the ordinary aggro guards are ignored.
        /// </summary>
        void TryAcquireAggroAfterDetection(uint observer, uint target);

        /// <summary>
        /// Stops a chase whose proximity threat has expired, using the normal return-home behavior.
        /// </summary>
        void StopCombatAfterProximityLoss(uint enemy);

        bool IsCreatureAIEnabled(uint creature);

        /// <summary>
        /// Forgets the AI state (phase, cooldowns, cached actions) of a creature.
        /// </summary>
        void ClearAIState(uint creature);

        bool TryUseBestTargetedSupportAbility(uint creature, uint target, out string abilityName);
    }
}
