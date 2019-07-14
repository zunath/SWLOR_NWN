using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class KeyItemCategoryCache: CacheBase<KeyItemCategory>
    {
        protected override void OnCacheObjectSet(KeyItemCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(KeyItemCategory entity)
        {
        }
    }
}
