using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class LootTableItemCache: CacheBase<LootTableItem>
    {
        public LootTableItemCache() 
            : base("LootTableItem")
        {
        }

        private const string ByLootTableIDIndex = "ByLootTableID";

        protected override void OnCacheObjectSet(LootTableItem entity)
        {
            SetIntoListIndex(ByLootTableIDIndex, entity.LootTableID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(LootTableItem entity)
        {
            RemoveFromListIndex(ByLootTableIDIndex, entity.LootTableID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public LootTableItem GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<LootTableItem> GetAllByLootTableID(int lootTableID)
        {
            if (!ExistsByListIndex(ByLootTableIDIndex, lootTableID.ToString()))
                return new List<LootTableItem>();

            return GetFromListIndex(ByLootTableIDIndex, lootTableID.ToString());
        }
    }
}
