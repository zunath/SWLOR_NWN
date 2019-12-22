using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCQuestItemProgressCache: CacheBase<PCQuestItemProgress>
    {
        private Dictionary<Guid, Dictionary<string, PCQuestItemProgress>> ByQuestStatusIDAndResref { get; } = new Dictionary<Guid, Dictionary<string, PCQuestItemProgress>>();

        protected override void OnCacheObjectSet(string @namespace, object id, PCQuestItemProgress entity)
        {
            SetEntityIntoDictionary(entity.PCQuestStatusID, entity.Resref, entity, ByQuestStatusIDAndResref);
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, PCQuestItemProgress entity)
        {
            RemoveEntityFromDictionary(entity.PCQuestStatusID, entity.Resref, ByQuestStatusIDAndResref);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCQuestItemProgress GetByID(Guid id)
        {
            return ByID(id);
        }

        public int GetCountByPCQuestStatusID(Guid pcQuestStatusID)
        {
            return ByID.Values.Count(x => x.PCQuestStatusID == pcQuestStatusID);
        }

        public PCQuestItemProgress GetByPCQuestStatusIDAndResrefOrDefault(Guid pcQuestStatusID, string resref)
        {
            return GetEntityFromDictionaryOrDefault(pcQuestStatusID, resref, ByQuestStatusIDAndResref);
        }

        public IEnumerable<PCQuestItemProgress> GetAllByPCQuestStatusID(Guid pcQuestStatusID)
        {
            var list = new List<PCQuestItemProgress>();
            if (!ByQuestStatusIDAndResref.ContainsKey(pcQuestStatusID))
                return list;

            foreach (var record in ByQuestStatusIDAndResref[pcQuestStatusID].Values)
            {
                list.Add((PCQuestItemProgress)record.Clone());
            }

            return list;
        }
    }
}
