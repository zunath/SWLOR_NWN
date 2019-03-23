using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Area
{
    internal class OnAreaEnter: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            // Location loading code is in the BaseService, to support
            // logging on within an instance.  This must be called before
            // the player service.
            BaseService.OnAreaEnter();
            PlayerService.OnAreaEnter();
            MapService.OnAreaEnter();
            CraftService.OnAreaEnter();
            WeatherService.OnAreaEnter();

            return true;
        }
    }
}
