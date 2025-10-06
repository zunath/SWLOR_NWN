using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Events.Attributes;
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
            ISoundSetCacheService soundSetCacheService)
        {
            _areaCacheService = areaCacheService;
            _itemCacheService = itemCacheService;
            _moduleCacheService = moduleCacheService;
            _portraitCacheService = portraitCacheService;
            _songCacheService = songCacheService;
            _soundSetCacheService = soundSetCacheService;
        }

        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadAreaCache()
        {
            _areaCacheService.LoadCache();
        }

        [ScriptHandler<OnModuleContentChange>]
        public void CacheItemNamesByResref()
        {
            _itemCacheService.CacheItemNamesByResref();
        }

        [ScriptHandler<OnHookEvents>]
        public void LoadModuleCache()
        {
            _moduleCacheService.LoadCache();
        }

        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadPortraitCache()
        {
            _portraitCacheService.LoadCache();
        }

        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadSongCache()
        {
            _songCacheService.LoadCache();
        }

        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadSoundSetCache()
        {
            _soundSetCacheService.LoadCache();
        }
    }
}
