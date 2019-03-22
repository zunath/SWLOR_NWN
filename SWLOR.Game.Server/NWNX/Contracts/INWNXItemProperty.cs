using NWN;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXItemProperty
    {
        ItemProperty PackIP(ItemPropertyUnpacked n);
        ItemPropertyUnpacked UnpackIP(ItemProperty ip);
    }
}