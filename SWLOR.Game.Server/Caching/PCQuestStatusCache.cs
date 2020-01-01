using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCQuestStatusCache: CacheBase<PCQuestStatus>
    {
        public PCQuestStatusCache() 
            : base("PCQuestStatus")
        {
        }

        private const string ByPlayerIDIndex = "ByPlayerID";
        
        protected override void OnCacheObjectSet(PCQuestStatus entity)
        {
            SetIntoListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCQuestStatus entity)
        {
            RemoveFromListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCQuestStatus GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCQuestStatus GetByPlayerAndQuestID(Guid playerID, int questID)
        {
            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .Single(x => x.QuestID == questID);
        }

        public PCQuestStatus GetByPlayerAndQuestIDOrDefault(Guid playerID, int questID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return default;

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .SingleOrDefault(x => x.QuestID == questID);
        }

        public IEnumerable<PCQuestStatus> GetAllByPlayerID(Guid playerID)
        {
            if(!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return new List<PCQuestStatus>();

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
        }

    }
}
