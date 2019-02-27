using NWN;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXItemProperty
    {
        ItemProperty NWNX_ItemProperty_PackIP(ItemPropertyUnpacked n);
        ItemPropertyUnpacked UnpackIP(ItemProperty ip);
    }
}