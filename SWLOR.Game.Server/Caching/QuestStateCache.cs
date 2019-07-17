using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestStateCache: CacheBase<QuestState>
    {
        private Dictionary<int, Dictionary<int, QuestState>> ByQuestIDAndSequence { get; } = new Dictionary<int, Dictionary<int, QuestState>>();

        protected override void OnCacheObjectSet(QuestState entity)
        {
            SetEntityIntoDictionary(entity.QuestID, entity.Sequence, entity, ByQuestIDAndSequence);
        }

        protected override void OnCacheObjectRemoved(QuestState entity)
        {
            RemoveEntityFromDictionary(entity.QuestID, entity.Sequence, ByQuestIDAndSequence);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public QuestState GetByID(int id)
        {
            return (QuestState)ByID[id].Clone();
        }

        public IEnumerable<QuestState> GetAllByQuestID(int questID)
        {
            if(!ByQuestIDAndSequence.ContainsKey(questID))
                return new List<QuestState>();

            var list = new List<QuestState>();

            foreach (var record in ByQuestIDAndSequence[questID].Values)
            {
                list.Add((QuestState)record.Clone());
            }

            return list;
        }

        public QuestState GetByQuestIDAndSequence(int questID, int sequence)
        {
            return GetEntityFromDictionary(questID, sequence, ByQuestIDAndSequence);
        }

        public QuestState GetByQuestIDAndSequenceOrDefault(int questID, int sequence)
        {
            return GetEntityFromDictionaryOrDefault(questID, sequence, ByQuestIDAndSequence);
        }
    }
}
