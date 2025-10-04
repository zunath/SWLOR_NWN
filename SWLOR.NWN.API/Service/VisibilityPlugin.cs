using System;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.Service
{
    public static class VisibilityPlugin
    {
        private static IVisibilityPluginService _service = new VisibilityPluginService();

        internal static void SetService(IVisibilityPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IVisibilityPluginService.GetVisibilityOverride"/>
        public static VisibilityType GetVisibilityOverride(uint player, uint target) => _service.GetVisibilityOverride(player, target);

        /// <inheritdoc cref="IVisibilityPluginService.SetVisibilityOverride"/>
        public static void SetVisibilityOverride(uint player, uint target, VisibilityType @override) => _service.SetVisibilityOverride(player, target, @override);
    }
}
