using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestRequiredItemCache: CacheBase<QuestRequiredItem>
    {
        private Dictionary<int, Dictionary<int, QuestRequiredItem>> ByQuestStateID { get; } = new Dictionary<int, Dictionary<int, QuestRequiredItem>>();

        protected override void OnCacheObjectSet(QuestRequiredItem entity)
        {
            SetEntityIntoDictionary(entity.QuestStateID, entity.ID, entity, ByQuestStateID);
        }

        protected override void OnCacheObjectRemoved(QuestRequiredItem entity)
        {
            SetEntityIntoDictionary(entity.QuestStateID, entity.ID, entity, ByQuestStateID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public QuestRequiredItem GetByID(int id)
        {
            return ByID[id];
        }

        public IEnumerable<QuestRequiredItem> GetAllByQuestStateID(int questStateID)
        {
            return ByQuestStateID[questStateID].Values;
        }
    }
}
