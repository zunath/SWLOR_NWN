using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ItemTypeCache: CacheBase<ItemType>
    {
        public ItemTypeCache() 
            : base("ItemType")
        {
        }

        protected override void OnCacheObjectSet(ItemType entity)
        {
        }

        protected override void OnCacheObjectRemoved(ItemType entity)
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
