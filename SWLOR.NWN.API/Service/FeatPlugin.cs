using System;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Service
{
    public static class FeatPlugin
    {
        private static IFeatPluginService _service = new FeatPluginService();

        internal static void SetService(IFeatPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IFeatPluginService.SetFeatModifier"/>
        public static void SetFeatModifier(
            FeatType featType, 
            FeatModifierType modifierType, 
            uint param1 = 0xDEADBEEF, 
            uint param2 = 0xDEADBEEF,
            uint param3 = 0xDEADBEEF, 
            uint param4 = 0xDEADBEEF) => 
            _service.SetFeatModifier(featType, modifierType, param1, param2, param3, param4);
    }
}
