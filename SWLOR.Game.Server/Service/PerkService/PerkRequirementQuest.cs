using SWLOR.Game.Server.Entity;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementQuest : IPerkRequirement
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private readonly string _questId;

        public PerkRequirementQuest(string questId)
        {
            _questId = questId;
        }

        public string CheckRequirements(uint player)
        {
            var quest = Quest.GetQuestById(_questId);
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
                var quest = Quest.GetQuestById(_questId);
                return $"Quest: {quest.Name} Completed";
            }
        }
    }
}
