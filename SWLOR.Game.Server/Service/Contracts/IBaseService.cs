using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IBaseService
    {
        void OnModuleUseFeat();
        PCTempBaseData GetPlayerTempData(NWPlayer player);
        void ClearPlayerTempData(NWPlayer player);
        void PurchaseArea(NWPlayer player, NWArea area, string sector);
        void OnModuleHeartbeat();
    }
}