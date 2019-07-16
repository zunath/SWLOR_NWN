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
            return ByID[id];
        }

        public IEnumerable<QuestPrerequisite> GetAllByQuestID(int questID)
        {
            if(!ByQuestID.ContainsKey(questID))
                return new List<QuestPrerequisite>();

            return ByQuestID[questID].Values;
        }
    }
}
