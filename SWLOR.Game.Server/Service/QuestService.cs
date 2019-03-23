using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Messaging.Contracts;
using SWLOR.Game.Server.Messaging.Messages;
using static NWN._;
using Quest = SWLOR.Game.Server.Data.Entity.Quest;
using QuestType = SWLOR.Game.Server.Enumeration.QuestType;

namespace SWLOR.Game.Server.Service
{
    public class QuestService : IQuestService
    {
        private const string TempStoragePlaceableTag = "QUEST_BARREL";

        
        
        private readonly IKeyItemService _keyItem;
        private readonly IMapPinService _mapPin;
        private readonly IDialogService _dialog;
        private readonly IColorTokenService _color;
        private readonly IObjectVisibilityService _ovs;
        

        public QuestService(
            
            IKeyItemService keyItem,
            IMapPinService mapPin,
            IDialogService dialog,
            IColorTokenService color,
            IObjectVisibilityService ovs)
        {
            
            
            _keyItem = keyItem;
            _mapPin = mapPin;
            _dialog = dialog;
            _color = color;
            _ovs = ovs;
        }

        public Quest GetQuestByID(int questID)
        {
            return DataService.Single<Quest>(x => x.ID == questID);
        }

        public ItemVO GetTempItemInformation(string resref, int quantity)
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

        public void CompleteQuest(NWPlayer player, NWObject questOwner, int questID, ItemVO selectedItem)
        {
            if (!player.IsPlayer) return;

            Quest quest = DataService.Single<Quest>(x => x.ID == questID);
            PCQuestStatus pcState = DataService.Single<PCQuestStatus>(x => x.PlayerID == player.GlobalID && x.QuestID == questID);
            
            QuestState finalState = DataService.GetAll<QuestState>().Where(x => x.QuestID == questID).OrderBy(o => o.Sequence).Last();

            if (finalState == null)
            {
                player.SendMessage("Could not find final state of quest. Please notify an admin this quest is bugged. (QuestID: " + questID + ")");
                return;
            }

            pcState.CurrentQuestStateID = finalState.ID;
            pcState.CompletionDate = DateTime.UtcNow;

            if (selectedItem == null)
            {
                var rewardItems = DataService.Where<QuestRewardItem>(x => x.QuestID == questID);
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
                _keyItem.GivePlayerKeyItem(player, (int)quest.RewardKeyItemID);
            }

            if (quest.RemoveStartKeyItemAfterCompletion && quest.StartKeyItemID != null)
            {
                _keyItem.RemovePlayerKeyItem(player, (int)quest.StartKeyItemID);
            }

            if (!string.IsNullOrWhiteSpace(quest.MapNoteTag))
            {
                _mapPin.DeleteMapPin(player, "QST_MAP_NOTE_" + questID);
            }

            if (quest.RewardFame > 0)
            {
                PCRegionalFame fame = DataService.SingleOrDefault<PCRegionalFame>(x => x.PlayerID == player.GlobalID && x.FameRegionID == quest.FameRegionID);
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
                App.ResolveByInterface<IQuestRule>("QuestRule." + quest.OnCompleteRule, rule =>
                {
                    string[] args = null;
                    if (!string.IsNullOrWhiteSpace(quest.OnCompleteArgs))
                        args = quest.OnCompleteArgs.Split(',');
                    rule.Run(player, questOwner, questID, args);
                });
            }

            MessageHub.Instance.Publish(new QuestCompletedMessage(player, questID));
        }

        public void OnModuleItemAcquired()
        {
            NWPlayer oPC = (_.GetModuleItemAcquiredBy());
            NWItem oItem = (_.GetModuleItemAcquired());

            if (!oPC.IsPlayer) return;

            int questID = oItem.GetLocalInt("QUEST_ID");

            if (questID <= 0) return;
            oItem.IsCursed = true;
        }

        public void OnClientEnter()
        {
            NWPlayer oPC = (_.GetEnteringObject());

            if (!oPC.IsPlayer) return;

            List<PCQuestStatus> pcQuests = DataService.Where<PCQuestStatus>(x => x.PlayerID == oPC.GlobalID && x.CompletionDate == null).ToList();
            
            foreach (PCQuestStatus pcQuest in pcQuests)
            {
                var quest = DataService.Get<Quest>(pcQuest.QuestID);
                var state = DataService.Get<QuestState>(pcQuest.CurrentQuestStateID);
                _.AddJournalQuestEntry(quest.JournalTag, state.JournalStateID, oPC.Object, FALSE);
            }
        }

