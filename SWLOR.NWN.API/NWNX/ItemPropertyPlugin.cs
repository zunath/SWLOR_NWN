using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.NWNX
{
    public static class ItemPropertyPlugin
    {
        /// <summary>
        /// Convert native itemproperty type to unpacked structure.
        /// </summary>
        /// <param name="ip">The itemproperty to convert.</param>
        /// <returns>A constructed ItemPropertyUnpacked.</returns>
        public static ItemPropertyUnpacked UnpackIP(ItemProperty ip)
        {
            return ItemPropertyPluginRuntime.Current.UnpackIP(ip);
        }

        /// <summary>
        /// Convert unpacked itemproperty structure to native type.
        /// </summary>
        /// <param name="itemProperty">The ItemPropertyUnpacked structure to convert.</param>
        /// <returns>The itemproperty.</returns>
        public static ItemProperty PackIP(ItemPropertyUnpacked itemProperty)
        {
            return ItemPropertyPluginRuntime.Current.PackIP(itemProperty);
        }

        /// <summary>
        /// Gets the active item property at the index.
        /// </summary>
        /// <param name="oItem">The item with the property.</param>
        /// <param name="nIndex">The index such as returned by some Item Events.</param>
        /// <returns>A constructed ItemPropertyUnpacked, except for creator, and spell id.</returns>
        public static ItemPropertyUnpacked GetActiveProperty(uint oItem, int nIndex)
        {
            return ItemPropertyPluginRuntime.Current.GetActiveProperty(oItem, nIndex);
        }
    }
}
