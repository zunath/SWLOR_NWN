using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class FameRegionCache: CacheBase<FameRegion>
    {
        protected override void OnCacheObjectSet(FameRegion entity)
        {
        }

        protected override void OnCacheObjectRemoved(FameRegion entity)
        {
        }
    }
}
