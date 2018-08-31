using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Area
{
    internal class OnAreaEnter: IRegisteredEvent
    {
        private readonly IMigrationService _migration;
        private readonly IPlayerService _player;
        private readonly IMapService _map;

        public OnAreaEnter(
            IMigrationService migration,
            IPlayerService player,
            IMapService map)
        {
            _migration = migration;
            _player = player;
            _map = map;
        }

        public bool Run(params object[] args)
        {
            _migration.OnAreaEnter();
            _player.OnAreaEnter();
            _map.OnAreaEnter();

            return true;
        }
    }
}
