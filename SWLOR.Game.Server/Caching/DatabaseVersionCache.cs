using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class DatabaseVersionCache: CacheBase<DatabaseVersion>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, DatabaseVersion entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, DatabaseVersion entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public DatabaseVersion GetByID(Guid id)
        {
            return ByID(id);
        }
    }
}
