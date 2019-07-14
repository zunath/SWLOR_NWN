using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCQuestKillTargetProgressCache: CacheBase<PCQuestKillTargetProgress>
    {
        protected override void OnCacheObjectSet(PCQuestKillTargetProgress entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCQuestKillTargetProgress entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCQuestKillTargetProgress GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
