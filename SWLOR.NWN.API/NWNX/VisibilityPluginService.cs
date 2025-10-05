using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    public class VisibilityPluginService : IVisibilityPluginService
    {
        /// <inheritdoc/>
        public VisibilityType GetVisibilityOverride(uint player, uint target)
        {
            int result = global::NWN.Core.NWNX.VisibilityPlugin.GetVisibilityOverride(player, target);
            return (VisibilityType)result;
        }

        /// <inheritdoc/>
        public void SetVisibilityOverride(uint player, uint target, VisibilityType @override)
        {
            global::NWN.Core.NWNX.VisibilityPlugin.SetVisibilityOverride(player, target, (int)@override);
        }
    }
}