using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.StatusEffect.Contracts
{
    /// <summary>
    /// Factory interface for creating status effect instances based on StatusEffectType enum.
    /// </summary>
    public interface IStatusEffectFactory
    {
        /// <summary>
        /// Creates a new instance of the specified status effect type.
        /// </summary>
        /// <param name="type">The status effect type to create</param>
        /// <returns>A new instance of the status effect</returns>
        /// <exception cref="ArgumentException">Thrown when the status effect type is not registered</exception>
        IStatusEffect CreateStatusEffect(StatusEffectType type);

        /// <summary>
        /// Gets the Type of the specified status effect.
        /// </summary>
        /// <param name="type">The status effect type</param>
        /// <returns>The Type of the status effect implementation</returns>
        /// <exception cref="ArgumentException">Thrown when the status effect type is not registered</exception>
        Type GetStatusEffectType(StatusEffectType type);

        /// <summary>
        /// Checks if a status effect type is registered.
        /// </summary>
        /// <param name="type">The status effect type to check</param>
        /// <returns>True if the type is registered, false otherwise</returns>
        bool IsRegistered(StatusEffectType type);

        /// <summary>
        /// Gets all registered status effect types.
        /// </summary>
        /// <returns>Collection of all registered status effect types</returns>
        IEnumerable<StatusEffectType> GetRegisteredTypes();
    }
}
