using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class GrowingPlantCache: CacheBase<GrowingPlant>
    {
        protected override void OnCacheObjectSet(GrowingPlant entity)
        {
        }

        protected override void OnCacheObjectRemoved(GrowingPlant entity)
        {
        }

        public GrowingPlant GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
