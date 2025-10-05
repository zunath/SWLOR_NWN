using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides feat modification functionality for customizing feat behavior and properties.
    /// This plugin allows for detailed manipulation of feat modifiers and parameters to create
    /// custom feat implementations and modify existing feat behavior.
    /// </summary>
    public class FeatPluginService : IFeatPluginService
    {
        /// <inheritdoc/>
        public void SetFeatModifier(
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
