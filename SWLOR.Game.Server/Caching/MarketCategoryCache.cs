using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class MarketCategoryCache: CacheBase<MarketCategory>
    {
        protected override void OnCacheObjectSet(MarketCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(MarketCategory entity)
        {
        }
    }
}
