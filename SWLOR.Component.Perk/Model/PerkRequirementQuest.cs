using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Perk.Model
{
    public class PerkRequirementQuest : IPerkRequirement
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _questId;
        
        // Lazy-loaded services to break circular dependencies
        private IQuestService QuestService => _serviceProvider.GetRequiredService<IQuestService>();

        public PerkRequirementQuest(IDatabaseService db, IServiceProvider serviceProvider, string questId)
        {
            _db = db;
            // Services are now lazy-loaded via IServiceProvider
            _questId = questId;
        }

        public string CheckRequirements(uint player)
        {
            var quest = QuestService.GetQuestById(_questId);
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var error = $"You have not completed the quest '{quest.Name}'.";

            if (!dbPlayer.Quests.ContainsKey(_questId)) return error;

            var playerQuest = dbPlayer.Quests[_questId];
            if (playerQuest.TimesCompleted <= 0) return error;

            return string.Empty;
        }

        public string RequirementText
        {
            get
            {
                var quest = QuestService.GetQuestById(_questId);
                return $"Quest: {quest.Name} Completed";
            }
        }
    }
}
