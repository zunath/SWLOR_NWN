using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;

namespace SWLOR.Shared.Domain.Combat.Contracts
{
    public interface IStatusEffectService
    {
        /// <summary>
        /// When the module loads, cache all status effects.
        /// </summary>
        void CacheStatusEffects();

        /// <summary>
        /// Gives a status effect to a creature.
        /// If creature already has the status effect, and their timer is shorter than length,
        /// it will be extended to the length specified.
        /// </summary>
        /// <param name="source">The source of the status effect.</param>
        /// <param name="target">The creature receiving the status effect.</param>
        /// <param name="statusEffectType">The type of status effect to give.</param>
        /// <param name="length">The amount of time, in seconds, the status effect should last. Set to 0.0f to make it permanent.</param>
        /// <param name="effectData">Effect data used by the effect.</param>
        /// <param name="concentrationFeatType">If status effect is associated with a concentration ability, this will track the feat type used.</param>
        /// <param name="sendApplicationMessage">If true, a message will be sent to nearby players when the status effect is applied.</param>
        void Apply(
            uint source,
            uint target,
            StatusEffectType statusEffectType,
            float length,
            object effectData = null,
            FeatType concentrationFeatType = FeatType.Invalid,
            bool sendApplicationMessage = true);

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

        /// <summary>
        /// Checks if a creature has a status effect.
        /// If no status effect types are specified, false will be returned.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="statusEffectTypes">The status effect types to look for.</param>
        /// <returns>true if creature has status effect, false otherwise</returns>
        bool HasStatusEffect(uint creature, params StatusEffectType[] statusEffectTypes);

        /// <summary>
        /// Retrieves a status effect detail by its type.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <returns>A status effect detail</returns>
        StatusEffectDetail GetDetail(StatusEffectType type);

        /// <summary>
        /// Retrieves the effect data associated with a creature's effect.
        /// If creature does not have effect, the default value of T will be returned.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve from the effect.</typeparam>
        /// <param name="creature">The creature to check.</param>
        /// <param name="effectType">The type of effect.</param>
        /// <returns>An effect data object or a default object of type T</returns>
        T GetEffectData<T>(uint creature, StatusEffectType effectType);

        /// <summary>
        /// Retrieves the effect duration associated with a creature's effect.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="effectTypes">The type(s) of effect.</param>
        /// <returns>A float time remaining of the status effect</returns>
        int GetEffectDuration(uint creature, params StatusEffectType[] effectTypes);

        EffectScriptType GetEffectTypeFromIcon(EffectIconType effectIcon);
        List<StatusEffectType> GetStatusEffectTypesFromIcon(EffectIconType effectIcon);
        AbilityType GetAbilityTypeBuffed(EffectIconType effectIcon);
    }
}
