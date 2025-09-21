using System;
using System.Collections.Generic;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Game.Server.Service
{
    public static class NPCGroup
    {
        private static readonly IGenericCacheService _cacheService = ServiceContainer.GetService<IGenericCacheService>();
        private static IEnumCache<NPCGroupType, NPCGroupAttribute> _npcGroupCache;

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public static void CacheData()
        {
            RegisterNPCGroups();
        }

        /// <summary>
        /// Retrieves an NPC group detail by the type.
        /// </summary>
        /// <param name="npcGroupType">The type of NPC group to retrieve.</param>
        /// <returns>An NPC group detail</returns>
        public static NPCGroupAttribute GetNPCGroup(NPCGroupType npcGroupType)
        {
            return _npcGroupCache?.AllItems[npcGroupType] ?? throw new KeyNotFoundException($"NPC Group {npcGroupType} not found in cache");
        }


        /// <summary>
        /// When the module loads, all of the NPCGroupTypes are iterated over and their data is stored into the cache.
        /// </summary>
        private static void RegisterNPCGroups()
        {
            _npcGroupCache = _cacheService.BuildEnumCache<NPCGroupType, NPCGroupAttribute>()
                .WithAllItems()
                .Build();

            Console.WriteLine($"Loaded {_npcGroupCache.AllItems.Count} NPC groups.");
        }
    }
}
