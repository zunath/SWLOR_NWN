using SWLOR.Component.Quest.Service;

namespace SWLOR.Component.Quest.Contracts
{
    public interface IQuestListDefinition
    {
        public Dictionary<string, QuestDetail> BuildQuests();
    }
}
