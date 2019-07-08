using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.AreaSpecific.Contracts
{
    public interface IAreaInstance
    {
        void OnSpawn(NWArea area);
    }
}
