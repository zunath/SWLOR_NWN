
using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleUseFeat : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IAbilityService _ability;
        private readonly IPlayerService _player;

        public OnModuleUseFeat(
            INWScript script,
            IAbilityService ability,
            IPlayerService player)
        {
            _ = script;
            _ability = ability;
            _player = player;
        }

        public bool Run(params object[] args)
        {
            _ability.OnModuleUseFeat();
            _player.OnModuleUseFeat();
            return true;

        }
    }
}
