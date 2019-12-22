using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class KeyItemCategoryCache: CacheBase<KeyItemCategory>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, KeyItemCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, KeyItemCategory entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public KeyItemCategory GetByID(int id)
        {
            return ByID(id);
        }
    }
}
