using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;
using SWLOR.Component.StatusEffect.Contracts;

namespace SWLOR.Component.StatusEffect.Service
{
    /// <summary>
    /// Facade service that coordinates the focused status effect services.
    /// Maintains backward compatibility with the original IStatusEffectService interface.
    /// </summary>
    public class StatusEffectService : IStatusEffectService
    {
        private readonly IStatusEffectApplicationService _applicationService;
        private readonly IStatusEffectManagementService _managementService;
        private readonly IStatusEffectQueryService _queryService;
        private readonly IStatusEffectIconService _iconService;

        public StatusEffectService(
            IStatusEffectApplicationService applicationService,
            IStatusEffectManagementService managementService,
            IStatusEffectQueryService queryService,
            IStatusEffectIconService iconService)
        {
            _applicationService = applicationService;
            _managementService = managementService;
            _queryService = queryService;
            _iconService = iconService;
        }

        /// <summary>
        /// When the module loads, cache all status effects.
        /// </summary>
        public void CacheStatusEffects()
        {
            _managementService.CacheStatusEffects();
        }

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
        public void Apply(
            uint source,
            uint target,
            StatusEffectType statusEffectType,
            float length,
            object effectData = null,
            FeatType concentrationFeatType = FeatType.Invalid,
            bool sendApplicationMessage = true)
        {
            _applicationService.Apply(source, target, statusEffectType, length, effectData, concentrationFeatType, sendApplicationMessage);
        }

        /// <summary>
        /// When a player enters the server, if any of their status effects in limbo, re-add them to the
        /// dictionary for processing.
        /// </summary>
        public void PlayerEnter()
        {
            _managementService.PlayerEnter();
        }

        /// <summary>
        /// When a player leaves the server, move their status effects to a different dictionary
        /// so they aren't processed unnecessarily.  
        /// </summary>
        public void PlayerExit()
        {
            _managementService.PlayerExit();
        }

        /// <summary>
        /// When the module heartbeat runs, execute and clean up status effects on all creatures.
        /// </summary>
        public void TickStatusEffects()
        {
            _managementService.TickStatusEffects();
        }

        /// <summary>
        /// When a player dies, remove any status effects which are present.
        /// </summary>
        public void OnPlayerDeath()
        {
            _managementService.OnPlayerDeath();
        }

        /// <summary>
        /// Removes a status effect from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove the status effect from.</param>
        /// <param name="statusEffectType">The type of status effect to remove.</param>
        /// <param name="showMessage">If true, a message will be displayed. Otherwise no message is displayed.</param>
        public void Remove(uint creature, StatusEffectType statusEffectType, bool showMessage = true)
        {
            _managementService.Remove(creature, statusEffectType, showMessage);
        }

        /// <summary>
        /// Removes all status effects from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove all effects from.</param>
        public void RemoveAll(uint creature)
        {
            _managementService.RemoveAll(creature);
        }

        /// <summary>
        /// Checks if a creature has a status effect.
        /// If no status effect types are specified, false will be returned.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="statusEffectTypes">The status effect types to look for.</param>
        /// <returns>true if creature has status effect, false otherwise</returns>
        public bool HasStatusEffect(uint creature, params StatusEffectType[] statusEffectTypes)
        {
            return _queryService.HasStatusEffect(creature, statusEffectTypes);
        }

        /// <summary>
        /// Retrieves a status effect detail by its type.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <returns>A status effect detail</returns>
        public StatusEffectDetail GetDetail(StatusEffectType type)
        {
            return _queryService.GetDetail(type);
        }

        /// <summary>
        /// Retrieves the effect data associated with a creature's effect.
        /// If creature does not have effect, the default value of T will be returned.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve from the effect.</typeparam>
        /// <param name="creature">The creature to check.</param>
        /// <param name="effectType">The type of effect.</param>
        /// <returns>An effect data object or a default object of type T</returns>
        public T GetEffectData<T>(uint creature, StatusEffectType effectType)
        {
            return _queryService.GetEffectData<T>(creature, effectType);
        }

        /// <summary>
        /// Retrieves the effect duration associated with a creature's effect.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="effectTypes">The type(s) of effect.</param>
        /// <returns>A float time remaining of the status effect</returns>
        public int GetEffectDuration(uint creature, params StatusEffectType[] effectTypes)
        {
            return _queryService.GetEffectDuration(creature, effectTypes);
        }

        /// <summary>
        /// Gets the effect script type from an effect icon type.
        /// </summary>
        /// <param name="effectIcon">The effect icon type.</param>
        /// <returns>The corresponding effect script type.</returns>
        public EffectScriptType GetEffectTypeFromIcon(EffectIconType effectIcon)
        {
            return _iconService.GetEffectTypeFromIcon(effectIcon);
        }

        /// <summary>
        /// Gets the status effect types associated with an effect icon.
        /// </summary>
        /// <param name="effectIcon">The effect icon type.</param>
        /// <returns>List of status effect types associated with the icon.</returns>
        public List<StatusEffectType> GetStatusEffectTypesFromIcon(EffectIconType effectIcon)
        {
            return _iconService.GetStatusEffectTypesFromIcon(effectIcon);
        }

        /// <summary>
        /// Gets the ability type that is buffed by an effect icon.
        /// </summary>
        /// <param name="effectIcon">The effect icon type.</param>
        /// <returns>The ability type that is buffed, or Invalid if none.</returns>
        public AbilityType GetAbilityTypeBuffed(EffectIconType effectIcon)
        {
            return _iconService.GetAbilityTypeBuffed(effectIcon);
        }
    }
}
