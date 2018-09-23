using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IDeathService
    {
        void SetRespawnLocation(NWPlayer player);
        void OnPlayerDeath();
        void OnPlayerRespawn();
        void TeleportPlayerToBindPoint(NWPlayer pc);
    }
}
