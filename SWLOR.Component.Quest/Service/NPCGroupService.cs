using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Quest.Enums;

namespace SWLOR.Component.Quest.Service
{
    public class NPCGroupService : INPCGroupService
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded service to break circular dependency
        private readonly Lazy<IGenericCacheService> _cacheService;
        private static IEnumCache<NPCGroupType, NPCGroupAttribute> _npcGroupCache;

        public NPCGroupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _cacheService = new Lazy<IGenericCacheService>(() => _serviceProvider.GetRequiredService<IGenericCacheService>());
        }
        
        // Lazy-loaded service to break circular dependency
        private IGenericCacheService CacheService => _cacheService.Value;

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        public void CacheData()
        {
            RegisterNPCGroups();
        }

        /// <summary>
        /// Retrieves an NPC group detail by the type.
        /// </summary>
        /// <param name="npcGroupType">The type of NPC group to retrieve.</param>
        /// <returns>An NPC group detail</returns>
        public NPCGroupAttribute GetNPCGroup(NPCGroupType npcGroupType)
        {
            return _npcGroupCache?.AllItems[npcGroupType] ?? throw new KeyNotFoundException($"NPC Group {npcGroupType} not found in cache");
        }


        /// <summary>
        /// When the module loads, all of the NPCGroupTypes are iterated over and their data is stored into the cache.
        /// </summary>
        private void RegisterNPCGroups()
        {
            _npcGroupCache = CacheService.BuildEnumCache<NPCGroupType, NPCGroupAttribute>()
                .WithAllItems()
                .Build();

            Console.WriteLine($"Loaded {_npcGroupCache.AllItems.Count} NPC groups.");
        }
    }
}
