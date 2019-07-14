using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCOverflowItemCache: CacheBase<PCOverflowItem>
    {
        protected override void OnCacheObjectSet(PCOverflowItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCOverflowItem entity)
        {
        }
    }
}
