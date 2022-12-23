using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementQuest : IPerkRequirement
    {
        private readonly string _questId;

        public PerkRequirementQuest(string questId)
        {
            _questId = questId;
        }

        public string CheckRequirements(uint player)
        {
            var quest = Quest.GetQuestById(_questId);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
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
