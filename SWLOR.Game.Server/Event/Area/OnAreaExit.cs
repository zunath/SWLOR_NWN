using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Area
{
    internal class OnAreaExit: IRegisteredEvent
    {
        
        private readonly IMapService _map;

        public OnAreaExit(
            
            IMapService map)
        {
            
            _map = map;
        }

        public bool Run(params object[] args)
        {
            SkillService.OnAreaExit();
            _map.OnAreaExit();
            return true;
        }
    }
}
