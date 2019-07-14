using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCQuestStatusCache: CacheBase<PCQuestStatus>
    {
        protected override void OnCacheObjectSet(PCQuestStatus entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCQuestStatus entity)
        {
        }

        public PCQuestStatus GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
