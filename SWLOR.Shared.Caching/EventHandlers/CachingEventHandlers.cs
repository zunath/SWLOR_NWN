using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Shared.Caching.EventHandlers
{
    internal class CachingEventHandlers
    {
        private readonly IAreaCacheService _areaCacheService;
        private readonly IItemCacheService _itemCacheService;
        private readonly IModuleCacheService _moduleCacheService;
        private readonly IPortraitCacheService _portraitCacheService;
        private readonly ISongCacheService _songCacheService;
        private readonly ISoundSetCacheService _soundSetCacheService;

        public CachingEventHandlers(
            IAreaCacheService areaCacheService,
            IItemCacheService itemCacheService,
            IModuleCacheService moduleCacheService,
            IPortraitCacheService portraitCacheService,
            ISongCacheService songCacheService,
            ISoundSetCacheService soundSetCacheService,
            IEventAggregator eventAggregator)
        {
            _areaCacheService = areaCacheService;
            _itemCacheService = itemCacheService;
            _moduleCacheService = moduleCacheService;
            _portraitCacheService = portraitCacheService;
            _songCacheService = songCacheService;
            _soundSetCacheService = soundSetCacheService;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => LoadAreaCache());
            eventAggregator.Subscribe<OnModuleContentChange>(e => CacheItemNamesByResref());
            eventAggregator.Subscribe<OnHookEvents>(e => LoadModuleCache());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => LoadPortraitCache());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => LoadSongCache());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => LoadSoundSetCache());
        }

        public void LoadAreaCache()
        {
            _areaCacheService.LoadCache();
        }

        public void CacheItemNamesByResref()
        {
            _itemCacheService.CacheItemNamesByResref();
        }

        public void LoadModuleCache()
        {
            _moduleCacheService.LoadCache();
        }

        public void LoadPortraitCache()
        {
            _portraitCacheService.LoadCache();
        }

        public void LoadSongCache()
        {
            _songCacheService.LoadCache();
        }

        public void LoadSoundSetCache()
        {
            _soundSetCacheService.LoadCache();
        }
    }
}
