using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleUseFeat : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            AbilityService.OnModuleUseFeat();
            PlayerService.OnModuleUseFeat();
            BaseService.OnModuleUseFeat();
            CraftService.OnModuleUseFeat();
            ChatCommandService.OnModuleUseFeat();
            return true;

        }
    }
}
