using System;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Model;

namespace SWLOR.NWN.API.Service
{
    public static class ItemPropertyPlugin
    {
        private static IItemPropertyPluginService _service = new ItemPropertyPluginService();

        internal static void SetService(IItemPropertyPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IItemPropertyPluginService.UnpackIP"/>
        public static ItemPropertyUnpacked UnpackIP(ItemProperty ip) => _service.UnpackIP(ip);

        /// <inheritdoc cref="IItemPropertyPluginService.PackIP"/>
        public static ItemProperty PackIP(ItemPropertyUnpacked itemProperty) => _service.PackIP(itemProperty);

        /// <inheritdoc cref="IItemPropertyPluginService.GetActiveProperty"/>
        public static ItemPropertyUnpacked GetActiveProperty(uint oItem, int nIndex) => _service.GetActiveProperty(oItem, nIndex);
    }
}
