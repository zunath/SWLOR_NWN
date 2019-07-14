using System;
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
    }
}
