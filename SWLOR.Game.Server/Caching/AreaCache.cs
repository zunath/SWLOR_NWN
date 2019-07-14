using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class AreaCache: CacheBase<Area>
    {
        protected override void OnCacheObjectSet(Area entity)
        {
        }

        protected override void OnCacheObjectRemoved(Area entity)
        {
        }
    }
}
