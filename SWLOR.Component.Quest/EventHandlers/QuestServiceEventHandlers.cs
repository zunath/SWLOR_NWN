using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Service;
using SWLOR.Shared.Domain.Quest.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

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
    }
}
