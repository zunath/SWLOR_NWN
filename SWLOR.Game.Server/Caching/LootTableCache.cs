using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class LootTableCache: CacheBase<LootTable>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, LootTable entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, LootTable entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public LootTable GetByID(int id)
        {
            return ByID(id);
        }

        public LootTable GetByIDOrDefault(int id)
        {
            if (Exists(id))
                return ByID(id);
            else return default;
        }
    }
}
