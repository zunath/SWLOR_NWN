using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestKillTargetCache: CacheBase<QuestKillTarget>
    {
        private Dictionary<int, Dictionary<int, QuestKillTarget>> ByQuestStateID { get; } = new Dictionary<int, Dictionary<int, QuestKillTarget>>();

        protected override void OnCacheObjectSet(QuestKillTarget entity)
        {
            SetEntityIntoDictionary(entity.QuestStateID, entity.ID, entity, ByQuestStateID);
        }

        protected override void OnCacheObjectRemoved(QuestKillTarget entity)
        {
            RemoveEntityFromDictionary(entity.QuestStateID, entity.ID, ByQuestStateID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public QuestKillTarget GetByID(int id)
        {
            return (QuestKillTarget)ByID[id].Clone();
        }

        public IEnumerable<QuestKillTarget> GetAllByQuestStateID(int questStateID)
        {
            var list = new List<QuestKillTarget>();
            if (!ByQuestStateID.ContainsKey(questStateID))
                return list;

            foreach(var record in ByQuestStateID[questStateID].Values)
            {
                list.Add((QuestKillTarget)record.Clone());
            }

            return list;
        }
    }
}
