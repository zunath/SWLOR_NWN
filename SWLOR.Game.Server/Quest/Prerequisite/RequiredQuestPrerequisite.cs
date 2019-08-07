using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Prerequisite
{
    public class RequiredQuestPrerequisite: IQuestPrerequisite
    {
        private readonly int _questID;

        public RequiredQuestPrerequisite(int questID)
        {
            _questID = questID;
        }

        public bool MeetsPrerequisite(NWPlayer player)
        {
            var quest = QuestService.GetQuestByID(_questID);
            return quest.IsComplete(player);
        }
    }
}
