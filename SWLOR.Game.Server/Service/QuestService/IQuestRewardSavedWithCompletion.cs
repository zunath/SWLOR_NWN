using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>
    /// A quest reward that records what it owes on the player record in the same save that completes the quest, so a server stop between
    /// the completion and the award cannot lose it. <see cref="IQuestReward.GiveReward"/> then gives what was recorded.
    /// </summary>
    public interface IQuestRewardSavedWithCompletion : IQuestReward
    {
        /// <summary>Records the reward on <paramref name="dbPlayer"/>; the caller saves it with the completion.</summary>
        void SaveWithCompletion(uint player, Player dbPlayer);
    }
}
