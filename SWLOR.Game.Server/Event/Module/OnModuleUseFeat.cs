
using NWN;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleUseFeat : IRegisteredEvent
    {
        
        
        private readonly IPlayerService _player;
        private readonly IBaseService _base;
        private readonly ICraftService _craft;
        private readonly IChatCommandService _chat;

        public OnModuleUseFeat(
            
            
            IPlayerService player,
            IBaseService @base,
            ICraftService craft,
            IChatCommandService chat)
        {
            _player = player;
            _base = @base;
            _craft = craft;
            _chat = chat;
        }

        public bool Run(params object[] args)
        {
            AbilityService.OnModuleUseFeat();
            _player.OnModuleUseFeat();
            _base.OnModuleUseFeat();
            _craft.OnModuleUseFeat();
            _chat.OnModuleUseFeat();
            return true;

        }
    }
}
