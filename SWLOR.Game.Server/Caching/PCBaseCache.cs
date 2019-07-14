using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseCache: CacheBase<PCBase>
    {
        protected override void OnCacheObjectSet(PCBase entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCBase entity)
        {
        }

        public PCBase GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
