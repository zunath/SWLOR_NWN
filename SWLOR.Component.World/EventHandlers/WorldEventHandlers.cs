using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Domain.Combat.Events;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Server;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.World.EventHandlers
{
    internal class WorldEventHandlers
    {
        private readonly ISpawnService _spawnService;
        private readonly IAreaService _areaService;
        private readonly IMusicService _musicService;
        private readonly IPlanetService _planetService;
        private readonly IWeatherService _weatherService;
        private readonly IVisibilityObjectCacheService _visibilityObjectCacheService;
        private readonly IPlayerVisibilityService _playerVisibilityService;
        private readonly ITaxiService _taxiService;

        public WorldEventHandlers(
            ISpawnService spawnService,
            IAreaService areaService,
            IMusicService musicService,
            IPlanetService planetService,
            IWeatherService weatherService,
            IVisibilityObjectCacheService visibilityObjectCacheService,
            IPlayerVisibilityService playerVisibilityService,
            ITaxiService taxiService,
            IEventAggregator eventAggregator)
        {
            _spawnService = spawnService;
            _areaService = areaService;
            _musicService = musicService;
            _planetService = planetService;
            _weatherService = weatherService;
            _visibilityObjectCacheService = visibilityObjectCacheService;
            _playerVisibilityService = playerVisibilityService;
            _taxiService = taxiService;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => OnModuleCacheBefore());
            eventAggregator.Subscribe<OnModuleCacheAfter>(e => OnModuleCacheAfter());
            eventAggregator.Subscribe<OnModuleEnter>(e => OnModuleEnter());
            eventAggregator.Subscribe<OnAreaEnter>(e => OnAreaEnter());
            eventAggregator.Subscribe<OnAreaExit>(e => OnAreaExit());
            eventAggregator.Subscribe<OnCreatureDeathAfter>(e => OnCreatureDeathAfter());
            eventAggregator.Subscribe<OnServerHeartbeat>(e => OnSwlorHeartbeat());
            eventAggregator.Subscribe<OnDMSpawnObjectAfter>(e => OnDMSpawnObjectAfter());
        }
        public void OnModuleCacheBefore()
        {
            _spawnService.CacheData();
            _musicService.LoadSongList();
            _planetService.CacheData();
            _weatherService.LoadData();
            _visibilityObjectCacheService.LoadVisibilityObjects();
            _taxiService.LoadTaxiDestinations();
        }
        public void OnModuleCacheAfter()
        {
            _areaService.RemoveInstancesFromCache();
        }
        public void OnModuleEnter()
        {
            _playerVisibilityService.LoadPlayerVisibilityObjects();
        }
        public void OnAreaEnter()
        {
            _spawnService.SpawnArea();
            _areaService.EnterArea();
            _musicService.ApplyBattleThemeToPlayer();
            _weatherService.OnAreaEnter();
        }
        public void OnAreaExit()
        {
            _spawnService.QueueDespawnArea();
            _areaService.ExitArea();
        }

        public void OnCreatureDeathAfter()
        {
            _spawnService.QueueRespawn();
        }
        public void OnSwlorHeartbeat()
        {
            _spawnService.ProcessSpawnSystem();
            _weatherService.OnModuleHeartbeat();
        }
        public void OnDMSpawnObjectAfter()
        {
            _spawnService.DMSpawnCreature();
        }
    }
}
