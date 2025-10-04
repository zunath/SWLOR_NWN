using SWLOR.NWN.API.NWNX;

namespace SWLOR.NWN.API.Service
{
    public static class WeaponPlugin
    {
        private static IWeaponPluginService _service = new WeaponPluginService();

        internal static void SetService(IWeaponPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

    }
}
