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
            return (QuestRewardItem)ByID[id].Clone();
        }

        public IEnumerable<QuestRewardItem> GetAllByQuestID(int questID)
        {
            var list = new List<QuestRewardItem>();
            if (!ByQuestID.ContainsKey(questID))
                return list;

            foreach (var record in ByQuestID[questID].Values)
            {
                list.Add((QuestRewardItem)record.Clone());
            }

            return list;
        }
    }
}
