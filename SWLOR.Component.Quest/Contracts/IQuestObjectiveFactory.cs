using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Quest.Contracts
{
    /// <summary>
    /// Factory interface for creating quest objective instances.
    /// This allows for proper DI management of objective creation.
    /// </summary>
    public interface IQuestObjectiveFactory
    {
        /// <summary>
        /// Creates a new CollectItemObjective instance.
        /// </summary>
        /// <param name="resref">The resref of the required item</param>
        /// <param name="amount">The amount of items needed</param>
        /// <returns>A new CollectItemObjective instance</returns>
        IQuestObjective CreateCollectItemObjective(string resref, int amount);

        /// <summary>
        /// Creates a new KillTargetObjective instance.
        /// </summary>
        /// <param name="group">The NPC group type to kill</param>
        /// <param name="amount">The amount of NPCs to kill</param>
        /// <returns>A new KillTargetObjective instance</returns>
        IQuestObjective CreateKillTargetObjective(NPCGroupType group, int amount);
    }
}
