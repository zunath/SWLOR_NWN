using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCCustomEffectCache: CacheBase<PCCustomEffect>
    {
        protected override void OnCacheObjectSet(PCCustomEffect entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCCustomEffect entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCCustomEffect GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
