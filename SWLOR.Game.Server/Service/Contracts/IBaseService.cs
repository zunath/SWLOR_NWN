using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IBaseService
    {
        void OnModuleUseFeat();
        BaseData GetPlayerTempData(NWPlayer player);
        void ClearPlayerTempData(NWPlayer player);
    }
}