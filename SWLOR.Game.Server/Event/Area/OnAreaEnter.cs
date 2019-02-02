﻿using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Area
{
    internal class OnAreaEnter: IRegisteredEvent
    {
        private readonly IBaseService _base;
        private readonly ICraftService _craft;
        private readonly IMapService _map;
        private readonly IPlayerService _player;
        private readonly IWeatherService _weather;

        public OnAreaEnter(
            IBaseService baseService,
            IPlayerService player,
            IMapService map,
            ICraftService craft,
            IWeatherService weather)
        {
            _base = baseService;
            _player = player;
            _map = map;
            _craft = craft;
            _weather = weather;
        }

        public bool Run(params object[] args)
        {
            // Location loading code is in the BaseService, to support
            // logging on within an instance.  This must be called before
            // the player service.
            _base.OnAreaEnter();
            _player.OnAreaEnter();
            _map.OnAreaEnter();
            _craft.OnAreaEnter();
            _weather.OnAreaEnter();

            return true;
        }
    }
}
