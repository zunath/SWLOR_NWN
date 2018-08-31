using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleNWNXChat: IRegisteredEvent
    {
        private readonly IActivityLoggingService _activityLogging;
        private readonly IChatCommandService _chatCommand;
        private readonly IStructureService _structure;

        public OnModuleNWNXChat(
            IActivityLoggingService activityLogging,
            IChatCommandService chatCommand,
            IStructureService structure)
        {
            _activityLogging = activityLogging;
            _chatCommand = chatCommand;
            _structure = structure;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = NWPlayer.Wrap(Object.OBJECT_SELF);
            _activityLogging.OnModuleNWNXChat(player);
            _chatCommand.OnModuleNWNXChat(player);
            _structure.OnModuleNWNXChat(player);
            return true;
        }
    }
}
