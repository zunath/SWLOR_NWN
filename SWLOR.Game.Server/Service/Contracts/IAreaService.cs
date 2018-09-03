using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IAreaService
    {
        void OnModuleLoad();
        void PurchaseArea(NWPlayer player, NWArea area, string sector);
    }
}