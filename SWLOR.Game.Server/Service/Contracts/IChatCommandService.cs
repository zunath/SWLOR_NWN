using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IChatCommandService
    {
        void OnModuleNWNXChat(NWPlayer sender);
        void OnModuleUseFeat();
    }
}
