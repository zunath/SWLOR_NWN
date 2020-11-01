using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using Player = NWN.FinalFantasy.Entity.Player;

namespace SWLOR.Game.Server.Feature
{
    public static class QuestProgression
    {

        /// <summary>
        /// When an NPC is killed, any objectives for quests a player currently has active will be updated.
        /// </summary>
        [NWNEventHandler("crea_death")]
        public static void ProgressKillTargetObjectives()
        {
            var creature = OBJECT_SELF;
            var npcGroupType = (NPCGroupType)GetLocalInt(creature, "QUEST_NPC_GROUP_ID");
            if (npcGroupType == NPCGroupType.Invalid) return;
            var possibleQuests = Quest.GetQuestsAssociatedWithNPCGroup(npcGroupType);
            if (possibleQuests.Count <= 0) return;

            var killer = GetLastKiller();

            // Iterate over every player in the killer's party.
            // Every player who needs this NPCGroupType for a quest will have their objective advanced.
            for (var member = GetFirstFactionMember(killer); GetIsObjectValid(member); member = GetNextFactionMember(killer))
            {
                if (!GetIsPC(member) || GetIsDM(member)) continue;

                var playerId = GetObjectUUID(member);
                var dbPlayer = DB.Get<Player>(playerId);

                // Need to iterate over every possible quest this creature is a part of.
                foreach (var questId in possibleQuests)
                {
                    // Players who don't have the quest are skipped.
                    if (!dbPlayer.Quests.ContainsKey(questId)) continue;

                    var quest = dbPlayer.Quests[questId];
                    var questDetail = Quest.GetQuestById(questId);
                    var questState = questDetail.States[quest.CurrentState];

                    // Iterate over all of the quest states which call for killing this enemy.
                    foreach (var objective in questState.GetObjectives())
                    {
                        // Only kill target objectives matching this NPC group ID are processed.
                        if (objective is KillTargetObjective killTargetObjective)
                        {
                            if (killTargetObjective.Group != npcGroupType) continue;

                            killTargetObjective.Advance(member, questId);
                        }
                    }

                    // Attempt to advance the quest detail. It's possible this will fail because objectives aren't all done. This is OK.
                    questDetail.Advance(member, creature);
                }
            }
        }

        /// <summary>
        /// When an item collector placeable is opened, 
        /// </summary>
        [NWNEventHandler("qst_collect_open")]
        public static void OpenItemCollector()
        {
            var container = OBJECT_SELF;
            SetUseableFlag(container, false);

            var questId = GetLocalString(container, "QUEST_ID");
            var player = GetLastOpenedBy();
            var playerId = GetObjectUUID(player);

            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.Quests.ContainsKey(questId))
            {
                SendMessageToPC(player, "You have not accepted this quest.");
                return;
            }

            FloatingTextStringOnCreature("Please place the items you would like to turn in for this quest into the container. If you want to cancel this process, move away from the container.", player, false);
            var quest = dbPlayer.Quests[questId];

            string text = "Required Items: \n\n";

            foreach (var itemProgress in quest.ItemProgresses)
            {
                var itemName = Cache.GetItemNameByResref(itemProgress.Key);
                text += $"{itemProgress.Value}x {itemName}\n";
            }

            SendMessageToPC(player, text);
        }

        /// <summary>
        /// When an item collector placeable is closed, clear its inventory and destroy it.
        /// </summary>
        [NWNEventHandler("qst_collect_clsd")]
        public static void CloseItemCollector()
        {
            for (var item = GetFirstItemInInventory(OBJECT_SELF); GetIsObjectValid(item); item = GetNextItemInInventory(OBJECT_SELF))
            {
                DestroyObject(item);
            }

            DestroyObject(OBJECT_SELF);
        }

        /// <summary>
        /// When an item collector placeable is disturbed, 
        /// </summary>
        [NWNEventHandler("qst_collect_dist")]
        public static void DisturbItemCollector()
        {
            var type = GetInventoryDisturbType();
            if (type != DisturbType.Added) return;

            var container = OBJECT_SELF;
            var owner = GetLocalObject(container, "QUEST_OWNER");
            var player = GetLastDisturbed();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var item = GetInventoryDisturbItem();
            var resref = GetResRef(item);
            var questId = GetLocalString(container, "QUEST_ID");
            var quest = dbPlayer.Quests[questId];

            // Item not required, or all items have been turned in.
            if (!quest.ItemProgresses.ContainsKey(resref) ||
                quest.ItemProgresses[resref] <= 0)
            {
                Item.ReturnItem(player, item);
                SendMessageToPC(player, "That item is not required for this quest.");
                return;
            }

            // Decrement the required items and update the DB.
            dbPlayer.Quests[questId].ItemProgresses[resref]--;
            DB.Set(playerId, dbPlayer);

            // Attempt to advance the quest.
            // If player hasn't completed the other objectives, nothing will happen when this is called.
            Quest.AdvanceQuest(player, owner, questId);

            // Give the player an update and destroy the item.
            var itemName = Cache.GetItemNameByResref(resref);
            SendMessageToPC(player, $"You need {dbPlayer.Quests[questId].ItemProgresses[resref]}x {itemName} to complete this quest.");
            DestroyObject(item);

            // If no more items are necessary for this quest, force the player to speak with the NPC again.
            var itemsRequired = dbPlayer.Quests[questId].ItemProgresses.Sum(x => x.Value);

            if (itemsRequired <= 0)
            {
                AssignCommand(player, () => ActionStartConversation(owner, string.Empty, true, false));
            }
        }
    }
}
