using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Social.Enums;

namespace SWLOR.Component.Quest.Contracts
{
    /// <summary>
    /// Factory interface for creating quest reward instances.
    /// This allows for proper DI management of reward creation.
    /// </summary>
    public interface IQuestRewardFactory
    {
        /// <summary>
        /// Creates a new GoldReward instance.
        /// </summary>
        /// <param name="amount">The amount of gold to reward</param>
        /// <param name="isSelectable">Whether the reward is selectable</param>
        /// <param name="isGuildQuest">Whether this is a guild quest</param>
        /// <returns>A new GoldReward instance</returns>
        IQuestReward CreateGoldReward(int amount, bool isSelectable = true, bool isGuildQuest = false);

        /// <summary>
        /// Creates a new XPReward instance.
        /// </summary>
        /// <param name="amount">The amount of XP to reward</param>
        /// <param name="isSelectable">Whether the reward is selectable</param>
        /// <returns>A new XPReward instance</returns>
        IQuestReward CreateXPReward(int amount, bool isSelectable = true);

        /// <summary>
        /// Creates a new ItemReward instance.
        /// </summary>
        /// <param name="itemResref">The resref of the item to reward</param>
        /// <param name="quantity">The quantity of items to reward</param>
        /// <param name="isSelectable">Whether the reward is selectable</param>
        /// <returns>A new ItemReward instance</returns>
        IQuestReward CreateItemReward(string itemResref, int quantity, bool isSelectable = true);

        /// <summary>
        /// Creates a new KeyItemReward instance.
        /// </summary>
        /// <param name="keyItemType">The type of key item to reward</param>
        /// <param name="isSelectable">Whether the reward is selectable</param>
        /// <returns>A new KeyItemReward instance</returns>
        IQuestReward CreateKeyItemReward(KeyItemType keyItemType, bool isSelectable = true);

        /// <summary>
        /// Creates a new GPReward instance.
        /// </summary>
        /// <param name="guild">The guild type for the GP reward</param>
        /// <param name="amount">The amount of GP to reward</param>
        /// <param name="isSelectable">Whether the reward is selectable</param>
        /// <returns>A new GPReward instance</returns>
        IQuestReward CreateGPReward(GuildType guild, int amount, bool isSelectable = true);

        /// <summary>
        /// Creates a new FactionStandingReward instance.
        /// </summary>
        /// <param name="faction">The faction type for the standing reward</param>
        /// <param name="amount">The amount of standing to reward</param>
        /// <param name="isSelectable">Whether the reward is selectable</param>
        /// <returns>A new FactionStandingReward instance</returns>
        IQuestReward CreateFactionStandingReward(FactionType faction, int amount, bool isSelectable = true);

        /// <summary>
        /// Creates a new FactionPointsReward instance.
        /// </summary>
        /// <param name="faction">The faction type for the points reward</param>
        /// <param name="amount">The amount of points to reward</param>
        /// <param name="isSelectable">Whether the reward is selectable</param>
        /// <returns>A new FactionPointsReward instance</returns>
        IQuestReward CreateFactionPointsReward(FactionType faction, int amount, bool isSelectable = true);
    }
}
