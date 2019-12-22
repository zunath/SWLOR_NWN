using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class FameRegionCache: CacheBase<FameRegion>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, FameRegion entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, FameRegion entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public FameRegion GetByID(int id)
        {
            return ByID(id);
        }
    }
}
