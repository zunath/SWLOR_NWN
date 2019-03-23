using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Area
{
    internal class OnAreaExit: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            SkillService.OnAreaExit();
            MapService.OnAreaExit();
            return true;
        }
    }
}
