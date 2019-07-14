using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCMapPinCache: CacheBase<PCMapPin>
    {
        protected override void OnCacheObjectSet(PCMapPin entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCMapPin entity)
        {
        }
    }
}
