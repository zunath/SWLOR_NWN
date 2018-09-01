using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using Quest = SWLOR.Game.Server.Data.Entities.Quest;

namespace SWLOR.Game.Server.Service
{
    public class QuestService : IQuestService
    {
        private const string TempStoragePlaceableTag = "QUEST_BARREL";
        private const string SubmitQuestItemResref = "qst_submit";

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
            NWPlaceable tempStorage = NWPlaceable.Wrap(_.GetObjectByTag(TempStoragePlaceableTag));
            NWItem tempItem = NWItem.Wrap(_.CreateItemOnObject(resref, tempStorage.Object));
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

        public void CompleteQuest(NWPlayer player, int questID, ItemVO selectedItem)
        {
            if (!player.IsPlayer) return;
            
            Quest quest = _db.Quests.Single(x => x.QuestID == questID);
            PCQuestStatus pcState = _db.PCQuestStatus.Single(x => x.PlayerID == player.GlobalID && x.QuestID == questID);

            QuestState finalState = null;
            foreach (QuestState questState in quest.QuestStates)
            {
                if (questState.IsFinalState)
                {
                    finalState = questState;
                    break;
                }
            }

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
                _.CreateItemOnObject(selectedItem.Resref, player.Object, selectedItem.Quantity, "");
            }

            if (quest.RewardGold > 0)
            {
                _.GiveGoldToCreature(player.Object, quest.RewardGold);
            }

            if (quest.RewardXP > 0)
            {
                // TODO: Skill-related exp rewards??
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
                PCRegionalFame fame = _db.PCRegionalFames.Single(x => x.PlayerID == player.GlobalID && x.FameRegionID == quest.FameRegionID);
                fame.Amount += quest.RewardFame;
            }

            player.SendMessage("Quest '" + quest.Name + "' complete!");
            _db.SaveChanges();
        }

