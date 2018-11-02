using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ICachingService
    {
        void OnModuleClientEnter();
        void OnModuleEquipItem();
        void OnModuleLoad();
        void CachePCSkills(NWPlayer player);
    }
}