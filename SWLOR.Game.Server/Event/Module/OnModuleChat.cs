using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleChat : IRegisteredEvent
    {
        private readonly IPlayerDescriptionService _playerDescription;
        private readonly IChatTextService _chatText;

        public OnModuleChat(
            IChatTextService chatText,
            IPlayerDescriptionService playerDescription)
        {
            _chatText = chatText;
            _playerDescription = playerDescription;
        }

        public bool Run(params object[] args)
        {
            _playerDescription.OnModuleChat();
            _chatText.OnModuleChat();
            return true;

        }
    }
}
