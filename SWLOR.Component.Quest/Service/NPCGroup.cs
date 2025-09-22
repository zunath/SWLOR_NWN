using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Enums;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Quest.Service
{
    public class NPCGroup : INPCGroupService
    {
        private readonly IGenericCacheService _cacheService;
        private static IEnumCache<NPCGroupType, NPCGroupAttribute> _npcGroupCache;

        public NPCGroup(IGenericCacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
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
            _npcGroupCache = _cacheService.BuildEnumCache<NPCGroupType, NPCGroupAttribute>()
                .WithAllItems()
                .Build();

            Console.WriteLine($"Loaded {_npcGroupCache.AllItems.Count} NPC groups.");
        }
    }
}
