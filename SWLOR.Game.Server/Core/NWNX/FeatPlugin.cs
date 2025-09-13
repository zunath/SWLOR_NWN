using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class FeatPlugin
    {
        private const string PluginName = "NWNX_Feat";

        /// <summary>
        /// Sets a feat modifier.
        /// </summary>
        /// <param name="featType">The Feat constant or value in feat.2da</param>
        /// <param name="modifierType">The feat modifier to set.</param>
        /// <param name="param1">The first parameter for this feat modifier.</param>
        /// <param name="param2">The second parameter for this feat modifier.</param>
        /// <param name="param3">The third parameter for this feat modifier.</param>
        /// <param name="param4">The fourth parameter for this feat modifier.</param>
        public static void SetFeatModifier(
            FeatType featType, 
            FeatModifierType modifierType, 
            uint param1 = 0xDEADBEEF, 
            uint param2 = 0xDEADBEEF,
            uint param3 = 0xDEADBEEF, 
            uint param4 = 0xDEADBEEF)
        {
            const string Function = "SetFeatModifier";

            NWNXCore.NWNX_PushArgumentInt((int)param4);
            NWNXCore.NWNX_PushArgumentInt((int)param3);
            NWNXCore.NWNX_PushArgumentInt((int)param2);
            NWNXCore.NWNX_PushArgumentInt((int)param1);
            NWNXCore.NWNX_PushArgumentInt((int)modifierType);
            NWNXCore.NWNX_PushArgumentInt((int)featType);

            NWNXCore.NWNX_CallFunction(PluginName, Function);
        }
    }
}
