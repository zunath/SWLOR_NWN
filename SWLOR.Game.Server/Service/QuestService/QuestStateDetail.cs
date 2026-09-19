using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Service.QuestService
{
    public class QuestStateDetail
    {
        private Dictionary<int, IQuestObjective> Objectives { get; } = new Dictionary<int, IQuestObjective>();
        public List<KeyItemType> KeyItemsGrantedOnAdvance { get; } = new();
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

        /// <summary>
        /// Keeps saved counters aligned with this state's objectives after content updates,
        /// preserving progress for objectives that are still required.
        /// </summary>
        public bool ReconcileProgress(PlayerQuest quest)
        {
            var items = GetObjectives().OfType<CollectItemObjective>()
                .ToDictionary(objective => objective.Resref, objective => objective.Quantity);
            var kills = GetObjectives().OfType<KillTargetObjective>()
                .ToDictionary(objective => objective.Group, objective => objective.Amount);
            return Reconcile(quest.ItemProgresses, items) | Reconcile(quest.KillProgresses, kills);
        }

        private static bool Reconcile<T>(Dictionary<T, int> progress, Dictionary<T, int> required)
        {
            var changed = false;
            foreach (var key in progress.Keys.Where(key => !required.ContainsKey(key)).ToArray())
            {
                progress.Remove(key);
                changed = true;
            }
            foreach (var (key, amount) in required)
            {
                if (progress.ContainsKey(key)) continue;
                progress[key] = amount;
                changed = true;
            }
            return changed;
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
