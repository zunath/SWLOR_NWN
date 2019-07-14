using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCQuestItemProgressCache: CacheBase<PCQuestItemProgress>
    {
        protected override void OnCacheObjectSet(PCQuestItemProgress entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCQuestItemProgress entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCQuestItemProgress GetByID(Guid id)
        {
            return ByID[id];
        }

        public int GetCountByPCQuestStatusID(Guid pcQuestStatusID)
        {
            return ByID.Values.Count(x => x.PCQuestStatusID == pcQuestStatusID);
        }
    }
}
