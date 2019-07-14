using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ItemTypeCache: CacheBase<ItemType>
    {
        protected override void OnCacheObjectSet(ItemType entity)
        {
        }

        protected override void OnCacheObjectRemoved(ItemType entity)
        {
        }
    }
}
