using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using static NWN.NWScript;
using Quest = SWLOR.Game.Server.Data.Entities.Quest;

namespace SWLOR.Game.Server.Service
{
    public class QuestService : IQuestService
    {
        private const string TempStoragePlaceableTag = "QUEST_BARREL";

        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IKeyItemService _keyItem;
        private readonly IMapPinService _mapPin;
        private readonly IDialogService _dialog;
        private readonly IColorTokenService _color;

        public QuestService(INWScript script,
            IDataContext db,
            IKeyItemService keyItem,
            IMapPinService mapPin,
            IDialogService dialog,
            IColorTokenService color)
        {
            _ = script;
            _db = db;
            _keyItem = keyItem;
            _mapPin = mapPin;
            _dialog = dialog;
            _color = color;
        }

        public Quest GetQuestByID(int questID)
        {
            return _db.Quests.Single(x => x.QuestID == questID);
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

            Quest quest = _db.Quests.Single(x => x.QuestID == questID);
            PCQuestStatus pcState = _db.PCQuestStatus.Single(x => x.PlayerID == player.GlobalID && x.QuestID == questID);

            QuestState finalState = quest.QuestStates.OrderBy(o => o.Sequence).Last();

            if (finalState == null)
            {
                player.SendMessage("Could not find final state of quest. Please notify an admin this quest is bugged. (QuestID: " + questID + ")");
                return;
            }

            pcState.CurrentQuestStateID = finalState.QuestStateID;
            pcState.CompletionDate = DateTime.UtcNow;

            if (selectedItem == null)
            {
                foreach (QuestRewardItem reward in quest.QuestRewardItems)
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
                PCRegionalFame fame = _db.PCRegionalFames.SingleOrDefault(x => x.PlayerID == player.GlobalID && x.FameRegionID == quest.FameRegionID);
                if (fame == null)
                {
                    fame = new PCRegionalFame
                    {
                        PlayerID = player.GlobalID,
                        FameRegionID = quest.FameRegionID,
                        Amount = 0
                    };

                    _db.PCRegionalFames.Add(fame);
                }

                fame.Amount += quest.RewardFame;
            }

            player.SendMessage("Quest '" + quest.Name + "' complete!");
            _db.SaveChanges();

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

            List<PCQuestStatus> pcQuests = _db.PCQuestStatus.Where(x => x.PlayerID == oPC.GlobalID && x.CompletionDate == null).ToList();

            foreach (PCQuestStatus quest in pcQuests)
            {
                _.AddJournalQuestEntry(quest.Quest.JournalTag, quest.CurrentQuestState.JournalStateID, oPC.Object, FALSE);
            }
        }

        public bool CanAcceptQuest(NWPlayer oPC, int questID, bool sendMessage)
        {
            var quest = GetQuestByID(questID);
            return CanAcceptQuest(oPC, quest, sendMessage);
        }

        public bool CanAcceptQuest(NWPlayer oPC, Quest quest, bool sendMessage)
        {
            PCQuestStatus status = _db.PCQuestStatus.SingleOrDefault(x => x.PlayerID == oPC.GlobalID && x.QuestID == quest.QuestID);

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

            if (!DoesPlayerMeetPrerequisites(oPC, quest.QuestPrerequisites))
            {
                if (sendMessage)
                    oPC.SendMessage("You do not meet the prerequisites necessary to accept this quest.");
                return false;
            }

            if (!DoesPlayerHaveRequiredKeyItems(oPC, quest.QuestStates.ElementAt(0).QuestRequiredKeyItemLists))
            {
                if (sendMessage)
                    oPC.SendMessage("You do not have the required key items to accept this quest.");
                return false;
            }

            PCRegionalFame fame = _db.PCRegionalFames.SingleOrDefault(x => x.PlayerID == oPC.GlobalID && x.FameRegionID == quest.FameRegionID);

            if (fame != null)
            {
                if (fame.Amount < quest.RequiredFameAmount)
                {
                    if (sendMessage)
                        oPC.SendMessage("You do not have enough fame to accept this quest.");
                    return false;
                }
            }

            return true;
        }


