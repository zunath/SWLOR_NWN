using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CustomEffectCache: CacheBase<CustomEffect>
    {
        protected override void OnCacheObjectSet(CustomEffect entity)
        {
        }

        protected override void OnCacheObjectRemoved(CustomEffect entity)
        {
        }
    }
}
