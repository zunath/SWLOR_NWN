using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CraftDeviceCache: CacheBase<CraftDevice>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, CraftDevice entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, CraftDevice entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public CraftDevice GetByID(int id)
        {
            return ByID(id);
        }
    }
}
