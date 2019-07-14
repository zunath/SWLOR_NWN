using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCObjectVisibilityCache: CacheBase<PCObjectVisibility>
    {
        protected override void OnCacheObjectSet(PCObjectVisibility entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCObjectVisibility entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCObjectVisibility GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
