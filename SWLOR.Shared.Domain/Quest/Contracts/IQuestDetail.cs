using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Delegates;
using SWLOR.Component.Quest.Model;
using SWLOR.Shared.Domain.Social.Enums;

namespace SWLOR.Component.Quest.Service;

public interface IQuestDetail
{
    string QuestId { get; set; }
    string Name { get; set; }
    bool IsRepeatable { get; set; }
    GuildType GuildType { get; set; }
    int GuildRank { get; set; }
    bool AllowRewardSelection { get; set; }
    List<IQuestReward> Rewards { get; }
    List<IQuestPrerequisite> Prerequisites { get; }
    Dictionary<int, QuestStateDetail> States { get; }
    List<AcceptQuestDelegate> OnAcceptActions { get; }
    List<AbandonQuestDelegate> OnAbandonActions { get; }
    List<AdvanceQuestDelegate> OnAdvanceActions { get; }
    List<CompleteQuestDelegate> OnCompleteActions { get; }

    /// <summary>
    /// Returns true if player can complete this quest. Returns false otherwise.
    /// </summary>
    /// <param name="player">The player to check</param>
    /// <returns>true if player can complete, false otherwise</returns>
    bool CanComplete(uint player);

    /// <summary>
    /// Returns the rewards given for completing this quest.
    /// </summary>
    /// <returns></returns>
    IEnumerable<IQuestReward> GetRewards();

    /// <summary>
    /// Gives all rewards for this quest to the player.
    /// </summary>
    /// <param name="player">The player receiving the rewards.</param>
    void GiveRewards(uint player);

    /// <summary>
    /// Abandons a quest.
    /// </summary>
    /// <param name="player">The player abandoning a quest.</param>
    void Abandon(uint player);

    /// <summary>
    /// Accepts a quest using the configured settings.
    /// </summary>
    /// <param name="player">The player accepting the quest.</param>
    /// <param name="questSource">The source of the quest giver</param>
    void Accept(uint player, uint questSource);

    /// <summary>
    /// Advances the player to the next quest state.
    /// </summary>
    /// <param name="player">The player advancing to the next quest state</param>
    /// <param name="questSource">The source of quest advancement</param>
    void Advance(uint player, uint questSource);

    /// <summary>
    /// Completes a quest for a player. If a reward is selected, that reward will be given to the player.
    /// Otherwise, all rewards configured for this quest will be given to the player.
    /// </summary>
    /// <param name="player">The player completing the quest.</param>
    /// <param name="questSource">The source of the quest completion</param>
    /// <param name="selectedReward">The reward selected by the player</param>
    void Complete(uint player, uint questSource, IQuestReward selectedReward);
}