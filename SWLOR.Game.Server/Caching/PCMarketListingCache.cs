using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCMarketListingCache: CacheBase<PCMarketListing>
    {
        private Dictionary<int, Dictionary<Guid, PCMarketListing>> ByMarketRegionID { get; } = new Dictionary<int, Dictionary<Guid, PCMarketListing>>();
        private Dictionary<Guid, Dictionary<Guid, PCMarketListing>> BySellerPlayerID { get; } = new Dictionary<Guid, Dictionary<Guid, PCMarketListing>>();

        protected override void OnCacheObjectSet(PCMarketListing entity)
        {
            SetEntityIntoDictionary(entity.MarketRegionID, entity.ID, entity, ByMarketRegionID);
            SetEntityIntoDictionary(entity.SellerPlayerID, entity.ID, entity, BySellerPlayerID);
        }

        protected override void OnCacheObjectRemoved(PCMarketListing entity)
        {
            RemoveEntityFromDictionary(entity.MarketRegionID, entity.ID, ByMarketRegionID);
            RemoveEntityFromDictionary(entity.SellerPlayerID, entity.ID, BySellerPlayerID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCMarketListing GetByID(Guid id)
        {
            return (PCMarketListing)ByID[id].Clone();
        }

        public PCMarketListing GetByIDOrDefault(Guid id)
        {
            if (!ByID.ContainsKey(id)) return default;

            return (PCMarketListing)ByID[id].Clone();
        }

        public IEnumerable<PCMarketListing> GetAllByMarketRegionID(int marketRegionID)
        {
            if(!ByMarketRegionID.ContainsKey(marketRegionID))
                return new List<PCMarketListing>();

            var list = new List<PCMarketListing>();

            foreach (var record in ByMarketRegionID[marketRegionID].Values)
            {
                list.Add((PCMarketListing)record.Clone());
            }

            return list;
        }

        public IEnumerable<PCMarketListing> GetAllBySellerPlayerID(Guid sellerPlayerID)
        {
            if(!BySellerPlayerID.ContainsKey(sellerPlayerID))
                return new List<PCMarketListing>();

            var list = new List<PCMarketListing>();
            foreach (var record in BySellerPlayerID[sellerPlayerID].Values)
            {
                list.Add((PCMarketListing)record.Clone());
            }

            return list;
        }
    }
}
