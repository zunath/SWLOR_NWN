using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IActivityLoggingService
    {
        void OnModuleClientEnter();
        void OnModuleClientLeave();
        void OnModuleNWNXChat(NWPlayer sender);
    }
}
