using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ItemTypeCache: CacheBase<ItemType>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, ItemType entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, ItemType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public ItemType GetByID(int id)
        {
            return ByID(id);
        }
    }
}
