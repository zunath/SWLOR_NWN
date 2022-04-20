using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.QuestService
{
    public interface IQuestListDefinition
    {
        public Dictionary<string, QuestDetail> BuildQuests();
    }
}
