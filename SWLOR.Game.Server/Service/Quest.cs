﻿using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.QuestService;
using Player = SWLOR.Game.Server.Entity.Player;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Quest
    {
        private static readonly Dictionary<string, QuestDetail> _quests = new Dictionary<string, QuestDetail>();
        private static readonly Dictionary<NPCGroupType, NPCGroupAttribute> _npcGroups = new Dictionary<NPCGroupType, NPCGroupAttribute>();
        private static readonly Dictionary<NPCGroupType, List<string>> _npcsWithKillQuests = new Dictionary<NPCGroupType, List<string>>();
        private static readonly Dictionary<GuildType, Dictionary<int, List<QuestDetail>>> _questsByGuildType = new Dictionary<GuildType, Dictionary<int, List<QuestDetail>>>();

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            RegisterNPCGroups();
            RegisterQuests();
        }

        /// <summary>
        /// When the module loads, all quests will be retrieved with reflection and stored into a cache.
        /// </summary>
        public static void RegisterQuests()
        {
            // Organize quests to make later reads quicker.
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IQuestListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IQuestListDefinition) Activator.CreateInstance(type);
                var quests = instance.BuildQuests();

                foreach (var (questId, quest) in quests)
                {
                    _quests[questId] = quest;

                    // If any state has a Kill Target objective, add the NPC Group ID to the cache
                    foreach (var state in quest.States)
                    {
                        foreach (var objective in state.Value.GetObjectives())
                        {
                            if (objective is KillTargetObjective killObjective)
                            {
                                if(!_npcsWithKillQuests.ContainsKey(killObjective.Group))
                                    _npcsWithKillQuests[killObjective.Group] = new List<string>();

                                if(!_npcsWithKillQuests[killObjective.Group].Contains(questId))
                                    _npcsWithKillQuests[killObjective.Group].Add(questId);
                            }
                        }
                    }

                    // If the quest is associated with a guild, add it to that guild's list.
                    if (quest.GuildType != GuildType.Invalid &&
                        quest.GuildRank >= 0)
                    {
                        if(!_questsByGuildType.ContainsKey(quest.GuildType))
                            _questsByGuildType[quest.GuildType] = new Dictionary<int, List<QuestDetail>>();

                        if(!_questsByGuildType[quest.GuildType].ContainsKey(quest.GuildRank))
                            _questsByGuildType[quest.GuildType][quest.GuildRank] = new List<QuestDetail>();

                        _questsByGuildType[quest.GuildType][quest.GuildRank].Add(quest);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves all quests associated with a guild.
        /// </summary>
        /// <param name="guild">The guild to search for</param>
        /// <param name="rank">The rank to search for</param>
        /// <returns>A list of quests associated with the guild.</returns>
        public static List<QuestDetail> GetQuestsByGuild(GuildType guild, int rank)
        {
            if(!_questsByGuildType.ContainsKey(guild))
                return new List<QuestDetail>();

            return _questsByGuildType[guild][rank].ToList();
        }

        /// <summary>
        /// When the module loads, all of the NPCGroupTypes are iterated over and their data is stored into the cache.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void RegisterNPCGroups()
        {
            var npcGroups = Enum.GetValues(typeof(NPCGroupType)).Cast<NPCGroupType>();
            foreach (var npcGroupType in npcGroups)
            {
                var npcGroupDetail = npcGroupType.GetAttribute<NPCGroupType, NPCGroupAttribute>();
                _npcGroups[npcGroupType] = npcGroupDetail;
            }
        }

        /// <summary>
        /// When a player enters the module, load their quests.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void LoadPlayerQuests()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);

            // Reapply quest journal entries on log-in.
            // An NWN quirk requires this to be on a short delay because journal entries are wiped on login.
            DelayCommand(0.5f, () =>
            {
                foreach (var (questId, playerQuest) in dbPlayer.Quests)
                {
                    var quest = _quests[questId];
                    var state = quest.States[playerQuest.CurrentState];

                    Core.NWNX.PlayerPlugin.AddCustomJournalEntry(player, new JournalEntry
                    {
                        Name = quest.Name,
                        Text = state.JournalText,
                        Tag = questId,
                        State = playerQuest.CurrentState,
                        Priority = 1,
                        IsQuestCompleted = false,
                        IsQuestDisplayed = true,
                        Updated = 0,
                        CalendarDay = GetCalendarDay(),
                        TimeOfDay = GetTimeHour()
                    }, true);
                }
            });
        }

        /// <summary>
        /// Retrieves a quest by its Id. If the quest has not been registered, a KeyNotFoundException will be thrown.
        /// </summary>
        /// <param name="questId">The quest Id to search for.</param>
        /// <returns>The quest detail matching this Id.</returns>
        public static QuestDetail GetQuestById(string questId)
        {
            if(!_quests.ContainsKey(questId))
                throw new KeyNotFoundException($"Quest '{questId}' was not registered. Did you set the right Id?");

            return _quests[questId];
        }

        /// <summary>
        /// Retrieves an NPC group detail by the type.
        /// </summary>
        /// <param name="npcGroupType">The type of NPC group to retrieve.</param>
        /// <returns>An NPC group detail</returns>
        public static NPCGroupAttribute GetNPCGroup(NPCGroupType npcGroupType)
        {
            return _npcGroups[npcGroupType];
        }

        /// <summary>
        /// Retrieves the quests associated with an NPC group.
        /// If no quests are associated with this NPC group, an empty list will be returned.
        /// </summary>
        /// <param name="npcGroupType">The NPC group to search for</param>
        /// <returns>A list of quests associated with an NPC group.</returns>
        public static List<string> GetQuestsAssociatedWithNPCGroup(NPCGroupType npcGroupType)
        {
            if(!_npcsWithKillQuests.ContainsKey(npcGroupType))
                return new List<string>();

            return _npcsWithKillQuests[npcGroupType];
        }

        public static void AbandonQuest(uint player, string questId)
        {
            _quests[questId].Abandon(player);
        }

        /// <summary>
        /// Makes a player accept a quest by the specified Id.
        /// If the quest Id is invalid, an exception will be thrown.
        /// </summary>
        /// <param name="player">The player who is accepting the quest</param>
        /// <param name="questId">The Id of the quest to accept.</param>
        public static void AcceptQuest(uint player, string questId)
        {
            _quests[questId].Accept(player, OBJECT_SELF);
        }

        /// <summary>
        /// Makes a player advance to the next state of the quest.
        /// If there are no additional states, the quest will be treated as completed.
        /// </summary>
        /// <param name="player">The player who is advancing to the next state of the quest.</param>
        /// <param name="questSource">The source of the quest. Typically an NPC or object.</param>
        /// <param name="questId">The Id of the quest to advance.</param>
        public static void AdvanceQuest(uint player, uint questSource, string questId)
        {
            _quests[questId].Advance(player, questSource);
        }

        /// <summary>
        /// Forces a player to open a collection placeable in which they will put items needed for the quest.
        /// </summary>
        /// <param name="player">The player who will open the collection placeable.</param>
        /// <param name="questId">The quest to collect items for.</param>
        public static void RequestItemsFromPlayer(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.Quests.ContainsKey(questId))
            {
                SendMessageToPC(player, "You have not accepted this quest yet.");
                return;
            }

            var quest = dbPlayer.Quests[questId];
            var questDetail = GetQuestById(questId);
            var questState = questDetail.States[quest.CurrentState];

            // Ensure there's at least one "Collect Item" objective on this quest state.
            var hasCollectItemObjective = questState.GetObjectives().OfType<CollectItemObjective>().Any();

            // The only time this should happen is if the quest is misconfigured.
            if (!hasCollectItemObjective)
            {
                SendMessageToPC(player, "There are no items to turn in for this quest. This is likely a bug. Please let the staff know.");
                return;
            }

            var collector = CreateObject(ObjectType.Placeable, "qst_item_collect", GetLocation(player));
            SetLocalObject(collector, "QUEST_OWNER", OBJECT_SELF);
            SetLocalString(collector, "QUEST_ID", questId);

            AssignCommand(collector, () => SetFacingPoint(GetPosition(player)));
            AssignCommand(player, () => ActionInteractObject(collector));
        }

        /// <summary>
        /// When an NPC is killed, any objectives for quests a player currently has active will be updated.
        /// </summary>
        [NWNEventHandler("crea_death_bef")]
        public static void ProgressKillTargetObjectives()
        {
            var creature = OBJECT_SELF;
            var npcGroupType = (NPCGroupType)GetLocalInt(creature, "QUEST_NPC_GROUP_ID");
            if (npcGroupType == NPCGroupType.Invalid) return;
            var possibleQuests = GetQuestsAssociatedWithNPCGroup(npcGroupType);
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
                    var questDetail = GetQuestById(questId);
                    var questState = questDetail.States[quest.CurrentState];
                    var killRequiredForQuestAndState = false;

                    // Iterate over all of the quest states which call for killing this enemy.
                    foreach (var objective in questState.GetObjectives())
                    {
                        // Only kill target objectives matching this NPC group ID are processed.
                        if (objective is KillTargetObjective killTargetObjective)
                        {
                            if (killTargetObjective.Group != npcGroupType) continue;

                            killRequiredForQuestAndState = true;
                            killTargetObjective.Advance(member, questId);
                        }
                    }

                    // Attempt to advance the quest detail. It's possible this will fail because objectives aren't all done. This is OK.
                    if (killRequiredForQuestAndState)
                    {
                        questDetail.Advance(member, creature);
                    }
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
            DB.Set(dbPlayer);

            // Attempt to advance the quest.
            // If player hasn't completed the other objectives, nothing will happen when this is called.
            AdvanceQuest(player, owner, questId);

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

        /// <summary>
        /// When a player uses a quest placeable, handle the progression.
        /// </summary>
        [NWNEventHandler("quest_placeable")]
        public static void UseQuestPlaceable()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            TriggerAndPlaceableProgression(player, OBJECT_SELF);
        }

        /// <summary>
        /// When a player enters a quest trigger, handle the progression.
        /// </summary>
        [NWNEventHandler("quest_trigger")]
        public static void EnterQuestTrigger()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            TriggerAndPlaceableProgression(player, OBJECT_SELF);
        }


        /// <summary>
        /// Handles advancing a player's quest when they enter a trigger or click a quest placeable.
        /// Trigger or placeable must have both QUEST_ID (string) and QUEST_STATE (int) set in order for this to work, otherwise an error will be raised.
        /// </summary>
        /// <param name="player">The player who entered the trigger or clicked a placeable.</param>
        /// <param name="triggerOrPlaceable">The trigger or placeable</param>
        public static void TriggerAndPlaceableProgression(uint player, uint triggerOrPlaceable)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            var questMessage = GetLocalString(triggerOrPlaceable, "QUEST_MESSAGE");
            var questId = GetLocalString(triggerOrPlaceable, "QUEST_ID");
            var questState = GetLocalInt(triggerOrPlaceable, "QUEST_STATE");

            if (string.IsNullOrWhiteSpace(questId))
            {
                SendMessageToPC(player, "QUEST_ID variable not set on object. Please inform admin this quest is bugged. (QuestID: " + questId + ")");
                return;
            }

            if (questState <= 0)
            {
                SendMessageToPC(player, "QUEST_STATE variable not set on object. Please inform admin this quest is bugged. (QuestID: " + questId + ")");
                return;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.Quests.ContainsKey(questId)) return;

            var dbQuest = dbPlayer.Quests[questId];

            if (dbQuest.CurrentState != questState)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(questMessage))
            {
                DelayCommand(1.0f, () =>
                {
                    SendMessageToPC(player, questMessage);
                });
            }

            var quest = GetQuestById(questId);
            quest.Advance(player, triggerOrPlaceable);
        }
    }
}
