using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IAreaService
    {
        void OnModuleLoad();
        NWArea CreateAreaInstance(string areaResref, string areaName);
    }
}