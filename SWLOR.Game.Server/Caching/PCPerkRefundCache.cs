using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCPerkRefundCache: CacheBase<PCPerkRefund>
    {
        protected override void OnCacheObjectSet(PCPerkRefund entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCPerkRefund entity)
        {
        }
    }
}
