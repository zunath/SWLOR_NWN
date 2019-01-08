using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Area
{
    internal class OnAreaEnter: IRegisteredEvent
    {
        private readonly ICraftService _craft;
        private readonly IMapService _map;
        private readonly IPlayerService _player;
        private readonly IWeatherService _weather;

        public OnAreaEnter(
            IPlayerService player,
            IMapService map,
            ICraftService craft,
            IWeatherService weather)
        {
            _player = player;
            _map = map;
            _craft = craft;
            _weather = weather;
        }

        public bool Run(params object[] args)
        {
            _player.OnAreaEnter();
            _map.OnAreaEnter();
            _craft.OnAreaEnter();
            _weather.OnAreaEnter();

            return true;
        }
    }
}
