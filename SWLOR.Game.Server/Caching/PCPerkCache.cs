using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCPerkCache: CacheBase<PCPerk>
    {
        protected override void OnCacheObjectSet(PCPerk entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCPerk entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCPerk GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
