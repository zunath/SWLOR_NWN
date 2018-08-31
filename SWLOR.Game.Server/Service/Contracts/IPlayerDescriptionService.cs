using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IPlayerDescriptionService
    {
        void OnModuleChat();
        void ChangePlayerDescription(NWPlayer player);
    }
}
