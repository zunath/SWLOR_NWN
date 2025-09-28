using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.StatusEffect.Contracts
{
    /// <summary>
    /// Service responsible for managing status effect lifecycle and state.
    /// </summary>
    public interface IStatusEffectManagementService
    {
        /// <summary>
        /// When the module loads, cache all status effects.
        /// </summary>
        void CacheStatusEffects();

        /// <summary>
        /// When a player enters the server, if any of their status effects in limbo, re-add them to the
        /// dictionary for processing.
        /// </summary>
        void PlayerEnter();

        /// <summary>
        /// When a player leaves the server, move their status effects to a different dictionary
        /// so they aren't processed unnecessarily.  
        /// </summary>
        void PlayerExit();

        /// <summary>
        /// When the module heartbeat runs, execute and clean up status effects on all creatures.
        /// </summary>
        void TickStatusEffects();

        /// <summary>
        /// When a player dies, remove any status effects which are present.
        /// </summary>
        void OnPlayerDeath();

        /// <summary>
        /// Removes a status effect from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove the status effect from.</param>
        /// <param name="statusEffectType">The type of status effect to remove.</param>
        /// <param name="showMessage">If true, a message will be displayed. Otherwise no message is displayed.</param>
        void Remove(uint creature, StatusEffectType statusEffectType, bool showMessage = true);

        /// <summary>
        /// Removes all status effects from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove all effects from.</param>
        void RemoveAll(uint creature);
    }
}
