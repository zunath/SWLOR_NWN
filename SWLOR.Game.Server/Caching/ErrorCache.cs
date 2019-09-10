using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ErrorCache: CacheBase<Error>
    {
        protected override void OnCacheObjectSet(Error entity)
        {
        }

        protected override void OnCacheObjectRemoved(Error entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Error GetByID(Guid id)
        {
            return (Error)ByID[id].Clone();
        }
    }
}
