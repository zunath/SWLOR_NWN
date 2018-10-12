using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Area
{
    internal class OnAreaExit: IRegisteredEvent
    {
        private readonly ISkillService _skill;
        private readonly IMapService _map;

        public OnAreaExit(
            ISkillService skill,
            IMapService map)
        {
            _skill = skill;
            _map = map;
        }

        public bool Run(params object[] args)
        {
            _skill.OnAreaExit();
            _map.OnAreaExit();
            return true;
        }
    }
}
