using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Contracts
{
    /// <summary>
    /// Factory interface for creating quest prerequisite instances.
    /// This allows for proper DI management of prerequisite creation.
    /// </summary>
    public interface IQuestPrerequisiteFactory
    {
        /// <summary>
        /// Creates a new RequiredQuestPrerequisite instance.
        /// </summary>
        /// <param name="prerequisiteQuestId">The ID of the prerequisite quest</param>
        /// <returns>A new RequiredQuestPrerequisite instance</returns>
        IQuestPrerequisite CreateRequiredQuestPrerequisite(string prerequisiteQuestId);

        /// <summary>
        /// Creates a new RequiredKeyItemPrerequisite instance.
        /// </summary>
        /// <param name="keyItemType">The type of key item required</param>
        /// <returns>A new RequiredKeyItemPrerequisite instance</returns>
        IQuestPrerequisite CreateRequiredKeyItemPrerequisite(KeyItemType keyItemType);

        /// <summary>
        /// Creates a new RequiredFactionStandingPrerequisite instance.
        /// </summary>
        /// <param name="faction">The faction type for the standing requirement</param>
        /// <param name="requiredAmount">The required standing amount</param>
        /// <returns>A new RequiredFactionStandingPrerequisite instance</returns>
        IQuestPrerequisite CreateRequiredFactionStandingPrerequisite(FactionType faction, int requiredAmount);
    }
}
