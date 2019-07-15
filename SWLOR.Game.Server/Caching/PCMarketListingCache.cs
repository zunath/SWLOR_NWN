using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCMarketListingCache: CacheBase<PCMarketListing>
    {
        protected override void OnCacheObjectSet(PCMarketListing entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCMarketListing entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCMarketListing GetByID(Guid id)
        {
            return ByID[id];
        }

        public PCMarketListing GetByIDOrDefault(Guid id)
        {
            if (!ByID.ContainsKey(id)) return default;

            return ByID[id];
        }
    }
}
