using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleChat : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IPlayerDescriptionService _playerDescription;

        public OnModuleChat(
            INWScript script,
            IPlayerDescriptionService playerDescription)
        {
            _ = script;
            _playerDescription = playerDescription;
        }

        public bool Run(params object[] args)
        {
            _playerDescription.OnModuleChat();
            return true;

        }
    }
}
