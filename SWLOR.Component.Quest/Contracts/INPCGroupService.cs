using SWLOR.Shared.Domain.Quest.Enums;

namespace SWLOR.Component.Quest.Contracts
{
    public interface INPCGroupService
    {
        void CacheData();

        /// <summary>
        /// Retrieves an NPC group detail by the type.
        /// </summary>
        /// <param name="npcGroupType">The type of NPC group to retrieve.</param>
        /// <returns>An NPC group detail</returns>
        NPCGroupAttribute GetNPCGroup(NPCGroupType npcGroupType);
    }
}
