using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleRespawn : IRegisteredEvent
    {
        private readonly IDeathService _death;

        public OnModuleRespawn(IDeathService death)
        {
            _death = death;
        }

        public bool Run(params object[] args)
        {
            _death.OnPlayerRespawn();
            return true;
        }
    }
}
