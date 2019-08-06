using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Quest.Objective;
using SWLOR.Game.Server.Quest.Prerequisite;
using SWLOR.Game.Server.Quest.Reward;
using SWLOR.Game.Server.Service;
using static NWN._;

namespace SWLOR.Game.Server.Quest
{
    public sealed class Quest: IQuest
    {
        private Dictionary<int, IQuestState> QuestStates { get; } = new Dictionary<int, IQuestState>();
        private List<IQuestReward> Rewards { get; } = new List<IQuestReward>();
        private List<IQuestPrerequisite> Prerequisites { get; } = new List<IQuestPrerequisite>();

        public int QuestID { get; }
        public string Name { get; }
        public string JournalTag { get; }
        private bool _repeatable;
        public bool AllowRewardSelection { get; private set; }

        private Action<NWPlayer, NWObject> _onAccept;
        private Action<NWPlayer, NWObject> _onAdvance;
        private Action<NWPlayer, NWObject> _onComplete;

        public Quest(int questID, string name, string journalTag)
        {
            QuestID = questID;
            Name = name;
            JournalTag = journalTag;
        }
        
        public IQuestState AddState()
        {
            int index = QuestStates.Count;
            QuestStates[index] = new QuestState();
            return QuestStates[index];
        }

        public IQuestState GetState(int state)
        {
            return GetStates().ElementAt(state - 1);
        }

        public IEnumerable<IQuestState> GetStates()
        {
            return QuestStates.OrderBy(o => o.Key).Select(x => x.Value);
        }

        public IEnumerable<IQuestReward> GetRewards()
        {
            return Rewards;
        }

        public bool CanAccept(NWPlayer player)
        {
            // Retrieve the player's current quest status for this quest.
            // If they haven't accepted it yet, this will be null.
            PCQuestStatus status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, QuestID);

            // If the status is null, it's assumed that the player hasn't accepted it yet.
            if (status != null)
            {
                // If the quest isn't repeatable, prevent the player from accepting it after it's already been completed.
                if (status.CompletionDate != null)
                {
                    // If it's repeatable, then we don't care if they've already completed it.
                    if (!_repeatable)
                    { 
                        player.SendMessage("You have already completed this quest.");
                        return false;
                    }
                }
                // If the player already accepted the quest, prevent them from accepting it again.
                else
                {
                    player.SendMessage("You have already accepted this quest.");
                    return false;
                }
            }

            // Check whether the player meets all necessary prerequisites.
            foreach (var prereq in Prerequisites)
            {
                if (!prereq.MeetsPrerequisite(player))
                {
                    player.SendMessage("You do not meet the prerequisites necessary to accept this quest.");
                    return false;
                }
            }

            return true;
        }

        public bool CanComplete(NWPlayer player)
        {
            // Has the player even accepted this quest?
            var pcStatus = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, QuestID);
            if (pcStatus == null) return false;

            // Is the player on the final state of this quest?
            if (pcStatus.QuestState != GetStates().Count()) return false;

            var state = GetState(pcStatus.QuestState);
            // Are all objectives complete?
            foreach (var objective in state.GetObjectives())
            {
                if (!objective.IsComplete(player, QuestID))
                    return false;
            }

