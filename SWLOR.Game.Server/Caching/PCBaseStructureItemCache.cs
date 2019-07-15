using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructureItemCache: CacheBase<PCBaseStructureItem>
    {
        private Dictionary<string, PCBaseStructureItem> ByItemGlobalID { get; } = new Dictionary<string, PCBaseStructureItem>();

        protected override void OnCacheObjectSet(PCBaseStructureItem entity)
        {
            ByItemGlobalID[entity.ItemGlobalID] = entity;
        }

        protected override void OnCacheObjectRemoved(PCBaseStructureItem entity)
        {
            ByItemGlobalID.Remove(entity.ItemGlobalID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBaseStructureItem GetByID(Guid id)
        {
            return ByID[id];
        }

        public PCBaseStructureItem GetByItemGlobalID(string itemGlobalID)
        {
            return ByItemGlobalID[itemGlobalID];
        }
    }
}
