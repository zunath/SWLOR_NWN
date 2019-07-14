using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BugReportCache: CacheBase<BugReport>
    {
        protected override void OnCacheObjectSet(BugReport entity)
        {
        }

        protected override void OnCacheObjectRemoved(BugReport entity)
        {
        }
    }
}
