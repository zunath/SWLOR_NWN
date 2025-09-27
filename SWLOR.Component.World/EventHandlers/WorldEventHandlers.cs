using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Service;
using SWLOR.Shared.Domain.Common.Contracts;
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
        private readonly IServiceProvider _serviceProvider;

        // Lazy-loaded services to break circular dependencies
        private ISpawnService SpawnService => _serviceProvider.GetRequiredService<ISpawnService>();
        private IAreaService AreaService => _serviceProvider.GetRequiredService<IAreaService>();
        private IMusicService MusicService => _serviceProvider.GetRequiredService<IMusicService>();
        private IPlanetService PlanetService => _serviceProvider.GetRequiredService<IPlanetService>();
        private IWeatherService WeatherService => _serviceProvider.GetRequiredService<IWeatherService>();
        private IObjectVisibilityService ObjectVisibilityService => _serviceProvider.GetRequiredService<IObjectVisibilityService>();
        private ITaxiService TaxiService => _serviceProvider.GetRequiredService<ITaxiService>();
        private ITileMagicService TileMagicService => _serviceProvider.GetRequiredService<ITileMagicService>();
        private IGuiService Gui => _serviceProvider.GetRequiredService<IGuiService>();

        public WorldEventHandlers(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [ScriptHandler<OnModuleCacheBefore>]
        public void OnModuleCacheBefore()
        {
            SpawnService.CacheData();
            AreaService.CacheData();
            MusicService.LoadSongList();
            PlanetService.CacheData();
            WeatherService.LoadData();
            ObjectVisibilityService.LoadVisibilityObjects();
            TaxiService.LoadTaxiDestinations();
        }

        [ScriptHandler<OnModuleCacheAfter>]
        public void OnModuleCacheAfter()
        {
            AreaService.RemoveInstancesFromCache();
        }

        [ScriptHandler<OnModuleEnter>]
        public void OnModuleEnter()
        {
            ObjectVisibilityService.LoadPlayerVisibilityObjects();
        }

        [ScriptHandler<OnAreaEnter>]
        public void OnAreaEnter()
        {
            SpawnService.SpawnArea();
            AreaService.EnterArea();
            MusicService.ApplyBattleThemeToPlayer();
            WeatherService.OnAreaEnter();
        }

        [ScriptHandler<OnAreaExit>]
        public void OnAreaExit()
        {
            SpawnService.QueueDespawnArea();
            AreaService.ExitArea();
        }

        [ScriptHandler<OnCreatureDeathAfter>]
        [ScriptHandler(ScriptName.OnPlaceableDeath)]
        public void OnCreatureDeathAfter()
        {
            SpawnService.QueueRespawn();
        }

        [ScriptHandler(ScriptName.OnSwlorHeartbeat)]
        public void OnSwlorHeartbeat()
        {
            SpawnService.ProcessSpawnSystem();
            WeatherService.OnModuleHeartbeat();
        }

        [ScriptHandler<OnDMSpawnObjectAfter>]
        public void OnDMSpawnObjectAfter()
        {
            SpawnService.DMSpawnCreature();
        }
    }
}
