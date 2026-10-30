using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the quest registry. The logic lives in <see cref="IQuestService"/>; every member here forwards to
    /// the container's instance, and the event handlers stay here so their names in the handler order manifest do not
    /// change. The quest encounters are the extra facade <see cref="QuestEncounter"/>.
    /// </summary>
    public static class Quest
    {
        private static IQuestService Instance => FacadeAccessor.Resolve<IQuestService>();

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// When the module loads, all quests will be retrieved with reflection and stored into a cache.
        /// </summary>
        public static void RegisterQuests()
        {
            Instance.RegisterQuests();
        }

        /// <summary>
        /// Retrieves all quests associated with a guild.
        /// </summary>
        /// <param name="guild">The guild to search for</param>
        /// <param name="rank">The rank to search for</param>
        /// <returns>A list of quests associated with the guild.</returns>
        public static List<QuestDetail> GetQuestsByGuild(GuildType guild, int rank)
        {
            return Instance.GetQuestsByGuild(guild, rank);
        }

        /// <summary>
        /// When a player enters the module, load their quests.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void LoadPlayerQuests()
        {
            Instance.LoadPlayerQuests();
        }

        /// <summary>
        /// Retrieves a quest by its Id. If the quest has not been registered, a KeyNotFoundException will be thrown.
        /// </summary>
        /// <param name="questId">The quest Id to search for.</param>
        /// <returns>The quest detail matching this Id.</returns>
        public static QuestDetail GetQuestById(string questId)
        {
            return Instance.GetQuestById(questId);
        }

        /// <summary>
        /// Retrieves a quest by its Id, or null if it has not been registered.
        /// Use this instead of <see cref="GetQuestById"/> when a missing quest is an expected, recoverable condition.
        /// </summary>
        /// <param name="questId">The quest Id to search for.</param>
        /// <returns>The quest detail matching this Id, or null if it isn't registered.</returns>
        public static QuestDetail GetQuestByIdOrDefault(string questId)
        {
            return Instance.GetQuestByIdOrDefault(questId);
        }

        /// <summary>
        /// Registers (or replaces) a quest at runtime, outside of the reflection-based <see cref="RegisterQuests"/> pass.
        /// Used by systems which build quests dynamically, such as player-authored quest contracts.
        /// </summary>
        /// <param name="quest">The quest to register.</param>
        public static void RegisterRuntimeQuest(QuestDetail quest)
        {
            Instance.RegisterRuntimeQuest(quest);
        }

        /// <summary>
        /// Removes a runtime-registered quest so it can no longer be accepted, advanced, or completed.
        /// Players who have already accepted the quest are unaffected until <see cref="LoadPlayerQuests"/> hardens their journal.
        /// </summary>
        /// <param name="questId">The Id of the quest to remove.</param>
        public static void UnregisterRuntimeQuest(string questId)
        {
            Instance.UnregisterRuntimeQuest(questId);
        }

        /// <summary>
        /// Retrieves the quests associated with an NPC group.
        /// If no quests are associated with this NPC group, an empty list will be returned.
        /// </summary>
        /// <param name="npcGroupType">The NPC group to search for</param>
        /// <returns>A list of quests associated with an NPC group.</returns>
        public static List<string> GetQuestsAssociatedWithNPCGroup(NPCGroupType npcGroupType)
        {
            return Instance.GetQuestsAssociatedWithNPCGroup(npcGroupType);
        }

        /// <summary>Credits only the reporting player's active, matching combat objectives.</summary>
        public static void RecordCombatObjective(uint player, string key, uint source)
        {
            Instance.RecordCombatObjective(player, key, source);
        }

        public static void AbandonQuest(uint player, string questId)
        {
            Instance.AbandonQuest(player, questId);
        }

        /// <summary>
        /// Marks a quest as completed for a player without granting rewards or running completion
        /// actions. Used by DM tooling to open quest-gated content such as capstone perk unlocks.
        /// If the quest Id is invalid, an exception will be thrown.
        /// </summary>
        /// <param name="player">The player whose quest record is marked complete.</param>
        /// <param name="questId">The Id of the quest to mark complete.</param>
        public static void ForceCompleteQuest(uint player, string questId)
        {
            Instance.ForceCompleteQuest(player, questId);
        }

        /// <summary>
        /// Makes a player accept a quest by the specified Id.
        /// If the quest Id is invalid, an exception will be thrown.
        /// </summary>
        /// <param name="player">The player who is accepting the quest</param>
        /// <param name="questId">The Id of the quest to accept.</param>
        public static bool AcceptQuest(uint player, string questId)
        {
            return Instance.AcceptQuest(player, questId);
        }

        /// <summary>
        /// Makes a player accept a quest while preserving the creature or placeable that offered it.
        /// Conversation callbacks attached to the quest run against this source.
        /// </summary>
        public static bool AcceptQuest(uint player, uint questSource, string questId)
        {
            return Instance.AcceptQuest(player, questSource, questId);
        }

        public static bool CanAcceptQuest(uint player, string questId)
        {
            return Instance.CanAcceptQuest(player, questId);
        }

        /// <summary>
        /// Makes a player advance to the next state of the quest.
        /// If there are no additional states, the quest will be treated as completed.
        /// </summary>
        /// <param name="player">The player who is advancing to the next state of the quest.</param>
        /// <param name="questSource">The source of the quest. Typically an NPC or object.</param>
        /// <param name="questId">The Id of the quest to advance.</param>
        public static bool AdvanceQuest(uint player, uint questSource, string questId)
        {
            return Instance.AdvanceQuest(player, questSource, questId);
        }

        /// <summary>
        /// Forces a player to open a collection placeable in which they will put items needed for the quest.
        /// </summary>
        /// <param name="player">The player who will open the collection placeable.</param>
        /// <param name="questId">The quest to collect items for.</param>
        public static bool RequestItemsFromPlayer(uint player, string questId)
        {
            return Instance.RequestItemsFromPlayer(player, questId);
        }

        /// <summary>
        /// Opens the quest item collector and records the conversation object that owns the hand-in.
        /// </summary>
        public static bool RequestItemsFromPlayer(uint player, uint questSource, string questId)
        {
            return Instance.RequestItemsFromPlayer(player, questSource, questId);
        }

        /// <summary>
        /// When an NPC is killed, any objectives for quests a player currently has active will be updated.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void ProgressKillTargetObjectives()
        {
            Instance.ProgressKillTargetObjectives();
        }

        /// <summary>
        /// When an item collector placeable is opened,
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestCollectOpen)]
        public static void OpenItemCollector()
        {
            Instance.OpenItemCollector();
        }

        /// <summary>
        /// When an item collector placeable is closed, clear its inventory and destroy it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestCollectClosed)]
        public static void CloseItemCollector()
        {
            Instance.CloseItemCollector();
        }

        /// <summary>
        /// When an item collector placeable is disturbed,
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestCollectDisturbed)]
        public static void DisturbItemCollector()
        {
            Instance.DisturbItemCollector();
        }

        /// <summary>
        /// When a player uses a quest placeable, handle the progression.
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestPlaceable)]
        public static void UseQuestPlaceable()
        {
            Instance.UseQuestPlaceable();
        }

        /// <summary>
        /// When a player enters a quest trigger, handle the progression.
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestTrigger)]
        public static void EnterQuestTrigger()
        {
            Instance.EnterQuestTrigger();
        }

        /// <summary>
        /// Handles advancing a player's quest when they enter a trigger or click a quest placeable.
        /// Trigger or placeable must have both QUEST_ID (string) and QUEST_STATE (int) set in order for this to work, otherwise an error will be raised.
        /// </summary>
        /// <param name="player">The player who entered the trigger or clicked a placeable.</param>
        /// <param name="triggerOrPlaceable">The trigger or placeable</param>
        public static void TriggerAndPlaceableProgression(uint player, uint triggerOrPlaceable)
        {
            Instance.TriggerAndPlaceableProgression(player, triggerOrPlaceable);
        }

        public static int CalculateQuestGoldReward(uint player, bool isGuildQuest, int baseAmount)
        {
            return Instance.CalculateQuestGoldReward(player, isGuildQuest, baseAmount);
        }
    }
}
