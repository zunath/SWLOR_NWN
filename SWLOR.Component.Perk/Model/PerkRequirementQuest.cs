using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Perk.Model
{
    public class PerkRequirementQuest : IPerkRequirement
    {
        private readonly IDatabaseService _db;
        private readonly IQuestService _questService;
        private readonly string _questId;

        public PerkRequirementQuest(IDatabaseService db, IQuestService questService, string questId)
        {
            _db = db;
            _questService = questService;
            _questId = questId;
        }

        public string CheckRequirements(uint player)
        {
            var quest = _questService.GetQuestById(_questId);
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
                var quest = _questService.GetQuestById(_questId);
                return $"Quest: {quest.Name} Completed";
            }
        }
    }
}
