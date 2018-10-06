using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IChatTextService
    {
        void OnModuleChat();
        void OnNWNXChat();
        void OnModuleEnter();
    }
}