using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class DMActionCache: CacheBase<DMAction>
    {
        protected override void OnCacheObjectSet(DMAction entity)
        {
        }

        protected override void OnCacheObjectRemoved(DMAction entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public DMAction GetByID(Guid id)
        {
            return (DMAction)ByID[id].Clone();
        }
    }
}
