using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleDying : IRegisteredEvent
    {
        private readonly IDeathService _death;

        public OnModuleDying(IDeathService death)
        {
            _death = death;
        }

        public bool Run(params object[] args)
        {
            _death.OnPlayerDying();

            return true;

        }
    }
}
