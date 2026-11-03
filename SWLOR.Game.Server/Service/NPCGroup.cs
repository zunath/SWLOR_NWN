using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the NPC groups. The logic and state live in <see cref="INPCGroupService"/>; every member here
    /// forwards to the container's instance, and the event handler stays here so its name in the handler order manifest
    /// does not change.
    /// </summary>
    public static class NPCGroup
    {
        private static INPCGroupService Instance => FacadeAccessor.Resolve<INPCGroupService>();

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// Retrieves an NPC group detail by the type.
        /// </summary>
        /// <param name="npcGroupType">The type of NPC group to retrieve.</param>
        /// <returns>An NPC group detail</returns>
        public static NPCGroupAttribute GetNPCGroup(NPCGroupType npcGroupType)
        {
            return Instance.GetNPCGroup(npcGroupType);
        }
    }
}
