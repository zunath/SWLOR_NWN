using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleNWNXChat: IRegisteredEvent
    {
        private readonly IActivityLoggingService _activityLogging;
        private readonly IChatCommandService _chatCommand;
        
        public OnModuleNWNXChat(
            IActivityLoggingService activityLogging,
            IChatCommandService chatCommand)
        {
            _activityLogging = activityLogging;
            _chatCommand = chatCommand;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = NWPlayer.Wrap(Object.OBJECT_SELF);
            _activityLogging.OnModuleNWNXChat(player);
            _chatCommand.OnModuleNWNXChat(player);
            return true;
        }
    }
}
