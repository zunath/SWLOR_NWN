using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.World.EventHandlers
{
    internal class WorldEventHandlers
    {
        private readonly ISpawnService _spawnService;
        private readonly IAreaService _areaService;
        private readonly IMusicService _musicService;
        private readonly IPlanetService _planetService;
        private readonly IWeatherService _weatherService;
        private readonly IObjectVisibilityService _objectVisibilityService;
        private readonly ITaxiService _taxiService;

        public WorldEventHandlers(
            ISpawnService spawnService,
            IAreaService areaService,
            IMusicService musicService,
            IPlanetService planetService,
            IWeatherService weatherService,
            IObjectVisibilityService objectVisibilityService,
            ITaxiService taxiService)
        {
            _spawnService = spawnService;
            _areaService = areaService;
            _musicService = musicService;
            _planetService = planetService;
            _weatherService = weatherService;
            _objectVisibilityService = objectVisibilityService;
            _taxiService = taxiService;
        }

        [ScriptHandler<OnModuleCacheBefore>]
        public void OnModuleCacheBefore()
        {
            _spawnService.CacheData();
            _musicService.LoadSongList();
            _planetService.CacheData();
            _weatherService.LoadData();
            _objectVisibilityService.LoadVisibilityObjects();
            _taxiService.LoadTaxiDestinations();
        }

        [ScriptHandler<OnModuleCacheAfter>]
        public void OnModuleCacheAfter()
        {
            _areaService.RemoveInstancesFromCache();
        }

        [ScriptHandler<OnModuleEnter>]
        public void OnModuleEnter()
        {
            _objectVisibilityService.LoadPlayerVisibilityObjects();
        }

        [ScriptHandler<OnAreaEnter>]
        public void OnAreaEnter()
        {
            _spawnService.SpawnArea();
            _areaService.EnterArea();
            _musicService.ApplyBattleThemeToPlayer();
            _weatherService.OnAreaEnter();
        }

        [ScriptHandler<OnAreaExit>]
        public void OnAreaExit()
        {
            _spawnService.QueueDespawnArea();
            _areaService.ExitArea();
        }

        [ScriptHandler<OnCreatureDeathAfter>]
        [ScriptHandler(ScriptName.OnPlaceableDeath)]
        public void OnCreatureDeathAfter()
        {
            _spawnService.QueueRespawn();
        }

        [ScriptHandler(ScriptName.OnSwlorHeartbeat)]
        public void OnSwlorHeartbeat()
        {
            _spawnService.ProcessSpawnSystem();
            _weatherService.OnModuleHeartbeat();
        }

        [ScriptHandler<OnDMSpawnObjectAfter>]
        public void OnDMSpawnObjectAfter()
        {
            _spawnService.DMSpawnCreature();
        }
    }
}
