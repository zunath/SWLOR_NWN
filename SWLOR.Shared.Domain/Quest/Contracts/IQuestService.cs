using SWLOR.Shared.Domain.Quest.Enums;

namespace SWLOR.Shared.Domain.Quest.Contracts
{
    public interface IQuestService
    {
        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        void CacheData();

        /// <summary>
        /// When the module loads, all quests will be retrieved with reflection and stored into a cache.
        /// </summary>
        void RegisterQuests();

        /// <summary>
        /// Retrieves all quests associated with a guild.
        /// </summary>
        /// <param name="guild">The guild to search for</param>
        /// <param name="rank">The rank to search for</param>
        /// <returns>A list of quests associated with the guild.</returns>
        List<IQuestDetail> GetQuestsByGuild(GuildType guild, int rank);

        /// <summary>
        /// When a player enters the module, load their quests.
        /// </summary>
        void LoadPlayerQuests();

        /// <summary>
        /// Retrieves a quest by its Id. If the quest has not been registered, a KeyNotFoundException will be thrown.
        /// </summary>
        /// <param name="questId">The quest Id to search for.</param>
        /// <returns>The quest detail matching this Id.</returns>
        IQuestDetail GetQuestById(string questId);

        /// <summary>
        /// Retrieves the quests associated with an NPC group.
        /// If no quests are associated with this NPC group, an empty list will be returned.
        /// </summary>
        /// <param name="npcGroupType">The NPC group to search for</param>
        /// <returns>A list of quests associated with an NPC group.</returns>
        List<string> GetQuestsAssociatedWithNPCGroup(NPCGroupType npcGroupType);

        void AbandonQuest(uint player, string questId);

        /// <summary>
        /// Makes a player accept a quest by the specified Id.
        /// If the quest Id is invalid, an exception will be thrown.
        /// </summary>
        /// <param name="player">The player who is accepting the quest</param>
        /// <param name="questId">The Id of the quest to accept.</param>
        void AcceptQuest(uint player, string questId);

        /// <summary>
        /// Makes a player advance to the next state of the quest.
        /// If there are no additional states, the quest will be treated as completed.
        /// </summary>
        /// <param name="player">The player who is advancing to the next state of the quest.</param>
        /// <param name="questSource">The source of the quest. Typically an NPC or object.</param>
        /// <param name="questId">The Id of the quest to advance.</param>
        void AdvanceQuest(uint player, uint questSource, string questId);

        /// <summary>
        /// Forces a player to open a collection placeable in which they will put items needed for the quest.
        /// </summary>
        /// <param name="player">The player who will open the collection placeable.</param>
        /// <param name="questId">The quest to collect items for.</param>
        void RequestItemsFromPlayer(uint player, string questId);

        /// <summary>
        /// When an NPC is killed, any objectives for quests a player currently has active will be updated.
        /// </summary>
        void ProgressKillTargetObjectives();

        /// <summary>
        /// When an item collector placeable is opened, 
        /// </summary>
        void OpenItemCollector();

        /// <summary>
        /// When an item collector placeable is closed, clear its inventory and destroy it.
        /// </summary>
        void CloseItemCollector();

        /// <summary>
        /// When an item collector placeable is disturbed, 
        /// </summary>
        void DisturbItemCollector();

        /// <summary>
        /// When a player uses a quest placeable, handle the progression.
        /// </summary>
        void UseQuestPlaceable();

        /// <summary>
        /// When a player enters a quest trigger, handle the progression.
        /// </summary>
        void EnterQuestTrigger();

        /// <summary>
        /// Handles advancing a player's quest when they enter a trigger or click a quest placeable.
        /// Trigger or placeable must have both QUEST_ID (string) and QUEST_STATE (int) set in order for this to work, otherwise an error will be raised.
        /// </summary>
        /// <param name="player">The player who entered the trigger or clicked a placeable.</param>
        /// <param name="triggerOrPlaceable">The trigger or placeable</param>
        void TriggerAndPlaceableProgression(uint player, uint triggerOrPlaceable);

        int CalculateQuestGoldReward(uint player, bool isGuildQuest, int baseAmount);

        /// <summary>
        /// After quests are registered, refresh the available guild tasks.
        /// </summary>
        void RefreshGuildTasks();

        /// <summary>
        /// Retrieves quest details associated with the active guild tasks by rank.
        /// </summary>
        /// <param name="guild">The guild type to retrieve for</param>
        /// <param name="rank">The rank to retrieve for</param>
        /// <returns>A list of active guild tasks</returns>
        List<IQuestDetail> GetActiveGuildTasksByRank(GuildType guild, int rank);

        /// <summary>
        /// Retrieves quest details associated with the active guild tasks.
        /// </summary>
        /// <param name="guild">The guild type to retrieve for</param>
        /// <returns>A list of active guild tasks</returns>
        Dictionary<string, IQuestDetail> GetAllActiveGuildTasks(GuildType guild);

        /// <summary>
        /// Gets the date when guild tasks were last loaded.
        /// </summary>
        DateTime? DateTasksLoaded { get; }
    }
}