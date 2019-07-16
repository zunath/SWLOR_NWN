using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestRewardItemCache: CacheBase<QuestRewardItem>
    {
        private Dictionary<int, Dictionary<int, QuestRewardItem>> ByQuestID { get; } = new Dictionary<int, Dictionary<int, QuestRewardItem>>();

        protected override void OnCacheObjectSet(QuestRewardItem entity)
        {
            SetEntityIntoDictionary(entity.QuestID, entity.ID, entity, ByQuestID);
        }

        protected override void OnCacheObjectRemoved(QuestRewardItem entity)
        {
            RemoveEntityFromDictionary(entity.QuestID, entity.ID, ByQuestID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public QuestRewardItem GetByID(int id)
        {
            return ByID[id];
        }

        public IEnumerable<QuestRewardItem> GetAllByQuestID(int questID)
        {
            return ByQuestID[questID].Values;
        }
    }
}
