using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The NPC groups: the display details of every <see cref="NPCGroupType"/>, cached when the module loads. Static
    /// callers reach the container's instance through <c>GameServices.NPCGroup</c>; the event handler is on
    /// <see cref="NPCGroupService"/>.
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
