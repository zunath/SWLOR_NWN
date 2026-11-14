using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Quests
{
    public interface IQuestListDefinition
    {
        public Dictionary<string, QuestDetail> BuildQuests();
    }
}
