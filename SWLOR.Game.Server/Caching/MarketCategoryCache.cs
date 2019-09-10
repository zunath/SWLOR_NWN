using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class MarketCategoryCache: CacheBase<MarketCategory>
    {
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
            return (MarketCategory)ByID[id].Clone();
        }

        public IEnumerable<MarketCategory> GetAllByIDs(IEnumerable<int> ids)
        {
            var list = new List<MarketCategory>();
            foreach (var id in ids)
            {
                list.Add( (MarketCategory) ByID[id].Clone());
            }

            return list;
        }
    }
}
