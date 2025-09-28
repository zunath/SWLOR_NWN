using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Component.StatusEffect.Contracts;

namespace SWLOR.Component.StatusEffect.Service
{
    /// <summary>
    /// Service responsible for managing status effect icons and mappings.
    /// </summary>
    public class StatusEffectIconService : IStatusEffectIconService
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded service to break circular dependency
        private readonly Lazy<IStatusEffectDataService> _dataService;

        public StatusEffectIconService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _dataService = new Lazy<IStatusEffectDataService>(() => _serviceProvider.GetRequiredService<IStatusEffectDataService>());
        }
        
        // Lazy-loaded service to break circular dependency
        private IStatusEffectDataService DataService => _dataService.Value;

        /// <summary>
        /// Gets the effect script type from an effect icon type.
        /// </summary>
        /// <param name="effectIcon">The effect icon type.</param>
        /// <returns>The corresponding effect script type.</returns>
        public EffectScriptType GetEffectTypeFromIcon(EffectIconType effectIcon)
        {
            if (!DataService.EffectIconToEffectType.TryGetValue(effectIcon, out EffectScriptType effectType))
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
            if (!DataService.EffectIconToStatusEffects.TryGetValue(effectIcon, out List<StatusEffectType> statusTypes))
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
            if (!DataService.AbilityIncreaseIconType.TryGetValue(effectIcon, out AbilityType abilityType))
                return AbilityType.Invalid;

            return abilityType;
        }
    }
}
