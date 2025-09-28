using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Model;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Quest.Contracts;
using SWLOR.Shared.Domain.Quest.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Quest;

namespace SWLOR.Component.Quest.Service
{
    public class QuestService : IQuestService
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventAggregator _eventAggregator;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IItemCacheService> _itemCache;
        private readonly Lazy<IGenericCacheService> _cacheService;
        private readonly Lazy<IEnmityService> _enmityService;
        private readonly Lazy<IActivityService> _activityService;
        private readonly Lazy<IRandomService> _randomService;
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<IPerkService> _perkService;
        
        // Cached data
        private IInterfaceCache<string, IQuestDetail> _questCache;
        
        // Additional caches for complex data
        private readonly Dictionary<NPCGroupType, List<string>> _npcsWithKillQuests = new();
        private readonly Dictionary<GuildType, Dictionary<int, List<IQuestDetail>>> _questsByGuildType = new();
        
        // Guild quest management
        public DateTime? DateTasksLoaded { get; private set; }
        private readonly Dictionary<GuildType, Dictionary<int, List<IQuestDetail>>> _activeGuildTasksByRank = new();
        private readonly Dictionary<GuildType, Dictionary<string, IQuestDetail>> _activeGuildTasks = new();

        public QuestService(
            IDatabaseService db,
            IServiceProvider serviceProvider,
            IEventAggregator eventAggregator)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _eventAggregator = eventAggregator;
            
