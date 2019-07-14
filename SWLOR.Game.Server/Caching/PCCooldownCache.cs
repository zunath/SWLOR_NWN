using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCCooldownCache: CacheBase<PCCooldown>
    {
        protected override void OnCacheObjectSet(PCCooldown entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCCooldown entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCCooldown GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