        public bool CanAcceptQuest(NWPlayer oPC, int questID, bool sendMessage)
        {
            var quest = GetQuestByID(questID);
            return CanAcceptQuest(oPC, quest, sendMessage);
        }

        public bool CanAcceptQuest(NWPlayer oPC, Quest quest, bool sendMessage)
        {
            PCQuestStatus status = DataService.SingleOrDefault<PCQuestStatus>(x => x.PlayerID == oPC.GlobalID && x.QuestID == quest.ID);

            if (status != null)
            {
                if (status.CompletionDate != null)
                {
                    if (sendMessage)
                        oPC.SendMessage("You have already completed this quest.");
                    return false;
                }
                else
                {
                    if (sendMessage)
                        oPC.SendMessage("You have already accepted this quest.");
                    return false;
                }
            }

            if (!DoesPlayerMeetPrerequisites(oPC, quest.ID))
            {
                if (sendMessage)
                    oPC.SendMessage("You do not meet the prerequisites necessary to accept this quest.");
                return false;
            }

            var questState = DataService.Where<QuestState>(x => x.QuestID == quest.ID).First();
            if (!DoesPlayerHaveRequiredKeyItems(oPC, questState.ID))
            {
                if (sendMessage)
                    oPC.SendMessage("You do not have the required key items to accept this quest.");
                return false;
            }

            PCRegionalFame fame = DataService.SingleOrDefault<PCRegionalFame>(x => x.PlayerID == oPC.GlobalID && x.FameRegionID == quest.FameRegionID);
            int fameAmount = fame == null ? 0 : fame.Amount;

            if (fameAmount < quest.RequiredFameAmount)
            {
                if (sendMessage)
                    oPC.SendMessage("You do not have enough fame to accept this quest.");
                return false;               
            }

            return true;
        }


        public void AcceptQuest(NWPlayer player, NWObject questOwner, int questID)
        {
            if (!player.IsPlayer) return;

            Quest quest = DataService.Single<Quest>(x => x.ID == questID);

            if (!CanAcceptQuest(player, quest, true))
            {
                return;
            }

            var questState = DataService.Single<QuestState>(x => x.QuestID == questID && x.Sequence == 1);
            var status = new PCQuestStatus
            {
                CurrentQuestStateID = questState.ID
            };

            // Give temporary key item at start of quest.
            if (quest.StartKeyItemID != null)
            {
                _keyItem.GivePlayerKeyItem(player, (int)quest.StartKeyItemID);
            }

            if (!string.IsNullOrWhiteSpace(quest.MapNoteTag))
            {
                _mapPin.AddWaypointMapPin(player, quest.MapNoteTag, quest.Name, "QST_MAP_NOTE_" + questID);
            }

            status.QuestID = quest.ID;
            status.PlayerID = player.GlobalID;
            DataService.SubmitDataChange(status, DatabaseActionType.Insert);
            CreateExtendedQuestDataEntries(status);

            _.AddJournalQuestEntry(quest.JournalTag, 1, player.Object, FALSE);
            player.SendMessage("Quest '" + quest.Name + "' accepted. Refer to your journal for more information on this quest.");


            if (!string.IsNullOrWhiteSpace(quest.OnAcceptRule) && questOwner != null)
            {
                App.ResolveByInterface<IQuestRule>("QuestRule." + quest.OnAcceptRule, rule =>
                {
                    string[] args = null;
                    if (!string.IsNullOrWhiteSpace(quest.OnAcceptArgs))
                        args = quest.OnAcceptArgs.Split(',');
                    rule.Run(player, questOwner, questID, args);
                });
            }
        }