            // Initialize lazy services
            _itemCache = new Lazy<IItemCacheService>(() => _serviceProvider.GetRequiredService<IItemCacheService>());
            _cacheService = new Lazy<IGenericCacheService>(() => _serviceProvider.GetRequiredService<IGenericCacheService>());
            _enmityService = new Lazy<IEnmityService>(() => _serviceProvider.GetRequiredService<IEnmityService>());
            _activityService = new Lazy<IActivityService>(() => _serviceProvider.GetRequiredService<IActivityService>());
            _randomService = new Lazy<IRandomService>(() => _serviceProvider.GetRequiredService<IRandomService>());
            _itemService = new Lazy<IItemService>(() => _serviceProvider.GetRequiredService<IItemService>());
            _perkService = new Lazy<IPerkService>(() => _serviceProvider.GetRequiredService<IPerkService>());
        }
        
        // Lazy-loaded services to break circular dependencies
        private IItemCacheService ItemCache => _itemCache.Value;
        private IGenericCacheService CacheService => _cacheService.Value;
        private IEnmityService EnmityService => _enmityService.Value;
        private IActivityService ActivityService => _activityService.Value;
        private IRandomService RandomService => _randomService.Value;
        private IItemService ItemService => _itemService.Value;
        private IPerkService PerkService => _perkService.Value;

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            RegisterQuests();
        }

        /// <summary>
        /// When the module loads, all quests will be retrieved with reflection and stored into a cache.
        /// </summary>
        public void RegisterQuests()
        {
            _questCache = CacheService.BuildInterfaceCache<IQuestListDefinition, string, IQuestDetail>()
                .WithDataExtractor(instance => instance.BuildQuests())
                .Build();

            // Process quests for additional caches
            foreach (var (questId, questDetail) in _questCache.AllItems)
            {
                // If any state has a Kill Target objective, add the NPC Group ID to the cache
                foreach (var state in questDetail.States)
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
                if (questDetail.GuildType != GuildType.Invalid &&
                    questDetail.GuildRank >= 0)
                {
                    if(!_questsByGuildType.ContainsKey(questDetail.GuildType))
                        _questsByGuildType[questDetail.GuildType] = new Dictionary<int, List<IQuestDetail>>();

                    if(!_questsByGuildType[questDetail.GuildType].ContainsKey(questDetail.GuildRank))
                        _questsByGuildType[questDetail.GuildType][questDetail.GuildRank] = new List<IQuestDetail>();

                    _questsByGuildType[questDetail.GuildType][questDetail.GuildRank].Add(questDetail);
                }
            }

            Console.WriteLine($"Loaded {_questCache.AllItems.Count} quests.");
            _eventAggregator.Publish(new OnQuestsRegistered(), GetModule());
        }

        /// <summary>
        /// Retrieves all quests associated with a guild.
        /// </summary>
        /// <param name="guild">The guild to search for</param>
        /// <param name="rank">The rank to search for</param>
        /// <returns>A list of quests associated with the guild.</returns>
        public List<IQuestDetail> GetQuestsByGuild(GuildType guild, int rank)
        {
            if(!_questsByGuildType.ContainsKey(guild))
                return new List<IQuestDetail>();

            return _questsByGuildType[guild][rank].ToList();
        }


        /// <summary>
        /// When a player enters the module, load their quests.
        /// </summary>
        public void LoadPlayerQuests()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId) ?? new Player(playerId);

            // Reapply quest journal entries on log-in.
            // An NWN quirk requires this to be on a short delay because journal entries are wiped on login.
            DelayCommand(0.5f, () =>
            {
                foreach (var (questId, playerQuest) in dbPlayer.Quests)
                {
                    var quest = _questCache.AllItems[questId];
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
            });
        }

        /// <summary>
        /// Retrieves a quest by its Id. If the quest has not been registered, a KeyNotFoundException will be thrown.
        /// </summary>
        /// <param name="questId">The quest Id to search for.</param>
        /// <returns>The quest detail matching this Id.</returns>
        public IQuestDetail GetQuestById(string questId)
        {
            return _questCache?.AllItems[questId] ?? throw new KeyNotFoundException($"Quest {questId} not found in cache");
        }

        /// <summary>
        /// Retrieves the quests associated with an NPC group.
        /// If no quests are associated with this NPC group, an empty list will be returned.
        /// </summary>
        /// <param name="npcGroupType">The NPC group to search for</param>
        /// <returns>A list of quests associated with an NPC group.</returns>
        public List<string> GetQuestsAssociatedWithNPCGroup(NPCGroupType npcGroupType)
        {
            if(!_npcsWithKillQuests.ContainsKey(npcGroupType))
                return new List<string>();

            return _npcsWithKillQuests[npcGroupType];
        }

        public void AbandonQuest(uint player, string questId)
        {
            _questCache.AllItems[questId].Abandon(player);
        }

        /// <summary>
        /// Makes a player accept a quest by the specified Id.
        /// If the quest Id is invalid, an exception will be thrown.
        /// </summary>
        /// <param name="player">The player who is accepting the quest</param>
        /// <param name="questId">The Id of the quest to accept.</param>
        public void AcceptQuest(uint player, string questId)
        {
            _questCache.AllItems[questId].Accept(player, OBJECT_SELF);
        }

        /// <summary>
        /// Makes a player advance to the next state of the quest.
        /// If there are no additional states, the quest will be treated as completed.
        /// </summary>
        /// <param name="player">The player who is advancing to the next state of the quest.</param>
        /// <param name="questSource">The source of the quest. Typically an NPC or object.</param>
        /// <param name="questId">The Id of the quest to advance.</param>
        public void AdvanceQuest(uint player, uint questSource, string questId)
        {
            _questCache.AllItems[questId].Advance(player, questSource);
        }

        /// <summary>
        /// Forces a player to open a collection placeable in which they will put items needed for the quest.
        /// </summary>
        /// <param name="player">The player who will open the collection placeable.</param>
        /// <param name="questId">The quest to collect items for.</param>
        public void RequestItemsFromPlayer(uint player, string questId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

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
        [ScriptHandler<OnCreatureDeathBefore>]
        public void ProgressKillTargetObjectives()
        {
            var creature = OBJECT_SELF;
            var npcGroupType = (NPCGroupType)GetLocalInt(creature, "QUEST_NPC_GROUP_ID");
            if (npcGroupType == NPCGroupType.Invalid) return;
            var possibleQuests = GetQuestsAssociatedWithNPCGroup(npcGroupType);
            if (possibleQuests.Count <= 0) return;

            // We can't use GetLastKiller() as various abilities deal damage that isn't sourced from
            // the PC.  So use the enmity service to pull the highest enmity PC (i.e. the one that 
            // did the most attacks).  If we can't find one for some reason, pull the nearest PC.
            // Note: this event needs to be called before the Enmity tables are cleared up after
            // creature death. 
            var killer = EnmityService.GetHighestEnmityTarget(creature);
            if (killer == OBJECT_INVALID) killer = GetNearestCreature(CreatureType.PlayerCharacter, 1, creature);

            // Iterate over every player in the killer's party.
            // Every player who needs this NPCGroupType for a quest will have their objective advanced if they are within range and in the same area.
            for (var member = GetFirstFactionMember(killer); GetIsObjectValid(member); member = GetNextFactionMember(killer))
            {
                if (!GetIsPC(member) || GetIsDM(member)) 
                    continue;

                if (GetArea(member) != GetArea(killer))
                    continue;

                if (GetDistanceBetween(member, creature) > 50f)
                    continue;

                var playerId = GetObjectUUID(member);
                var dbPlayer = _db.Get<Player>(playerId);

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
        [ScriptHandler<OnQuestCollectOpen>]
        public void OpenItemCollector()
        {
            var container = OBJECT_SELF;
            SetUseableFlag(container, false);

            var questId = GetLocalString(container, "QUEST_ID");
            var player = GetLastOpenedBy();
            var playerId = GetObjectUUID(player);

            var dbPlayer = _db.Get<Player>(playerId);

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
                var itemName = ItemCache.GetItemNameByResref(itemProgress.Key);
                text += $"{itemProgress.Value}x {itemName}\n";
            }

            SendMessageToPC(player, text);

            ActivityService.SetBusy(player, ActivityStatusType.Quest);
        }

        /// <summary>
        /// When an item collector placeable is closed, clear its inventory and destroy it.
        /// </summary>
        [ScriptHandler<OnQuestCollectClosed>]
        public void CloseItemCollector()
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

            ActivityService.ClearBusy(player);
        }

        /// <summary>
        /// When an item collector placeable is disturbed, 
        /// </summary>
        [ScriptHandler<OnQuestCollectDisturbed>]
        public void DisturbItemCollector()
        {
            var type = GetInventoryDisturbType();
            if (type != DisturbType.Added) return;

            var container = OBJECT_SELF;
            var owner = GetLocalObject(container, "QUEST_OWNER");
            var player = GetLastDisturbed();
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var item = GetInventoryDisturbItem();
            var resref = GetResRef(item);
            var questId = GetLocalString(container, "QUEST_ID");
            var quest = dbPlayer.Quests[questId];

            // Item not required, or all items have been turned in.
            if (!quest.ItemProgresses.ContainsKey(resref) ||
                quest.ItemProgresses[resref] <= 0)
            {
                ItemService.ReturnItem(player, item);
                SendMessageToPC(player, "That item is not required for this quest.");
                return;
            }

            var requiredAmount = dbPlayer.Quests[questId].ItemProgresses[resref];
            var stackSize = GetItemStackSize(item);

            // Decrement the required items and update the DB.
            if (stackSize > requiredAmount)
            {
                dbPlayer.Quests[questId].ItemProgresses[resref] = 0;
                ItemService.ReduceItemStack(item, requiredAmount);
                ItemService.ReturnItem(player, item);
            }
            else
            {
                dbPlayer.Quests[questId].ItemProgresses[resref] -= stackSize;
                ItemService.ReduceItemStack(item, stackSize);
            }

            _db.Set(dbPlayer);

            // Give the player an update and reduce the item stack.
            var itemName = ItemCache.GetItemNameByResref(resref);
            SendMessageToPC(player, $"You need {dbPlayer.Quests[questId].ItemProgresses[resref]}x {itemName} to complete this quest.");
            
            // Attempt to advance the quest.
            // If player hasn't completed the other objectives, nothing will happen when this is called.
            AdvanceQuest(player, owner, questId);

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
        [ScriptHandler<OnQuestPlaceable>]
        public void UseQuestPlaceable()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            TriggerAndPlaceableProgression(player, OBJECT_SELF);
        }

        /// <summary>
        /// When a player enters a quest trigger, handle the progression.
        /// </summary>
        [ScriptHandler<OnQuestTrigger>]
        public void EnterQuestTrigger()
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
        public void TriggerAndPlaceableProgression(uint player, uint triggerOrPlaceable)
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
            var dbPlayer = _db.Get<Player>(playerId);

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

        public int CalculateQuestGoldReward(uint player, bool isGuildQuest, int baseAmount)
        {
            // 5% credit bonus per social modifier.
            var social = GetAbilityModifier(AbilityType.Social, player) * 0.05f;

            // 5% credit bonus per Guild Relations perk level, if quest is associated with a guild.
            var guildRelations = 0f;
            if (isGuildQuest)
            {
                var perkLevel = PerkService.GetPerkLevel(player, PerkType.GuildRelations);
                guildRelations = perkLevel * 0.05f;
            }
            var amount = baseAmount +
                         (int)(baseAmount * social) +
                         (int)(baseAmount * guildRelations);

            return amount;
        }

        /// <summary>
        /// After quests are registered, refresh the available guild tasks.
        /// </summary>
        [ScriptHandler<OnQuestsRegistered>]
        public void RefreshGuildTasks()
        {
            if (DateTasksLoaded != null) return;

            // Get the max rank - using hardcoded value to avoid circular dependency
            // This should match the MaxRank in the Guild service
            var maxRank = 5;

            for (var rank = 0; rank < maxRank; rank++)
            {
                var guildTypes = Enum.GetValues(typeof(GuildType)).Cast<GuildType>();
                foreach (var guildType in guildTypes)
                {
                    var potentialTasks = GetQuestsByGuild(guildType, rank);
                    List<IQuestDetail> tasks;

                    // Need at least 11 tasks to randomize. We have ten or less. Simply enable all of these.
                    if (potentialTasks.Count <= 10)
                    {
                        tasks = potentialTasks;
                    }
                    // Pick 10 tasks randomly out of the potential list.
                    else
                    {
                        tasks = potentialTasks
                            .OrderBy(o => RandomService.Next())
                            .Take(10)
                            .ToList();
                    }

                    if (!_activeGuildTasks.ContainsKey(guildType))
                        _activeGuildTasks[guildType] = new Dictionary<string, IQuestDetail>();

                    if (!_activeGuildTasksByRank.ContainsKey(guildType))
                        _activeGuildTasksByRank[guildType] = new Dictionary<int, List<IQuestDetail>>();

                    foreach (var task in tasks)
                    {
                        _activeGuildTasks[guildType][task.QuestId] = task;
                    }

                    _activeGuildTasksByRank[guildType][rank] = tasks;
                }
            }

            DateTasksLoaded = DateTime.UtcNow;
        }

        /// <summary>
        /// Retrieves quest details associated with the active guild tasks by rank.
        /// </summary>
        /// <param name="guild">The guild type to retrieve for</param>
        /// <param name="rank">The rank to retrieve for</param>
        /// <returns>A list of active guild tasks</returns>
        public List<IQuestDetail> GetActiveGuildTasksByRank(GuildType guild, int rank)
        {
            if (!_activeGuildTasksByRank.ContainsKey(guild))
                return new List<IQuestDetail>();

            return _activeGuildTasksByRank[guild][rank].ToList();
        }

        /// <summary>
        /// Retrieves quest details associated with the active guild tasks.
        /// </summary>
        /// <param name="guild">The guild type to retrieve for</param>
        /// <returns>A list of active guild tasks</returns>
        public Dictionary<string, IQuestDetail> GetAllActiveGuildTasks(GuildType guild)
        {
            if (!_activeGuildTasks.ContainsKey(guild))
                return new Dictionary<string, IQuestDetail>();

            return _activeGuildTasks[guild].ToDictionary(x => x.Key, y => y.Value);
        }
    }
}
