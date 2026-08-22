using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementQuest : IPerkRequirement
    {
        public string QuestId { get; }

        public PerkRequirementQuest(string questId)
        {
            QuestId = questId;
        }

        public PerkRequirementCategory Category => PerkRequirementCategory.Quest;

        public string CheckRequirements(uint player)
        {
            var quest = Quest.GetQuestById(QuestId);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var error = $"You have not completed the quest '{quest.Name}'.";

            if (!dbPlayer.Quests.ContainsKey(QuestId)) return error;

            var playerQuest = dbPlayer.Quests[QuestId];
            if (playerQuest.TimesCompleted <= 0) return error;

            return string.Empty;
        }

        public string RequirementText
        {
            get
            {
                var quest = Quest.GetQuestById(QuestId);
                return $"Quest: {quest.Name} Completed";
            }
        }
    }
}