        public void AdvanceQuestState(NWPlayer player, NWObject questOwner, int questID)
        {
            if (!player.IsPlayer) return;

            PCQuestStatus questStatus = DataService.SingleOrDefault<PCQuestStatus>(x => x.PlayerID == player.GlobalID && x.QuestID == questID);

            if (questStatus == null)
            {
                player.SendMessage("You have not accepted this quest yet.");
                return;
            }

            if (questStatus.CompletionDate != null) return;

            Quest quest = DataService.Get<Quest>(questStatus.QuestID);
            QuestState currentState = DataService.Get<QuestState>(questStatus.CurrentQuestStateID);
            QuestState nextState = DataService.SingleOrDefault<QuestState>(x => x.QuestID == quest.ID && x.Sequence == currentState.Sequence + 1);
            
            // Either complete the quest or move to the new state.
            if (nextState == null) // We assume this is the last state in the quest, so it must be time to complete it.
            {
                RequestRewardSelectionFromPC(player, questOwner, questID);
            }
            else
            {
                _.AddJournalQuestEntry(quest.JournalTag, nextState.JournalStateID, player, FALSE);
                questStatus.CurrentQuestStateID = nextState.ID;
                player.SendMessage("Objective for quest '" + quest.Name + "' complete! Check your journal for information on the next objective.");
                
                CreateExtendedQuestDataEntries(questStatus);
                DataService.SubmitDataChange(questStatus, DatabaseActionType.Update);
                
                if (!string.IsNullOrWhiteSpace(quest.OnAdvanceRule) && questOwner != null)
                {
                    App.ResolveByInterface<IQuestRule>("QuestRule." + quest.OnAdvanceRule, rule =>
                    {
                        string[] args = null;
                        if (!string.IsNullOrWhiteSpace(quest.OnAdvanceArgs))
                            args = quest.OnAdvanceArgs.Split(',');
                        rule.Run(player, questOwner, questID, args);
                    });
                }
            }
        }


        private void CreateExtendedQuestDataEntries(PCQuestStatus status)
        {
            var quest = DataService.Get<Quest>(status.QuestID);
            var state = DataService.Single<QuestState>(x => x.QuestID == quest.ID && x.ID == status.CurrentQuestStateID);
            var killTargets = DataService.Where<QuestKillTarget>(x => x.QuestStateID == state.ID);
            var requiredItems = DataService.Where<QuestRequiredItem>(x => x.QuestStateID == state.ID);

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

            DataService.SubmitDataChange(status, DatabaseActionType.Update);
        }

        private void RequestRewardSelectionFromPC(NWPlayer oPC, NWObject questOwner, int questID)
        {
            if (!oPC.IsPlayer) return;
            
            Quest quest = DataService.Single<Quest>(x => x.ID == questID);

            if (quest.AllowRewardSelection)
            {
                oPC.SetLocalInt("QST_REWARD_SELECTION_QUEST_ID", questID);
                _dialog.StartConversation(oPC, oPC, "QuestRewardSelection");
            }
            else
            {
                CompleteQuest(oPC, questOwner, questID, null);
            }

        }

        private bool DoesPlayerMeetPrerequisites(NWPlayer oPC, int questID)
        {
            var prereqs = DataService.Where<QuestPrerequisite>(x => x.QuestID == questID).ToList();

            if (!oPC.IsPlayer) return false;
            if (prereqs.Count <= 0) return true;

            List<int> completedQuestIDs = DataService.Where<PCQuestStatus>(x => x.PlayerID == oPC.GlobalID && x.CompletionDate != null)
                .Select(s => s.QuestID).ToList();

            List<int> prereqIDs = new List<int>();
            foreach (QuestPrerequisite prereq in prereqs)
            {
                prereqIDs.Add(prereq.RequiredQuestID);
            }

            return completedQuestIDs.ContainsAll(prereqIDs);
        }

        private bool DoesPlayerHaveRequiredKeyItems(NWPlayer oPC, int questStateID)
        {
            var requiredKeyItems = DataService.Where<QuestRequiredKeyItem>(x => x.QuestStateID == questStateID).ToList();
            if (!oPC.IsPlayer) return false;
            if (requiredKeyItems.Count <= 0) return true;

            List<int> keyItemIDs = DataService.Where<PCKeyItem>(x => x.PlayerID == oPC.GlobalID)
                .Select(s => s.KeyItemID).ToList();

            List<int> requiredKeyItemIDs = new List<int>();
            foreach (QuestRequiredKeyItem ki in requiredKeyItems)
            {
                requiredKeyItemIDs.Add(ki.KeyItemID);
            }

            return keyItemIDs.ContainsAll(requiredKeyItemIDs);
        }


