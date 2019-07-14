using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCSearchSiteItemCache: CacheBase<PCSearchSiteItem>
    {
        protected override void OnCacheObjectSet(PCSearchSiteItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCSearchSiteItem entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCSearchSiteItem GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
