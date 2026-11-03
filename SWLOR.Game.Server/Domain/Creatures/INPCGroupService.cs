using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The NPC groups: the display details of every <see cref="NPCGroupType"/>, cached when the module loads. The
    /// static facade <c>Service.NPCGroup</c> forwards to the container's instance of this interface and keeps the
    /// event handler.
    /// </summary>
    public interface INPCGroupService
    {
        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Retrieves an NPC group detail by the type.
        /// </summary>
        /// <param name="npcGroupType">The type of NPC group to retrieve.</param>
        /// <returns>An NPC group detail</returns>
        NPCGroupAttribute GetNPCGroup(NPCGroupType npcGroupType);
    }
}
