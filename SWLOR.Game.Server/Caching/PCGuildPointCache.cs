using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCGuildPointCache: CacheBase<PCGuildPoint>
    {
        protected override void OnCacheObjectSet(PCGuildPoint entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCGuildPoint entity)
        {
        }
    }
}
