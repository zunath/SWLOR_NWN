using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCSearchSiteCache: CacheBase<PCSearchSite>
    {
        protected override void OnCacheObjectSet(PCSearchSite entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCSearchSite entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCSearchSite GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
