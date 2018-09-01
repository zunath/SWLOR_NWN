using NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IComponentBonusService
    {
        void ApplyComponentBonus(NWItem product, ItemProperty sourceIP);
    }
}