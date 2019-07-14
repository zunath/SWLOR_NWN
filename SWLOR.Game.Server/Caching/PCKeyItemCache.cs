using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCKeyItemCache: CacheBase<PCKeyItem>
    {
        protected override void OnCacheObjectSet(PCKeyItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCKeyItem entity)
        {
        }
    }
}
