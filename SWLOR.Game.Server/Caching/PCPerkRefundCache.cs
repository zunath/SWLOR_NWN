using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCPerkRefundCache: CacheBase<PCPerkRefund>
    {
        public PCPerkRefundCache() 
            : base("PCPerkRefund")
        {
        }

        protected override void OnCacheObjectSet(PCPerkRefund entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCPerkRefund entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCPerkRefund GetByID(Guid id)
        {
            return ByID(id);
        }
    }
}