        public void OnModuleItemAcquired()
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetModuleItemAcquiredBy());
            NWItem oItem = NWItem.Wrap(_.GetModuleItemAcquired());

            if (!oPC.IsPlayer) return;

            int questID = oItem.GetLocalInt("QUEST_ID");

            if (questID <= 0) return;
            oItem.IsCursed = true;
        }

        public void OnClientEnter()
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetEnteringObject());

            if (!oPC.IsPlayer) return;

            List<PCQuestStatus> pcQuests = _db.PCQuestStatus.Where(x => x.PlayerID == oPC.GlobalID).ToList();

            foreach (PCQuestStatus quest in pcQuests)
            {
                _.AddJournalQuestEntry(quest.Quest.JournalTag, quest.CurrentQuestState.JournalStateID, oPC.Object, NWScript.FALSE);
            }
        }


        public void AcceptQuest(NWPlayer oPC, int questID)
        {
            if (!oPC.IsPlayer) return;

            PCQuestStatus status = _db.PCQuestStatus.Single(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);

            if (status != null)
            {
                if (status.CompletionDate != null)
                {
                    oPC.SendMessage("You have already completed this quest.");
                    return;
                }
                else
                {
                    oPC.SendMessage("You have already accepted this quest.");
                    return;
                }
            }


            Quest quest = _db.Quests.Single(x => x.QuestID == questID);

            if (!DoesPlayerMeetPrerequisites(oPC, quest.QuestPrerequisites))
            {
                oPC.SendMessage("You do not meet the prerequisites necessary to accept this quest.");
                return;
            }

            if (!DoesPlayerHaveRequiredKeyItems(oPC, quest.QuestStates.ElementAt(0).QuestRequiredKeyItemLists))
            {
                oPC.SendMessage("You do not have the required key items to accept this quest.");
                return;
            }

            PCRegionalFame fame = _db.PCRegionalFames.Single(x => x.PlayerID == oPC.GlobalID && x.FameRegionID == quest.FameRegionID); 

            if (fame.Amount < quest.RequiredFameAmount)
            {
                oPC.SendMessage("You do not have enough fame to accept this quest.");
                return;
            }

            status = new PCQuestStatus();
            foreach (QuestState state in quest.QuestStates)
            {
                if (state.Sequence == 1)
                {
                    status.CurrentQuestStateID = state.QuestStateID;
                    break;
                }
            }

            if (status.CurrentQuestState == null)
            {
                oPC.SendMessage("There was an error accepting the quest '" + quest.Name + "'. Please inform an admin this quest is bugged. (QuestID: " + questID + ")");
                return;
            }

            // Give temporary key item at start of quest.
            if (quest.StartKeyItemID != null)
            {
                _keyItem.GivePlayerKeyItem(oPC, (int)quest.StartKeyItemID);
            }

            if (!string.IsNullOrWhiteSpace(quest.MapNoteTag))
            {
                _mapPin.AddWaypointMapPin(oPC, quest.MapNoteTag, quest.Name, "QST_MAP_NOTE_" + questID);
            }

            status.QuestID = quest.QuestID;
            status.PlayerID = oPC.GlobalID;
            _db.PCQuestStatus.Add(status);
            _db.SaveChanges();
            
            CreateExtendedQuestDataEntries(status, questID, 1);

            _.AddJournalQuestEntry(quest.JournalTag, 1, oPC.Object, NWScript.FALSE);
            oPC.SendMessage("Quest '" + quest.Name + "' accepted. Refer to your journal for more information on this quest.");
        }

        public void AdvanceQuestState(NWPlayer oPC, int questID)
        {
            if (!oPC.IsPlayer) return;

            PCQuestStatus questStatus = _db.PCQuestStatus.Single(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);

            if (questStatus == null)
            {
                oPC.SendMessage("You have not accepted this quest yet.");
                return;
            }

            if (questStatus.CompletionDate != null) return;

            Quest quest = questStatus.Quest;
            
            // Find the next quest state.
            foreach (QuestState nextState in quest.QuestStates)
            {
                if (nextState.Sequence == questStatus.CurrentQuestState.Sequence + 1)
                {
                    // Either complete the quest or move to the new state.
                    if (nextState.IsFinalState)
                    {
                        RequestRewardSelectionFromPC(oPC, questID);
                        return;
                    }
                    else
                    {
                        _.AddJournalQuestEntry(quest.JournalTag, nextState.JournalStateID, oPC.Object, NWScript.FALSE);
                        questStatus.CurrentQuestStateID = nextState.QuestStateID;
                        oPC.SendMessage("Objective for quest '" + quest.Name + "' complete! Check your journal for information on the next objective.");
                        _db.SaveChanges();
                        
                        CreateExtendedQuestDataEntries(questStatus, questID, nextState.Sequence);
                        return;
                    }
                }
            }

            // Shouldn't reach this point unless configuration for the quest is broken.
            oPC.SendMessage("There was an error advancing you to the next objective for quest '" + quest.Name + "'. Please inform an admin this quest is bugged. (QuestID = " + questID + ")");
        }


        private void CreateExtendedQuestDataEntries(PCQuestStatus status, int questID, int sequenceID)
        {
            // Create entries for the PC kill targets.
            List<QuestKillTargetList> killTargets = _db.StoredProcedure<QuestKillTargetList>("GetQuestKillTargetsByQuestSequenceID",
                new SqlParameter("QuestID", questID),
                new SqlParameter("SequenceID", sequenceID));

            foreach (QuestKillTargetList kt in killTargets)
            {
                PCQuestKillTargetProgress pcKT = new PCQuestKillTargetProgress
                {
                    RemainingToKill = kt.Quantity,
                    NPCGroupID = kt.NPCGroupID,
                    PCQuestStatusID = status.PCQuestStatusID,
                    PlayerID = status.PlayerID
                };
                _db.PCQuestKillTargetProgresses.Add(pcKT);
                _db.SaveChanges();
            }
        }

        private void RequestRewardSelectionFromPC(NWPlayer oPC, int questID)
        {
            if (!oPC.IsPlayer) return;

            Quest quest = _db.Quests.Single(x => x.QuestID == questID);

            if (quest.AllowRewardSelection)
            {
                oPC.SetLocalInt("QST_REWARD_SELECTION_QUEST_ID", questID);
                _dialog.StartConversation(oPC, oPC, "QuestRewardSelection");
            }
            else
            {
                QuestState lastState = quest.QuestStates.ElementAt(quest.QuestStates.Count - 1);
                _.AddJournalQuestEntry(quest.JournalTag, lastState.JournalStateID, oPC.Object, NWScript.FALSE);
                CompleteQuest(oPC, questID, null);
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
            if (_.GetIsObjectValid(_.GetItemPossessedBy(oPC.Object, questItemResref)) == NWScript.FALSE) return;

            // PC is on the correct quest, correct state, the chest creates quest items, and the PC does not already have the quest item.
            // Spawn it.

            _.CreateItemOnObject(questItemResref, oChest.Object);

        }


        public void OnCreatureDeath(NWCreature creature)
        {
            int npcGroupID = creature.GetLocalInt("NPC_GROUP"); 
            if (npcGroupID <= 0) return;

            NWObject oKiller = NWObject.Wrap(_.GetLastKiller());

            if (_.GetIsPC(oKiller.Object) == NWScript.FALSE || _.GetIsDM(oKiller.Object) == NWScript.TRUE) return;

            string areaResref = creature.Area.Resref;

            NWPlayer oPC = NWPlayer.Wrap(_.GetFirstFactionMember(oKiller.Object));
            while(oPC.IsValid)
            {
                if (areaResref != oPC.Area.Resref) continue;
                if (_.GetDistanceBetween(creature.Object, oPC.Object) == 0.0f || _.GetDistanceBetween(creature.Object, oPC.Object) > 20.0f) continue;

                List<PCQuestKillTargetProgress> killTargets = _db.PCQuestKillTargetProgresses.Where(x => x.PlayerID == oPC.GlobalID && x.NPCGroupID == npcGroupID).ToList();

                foreach (PCQuestKillTargetProgress kt in killTargets)
                {
                    kt.RemainingToKill--; 
                    string targetGroupName = kt.NPCGroup.Name;
                    string questName = kt.PcQuestStatus.Quest.Name;
                    string updateMessage = "[" + questName + "] " + targetGroupName + " remaining: " + kt.RemainingToKill;

                    if (kt.RemainingToKill <= 0)
                    {
                        _db.PCQuestKillTargetProgresses.Remove(kt);
                        updateMessage += " " + _color.Green(" {COMPLETE}");

                        if (kt.PcQuestStatus.PCQuestKillTargetProgresses.Count - 1 <= 0)
                        {
                            AdvanceQuestState(oPC, kt.PcQuestStatus.QuestID);
                        }
                    }

                    _db.SaveChanges();
                    string finalMessage = updateMessage;
                    var pc = oPC;
                    oPC.DelayCommand(() =>
                    {
                        pc.SendMessage(finalMessage);
                    }, 1.0f);
                }

                oPC = NWPlayer.Wrap(_.GetNextFactionMember(oKiller.Object));
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
                oPC.SendMessage("QUEST_ID variable not set on object. Please inform admin this quest is bugged.");
                return;
            }

            if (questSequence <= 0)
            {
                oPC.SendMessage("QUEST_SEQUENCE variable not set on object. Please inform admin this quest is bugged. (QuestID: " + questID + ")");
                return;
            }

            PCQuestStatus pcQuestStatus = _db.PCQuestStatus.Single(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);
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
                oPC.DelayCommand(() =>
                {
                    oPC.SendMessage(questMessage);
                }, 1.0f);
            }

            AdvanceQuestState(oPC, questID);
        }

        public void OnQuestPlaceableUsed(NWObject oObject)
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastUsedBy());
            HandleTriggerAndPlaceableQuestLogic(oPC, oObject);
        }

        public void OnQuestTriggerEntered(NWObject oObject)
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetEnteringObject());
            HandleTriggerAndPlaceableQuestLogic(oPC, oObject);
        }


        public void OnItemCollectorOpened(NWPlaceable container)
        {
            container.IsUseable = false;

            NWPlayer oPC = NWPlayer.Wrap(_.GetLastOpenedBy());
            int questID = container.GetLocalInt("QUEST_ID");
            QuestState state = _db.PCQuestStatus.Single(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID).CurrentQuestState;

            oPC.FloatingText("Please place the items you would like to turn in for this quest into the container.");
            
            string text = "Required Items: \n\n";

            foreach (QuestRequiredItemList item in state.QuestRequiredItemLists)
            {
                ItemVO tempItemModel = GetTempItemInformation(item.Resref, item.Quantity);
                text += tempItemModel.Quantity + "x " + tempItemModel.Name + "\n";
            }

            oPC.SendMessage(text);
        }

        public void OnItemCollectorClosed(NWObject container)
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastClosedBy());
            foreach (NWItem item in container.InventoryItems)
            {
                if (item.Resref != SubmitQuestItemResref)
                {
                    _.CopyItem(item.Object, oPC.Object, NWScript.TRUE);
                }
                item.Destroy();
            }

            container.Destroy();
        }

        private void HandleAddItemToItemCollector(NWPlayer oPC, NWPlaceable container, NWItem oItem)
        {
            string resref = oItem.Resref;
            if (resref == SubmitQuestItemResref) return;

            int questID = container.GetLocalInt("QUEST_ID");
            QuestState state = _db.PCQuestStatus.Single(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID).CurrentQuestState;

            foreach (QuestRequiredItemList reqItem in state.QuestRequiredItemLists)
            {
                if (reqItem.Resref == resref)
                {
                    return;
                }
            }

            _.CopyItem(oItem.Object, oPC.Object, NWScript.TRUE);
            oItem.Destroy();
            oPC.SendMessage(_color.Red("That item is not required for this quest."));
        }


        private void HandleRemoveItemFromItemCollector(NWPlayer oPC, NWPlaceable container, NWItem oItem)
        {
            string resref = oItem.Resref;
            if (resref != SubmitQuestItemResref) return;

            oItem.Destroy();
            _.CreateItemOnObject(SubmitQuestItemResref, container.Object);

            int questID = container.GetLocalInt("QUEST_ID");
            QuestState state = _db.PCQuestStatus.Single(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID).CurrentQuestState;
            List<NWItem> submittedItems = container.InventoryItems;

            Dictionary<string, int> requiredItems = new Dictionary<string, int>();

            // Consolidate the number of items required. This is necessary in case there are two entries for the same
            // item in the database. We simply sum those up to get one result in the hash map.
            foreach (QuestRequiredItemList ri in state.QuestRequiredItemLists)
            {
                if (requiredItems.ContainsKey(ri.Resref))
                {
                    int count = requiredItems[ri.Resref] + ri.Quantity;
                    requiredItems[ri.Resref] = count;
                }
                else
                {
                    requiredItems[ri.Resref] = ri.Quantity;
                }
            }

            foreach (NWItem item in submittedItems)
            {
                resref = item.Resref;
                if (resref == SubmitQuestItemResref)
                    continue;

                int stackSize = item.StackSize;

                if (requiredItems.ContainsKey(resref))
                {
                    int amountRequired = requiredItems[resref];

                    // Same amount - destroy and remove.
                    if (stackSize == amountRequired)
                    {
                        item.Destroy();
                        requiredItems.Remove(resref);
                    }
                    // Stack > amount - change stack size and remove.
                    else if (stackSize > amountRequired)
                    {
                        stackSize = stackSize - amountRequired;
                        item.StackSize = stackSize;
                        requiredItems.Remove(resref);
                        _.CopyItem(item.Object, oPC.Object, NWScript.TRUE);
                    }
                    // Stack < amount - destroy and update required remaining.
                    else if (stackSize < amountRequired)
                    {
                        item.Destroy();
                        amountRequired = amountRequired - stackSize;
                        requiredItems[resref] = amountRequired;
                    }
                }
                // No more items needed.
                else
                {
                    _.CopyItem(item.Object, oPC.Object, NWScript.TRUE);
                    item.Destroy();
                }
            }

            // Still missing items. Return everything to player and give error message.
            if (requiredItems.Count > 0)
            {
                foreach (NWItem item in container.InventoryItems)
                {
                    if (item.Resref != SubmitQuestItemResref)
                        _.CopyItem(item.Object, oPC.Object, NWScript.TRUE);
                    item.Destroy();
                }

                oPC.SendMessage(_color.Red("You are missing some required items..."));
            }
            // Succeeded. Take everything and advance the quest state.
            else
            {
                foreach (NWItem item in container.InventoryItems)
                {
                    item.Destroy();
                }

                AdvanceQuestState(oPC, questID);
            }

            container.Destroy();
        }

        public void OnItemCollectorDisturbed(NWPlaceable container)
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastDisturbed());
            NWItem oItem = NWItem.Wrap(_.GetInventoryDisturbItem());
            int disturbType = _.GetInventoryDisturbType();

            if (disturbType == NWScript.INVENTORY_DISTURB_TYPE_ADDED)
            {
                HandleAddItemToItemCollector(oPC, container, oItem);
            }
            else if (disturbType == NWScript.INVENTORY_DISTURB_TYPE_REMOVED)
            {
                HandleRemoveItemFromItemCollector(oPC, container, oItem);
            }
        }

        public void RequestItemsFromPC(NWPlayer oPC, NWObject questOwner, int questID, int sequenceID)
        {
            if (!oPC.IsPlayer) return;

            PCQuestStatus pcStatus = _db.PCQuestStatus.Single(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);
            
            if (pcStatus == null)
            {
                oPC.SendMessage("You have not accepted this quest yet.");
                return;
            }

            QuestState questState = pcStatus.CurrentQuestState;

            if (questState.Sequence != sequenceID)
            {
                oPC.SendMessage("SequenceID mismatch. Please inform an admin there is a bug with this quest. (QuestID = " + questID + ")");
                return;
            }

            foreach (QuestRequiredKeyItemList ki in questState.QuestRequiredKeyItemLists)
            {
                if (!_keyItem.PlayerHasKeyItem(oPC, ki.KeyItemID))
                {
                    oPC.SendMessage("You are missing a required key item.");
                    return;
                }
            }

            Location location = oPC.Location;
            NWPlaceable collector = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "qst_item_collect", location));
            
            collector.AssignCommand(() =>
            {
                _.SetFacingPoint(oPC.Position);
            });
            collector.SetLocalInt("QUEST_ID", questID);
            _.CreateItemOnObject(SubmitQuestItemResref, collector.Object);

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
