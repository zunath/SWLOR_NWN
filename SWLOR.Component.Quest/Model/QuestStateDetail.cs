using SWLOR.Component.Quest.Contracts;

namespace SWLOR.Component.Quest.Model
{
    public class QuestStateDetail
    {
        private Dictionary<int, IQuestObjective> Objectives { get; } = new();
        public string JournalText { get; set; }
        public QuestStateDetail()
        {
            JournalText = string.Empty;
        }

        public void AddObjective(IQuestObjective objective)
        {
            int index = Objectives.Count;
            Objectives[index] = objective;
        }

        public IEnumerable<IQuestObjective> GetObjectives()
        {
            return Objectives.Values;
        }

        public bool IsComplete(uint player, string questID)
        {
            foreach (var objective in Objectives)
            {
                if (!objective.Value.IsComplete(player, questID))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
