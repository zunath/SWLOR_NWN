using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CooldownCategoryCache: CacheBase<CooldownCategory>
    {
        protected override void OnCacheObjectSet(CooldownCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(CooldownCategory entity)
        {
        }
    }
}
