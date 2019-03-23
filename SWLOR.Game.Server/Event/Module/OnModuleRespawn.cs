using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleRespawn : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            DeathService.OnPlayerRespawn();
            return true;
        }
    }
}
