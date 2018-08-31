using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleDeath : IRegisteredEvent
    {
        private readonly IDeathService _death;

        public OnModuleDeath(IDeathService death)
        {
            _death = death;
        }

        public bool Run(params object[] args)
        {
            _death.OnPlayerDeath();

            return true;

        }
    }
}
