using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestPrerequisiteCache: CacheBase<QuestPrerequisite>
    {
        private Dictionary<int, Dictionary<int, QuestPrerequisite>> ByQuestID { get; } = new Dictionary<int, Dictionary<int, QuestPrerequisite>>();

        protected override void OnCacheObjectSet(QuestPrerequisite entity)
        {
            SetEntityIntoDictionary(entity.QuestID, entity.ID, entity, ByQuestID);
        }

        protected override void OnCacheObjectRemoved(QuestPrerequisite entity)
        {
            RemoveEntityFromDictionary(entity.QuestID, entity.ID, ByQuestID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public QuestPrerequisite GetByID(int id)
        {
            return (QuestPrerequisite)ByID[id].Clone();
        }

        public IEnumerable<QuestPrerequisite> GetAllByQuestID(int questID)
        {
            var list = new List<QuestPrerequisite>();
            if (!ByQuestID.ContainsKey(questID))
                return list;

            foreach (var record in ByQuestID[questID].Values)
            {
                list.Add((QuestPrerequisite)record.Clone());
            }
            return list;
        }
    }
}