        public void AcceptQuest(NWPlayer player, NWObject questOwner, int questID)
        {
            if (!player.IsPlayer) return;

            Quest quest = _db.Quests.Single(x => x.QuestID == questID);

            if (!CanAcceptQuest(player, quest, true))
            {
                return;
            }

            var status = new PCQuestStatus
            {
                CurrentQuestStateID = quest.QuestStates.Single(x => x.QuestID == questID && x.Sequence == 1).QuestStateID
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

            status.QuestID = quest.QuestID;
            status.PlayerID = player.GlobalID;
            _db.PCQuestStatus.Add(status);
            CreateExtendedQuestDataEntries(status);
            _db.SaveChanges();

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

            PCQuestStatus questStatus = _db.PCQuestStatus.SingleOrDefault(x => x.PlayerID == player.GlobalID && x.QuestID == questID);

            if (questStatus == null)
            {
                player.SendMessage("You have not accepted this quest yet.");
                return;
            }

            if (questStatus.CompletionDate != null) return;

            Quest quest = questStatus.Quest;
            QuestState nextState = quest.QuestStates.SingleOrDefault(x => x.Sequence == questStatus.CurrentQuestState.Sequence + 1);
            
            // Either complete the quest or move to the new state.
            if (nextState == null) // We assume this is the last state in the quest, so it must be time to complete it.
            {
                RequestRewardSelectionFromPC(player, questOwner, questID);
            }
            else
            {
                _.AddJournalQuestEntry(quest.JournalTag, nextState.JournalStateID, player, FALSE);
                questStatus.CurrentQuestStateID = nextState.QuestStateID;
                player.SendMessage("Objective for quest '" + quest.Name + "' complete! Check your journal for information on the next objective.");
                
                CreateExtendedQuestDataEntries(questStatus);
                _db.SaveChanges();

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
            var state = status.Quest.QuestStates.Single(x => x.QuestStateID == status.CurrentQuestStateID);

            // Create entries for the PC kill targets.
            foreach (var kt in state.QuestKillTargetLists)
            {
                PCQuestKillTargetProgress pcKT = new PCQuestKillTargetProgress
                {
                    RemainingToKill = kt.Quantity,
                    NPCGroupID = kt.NPCGroupID,
                    PCQuestStatusID = status.PCQuestStatusID,
                    PlayerID = status.PlayerID
                };
                status.PCQuestKillTargetProgresses.Add(pcKT);
            }

            // Create entries for PC items required.
            foreach (var item in state.QuestRequiredItemLists)
            {
                PCQuestItemProgress itemProgress = new PCQuestItemProgress
                {
                    Resref = item.Resref,
                    PlayerID = status.PlayerID,
                    PCQuestStatusID = status.PCQuestStatusID,
                    Remaining = item.Quantity
                };
                status.PCQuestItemProgresses.Add(itemProgress);
            }
        }

        private void RequestRewardSelectionFromPC(NWPlayer oPC, NWObject questOwner, int questID)
        {
            Console.WriteLine("asking for reward selection");
            if (!oPC.IsPlayer) return;

            Quest quest = _db.Quests.Single(x => x.QuestID == questID);

            if (quest.AllowRewardSelection)
            {
                oPC.SetLocalInt("QST_REWARD_SELECTION_QUEST_ID", questID);
                _dialog.StartConversation(oPC, oPC, "QuestRewardSelection");
            }
            else
            {
                _.RemoveJournalQuestEntry(quest.JournalTag, oPC, FALSE);
                CompleteQuest(oPC, questOwner, questID, null);
            }

        }

        private bool DoesPlayerMeetPrerequisites(NWPlayer oPC, ICollection<QuestPrerequisite> prereqs)
        {
            if (!oPC.IsPlayer) return false;
            if (prereqs.Count <= 0) return true;

            List<int> completedQuestIDs = _db.PCQuestStatus.Where(x => x.PlayerID == oPC.GlobalID && x.CompletionDate != null)
                .Select(s => s.QuestID).ToList();

            List<int> prereqIDs = new List<int>();
            foreach (QuestPrerequisite prereq in prereqs)
            {
                prereqIDs.Add(prereq.RequiredQuestID);
            }

            return completedQuestIDs.ContainsAll(prereqIDs);
        }

        private bool DoesPlayerHaveRequiredKeyItems(NWPlayer oPC, ICollection<QuestRequiredKeyItemList> requiredKeyItems)
        {
            if (!oPC.IsPlayer) return false;
            if (requiredKeyItems.Count <= 0) return true;

            List<int> keyItemIDs = _db.PCKeyItems.Where(x => x.PlayerID == oPC.GlobalID)
                .Select(s => s.KeyItemID).ToList();

            List<int> requiredKeyItemIDs = new List<int>();
            foreach (QuestRequiredKeyItemList ki in requiredKeyItems)
            {
                requiredKeyItemIDs.Add(ki.KeyItemID);
            }

            return keyItemIDs.ContainsAll(requiredKeyItemIDs);
        }


        public void SpawnQuestItems(NWPlaceable oChest, NWPlayer oPC)
        {
            int questID = oChest.GetLocalInt("QUEST_ID");
            int questStateSequence = oChest.GetLocalInt("SEQUENCE_ID");
            string questItemResref = oChest.GetLocalString("QUEST_ITEM_RESREF");

            if (questID <= 0 || questStateSequence <= 0 || string.IsNullOrWhiteSpace(questItemResref)) return;

            PCQuestStatus status = _db.PCQuestStatus.Single(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);
            QuestState questState = status.CurrentQuestState;

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

                if (_.GetDistanceBetween(creature, oPC) == 0.0f || _.GetDistanceBetween(creature, oPC) > 20.0f)
                {
                    oPC = _.GetNextFactionMember(oKiller);
                    continue;
                }

                foreach (var kt in _db.PCQuestKillTargetProgresses.Where(x => x.PlayerID == oPC.GlobalID && x.NPCGroupID == npcGroupID))
                {
                    kt.RemainingToKill--;
                    int questID = kt.PcQuestStatus.QuestID;
                    var quest = kt.PcQuestStatus.Quest;
                    string targetGroupName = kt.NPCGroup.Name;
                    
                    string updateMessage = "[" + quest.Name + "] " + targetGroupName + " remaining: " + kt.RemainingToKill;

                    if (kt.RemainingToKill <= 0)
                    {
                        updateMessage += " " + _color.Green(" {COMPLETE}");
                        playersToAdvance.Add(new KeyValuePair<NWPlayer, int>(oPC, questID));
                        _db.PCQuestKillTargetProgresses.Remove(kt);

                    }

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
                            rule.Run(pcCopy, creature, quest.QuestID, args);
                        });
                    }

                }

                oPC = _.GetNextFactionMember(oKiller);
            }

            _db.SaveChanges();

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

            PCQuestStatus pcQuestStatus = _db.PCQuestStatus.SingleOrDefault(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);
            if (pcQuestStatus == null) return;

            QuestState questState = pcQuestStatus.CurrentQuestState;

            if (questState.Sequence == questSequence ||
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

            PCQuestStatus pcStatus = _db.PCQuestStatus.SingleOrDefault(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);

            if (pcStatus == null)
            {
                oPC.SendMessage("You have not accepted this quest yet.");
                return;
            }

            QuestState questState = pcStatus.CurrentQuestState;

            foreach (QuestRequiredKeyItemList ki in questState.QuestRequiredKeyItemLists)
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
            PCQuestStatus status = _db.PCQuestStatus.Single(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);

            return status?.CompletionDate != null;
        }

        public int GetPlayerQuestJournalID(NWObject oPC, int questID)
        {
            PCQuestStatus status = _db.PCQuestStatus.Single(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);

            if (status == null) return -1;
            return status.CurrentQuestState.JournalStateID;
        }

    }
}
