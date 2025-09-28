using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.StatusEffect.Contracts
{
    /// <summary>
    /// Service responsible for managing status effect icons and mappings.
    /// </summary>
    public interface IStatusEffectIconService
    {
        /// <summary>
        /// Gets the effect script type from an effect icon type.
        /// </summary>
        /// <param name="effectIcon">The effect icon type.</param>
        /// <returns>The corresponding effect script type.</returns>
        EffectScriptType GetEffectTypeFromIcon(EffectIconType effectIcon);

        /// <summary>
        /// Gets the status effect types associated with an effect icon.
        /// </summary>
        /// <param name="effectIcon">The effect icon type.</param>
        /// <returns>List of status effect types associated with the icon.</returns>
        List<StatusEffectType> GetStatusEffectTypesFromIcon(EffectIconType effectIcon);

        /// <summary>
        /// Gets the ability type that is buffed by an effect icon.
        /// </summary>
        /// <param name="effectIcon">The effect icon type.</param>
        /// <returns>The ability type that is buffed, or Invalid if none.</returns>
        AbilityType GetAbilityTypeBuffed(EffectIconType effectIcon);
    }
}
