using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class DMActionCache: CacheBase<DMAction>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, DMAction entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, DMAction entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public DMAction GetByID(Guid id)
        {
            return ByID(id);
        }
    }
}
