using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// Creature AI: the profile tables, the per-creature state and flags, the ally cache and the combat leash. Static callers reach the container's
    /// instance through <c>GameServices.AI</c>; the creature event handlers are on <see cref="IAICreatureEventService"/>,
    /// the aggro aura on <see cref="IAIAggroService"/> and the decision engine on <see cref="IAIDecisionService"/> and
    /// <see cref="IAIActionService"/>.
    /// </summary>
    public interface IAIService : IAIProfileDescription, ICombatLeash
    {
        /// <summary>
        /// When the module cache is built, the AI profiles of every profile definition are loaded and validated.
        /// </summary>
        void CacheAIData();

        /// <summary>
        /// Entry point for creature perception logic.
        /// </summary>
        void CreaturePerception();

        /// <summary>
        /// The profiles of every profile definition, by type.
        /// </summary>
        IReadOnlyDictionary<AIProfileType, AIProfile> Profiles { get; }

        /// <summary>
        /// The profile a creature uses: the one set on it, or the companion or generic profile its kind implies.
        /// </summary>
        AIProfileType GetProfileType(uint creature);

        /// <summary>
        /// The per-creature state of the engine for a profile; made when the creature has none, and reset when the profile changed.
        /// </summary>
        AIState GetState(uint creature, AIProfileType profileType);

        /// <summary>
        /// The state of a creature the engine has already made one for; false when it has none.
        /// </summary>
        bool TryGetState(uint creature, out AIState state);

        /// <summary>
        /// The creatures a creature may count as its allies: its master's party, or the creatures it has seen of its own faction.
        /// </summary>
        IReadOnlyList<uint> BuildAllies(uint creature);

        /// <summary>
        /// The target selector registered for an ability that its action does not name one for.
        /// </summary>
        bool TryGetDefaultTargetSelector(FeatType feat, out AITargetSelector selector);

        void SetAIProfile(uint creature, AIProfileType profile);

        AIProfileType GetAIProfile(uint creature);

        /// <summary>
        /// When the creature dies or is destroyed, remove it from all caches.
        /// </summary>
        void RemoveFromAlliesCache();

        /// <summary>
        /// The creatures a creature has seen of its own faction.
        /// </summary>
        bool TryGetAllies(uint creature, out HashSet<uint> allies);

        /// <summary>
        /// When a creature's heartbeat fires, if they have the RandomWalk or ReturnHome AI flag,
        /// and they are not currently preoccupied (combat, talking, etc.) force them to randomly walk or return home if they are too far away.
        /// </summary>
        void ProcessFlags();

        /// <summary>
        /// Starts the combat leash evade when the target is beyond the leash.
        /// </summary>
        /// <returns>true if the creature is evading or began to, false if the leash does not apply.</returns>
        bool TryStartLeashEvade(uint creature, uint target);

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

        /// <summary>
        /// Forgets the AI state (phase, cooldowns, cached actions) of a creature.
        /// </summary>
        void ClearAIState(uint creature);
    }
}
