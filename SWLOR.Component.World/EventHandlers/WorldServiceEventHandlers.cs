using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Service;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Server;

namespace SWLOR.Component.World.EventHandlers
{
    internal class WorldServiceEventHandlers
    {
        private readonly IPlanetService _planetService;
        private readonly IPlayerVisibilityService _playerVisibilityService;
        private readonly ITaxiService _taxiService;
        private readonly ITileMagicService _tileMagicService;
        private readonly IVisibilityObjectCacheService _visibilityObjectCacheService;
        private readonly IWalkmeshService _walkmeshService;
        private readonly IWeatherClimateService _weatherClimateService;
        private readonly IWeatherService _weatherService;
        private readonly IWeatherVisualService _weatherVisualService;

        public WorldServiceEventHandlers(
            IPlanetService planetService,
            IPlayerVisibilityService playerVisibilityService,
            ITaxiService taxiService,
            ITileMagicService tileMagicService,
            IVisibilityObjectCacheService visibilityObjectCacheService,
            IWalkmeshService walkmeshService,
            IWeatherClimateService weatherClimateService,
            IWeatherService weatherService,
            IWeatherVisualService weatherVisualService)
        {
            _planetService = planetService;
            _playerVisibilityService = playerVisibilityService;
            _taxiService = taxiService;
            _tileMagicService = tileMagicService;
            _visibilityObjectCacheService = visibilityObjectCacheService;
            _walkmeshService = walkmeshService;
            _weatherClimateService = weatherClimateService;
            _weatherService = weatherService;
            _weatherVisualService = weatherVisualService;
        }

        /// <summary>
        /// Loads visibility objects for a player when they enter the server.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void LoadPlayerVisibilityObjects()
        {
            _playerVisibilityService.LoadPlayerVisibilityObjects();
        }

        /// <summary>
        /// When the module loads, load the tile magic configured on every area.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void ApplyAreaConfiguration()
        {
            _tileMagicService.ApplyAreaConfiguration();
        }

        /// <summary>
        /// When the module loads, retrieve the list of walkable locations from the database.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void RetrieveWalkmeshes()
        {
            _walkmeshService.RetrieveWalkmeshes();
        }

        /// <summary>
        /// When the module loads, cache planet climates and other pertinent data.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadWeatherData()
        {
            _weatherService.LoadData();
        }

        /// <summary>
        /// Handles weather when a player enters an area.
        /// </summary>
        [ScriptHandler<OnAreaEnter>]
        public void OnWeatherAreaEnter()
        {
            _weatherService.OnAreaEnter();
        }

        /// <summary>
        /// Handles weather on server heartbeat.
        /// </summary>
        [ScriptHandler<OnServerHeartbeat>]
        public void OnWeatherHeartbeat()
        {
            _weatherService.OnModuleHeartbeat();
        }

        /// <summary>
        /// When the module loads, cache planet climates and other pertinent data.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadWeatherClimateData()
        {
            _weatherClimateService.LoadData();
        }

        /// <summary>
        /// When the module loads, cache relevant data needed by the Planet service.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CachePlanetData()
        {
            _planetService.CacheData();
        }
    }
}
