using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class AreaCache: CacheBase<Area>
    {
        protected override void OnCacheObjectSet(Area entity)
        {
        }

        protected override void OnCacheObjectRemoved(Area entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Area GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
