using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructureItemCache: CacheBase<PCBaseStructureItem>
    {
        protected override void OnCacheObjectSet(PCBaseStructureItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCBaseStructureItem entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBaseStructureItem GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
