using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleDeath : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            DeathService.OnPlayerDeath();

            return true;

        }
    }
}
