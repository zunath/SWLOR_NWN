using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The NPC groups: the logic and state that used to be static in <c>Service.NPCGroup</c>, moved here unchanged. The
    /// event handler runs on the container's instance; static callers reach it through <c>GameServices.NPCGroup</c>.
    /// </summary>
    public class NPCGroupService : INPCGroupService
    {
        private readonly Dictionary<NPCGroupType, NPCGroupAttribute> _npcGroups = new();

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CacheData()
        {
            RegisterNPCGroups();
        }

        public NPCGroupAttribute GetNPCGroup(NPCGroupType npcGroupType)
        {
            return _npcGroups[npcGroupType];
        }


        /// <summary>
        /// When the module loads, all of the NPCGroupTypes are iterated over and their data is stored into the cache.
        /// </summary>
        private void RegisterNPCGroups()
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
