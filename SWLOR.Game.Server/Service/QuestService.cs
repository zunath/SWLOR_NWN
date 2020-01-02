using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Creature;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Quest.Objective;
using SWLOR.Game.Server.Scripting;
using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class QuestService
    {
        private static readonly Dictionary<int, IQuest> _quests = new Dictionary<int, IQuest>();
        private const string TempStoragePlaceableTag = "QUEST_BARREL";

        public static void SubscribeEvents()
        {
            // Creature Events
            MessageHub.Instance.Subscribe<OnCreatureDeath>(message => OnCreatureDeath());

            // Module Events
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
            
            // Scripting Events
            MessageHub.Instance.Subscribe<OnQuestLoaded>(message => LoadQuest(message.Quest.Quest));
        }

        private static void LoadQuest(IQuest quest)
        {
            if (_quests.ContainsKey(quest.QuestID))
            {
                throw new Exception("Quest with ID " + quest.QuestID + " has already been registered. IDs must be unique across all quests.");
            }

            _quests[quest.QuestID] = quest;
            Console.WriteLine("Registered quest: " + quest.Name + " ( " + quest.QuestID + " )");
        }

        /// <summary>
        /// Retrieves a quest from the cache by its ID.
        /// </summary>
        /// <param name="questID">The key of the quest to retrieve.</param>
        /// <returns>The quest with the provided quest ID.</returns>
        public static IQuest GetQuestByID(int questID)
        {
            return _quests[questID];
        }

        public static bool QuestExistsByID(int questID)
        {
            return _quests.ContainsKey(questID);
        }

        /// <summary>
        /// Creates an item temporarily and then returns its details. 
        /// </summary>
        /// <param name="resref">The resref of the item to create.</param>
        /// <param name="quantity">The number to create.</param>
        /// <returns></returns>
        public static ItemVO GetTempItemInformation(string resref, int quantity)
        {
            NWPlaceable tempStorage = GetObjectByTag(TempStoragePlaceableTag);
            NWItem tempItem = CreateItemOnObject(resref, tempStorage.Object);
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
        /// Rebuilds players' quests on module entry.
        /// Only incomplete quests will be loaded.
        /// </summary>
        private static void OnModuleEnter()
        {
            NWPlayer oPC = GetEnteringObject();

            if (!oPC.IsPlayer) return;

            List<PCQuestStatus> pcQuests = DataService.PCQuestStatus.GetAllByPlayerID(oPC.GlobalID).Where(x => x.CompletionDate == null).ToList();

            foreach (PCQuestStatus pcQuest in pcQuests)
            {
                var quest = _quests[pcQuest.QuestID];
                AddJournalQuestEntry(quest.JournalTag, pcQuest.QuestState, oPC.Object, false);
            }
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

            NWObject oKiller = GetLastKiller();

            if (!oKiller.IsPlayer) return;

            string areaResref = creature.Area.Resref;

            List<KeyValuePair<NWPlayer, int>> playersToAdvance = new List<KeyValuePair<NWPlayer, int>>();
            NWPlayer oPC = GetFirstFactionMember(oKiller);
            while (oPC.IsValid)
            {
                if (areaResref != oPC.Area.Resref)
                {
                    oPC = GetNextFactionMember(oKiller);
                    continue;
                }

                if (GetDistanceBetween(creature, oPC) <= 0.0f || GetDistanceBetween(creature, oPC) > 20.0f)
                {
                    oPC = GetNextFactionMember(oKiller);
                    continue;
                }

                var playerID = oPC.GlobalID;
                var killTargets = DataService.PCQuestKillTargetProgress.GetAllByPlayerIDAndNPCGroupID(playerID, npcGroupID).ToList();

                foreach (var kt in killTargets)
                {
                    var questStatus = DataService.PCQuestStatus.GetByID(kt.PCQuestStatusID);
                    var quest = GetQuestByID(questStatus.QuestID);
                    var npcGroup = DataService.NPCGroup.GetByID(kt.NPCGroupID);

                    kt.RemainingToKill--;
                    string targetGroupName = npcGroup.Name;
                    string updateMessage = "[" + quest.Name + "] " + targetGroupName + " remaining: " + kt.RemainingToKill;
                    DatabaseActionType action = DatabaseActionType.Update;

                    if (kt.RemainingToKill <= 0)
                    {
                        updateMessage += " " + ColorTokenService.Green(" {COMPLETE}");
                        playersToAdvance.Add(new KeyValuePair<NWPlayer, int>(oPC, quest.QuestID));
                        action = DatabaseActionType.Delete;
                    }

                    DataService.SubmitDataChange(kt, action);

                    var pc = oPC;
                    DelayCommand(1.0f, () =>
                    {
                        pc.SendMessage(updateMessage);
                    });
                }

                oPC = GetNextFactionMember(oKiller);
            }

            foreach (var player in playersToAdvance)
            {
                var quest = GetQuestByID(player.Value);
                quest.Advance(player.Key, creature);
            }
        }

        /// <summary>
        /// Progresses a player to the next state if they meet all requirements to do so.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="trigger">The trigger or placeable being used/entered.</param>
        private static void HandleTriggerAndPlaceableQuestLogic(NWPlayer player, NWObject trigger)
        {
            if (!player.IsPlayer) return;
            string questMessage = trigger.GetLocalString("QUEST_MESSAGE");
            int questID = trigger.GetLocalInt("QUEST_ID");
            int questState = trigger.GetLocalInt("QUEST_STATE");
            string visibilityObjectID = trigger.GetLocalString("VISIBILITY_OBJECT_ID");

            if (questID <= 0)
            {
                player.SendMessage("QUEST_ID variable not set on object. Please inform admin this quest is bugged. (QuestID: " + questID + ")");
                return;
            }

            if (questState <= 0)
            {
                player.SendMessage("QUEST_STATE variable not set on object. Please inform admin this quest is bugged. (QuestID: " + questID + ")");
                return;
            }

            PCQuestStatus pcQuestStatus = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);
            if (pcQuestStatus == null) return;


            if (pcQuestStatus.QuestState != questState)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(questMessage))
            {
                DelayCommand(1.0f, () =>
                {
                    player.SendMessage(questMessage);
                });
            }

            var quest = GetQuestByID(questID);
            quest.Advance(player, trigger);

            if (!string.IsNullOrWhiteSpace(visibilityObjectID))
            {
                ObjectVisibilityService.AdjustVisibility(player, trigger, false);
            }
        }

        /// <summary>
        /// Handles updating quest state when a placeable is used by the player and
        /// the player is on a matching quest.
        /// </summary>
        /// <param name="oObject"></param>
        public static void OnQuestPlaceableUsed(NWObject oObject)
        {
            NWPlayer oPC = GetLastUsedBy();
            HandleTriggerAndPlaceableQuestLogic(oPC, oObject);
        }

        /// <summary>
        /// Handles updating quest state when a trigger is entered by a player and
        /// the player is on a matching quest.
        /// </summary>
        /// <param name="oObject"></param>
        public static void OnQuestTriggerEntered(NWObject oObject)
        {
            NWPlayer oPC = GetEnteringObject();
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

            var quest = GetQuestByID(questID);
            var questState = quest.GetState(pcStatus.QuestState);
            var collectKeyItemObjectives = questState.GetObjectives().Where(x => x.GetType() == typeof(CollectKeyItemObjective)).Cast<CollectKeyItemObjective>();

            foreach (var ki in collectKeyItemObjectives)
            {
                if (!KeyItemService.PlayerHasKeyItem(oPC, ki.KeyItemID))
                {
                    oPC.SendMessage("You are missing a required key item.");
                    return;
                }
            }

            Location location = oPC.Location;
            NWPlaceable collector = CreateObject(ObjectType.Placeable, "qst_item_collect", location);
            collector.SetLocalObject("QUEST_OWNER", questOwner);

            collector.AssignCommand(() =>
            {
                SetFacingPoint(oPC.Position);
            });
            collector.SetLocalInt("QUEST_ID", questID);

            oPC.AssignCommand(() =>
            {
                ActionInteractObject(collector.Object);
            });
        }
    }
}
