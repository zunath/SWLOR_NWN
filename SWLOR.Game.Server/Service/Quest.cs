using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.QuestService;
using Player = SWLOR.Game.Server.Entity.Player;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Service
{
    public static class Quest
    {
        private static readonly Dictionary<string, QuestDetail> _quests = new();
        private static readonly Dictionary<NPCGroupType, List<string>> _npcsWithKillQuests = new();
        private static readonly Dictionary<GuildType, Dictionary<int, List<QuestDetail>>> _questsByGuildType = new();

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
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

            Console.WriteLine($"Loaded {_quests.Count} quests.");
            ExecuteScript(ScriptName.OnQuestsRegistered, GetModule());
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
        /// When a player enters the module, load their quests.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
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
                var staleQuestIds = new List<string>();

                foreach (var (questId, playerQuest) in dbPlayer.Quests)
                {
                    var quest = GetQuestByIdOrDefault(questId);

                    if (quest == null)
                    {
                        staleQuestIds.Add(questId);
                        Log.Write(LogGroup.Error, $"Player '{playerId}' has quest '{questId}' which is no longer registered. Removing it from their quest log.");
                        continue;
                    }

                    var state = quest.States[playerQuest.CurrentState];

                    PlayerPlugin.AddCustomJournalEntry(player, new JournalEntry
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

                if (staleQuestIds.Count > 0)
                {
                    foreach (var staleQuestId in staleQuestIds)
                    {
                        dbPlayer.Quests.Remove(staleQuestId);
                    }

                    DB.Set(dbPlayer);
                    SendMessageToPC(player, ColorToken.Red("One or more quests in your journal are no longer available (e.g. a cancelled or expired contract) and have been removed."));
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
        /// Retrieves a quest by its Id, or null if it has not been registered.
        /// Use this instead of <see cref="GetQuestById"/> when a missing quest is an expected, recoverable condition.
        /// </summary>
        /// <param name="questId">The quest Id to search for.</param>
        /// <returns>The quest detail matching this Id, or null if it isn't registered.</returns>
        public static QuestDetail GetQuestByIdOrDefault(string questId)
        {
            return _quests.TryGetValue(questId, out var quest) ? quest : null;
        }

        /// <summary>
        /// Registers (or replaces) a quest at runtime, outside of the reflection-based <see cref="RegisterQuests"/> pass.
        /// Used by systems which build quests dynamically, such as player-authored quest contracts.
        /// </summary>
        /// <param name="quest">The quest to register.</param>
        public static void RegisterRuntimeQuest(QuestDetail quest)
        {
            _quests[quest.QuestId] = quest;
        }

        /// <summary>
        /// Removes a runtime-registered quest so it can no longer be accepted, advanced, or completed.
        /// Players who have already accepted the quest are unaffected until <see cref="LoadPlayerQuests"/> hardens their journal.
        /// </summary>
        /// <param name="questId">The Id of the quest to remove.</param>
        public static void UnregisterRuntimeQuest(string questId)
        {
            _quests.Remove(questId);
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
        /// Marks a quest as completed for a player without granting rewards or running completion
        /// actions. Used by DM tooling to open quest-gated content such as capstone perk unlocks.
        /// If the quest Id is invalid, an exception will be thrown.
        /// </summary>
        /// <param name="player">The player whose quest record is marked complete.</param>
        /// <param name="questId">The Id of the quest to mark complete.</param>
        public static void ForceCompleteQuest(uint player, string questId)
        {
            _quests[questId].ForceComplete(player);
        }

        /// <summary>
        /// Makes a player accept a quest by the specified Id.
        /// If the quest Id is invalid, an exception will be thrown.
        /// </summary>
        /// <param name="player">The player who is accepting the quest</param>
        /// <param name="questId">The Id of the quest to accept.</param>
        public static bool AcceptQuest(uint player, string questId)
        {
            return AcceptQuest(player, OBJECT_SELF, questId);
        }

        /// <summary>
        /// Makes a player accept a quest while preserving the creature or placeable that offered it.
        /// Conversation callbacks attached to the quest run against this source.
        /// </summary>
        public static bool AcceptQuest(uint player, uint questSource, string questId)
        {
            return _quests[questId].Accept(player, questSource);
        }

        public static bool CanAcceptQuest(uint player, string questId)
        {
            return _quests[questId].CanAccept(player, false);
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
            return _quests[questId].Advance(player, questSource);
        }

        /// <summary>
        /// Forces a player to open a collection placeable in which they will put items needed for the quest.
        /// </summary>
        /// <param name="player">The player who will open the collection placeable.</param>
        /// <param name="questId">The quest to collect items for.</param>
        public static bool RequestItemsFromPlayer(uint player, string questId)
        {
            return RequestItemsFromPlayer(player, OBJECT_SELF, questId);
        }

        /// <summary>
        /// Opens the quest item collector and records the conversation object that owns the hand-in.
        /// </summary>
        public static bool RequestItemsFromPlayer(uint player, uint questSource, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.Quests.ContainsKey(questId))
            {
                SendMessageToPC(player, "You have not accepted this quest yet.");
                return false;
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
                return false;
            }

            var collector = CreateObject(ObjectType.Placeable, "qst_item_collect", GetLocation(player));
            SetLocalObject(collector, "QUEST_OWNER", questSource);
            SetLocalString(collector, "QUEST_ID", questId);

            AssignCommand(collector, () => SetFacingPoint(GetPosition(player)));
            AssignCommand(player, () => ActionInteractObject(collector));

            // Collectors are destroyed when the final item is turned in, but a player can open one
            // and walk away - sweep it up after a few minutes so abandoned collectors don't litter
            // the area until reboot. Turned-in items are consumed immediately, so nothing is lost.
            DelayCommand(300f, () =>
            {
                if (GetIsObjectValid(collector))
                    DestroyObject(collector);
            });

            return true;
        }

        /// <summary>
        /// When an NPC is killed, any objectives for quests a player currently has active will be updated.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void ProgressKillTargetObjectives()
        {
            var creature = OBJECT_SELF;
            var npcGroupType = (NPCGroupType)GetLocalInt(creature, "QUEST_NPC_GROUP_ID");
            if (npcGroupType == NPCGroupType.Invalid) return;
            var possibleQuests = GetQuestsAssociatedWithNPCGroup(npcGroupType);
            if (possibleQuests.Count <= 0) return;

            if (QuestEncounter.IsQuestEncounterCreature(creature))
            {
                QuestEncounter.ProgressKillCredit(creature, npcGroupType, possibleQuests);
                QuestEncounter.ClearEncounterForCreature(creature);
                return;
            }

            // We can't use GetLastKiller() as various abilities deal damage that isn't sourced from
            // the PC.  So use the enmity service to pull the highest enmity PC (i.e. the one that
            // did the most attacks).  If we can't find one for some reason, pull the nearest PC.
            // Note: this event needs to be called before the Enmity tables are cleared up after
            // creature death.
            var killer = Enmity.GetHighestEnmityTarget(creature);
            if (killer == OBJECT_INVALID) killer = GetNearestCreature(CreatureType.PlayerCharacter, 1, creature);

            // Iterate over every player in the killer's party.
            // Every player who needs this NPCGroupType for a quest will have their objective advanced if they are within range and in the same area.
            for (var member = GetFirstFactionMember(killer); GetIsObjectValid(member); member = GetNextFactionMember(killer))
            {
                if (!GetIsPC(member) ||
                    GetIsDM(member) ||
                    GetIsDead(member) ||
                    GetCurrentHitPoints(member) <= 0)
                    continue;

                if (GetArea(member) != GetArea(killer))
                    continue;

                if (GetDistanceBetween(member, creature) > 50f)
                    continue;

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
        [NWNEventHandler(ScriptName.OnQuestCollectOpen)]
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

            Activity.SetBusy(player, ActivityStatusType.Quest);
        }

        /// <summary>
        /// When an item collector placeable is closed, clear its inventory and destroy it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestCollectClosed)]
        public static void CloseItemCollector()
        {
            var player = GetLastClosedBy();
            DelayCommand(0.02f, () =>
            {
                for (var item = GetFirstItemInInventory(OBJECT_SELF); GetIsObjectValid(item); item = GetNextItemInInventory(OBJECT_SELF))
                {
                    DestroyObject(item);
                }

                DestroyObject(OBJECT_SELF);
            });

            Activity.ClearBusy(player);
        }

        /// <summary>
        /// When an item collector placeable is disturbed,
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestCollectDisturbed)]
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

            var questDetail = GetQuestByIdOrDefault(questId);

            // The quest was unregistered while the collector was open (e.g. a quest contract which was
            // fulfilled by another player or taken down). Return the item rather than consuming it.
            if (questDetail == null)
            {
                Item.ReturnItem(player, item);
                SendMessageToPC(player, "This quest is no longer available.");
                return;
            }

            var questState = questDetail.States[quest.CurrentState];
            var collectItemObjective = questState.GetObjectives()
                .OfType<CollectItemObjective>()
                .FirstOrDefault(x => x.Resref == resref);

            if (collectItemObjective != null)
            {
                var rejection = collectItemObjective.GetCollectTurnInRejectionMessage(player, item);
                if (rejection != string.Empty)
                {
                    Item.ReturnItem(player, item);
                    SendMessageToPC(player, rejection);
                    return;
                }
            }

            var requiredAmount = dbPlayer.Quests[questId].ItemProgresses[resref];
            var stackSize = GetItemStackSize(item);

            // If a handler is configured, give it a chance to reroute the items that are actually being
            // consumed this turn-in before they're reduced/destroyed below. Only the consumed portion of the
            // stack is handed off; if part of the stack is being returned to the player, a scoped copy sized
            // to the consumed amount is used instead so the returned portion isn't duplicated into the handler.
            if (questDetail.CollectedItemHandler != null)
            {
                var consumedAmount = Math.Min(stackSize, requiredAmount);
                var isFullStackConsumed = stackSize <= requiredAmount;
                var consumedItem = isFullStackConsumed ? item : CopyItem(item, container, true);

                if (!isFullStackConsumed)
                    SetItemStackSize(consumedItem, consumedAmount);

                questDetail.CollectedItemHandler.Invoke(player, consumedItem);

                if (!isFullStackConsumed)
                    DestroyObject(consumedItem);
            }

            // Decrement the required items and update the DB.
            if (stackSize > requiredAmount)
            {
                dbPlayer.Quests[questId].ItemProgresses[resref] = 0;
                Item.ReduceItemStack(item, requiredAmount);
                Item.ReturnItem(player, item);
            }
            else
            {
                dbPlayer.Quests[questId].ItemProgresses[resref] -= stackSize;
                Item.ReduceItemStack(item, stackSize);
            }

            DB.Set(dbPlayer);

            // Give the player an update and reduce the item stack.
            var itemName = Cache.GetItemNameByResref(resref);
            SendMessageToPC(player, $"You need {dbPlayer.Quests[questId].ItemProgresses[resref]}x {itemName} to complete this quest.");

            // Attempt to advance the quest.
            // If player hasn't completed the other objectives, nothing will happen when this is called.
            AdvanceQuest(player, owner, questId);

            // If no more items are necessary for this quest, force the player to speak with the NPC again.
            // Quests turned in without an NPC (e.g. quest contracts started from a board's NUI, where the
            // collector's owner is the module object) complete inline during AdvanceQuest above - starting
            // a conversation with a non-creature owner hard-crashes the server.
            var itemsRequired = dbPlayer.Quests[questId].ItemProgresses.Sum(x => x.Value);

            if (itemsRequired <= 0)
            {
                if (GetIsObjectValid(owner) && GetObjectType(owner) == ObjectType.Creature)
                {
                    if (!Conversation.TryStartAssigned(player, owner))
                        AssignCommand(player, () => ActionStartConversation(owner, string.Empty, true, false));
                }

                // The collector has served its purpose - destroy it so it doesn't linger on the
                // ground (closing the player's open container view in the process).
                DestroyObject(container);
            }
        }

        /// <summary>
        /// When a player uses a quest placeable, handle the progression.
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestPlaceable)]
        public static void UseQuestPlaceable()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            TriggerAndPlaceableProgression(player, OBJECT_SELF);
        }

        /// <summary>
        /// When a player enters a quest trigger, handle the progression.
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestTrigger)]
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

        public static int CalculateQuestGoldReward(uint player, bool isGuildQuest, int baseAmount)
        {
            // 5% credit bonus per social modifier.
            var social = GetAbilityModifier(AbilityType.Social, player) * 0.05f;

            // 5% credit bonus per Guild Relations perk level, if quest is associated with a guild.
            var guildRelations = 0f;
            if (isGuildQuest)
            {
                var perkLevel = Perk.GetPerkLevel(player, PerkType.GuildRelations);
                guildRelations = perkLevel * 0.05f;
            }
            var amount = baseAmount +
                         (int)(baseAmount * social) +
                         (int)(baseAmount * guildRelations);

            return amount;
        }
    }
}
