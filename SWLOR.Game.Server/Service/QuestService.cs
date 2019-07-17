using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Creature;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using static NWN._;
using Quest = SWLOR.Game.Server.Data.Entity.Quest;
using QuestType = SWLOR.Game.Server.Enumeration.QuestType;

namespace SWLOR.Game.Server.Service
{
    public static class QuestService
    {
        private static readonly Dictionary<string, IQuestRule> _questRules;
        private const string TempStoragePlaceableTag = "QUEST_BARREL";

        static QuestService()
        {
            _questRules = new Dictionary<string, IQuestRule>();
        }

        public static void SubscribeEvents()
        {
            // Creature Events
            MessageHub.Instance.Subscribe<OnCreatureDeath>(message => OnCreatureDeath());

            // Module Events
            MessageHub.Instance.Subscribe<OnModuleAcquireItem>(message => OnModuleItemAcquired());
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
        }

        /// <summary>
        /// Registers all of the custom quest rules into memory.
        /// </summary>
        private static void OnModuleLoad()
        {
            RegisterQuestRules();
        }

        /// <summary>
        /// Reflects over the codebase, looking for implementations of IQuestRule.
        /// This cache is later used when players complete quests and specific code needs to be run.
        /// </summary>
        private static void RegisterQuestRules()
        {
            // Use reflection to get all of QuestRule implementations.
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IQuestRule).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract).ToArray();
            foreach (var type in classes)
            {
                IQuestRule instance = Activator.CreateInstance(type) as IQuestRule;
                if (instance == null)
                {
                    throw new NullReferenceException("Unable to activate instance of type: " + type);
                }
                _questRules.Add(type.Name, instance);
            }
        }

        /// <summary>
        /// Retrieves a specific quest rule from the cache.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static IQuestRule GetQuestRule(string key)
        {
            if (!_questRules.ContainsKey(key))
            {
                throw new KeyNotFoundException("Quest rule '" + key + "' is not registered. Did you create a class for it?");
            }

            return _questRules[key];
        }

        /// <summary>
        /// Retrieves a quest from the cache by its ID.
        /// </summary>
        /// <param name="questID">The key of the quest to retrieve.</param>
        /// <returns>The quest with the provided quest ID.</returns>
        public static Quest GetQuestByID(int questID)
        {
            return DataService.Quest.GetByID(questID);
        }

        /// <summary>
        /// Creates an item temporarily and then returns its details. 
        /// </summary>
        /// <param name="resref">The resref of the item to create.</param>
        /// <param name="quantity">The number to create.</param>
        /// <returns></returns>
        public static ItemVO GetTempItemInformation(string resref, int quantity)
        {
            NWPlaceable tempStorage = (_.GetObjectByTag(TempStoragePlaceableTag));
            NWItem tempItem = (_.CreateItemOnObject(resref, tempStorage.Object));
            ItemVO model = new ItemVO
            {
                Name = tempItem.Name,
                Quantity = quantity,
                Resref = resref,
                Tag = tempItem.Tag,
                Description = tempItem.IdentifiedDescription
            };

            return model;
        }

        /// <summary>
        /// Completes a quest for a player.
        /// </summary>
        /// <param name="player">The player we're completing the quest for.</param>
        /// <param name="questOwner">The quest giver object.</param>
        /// <param name="questID">The ID number of the quest.</param>
        /// <param name="selectedItem">The item selected by the player.</param>
        public static void CompleteQuest(NWPlayer player, NWObject questOwner, int questID, ItemVO selectedItem)
        {
            if (!player.IsPlayer) return;

            Quest quest = DataService.Quest.GetByID(questID);
            PCQuestStatus pcState = DataService.PCQuestStatus.GetByPlayerAndQuestID(player.GlobalID, questID);

            // Get all quest states, order by the sequence, and then get the last one.
            QuestState finalState = DataService.QuestState.GetAllByQuestID(questID)
                .OrderBy(o => o.Sequence)
                .Last();

            if (finalState == null)
            {
                player.SendMessage("Could not find final state of quest. Please notify an admin this quest is bugged. (QuestID: " + questID + ")");
                return;
            }

            pcState.CurrentQuestStateID = finalState.ID;
            pcState.CompletionDate = DateTime.UtcNow;
            pcState.TimesCompleted++;

            if (selectedItem == null)
            {
                var rewardItems = DataService.QuestRewardItem.GetAllByQuestID(questID);
                foreach (QuestRewardItem reward in rewardItems)
                {
                    _.CreateItemOnObject(reward.Resref, player.Object, reward.Quantity);
                }
            }
            else
            {
                _.CreateItemOnObject(selectedItem.Resref, player.Object, selectedItem.Quantity);
            }

            if (quest.RewardGold > 0)
            {
                _.GiveGoldToCreature(player.Object, quest.RewardGold);
            }

            if (quest.RewardKeyItemID != null)
            {
                KeyItemService.GivePlayerKeyItem(player, (int)quest.RewardKeyItemID);
            }

            if (quest.RemoveStartKeyItemAfterCompletion && quest.StartKeyItemID != null)
            {
                KeyItemService.RemovePlayerKeyItem(player, (int)quest.StartKeyItemID);
            }

            if (!string.IsNullOrWhiteSpace(quest.MapNoteTag))
            {
                MapPinService.DeleteMapPin(player, "QST_MAP_NOTE_" + questID);
            }

            if (quest.RewardFame > 0)
            {
                PCRegionalFame fame = DataService.PCRegionalFame.GetByPlayerIDAndFameRegionIDOrDefault(player.GlobalID, quest.FameRegionID);
                DatabaseActionType action = DatabaseActionType.Update;

                if (fame == null)
                {
                    fame = new PCRegionalFame
                    {
                        PlayerID = player.GlobalID,
                        FameRegionID = quest.FameRegionID,
                        Amount = 0
                    };

                    action = DatabaseActionType.Insert;
                }

                fame.Amount += quest.RewardFame;
                DataService.SubmitDataChange(fame, action);
            }

            player.SendMessage("Quest '" + quest.Name + "' complete!");
            DataService.SubmitDataChange(pcState, DatabaseActionType.Update);
            _.RemoveJournalQuestEntry(quest.JournalTag, player, FALSE);

            if (!string.IsNullOrWhiteSpace(quest.OnCompleteRule) && questOwner != null)
            {
                var rule = GetQuestRule(quest.OnCompleteRule);

                string[] args = null;
                if (!string.IsNullOrWhiteSpace(quest.OnCompleteArgs))
                    args = quest.OnCompleteArgs.Split(',');
                rule.Run(player, questOwner, questID, args);
            }

            MessageHub.Instance.Publish(new OnQuestCompleted(player, questID));
        }

        /// <summary>
        /// When a quest item is picked up, it is marked as undroppable.
        /// </summary>
        private static void OnModuleItemAcquired()
        {
            NWPlayer oPC = (_.GetModuleItemAcquiredBy());
            NWItem oItem = (_.GetModuleItemAcquired());

            if (!oPC.IsPlayer) return;

            int questID = oItem.GetLocalInt("QUEST_ID");

            if (questID <= 0) return;
            oItem.IsCursed = true;
        }

        /// <summary>
        /// Rebuilds players' quests on module entry.
        /// Only incomplete quests will be loaded.
        /// </summary>
        private static void OnModuleEnter()
        {
            NWPlayer oPC = (_.GetEnteringObject());

            if (!oPC.IsPlayer) return;

            List<PCQuestStatus> pcQuests = DataService.PCQuestStatus.GetAllByPlayerID(oPC.GlobalID).Where(x => x.CompletionDate == null).ToList();

            foreach (PCQuestStatus pcQuest in pcQuests)
            {
                var quest = DataService.Quest.GetByID(pcQuest.QuestID);
                var state = DataService.QuestState.GetByID(pcQuest.CurrentQuestStateID);
                _.AddJournalQuestEntry(quest.JournalTag, state.JournalStateID, oPC.Object, FALSE);
            }
        }

        /// <summary>
        /// Returns true if player can accept a quest with the given ID.
        /// Returns false if player cannot accept the quest.
        /// </summary>
        /// <param name="oPC">The player to check</param>
        /// <param name="questID">The ID number of the quest.</param>
        /// <param name="sendMessage">If true, a message will be sent to player about why they can't accept the quest.</param>
        /// <returns>true if player can accept quest. false otherwise.</returns>
        public static bool CanAcceptQuest(NWPlayer oPC, int questID, bool sendMessage)
        {
            var quest = GetQuestByID(questID);
            return CanAcceptQuest(oPC, quest, sendMessage);
        }

        /// <summary>
        /// Returns true if player can accept a quest with the given ID.
        /// Returns false if player cannot accept the quest.
        /// </summary>
        /// <param name="oPC">The player to check</param>
        /// <param name="quest">The quest to check.</param>
        /// <param name="sendMessage">If true, a message will be sent to player about why they can't accept the quest.</param>
        /// <returns>true if player can accept quest. false otherwise.</returns>
        public static bool CanAcceptQuest(NWPlayer oPC, Quest quest, bool sendMessage)
        {
            // Retrieve the player's current quest status for this quest.
            // If they haven't accepted it yet, this will be null.
            PCQuestStatus status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(oPC.GlobalID, quest.ID);

            // If the status is null, it's assumed that the player hasn't accepted it yet.
            if (status != null)
            {
                // If the quest isn't repeatable, prevent the player from accepting it after it's already been completed.
                if (status.CompletionDate != null)
                {
                    // If it's repeatable, then we don't care if they've already completed it.
                    if (!quest.IsRepeatable)
                    {
                        if (sendMessage)
                            oPC.SendMessage("You have already completed this quest.");
                        return false;
                    }
                }
                // If the player already accepted the quest, prevent them from accepting it again.
                else
                {
                    if (sendMessage)
                        oPC.SendMessage("You have already accepted this quest.");
                    return false;
                }
            }

            // Check whether the player meets all necessary prerequisites.
            if (!DoesPlayerMeetPrerequisites(oPC, quest.ID))
            {
                if (sendMessage)
                    oPC.SendMessage("You do not meet the prerequisites necessary to accept this quest.");
                return false;
            }

            // Retrieve the first state of the quest.
            var questState = DataService.QuestState.GetAllByQuestID(quest.ID).OrderBy(o => o.Sequence).First();

            // If this quest requires key items, ensure player has acquired them.
            if (!DoesPlayerHaveRequiredKeyItems(oPC, questState.ID))
            {
                if (sendMessage)
                    oPC.SendMessage("You do not have the required key items to accept this quest.");
                return false;
            }

            // Retrieve the player's fame information. Treat a missing record as having 0 fame for this region.
            PCRegionalFame fame = DataService.PCRegionalFame.GetByPlayerIDAndFameRegionIDOrDefault(oPC.GlobalID, quest.FameRegionID);
            int fameAmount = fame == null ? 0 : fame.Amount;

            // Ensure player has necessary fame for accepting this quest.
            if (fameAmount < quest.RequiredFameAmount)
            {
                if (sendMessage)
                    oPC.SendMessage("You do not have enough fame to accept this quest.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Accepts a quest for a player. This updates their journal entry and marks all necessary flags
        /// on the player.
        /// </summary>
        /// <param name="player">The player who is accepting the quest.</param>
        /// <param name="questOwner">The quest giver object.</param>
        /// <param name="questID">The ID number of the quest to accept.</param>
        public static void AcceptQuest(NWPlayer player, NWObject questOwner, int questID)
        {
            if (!player.IsPlayer) return;

            // Retrieve quest from the cache.
            Quest quest = DataService.Quest.GetByID(questID);

            // Check whether player can accept the quest. Send a message if they can't.
            if (!CanAcceptQuest(player, quest, true))
            {
                return;
            }

            // By this point, it's assumed the player will accept the quest.
            // However, if this quest is repeatable we must first update the existing entry.
            var status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);
            bool foundExisting = status != null;

            // Didn't find an existing state so we'll create a new object.
            if (status == null)
            {
                status = new PCQuestStatus();
            }
            else
            {
                status.CompletionDate = null;
            }

            // Retrieve the first quest state for this quest.
            var questState = DataService.QuestState.GetByQuestIDAndSequence(questID, 1);
            status.CurrentQuestStateID = questState.ID;
            
            // Give temporary key item at start of quest.
            if (quest.StartKeyItemID != null)
            {
                KeyItemService.GivePlayerKeyItem(player, (int)quest.StartKeyItemID);
            }

            // Add a map pin if specified by the quest.
            if (!string.IsNullOrWhiteSpace(quest.MapNoteTag))
            {
                MapPinService.AddWaypointMapPin(player, quest.MapNoteTag, quest.Name, "QST_MAP_NOTE_" + questID);
            }
            
            status.QuestID = quest.ID;
            status.PlayerID = player.GlobalID;

            // Insert or update player's quest status.
            DataService.SubmitDataChange(status, foundExisting ? DatabaseActionType.Update : DatabaseActionType.Insert);

            // Create extended quest entries, if necessary.
            CreateExtendedQuestDataEntries(status);

            // Add the journal entry to the player.
            _.AddJournalQuestEntry(quest.JournalTag, 1, player.Object, FALSE);

            // Notify them that they've accepted a quest.
            player.SendMessage("Quest '" + quest.Name + "' accepted. Refer to your journal for more information on this quest.");

            // If this quest runs any custom rules, do those now.
            if (!string.IsNullOrWhiteSpace(quest.OnAcceptRule) && questOwner != null)
            {
                var rule = GetQuestRule(quest.OnAcceptRule);

                string[] args = null;
                if (!string.IsNullOrWhiteSpace(quest.OnAcceptArgs))
                    args = quest.OnAcceptArgs.Split(',');
                rule.Run(player, questOwner, questID, args);
            }

            // Notify to subscribers that a quest has just been accepted.
            MessageHub.Instance.Publish(new OnQuestAccepted(player, questID));
        }

        /// <summary>
        /// Progresses a player to the next state of a quest.
        /// If they're on the last state of the quest, it will be completed for them.
        /// </summary>
        /// <param name="player">The player to advance.</param>
        /// <param name="questOwner">The quest giver object.</param>
        /// <param name="questID">The ID number of the quest.</param>
        public static void AdvanceQuestState(NWPlayer player, NWObject questOwner, int questID)
        {
            if (!player.IsPlayer) return;

            // Retrieve the player's current quest state.
            PCQuestStatus questStatus = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);

            // Can't find a state? Notify the player they haven't accepted the quest.
            if (questStatus == null)
            {
                player.SendMessage("You have not accepted this quest yet.");
                return;
            }

            // If this quest has already been completed, exit early.
            // This is used in case a module builder incorrectly configures a quest.
            // We don't want to risk giving duplicate rewards.
            if (questStatus.CompletionDate != null) return;

            // Retrieve the quest, the current state, and the next state of the quest from the cache.
            Quest quest = DataService.Quest.GetByID(questStatus.QuestID);
            QuestState currentState = DataService.QuestState.GetByID(questStatus.CurrentQuestStateID);
            QuestState nextState = DataService.QuestState.GetByQuestIDAndSequenceOrDefault(quest.ID, currentState.Sequence + 1);

            // If there's no state after this one, the assumption is that it's time to complete the quest.
            if (nextState == null) 
            {
                // We'll request a reward selection from the player if the quest is configured that way.
                // Otherwise, this method will simply complete the quest outright.
                RequestRewardSelectionFromPC(player, questOwner, questID);
            }
            // We found another state to this quest. Let's advance their progress now.
            else
            {
                // Update the player's journal
                _.AddJournalQuestEntry(quest.JournalTag, nextState.JournalStateID, player, FALSE);

                // Progress player's quest status to the next state.
                questStatus.CurrentQuestStateID = nextState.ID;

                // Notify the player they've progressed.
                player.SendMessage("Objective for quest '" + quest.Name + "' complete! Check your journal for information on the next objective.");

                // Create any extended data entries for the next state of the quest.
                CreateExtendedQuestDataEntries(questStatus);

                // Submit all of these changes to the cache/DB.
                DataService.SubmitDataChange(questStatus, DatabaseActionType.Update);

                // If this quest has a custom rule configured, run that now.
                if (!string.IsNullOrWhiteSpace(quest.OnAdvanceRule) && questOwner != null)
                {
                    var rule = GetQuestRule(quest.OnAdvanceRule);

                    string[] args = null;
                    if (!string.IsNullOrWhiteSpace(quest.OnAdvanceArgs))
                        args = quest.OnAdvanceArgs.Split(',');
                    rule.Run(player, questOwner, questID, args);
                }

                // Notify subscribers we've advanced the player's quest status.
                MessageHub.Instance.Publish(new OnQuestAdvanced(player, questID, currentState.Sequence + 1));
            }
        }

        /// <summary>
        /// Quests which require items, kill targets, etc. all need to have their entries added for this player's
        /// quest progress. This method handles that.
        /// </summary>
        /// <param name="status"></param>
        private static void CreateExtendedQuestDataEntries(PCQuestStatus status)
        {
            // Retrieve the quest and state information from the cache.
            var quest = DataService.Quest.GetByID(status.QuestID);
            var state = DataService.QuestState.GetByID(status.CurrentQuestStateID);

            // Retrieve the kill targets and required items necessary for this quest state.
            var killTargets = DataService.QuestKillTarget.GetAllByQuestStateID(state.ID);
            var requiredItems = DataService.QuestRequiredItem.GetAllByQuestStateID(state.ID);

            // Create entries for the PC kill targets.
            foreach (var kt in killTargets)
            {
                PCQuestKillTargetProgress pcKT = new PCQuestKillTargetProgress
                {
                    RemainingToKill = kt.Quantity,
                    NPCGroupID = kt.NPCGroupID,
                    PCQuestStatusID = status.ID,
                    PlayerID = status.PlayerID
                };
                DataService.SubmitDataChange(pcKT, DatabaseActionType.Insert);
            }

            // Create entries for PC items required.
            foreach (var item in requiredItems)
            {
                PCQuestItemProgress itemProgress = new PCQuestItemProgress
                {
                    Resref = item.Resref,
                    PlayerID = status.PlayerID,
                    PCQuestStatusID = status.ID,
                    Remaining = item.Quantity,
                    MustBeCraftedByPlayer = item.MustBeCraftedByPlayer
                };
                DataService.SubmitDataChange(itemProgress, DatabaseActionType.Insert);
            }

            // Submit changes to the cache/DB.
            DataService.SubmitDataChange(status, DatabaseActionType.Update);
        }

        /// <summary>
        /// If a quest requires the player to select a reward, a conversation will be opened for the player.
        /// Otherwise, the quest will be completed as normal.
        /// </summary>
        /// <param name="oPC">The player we're requesting a reward from.</param>
        /// <param name="questOwner">The quest giver object.</param>
        /// <param name="questID">The ID number of the quest</param>
        private static void RequestRewardSelectionFromPC(NWPlayer oPC, NWObject questOwner, int questID)
        {
            if (!oPC.IsPlayer) return;

            Quest quest = DataService.Quest.GetByID(questID);

            if (quest.AllowRewardSelection)
            {
                oPC.SetLocalInt("QST_REWARD_SELECTION_QUEST_ID", questID);
                DialogService.StartConversation(oPC, oPC, "QuestRewardSelection");
            }
            else
            {
                CompleteQuest(oPC, questOwner, questID, null);
            }

        }

        /// <summary>
        /// Check whether a player meets the necessary prerequisites to accept a quest, based on ID.
        /// </summary>
        /// <param name="oPC">The player to check</param>
        /// <param name="questID">The ID number of the quest</param>
        /// <returns>true if the player can accept the quest. false otherwise.</returns>
        private static bool DoesPlayerMeetPrerequisites(NWPlayer oPC, int questID)
        {
            var prereqs = DataService.QuestPrerequisite.GetAllByQuestID(questID).ToList();

            if (!oPC.IsPlayer) return false;
            if (prereqs.Count <= 0) return true;

            List<int> completedQuestIDs = DataService.PCQuestStatus.GetAllByPlayerID(oPC.GlobalID).Where(x => x.CompletionDate != null)
                .Select(s => s.QuestID).ToList();

            List<int> prereqIDs = new List<int>();
            foreach (QuestPrerequisite prereq in prereqs)
            {
                prereqIDs.Add(prereq.RequiredQuestID);
            }

            return completedQuestIDs.ContainsAll(prereqIDs);
        }

        /// <summary>
        /// Checks whether player has required key items to complete the quest.
        /// </summary>
        /// <param name="oPC">The player to check</param>
        /// <param name="questStateID">The ID number of the quest state to check.</param>
        /// <returns>true if the player has required key items. false otherwise.</returns>
        private static bool DoesPlayerHaveRequiredKeyItems(NWPlayer oPC, int questStateID)
        {
            var requiredKeyItems = DataService.QuestRequiredKeyItem.GetAllByQuestStateID(questStateID).ToList();
            if (!oPC.IsPlayer) return false;
            if (requiredKeyItems.Count <= 0) return true;

            List<int> keyItemIDs = DataService.PCKeyItem.GetAllByPlayerID(oPC.GlobalID)
                .Select(s => s.KeyItemID).ToList();

            List<int> requiredKeyItemIDs = new List<int>();
            foreach (QuestRequiredKeyItem ki in requiredKeyItems)
            {
                requiredKeyItemIDs.Add(ki.KeyItemID);
            }

            return keyItemIDs.ContainsAll(requiredKeyItemIDs);
        }

        /// <summary>
        /// Spawns quest items on oChest if the player is on the correct state of the quest and
        /// the container grants it.
        /// </summary>
        /// <param name="oChest">The chest we're spawning items into.</param>
        /// <param name="oPC">The player object.</param>
        public static void SpawnQuestItems(NWPlaceable oChest, NWPlayer oPC)
        {
            int questID = oChest.GetLocalInt("QUEST_ID");
            int questStateSequence = oChest.GetLocalInt("QUEST_SEQUENCE");
            string questItemResref = oChest.GetLocalString("QUEST_ITEM_RESREF");

            if (questID <= 0 || questStateSequence <= 0 || string.IsNullOrWhiteSpace(questItemResref)) return;

            PCQuestStatus status = DataService.PCQuestStatus.GetByPlayerAndQuestID(oPC.GlobalID, questID);
            QuestState questState = DataService.QuestState.GetByID(status.CurrentQuestStateID);

            if (questStateSequence != questState.Sequence) return;
            if (_.GetIsObjectValid(_.GetItemPossessedBy(oPC.Object, questItemResref)) == FALSE) return;

            // PC is on the correct quest, correct state, the chest creates quest items, and the PC does not already have the quest item.
            // Spawn it.

            _.CreateItemOnObject(questItemResref, oChest.Object);

        }

        /// <summary>
        /// Updates a player's quest status if the creature is part of an ongoing quest.
        /// Progresses the player to the next state if all requirements are met.
        /// </summary>
        private static void OnCreatureDeath()
        {
            NWCreature creature = NWGameObject.OBJECT_SELF;

            int npcGroupID = creature.GetLocalInt("NPC_GROUP");
            if (npcGroupID <= 0) return;

            NWObject oKiller = _.GetLastKiller();

            if (!oKiller.IsPlayer) return;

            string areaResref = creature.Area.Resref;

            List<KeyValuePair<NWPlayer, int>> playersToAdvance = new List<KeyValuePair<NWPlayer, int>>();
            NWPlayer oPC = _.GetFirstFactionMember(oKiller);
            while (oPC.IsValid)
            {
                if (areaResref != oPC.Area.Resref)
                {
                    oPC = _.GetNextFactionMember(oKiller);
                    continue;
                }

                if (_.GetDistanceBetween(creature, oPC) <= 0.0f || _.GetDistanceBetween(creature, oPC) > 20.0f)
                {
                    oPC = _.GetNextFactionMember(oKiller);
                    continue;
                }

                var playerID = oPC.GlobalID;
                var killTargets = DataService.PCQuestKillTargetProgress.GetAllByPlayerIDAndNPCGroupID(playerID, npcGroupID).ToList();

                foreach (var kt in killTargets)
                {
                    var questStatus = DataService.PCQuestStatus.GetByID(kt.PCQuestStatusID);
                    var quest = DataService.Quest.GetByID(questStatus.QuestID);
                    var npcGroup = DataService.NPCGroup.GetByID(kt.NPCGroupID);

                    kt.RemainingToKill--;
                    string targetGroupName = npcGroup.Name;
                    string updateMessage = "[" + quest.Name + "] " + targetGroupName + " remaining: " + kt.RemainingToKill;
                    DatabaseActionType action = DatabaseActionType.Update;

                    if (kt.RemainingToKill <= 0)
                    {
                        updateMessage += " " + ColorTokenService.Green(" {COMPLETE}");
                        playersToAdvance.Add(new KeyValuePair<NWPlayer, int>(oPC, quest.ID));
                        action = DatabaseActionType.Delete;
                    }

                    DataService.SubmitDataChange(kt, action);

                    var pc = oPC;
                    _.DelayCommand(1.0f, () =>
                    {
                        pc.SendMessage(updateMessage);
                    });

                    string ruleName = quest.OnKillTargetRule;
                    if (!string.IsNullOrWhiteSpace(ruleName))
                    {
                        var pcCopy = oPC;
                        var rule = GetQuestRule(ruleName);

                        string[] args = null;
                        if (!string.IsNullOrWhiteSpace(quest.OnKillTargetArgs))
                            args = quest.OnKillTargetArgs.Split(',');
                        rule.Run(pcCopy, creature, quest.ID, args);
                    }

                }

                oPC = _.GetNextFactionMember(oKiller);
            }

            foreach (var player in playersToAdvance)
            {
                AdvanceQuestState(player.Key, null, player.Value);
            }
        }

        /// <summary>
        /// Progresses a player to the next state if they meet all requirements to do so.
        /// </summary>
        /// <param name="oPC">The player object</param>
        /// <param name="oObject">The trigger or placeable being used/entered.</param>
        private static void HandleTriggerAndPlaceableQuestLogic(NWPlayer oPC, NWObject oObject)
        {
            if (!oPC.IsPlayer) return;
            string questMessage = oObject.GetLocalString("QUEST_MESSAGE");
            int questID = oObject.GetLocalInt("QUEST_ID");
            int questSequence = oObject.GetLocalInt("QUEST_SEQUENCE");
            string visibilityObjectID = oObject.GetLocalString("VISIBILITY_OBJECT_ID");

            if (questID <= 0)
            {
                oPC.SendMessage("QUEST_ID variable not set on object. Please inform admin this quest is bugged. (QuestID: " + questID + ")");
                return;
            }

            if (questSequence <= 0)
            {
                oPC.SendMessage("QUEST_SEQUENCE variable not set on object. Please inform admin this quest is bugged. (QuestID: " + questID + ")");
                return;
            }

            PCQuestStatus pcQuestStatus = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(oPC.GlobalID, questID);
            if (pcQuestStatus == null) return;

            QuestState questState = DataService.QuestState.GetByID(pcQuestStatus.CurrentQuestStateID);

            if (questState.Sequence != questSequence ||
              (questState.QuestTypeID != (int)QuestType.UseObject &&
                      questState.QuestTypeID != (int)QuestType.ExploreArea))
            {
                return;
            }


            if (!string.IsNullOrWhiteSpace(questMessage))
            {
                _.DelayCommand(1.0f, () =>
                {
                    oPC.SendMessage(questMessage);
                });
            }

            AdvanceQuestState(oPC, oObject, questID);

            if (!string.IsNullOrWhiteSpace(visibilityObjectID))
            {
                ObjectVisibilityService.AdjustVisibility(oPC, oObject, false);
            }
        }

        /// <summary>
        /// Handles updating quest state when a placeable is used by the player and
        /// the player is on a matching quest.
        /// </summary>
        /// <param name="oObject"></param>
        public static void OnQuestPlaceableUsed(NWObject oObject)
        {
            NWPlayer oPC = (_.GetLastUsedBy());
            HandleTriggerAndPlaceableQuestLogic(oPC, oObject);
        }

        /// <summary>
        /// Handles updating quest state when a trigger is entered by a player and
        /// the player is on a matching quest.
        /// </summary>
        /// <param name="oObject"></param>
        public static void OnQuestTriggerEntered(NWObject oObject)
        {
            NWPlayer oPC = (_.GetEnteringObject());
            HandleTriggerAndPlaceableQuestLogic(oPC, oObject);
        }

        /// <summary>
        /// Spawns a container and forces a player to open it. They are then instructed to insert any
        /// quest items inside.
        /// </summary>
        /// <param name="oPC">The player we're requesting items from.</param>
        /// <param name="questOwner">The quest giver object</param>
        /// <param name="questID">The ID number of the quest</param>
        public static void RequestItemsFromPC(NWPlayer oPC, NWObject questOwner, int questID)
        {
            if (!oPC.IsPlayer) return;

            PCQuestStatus pcStatus = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(oPC.GlobalID, questID);

            if (pcStatus == null)
            {
                oPC.SendMessage("You have not accepted this quest yet.");
                return;
            }

            QuestState questState = DataService.QuestState.GetByID(pcStatus.CurrentQuestStateID);
            var requiredKeyItems = DataService.QuestRequiredKeyItem.GetAllByQuestStateID(pcStatus.CurrentQuestStateID);

            foreach (QuestRequiredKeyItem ki in requiredKeyItems)
            {
                if (!KeyItemService.PlayerHasKeyItem(oPC, ki.KeyItemID))
                {
                    oPC.SendMessage("You are missing a required key item.");
                    return;
                }
            }

            if (questState.QuestTypeID != (int)QuestType.CollectItems)
            {
                oPC.SendMessage("The quest state you are currently on is not configured to collect items. Please inform an admin of this issue. (QuestID: " + questID + ")");
                return;
            }

            Location location = oPC.Location;
            NWPlaceable collector = (_.CreateObject(OBJECT_TYPE_PLACEABLE, "qst_item_collect", location));
            collector.SetLocalObject("QUEST_OWNER", questOwner);

            collector.AssignCommand(() =>
            {
                _.SetFacingPoint(oPC.Position);
            });
            collector.SetLocalInt("QUEST_ID", questID);

            oPC.AssignCommand(() =>
            {
                _.ActionInteractObject(collector.Object);
            });
        }

        /// <summary>
        /// Checks whether player has completed a given quest, by ID.
        /// </summary>
        /// <param name="oPC">The player to check</param>
        /// <param name="questID">The ID number of the quest.</param>
        /// <returns>true if player has completed quest. false otherwise.</returns>
        public static bool HasPlayerCompletedQuest(NWObject oPC, int questID)
        {
            PCQuestStatus status = DataService.PCQuestStatus.GetByPlayerAndQuestID(oPC.GlobalID, questID);

            return status?.CompletionDate != null;
        }

        /// <summary>
        /// Get the journal state ID number for the current quest state the player is on.
        /// </summary>
        /// <param name="oPC">The player object</param>
        /// <param name="questID">The ID number of the quest</param>
        /// <returns>The ID number of the journal entry</returns>
        public static int GetPlayerQuestJournalID(NWObject oPC, int questID)
        {
            PCQuestStatus status = DataService.PCQuestStatus.GetByPlayerAndQuestID(oPC.GlobalID, questID);

            if (status == null) return -1;
            var state = DataService.QuestState.GetByID(status.CurrentQuestStateID);
            return state.JournalStateID;
        }

        /// <summary>
        /// Determines whether a player has finished all requirements for a quest.
        /// Requirements:
        ///     - Player must be on the last quest state
        ///     - Player must have no remaining kill targets
        ///     - Player must have no remaining required items
        ///     - Player must have no remaining required key items
        /// </summary>
        /// <param name="player">The player we're checking</param>
        /// <param name="questID">The ID number of the quest.</param>
        /// <returns>true if player can complete a quest, false otherwise.</returns>
        public static bool CanPlayerCompleteQuest(NWPlayer player, int questID)
        {
            // Has the player even accepted this quest?
            var pcStatus = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);
            if (pcStatus == null) return false;

            // Is the player on the final state of this quest?
            var finalState = DataService.QuestState.GetAllByQuestID(questID) 
                .OrderBy(o => o.Sequence)
                .Last();
            if (pcStatus.CurrentQuestStateID != finalState.ID) return false;

            // Are there any remaining kill targets for this quest and player?
            var killTargetCount = DataService.PCQuestKillTargetProgress.GetAllByPlayerIDAndPCQuestStatusID(player.GlobalID, pcStatus.ID).Count();
            if (killTargetCount > 0) return false;

            // Are there any remaining item requirements?
            var itemCount = DataService.PCQuestItemProgress.GetCountByPCQuestStatusID(pcStatus.ID);
            if (itemCount > 0) return false;

            // Are there any remaining key item requirements?
            var requiredKeyItems = DataService.QuestRequiredKeyItem.GetAllByQuestStateID(finalState.ID)
                .Select(s => s.KeyItemID).ToArray();
            bool hasAllKeyItems = KeyItemService.PlayerHasAllKeyItems(player, requiredKeyItems);
            if (!hasAllKeyItems) return false;

            // Met all requirements. We can complete this quest.
            return true;
        }

    }
}
