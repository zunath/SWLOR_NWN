namespace SWLOR.Core.Service.QuestService
{
    public interface IQuestListDefinition
    {
        public Dictionary<string, QuestDetail> BuildQuests();
    }
}
