using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Area
{
    internal class OnAreaEnter: IRegisteredEvent
    {
        private readonly IPlayerService _player;
        private readonly IMapService _map;
        private readonly ICraftService _craft;

        public OnAreaEnter(
            IPlayerService player,
            IMapService map,
            ICraftService craft)
        {
            _player = player;
            _map = map;
            _craft = craft;
        }

        public bool Run(params object[] args)
        {
            _player.OnAreaEnter();
            _map.OnAreaEnter();
            _craft.OnAreaEnter();

            return true;
        }
    }
}
