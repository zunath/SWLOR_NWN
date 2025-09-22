using SWLOR.Component.Quest.Service;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Component.Quest.Contracts
{
    public interface IQuestListDefinition
    {
        public Dictionary<string, QuestDetail> BuildQuests();
    }
}
