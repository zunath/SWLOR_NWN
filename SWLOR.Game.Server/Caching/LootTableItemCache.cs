using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class LootTableItemCache: CacheBase<LootTableItem>
    {
        protected override void OnCacheObjectSet(LootTableItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(LootTableItem entity)
        {
        }
    }
}
