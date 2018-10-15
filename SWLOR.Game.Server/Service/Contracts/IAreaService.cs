using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IAreaService
    {
        void OnModuleLoad();
        NWArea CreateAreaInstance(NWPlayer owner, string areaResref, string areaName, string entranceWaypointTag);
    }
}