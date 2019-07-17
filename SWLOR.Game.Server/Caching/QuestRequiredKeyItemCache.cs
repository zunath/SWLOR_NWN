using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestRequiredKeyItemCache: CacheBase<QuestRequiredKeyItem>
    {
        private Dictionary<int, Dictionary<int, QuestRequiredKeyItem>> ByQuestStateID { get; } = new Dictionary<int, Dictionary<int, QuestRequiredKeyItem>>();

        protected override void OnCacheObjectSet(QuestRequiredKeyItem entity)
        {
            SetEntityIntoDictionary(entity.QuestStateID, entity.ID, entity, ByQuestStateID);
        }

        protected override void OnCacheObjectRemoved(QuestRequiredKeyItem entity)
        {
            RemoveEntityFromDictionary(entity.QuestStateID, entity.ID, ByQuestStateID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public QuestRequiredKeyItem GetByID(int id)
        {
            return (QuestRequiredKeyItem)ByID[id].Clone();
        }

        public IEnumerable<QuestRequiredKeyItem> GetAllByQuestStateID(int questStateID)
        {
            var list = new List<QuestRequiredKeyItem>();
            if (!ByQuestStateID.ContainsKey(questStateID))
                return list;

            foreach (var record in ByQuestStateID[questStateID].Values)
            {
                list.Add((QuestRequiredKeyItem)record.Clone());
            }

            return list;
        }
    }
}
