using System.Collections.Generic;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;

namespace SWLOR.Game.Server.Quest
{
    public class QuestState: IQuestState
    {
        private Dictionary<int, IQuestObjective> Objectives { get; } = new Dictionary<int, IQuestObjective>();
        
        public void AddObjective(IQuestObjective objective)
        {
            int index = Objectives.Count;
            Objectives[index] = objective;
        }

        public IEnumerable<IQuestObjective> GetObjectives()
        {
            return Objectives.Values;
        }

        public bool IsComplete(NWPlayer player, int questID)
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
