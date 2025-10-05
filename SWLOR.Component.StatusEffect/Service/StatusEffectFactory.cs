using System.Reflection;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.StatusEffect.Service
{
    /// <summary>
    /// Factory for creating status effect instances based on StatusEffectType enum.
    /// Maps enum values to concrete status effect implementations using reflection.
    /// </summary>
    public class StatusEffectFactory : IStatusEffectFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<StatusEffectType, Type> _statusEffectTypes = new();

        public StatusEffectFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            DiscoverAndRegisterStatusEffects();
        }

        /// <summary>
        /// Automatically discovers and registers all status effect implementations using reflection.
        /// Uses the IStatusEffect.Type property to determine the mapping.
        /// </summary>
        private void DiscoverAndRegisterStatusEffects()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var statusEffectTypes = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && typeof(IStatusEffect).IsAssignableFrom(type));

            foreach (var statusEffectType in statusEffectTypes)
            {
                // Create a temporary instance to get the Type property
                var tempInstance = (IStatusEffect)_serviceProvider.GetService(statusEffectType);
                if (tempInstance != null)
                {
                    _statusEffectTypes[tempInstance.Type] = statusEffectType;
                }
            }
        }

        /// <summary>
        /// Creates a new instance of the specified status effect type.
        /// </summary>
        /// <param name="type">The status effect type to create</param>
        /// <returns>A new instance of the status effect</returns>
        /// <exception cref="ArgumentException">Thrown when the status effect type is not registered</exception>
        public IStatusEffect CreateStatusEffect(StatusEffectType type)
        {
            if (!_statusEffectTypes.TryGetValue(type, out var statusEffectType))
                throw new ArgumentException($"Status effect type '{type}' is not registered");

            return (IStatusEffect)_serviceProvider.GetService(statusEffectType);
        }

        /// <summary>
        /// Gets the Type of the specified status effect.
        /// </summary>
        /// <param name="type">The status effect type</param>
        /// <returns>The Type of the status effect implementation</returns>
        /// <exception cref="ArgumentException">Thrown when the status effect type is not registered</exception>
        public Type GetStatusEffectType(StatusEffectType type)
        {
            if (!_statusEffectTypes.TryGetValue(type, out var statusEffectType))
                throw new ArgumentException($"Status effect type '{type}' is not registered");

            return statusEffectType;
        }

        /// <summary>
        /// Checks if a status effect type is registered.
        /// </summary>
        /// <param name="type">The status effect type to check</param>
        /// <returns>True if the type is registered, false otherwise</returns>
        public bool IsRegistered(StatusEffectType type)
        {
            return _statusEffectTypes.ContainsKey(type);
        }

        /// <summary>
        /// Gets all registered status effect types.
        /// </summary>
        /// <returns>Collection of all registered status effect types</returns>
        public IEnumerable<StatusEffectType> GetRegisteredTypes()
        {
            return _statusEffectTypes.Keys;
        }
    }
}
