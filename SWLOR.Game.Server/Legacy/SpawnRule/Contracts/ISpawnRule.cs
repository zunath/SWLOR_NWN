using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.SpawnRule.Contracts
{
    public interface ISpawnRule
    {
        void Run(NWObject target, params object[] args);
    }
}
