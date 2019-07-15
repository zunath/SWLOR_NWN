using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestStateCache: CacheBase<QuestState>
    {
        private Dictionary<int, List<QuestState>> ByQuestID { get; } = new Dictionary<int, List<QuestState>>();

        protected override void OnCacheObjectSet(QuestState entity)
        {
            SetEntityIntoDictionary(entity.QuestID, entity, ByQuestID);
        }

        protected override void OnCacheObjectRemoved(QuestState entity)
        {
            RemoveEntityFromDictionary(entity.QuestID, entity, ByQuestID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public QuestState GetByID(int id)
        {
            return ByID[id];
        }

        public IEnumerable<QuestState> GetAllByQuestID(int questID)
        {
            return GetEntityListFromDictionary(questID, ByQuestID);
        }
    }
}
