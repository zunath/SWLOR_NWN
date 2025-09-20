using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides feat modification functionality for customizing feat behavior and properties.
    /// This plugin allows for detailed manipulation of feat modifiers and parameters to create
    /// custom feat implementations and modify existing feat behavior.
    /// </summary>
    public static class FeatPlugin
    {
        /// <summary>
        /// Sets a feat modifier with custom parameters to modify feat behavior.
        /// </summary>
        /// <param name="featType">The feat type to modify. Must be a valid FeatType enum value or feat.2da index.</param>
        /// <param name="modifierType">The type of modifier to apply. See FeatModifierType enum for available modifiers.</param>
        /// <param name="param1">The first parameter for this feat modifier. Defaults to 0xDEADBEEF if not specified.</param>
        /// <param name="param2">The second parameter for this feat modifier. Defaults to 0xDEADBEEF if not specified.</param>
        /// <param name="param3">The third parameter for this feat modifier. Defaults to 0xDEADBEEF if not specified.</param>
        /// <param name="param4">The fourth parameter for this feat modifier. Defaults to 0xDEADBEEF if not specified.</param>
        /// <remarks>
        /// This function allows you to customize how feats behave by setting various modifiers and parameters.
        /// The specific meaning of each parameter depends on the modifier type being applied.
        /// Use 0xDEADBEEF for unused parameters to maintain default behavior.
        /// Changes take effect immediately and persist until modified again.
        /// </remarks>
        public static void SetFeatModifier(
            FeatType featType, 
            FeatModifierType modifierType, 
            uint param1 = 0xDEADBEEF, 
            uint param2 = 0xDEADBEEF,
            uint param3 = 0xDEADBEEF, 
            uint param4 = 0xDEADBEEF)
        {
            global::NWN.Core.NWNX.FeatPlugin.SetFeatModifier(
                (int)featType, 
                (int)modifierType, 
                (int)param1, 
                (int)param2, 
                (int)param3, 
                (int)param4);
        }
    }
}
