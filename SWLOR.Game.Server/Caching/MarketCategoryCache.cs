using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class MarketCategoryCache: CacheBase<MarketCategory>
    {
        public MarketCategoryCache() 
            : base("MarketCategory")
        {
        }

        protected override void OnCacheObjectSet(MarketCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(MarketCategory entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public MarketCategory GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<MarketCategory> GetAllByIDs(IEnumerable<int> ids)
        {
            var list = new List<MarketCategory>();
            foreach (var id in ids)
            {
                list.Add(ByID(id));
            }

            return list;
        }
    }
}
