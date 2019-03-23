using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleNWNXChat: IRegisteredEvent
    {

        public bool Run(params object[] args)
        {
            NWPlayer player = (Object.OBJECT_SELF);
            ActivityLoggingService.OnModuleNWNXChat(player);
            SpaceService.OnNWNXChat();
            ChatCommandService.OnModuleNWNXChat(player);
            BaseService.OnModuleNWNXChat(player);
            ChatTextService.OnNWNXChat();
            CraftService.OnNWNXChat();
            MarketService.OnModuleNWNXChat();
            MessageBoardService.OnModuleNWNXChat();
            return true;
        }
    }
}
