using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Quest.Contracts;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Quest.Prerequisite
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
