using System;
using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class BugReportCache: CacheBase<BugReport>
    {
        protected override void OnCacheObjectSet(BugReport entity)
        {
        }

        protected override void OnCacheObjectRemoved(BugReport entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public BugReport GetByID(Guid id)
        {
            return (BugReport)ByID[id].Clone();
        }
    }
}
