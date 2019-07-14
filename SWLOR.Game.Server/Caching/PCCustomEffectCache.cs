using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCCustomEffectCache: CacheBase<PCCustomEffect>
    {
        protected override void OnCacheObjectSet(PCCustomEffect entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCCustomEffect entity)
        {
        }
    }
}
