using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCQuestStatusCache: CacheBase<PCQuestStatus>
    {
        private Dictionary<Tuple<Guid, int>, PCQuestStatus> ByPlayerAndQuestID { get; } = new Dictionary<Tuple<Guid, int>, PCQuestStatus>();

        protected override void OnCacheObjectSet(PCQuestStatus entity)
        {
            ByPlayerAndQuestID[new Tuple<Guid, int>(entity.PlayerID, entity.QuestID)] = entity;
        }

        protected override void OnCacheObjectRemoved(PCQuestStatus entity)
        {
            ByPlayerAndQuestID.Remove(new Tuple<Guid, int>(entity.PlayerID, entity.QuestID));
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCQuestStatus GetByID(Guid id)
        {
            return ByID[id];
        }

        public PCQuestStatus GetByPlayerAndQuestID(Guid playerID, int questID)
        {
            return ByPlayerAndQuestID[new Tuple<Guid, int>(playerID, questID)];
        }

        public PCQuestStatus GetByPlayerAndQuestIDOrDefault(Guid playerID, int questID)
        {
            var key = new Tuple<Guid, int>(playerID, questID);
            if (!ByPlayerAndQuestID.ContainsKey(key)) return default;

            return ByPlayerAndQuestID[key];
        }

    }
}
