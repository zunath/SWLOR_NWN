using SWLOR.Game.Server.Service.NPCService;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service
{
    public static class NPCGroup
    {
        private static readonly Dictionary<NPCGroupType, NPCGroupAttribute> _npcGroups = new();

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        [NWNEventHandler("mod_cache")]
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
            return _npcGroups[npcGroupType];
        }


        /// <summary>
        /// When the module loads, all of the NPCGroupTypes are iterated over and their data is stored into the cache.
        /// </summary>
        private static void RegisterNPCGroups()
        {
            var npcGroups = Enum.GetValues(typeof(NPCGroupType)).Cast<NPCGroupType>();
            foreach (var npcGroupType in npcGroups)
            {
                var npcGroupDetail = npcGroupType.GetAttribute<NPCGroupType, NPCGroupAttribute>();
                _npcGroups[npcGroupType] = npcGroupDetail;
            }

            Console.WriteLine($"Loaded {_npcGroups.Count} NPC groups.");
        }
    }
}
