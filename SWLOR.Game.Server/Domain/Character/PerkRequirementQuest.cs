using SWLOR.Game.Server.Entity;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Domain.Character
{
    public class PerkRequirementQuest : IPerkRequirement
    {
        public string QuestId { get; }

        public PerkRequirementQuest(string questId)
        {
            QuestId = questId;
        }

        public PerkRequirementCategory Category => PerkRequirementCategory.Quest;

        public string CheckRequirements(uint player) => CheckRequirements(DB.Get<Player>(GetObjectUUID(player)), null);

        public string CheckRequirements(Player dbPlayer, Beast activeBeast)
        {
            var quest = Quest.GetQuestById(QuestId);
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
