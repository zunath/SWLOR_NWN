
using SWLOR.Game.Server.Service.AIService;

namespace SWLOR.Game.Server.Service;

public interface IAI
{
    void CacheAIData();

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

    /// <summary>
    /// Handles custom perk usage
    /// </summary>
    void ProcessPerkAI(AIDefinitionType aiType, uint creature, bool usesEnmity);

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
}