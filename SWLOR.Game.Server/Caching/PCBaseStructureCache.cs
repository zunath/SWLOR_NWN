using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructureCache: CacheBase<PCBaseStructure>
    {
        protected override void OnCacheObjectSet(PCBaseStructure entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCBaseStructure entity)
        {
        }

        public PCBaseStructure GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
