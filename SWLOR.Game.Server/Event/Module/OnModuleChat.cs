using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleChat : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            PlayerDescriptionService.OnModuleChat();
            return true;

        }
    }
}
