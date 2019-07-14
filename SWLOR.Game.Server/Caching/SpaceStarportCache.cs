using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SpaceStarportCache: CacheBase<SpaceStarport>
    {
        protected override void OnCacheObjectSet(SpaceStarport entity)
        {
        }

        protected override void OnCacheObjectRemoved(SpaceStarport entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public SpaceStarport GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
