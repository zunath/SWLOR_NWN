using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCMarketListingCache: CacheBase<PCMarketListing>
    {
        public PCMarketListingCache() 
            : base("PCMarketListing")
        {
        }

        private const string ByMarketRegionIDIndex = "ByMarketRegionID";
        private const string BySellerPlayerIDIndex = "BySellerPlayerID";

        protected override void OnCacheObjectSet(PCMarketListing entity)
        {
            SetIntoListIndex(ByMarketRegionIDIndex, entity.MarketRegionID.ToString(), entity);
            SetIntoListIndex(BySellerPlayerIDIndex, entity.SellerPlayerID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCMarketListing entity)
        {
            RemoveFromListIndex(ByMarketRegionIDIndex, entity.MarketRegionID.ToString(), entity);
            RemoveFromListIndex(BySellerPlayerIDIndex, entity.SellerPlayerID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCMarketListing GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCMarketListing GetByIDOrDefault(Guid id)
        {
            if (!Exists(id)) 
                return default;

            return ByID(id);
        }

        public IEnumerable<PCMarketListing> GetAllByMarketRegionID(int marketRegionID)
        {
            if(!ExistsByListIndex(ByMarketRegionIDIndex, marketRegionID.ToString()))
                return new List<PCMarketListing>();

            return GetFromListIndex(ByMarketRegionIDIndex, marketRegionID.ToString());
        }

        public IEnumerable<PCMarketListing> GetAllBySellerPlayerID(Guid sellerPlayerID)
        {
            if(!ExistsByListIndex(BySellerPlayerIDIndex, sellerPlayerID.ToString()))
                return new List<PCMarketListing>();

            return GetFromListIndex(BySellerPlayerIDIndex, sellerPlayerID.ToString());
        }
    }
}
