using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class LootTableCache: CacheBase<LootTable>
    {
        public LootTableCache() 
            : base("LootTable")
        {
        }

        protected override void OnCacheObjectSet(LootTable entity)
        {
        }

        protected override void OnCacheObjectRemoved(LootTable entity)
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
