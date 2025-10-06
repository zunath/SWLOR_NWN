using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Domain.Quest.Contracts;
using SWLOR.Shared.Domain.Quest.Events;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Server;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Quest.EventHandlers
{
    internal class QuestServiceEventHandlers
    {
        private readonly IGuildService _guildService;
        private readonly INPCGroupService _npcGroupService;
        private readonly IQuestService _questService;

        public QuestServiceEventHandlers(
            IGuildService guildService,
            INPCGroupService npcGroupService,
            IQuestService questService,
            IEventAggregator eventAggregator)
        {
            _guildService = guildService;
            _npcGroupService = npcGroupService;
            _questService = questService;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleEnter>(e => LoadPlayerQuests());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CacheNPCGroupData());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => LoadGuildData());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CacheData());
            eventAggregator.Subscribe<OnCreatureDeathBefore>(e => ProgressKillTargetObjectives());
            eventAggregator.Subscribe<OnQuestCollectOpen>(e => OpenItemCollector());
            eventAggregator.Subscribe<OnQuestCollectClosed>(e => CloseItemCollector());
            eventAggregator.Subscribe<OnQuestCollectDisturbed>(e => DisturbItemCollector());
            eventAggregator.Subscribe<OnQuestPlaceable>(e => UseQuestPlaceable());
            eventAggregator.Subscribe<OnQuestTrigger>(e => EnterQuestTrigger());
            eventAggregator.Subscribe<OnServerHeartbeat>(e => RefreshGuildTasks());
        }

        /// <summary>
        /// When a player enters the module, load their quests.
        /// </summary>
        public void LoadPlayerQuests()
        {
            _questService.LoadPlayerQuests();
        }

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        public void CacheNPCGroupData()
        {
            _npcGroupService.CacheData();
        }

        /// <summary>
        /// When the module caches, cache relevant data and load guild tasks.
        /// </summary>
        public void LoadGuildData()
        {
            _guildService.LoadData();
        }

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        public void CacheData()
        {
            _questService.CacheData();
        }

        /// <summary>
        /// When an NPC is killed, any objectives for quests a player currently has active will be updated.
        /// </summary>
        public void ProgressKillTargetObjectives()
        {
            _questService.ProgressKillTargetObjectives();
        }

        /// <summary>
        /// When an item collector placeable is opened, 
        /// </summary>
        public void OpenItemCollector()
        {
            _questService.OpenItemCollector();
        }

        /// <summary>
        /// When an item collector placeable is closed, clear its inventory and destroy it.
        /// </summary>
        public void CloseItemCollector()
        {
            _questService.CloseItemCollector();
        }

        /// <summary>
        /// When an item collector placeable is disturbed, 
        /// </summary>
        public void DisturbItemCollector()
        {
            _questService.DisturbItemCollector();
        }

        /// <summary>
        /// When a player uses a quest placeable, handle the progression.
        /// </summary>
        public void UseQuestPlaceable()
        {
            _questService.UseQuestPlaceable();
        }

        /// <summary>
        /// When a player enters a quest trigger, handle the progression.
        /// </summary>
        public void EnterQuestTrigger()
        {
            _questService.EnterQuestTrigger();
        }

        /// <summary>
        /// After quests are registered, refresh the available guild tasks.
        /// </summary>
        public void RefreshGuildTasks()
        {
            _questService.RefreshGuildTasks();
        }
    }
}
