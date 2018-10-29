using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IChatTextService
    {
        void OnNWNXChat();
        void OnModuleEnter();
    }
}