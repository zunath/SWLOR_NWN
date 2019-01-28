using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleNWNXChat: IRegisteredEvent
    {
        private readonly IActivityLoggingService _activityLogging;
        private readonly IChatCommandService _chatCommand;
        private readonly IBaseService _base;
        private readonly IChatTextService _chatText;
        private readonly ICraftService _craft;
        private readonly ISpaceService _space;

        public OnModuleNWNXChat(
            IActivityLoggingService activityLogging,
            IChatCommandService chatCommand,
            IBaseService @base,
            IChatTextService chatText,
            ICraftService craft,
            ISpaceService space)
        {
            _activityLogging = activityLogging;
            _chatCommand = chatCommand;
            _base = @base;
            _chatText = chatText;
            _craft = craft;
            _space = space;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = (Object.OBJECT_SELF);
            _activityLogging.OnModuleNWNXChat(player);
            _space.OnNWNXChat();
            _chatCommand.OnModuleNWNXChat(player);
            _base.OnModuleNWNXChat(player);
            _chatText.OnNWNXChat();
            _craft.OnNWNXChat();
            return true;
        }
    }
}
