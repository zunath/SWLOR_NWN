using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.SpawnRule.Contracts
{
    public interface ISpawnRule
    {
        void Run(NWObject target, params object[] args);
    }
}