            // Met all requirements. We can complete this quest.
            return true;
        }

        public bool IsComplete(NWPlayer player)
        {
            var pcStatus = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, QuestID);
            if (pcStatus == null) return false;
            
            int count = GetStates().Count();
            return pcStatus.QuestState == count;
        }

        public void Accept(NWPlayer player, NWObject questSource)
        {
            if (!player.IsPlayer) return;

            if (!CanAccept(player))
            {
                return;
            }
            
            // By this point, it's assumed the player will accept the quest.
            // However, if this quest is repeatable we must first update the existing entry.
            var status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, QuestID);
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
            status.QuestState = 1;
            status.QuestID = QuestID;
            status.PlayerID = player.GlobalID;

            // Insert or update player's quest status.
            DataService.SubmitDataChange(status, foundExisting ? DatabaseActionType.Update : DatabaseActionType.Insert);

            var state = GetState(1);
            foreach (var objective in state.GetObjectives())
            {
                objective.Initialize(player, QuestID);
            }

            // Add the journal entry to the player.
            AddJournalQuestEntry(JournalTag, 1, player.Object, FALSE);

            // Notify them that they've accepted a quest.
            player.SendMessage("Quest '" + Name + "' accepted. Refer to your journal for more information on this quest.");

            // Run any quest-specific code.
            _onAccept?.Invoke(player, questSource);

            // Notify to subscribers that a quest has just been accepted.
            MessageHub.Instance.Publish(new OnQuestAccepted(player, QuestID));
        }

        public void Advance(NWPlayer player, NWObject questSource)
        {
            if (!player.IsPlayer) return;
            
            // Retrieve the player's current quest state.
            PCQuestStatus questStatus = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, QuestID);

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

            var currentState = GetState(questStatus.QuestState);
            var lastState = GetStates().Last();

            // If this is the last state, the assumption is that it's time to complete the quest.
            if (currentState == lastState)
            {
                RequestRewardSelectionFromPC(player, questSource);
            }
            else
            {
                // Progress player's quest status to the next state.
                questStatus.QuestState++;
                var nextState = GetState(questStatus.QuestState);
                
                // Update the player's journal
                AddJournalQuestEntry(JournalTag, questStatus.QuestState, player, FALSE);

                // Notify the player they've progressed.
                player.SendMessage("Objective for quest '" + Name + "' complete! Check your journal for information on the next objective.");
                
                // Submit all of these changes to the cache/DB.
                DataService.SubmitDataChange(questStatus, DatabaseActionType.Update);

                // Create any extended data entries for the next state of the quest.
                foreach (var objective in nextState.GetObjectives())
                {
                    objective.Initialize(player, QuestID);
                }
                
                // Run any quest-specific code.
                _onAdvance?.Invoke(player, questSource);

                // Notify to subscribers that the player has advanced to the next state of the quest.
                MessageHub.Instance.Publish(new OnQuestAdvanced(player, QuestID, questStatus.QuestState));
            }


        }

        public void Complete(NWPlayer player, NWObject questSource, IQuestReward selectedReward)
        {
            if (!player.IsPlayer) return;
            if (!CanComplete(player)) return;

            PCQuestStatus pcState = DataService.PCQuestStatus.GetByPlayerAndQuestID(player.GlobalID, QuestID);

            // Mark player as being on the last state of the quest.
            pcState.QuestState = GetStates().Count();
            pcState.CompletionDate = DateTime.UtcNow;
            pcState.TimesCompleted++;
            
            if (selectedReward == null)
            {
                foreach (var reward in Rewards)
                {
                    reward.GiveReward(player);
                }
            }
            else
            {
                selectedReward.GiveReward(player);
            }

            DataService.SubmitDataChange(pcState, DatabaseActionType.Update);
            _onComplete?.Invoke(player, questSource);
            
            player.SendMessage("Quest '" + Name + "' complete!");
            RemoveJournalQuestEntry(JournalTag, player, FALSE);
            MessageHub.Instance.Publish(new OnQuestCompleted(player, QuestID));
        }

        private void RequestRewardSelectionFromPC(NWPlayer player, NWObject questGiver)
        {
            if (!player.IsPlayer) return;

            if (AllowRewardSelection)
            {
                player.SetLocalInt("QST_REWARD_SELECTION_QUEST_ID", QuestID);
                DialogService.StartConversation(player, player, "QuestRewardSelection");
            }
            else
            {
                Complete(player, questGiver, null);
            }
        }

        public void GiveRewards(NWPlayer player)
        {
            foreach (var reward in Rewards)
            {
                reward.GiveReward(player);
            }
        }

        public IQuest OnAccepted(Action<NWPlayer, NWObject> action)
        {
            _onAccept = action;
            return this;
        }

        public IQuest OnAdvanced(Action<NWPlayer, NWObject> action)
        {
            _onAdvance = action;
            return this;
        }

        public IQuest OnCompleted(Action<NWPlayer, NWObject> action)
        {
            _onComplete = action;
            return this;
        }

        public IQuest IsRepeatable()
        {
            _repeatable = true;
            return this;
        }

        public IQuest EnableRewardSelection()
        {
            AllowRewardSelection = true;
            return this;
        }

        public IQuest AddObjective(int state, IQuestObjective objective)
        {
            if (!QuestStates.ContainsKey(state - 1))
            {
                QuestStates[state-1] = new QuestState();
            }

            var questState = QuestStates[state - 1];
            questState.AddObjective(objective);

            return this;
        }

        public IQuest AddReward(IQuestReward reward)
        {
            Rewards.Add(reward);

            return this;
        }

        public IQuest AddPrerequisite(IQuestPrerequisite prerequisite)
        {
            Prerequisites.Add(prerequisite);
            return this;
        }

        // Convenience functions for commonly used objectives
        public IQuest AddObjectiveKillTarget(int state, NPCGroupType group, int amount)
        {
            AddObjective(state, new KillTargetObjective(group, amount));
            return this;
        }

        public IQuest AddObjectiveCollectItem(int state, string resref, int quantity, bool mustBeCraftedByPlayer)
        {
            AddObjective(state, new CollectItemObjective(resref, quantity, mustBeCraftedByPlayer));
            return this;
        }

        public IQuest AddObjectiveCollectKeyItem(int state, int keyItemID)
        {
            AddObjective(state, new CollectKeyItemObjective(keyItemID));
            return this;
        }

        public IQuest AddObjectiveTalkToNPC(int state)
        {
            AddObjective(state, new TalkToNPCObjective());
            return this;
        }

        public IQuest AddObjectiveEnterTrigger(int state)
        {
            AddObjective(state, new EnterTriggerObjective());
            return this;
        }

        public IQuest AddObjectiveUseObject(int state)
        {
            AddObjective(state, new UseObjectObjective());
            return this;
        }

        // Convenience functions for commonly used rewards
        public IQuest AddRewardGold(int amount)
        {
            AddReward(new QuestGoldReward(amount));
            return this;
        }

        public IQuest AddRewardItem(string resref, int quantity)
        {
            AddReward(new QuestItemReward(resref, quantity));
            return this;
        }

        public IQuest AddRewardKeyItem(int keyItemID)
        {
            AddReward(new QuestKeyItemReward(keyItemID));
            return this;
        }

        public IQuest AddRewardFame(int regionID, int amount)
        {
            AddReward(new QuestFameReward(regionID, amount));
            return this;
        }

        // Convenience functions for commonly used prerequisites

        public IQuest AddPrerequisiteFame(int regionID, int amount)
        {
            AddPrerequisite(new FamePrerequisite(regionID, amount));
            return this;
        }

        public IQuest AddPrerequisiteKeyItem(int keyItemID)
        {
            AddPrerequisite(new KeyItemPrerequisite(keyItemID));
            return this;
        }

        public IQuest AddPrerequisiteQuest(int questID)
        {
            AddPrerequisite(new RequiredQuestPrerequisite(questID));
            return this;
        }
    }
}
