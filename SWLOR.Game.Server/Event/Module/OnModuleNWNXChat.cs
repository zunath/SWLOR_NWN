using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleNWNXChat: IRegisteredEvent
    {
        private readonly IChatCommandService _chatCommand;
        private readonly IBaseService _base;
        private readonly IChatTextService _chatText;
        private readonly ICraftService _craft;
        private readonly ISpaceService _space;
        private readonly IMarketService _market;
        private readonly IMessageBoardService _messageBoard;

        public OnModuleNWNXChat(
            IChatCommandService chatCommand,
            IBaseService @base,
            IChatTextService chatText,
            ICraftService craft,
            ISpaceService space,
            IMarketService market,
            IMessageBoardService messageBoard)
        {
            _chatCommand = chatCommand;
            _base = @base;
            _chatText = chatText;
            _craft = craft;
            _space = space;
            _market = market;
            _messageBoard = messageBoard;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = (Object.OBJECT_SELF);
            ActivityLoggingService.OnModuleNWNXChat(player);
            _space.OnNWNXChat();
            _chatCommand.OnModuleNWNXChat(player);
            _base.OnModuleNWNXChat(player);
            _chatText.OnNWNXChat();
            _craft.OnNWNXChat();
            _market.OnModuleNWNXChat();
            _messageBoard.OnModuleNWNXChat();
            return true;
        }
    }
}
