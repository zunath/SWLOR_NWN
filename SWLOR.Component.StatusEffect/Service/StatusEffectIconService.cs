using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Component.StatusEffect.Contracts;

namespace SWLOR.Component.StatusEffect.Service
{
    /// <summary>
    /// Service responsible for managing status effect icons and mappings.
    /// </summary>
    public class StatusEffectIconService : IStatusEffectIconService
    {
        private readonly IStatusEffectDataService _dataService;

        public StatusEffectIconService(IStatusEffectDataService dataService)
        {
            _dataService = dataService;
        }

        /// <summary>
        /// Gets the effect script type from an effect icon type.
        /// </summary>
        /// <param name="effectIcon">The effect icon type.</param>
        /// <returns>The corresponding effect script type.</returns>
        public EffectScriptType GetEffectTypeFromIcon(EffectIconType effectIcon)
        {
            if (!_dataService.EffectIconToEffectType.TryGetValue(effectIcon, out EffectScriptType effectType))
                return EffectScriptType.Invalideffect;

            return effectType;
        }

        /// <summary>
        /// Gets the status effect types associated with an effect icon.
        /// </summary>
        /// <param name="effectIcon">The effect icon type.</param>
        /// <returns>List of status effect types associated with the icon.</returns>
        public List<StatusEffectType> GetStatusEffectTypesFromIcon(EffectIconType effectIcon)
        {
            if (!_dataService.EffectIconToStatusEffects.TryGetValue(effectIcon, out List<StatusEffectType> statusTypes))
                return new List<StatusEffectType>();

            return statusTypes;
        }

        /// <summary>
        /// Gets the ability type that is buffed by an effect icon.
        /// </summary>
        /// <param name="effectIcon">The effect icon type.</param>
        /// <returns>The ability type that is buffed, or Invalid if none.</returns>
        public AbilityType GetAbilityTypeBuffed(EffectIconType effectIcon)
        {
            if (!_dataService.AbilityIncreaseIconType.TryGetValue(effectIcon, out AbilityType abilityType))
                return AbilityType.Invalid;

            return abilityType;
        }
    }
}