        public void SpawnQuestItems(NWPlaceable oChest, NWPlayer oPC)
        {
            int questID = oChest.GetLocalInt("QUEST_ID");
            int questStateSequence = oChest.GetLocalInt("QUEST_SEQUENCE");
            string questItemResref = oChest.GetLocalString("QUEST_ITEM_RESREF");

            if (questID <= 0 || questStateSequence <= 0 || string.IsNullOrWhiteSpace(questItemResref)) return;

            PCQuestStatus status = DataService.Single<PCQuestStatus>(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);
            QuestState questState = DataService.Get<QuestState>(status.CurrentQuestStateID);

            if (questStateSequence != questState.Sequence) return;
            if (_.GetIsObjectValid(_.GetItemPossessedBy(oPC.Object, questItemResref)) == FALSE) return;

            // PC is on the correct quest, correct state, the chest creates quest items, and the PC does not already have the quest item.
            // Spawn it.

            _.CreateItemOnObject(questItemResref, oChest.Object);

        }


        public void OnCreatureDeath(NWCreature creature)
        {
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
                var killTargets = DataService.Where<PCQuestKillTargetProgress>(x => x.PlayerID == playerID && x.NPCGroupID == npcGroupID).ToList();
                
                foreach (var kt in killTargets)
                {
                    var questStatus = DataService.Get<PCQuestStatus>(kt.PCQuestStatusID);
                    var quest = DataService.Get<Quest>(questStatus.QuestID);
                    var npcGroup = DataService.Get<NPCGroup>(kt.NPCGroupID);

                    kt.RemainingToKill--;
                    string targetGroupName = npcGroup.Name;
                    string updateMessage = "[" + quest.Name + "] " + targetGroupName + " remaining: " + kt.RemainingToKill;
                    DatabaseActionType action = DatabaseActionType.Update;

                    if (kt.RemainingToKill <= 0)
                    {
                        updateMessage += " " + _color.Green(" {COMPLETE}");
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
                        App.ResolveByInterface<IQuestRule>("QuestRule." + ruleName, (rule) =>
                        {
                            string[] args = null;
                            if (!string.IsNullOrWhiteSpace(quest.OnKillTargetArgs))
                                args = quest.OnKillTargetArgs.Split(',');
                            rule.Run(pcCopy, creature, quest.ID, args);
                        });
                    }

                }

                oPC = _.GetNextFactionMember(oKiller);
            }
            
            foreach (var player in playersToAdvance)
            {
                AdvanceQuestState(player.Key, null, player.Value);
            }
        }
        
        private void HandleTriggerAndPlaceableQuestLogic(NWPlayer oPC, NWObject oObject)
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

            PCQuestStatus pcQuestStatus = DataService.SingleOrDefault<PCQuestStatus>(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);
            if (pcQuestStatus == null) return;

            QuestState questState = DataService.Get<QuestState>(pcQuestStatus.CurrentQuestStateID);

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
                _ovs.AdjustVisibility(oPC, oObject, false);
            }
        }

        public void OnQuestPlaceableUsed(NWObject oObject)
        {
            NWPlayer oPC = (_.GetLastUsedBy());
            HandleTriggerAndPlaceableQuestLogic(oPC, oObject);
        }

        public void OnQuestTriggerEntered(NWObject oObject)
        {
            NWPlayer oPC = (_.GetEnteringObject());
            HandleTriggerAndPlaceableQuestLogic(oPC, oObject);
        }

        public void RequestItemsFromPC(NWPlayer oPC, NWObject questOwner, int questID)
        {
            if (!oPC.IsPlayer) return;

            PCQuestStatus pcStatus = DataService.SingleOrDefault<PCQuestStatus>(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);

            if (pcStatus == null)
            {
                oPC.SendMessage("You have not accepted this quest yet.");
                return;
            }

            QuestState questState = DataService.Get<QuestState>(pcStatus.CurrentQuestStateID);
            var requiredKeyItems = DataService.Where<QuestRequiredKeyItem>(x => x.QuestStateID == pcStatus.CurrentQuestStateID);

            foreach (QuestRequiredKeyItem ki in requiredKeyItems)
            {
                if (!_keyItem.PlayerHasKeyItem(oPC, ki.KeyItemID))
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

        public bool HasPlayerCompletedQuest(NWObject oPC, int questID)
        {
            PCQuestStatus status = DataService.Single<PCQuestStatus>(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);

            return status?.CompletionDate != null;
        }

        public int GetPlayerQuestJournalID(NWObject oPC, int questID)
        {
            PCQuestStatus status = DataService.Single<PCQuestStatus>(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);

            if (status == null) return -1;
            var state = DataService.Get<QuestState>(status.CurrentQuestStateID);
            return state.JournalStateID;
        }

    }
}
