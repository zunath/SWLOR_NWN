using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Server;
using SWLOR.Shared.Abstractions.Contracts;

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
            IWeatherVisualService weatherVisualService,
            IEventAggregator eventAggregator)
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

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleEnter>(e => LoadPlayerVisibilityObjects());
            eventAggregator.Subscribe<OnModuleLoad>(e => ApplyAreaConfiguration());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => RetrieveWalkmeshes());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => LoadWeatherData());
            eventAggregator.Subscribe<OnAreaEnter>(e => OnWeatherAreaEnter());
            eventAggregator.Subscribe<OnServerHeartbeat>(e => OnWeatherHeartbeat());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => LoadWeatherClimateData());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CachePlanetData());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => LoadWalkmeshes());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => LoadVisibilityObjects());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => LoadTaxiDestinations());
        }

        /// <summary>
        /// Loads visibility objects for a player when they enter the server.
        /// </summary>
        public void LoadPlayerVisibilityObjects()
        {
            _playerVisibilityService.LoadPlayerVisibilityObjects();
        }

        /// <summary>
        /// When the module loads, load the tile magic configured on every area.
        /// </summary>
        public void ApplyAreaConfiguration()
        {
            _tileMagicService.ApplyAreaConfiguration();
        }

        /// <summary>
        /// When the module loads, retrieve the list of walkable locations from the database.
        /// </summary>
        public void RetrieveWalkmeshes()
        {
            _walkmeshService.RetrieveWalkmeshes();
        }

        /// <summary>
        /// When the module loads, cache planet climates and other pertinent data.
        /// </summary>
        public void LoadWeatherData()
        {
            _weatherService.LoadData();
        }

        /// <summary>
        /// Handles weather when a player enters an area.
        /// </summary>
        public void OnWeatherAreaEnter()
        {
            _weatherService.OnAreaEnter();
        }

        /// <summary>
        /// Handles weather on server heartbeat.
        /// </summary>
        public void OnWeatherHeartbeat()
        {
            _weatherService.OnModuleHeartbeat();
        }

        /// <summary>
        /// When the module loads, cache planet climates and other pertinent data.
        /// </summary>
        public void LoadWeatherClimateData()
        {
            _weatherClimateService.LoadData();
        }

        /// <summary>
        /// When the module loads, cache relevant data needed by the Planet service.
        /// </summary>
        public void CachePlanetData()
        {
            _planetService.CacheData();
        }

        /// <summary>
        /// When the module content changes, rerun the baking process.
        /// </summary>
        public void LoadWalkmeshes()
        {
            _walkmeshService.LoadWalkmeshes();
        }

        /// <summary>
        /// Loads all visibility objects from areas and caches them.
        /// </summary>
        public void LoadVisibilityObjects()
        {
            _visibilityObjectCacheService.LoadVisibilityObjects();
        }

        /// <summary>
        /// When the module loads, cache all taxi destinations.
        /// </summary>
        public void LoadTaxiDestinations()
        {
            _taxiService.LoadTaxiDestinations();
        }
    }
}
