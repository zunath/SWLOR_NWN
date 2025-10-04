using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX.Model;

namespace SWLOR.NWN.API.NWNX;

public interface IItemPropertyPluginService
{
    /// <summary>
    /// Convert native itemproperty type to unpacked structure.
    /// </summary>
    /// <param name="ip">The itemproperty to convert.</param>
    /// <returns>A constructed ItemPropertyUnpacked.</returns>
    ItemPropertyUnpacked UnpackIP(ItemProperty ip);

    /// <summary>
    /// Convert unpacked itemproperty structure to native type.
    /// </summary>
    /// <param name="itemProperty">The ItemPropertyUnpacked structure to convert.</param>
    /// <returns>The itemproperty.</returns>
    ItemProperty PackIP(ItemPropertyUnpacked itemProperty);

    /// <summary>
    /// Gets the active item property at the index.
    /// </summary>
    /// <param name="oItem">The item with the property.</param>
    /// <param name="nIndex">The index such as returned by some Item Events.</param>
    /// <returns>A constructed ItemPropertyUnpacked, except for creator, and spell id.</returns>
    ItemPropertyUnpacked GetActiveProperty(uint oItem, int nIndex);
}