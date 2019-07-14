using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCImpoundedItemCache: CacheBase<PCImpoundedItem>
    {
        protected override void OnCacheObjectSet(PCImpoundedItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCImpoundedItem entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCImpoundedItem GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
