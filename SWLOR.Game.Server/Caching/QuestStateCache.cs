using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestStateCache: CacheBase<QuestState>
    {
        private Dictionary<int, List<QuestState>> ByQuestID { get; } = new Dictionary<int, List<QuestState>>();
        private Dictionary<int, Dictionary<int, QuestState>> ByQuestIDAndSequence { get; } = new Dictionary<int, Dictionary<int, QuestState>>();

        protected override void OnCacheObjectSet(QuestState entity)
        {
            SetEntityIntoDictionary(entity.QuestID, entity, ByQuestID);
            SetEntityIntoDictionary(entity.QuestID, entity.Sequence, entity, ByQuestIDAndSequence);
        }

        protected override void OnCacheObjectRemoved(QuestState entity)
        {
            RemoveEntityFromDictionary(entity.QuestID, entity, ByQuestID);
            RemoveEntityFromDictionary(entity.QuestID, entity.Sequence, ByQuestIDAndSequence);
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

        public QuestState GetByQuestIDAndSequence(int questID, int sequence)
        {
            return GetEntityFromDictionary(questID, sequence, ByQuestIDAndSequence);
        }
    }
}
