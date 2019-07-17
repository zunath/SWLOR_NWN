using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCQuestStatusCache: CacheBase<PCQuestStatus>
    {
        private Dictionary<Guid, Dictionary<int, PCQuestStatus>> ByPlayerAndQuestID { get; } = new Dictionary<Guid, Dictionary<int, PCQuestStatus>>();

        protected override void OnCacheObjectSet(PCQuestStatus entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.QuestID, entity, ByPlayerAndQuestID);
        }

        protected override void OnCacheObjectRemoved(PCQuestStatus entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.QuestID, ByPlayerAndQuestID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCQuestStatus GetByID(Guid id)
        {
            return (PCQuestStatus)ByID[id].Clone();
        }

        public PCQuestStatus GetByPlayerAndQuestID(Guid playerID, int questID)
        {
            return GetEntityFromDictionary(playerID, questID, ByPlayerAndQuestID);
        }

        public PCQuestStatus GetByPlayerAndQuestIDOrDefault(Guid playerID, int questID)
        {
            return GetEntityFromDictionaryOrDefault(playerID, questID, ByPlayerAndQuestID);
        }

        public IEnumerable<PCQuestStatus> GetAllByPlayerID(Guid playerID)
        {
            if(!ByPlayerAndQuestID.ContainsKey(playerID))
                return new List<PCQuestStatus>();

            var list = new List<PCQuestStatus>();
            foreach (var record in ByPlayerAndQuestID[playerID].Values)
            {
                list.Add((PCQuestStatus)record.Clone());
            }
            return list;
        }

    }
}
