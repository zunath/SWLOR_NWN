using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Domain.Quest.Contracts;
using SWLOR.Shared.Domain.Quest.Events;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Creature;

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
            IQuestService questService)
        {
            _guildService = guildService;
            _npcGroupService = npcGroupService;
            _questService = questService;
        }

        /// <summary>
        /// When a player enters the module, load their quests.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void LoadPlayerQuests()
        {
            _questService.LoadPlayerQuests();
        }

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheNPCGroupData()
        {
            _npcGroupService.CacheData();
        }

        /// <summary>
        /// When the module caches, cache relevant data and load guild tasks.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadGuildData()
        {
            _guildService.LoadData();
        }

        /// <summary>
        /// When the module loads, data is cached to speed up searches later.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            _questService.CacheData();
        }

        /// <summary>
        /// When an NPC is killed, any objectives for quests a player currently has active will be updated.
        /// </summary>
        [ScriptHandler<OnCreatureDeathBefore>]
        public void ProgressKillTargetObjectives()
        {
            _questService.ProgressKillTargetObjectives();
        }

        /// <summary>
        /// When an item collector placeable is opened, 
        /// </summary>
        [ScriptHandler<OnQuestCollectOpen>]
        public void OpenItemCollector()
        {
            _questService.OpenItemCollector();
        }

        /// <summary>
        /// When an item collector placeable is closed, clear its inventory and destroy it.
        /// </summary>
        [ScriptHandler<OnQuestCollectClosed>]
        public void CloseItemCollector()
        {
            _questService.CloseItemCollector();
        }

        /// <summary>
        /// When an item collector placeable is disturbed, 
        /// </summary>
        [ScriptHandler<OnQuestCollectDisturbed>]
        public void DisturbItemCollector()
        {
            _questService.DisturbItemCollector();
        }

        /// <summary>
        /// When a player uses a quest placeable, handle the progression.
        /// </summary>
        [ScriptHandler<OnQuestPlaceable>]
        public void UseQuestPlaceable()
        {
            _questService.UseQuestPlaceable();
        }

        /// <summary>
        /// When a player enters a quest trigger, handle the progression.
        /// </summary>
        [ScriptHandler<OnQuestTrigger>]
        public void EnterQuestTrigger()
        {
            _questService.EnterQuestTrigger();
        }

        /// <summary>
        /// After quests are registered, refresh the available guild tasks.
        /// </summary>
        [ScriptHandler<OnQuestsRegistered>]
        public void RefreshGuildTasks()
        {
            _questService.RefreshGuildTasks();
        }
    }
}
