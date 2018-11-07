using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Area
{
    internal class OnAreaEnter: IRegisteredEvent
    {
        private readonly IPlayerService _player;
        private readonly IMapService _map;

        public OnAreaEnter(
            IPlayerService player,
            IMapService map)
        {
            _player = player;
            _map = map;
        }

        public bool Run(params object[] args)
        {
            _player.OnAreaEnter();
            _map.OnAreaEnter();

            return true;
        }
    }
}
