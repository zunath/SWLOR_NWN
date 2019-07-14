using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCMapProgressionCache: CacheBase<PCMapProgression>
    {
        protected override void OnCacheObjectSet(PCMapProgression entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCMapProgression entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCMapProgression GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
