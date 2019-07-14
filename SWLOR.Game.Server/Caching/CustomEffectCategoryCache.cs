using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CustomEffectCategoryCache: CacheBase<CustomEffectCategory>
    {
        protected override void OnCacheObjectSet(CustomEffectCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(CustomEffectCategory entity)
        {
        }
    }
}
