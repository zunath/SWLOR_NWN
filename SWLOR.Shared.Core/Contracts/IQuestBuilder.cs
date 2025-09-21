using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IQuestBuilder
    {
        /// <summary>
        /// Creates a new quest with a given questId, name, and journalTag.
        /// All arguments are required. Exceptions will be thrown if any are null or whitespace.
        /// </summary>
        /// <param name="questId">The quest Id to assign this quest.</param>
        /// <param name="name">The name of the quest.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder Create(string questId, string name);

        /// <summary>
        /// Marks the quest as repeatable.
        /// </summary>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder IsRepeatable();

        /// <summary>
        /// Marks the quest as a guild task.
        /// </summary>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder IsGuildTask(GuildType guild, int rank);

        /// <summary>
        /// Marks that the quest allows the player to select a reward when completed.
        /// </summary>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder HasRewardSelection();

        /// <summary>
        /// Adds an item reward for completing this quest.
        /// </summary>
        /// <param name="itemResref">The resref of the item to create.</param>
        /// <param name="quantity">The number of items to create.</param>
        /// <param name="isSelectable">If true, player will have the option to select the item as a reward. If false, they will receive it no matter what.
        /// If IsRepeatable() has not been called, this argument is ignored and all items are given to the player.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder AddItemReward(string itemResref, int quantity, bool isSelectable = true);

        /// <summary>
        /// Adds a gold reward for completing this quest.
        /// </summary>
        /// <param name="amount">The amount of gold to create.</param>
        /// <param name="isSelectable">If true, player will have the option to select the gold as a reward. If false, they will receive it no matter what.
        /// If IsRepeatable() has not been called, this argument is ignored and all gold is given to the player.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder AddGoldReward(int amount, bool isSelectable = true);

        /// <summary>
        /// Adds an XP reward for completing this quest.
        /// </summary>
        /// <param name="amount">The amount of XP to give.</param>
        /// <param name="isSelectable">If true, player will have the option to select XP as a reward. If false, they will receive it no matter what.
        /// If IsRepeatable() has not been called, this argument is ignored and all XP is given to the player</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder AddXPReward(int amount, bool isSelectable = true);

        /// <summary>
        /// Adds a key item reward for completing this quest.
        /// </summary>
        /// <param name="keyItemType">The type of key item to award.</param>
        /// <param name="isSelectable">If true, player will have the option to select the key item as a reward. If false, they will receive it no matter what. If IsRepeatable() has not been called, this argument is ignored and all gold is given to the player.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder AddKeyItemReward(KeyItemType keyItemType, bool isSelectable = true);

        /// <summary>
        /// Adds a guild GP reward for completing this quest.
        /// </summary>
        /// <param name="guild">The type of guild GP to reward.</param>
        /// <param name="amount">The amount of GP to award</param>
        /// <param name="isSelectable">If true, player will have the option to select the GP as a reward. If false, they will receive it no matter what. If IsRepeatable() has not been called, this argument is ignored and all GP is given to the player.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder AddGPReward(GuildType guild, int amount, bool isSelectable = true);

        /// <summary>
        /// Adds a faction standing reward for completing this quest.
        /// </summary>
        /// <param name="faction">The type of faction to use.</param>
        /// <param name="amount">Amount of standing to give</param>
        /// <param name="isSelectable">If true, player will have the option to select the Standing as a reward. If false, they will receive it no matter what. If IsRepeatable() has not been called, this argument is ignored and all Standing is given to the player.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder AddFactionStandingReward(FactionType faction, int amount, bool isSelectable = true);

        /// <summary>
        /// Adds a faction point reward for completing this quest.
        /// </summary>
        /// <param name="faction">The type of faction to use.</param>
        /// <param name="amount">Amount of points to give</param>
        /// <param name="isSelectable">If true, player will have the option to select the Points as a reward. If false, they will receive it no matter what. If IsRepeatable() has not been called, this argument is ignored and all Standing is given to the player.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder AddFactionPointsReward(FactionType faction, int amount, bool isSelectable = true);

        /// <summary>
        /// Adds a prerequisite to the quest. If the player has not completed this prerequisite quest, they will be unable to accept it.
        /// </summary>
        /// <param name="prerequisiteQuestId">The unique Id of the prerequisite quest. If this Id has not been registered, an exception will be thrown.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder PrerequisiteQuest(string prerequisiteQuestId);

        /// <summary>
        /// Adds a prerequisite to the quest. If the player has not acquired this key item, they will be unable to accept it.
        /// </summary>
        /// <param name="keyItemType">The type of key item to require.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder PrerequisiteKeyItem(KeyItemType keyItemType);

        /// <summary>
        /// Adds an action to run when a player accepts a quest.
        /// </summary>
        /// <param name="action">The action to run when a player accepts a quest.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder OnAcceptAction(object action);

        /// <summary>
        /// Adds an action to run when a player abandons a quest.
        /// </summary>
        /// <param name="action">The action to run when a player abandons a quest.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder OnAbandonAction(object action);

        /// <summary>
        /// Adds an action to run when a player advances to a new quest state.
        /// </summary>
        /// <param name="action">The action to run when a player advances to a new quest state.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder OnAdvanceAction(object action);

        /// <summary>
        /// Adds an action to run when a player completes a quest.
        /// </summary>
        /// <param name="action">The action to run when a player completes the quest.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder OnCompleteAction(object action);

        /// <summary>
        /// Adds a new quest state to the quest.
        /// </summary>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder AddState();

        /// <summary>
        /// Sets the journal text of this quest state.
        /// </summary>
        /// <param name="journalText">The journal text to set.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder SetStateJournalText(string journalText);

        /// <summary>
        /// Adds a kill objective to this quest state.
        /// </summary>
        /// <param name="group">The NPC group Id</param>
        /// <param name="amount">The number of NPCs in this group required to kill to complete the objective.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder AddKillObjective(NPCGroupType group, int amount);

        /// <summary>
        /// Adds a collect item objective to this quest state.
        /// </summary>
        /// <param name="resref">The resref of the required item.</param>
        /// <param name="amount">The number of items needed to complete the objective.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        IQuestBuilder AddCollectItemObjective(string resref, int amount);

        /// <summary>
        /// Builds all of the configured quests.
        /// </summary>
        /// <returns>A dictionary containing all of the new quests.</returns>
        Dictionary<string, QuestDetail> Build();
    }
}
